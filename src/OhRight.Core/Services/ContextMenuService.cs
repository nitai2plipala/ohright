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
/// 右键菜单管理服务
/// </summary>
public class ContextMenuService : IContextMenuService, IAsyncDisposable
{
    private readonly IRegistryService _registryService;
    private readonly ILoggerService _logger;
    private readonly OhRight.Core.Configuration.RegistryOptions _options;
    private bool _disposed;

    public ContextMenuService(
        IRegistryService registryService,
        OhRight.Core.Configuration.RegistryOptions options,
        ILoggerService? logger = null)
    {
        _registryService = registryService ?? throw new ArgumentNullException(nameof(registryService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? new SerilogService(_options);
    }

    /// <summary>
    /// 获取右键菜单项列表
    /// </summary>
    public async Task<IReadOnlyList<ContextMenuItem>> GetContextMenuItemsAsync(ContextMenuType targetType, string? extension = null, PermissionLevel permissionLevel = PermissionLevel.User, CancellationToken cancellationToken = default)
    {
        _logger.Debug("获取右键菜单项（业务层）: TargetType={TargetType}, Extension={Extension}", targetType, extension);

        try
        {
            var items = await _registryService.GetContextMenuItemsAsync(targetType, extension, permissionLevel, cancellationToken);

            // 按 Position 排序
            var sortedItems = items.OrderBy(i => i.Position).ThenBy(i => i.Name).ToList();

            _logger.Debug("成功获取 {Count} 个右键菜单项", sortedItems.Count);
            return sortedItems;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取右键菜单项失败: TargetType={TargetType}, Extension={Extension}", targetType, extension);
            throw;
        }
    }

    /// <summary>
    /// 添加右键菜单项
    /// </summary>
    public async Task<bool> AddContextMenuItemAsync(ContextMenuItem item, ContextMenuType targetType, string? extension = null, CancellationToken cancellationToken = default)
    {
        _logger.Debug("添加右键菜单项（业务层）: {Name}, TargetType={TargetType}, Extension={Extension}",
            item.Name, targetType, extension);

        try
        {
            // 验证参数
            if (string.IsNullOrEmpty(item.Name))
                throw new ArgumentException("菜单项名称不能为空", nameof(item));

            if (string.IsNullOrEmpty(item.Command))
                throw new ArgumentException("菜单项命令不能为空", nameof(item));

            // 检查是否已存在
            var existingItems = await GetContextMenuItemsAsync(targetType, extension, item.PermissionLevel, cancellationToken);
            if (existingItems.Any(i => i.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"菜单项 '{item.Name}' 已存在");
            }

            // 计算 Position（如果未指定）
            if (item.Position <= 0)
            {
                var maxPosition = existingItems.Any() ? existingItems.Max(i => i.Position) : 0;
                item.Position = maxPosition + 1;
            }

            return await _registryService.AddContextMenuItemAsync(item, targetType, extension, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "添加右键菜单项失败: {Name}", item.Name);
            throw;
        }
    }

    /// <summary>
    /// 移除右键菜单项
    /// </summary>
    public async Task<bool> RemoveContextMenuItemAsync(string itemId, ContextMenuType targetType, string? extension = null, CancellationToken cancellationToken = default)
    {
        _logger.Debug("移除右键菜单项（业务层）: {ItemId}, TargetType={TargetType}", itemId, targetType);

        try
        {
            return await _registryService.RemoveContextMenuItemAsync(itemId, targetType, extension, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "移除右键菜单项失败: {ItemId}", itemId);
            throw;
        }
    }

    /// <summary>
    /// 启用/禁用右键菜单项
    /// </summary>
    public async Task<bool> ToggleContextMenuItemAsync(string itemId, bool enabled, ContextMenuType targetType, string? extension = null, CancellationToken cancellationToken = default)
    {
        _logger.Debug("切换右键菜单项状态: {ItemId}, Enabled={Enabled}", itemId, enabled);

        try
        {
            // 获取所有菜单项
            var items = await GetContextMenuItemsAsync(targetType, extension, PermissionLevel.User, cancellationToken);
            var item = items.FirstOrDefault(i => i.Id == itemId);

            if (item == null)
            {
                _logger.Warning("未找到菜单项: {ItemId}", itemId);
                return false;
            }

            // 如果状态相同，无需更改
            if (item.Enabled == enabled)
            {
                _logger.Debug("菜单项状态未改变: {ItemId}, Enabled={Enabled}", itemId, enabled);
                return true;
            }

            // 通过重命名注册表键来切换启用/禁用状态
            var oldMenuName = item.Enabled ? item.Name : RegistryPaths.DisabledMenuPrefix + item.Name;
            var newMenuName = enabled ? item.Name : RegistryPaths.DisabledMenuPrefix + item.Name;

            var oldPath = $@"{item.RegistryPath}";
            var newPath = oldPath.Replace(oldMenuName, newMenuName);

            try
            {
                // 获取父键
                var parentPath = Path.GetDirectoryName(oldPath) ?? string.Empty;
                using var parentKey = Registry.ClassesRoot.OpenSubKey(parentPath, true);
                if (parentKey == null)
                {
                    throw new RegistryKeyNotFoundException(parentPath, "无法打开父注册表键");
                }

                // 重命名键（手动实现，因为 RegistryKey 没有 RenameSubKey 方法）
                using var oldKey = parentKey.OpenSubKey(oldMenuName, true);
                if (oldKey != null)
                {
                    // 复制所有子键和值到新键
                    parentKey.CreateSubKey(newMenuName);
                    using var newKey = parentKey.OpenSubKey(newMenuName, true);
                    if (newKey != null)
                    {
                        foreach (var valueName in oldKey.GetValueNames())
                        {
                            newKey.SetValue(valueName, oldKey.GetValue(valueName));
                        }
                        foreach (var subKeyName in oldKey.GetSubKeyNames())
                        {
                            CopySubKey(oldKey, newKey, subKeyName);
                        }
                    }
                    parentKey.DeleteSubKeyTree(oldMenuName);
                }

                _logger.Information("成功切换菜单项状态: {ItemId}, Enabled={Enabled}", itemId, enabled);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "切换菜单项状态失败: {ItemId}", itemId);
                throw new RegistryException($"切换菜单项状态失败：{ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "切换右键菜单项状态失败: {ItemId}", itemId);
            throw;
        }
    }

    /// <summary>
    /// 重新排序右键菜单项
    /// </summary>
    public async Task ReorderContextMenuItemsAsync(IEnumerable<string> orderedItemIds, ContextMenuType targetType, string? extension = null, CancellationToken cancellationToken = default)
    {
        _logger.Debug("重新排序右键菜单项: TargetType={TargetType}, Extension={Extension}", targetType, extension);

        try
        {
            var items = await GetContextMenuItemsAsync(targetType, extension, PermissionLevel.User, cancellationToken);
            var itemDict = items.ToDictionary(i => i.Id);

            var position = 1;
            foreach (var itemId in orderedItemIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (itemDict.TryGetValue(itemId, out var item))
                {
                    item.Position = position++;
                    // 更新注册表中的 Position 值
                    await UpdateMenuItemPositionAsync(item, cancellationToken);
                }
            }

            _logger.Information("右键菜单项重新排序完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "重新排序右键菜单项失败");
            throw;
        }
    }

    /// <summary>
    /// 更新菜单项位置
    /// </summary>
    private async Task UpdateMenuItemPositionAsync(ContextMenuItem item, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var menuName = item.Enabled ? item.Name : RegistryPaths.DisabledMenuPrefix + item.Name;
                var menuPath = $@"{item.RegistryPath}";

                using var menuKey = Registry.ClassesRoot.OpenSubKey(menuPath, true);
                if (menuKey != null)
                {
                    menuKey.SetValue("Position", item.Position.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "更新菜单项位置失败: {ItemName}", item.Name);
                throw;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 获取右键菜单统计信息
    /// </summary>
    public async Task<ContextMenuStatistics> GetStatisticsAsync(ContextMenuType? targetType = null, string? extension = null, CancellationToken cancellationToken = default)
    {
        _logger.Debug("获取右键菜单统计信息: TargetType={TargetType}, Extension={Extension}", targetType, extension);

        try
        {
            IEnumerable<ContextMenuItem> allItems;

            if (targetType.HasValue)
            {
                allItems = await GetContextMenuItemsAsync(targetType.Value, extension, PermissionLevel.User, cancellationToken);
            }
            else
            {
                // 获取所有类型的菜单项
                var allList = new List<ContextMenuItem>();
                foreach (var type in Enum.GetValues<ContextMenuType>().Where(t => t != ContextMenuType.Unknown))
                {
                    var items = await GetContextMenuItemsAsync(type, null, PermissionLevel.User, cancellationToken);
                    allList.AddRange(items);
                }
                allItems = allList;
            }

            var stats = new ContextMenuStatistics
            {
                TotalCount = allItems.Count(),
                EnabledCount = allItems.Count(i => i.Enabled),
                DisabledCount = allItems.Count(i => !i.Enabled),
                ByType = allItems.GroupBy(i => i.Type).ToDictionary(g => g.Key, g => g.Count()),
                ByExtension = allItems.Where(i => i.TargetExtension != null)
                                      .GroupBy(i => i.TargetExtension!)
                                      .ToDictionary(g => g.Key, g => g.Count())
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取右键菜单统计信息失败");
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Run(() =>
        {
            if (!_disposed)
            {
                _logger.Debug("释放 ContextMenuService 资源");
                _disposed = true;
            }
        });
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 复制注册表子键及其所有子键和值
    /// </summary>
    private static void CopySubKey(RegistryKey sourceKey, RegistryKey destKey, string subKeyName)
    {
        using var sourceSubKey = sourceKey.OpenSubKey(subKeyName);
        if (sourceSubKey == null) return;

        using var destSubKey = destKey.CreateSubKey(subKeyName);
        if (destSubKey == null) return;

        // 复制所有值
        foreach (var valueName in sourceSubKey.GetValueNames())
        {
            destSubKey.SetValue(valueName, sourceSubKey.GetValue(valueName));
        }

        // 递归复制所有子键
        foreach (var name in sourceSubKey.GetSubKeyNames())
        {
            CopySubKey(sourceSubKey, destSubKey, name);
        }
    }
}

/// <summary>
/// 右键菜单管理服务接口（扩展）
/// </summary>
public interface IContextMenuService : IDisposable, IAsyncDisposable
{
    Task<IReadOnlyList<ContextMenuItem>> GetContextMenuItemsAsync(ContextMenuType targetType, string? extension = null, PermissionLevel permissionLevel = PermissionLevel.User, CancellationToken cancellationToken = default);
    Task<bool> AddContextMenuItemAsync(ContextMenuItem item, ContextMenuType targetType, string? extension = null, CancellationToken cancellationToken = default);
    Task<bool> RemoveContextMenuItemAsync(string itemId, ContextMenuType targetType, string? extension = null, CancellationToken cancellationToken = default);
    Task<bool> ToggleContextMenuItemAsync(string itemId, bool enabled, ContextMenuType targetType, string? extension = null, CancellationToken cancellationToken = default);
    Task ReorderContextMenuItemsAsync(IEnumerable<string> orderedItemIds, ContextMenuType targetType, string? extension = null, CancellationToken cancellationToken = default);
    Task<ContextMenuStatistics> GetStatisticsAsync(ContextMenuType? targetType = null, string? extension = null, CancellationToken cancellationToken = default);
}
