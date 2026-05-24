using OhRight.Core.Enums;

namespace OhRight.Core.Exceptions;

/// <summary>
/// 注册表操作基础异常
/// </summary>
public class RegistryException : Exception
{
    /// <summary>
    /// 错误码
    /// </summary>
    public ErrorCode ErrorCode { get; }

    /// <summary>
    /// 注册表路径
    /// </summary>
    public string? RegistryPath { get; }

    public RegistryException(string message, ErrorCode errorCode = ErrorCode.Unknown, string? registryPath = null)
        : base(message)
    {
        ErrorCode = errorCode;
        RegistryPath = registryPath;
    }

    public RegistryException(string message, Exception innerException, ErrorCode errorCode = ErrorCode.Unknown, string? registryPath = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        RegistryPath = registryPath;
    }

    public override string ToString()
    {
        var baseString = base.ToString();
        return $"{baseString}\nErrorCode: {ErrorCode}\nRegistryPath: {RegistryPath ?? "N/A"}";
    }
}
