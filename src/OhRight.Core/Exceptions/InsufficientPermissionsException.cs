using OhRight.Core.Enums;

namespace OhRight.Core.Exceptions;

/// <summary>
/// 权限不足异常
/// </summary>
public class InsufficientPermissionsException : RegistryException
{
    /// <summary>
    /// 所需权限级别
    /// </summary>
    public PermissionLevel RequiredPermission { get; }

    /// <summary>
    /// 当前权限级别
    /// </summary>
    public PermissionLevel CurrentPermission { get; }

    public InsufficientPermissionsException(string message, PermissionLevel requiredPermission, PermissionLevel currentPermission)
        : base(message, ErrorCode.PermissionDenied)
    {
        RequiredPermission = requiredPermission;
        CurrentPermission = currentPermission;
    }

    public InsufficientPermissionsException(string message, Exception innerException, PermissionLevel requiredPermission, PermissionLevel currentPermission)
        : base(message, innerException, ErrorCode.PermissionDenied)
    {
        RequiredPermission = requiredPermission;
        CurrentPermission = currentPermission;
    }

    public override string ToString()
    {
        var baseString = base.ToString();
        return $"{baseString}\nRequiredPermission: {RequiredPermission}\nCurrentPermission: {CurrentPermission}";
    }
}
