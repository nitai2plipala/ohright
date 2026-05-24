using Grpc.Core;
using OhRight.Core.Configuration;
using OhRight.Core.Enums;
using OhRight.Core.Interfaces;
using OhRight.Core.Logging;
using OhRight.Core.Services;
using OhRight.Grpc;

namespace OhRight.Grpc.Server.Services;

/// <summary>
/// gRPC 注册表服务实现
/// </summary>
public class RegistryGrpcService : RegistryService.RegistryServiceBase
{
    private readonly IRegistryService _registryService;
    private readonly IFileAssociationService _fileAssociationService;
    private readonly IContextMenuService _contextMenuService;
    private readonly IOpenWithService _openWithService;
    private readonly ILoggerService _logger;
    private readonly string _version = "1.0.0";

    public RegistryGrpcService(
        IRegistryService registryService,
        IFileAssociationService fileAssociationService,
        IContextMenuService contextMenuService,
        IOpenWithService openWithService,
        ILoggerService logger)
    {
        _registryService = registryService ?? throw new ArgumentNullException(nameof(registryService));
        _fileAssociationService = fileAssociationService ?? throw new ArgumentNullException(nameof(fileAssociationService));
        _contextMenuService = contextMenuService ?? throw new ArgumentNullException(nameof(contextMenuService));
        _openWithService = openWithService ?? throw new ArgumentNullException(nameof(openWithService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取文件关联信息
    /// </summary>
    public override async Task<FileAssociationResponse> GetFileAssociation(
        FileAssociationRequest request,
        ServerCallContext context)
    {
        _logger.Debug("gRPC: GetFileAssociation - Extension: {Extension}", request.Extension);

        try
        {
            if (string.IsNullOrEmpty(request.Extension))
            {
                return new FileAssociationResponse
                {
                    ErrorCode = ErrorCode.InvalidArgument,
                    ErrorMessage = "扩展名不能为空"
                };
            }

            var association = await _fileAssociationService.GetFileAssociationAsync(request.Extension);

            return new FileAssociationResponse
            {
                Association = new FileAssociation
                {
                    Extension = association.Extension,
                    ProgId = association.ProgId,
                    Command = association.Command ?? string.Empty,
                    Description = association.Description ?? string.Empty
                },
                ErrorCode = ErrorCode.Success
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "GetFileAssociation 失败: {Extension}", request.Extension);
            return new FileAssociationResponse
            {
                ErrorCode = ErrorCode.InternalError,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 设置文件关联
    /// </summary>
    public override async Task<SetFileAssociationResponse> SetFileAssociation(
        SetFileAssociationRequest request,
        ServerCallContext context)
    {
        _logger.Debug("gRPC: SetFileAssociation - Extension: {Extension}, ProgId: {ProgId}",
            request.Association?.Extension, request.Association?.ProgId);

        try
        {
            if (request.Association == null || string.IsNullOrEmpty(request.Association.Extension))
            {
                return new SetFileAssociationResponse
                {
                    Success = false,
                    ErrorCode = ErrorCode.InvalidArgument,
                    ErrorMessage = "无效的文件关联参数"
                };
            }

            var association = new OhRight.Core.Models.FileAssociation
            {
                Extension = request.Association.Extension,
                ProgId = request.Association.ProgId,
                Command = request.Association.Command,
                Description = request.Association.Description
            };

            var result = await _fileAssociationService.SetFileAssociationAsync(association, request.Force);

            return new SetFileAssociationResponse
            {
                Success = result,
                ErrorCode = result ? ErrorCode.Success : ErrorCode.InternalError,
                ErrorMessage = result ? null : "设置失败"
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "SetFileAssociation 失败");
            return new SetFileAssociationResponse
            {
                Success = false,
                ErrorCode = ErrorCode.InternalError,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 获取右键菜单项列表
    /// </summary>
    public override async Task<ContextMenuResponse> GetContextMenuItems(
        ContextMenuRequest request,
        ServerCallContext context)
    {
        _logger.Debug("gRPC: GetContextMenuItems - Type: {Type}, Extension: {Extension}",
            request.TargetType, request.Extension);

        try
        {
            var targetType = ParseContextMenuType(request.TargetType);
            var items = await _contextMenuService.GetContextMenuItemsAsync(targetType, request.Extension);

            var response = new ContextMenuResponse
            {
                ErrorCode = ErrorCode.Success
            };

            foreach (var item in items)
            {
                response.Items.Add(new ContextMenuItem
                {
                    Id = item.Id,
                    Name = item.Name,
                    Command = item.Command ?? string.Empty,
                    Icon = item.Icon ?? string.Empty,
                    Enabled = item.Enabled,
                    Position = item.Position,
                    Type = ConvertToGrpcContextMenuType(item.Type)
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "GetContextMenuItems 失败");
            return new ContextMenuResponse
            {
                ErrorCode = ErrorCode.InternalError,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 添加右键菜单项
    /// </summary>
    public override async Task<AddContextMenuResponse> AddContextMenuItem(
        AddContextMenuRequest request,
        ServerCallContext context)
    {
        _logger.Debug("gRPC: AddContextMenuItem - Name: {Name}", request.Item?.Name);

        try
        {
            if (request.Item == null)
            {
                return new AddContextMenuResponse
                {
                    Success = false,
                    ErrorCode = ErrorCode.InvalidArgument,
                    ErrorMessage = "无效的菜单项参数"
                };
            }

            var item = new OhRight.Core.Models.ContextMenuItem
            {
                Name = request.Item.Name,
                Command = request.Item.Command,
                Icon = request.Item.Icon,
                Enabled = request.Item.Enabled,
                Position = request.Item.Position
            };

            var targetType = ParseContextMenuType(request.TargetType);
            var result = await _contextMenuService.AddContextMenuItemAsync(item, targetType, request.Extension);

            return new AddContextMenuResponse
            {
                Success = result,
                ErrorCode = result ? ErrorCode.Success : ErrorCode.InternalError,
                ErrorMessage = result ? null : "添加失败"
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "AddContextMenuItem 失败");
            return new AddContextMenuResponse
            {
                Success = false,
                ErrorCode = ErrorCode.InternalError,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 移除右键菜单项
    /// </summary>
    public override async Task<RemoveContextMenuResponse> RemoveContextMenuItem(
        RemoveContextMenuRequest request,
        ServerCallContext context)
    {
        _logger.Debug("gRPC: RemoveContextMenuItem - ItemId: {ItemId}", request.ItemId);

        try
        {
            if (string.IsNullOrEmpty(request.ItemId))
            {
                return new RemoveContextMenuResponse
                {
                    Success = false,
                    ErrorCode = ErrorCode.InvalidArgument,
                    ErrorMessage = "菜单项 ID 不能为空"
                };
            }

            var targetType = ParseContextMenuType(request.TargetType);
            var result = await _contextMenuService.RemoveContextMenuItemAsync(request.ItemId, targetType, request.Extension);

            return new RemoveContextMenuResponse
            {
                Success = result,
                ErrorCode = result ? ErrorCode.Success : ErrorCode.NotFound,
                ErrorMessage = result ? null : "菜单项不存在"
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "RemoveContextMenuItem 失败");
            return new RemoveContextMenuResponse
            {
                Success = false,
                ErrorCode = ErrorCode.InternalError,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 获取打开方式列表
    /// </summary>
    public override async Task<OpenWithResponse> GetOpenWithList(
        OpenWithRequest request,
        ServerCallContext context)
    {
        _logger.Debug("gRPC: GetOpenWithList - Extension: {Extension}", request.Extension);

        try
        {
            if (string.IsNullOrEmpty(request.Extension))
            {
                return new OpenWithResponse
                {
                    ErrorCode = ErrorCode.InvalidArgument,
                    ErrorMessage = "扩展名不能为空"
                };
            }

            var entries = await _openWithService.GetOpenWithListAsync(request.Extension);

            var response = new OpenWithResponse
            {
                ErrorCode = ErrorCode.Success
            };

            foreach (var entry in entries)
            {
                response.Entries.Add(new OpenWithEntry
                {
                    ProgId = entry.ProgId,
                    Name = entry.Name ?? string.Empty,
                    Command = entry.Command ?? string.Empty,
                    Icon = entry.Icon ?? string.Empty,
                    IsUser = entry.PermissionLevel == PermissionLevel.User
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "GetOpenWithList 失败");
            return new OpenWithResponse
            {
                ErrorCode = ErrorCode.InternalError,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 添加打开方式
    /// </summary>
    public override async Task<AddOpenWithResponse> AddOpenWithEntry(
        AddOpenWithRequest request,
        ServerCallContext context)
    {
        _logger.Debug("gRPC: AddOpenWithEntry - ProgId: {ProgId}", request.Entry?.ProgId);

        try
        {
            if (request.Entry == null || string.IsNullOrEmpty(request.Entry.ProgId))
            {
                return new AddOpenWithResponse
                {
                    Success = false,
                    ErrorCode = ErrorCode.InvalidArgument,
                    ErrorMessage = "无效的打开方式参数"
                };
            }

            var entry = new OhRight.Core.Models.OpenWithEntry
            {
                ProgId = request.Entry.ProgId,
                Name = request.Entry.Name,
                Command = request.Entry.Command,
                Icon = request.Entry.Icon,
                PermissionLevel = request.Entry.IsUser ? PermissionLevel.User : PermissionLevel.Administrator
            };

            var result = await _openWithService.AddOpenWithEntryAsync(entry, request.Extension);

            return new AddOpenWithResponse
            {
                Success = result,
                ErrorCode = result ? ErrorCode.Success : ErrorCode.InternalError,
                ErrorMessage = result ? null : "添加失败"
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "AddOpenWithEntry 失败");
            return new AddOpenWithResponse
            {
                Success = false,
                ErrorCode = ErrorCode.InternalError,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 移除打开方式
    /// </summary>
    public override async Task<RemoveOpenWithResponse> RemoveOpenWithEntry(
        RemoveOpenWithRequest request,
        ServerCallContext context)
    {
        _logger.Debug("gRPC: RemoveOpenWithEntry - ProgId: {ProgId}", request.ProgId);

        try
        {
            if (string.IsNullOrEmpty(request.ProgId) || string.IsNullOrEmpty(request.Extension))
            {
                return new RemoveOpenWithResponse
                {
                    Success = false,
                    ErrorCode = ErrorCode.InvalidArgument,
                    ErrorMessage = "参数无效"
                };
            }

            var result = await _openWithService.RemoveOpenWithEntryAsync(request.ProgId, request.Extension);

            return new RemoveOpenWithResponse
            {
                Success = result,
                ErrorCode = result ? ErrorCode.Success : ErrorCode.NotFound,
                ErrorMessage = result ? null : "打开方式不存在"
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "RemoveOpenWithEntry 失败");
            return new RemoveOpenWithResponse
            {
                Success = false,
                ErrorCode = ErrorCode.InternalError,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 健康检查
    /// </summary>
    public override Task<HealthCheckResponse> HealthCheck(
        HealthCheckRequest request,
        ServerCallContext context)
    {
        _logger.Debug("gRPC: HealthCheck");

        return Task.FromResult(new HealthCheckResponse
        {
            Healthy = true,
            Version = _version,
            Status = "Running"
        });
    }

    #region 辅助方法

    private static Core.Enums.ContextMenuType ParseContextMenuType(string type)
    {
        return type?.ToLowerInvariant() switch
        {
            "file" => Core.Enums.ContextMenuType.File,
            "directory" => Core.Enums.ContextMenuType.Directory,
            "all" => Core.Enums.ContextMenuType.All,
            _ => Core.Enums.ContextMenuType.File
        };
    }

    private static OhRight.Grpc.ContextMenuType ConvertToGrpcContextMenuType(Core.Enums.ContextMenuType type)
    {
        return type switch
        {
            Core.Enums.ContextMenuType.File => OhRight.Grpc.ContextMenuType.File,
            Core.Enums.ContextMenuType.Directory => OhRight.Grpc.ContextMenuType.Directory,
            Core.Enums.ContextMenuType.All => OhRight.Grpc.ContextMenuType.All,
            Core.Enums.ContextMenuType.ShellEx => OhRight.Grpc.ContextMenuType.Shellx,
            _ => OhRight.Grpc.ContextMenuType.Unknown
        };
    }

    #endregion
}