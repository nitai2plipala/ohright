using OhRight.Core.Enums;

namespace OhRight.Core.Models;

/// <summary>
/// 打开方式条目模型
/// </summary>
public class OpenWithEntry
{
    /// <summary>
    /// 程序标识符（ProgID）
    /// </summary>
    public string ProgId { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
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
    /// 类型
    /// </summary>
    public OpenWithType Type { get; set; } = OpenWithType.ProgId;

    /// <summary>
    /// 关联的扩展名
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// 注册表路径
    /// </summary>
    public string RegistryPath { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTime ModifiedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否为默认打开方式
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 权限级别
    /// </summary>
    public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.User;

    /// <summary>
    /// MRU 顺序键（a-z）
    /// </summary>
    public string? MruKey { get; set; }

    public override string ToString()
    {
        return $"ProgId: {ProgId}, Name: {Name}, Command: {Command}, Type: {Type}";
    }
}
