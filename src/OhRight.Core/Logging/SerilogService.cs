using Microsoft.Extensions.Configuration;
using OhRight.Core.Configuration;
using Serilog;
using Serilog.Sinks.File;
using RollingInterval = Serilog.RollingInterval;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace OhRight.Core.Logging;

/// <summary>
/// 日志服务接口
/// </summary>
public interface ILoggerService
{
    /// <summary>
    /// 记录调试日志
    /// </summary>
    void Debug(string message, params object[] args);

    /// <summary>
    /// 记录信息日志
    /// </summary>
    void Information(string message, params object[] args);

    /// <summary>
    /// 记录警告日志
    /// </summary>
    void Warning(string message, params object[] args);

    /// <summary>
    /// 记录错误日志
    /// </summary>
    void Error(string message, params object[] args);

    /// <summary>
    /// 记录异常
    /// </summary>
    void Error(Exception ex, string message, params object[] args);
}

/// <summary>
/// Serilog 日志服务实现
/// </summary>
public class SerilogService : ILoggerService, IDisposable
{
    private readonly Serilog.ILogger _logger;
    private bool _disposed;

    public SerilogService(RegistryOptions options, string applicationName = "OhRight.Core")
    {
        var loggerConfiguration = new Serilog.LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", applicationName)
            .Enrich.WithProcessId()
            .Enrich.WithThreadId();

        // 控制台输出
        loggerConfiguration = loggerConfiguration.WriteTo.Console(
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

        // 文件输出（如果启用）
        if (options.EnableVerboseLogging)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logPath);
            loggerConfiguration = loggerConfiguration.WriteTo.File(
                Path.Combine(logPath, "ohright-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        }

        _logger = loggerConfiguration.CreateLogger();
    }

    /// <summary>
    /// 从 ILogger 创建 SerilogService
    /// </summary>
    public static SerilogService FromMicrosoftLogger(ILogger logger)
    {
        var options = new RegistryOptions();
        var service = new SerilogService(options);
        return service;
    }

    public void Debug(string message, params object[] args)
    {
        _logger.Debug(message, args);
    }

    public void Information(string message, params object[] args)
    {
        _logger.Information(message, args);
    }

    public void Warning(string message, params object[] args)
    {
        _logger.Warning(message, args);
    }

    public void Error(string message, params object[] args)
    {
        _logger.Error(message, args);
    }

    public void Error(Exception ex, string message, params object[] args)
    {
        _logger.Error(ex, message, args);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Serilog.Log.CloseAndFlush();
            _disposed = true;
        }
    }
}
