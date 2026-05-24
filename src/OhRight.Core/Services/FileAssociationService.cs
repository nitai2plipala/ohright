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
/// 文件关联管理服务
/// </summary>
public class FileAssociationService : IFileAssociationService, IAsyncDisposable
{
    private readonly IRegistryService _registryService;
    private readonly IUserChoiceService _userChoiceService;
    private readonly ILoggerService _logger;
    private readonly OhRight.Core.Configuration.RegistryOptions _options;
    private bool _disposed;

    public FileAssociationService(
        IRegistryService registryService,
        IUserChoiceService userChoiceService,
        OhRight.Core.Configuration.RegistryOptions options,
        ILoggerService? logger = null)
    {
        _registryService = registryService ?? throw new ArgumentNullException(nameof(registryService));
        _userChoiceService = userChoiceService ?? throw new ArgumentNullException(nameof(userChoiceService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? new SerilogService(_options);
    }

    /// <summary>
    /// 获取文件关联信息（包含 UserChoice 状态）
    /// </summary>
    public async Task<FileAssociation> GetFileAssociationAsync(string extension, PermissionLevel permissionLevel = PermissionLevel.User, CancellationToken cancellationToken = default)
    {
        _logger.Debug("获取文件关联信息（业务层）: {Extension}", extension);

        try
        {
            // 获取基础关联信息
            var association = await _registryService.GetFileAssociationAsync(extension, permissionLevel, cancellationToken);

            // 检查 UserChoice 保护状态
            try
            {
                var isProtected = await _userChoiceService.IsUserChoiceProtectedAsync(extension, cancellationToken);
                association.IsUserChoiceProtected = isProtected;
                association.IsDefault = isProtected; // 如果受保护，则认为是默认关联
            }
            catch (Exception ex)
            {
                _logger.Warning("检查 UserChoice 状态失败: {Extension}, Error: {Error}", extension, ex.Message);
                // 不抛出异常，只是标记为未保护
                association.IsUserChoiceProtected = false;
                association.IsDefault = false;
            }

            return association;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取文件关联信息失败: {Extension}", extension);
            throw;
        }
    }

    /// <summary>
    /// 设置文件关联（自动处理 UserChoice）
    /// </summary>
    public async Task<bool> SetFileAssociationAsync(FileAssociation association, bool force = false, bool updateUserChoice = true, CancellationToken cancellationToken = default)
    {
        _logger.Debug("设置文件关联（业务层）: {Extension} -> {ProgId}, Force={Force}, UpdateUserChoice={UpdateUserChoice}",
            association.Extension, association.ProgId, force, updateUserChoice);

        try
        {
            // 检查 UserChoice 保护
            if (!force && await _userChoiceService.IsUserChoiceProtectedAsync(association.Extension, cancellationToken))
            {
                _logger.Warning("文件关联受 UserChoice 保护，需要强制覆盖或引导用户设置: {Extension}", association.Extension);

                if (!force)
                {
                    throw new HashValidationFailedException(
                        $"扩展名 {association.Extension} 的关联受 Windows UserChoice 保护。请使用 force=true 强制覆盖，或引导用户通过系统设置选择。");
                }
            }

            // 设置基础关联
            var result = await _registryService.SetFileAssociationAsync(association, force, cancellationToken);

            if (result && updateUserChoice)
            {
                try
                {
                    // 更新 UserChoice（如果受保护）
                    if (await _userChoiceService.IsUserChoiceProtectedAsync(association.Extension, cancellationToken))
                    {
                        await _userChoiceService.WriteUserChoiceAsync(association.Extension, association.ProgId, cancellationToken);
                        _logger.Debug("已更新 UserChoice: {Extension} -> {ProgId}", association.Extension, association.ProgId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "更新 UserChoice 失败: {Extension}", association.Extension);
                    // 不抛出异常，基础关联已设置成功
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "设置文件关联失败: {Extension}", association.Extension);
            throw;
        }
    }

    /// <summary>
    /// 重置文件关联（恢复系统默认）
    /// </summary>
    public async Task<bool> ResetFileAssociationAsync(string extension, CancellationToken cancellationToken = default)
    {
        _logger.Debug("重置文件关联: {Extension}", extension);

        try
        {
            if (!extension.StartsWith('.'))
                extension = "." + extension;

            // 删除 UserChoice
            try
            {
                var userChoicePath = RegistryPaths.GetUserChoicePath(extension);
                Registry.CurrentUser.DeleteSubKeyTree(userChoicePath, false);
                _logger.Debug("已删除 UserChoice: {Extension}", extension);
            }
            catch (ArgumentException)
            {
                // UserChoice 不存在，忽略
            }

            // 删除扩展名关联
            try
            {
                Registry.ClassesRoot.DeleteSubKeyTree(extension, false);
                _logger.Debug("已删除扩展名关联: {Extension}", extension);
            }
            catch (ArgumentException)
            {
                // 关联不存在，忽略
            }

            _logger.Information("文件关联已重置: {Extension}", extension);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "重置文件关联失败: {Extension}", extension);
            throw new RegistryException($"重置文件关联失败：{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 检查关联是否受 UserChoice 保护
    /// </summary>
    public async Task<bool> IsProtectedByUserChoiceAsync(string extension, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _userChoiceService.IsUserChoiceProtectedAsync(extension, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "检查 UserChoice 保护状态失败: {Extension}", extension);
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Run(() =>
        {
            if (!_disposed)
            {
                _logger.Debug("释放 FileAssociationService 资源");
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
/// 文件关联管理服务接口（扩展）
/// </summary>
public interface IFileAssociationService : IDisposable, IAsyncDisposable
{
    Task<FileAssociation> GetFileAssociationAsync(string extension, PermissionLevel permissionLevel = PermissionLevel.User, CancellationToken cancellationToken = default);
    Task<bool> SetFileAssociationAsync(FileAssociation association, bool force = false, bool updateUserChoice = true, CancellationToken cancellationToken = default);
    Task<bool> ResetFileAssociationAsync(string extension, CancellationToken cancellationToken = default);
    Task<bool> IsProtectedByUserChoiceAsync(string extension, CancellationToken cancellationToken = default);
}
