namespace OhRight.Core.Exceptions;

/// <summary>
/// 哈希验证失败异常
/// </summary>
public class HashValidationFailedException : RegistryException
{
    /// <summary>
    /// 预期的哈希值
    /// </summary>
    public string? ExpectedHash { get; }

    /// <summary>
    /// 实际的哈希值
    /// </summary>
    public string? ActualHash { get; }

    public HashValidationFailedException(string message)
        : base(message, OhRight.Core.Enums.ErrorCode.HashValidationFailed)
    {
    }

    public HashValidationFailedException(string message, string expectedHash, string actualHash)
        : base(message, OhRight.Core.Enums.ErrorCode.HashValidationFailed)
    {
        ExpectedHash = expectedHash;
        ActualHash = actualHash;
    }

    public HashValidationFailedException(string message, Exception innerException)
        : base(message, innerException, OhRight.Core.Enums.ErrorCode.HashValidationFailed)
    {
    }

    public HashValidationFailedException(string message, Exception innerException, string expectedHash, string actualHash)
        : base(message, innerException, OhRight.Core.Enums.ErrorCode.HashValidationFailed)
    {
        ExpectedHash = expectedHash;
        ActualHash = actualHash;
    }

    public override string ToString()
    {
        var baseString = base.ToString();
        var hashInfo = "";
        if (ExpectedHash != null && ActualHash != null)
        {
            hashInfo = $"\nExpectedHash: {ExpectedHash}\nActualHash: {ActualHash}";
        }
        return $"{baseString}{hashInfo}";
    }
}
