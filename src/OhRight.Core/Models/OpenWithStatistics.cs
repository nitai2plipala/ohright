using System.Collections.ObjectModel;
using OhRight.Core.Enums;

namespace OhRight.Core.Models;

/// <summary>
/// 打开方式统计信息
/// </summary>
public class OpenWithStatistics
{
    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// ProgId 类型数量
    /// </summary>
    public int ProgIdCount { get; set; }

    /// <summary>
    /// List 类型数量
    /// </summary>
    public int ListCount { get; set; }

    /// <summary>
    /// 用户级数量
    /// </summary>
    public int UserLevelCount { get; set; }

    /// <summary>
    /// 系统级数量
    /// </summary>
    public int SystemLevelCount { get; set; }

    /// <summary>
    /// 按类型分组
    /// </summary>
    public Dictionary<OpenWithType, int> ByType { get; set; } = new();

    /// <summary>
    /// 按扩展名分组
    /// </summary>
    public Dictionary<string, int> ByExtension { get; set; } = new();

    /// <summary>
    /// 有效数量（命令非空）
    /// </summary>
    public int ValidCount { get; set; }

    /// <summary>
    /// 有效率
    /// </summary>
    public double ValidRate => TotalCount > 0 ? (double)ValidCount / TotalCount * 100 : 0;

    public override string ToString()
    {
        return $"总计: {TotalCount}, 有效: {ValidCount}, 有效率: {ValidRate:F1}%";
    }
}
