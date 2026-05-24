namespace OhRight.Core.Exceptions;

/// <summary>
/// 注册表键不存在异常
/// </summary>
public class RegistryKeyNotFoundException : RegistryException
{
    /// <summary>
    /// 键路径
    /// </summary>
    public new string? RegistryPath => base.RegistryPath;

    public RegistryKeyNotFoundException(string keyPath, string message)
        : base(message, OhRight.Core.Enums.ErrorCode.NotFound, keyPath)
    {
    }

    public RegistryKeyNotFoundException(string keyPath, string message, Exception innerException)
        : base(message, innerException, OhRight.Core.Enums.ErrorCode.NotFound, keyPath)
    {
    }

    public override string ToString()
    {
        var baseString = base.ToString();
        return $"{baseString}\nKeyPath: {RegistryPath ?? "N/A"}";
    }
}
