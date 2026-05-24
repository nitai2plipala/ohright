namespace OhRight.Core.Constants;

/// <summary>
/// 注册表路径常量
/// </summary>
public static class RegistryPaths
{
    /// <summary>
    /// HKEY_CLASSES_ROOT 根路径
    /// </summary>
    public const string HKCR = @"HKEY_CLASSES_ROOT";

    /// <summary>
    /// HKEY_CURRENT_USER 根路径
    /// </summary>
    public const string HKCU = @"HKEY_CURRENT_USER";

    /// <summary>
    /// HKEY_LOCAL_MACHINE 根路径
    /// </summary>
    public const string HKLM = @"HKEY_LOCAL_MACHINE";

    /// <summary>
    /// 通用文件右键菜单路径
    /// </summary>
    public const string AllFilesContextMenu = @"*\shell";

    /// <summary>
    /// 文件夹右键菜单路径
    /// </summary>
    public const string DirectoryContextMenu = @"Directory\shell";

    /// <summary>
    /// 所有文件和文件夹右键菜单路径
    /// </summary>
    public const string AllObjectsContextMenu = @"AllFilesystemObjects\shell";

    /// <summary>
    /// Shell 路径
    /// </summary>
    public const string ShellPath = @"shell";

    /// <summary>
    /// Command 路径
    /// </summary>
    public const string CommandPath = @"command";

    /// <summary>
    /// DefaultIcon 路径
    /// </summary>
    public const string DefaultIconPath = @"DefaultIcon";

    /// <summary>
    /// shellex 路径
    /// </summary>
    public const string ShellExPath = @"shellex";

    /// <summary>
    /// ContextMenuHandlers 路径
    /// </summary>
    public const string ContextMenuHandlersPath = @"shellex\ContextMenuHandlers";

    /// <summary>
    /// 禁用菜单项前缀
    /// </summary>
    public const string DisabledMenuPrefix = "-";

    /// <summary>
    /// UserChoice 路径模板
    /// </summary>
    public const string UserChoiceTemplate = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{0}\UserChoice";

    /// <summary>
    /// OpenWithProgids 路径模板
    /// </summary>
    public const string OpenWithProgidsTemplate = @"{0}\OpenWithProgids";

    /// <summary>
    /// OpenWithList 路径模板
    /// </summary>
    public const string OpenWithListTemplate = @"{0}\OpenWithList";

    /// <summary>
    /// MRUList 路径模板
    /// </summary>
    public const string MRUListTemplate = @"{0}\OpenWithList\MRUList";

    /// <summary>
    /// FileExts 路径
    /// </summary>
    public const string FileExtsPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts";

    /// <summary>
    /// ProgIDs 路径
    /// </summary>
    public const string ProgIDsPath = @"SOFTWARE\Classes";

    /// <summary>
    /// 获取文件扩展名的完整注册表路径
    /// </summary>
    public static string GetFileExtensionPath(string extension)
    {
        if (!extension.StartsWith('.'))
            extension = "." + extension;
        return $@"{HKCR}\{extension}";
    }

    /// <summary>
    /// 获取 OpenWithProgids 的完整路径
    /// </summary>
    public static string GetOpenWithProgidsPath(string extension)
    {
        if (!extension.StartsWith('.'))
            extension = "." + extension;
        return string.Format(OpenWithProgidsTemplate, extension);
    }

    /// <summary>
    /// 获取 OpenWithList 的完整路径
    /// </summary>
    public static string GetOpenWithListPath(string extension)
    {
        if (!extension.StartsWith('.'))
            extension = "." + extension;
        return string.Format(OpenWithListTemplate, extension);
    }

    /// <summary>
    /// 获取 UserChoice 的完整路径
    /// </summary>
    public static string GetUserChoicePath(string extension)
    {
        if (!extension.StartsWith('.'))
            extension = "." + extension;
        return string.Format(UserChoiceTemplate, extension);
    }
}
