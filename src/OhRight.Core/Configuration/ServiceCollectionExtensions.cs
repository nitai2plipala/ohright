using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OhRight.Core.Interfaces;
using OhRight.Core.Logging;
using OhRight.Core.Services;

namespace OhRight.Core.Configuration;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 OhRight.Core 服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置操作</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddOhRightCore(this IServiceCollection services, Action<RegistryOptions>? configureOptions = null)
    {
        // 配置选项
        var options = new RegistryOptions();
        configureOptions?.Invoke(options);

        // 从配置文件加载（如果存在）
        var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
        if (configuration != null)
        {
            configuration.GetSection("Registry").Bind(options);
        }

        services.AddSingleton(options);

        // 添加日志服务
        services.AddSingleton<ILoggerService>(sp =>
        {
            var opts = sp.GetRequiredService<RegistryOptions>();
            return new SerilogService(opts);
        });

        // 添加核心服务
        services.AddSingleton<IRegistryService, RegistryService>();
        services.AddSingleton<IUserChoiceService, UserChoiceService>();
        services.AddSingleton<IFileAssociationService, FileAssociationService>();
        services.AddSingleton<IContextMenuService, ContextMenuService>();
        services.AddSingleton<IOpenWithService, OpenWithService>();

        return services;
    }

    /// <summary>
    /// 添加 OhRight.Core 服务（从配置文件）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddOhRightCore(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new RegistryOptions();
        configuration.GetSection("Registry").Bind(options);

        services.AddSingleton(options);
        services.AddSingleton<ILoggerService>(sp => new SerilogService(sp.GetRequiredService<RegistryOptions>()));
        services.AddSingleton<IRegistryService, RegistryService>();
        services.AddSingleton<IUserChoiceService, UserChoiceService>();
        services.AddSingleton<IFileAssociationService, FileAssociationService>();
        services.AddSingleton<IContextMenuService, ContextMenuService>();
        services.AddSingleton<IOpenWithService, OpenWithService>();

        return services;
    }
}
