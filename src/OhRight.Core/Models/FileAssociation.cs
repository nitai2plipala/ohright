using OhRight.Core.Enums;

namespace OhRight.Core.Models;

/// <summary>
/// 文件关联模型
/// </summary>
public class FileAssociation
{
    /// <summary>
    /// 文件扩展名（例如：.txt）
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// 程序标识符（ProgID）
    /// </summary>
    public string ProgId { get; set; } = string.Empty;

    /// <summary>
    /// 执行命令
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// 描述信息
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 图标路径
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// 文件类型名称
    /// </summary>
    public string FileTypeName { get; set; } = string.Empty;

    /// <summary>
    /// 默认图标索引
    /// </summary>
    public int IconIndex { get; set; }

    /// <summary>
    /// 是否为默认关联
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 权限级别
    /// </summary>
    public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.User;

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
    /// 是否受 UserChoice 保护
    /// </summary>
    public bool IsUserChoiceProtected { get; set; }

    public override string ToString()
    {
        return $"Extension: {Extension}, ProgId: {ProgId}, Command: {Command}, Description: {Description}";
    }
}
