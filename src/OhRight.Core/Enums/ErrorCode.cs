namespace OhRight.Core.Enums;

/// <summary>
/// 错误码枚举
/// </summary>
public enum ErrorCode
{
    /// <summary>
    /// 无错误
    /// </summary>
    None = 0,

    /// <summary>
    /// 权限不足
    /// </summary>
    PermissionDenied = 1,

    /// <summary>
    /// 注册表键不存在
    /// </summary>
    NotFound = 2,

    /// <summary>
    /// 哈希验证失败
    /// </summary>
    HashValidationFailed = 3,

    /// <summary>
    /// 无效参数
    /// </summary>
    InvalidParameter = 4,

    /// <summary>
    /// 操作超时
    /// </summary>
    Timeout = 5,

    /// <summary>
    /// 操作被取消
    /// </summary>
    Cancelled = 6,

    /// <summary>
    /// 未知错误
    /// </summary>
    Unknown = 99
}
