using Microsoft.Extensions.Hosting;
using OhRight.Core.Logging;

namespace OhRight.Grpc.Server.Services;

/// <summary>
/// gRPC 服务器生命周期管理服务
/// </summary>
public class GrpcServerLifecycleService : IHostedService, IDisposable
{
    private readonly ILoggerService _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ServerConfiguration _config;
    private bool _isRunning;

    public GrpcServerLifecycleService(
        ILoggerService logger,
        IHostApplicationLifetime lifetime,
        ServerConfiguration config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// 启动服务
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.Information("gRPC 服务器生命周期服务启动");
        _isRunning = true;

        // 注册关闭事件
        _lifetime.ApplicationStopping.Register(OnApplicationStopping);
        _lifetime.ApplicationStopped.Register(OnApplicationStopped);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止服务
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Information("gRPC 服务器生命周期服务停止");
        _isRunning = false;

        // 执行清理操作
        Cleanup();

        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取服务器状态
    /// </summary>
    public ServerStatus GetStatus()
    {
        return new ServerStatus
        {
            IsRunning = _isRunning,
            HttpPort = _config.HttpPort,
            GrpcPort = _config.GrpcPort,
            StartTime = _startTime,
            Uptime = DateTime.UtcNow - _startTime
        };
    }

    private DateTime _startTime = DateTime.UtcNow;

    private void OnApplicationStopping()
    {
        _logger.Information("应用程序正在停止...");
    }

    private void OnApplicationStopped()
    {
        _logger.Information("应用程序已停止");
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    private void Cleanup()
    {
        try
        {
            _logger.Debug("执行资源清理...");
            // 清理逻辑可以在这里添加
            // 例如：关闭数据库连接、释放缓存等
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "清理资源时发生错误");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // 释放托管资源
        }
    }
}

/// <summary>
/// 服务器状态信息
/// </summary>
public class ServerStatus
{
    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// HTTP 端口
    /// </summary>
    public int HttpPort { get; set; }

    /// <summary>
    /// gRPC 端口
    /// </summary>
    public int GrpcPort { get; set; }

    /// <summary>
    /// 启动时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 运行时间
    /// </summary>
    public TimeSpan Uptime { get; set; }
}