namespace OhRight.Core.Interfaces;

/// <summary>
/// UserChoice 哈希服务接口
/// </summary>
public interface IUserChoiceService
{
    /// <summary>
    /// 计算 UserChoice 哈希
    /// </summary>
    Task<string> CalculateHashAsync(string extension, string progId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证 UserChoice 哈希
    /// </summary>
    Task<bool> ValidateHashAsync(string extension, string progId, string? expectedHash = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入 UserChoice 信息到注册表
    /// </summary>
    Task WriteUserChoiceAsync(string extension, string progId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从注册表读取 UserChoice 信息
    /// </summary>
    Task<(string? ProgId, string? Hash)> ReadUserChoiceAsync(string extension, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查 UserChoice 保护状态
    /// </summary>
    Task<bool> IsUserChoiceProtectedAsync(string extension, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户 SID
    /// </summary>
    Task<string> GetCurrentUserSidAsync(CancellationToken cancellationToken = default);
}
