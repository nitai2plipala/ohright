using Microsoft.Win32;
using OhRight.Core.Configuration;
using OhRight.Core.Constants;
using OhRight.Core.Enums;
using OhRight.Core.Exceptions;
using OhRight.Core.Interfaces;
using OhRight.Core.Logging;
using OhRight.Core.Models;

namespace OhRight.Core.Services;

/// <summary>
/// 基础注册表操作服务实现
/// </summary>
public class RegistryService : IRegistryService, IAsyncDisposable
{
    private readonly ILoggerService _logger;
    private readonly OhRight.Core.Configuration.RegistryOptions _options;
    private bool _disposed;

    public RegistryService(OhRight.Core.Configuration.RegistryOptions options, ILoggerService? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? new SerilogService(_options);
    }

    /// <summary>
    /// 获取文件关联信息
    /// </summary>
    public async Task<FileAssociation> GetFileAssociationAsync(string extension, PermissionLevel permissionLevel = PermissionLevel.User, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(extension))
                throw new ArgumentException("扩展名不能为空", nameof(extension));

            // 确保扩展名以点开头
            if (!extension.StartsWith('.'))
                extension = "." + extension;

            _logger.Debug("获取文件关联信息: {Extension}", extension);

            try
            {
                using var key = Registry.ClassesRoot.OpenSubKey(extension);
                if (key == null)
                {
                    var path = RegistryPaths.GetFileExtensionPath(extension);
                    throw new RegistryKeyNotFoundException(path, $"找不到扩展名 {extension} 的注册表项");
                }

                var progId = key.GetValue("") as string ?? string.Empty;
                var association = new FileAssociation
                {
                    Extension = extension,
                    ProgId = progId,
                    RegistryPath = RegistryPaths.GetFileExtensionPath(extension),
                    PermissionLevel = permissionLevel,
                    CreatedTime = DateTime.Now,
                    ModifiedTime = DateTime.Now
                };

                // 获取 ProgID 信息
                if (!string.IsNullOrEmpty(progId))
                {
                    using var progIdKey = Registry.ClassesRoot.OpenSubKey(progId);
                    if (progIdKey != null)
                    {
                        association.Description = progIdKey.GetValue("") as string ?? string.Empty;

                        // 获取命令
                        using var commandKey = progIdKey.OpenSubKey(RegistryPaths.CommandPath);
                        if (commandKey != null)
                        {
                            association.Command = commandKey.GetValue("") as string ?? string.Empty;
                        }

                        // 获取图标
                        using var iconKey = progIdKey.OpenSubKey(RegistryPaths.DefaultIconPath);
                        if (iconKey != null)
                        {
                            association.Icon = iconKey.GetValue("") as string ?? string.Empty;
                        }
                    }
                }

                _logger.Debug("成功获取文件关联: {Extension} -> {ProgId}", extension, progId);
                return association;
            }
            catch (RegistryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取文件关联信息时发生错误: {Extension}", extension);
                throw new RegistryException($"获取文件关联信息时发生错误：{ex.Message}", ex);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 设置文件关联
    /// </summary>
    public async Task<bool> SetFileAssociationAsync(FileAssociation association, bool force = false, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (association == null)
                throw new ArgumentNullException(nameof(association));

            if (string.IsNullOrEmpty(association.Extension))
                throw new ArgumentException("扩展名不能为空", nameof(association));

            if (string.IsNullOrEmpty(association.ProgId))
                throw new ArgumentException("ProgID 不能为空", nameof(association));

            // 确保扩展名以点开头
            if (!association.Extension.StartsWith('.'))
                association.Extension = "." + association.Extension;

            _logger.Debug("设置文件关联: {Extension} -> {ProgId}, Force: {Force}", association.Extension, association.ProgId, force);

            try
            {
                // 检查权限
                CheckPermission(association.PermissionLevel);

                // 设置扩展名到 ProgID 的映射
                using var extensionKey = Registry.ClassesRoot.CreateSubKey(association.Extension);
                extensionKey?.SetValue("", association.ProgId);

                // 设置 ProgID 信息
                using var progIdKey = Registry.ClassesRoot.CreateSubKey(association.ProgId);
                if (progIdKey != null)
                {
                    if (!string.IsNullOrEmpty(association.Description))
                    {
                        progIdKey.SetValue("", association.Description);
                    }

                    // 设置命令
                    if (!string.IsNullOrEmpty(association.Command))
                    {
                        using var commandKey = progIdKey.CreateSubKey(RegistryPaths.CommandPath);
                        commandKey?.SetValue("", association.Command);
                    }

                    // 设置图标
                    if (!string.IsNullOrEmpty(association.Icon))
                    {
                        using var iconKey = progIdKey.CreateSubKey(RegistryPaths.DefaultIconPath);
                        iconKey?.SetValue("", association.Icon);
                    }
                }

                _logger.Information("成功设置文件关联: {Extension} -> {ProgId}", association.Extension, association.ProgId);
                return true;
            }
            catch (RegistryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "设置文件关联时发生错误: {Extension} -> {ProgId}", association.Extension, association.ProgId);
                throw new RegistryException($"设置文件关联时发生错误：{ex.Message}", ex);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 检查权限
    /// </summary>
    private void CheckPermission(PermissionLevel requiredLevel)
    {
        if (_options.RequireAdministrator && requiredLevel == PermissionLevel.Administrator)
        {
            // 在实际实现中，这里会检查是否具有管理员权限
            // 暂时简化处理
            _logger.Debug("需要管理员权限，当前检查通过");
        }
    }

    public async Task<IReadOnlyList<ContextMenuItem>> GetContextMenuItemsAsync(ContextMenuType targetType, string? extension = null, PermissionLevel permissionLevel = PermissionLevel.User, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var items = new List<ContextMenuItem>();
            string basePath;

            switch (targetType)
            {
                case ContextMenuType.File:
                    basePath = RegistryPaths.AllFilesContextMenu;
                    break;
                case ContextMenuType.Directory:
                    basePath = RegistryPaths.DirectoryContextMenu;
                    break;
                case ContextMenuType.All:
                    basePath = RegistryPaths.AllObjectsContextMenu;
                    break;
                case ContextMenuType.Unknown:
                    throw new ArgumentException("未知的菜单类型", nameof(targetType));
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetType));
            }

            // 如果是特定扩展名
            if (!string.IsNullOrEmpty(extension))
            {
                if (!extension.StartsWith('.'))
                    extension = "." + extension;
                basePath = extension;
            }

            _logger.Debug("获取右键菜单项: TargetType={TargetType}, Extension={Extension}", targetType, extension);

            try
            {
                using var baseKey = Registry.ClassesRoot.OpenSubKey(basePath);
                if (baseKey == null)
                {
                    _logger.Debug("基础路径不存在: {BasePath}", basePath);
                    return items;
                }

                using var shellKey = baseKey.OpenSubKey(RegistryPaths.ShellPath);
                if (shellKey == null)
                {
                    _logger.Debug("Shell 路径不存在: {BasePath}\\shell", basePath);
                    return items;
                }

                foreach (var menuName in shellKey.GetSubKeyNames())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using var menuKey = shellKey.OpenSubKey(menuName);
                    if (menuKey == null) continue;

                    var name = menuKey.GetValue("") as string ?? menuName;
                    var command = string.Empty;
                    var icon = string.Empty;
                    var enabled = true;

                    // 检查是否禁用（以 - 开头）
                    if (menuName.StartsWith(RegistryPaths.DisabledMenuPrefix))
                    {
                        enabled = false;
                        name = name.TrimStart(RegistryPaths.DisabledMenuPrefix[0]);
                    }

                    // 获取命令
                    using var commandKey = menuKey.OpenSubKey(RegistryPaths.CommandPath);
                    if (commandKey != null)
                    {
                        command = commandKey.GetValue("") as string ?? string.Empty;
                    }

                    // 获取图标
                    using var iconKey = menuKey.OpenSubKey(RegistryPaths.DefaultIconPath);
                    if (iconKey != null)
                    {
                        icon = iconKey.GetValue("") as string ?? string.Empty;
                    }

                    var item = new ContextMenuItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = name,
                        Command = command,
                        Icon = icon,
                        Enabled = enabled,
                        Type = targetType,
                        TargetExtension = extension,
                        RegistryPath = $@"{basePath}\{RegistryPaths.ShellPath}\{menuName}",
                        CreatedTime = DateTime.Now,
                        ModifiedTime = DateTime.Now,
                        IsShellEx = false
                    };

                    items.Add(item);
                }

                _logger.Debug("成功获取 {Count} 个右键菜单项", items.Count);
                return items;
            }
            catch (RegistryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取右键菜单项时发生错误: TargetType={TargetType}, Extension={Extension}", targetType, extension);
                throw new RegistryException($"获取右键菜单项时发生错误：{ex.Message}", ex);
            }
        }, cancellationToken);
    }

    public async Task<bool> AddContextMenuItemAsync(ContextMenuItem item, ContextMenuType targetType, string? extension = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (string.IsNullOrEmpty(item.Name))
                throw new ArgumentException("菜单项名称不能为空", nameof(item));

            string basePath;

            switch (targetType)
            {
                case ContextMenuType.File:
                    basePath = RegistryPaths.AllFilesContextMenu;
                    break;
                case ContextMenuType.Directory:
                    basePath = RegistryPaths.DirectoryContextMenu;
                    break;
                case ContextMenuType.All:
                    basePath = RegistryPaths.AllObjectsContextMenu;
                    break;
                case ContextMenuType.Unknown:
                    throw new ArgumentException("未知的菜单类型", nameof(targetType));
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetType));
            }

            // 如果是特定扩展名
            if (!string.IsNullOrEmpty(extension))
            {
                if (!extension.StartsWith('.'))
                    extension = "." + extension;
                basePath = extension;
            }

            _logger.Debug("添加右键菜单项: {Name}, TargetType={TargetType}, Extension={Extension}", item.Name, targetType, extension);

            try
            {
                CheckPermission(item.PermissionLevel);

                var menuName = item.Enabled ? item.Name : RegistryPaths.DisabledMenuPrefix + item.Name;
                var menuPath = $@"{basePath}\{RegistryPaths.ShellPath}\{menuName}";

                using var menuKey = Registry.ClassesRoot.CreateSubKey(menuPath);
                if (menuKey == null)
                {
                    _logger.Warning("创建菜单键失败: {MenuPath}", menuPath);
                    return false;
                }

                // 设置菜单项名称
                if (!string.IsNullOrEmpty(item.Name))
                {
                    menuKey.SetValue("", item.Name);
                }

                // 设置命令
                if (!string.IsNullOrEmpty(item.Command))
                {
                    using var commandKey = menuKey.CreateSubKey(RegistryPaths.CommandPath);
                    commandKey?.SetValue("", item.Command);
                }

                // 设置图标
                if (!string.IsNullOrEmpty(item.Icon))
                {
                    using var iconKey = menuKey.CreateSubKey(RegistryPaths.DefaultIconPath);
                    iconKey?.SetValue("", item.Icon);
                }

                // 设置位置顺序
                if (item.Position > 0)
                {
                    menuKey.SetValue("Position", item.Position.ToString());
                }

                _logger.Information("成功添加右键菜单项: {Name} at {MenuPath}", item.Name, menuPath);
                return true;
            }
            catch (RegistryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "添加右键菜单项时发生错误: {Name}", item.Name);
                throw new RegistryException($"添加右键菜单项时发生错误：{ex.Message}", ex);
            }
        }, cancellationToken);
    }

    public async Task<bool> RemoveContextMenuItemAsync(string itemId, ContextMenuType targetType, string? extension = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(itemId))
                throw new ArgumentException("菜单项 ID 不能为空", nameof(itemId));

            _logger.Debug("移除右键菜单项: {ItemId}, TargetType={TargetType}", itemId, targetType);

            try
            {
                // 获取所有菜单项
                var items = GetContextMenuItemsAsync(targetType, extension, PermissionLevel.User, cancellationToken).GetAwaiter().GetResult();
                var item = items.FirstOrDefault(i => i.Id == itemId);

                if (item == null)
                {
                    _logger.Warning("未找到菜单项: {ItemId}", itemId);
                    return false;
                }

                var menuName = item.Enabled ? item.Name : RegistryPaths.DisabledMenuPrefix + item.Name;
                var menuPath = item.RegistryPath;

                // 删除注册表项
                Registry.ClassesRoot.DeleteSubKeyTree(menuPath, false);

                _logger.Information("成功移除右键菜单项: {ItemId} at {MenuPath}", itemId, menuPath);
                return true;
            }
            catch (RegistryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "移除右键菜单项时发生错误: {ItemId}", itemId);
                throw new RegistryException($"移除右键菜单项时发生错误：{ex.Message}", ex);
            }
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<OpenWithEntry>> GetOpenWithListAsync(string extension, PermissionLevel permissionLevel = PermissionLevel.User, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entries = new List<OpenWithEntry>();

            if (string.IsNullOrEmpty(extension))
                throw new ArgumentException("扩展名不能为空", nameof(extension));

            if (!extension.StartsWith('.'))
                extension = "." + extension;

            _logger.Debug("获取打开方式列表: {Extension}", extension);

            try
            {
                // 获取 OpenWithProgids
                var progIdsPath = RegistryPaths.GetOpenWithProgidsPath(extension);
                using var progIdsKey = Registry.ClassesRoot.OpenSubKey(progIdsPath);
                if (progIdsKey != null)
                {
                    foreach (var progId in progIdsKey.GetValueNames())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (string.IsNullOrEmpty(progId)) continue;

                        // 获取 ProgID 信息
                        var name = string.Empty;
                        var command = string.Empty;
                        var icon = string.Empty;

                        using var progIdKey = Registry.ClassesRoot.OpenSubKey(progId);
                        if (progIdKey != null)
                        {
                            name = progIdKey.GetValue("") as string ?? progId;

                            using var commandKey = progIdKey.OpenSubKey(RegistryPaths.CommandPath);
                            if (commandKey != null)
                            {
                                command = commandKey.GetValue("") as string ?? string.Empty;
                            }

                            using var iconKey = progIdKey.OpenSubKey(RegistryPaths.DefaultIconPath);
                            if (iconKey != null)
                            {
                                icon = iconKey.GetValue("") as string ?? string.Empty;
                            }
                        }

                        entries.Add(new OpenWithEntry
                        {
                            ProgId = progId,
                            Name = name,
                            Command = command,
                            Icon = icon,
                            Type = OpenWithType.ProgId,
                            Extension = extension,
                            RegistryPath = $@"{RegistryPaths.HKCR}\{progIdsPath}\{progId}",
                            CreatedTime = DateTime.Now,
                            ModifiedTime = DateTime.Now,
                            PermissionLevel = permissionLevel
                        });
                    }
                }

                // 获取 OpenWithList
                var listPath = RegistryPaths.GetOpenWithListPath(extension);
                using var listKey = Registry.ClassesRoot.OpenSubKey(listPath);
                if (listKey != null)
                {
                    foreach (var program in listKey.GetValueNames())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (string.IsNullOrEmpty(program)) continue;

                        // 获取程序信息
                        var command = string.Empty;
                        var icon = string.Empty;

                        // 尝试从 ProgID 获取信息
                        using var progIdKey = Registry.ClassesRoot.OpenSubKey(program);
                        if (progIdKey != null)
                        {
                            using var commandKey = progIdKey.OpenSubKey(RegistryPaths.CommandPath);
                            if (commandKey != null)
                            {
                                command = commandKey.GetValue("") as string ?? string.Empty;
                            }

                            using var iconKey = progIdKey.OpenSubKey(RegistryPaths.DefaultIconPath);
                            if (iconKey != null)
                            {
                                icon = iconKey.GetValue("") as string ?? string.Empty;
                            }
                        }

                        entries.Add(new OpenWithEntry
                        {
                            ProgId = program,
                            Name = program,
                            Command = command,
                            Icon = icon,
                            Type = OpenWithType.List,
                            Extension = extension,
                            RegistryPath = $@"{RegistryPaths.HKCR}\{listPath}\{program}",
                            CreatedTime = DateTime.Now,
                            ModifiedTime = DateTime.Now,
                            PermissionLevel = permissionLevel
                        });
                    }
                }

                _logger.Debug("成功获取 {Count} 个打开方式条目", entries.Count);
                return entries;
            }
            catch (RegistryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "获取打开方式列表时发生错误: {Extension}", extension);
                throw new RegistryException($"获取打开方式列表时发生错误：{ex.Message}", ex);
            }
        }, cancellationToken);
    }

    public async Task<bool> AddOpenWithEntryAsync(OpenWithEntry entry, string extension, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (string.IsNullOrEmpty(extension))
                throw new ArgumentException("扩展名不能为空", nameof(extension));

            if (string.IsNullOrEmpty(entry.ProgId))
                throw new ArgumentException("ProgID 不能为空", nameof(entry));

            if (!extension.StartsWith('.'))
                extension = "." + extension;

            _logger.Debug("添加打开方式: {ProgId}, Extension={Extension}, Type={Type}", entry.ProgId, extension, entry.Type);

            try
            {
                CheckPermission(entry.PermissionLevel);

                if (entry.Type == OpenWithType.ProgId)
                {
                    var path = RegistryPaths.GetOpenWithProgidsPath(extension);
                    using var key = Registry.ClassesRoot.CreateSubKey(path);
                    key?.SetValue(entry.ProgId, "");
                }
                else
                {
                    var path = RegistryPaths.GetOpenWithListPath(extension);
                    using var key = Registry.ClassesRoot.CreateSubKey(path);
                    key?.SetValue(entry.ProgId, "");
                }

                _logger.Information("成功添加打开方式: {ProgId} to {Extension}", entry.ProgId, extension);
                return true;
            }
            catch (RegistryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "添加打开方式时发生错误: {ProgId}", entry.ProgId);
                throw new RegistryException($"添加打开方式时发生错误：{ex.Message}", ex);
            }
        }, cancellationToken);
    }

    public async Task<bool> RemoveOpenWithEntryAsync(string progId, string extension, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(progId))
                throw new ArgumentException("ProgID 不能为空", nameof(progId));

            if (string.IsNullOrEmpty(extension))
                throw new ArgumentException("扩展名不能为空", nameof(extension));

            if (!extension.StartsWith('.'))
                extension = "." + extension;

            _logger.Debug("移除打开方式: {ProgId}, Extension={Extension}", progId, extension);

            try
            {
                // 尝试从 OpenWithProgids 移除
                var progIdsPath = RegistryPaths.GetOpenWithProgidsPath(extension);
                using var progIdsKey = Registry.ClassesRoot.OpenSubKey(progIdsPath, true);
                if (progIdsKey != null)
                {
                    try
                    {
                        progIdsKey.DeleteValue(progId, false);
                        _logger.Debug("从 OpenWithProgids 移除: {ProgId}", progId);
                    }
                    catch (ArgumentException)
                    {
                        // 如果不存在，忽略
                    }
                }

                // 尝试从 OpenWithList 移除
                var listPath = RegistryPaths.GetOpenWithListPath(extension);
                using var listKey = Registry.ClassesRoot.OpenSubKey(listPath, true);
                if (listKey != null)
                {
                    try
                    {
                        listKey.DeleteValue(progId, false);
                        _logger.Debug("从 OpenWithList 移除: {ProgId}", progId);
                    }
                    catch (ArgumentException)
                    {
                        // 如果不存在，忽略
                    }
                }

                _logger.Information("成功移除打开方式: {ProgId} from {Extension}", progId, extension);
                return true;
            }
            catch (RegistryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "移除打开方式时发生错误: {ProgId}", progId);
                throw new RegistryException($"移除打开方式时发生错误：{ex.Message}", ex);
            }
        }, cancellationToken);
    }

    public async Task<bool> SetDefaultOpenWithAsync(string progId, string extension, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(progId))
                throw new ArgumentException("ProgID 不能为空", nameof(progId));

            if (string.IsNullOrEmpty(extension))
                throw new ArgumentException("扩展名不能为空", nameof(extension));

            if (!extension.StartsWith('.'))
                extension = "." + extension;

            _logger.Debug("设置默认打开方式: {ProgId}, Extension={Extension}", progId, extension);

            try
            {
                CheckPermission(PermissionLevel.Administrator);

                // 设置扩展名的默认 ProgID
                using var extensionKey = Registry.ClassesRoot.CreateSubKey(extension);
                extensionKey?.SetValue("", progId);

                _logger.Information("成功设置默认打开方式: {ProgId} for {Extension}", progId, extension);
                return true;
            }
            catch (RegistryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "设置默认打开方式时发生错误: {ProgId}", progId);
                throw new RegistryException($"设置默认打开方式时发生错误：{ex.Message}", ex);
            }
        }, cancellationToken);
    }

    public async Task<int> CleanupUninstalledEntriesAsync(string extension, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(extension))
                throw new ArgumentException("扩展名不能为空", nameof(extension));

            if (!extension.StartsWith('.'))
                extension = "." + extension;

            _logger.Debug("清理卸载残留: {Extension}", extension);

            var cleanedCount = 0;

            try
            {
                // 清理 OpenWithProgids 中的无效条目
                var progIdsPath = RegistryPaths.GetOpenWithProgidsPath(extension);
                using var progIdsKey = Registry.ClassesRoot.OpenSubKey(progIdsPath, true);
                if (progIdsKey != null)
                {
                    foreach (var progId in progIdsKey.GetValueNames())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (string.IsNullOrEmpty(progId)) continue;

                        // 检查 ProgID 是否存在
                        using var progIdKey = Registry.ClassesRoot.OpenSubKey(progId);
                        if (progIdKey == null)
                        {
                            progIdsKey.DeleteValue(progId, false);
                            cleanedCount++;
                            _logger.Debug("清理无效 ProgID: {ProgId}", progId);
                        }
                    }
                }

                // 清理 OpenWithList 中的无效条目
                var listPath = RegistryPaths.GetOpenWithListPath(extension);
                using var listKey = Registry.ClassesRoot.OpenSubKey(listPath, true);
                if (listKey != null)
                {
                    foreach (var program in listKey.GetValueNames())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (string.IsNullOrEmpty(program)) continue;

                        // 检查程序是否存在
                        using var progIdKey = Registry.ClassesRoot.OpenSubKey(program);
                        if (progIdKey == null)
                        {
                            listKey.DeleteValue(program, false);
                            cleanedCount++;
                            _logger.Debug("清理无效程序: {Program}", program);
                        }
                    }
                }

                _logger.Information("清理完成: {Extension}, 清理了 {Count} 个条目", extension, cleanedCount);
                return cleanedCount;
            }
            catch (RegistryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "清理卸载残留时发生错误: {Extension}", extension);
                throw new RegistryException($"清理卸载残留时发生错误：{ex.Message}", ex);
            }
        }, cancellationToken);
    }

    public async Task<bool> IsAdministratorAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "检查管理员权限时发生错误");
                return false;
            }
        }, cancellationToken);
    }

    public async Task<bool> RequestAdministratorPrivilegeAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 在实际应用中，这会启动一个新的进程并请求管理员权限
            // 这里返回当前是否已经是管理员
            _logger.Warning("RequestAdministratorPrivilegeAsync 尚未完整实现，返回当前管理员状态");
            return IsAdministratorAsync(cancellationToken).GetAwaiter().GetResult();
        }, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Run(() =>
        {
            if (!_disposed)
            {
                _logger.Debug("释放 RegistryService 资源");
                _disposed = true;
            }
        });
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
