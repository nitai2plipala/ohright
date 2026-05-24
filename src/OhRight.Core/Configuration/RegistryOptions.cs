using System.Text.Json;

namespace OhRight.Core.Configuration;

/// <summary>
/// 注册表操作配置选项
/// </summary>
public class RegistryOptions
{
    /// <summary>
    /// 是否使用管理员权限进行操作
    /// </summary>
    public bool RequireAdministrator { get; set; } = false;

    /// <summary>
    /// 超时时间（毫秒）
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 30000;

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// 是否启用详细日志
    /// </summary>
    public bool EnableVerboseLogging { get; set; } = false;

    /// <summary>
    /// 自定义注册表路径配置（可选）
    /// </summary>
    public RegistryPathOptions? CustomPaths { get; set; }
}

/// <summary>
/// 自定义注册表路径配置
/// </summary>
public class RegistryPathOptions
{
    public string? AllFilesContextMenu { get; set; }
    public string? DirectoryContextMenu { get; set; }
    public string? AllObjectsContextMenu { get; set; }
    public string? UserChoiceTemplate { get; set; }
    public string? OpenWithProgidsTemplate { get; set; }
    public string? OpenWithListTemplate { get; set; }
}
