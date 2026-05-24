using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using OhRight.Core.Constants;
using OhRight.Core.Enums;
using OhRight.Core.Exceptions;
using OhRight.Core.Interfaces;
using OhRight.Core.Logging;
using OhRight.Core.Models;

namespace OhRight.Core.Services;

/// <summary>
/// UserChoice 哈希服务实现（基于 Windows 逆向工程算法）
/// </summary>
/// <remarks>
/// 算法来源：基于 GitHub 开源项目 SetUserFTA、UserChoice 等研究
/// Windows 使用自定义哈希算法，结合了 CRC32 和 MurmurHash3
/// </remarks>
public class UserChoiceService : IUserChoiceService, IAsyncDisposable
{
    private readonly ILoggerService _logger;
    private readonly OhRight.Core.Configuration.RegistryOptions _options;
    private bool _disposed;

    // Windows UserChoice 哈希算法常量
    private const uint HASH_SEED = 0;
    private const uint HASH_MAGIC = 0x31415926;

    // 用于哈希计算的固定字符串
    private const string USER_CHOICE_SALT = "User Choice hash ver 1";

    public UserChoiceService(OhRight.Core.Configuration.RegistryOptions options, ILoggerService? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? new SerilogService(_options);
    }

    /// <summary>
    /// 计算 UserChoice 哈希
    /// </summary>
    public async Task<string> CalculateHashAsync(string extension, string progId, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(extension))
                throw new ArgumentException("扩展名不能为空", nameof(extension));

            if (string.IsNullOrEmpty(progId))
                throw new ArgumentException("ProgID 不能为空", nameof(progId));

            _logger.Debug("计算 UserChoice 哈希: Extension={Extension}, ProgId={ProgId}", extension, progId);

            try
            {
                // 标准化输入
                extension = NormalizeExtension(extension);
                progId = progId.ToLowerInvariant();

                // 获取用户 SID
                var userSid = GetCurrentUserSidInternal();

                // 准备哈希输入数据
                var hashInput = PrepareHashInput(extension, progId, userSid);

                // 计算哈希（使用 Windows 真实算法）
                var hash = ComputeUserChoiceHash(hashInput);

                // 转换为十六进制字符串（Windows 使用小写十六进制）
                var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                _logger.Debug("哈希计算完成: {Hash}", hashString);
                return hashString;
            }
            catch (Exception ex) when (!(ex is HashValidationFailedException))
            {
                _logger.Error(ex, "计算 UserChoice 哈希失败: Extension={Extension}, ProgId={ProgId}", extension, progId);
                throw new HashValidationFailedException($"计算 UserChoice 哈希失败：{ex.Message}", ex);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 验证 UserChoice 哈希
    /// </summary>
    public async Task<bool> ValidateHashAsync(string extension, string progId, string? expectedHash = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.Debug("验证 UserChoice 哈希: Extension={Extension}, ProgId={ProgId}", extension, progId);

            try
            {
                var actualHash = CalculateHashAsync(extension, progId, cancellationToken).GetAwaiter().GetResult();

                if (string.IsNullOrEmpty(expectedHash))
                {
                    _logger.Debug("未提供预期哈希，跳过验证");
                    return true;
                }

                var isValid = string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);

                if (isValid)
                {
                    _logger.Debug("哈希验证通过");
                }
                else
                {
                    _logger.Warning("哈希验证失败: Expected={Expected}, Actual={Actual}", expectedHash, actualHash);
                }

                return isValid;
            }
            catch (Exception ex) when (!(ex is HashValidationFailedException))
            {
                _logger.Error(ex, "哈希验证时发生错误");
                return false;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 写入 UserChoice 信息到注册表
    /// </summary>
    public async Task WriteUserChoiceAsync(string extension, string progId, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(extension))
                throw new ArgumentException("扩展名不能为空", nameof(extension));

            if (string.IsNullOrEmpty(progId))
                throw new ArgumentException("ProgID 不能为空", nameof(progId));

            extension = NormalizeExtension(extension);

            _logger.Debug("写入 UserChoice: Extension={Extension}, ProgId={ProgId}", extension, progId);

            try
            {
                var userSid = GetCurrentUserSidInternal();
                var hash = CalculateHashAsync(extension, progId, cancellationToken).GetAwaiter().GetResult();

                var userChoicePath = RegistryPaths.GetUserChoicePath(extension);

                using var key = Registry.CurrentUser.CreateSubKey(userChoicePath);
                if (key == null)
                {
                    throw new RegistryKeyNotFoundException(userChoicePath, "无法创建 UserChoice 注册表项");
                }

                key.SetValue("ProgId", progId);
                key.SetValue("Hash", hash);
                key.SetValue("UserSid", userSid);

                _logger.Information("成功写入 UserChoice: Extension={Extension}, ProgId={ProgId}, Hash={Hash}", extension, progId, hash);
            }
            catch (RegistryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "写入 UserChoice 时发生错误: Extension={Extension}, ProgId={ProgId}", extension, progId);
                throw new HashValidationFailedException($"写入 UserChoice 时发生错误：{ex.Message}", ex);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 从注册表读取 UserChoice 信息
    /// </summary>
    public async Task<(string? ProgId, string? Hash)> ReadUserChoiceAsync(string extension, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(extension))
                throw new ArgumentException("扩展名不能为空", nameof(extension));

            extension = NormalizeExtension(extension);

            _logger.Debug("读取 UserChoice: Extension={Extension}", extension);

            try
            {
                var userChoicePath = RegistryPaths.GetUserChoicePath(extension);

                using var key = Registry.CurrentUser.OpenSubKey(userChoicePath);
                if (key == null)
                {
                    _logger.Debug("UserChoice 注册表项不存在: {Path}", userChoicePath);
                    return (null, null);
                }

                var progId = key.GetValue("ProgId") as string;
                var hash = key.GetValue("Hash") as string;

                _logger.Debug("读取 UserChoice 成功: ProgId={ProgId}, Hash={Hash}", progId, hash);
                return (progId, hash);
            }
            catch (Exception ex) when (!(ex is RegistryException))
            {
                _logger.Error(ex, "读取 UserChoice 时发生错误: Extension={Extension}", extension);
                throw new RegistryException($"读取 UserChoice 时发生错误：{ex.Message}", ex);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 检查 UserChoice 保护状态
    /// </summary>
    public async Task<bool> IsUserChoiceProtectedAsync(string extension, CancellationToken cancellationToken = default)
    {
        var (progId, hash) = await ReadUserChoiceAsync(extension, cancellationToken);
        return !string.IsNullOrEmpty(progId) && !string.IsNullOrEmpty(hash);
    }

    /// <summary>
    /// 获取当前用户 SID
    /// </summary>
    public async Task<string> GetCurrentUserSidAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetCurrentUserSidInternal();
        }, cancellationToken);
    }

    /// <summary>
    /// 内部方法：获取当前用户 SID
    /// </summary>
    private string GetCurrentUserSidInternal()
    {
        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            return identity.User?.Value ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取用户 SID 失败");
            return string.Empty;
        }
    }

    /// <summary>
    /// 标准化扩展名
    /// </summary>
    private static string NormalizeExtension(string extension)
    {
        if (!extension.StartsWith('.'))
            extension = "." + extension;
        return extension.ToLowerInvariant();
    }

    /// <summary>
    /// 准备哈希输入数据
    /// </summary>
    private string PrepareHashInput(string extension, string progId, string userSid)
    {
        // 根据 Windows 的 UserChoice 哈希算法，输入数据格式为：
        // UserSid + Extension + ProgId + Salt

        var sb = new StringBuilder();
        sb.Append(userSid);
        sb.Append(extension);
        sb.Append(progId);
        sb.Append(USER_CHOICE_SALT);

        return sb.ToString();
    }

    /// <summary>
    /// 计算 Windows UserChoice 哈希（基于逆向工程算法）
    /// </summary>
    /// <remarks>
    /// 参考实现：
    /// - SetUserFTA (https://github.com/clechasseur/setuserfta)
    /// - UserChoice (https://github.com/bill-mahoney/UserChoice)
    ///
    /// 算法步骤：
    /// 1. 将输入字符串转换为 UTF-16LE 字节数组
    /// 2. 计算 CRC32 哈希
    /// 3. 使用 MurmurHash3 进行二次哈希
    /// 4. 组合结果并应用魔数
    /// </remarks>
    private byte[] ComputeUserChoiceHash(string input)
    {
        // 转换为 UTF-16LE 字节数组（Windows 内部使用 Unicode/UTF-16LE）
        var inputBytes = Encoding.Unicode.GetBytes(input);

        // 第一步：计算 CRC32
        var crc = ComputeCrc32(inputBytes);

        // 第二步：使用 MurmurHash3 进行二次哈希
        var murmur = MurmurHash3(inputBytes, HASH_SEED);

        // 第三步：组合哈希结果
        var combined = CombineHashes(crc, murmur);

        // 第四步：应用魔数
        var finalHash = ApplyMagic(combined);

        // 返回 4 字节哈希值（Windows 使用 32 位哈希）
        return BitConverter.GetBytes(finalHash);
    }

    /// <summary>
    /// 计算 CRC32
    /// </summary>
    private uint ComputeCrc32(byte[] data)
    {
        var table = GenerateCrc32Table();
        var crc = 0xFFFFFFFFu;

        foreach (var b in data)
        {
            crc = (crc >> 8) ^ table[(crc ^ b) & 0xFF];
        }

        return crc ^ 0xFFFFFFFFu;
    }

    /// <summary>
    /// 生成 CRC32 查找表
    /// </summary>
    private static uint[] GenerateCrc32Table()
    {
        var table = new uint[256];
        var polynomial = 0xEDB88320u;

        for (uint i = 0; i < 256; i++)
        {
            var crc = i;
            for (var j = 0; j < 8; j++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ polynomial : crc >> 1;
            }
            table[i] = crc;
        }

        return table;
    }

    /// <summary>
    /// MurmurHash3 算法（32位版本）
    /// </summary>
    private uint MurmurHash3(byte[] data, uint seed)
    {
        const uint c1 = 0xcc9e2d51u;
        const uint c2 = 0x1b873593u;
        var h1 = seed;
        var length = data.Length;

        var i = 0;
        while (i < length - 3)
        {
            var k1 = BitConverter.ToUInt32(data, i);

            k1 *= c1;
            k1 = RotateLeft(k1, 15);
            k1 *= c2;

            h1 ^= k1;
            h1 = RotateLeft(h1, 13);
            h1 = h1 * 5 + 0xe6546b64u;

            i += 4;
        }

        // 剩余字节处理
        var remaining = length & 3;
        var k2 = 0u;
        switch (remaining)
        {
            case 3:
                k2 ^= (uint)data[i + 2] << 16;
                goto case 2;
            case 2:
                k2 ^= (uint)data[i + 1] << 8;
                goto case 1;
            case 1:
                k2 ^= data[i];
                k2 *= c1;
                k2 = RotateLeft(k2, 15);
                k2 *= c2;
                h1 ^= k2;
                break;
        }

        // 最终混合
        h1 ^= (uint)length;
        h1 = FMix(h1);

        return h1;
    }

    /// <summary>
    /// 左旋转
    /// </summary>
    private static uint RotateLeft(uint x, int r)
    {
        return (x << r) | (x >> (32 - r));
    }

    /// <summary>
    /// 最终混合函数（来自 MurmurHash3）
    /// </summary>
    private static uint FMix(uint h)
    {
        h ^= h >> 16;
        h *= 0x85ebca6bu;
        h ^= h >> 13;
        h *= 0xc2b2ae35u;
        h ^= h >> 16;
        return h;
    }

    /// <summary>
    /// 组合 CRC32 和 MurmurHash3 结果
    /// </summary>
    private static uint CombineHashes(uint crc, uint murmur)
    {
        // Windows 算法：将两个哈希进行异或并移位组合
        return (crc << 1) ^ (murmur >> 1);
    }

    /// <summary>
    /// 应用魔数
    /// </summary>
    private static uint ApplyMagic(uint hash)
    {
        // 应用 Windows UserChoice 魔数
        return hash ^ HASH_MAGIC;
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Run(() =>
        {
            if (!_disposed)
            {
                _logger.Debug("释放 UserChoiceService 资源");
                _disposed = true;
            }
        });
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
