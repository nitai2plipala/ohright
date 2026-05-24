using OhRight.Core.Enums;
using OhRight.Core.Logging;
using OhRight.Core.Models;
using OhRight.Core.Services;

namespace OhRight.Cli.Services;

/// <summary>
/// CLI 服务实现
/// </summary>
public class CliService : ICliService
{
    private readonly IFileAssociationService _fileAssociationService;
    private readonly IContextMenuService _contextMenuService;
    private readonly IOpenWithService _openWithService;
    private readonly ILoggerService _logger;

    public CliService(
        IFileAssociationService fileAssociationService,
        IContextMenuService contextMenuService,
        IOpenWithService openWithService,
        ILoggerService logger)
    {
        _fileAssociationService = fileAssociationService ?? throw new ArgumentNullException(nameof(fileAssociationService));
        _contextMenuService = contextMenuService ?? throw new ArgumentNullException(nameof(contextMenuService));
        _openWithService = openWithService ?? throw new ArgumentNullException(nameof(openWithService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取文件关联信息
    /// </summary>
    public async Task<FileAssociation?> GetFileAssociationAsync(string extension, PermissionLevel permissionLevel = PermissionLevel.User)
    {
        try
        {
            _logger.Debug("获取文件关联: {Extension}", extension);
            return await _fileAssociationService.GetFileAssociationAsync(extension, permissionLevel);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取文件关联失败: {Extension}", extension);
            throw;
        }
    }

    /// <summary>
    /// 设置文件关联
    /// </summary>
    public async Task<bool> SetFileAssociationAsync(string extension, string progId, bool force = false)
    {
        try
        {
            _logger.Debug("设置文件关联: {Extension} -> {ProgId}", extension, progId);

            var association = new FileAssociation
            {
                Extension = extension,
                ProgId = progId
            };

            return await _fileAssociationService.SetFileAssociationAsync(association, force);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "设置文件关联失败: {Extension} -> {ProgId}", extension, progId);
            throw;
        }
    }

    /// <summary>
    /// 获取右键菜单项列表
    /// </summary>
    public async Task<IReadOnlyList<ContextMenuItem>> GetContextMenuItemsAsync(string targetType, string? extension = null)
    {
        try
        {
            var type = ParseContextMenuType(targetType);
            _logger.Debug("获取右键菜单项: Type={Type}, Extension={Extension}", targetType, extension);
            return await _contextMenuService.GetContextMenuItemsAsync(type, extension);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取右键菜单项失败");
            throw;
        }
    }

    /// <summary>
    /// 添加右键菜单项
    /// </summary>
    public async Task<bool> AddContextMenuItemAsync(string name, string command, string targetType, string? extension = null, string? icon = null, int position = 0)
    {
        try
        {
            var type = ParseContextMenuType(targetType);
            _logger.Debug("添加右键菜单项: {Name}", name);

            var item = new ContextMenuItem
            {
                Name = name,
                Command = command,
                Icon = icon,
                Position = position,
                Enabled = true
            };

            return await _contextMenuService.AddContextMenuItemAsync(item, type, extension);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "添加右键菜单项失败: {Name}", name);
            throw;
        }
    }

    /// <summary>
    /// 移除右键菜单项
    /// </summary>
    public async Task<bool> RemoveContextMenuItemAsync(string itemId, string targetType, string? extension = null)
    {
        try
        {
            var type = ParseContextMenuType(targetType);
            _logger.Debug("移除右键菜单项: {ItemId}", itemId);
            return await _contextMenuService.RemoveContextMenuItemAsync(itemId, type, extension);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "移除右键菜单项失败: {ItemId}", itemId);
            throw;
        }
    }

    /// <summary>
    /// 获取打开方式列表
    /// </summary>
    public async Task<IReadOnlyList<OpenWithEntry>> GetOpenWithListAsync(string extension)
    {
        try
        {
            _logger.Debug("获取打开方式列表: {Extension}", extension);
            return await _openWithService.GetOpenWithListAsync(extension);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取打开方式列表失败: {Extension}", extension);
            throw;
        }
    }

    /// <summary>
    /// 添加打开方式
    /// </summary>
    public async Task<bool> AddOpenWithEntryAsync(string progId, string name, string command, string extension, bool isUser = true)
    {
        try
        {
            _logger.Debug("添加打开方式: {ProgId} -> {Extension}", progId, extension);

            var entry = new OpenWithEntry
            {
                ProgId = progId,
                Name = name,
                Command = command,
                PermissionLevel = isUser ? PermissionLevel.User : PermissionLevel.Administrator
            };

            return await _openWithService.AddOpenWithEntryAsync(entry, extension);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "添加打开方式失败: {ProgId} -> {Extension}", progId, extension);
            throw;
        }
    }

    /// <summary>
    /// 移除打开方式
    /// </summary>
    public async Task<bool> RemoveOpenWithEntryAsync(string progId, string extension)
    {
        try
        {
            _logger.Debug("移除打开方式: {ProgId} -> {Extension}", progId, extension);
            return await _openWithService.RemoveOpenWithEntryAsync(progId, extension);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "移除打开方式失败: {ProgId} -> {Extension}", progId, extension);
            throw;
        }
    }

    /// <summary>
    /// 解析右键菜单类型
    /// </summary>
    private static ContextMenuType ParseContextMenuType(string type)
    {
        return type?.ToLowerInvariant() switch
        {
            "file" => ContextMenuType.File,
            "directory" => ContextMenuType.Directory,
            "folder" => ContextMenuType.Directory,
            "all" => ContextMenuType.All,
            _ => ContextMenuType.File
        };
    }
}