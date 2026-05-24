using OhRight.Core.Enums;
using OhRight.Core.Models;

namespace OhRight.Cli.Services;

/// <summary>
/// CLI 服务接口
/// </summary>
public interface ICliService
{
    /// <summary>
    /// 获取文件关联信息
    /// </summary>
    Task<FileAssociation?> GetFileAssociationAsync(string extension, PermissionLevel permissionLevel = PermissionLevel.User);

    /// <summary>
    /// 设置文件关联
    /// </summary>
    Task<bool> SetFileAssociationAsync(string extension, string progId, bool force = false);

    /// <summary>
    /// 获取右键菜单项列表
    /// </summary>
    Task<IReadOnlyList<ContextMenuItem>> GetContextMenuItemsAsync(string targetType, string? extension = null);

    /// <summary>
    /// 添加右键菜单项
    /// </summary>
    Task<bool> AddContextMenuItemAsync(string name, string command, string targetType, string? extension = null, string? icon = null, int position = 0);

    /// <summary>
    /// 移除右键菜单项
    /// </summary>
    Task<bool> RemoveContextMenuItemAsync(string itemId, string targetType, string? extension = null);

    /// <summary>
    /// 获取打开方式列表
    /// </summary>
    Task<IReadOnlyList<OpenWithEntry>> GetOpenWithListAsync(string extension);

    /// <summary>
    /// 添加打开方式
    /// </summary>
    Task<bool> AddOpenWithEntryAsync(string progId, string name, string command, string extension, bool isUser = true);

    /// <summary>
    /// 移除打开方式
    /// </summary>
    Task<bool> RemoveOpenWithEntryAsync(string progId, string extension);
}