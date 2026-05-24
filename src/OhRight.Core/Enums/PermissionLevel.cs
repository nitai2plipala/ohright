namespace OhRight.Core.Enums;

/// <summary>
/// 权限级别枚举
/// </summary>
public enum PermissionLevel
{
    /// <summary>
    /// 用户级别（HKCU）
    /// </summary>
    User = 0,

    /// <summary>
    /// 系统级别（HKLM）
    /// </summary>
    System = 1,

    /// <summary>
    /// 管理员级别
    /// </summary>
    Administrator = 2
}
