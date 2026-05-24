using OhRight.Core.Enums;

namespace OhRight.Core.Models;

/// <summary>
/// 右键菜单项模型
/// </summary>
public class ContextMenuItem
{
    /// <summary>
    /// 菜单项 ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 菜单项名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 执行命令
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// 图标路径
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 位置顺序
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// 菜单类型
    /// </summary>
    public ContextMenuType Type { get; set; } = ContextMenuType.File;

    /// <summary>
    /// 目标扩展名（仅特定后缀菜单使用）
    /// </summary>
    public string? TargetExtension { get; set; }

    /// <summary>
    /// 权限级别
    /// </summary>
    public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.User;

    /// <summary>
    /// 注册表路径
    /// </summary>
    public string RegistryPath { get; set; } = string.Empty;

    /// <summary>
    /// 子菜单路径（用于 shellex 类型）
    /// </summary>
    public string? SubMenuPath { get; set; }

    /// <summary>
    /// 参数模板
    /// </summary>
    public string? ArgumentTemplate { get; set; }

    /// <summary>
    /// 工作目录
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTime ModifiedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否为 COM 组件（shellex）
    /// </summary>
    public bool IsShellEx { get; set; }

    public override string ToString()
    {
        return $"Name: {Name}, Command: {Command}, Type: {Type}, Enabled: {Enabled}";
    }
}
