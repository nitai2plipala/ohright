using System.Text.Json;
using System.Text.Json.Serialization;
using OhRight.Core.Enums;
using OhRight.Core.Models;

namespace OhRight.Cli.Formatters;

/// <summary>
/// 输出格式化服务
/// </summary>
public static class OutputFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// 输出文件关联信息
    /// </summary>
    public static void PrintFileAssociation(FileAssociation? association, bool jsonOutput)
    {
        if (association == null)
        {
            if (jsonOutput)
                Console.WriteLine("{}");
            else
                Console.WriteLine("未找到文件关联信息");
            return;
        }

        if (jsonOutput)
        {
            Console.WriteLine(JsonSerializer.Serialize(association, JsonOptions));
        }
        else
        {
            Console.WriteLine($"文件关联信息:");
            Console.WriteLine($"  扩展名: {association.Extension}");
            Console.WriteLine($"  ProgId: {association.ProgId}");
            Console.WriteLine($"  命令: {association.Command ?? "无"}");
            Console.WriteLine($"  描述: {association.Description ?? "无"}");
        }
    }

    /// <summary>
    /// 输出右键菜单项列表
    /// </summary>
    public static void PrintContextMenuItems(IReadOnlyList<ContextMenuItem> items, bool jsonOutput)
    {
        if (items.Count == 0)
        {
            if (jsonOutput)
                Console.WriteLine("[]");
            else
                Console.WriteLine("没有找到右键菜单项");
            return;
        }

        if (jsonOutput)
        {
            Console.WriteLine(JsonSerializer.Serialize(items, JsonOptions));
        }
        else
        {
            Console.WriteLine($"右键菜单项 ({items.Count} 个):");
            Console.WriteLine(new string('-', 60));
            Console.WriteLine($"{"ID",-12} {"名称",-20} {"启用",-6} {"位置",-6}");
            Console.WriteLine(new string('-', 60));

            foreach (var item in items)
            {
                var enabled = item.Enabled ? "✓" : "✗";
                Console.WriteLine($"{item.Id,-12} {item.Name,-20} {enabled,-6} {item.Position,-6}");
                if (!string.IsNullOrEmpty(item.Command))
                    Console.WriteLine($"             命令: {item.Command}");
            }
        }
    }

    /// <summary>
    /// 输出打开方式列表
    /// </summary>
    public static void PrintOpenWithList(IReadOnlyList<OpenWithEntry> entries, bool jsonOutput)
    {
        if (entries.Count == 0)
        {
            if (jsonOutput)
                Console.WriteLine("[]");
            else
                Console.WriteLine("没有找到打开方式");
            return;
        }

        if (jsonOutput)
        {
            Console.WriteLine(JsonSerializer.Serialize(entries, JsonOptions));
        }
        else
        {
            Console.WriteLine($"打开方式列表 ({entries.Count} 个):");
            Console.WriteLine(new string('-', 70));
            Console.WriteLine($"{"ProgId",-25} {"名称",-20} {"级别",-10}");
            Console.WriteLine(new string('-', 70));

            foreach (var entry in entries)
            {
                var level = entry.PermissionLevel == PermissionLevel.User ? "用户" : "系统";
                Console.WriteLine($"{entry.ProgId,-25} {entry.Name ?? "无",-20} {level,-10}");
                if (!string.IsNullOrEmpty(entry.Command))
                    Console.WriteLine($"             命令: {entry.Command}");
            }
        }
    }

    /// <summary>
    /// 输出成功消息
    /// </summary>
    public static void PrintSuccess(string message, bool jsonOutput)
    {
        if (jsonOutput)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { success = true, message }, JsonOptions));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ {message}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// 输出错误消息
    /// </summary>
    public static void PrintError(string message, bool jsonOutput)
    {
        if (jsonOutput)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { success = false, error = message }, JsonOptions));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ {message}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// 输出警告消息
    /// </summary>
    public static void PrintWarning(string message, bool jsonOutput)
    {
        if (jsonOutput)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { success = false, warning = message }, JsonOptions));
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠ {message}");
            Console.ResetColor();
        }
    }
}