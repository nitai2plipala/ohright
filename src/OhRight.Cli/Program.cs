using OhRight.Cli.Services;
using OhRight.Core.Logging;
using OhRight.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace OhRight.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // 配置依赖注入
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        var cliService = serviceProvider.GetRequiredService<ICliService>();
        var logger = serviceProvider.GetRequiredService<ILoggerService>();

        // 显示帮助信息
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            PrintHelp();
            return 0;
        }

        var command = args[0].ToLower();
        var jsonOutput = args.Contains("--json") || args.Contains("-j");

        try
        {
            switch (command)
            {
                case "get":
                    return await HandleGetCommand(cliService, args[1..], jsonOutput);
                case "set":
                    return await HandleSetCommand(cliService, args[1..], jsonOutput);
                case "list":
                    return await HandleListCommand(cliService, args[1..], jsonOutput);
                case "add":
                    return await HandleAddCommand(cliService, args[1..], jsonOutput);
                case "remove":
                    return await HandleRemoveCommand(cliService, args[1..], jsonOutput);
                default:
                    Console.WriteLine($"未知命令: {command}");
                    PrintHelp();
                    return 1;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "命令执行失败");
            Console.WriteLine($"错误: {ex.Message}");
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("OhRight CLI - Windows 文件关联与右键菜单管理工具");
        Console.WriteLine();
        Console.WriteLine("用法: ohright <命令> [参数] [选项]");
        Console.WriteLine();
        Console.WriteLine("命令:");
        Console.WriteLine("  get <扩展名>              获取文件关联信息");
        Console.WriteLine("  set <扩展名> <ProgId>     设置文件关联");
        Console.WriteLine("  list <类型>               列出右键菜单项");
        Console.WriteLine("  add <名称> <命令> <类型>  添加右键菜单项");
        Console.WriteLine("  remove <ID> <类型>        移除右键菜单项");
        Console.WriteLine();
        Console.WriteLine("类型:");
        Console.WriteLine("  file                      文件右键菜单");
        Console.WriteLine("  directory                 文件夹右键菜单");
        Console.WriteLine("  all                       通用右键菜单");
        Console.WriteLine();
        Console.WriteLine("选项:");
        Console.WriteLine("  --json, -j                以 JSON 格式输出");
        Console.WriteLine("  --force, -f               强制执行（仅用于 set 命令）");
        Console.WriteLine("  --extension, -e <扩展名>  指定扩展名（仅用于 list/add/remove）");
        Console.WriteLine("  --level, -l <级别>        权限级别：User 或 Administrator");
        Console.WriteLine();
        Console.WriteLine("示例:");
        Console.WriteLine("  ohright get .txt");
        Console.WriteLine("  ohright set .txt txtfile --force");
        Console.WriteLine("  ohright list file");
        Console.WriteLine("  ohright add \"打开记事本\" \"notepad.exe\" file --extension .txt");
        Console.WriteLine("  ohright remove {GUID} file");
    }

    private static async Task<int> HandleGetCommand(ICliService cliService, string[] args, bool json)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("错误: 请指定文件扩展名");
            return 1;
        }

        var extension = args[0];
        var level = OhRight.Core.Enums.PermissionLevel.User;

        // 解析 --level 参数
        var levelIndex = Array.IndexOf(args, "--level");
        if (levelIndex == -1) levelIndex = Array.IndexOf(args, "-l");
        if (levelIndex != -1 && levelIndex + 1 < args.Length)
        {
            if (Enum.TryParse<OhRight.Core.Enums.PermissionLevel>(args[levelIndex + 1], true, out var parsedLevel))
            {
                level = parsedLevel;
            }
        }

        var association = await cliService.GetFileAssociationAsync(extension, level);
        Formatters.OutputFormatter.PrintFileAssociation(association, json);
        return 0;
    }

    private static async Task<int> HandleSetCommand(ICliService cliService, string[] args, bool json)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("错误: 请指定文件扩展名和 ProgId");
            return 1;
        }

        var extension = args[0];
        var progId = args[1];
        var force = args.Contains("--force") || args.Contains("-f");

        var success = await cliService.SetFileAssociationAsync(extension, progId, force);
        if (success)
        {
            Formatters.OutputFormatter.PrintSuccess($"已设置文件关联: {extension} -> {progId}", json);
            return 0;
        }
        else
        {
            Formatters.OutputFormatter.PrintError("设置文件关联失败", json);
            return 1;
        }
    }

    private static async Task<int> HandleListCommand(ICliService cliService, string[] args, bool json)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("错误: 请指定目标类型（file、directory、all）");
            return 1;
        }

        var type = args[0];
        string? extension = null;

        // 解析 --extension 参数
        var extIndex = Array.IndexOf(args, "--extension");
        if (extIndex == -1) extIndex = Array.IndexOf(args, "-e");
        if (extIndex != -1 && extIndex + 1 < args.Length)
        {
            extension = args[extIndex + 1];
        }

        var items = await cliService.GetContextMenuItemsAsync(type, extension);
        Formatters.OutputFormatter.PrintContextMenuItems(items, json);
        return 0;
    }

    private static async Task<int> HandleAddCommand(ICliService cliService, string[] args, bool json)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("错误: 请指定名称、命令和类型");
            return 1;
        }

        var name = args[0];
        var command = args[1];
        var type = args[2];
        string? extension = null;
        string? icon = null;
        int position = 0;

        // 解析选项
        for (int i = 3; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--extension":
                case "-e":
                    if (i + 1 < args.Length) extension = args[++i];
                    break;
                case "--icon":
                case "-i":
                    if (i + 1 < args.Length) icon = args[++i];
                    break;
                case "--position":
                case "-p":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var pos))
                        position = pos;
                    break;
            }
        }

        var success = await cliService.AddContextMenuItemAsync(name, command, type, extension, icon, position);
        if (success)
        {
            Formatters.OutputFormatter.PrintSuccess($"已添加右键菜单项: {name}", json);
            return 0;
        }
        else
        {
            Formatters.OutputFormatter.PrintError("添加右键菜单项失败", json);
            return 1;
        }
    }

    private static async Task<int> HandleRemoveCommand(ICliService cliService, string[] args, bool json)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("错误: 请指定菜单项 ID 和类型");
            return 1;
        }

        var id = args[0];
        var type = args[1];
        string? extension = null;

        // 解析 --extension 参数
        var extIndex = Array.IndexOf(args, "--extension");
        if (extIndex == -1) extIndex = Array.IndexOf(args, "-e");
        if (extIndex != -1 && extIndex + 1 < args.Length)
        {
            extension = args[extIndex + 1];
        }

        var success = await cliService.RemoveContextMenuItemAsync(id, type, extension);
        if (success)
        {
            Formatters.OutputFormatter.PrintSuccess($"已移除右键菜单项: {id}", json);
            return 0;
        }
        else
        {
            Formatters.OutputFormatter.PrintError("移除右键菜单项失败（菜单项不存在）", json);
            return 1;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // 配置
        var options = new OhRight.Core.Configuration.RegistryOptions
        {
            EnableVerboseLogging = false
        };
        services.AddSingleton(options);

        // 日志服务
        services.AddSingleton<ILoggerService>(sp =>
        {
            return new SerilogService(options, "OhRight.Cli");
        });

        // Core 层服务
        services.AddSingleton<OhRight.Core.Interfaces.IRegistryService, RegistryService>();
        services.AddSingleton<OhRight.Core.Services.IFileAssociationService, FileAssociationService>();
        services.AddSingleton<OhRight.Core.Services.IContextMenuService, ContextMenuService>();
        services.AddSingleton<OhRight.Core.Services.IOpenWithService, OpenWithService>();
        services.AddSingleton<OhRight.Core.Interfaces.IUserChoiceService, UserChoiceService>();

        // CLI 服务
        services.AddSingleton<ICliService, CliService>();
    }
}