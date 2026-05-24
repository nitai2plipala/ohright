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
/// 打开方式管理服务
/// </summary>
public class OpenWithService : IOpenWithService, IAsyncDisposable
{
    private readonly IRegistryService _registryService;
    private readonly ILoggerService _logger;
    private readonly OhRight.Core.Configuration.RegistryOptions _options;
    private bool _disposed;

    public OpenWithService(
        IRegistryService registryService,
        OhRight.Core.Configuration.RegistryOptions options,
        ILoggerService? logger = null)
    {
        _registryService = registryService ?? throw new ArgumentNullException(nameof(registryService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? new SerilogService(_options);
    }

    /// <summary>
    /// 获取打开方式列表
    /// </summary>
    public async Task<IReadOnlyList<OpenWithEntry>> GetOpenWithListAsync(string extension, PermissionLevel permissionLevel = PermissionLevel.User, CancellationToken cancellationToken = default)
    {
        _logger.Debug("获取打开方式列表（业务层）: {Extension}", extension);

        try
        {
            var entries = await _registryService.GetOpenWithListAsync(extension, permissionLevel, cancellationToken);

            // 按类型和名称排序
            var sortedEntries = entries.OrderBy(e => e.Type).ThenBy(e => e.Name).ToList();

            _logger.Debug("成功获取 {Count} 个打开方式条目", sortedEntries.Count);
            return sortedEntries;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取打开方式列表失败: {Extension}", extension);
            throw;
        }
    }

    /// <summary>
    /// 获取用户级打开方式列表
    /// </summary>
    public async Task<IReadOnlyList<OpenWithEntry>> GetUserOpenWithListAsync(string extension, CancellationToken cancellationToken = default)
    {
        return await GetOpenWithListAsync(extension, PermissionLevel.User, cancellationToken);
    }

    /// <summary>
    /// 获取系统级打开方式列表
    /// </summary>
    public async Task<IReadOnlyList<OpenWithEntry>> GetSystemOpenWithListAsync(string extension, CancellationToken cancellationToken = default)
    {
        return await GetOpenWithListAsync(extension, PermissionLevel.System, cancellationToken);
    }

    /// <summary>
    /// 添加打开方式
    /// </summary>
    public async Task<bool> AddOpenWithEntryAsync(OpenWithEntry entry, string extension, CancellationToken cancellationToken = default)
    {
        _logger.Debug("添加打开方式（业务层）: {ProgId}, Extension={Extension}, Type={Type}",
            entry.ProgId, extension, entry.Type);

        try
        {
            // 验证参数
            if (string.IsNullOrEmpty(entry.ProgId))
                throw new ArgumentException("ProgID 不能为空", nameof(entry));

            if (string.IsNullOrEmpty(entry.Name))
                entry.Name = entry.ProgId;

            // 检查是否已存在
            var existingEntries = await GetOpenWithListAsync(extension, entry.PermissionLevel, cancellationToken);
            if (existingEntries.Any(e => e.ProgId.Equals(entry.ProgId, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"打开方式 '{entry.ProgId}' 已存在");
            }

            // 设置扩展名
            entry.Extension = extension.StartsWith('.') ? extension : "." + extension;

            return await _registryService.AddOpenWithEntryAsync(entry, extension, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "添加打开方式失败: {ProgId}", entry.ProgId);
            throw;
        }
    }

    /// <summary>
    /// 移除打开方式
    /// </summary>
    public async Task<bool> RemoveOpenWithEntryAsync(string progId, string extension, CancellationToken cancellationToken = default)
    {
        _logger.Debug("移除打开方式（业务层）: {ProgId}, Extension={Extension}", progId, extension);

        try
        {
            return await _registryService.RemoveOpenWithEntryAsync(progId, extension, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "移除打开方式失败: {ProgId}", progId);
            throw;
        }
    }

    /// <summary>
    /// 设置默认打开方式
    /// </summary>
    public async Task<bool> SetDefaultOpenWithAsync(string progId, string extension, bool updateUserChoice = true, CancellationToken cancellationToken = default)
    {
        _logger.Debug("设置默认打开方式（业务层）: {ProgId}, Extension={Extension}, UpdateUserChoice={UpdateUserChoice}",
            progId, extension, updateUserChoice);

        try
        {
            // 设置基础关联
            var association = new FileAssociation
            {
                Extension = extension,
                ProgId = progId,
                PermissionLevel = PermissionLevel.Administrator
            };

            var result = await _registryService.SetFileAssociationAsync(association, force: true, cancellationToken);

            if (result && updateUserChoice)
            {
                // 更新 UserChoice
                var userChoiceService = _registryService as IUserChoiceService;
                if (userChoiceService != null)
                {
                    await userChoiceService.WriteUserChoiceAsync(extension, progId, cancellationToken);
                    _logger.Debug("已更新 UserChoice: {Extension} -> {ProgId}", extension, progId);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "设置默认打开方式失败: {ProgId}", progId);
            throw;
        }
    }

    /// <summary>
    /// 更新 MRU 列表（最近使用顺序）
    /// </summary>
    public async Task UpdateMruListAsync(string extension, IEnumerable<string> orderedProgIds, CancellationToken cancellationToken = default)
    {
        _logger.Debug("更新 MRU 列表: Extension={Extension}, Count={Count}", extension, orderedProgIds.Count());

        try
        {
            if (!extension.StartsWith('.'))
                extension = "." + extension;

            var mruPath = string.Format(RegistryPaths.MRUListTemplate, extension);
            var listPath = RegistryPaths.GetOpenWithListPath(extension);

            // 使用 a-z 字符作为 MRU 键
            var mruChars = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
            var progIdList = orderedProgIds.Take(mruChars.Length).ToList();

            using var listKey = Registry.ClassesRoot.CreateSubKey(listPath);
            if (listKey == null)
            {
                throw new RegistryKeyNotFoundException(listPath, "无法创建 OpenWithList 键");
            }

            // 清除现有值
            foreach (var valueName in listKey.GetValueNames())
            {
                listKey.DeleteValue(valueName, false);
            }

            // 写入新的 MRU 列表
            for (var i = 0; i < progIdList.Count; i++)
            {
                var mruValue = mruChars[i].ToString();
                listKey.SetValue(mruValue, progIdList[i]);
            }

            // 更新 MRUList 键
            using var mruListKey = Registry.ClassesRoot.CreateSubKey(mruPath);
            mruListKey?.SetValue("MRUList", new string(mruChars, 0, progIdList.Count));

            _logger.Information("MRU 列表更新完成: {Extension}", extension);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "更新 MRU 列表失败: {Extension}", extension);
            throw new RegistryException($"更新 MRU 列表失败：{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 清理卸载残留
    /// </summary>
    public async Task<int> CleanupUninstalledEntriesAsync(string extension, CancellationToken cancellationToken = default)
    {
        _logger.Debug("清理卸载残留（业务层）: {Extension}", extension);

        try
        {
            return await _registryService.CleanupUninstalledEntriesAsync(extension, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "清理卸载残留失败: {Extension}", extension);
            throw;
        }
    }

    /// <summary>
    /// 验证打开方式有效性
    /// </summary>
    public async Task<bool> ValidateOpenWithEntryAsync(OpenWithEntry entry, CancellationToken cancellationToken = default)
    {
        _logger.Debug("验证打开方式条目: {ProgId}", entry.ProgId);

        try
        {
            // 检查 ProgID 是否存在
            using var progIdKey = Registry.ClassesRoot.OpenSubKey(entry.ProgId);
            if (progIdKey == null)
            {
                _logger.Debug("ProgID 不存在: {ProgId}", entry.ProgId);
                return false;
            }

            // 检查命令是否存在
            using var commandKey = progIdKey.OpenSubKey(RegistryPaths.CommandPath);
            if (commandKey == null)
            {
                _logger.Debug("命令键不存在: {ProgId}", entry.ProgId);
                return false;
            }

            var command = commandKey.GetValue("") as string;
            if (string.IsNullOrEmpty(command))
            {
                _logger.Debug("命令为空: {ProgId}", entry.ProgId);
                return false;
            }

            // 检查可执行文件是否存在（可选）
            // 这里可以扩展检查文件是否存在

            _logger.Debug("打开方式验证通过: {ProgId}", entry.ProgId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "验证打开方式时发生错误: {ProgId}", entry.ProgId);
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Run(() =>
        {
            if (!_disposed)
            {
                _logger.Debug("释放 OpenWithService 资源");
                _disposed = true;
            }
        });
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

/// <summary>
/// 打开方式管理服务接口（扩展）
/// </summary>
public interface IOpenWithService : IDisposable, IAsyncDisposable
{
    Task<IReadOnlyList<OpenWithEntry>> GetOpenWithListAsync(string extension, PermissionLevel permissionLevel = PermissionLevel.User, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OpenWithEntry>> GetUserOpenWithListAsync(string extension, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OpenWithEntry>> GetSystemOpenWithListAsync(string extension, CancellationToken cancellationToken = default);
    Task<bool> AddOpenWithEntryAsync(OpenWithEntry entry, string extension, CancellationToken cancellationToken = default);
    Task<bool> RemoveOpenWithEntryAsync(string progId, string extension, CancellationToken cancellationToken = default);
    Task<bool> SetDefaultOpenWithAsync(string progId, string extension, bool updateUserChoice = true, CancellationToken cancellationToken = default);
    Task UpdateMruListAsync(string extension, IEnumerable<string> orderedProgIds, CancellationToken cancellationToken = default);
    Task<int> CleanupUninstalledEntriesAsync(string extension, CancellationToken cancellationToken = default);
    Task<bool> ValidateOpenWithEntryAsync(OpenWithEntry entry, CancellationToken cancellationToken = default);
}
