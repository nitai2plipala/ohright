using OhRight.Core.Enums;
using OhRight.Core.Models;

namespace OhRight.Core.Interfaces;

/// <summary>
/// 基础注册表操作接口
/// </summary>
public interface IRegistryService : IDisposable
{
    /// <summary>
    /// 获取文件关联信息
    /// </summary>
    Task<FileAssociation> GetFileAssociationAsync(string extension, PermissionLevel permissionLevel = PermissionLevel.User, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置文件关联
    /// </summary>
    Task<bool> SetFileAssociationAsync(FileAssociation association, bool force = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取右键菜单项列表
    /// </summary>
    Task<IReadOnlyList<ContextMenuItem>> GetContextMenuItemsAsync(ContextMenuType targetType, string? extension = null, PermissionLevel permissionLevel = PermissionLevel.User, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加右键菜单项
    /// </summary>
    Task<bool> AddContextMenuItemAsync(ContextMenuItem item, ContextMenuType targetType, string? extension = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除右键菜单项
    /// </summary>
    Task<bool> RemoveContextMenuItemAsync(string itemId, ContextMenuType targetType, string? extension = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取打开方式列表
    /// </summary>
    Task<IReadOnlyList<OpenWithEntry>> GetOpenWithListAsync(string extension, PermissionLevel permissionLevel = PermissionLevel.User, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加打开方式
    /// </summary>
    Task<bool> AddOpenWithEntryAsync(OpenWithEntry entry, string extension, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除打开方式
    /// </summary>
    Task<bool> RemoveOpenWithEntryAsync(string progId, string extension, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置默认打开方式
    /// </summary>
    Task<bool> SetDefaultOpenWithAsync(string progId, string extension, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理卸载残留
    /// </summary>
    Task<int> CleanupUninstalledEntriesAsync(string extension, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查管理员权限
    /// </summary>
    Task<bool> IsAdministratorAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 请求管理员权限（UAC 提升）
    /// </summary>
    Task<bool> RequestAdministratorPrivilegeAsync(CancellationToken cancellationToken = default);
}
