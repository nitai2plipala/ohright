namespace OhRight.Core.Enums;

/// <summary>
/// 右键菜单类型枚举
/// </summary>
public enum ContextMenuType
{
    /// <summary>
    /// 未知类型
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 文件右键菜单
    /// </summary>
    File = 1,

    /// <summary>
    /// 文件夹右键菜单
    /// </summary>
    Directory = 2,

    /// <summary>
    /// 所有文件和文件夹
    /// </summary>
    All = 3,

    /// <summary>
    /// Shellex 类型（COM 组件）
    /// </summary>
    ShellEx = 4
}
