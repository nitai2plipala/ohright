using Microsoft.OpenApi.Models;
using OhRight.Core.Configuration;
using OhRight.Core.Interfaces;
using OhRight.Core.Logging;
using OhRight.Core.Services;
using OhRight.Grpc.Server.Services;
using System.Reflection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// 配置
var serverConfig = new ServerConfiguration();
builder.Configuration.GetSection("Server").Bind(serverConfig);

// 日志服务
builder.Services.AddSingleton<ILoggerService>(sp =>
{
    var options = new RegistryOptions { EnableVerboseLogging = builder.Environment.IsDevelopment() };
    return new SerilogService(options, "OhRight.Grpc.Server");
});

// 注册 Core 层服务
builder.Services.AddSingleton<IRegistryService, RegistryService>();
builder.Services.AddSingleton<IFileAssociationService, FileAssociationService>();
builder.Services.AddSingleton<IContextMenuService, ContextMenuService>();
builder.Services.AddSingleton<IOpenWithService, OpenWithService>();
builder.Services.AddSingleton<IUserChoiceService, UserChoiceService>();

// 注册 gRPC 服务
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4MB
    options.MaxSendMessageSize = 4 * 1024 * 1024; // 4MB
});

// 生命周期管理服务
builder.Services.AddSingleton(serverConfig);
builder.Services.AddHostedService<GrpcServerLifecycleService>();

// 健康检查
builder.Services.AddHealthChecks();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OhRight gRPC Server API",
        Version = "v1",
        Description = "Windows 文件关联与右键菜单管理工具 gRPC 服务",
        Contact = new OpenApiContact
        {
            Name = "OhRight Team",
            Email = "support@ohright.dev"
        }
    });
    
    // 包含 XML 注释
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// 配置 Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP 端点（用于健康检查和 Swagger）
    options.ListenAnyIP(serverConfig.HttpPort);

    // gRPC 端点 (h2c 明文 HTTP/2)
    options.ListenAnyIP(serverConfig.GrpcPort, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

var app = builder.Build();

// 配置 HTTP 管道
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Swagger/OpenAPI 中间件
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "OhRight gRPC Server API v1");
    options.RoutePrefix = string.Empty; // 根路径显示 Swagger UI
});

// 健康检查端点
app.MapHealthChecks("/health");

// gRPC 服务
app.MapGrpcService<RegistryGrpcService>();

// 根路径返回服务信息（重定向到 Swagger UI）
app.MapGet("/", () => Results.Redirect("/swagger"));

// 服务器状态端点
app.MapGet("/status", (GrpcServerLifecycleService lifecycleService) =>
{
    var status = lifecycleService.GetStatus();
    return Results.Ok(new
    {
        Service = "OhRight gRPC Server",
        Version = "1.0.0",
        Status = status.IsRunning ? "Running" : "Stopped",
        Ports = new
        {
            Http = status.HttpPort,
            Grpc = status.GrpcPort
        },
        StartTime = status.StartTime,
        Uptime = status.Uptime.ToString(@"hh\:mm\:ss"),
        Endpoints = new
        {
            Grpc = $"http://localhost:{status.GrpcPort}",
            Health = "/health",
            Status = "/status",
            Swagger = "/swagger"
        }
    });
});

// 启动日志
var logger = app.Services.GetRequiredService<ILoggerService>();
logger.Information("OhRight gRPC Server 启动完成");
logger.Information("HTTP 端点: http://localhost:{Port}", serverConfig.HttpPort);
logger.Information("gRPC 端点: http://localhost:{Port} (h2c 明文 HTTP/2)", serverConfig.GrpcPort);

app.Run();

// 配置类
public class ServerConfiguration
{
    public int HttpPort { get; set; } = 5000;
    public int GrpcPort { get; set; } = 5001;
    public int TimeoutSeconds { get; set; } = 30;
}