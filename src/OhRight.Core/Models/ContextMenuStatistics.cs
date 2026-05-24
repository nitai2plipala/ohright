using System.Collections.ObjectModel;
using OhRight.Core.Enums;

namespace OhRight.Core.Models;

/// <summary>
/// 右键菜单统计信息
/// </summary>
public class ContextMenuStatistics
{
    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 启用数量
    /// </summary>
    public int EnabledCount { get; set; }

    /// <summary>
    /// 禁用数量
    /// </summary>
    public int DisabledCount { get; set; }

    /// <summary>
    /// 按类型分组
    /// </summary>
    public Dictionary<ContextMenuType, int> ByType { get; set; } = new();

    /// <summary>
    /// 按扩展名分组
    /// </summary>
    public Dictionary<string, int> ByExtension { get; set; } = new();

    /// <summary>
    /// 启用率
    /// </summary>
    public double EnabledRate => TotalCount > 0 ? (double)EnabledCount / TotalCount * 100 : 0;

    public override string ToString()
    {
        return $"总计: {TotalCount}, 启用: {EnabledCount}, 禁用: {DisabledCount}, 启用率: {EnabledRate:F1}%";
    }
}
