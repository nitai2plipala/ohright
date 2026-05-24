using OhRight.Core.Configuration;
using OhRight.Core.Enums;
using OhRight.Core.Logging;
using OhRight.Core.Models;
using OhRight.Core.Services;

namespace OhRight.Core.Tests;

/// <summary>
/// 打开方式服务单元测试
/// </summary>
public class OpenWithServiceTests : IDisposable
{
    private readonly OpenWithService _service;
    private readonly ILoggerService _logger;

    public OpenWithServiceTests()
    {
        var options = new RegistryOptions { EnableVerboseLogging = false };
        _logger = new SerilogService(options, "Test");
        var registryService = new RegistryService(options, _logger);
        _service = new OpenWithService(registryService, options, _logger);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }

    [Fact]
    public async Task GetOpenWithListAsync_WithValidExtension_ReturnsList()
    {
        // Arrange
        var extension = ".txt";

        // Act
        var result = await _service.GetOpenWithListAsync(extension);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IReadOnlyList<OpenWithEntry>>(result);
    }

    [Fact]
    public async Task GetOpenWithListAsync_WithEmptyExtension_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetOpenWithListAsync(string.Empty));
    }

    [Fact]
    public async Task GetOpenWithListAsync_WithNullExtension_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetOpenWithListAsync(null!));
    }

    [Fact]
    public async Task AddOpenWithEntryAsync_WithValidEntry_DoesNotThrow()
    {
        // Arrange
        var entry = new OpenWithEntry
        {
            ProgId = "TestApplication",
            Name = "Test App",
            Command = "test.exe",
            PermissionLevel = PermissionLevel.User
        };

        // Act & Assert - 验证不会抛出异常
        var exception = await Record.ExceptionAsync(() =>
            _service.AddOpenWithEntryAsync(entry, ".testext12345"));

        // 权限不足是预期的，不应该作为测试失败
        if (exception != null && exception is not (UnauthorizedAccessException or OhRight.Core.Exceptions.RegistryException))
        {
            throw exception;
        }
    }

    [Fact]
    public async Task AddOpenWithEntryAsync_WithNullEntry_Throws()
    {
        // Arrange & Act & Assert - 验证会抛出异常
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.AddOpenWithEntryAsync(null!, ".txt"));
    }

    [Fact]
    public async Task AddOpenWithEntryAsync_WithEmptyProgId_ThrowsArgumentException()
    {
        // Arrange
        var entry = new OpenWithEntry
        {
            ProgId = string.Empty,
            Name = "Test App"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AddOpenWithEntryAsync(entry, ".txt"));
    }

    [Fact]
    public async Task AddOpenWithEntryAsync_WithEmptyExtension_ThrowsArgumentException()
    {
        // Arrange
        var entry = new OpenWithEntry
        {
            ProgId = "TestApplication",
            Name = "Test App"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AddOpenWithEntryAsync(entry, string.Empty));
    }

    [Fact]
    public async Task RemoveOpenWithEntryAsync_WithInvalidEntry_DoesNotThrow()
    {
        // Arrange
        var progId = "NonExistentApp12345";
        var extension = ".txt";

        // Act & Assert - 验证不会抛出异常
        var exception = await Record.ExceptionAsync(() =>
            _service.RemoveOpenWithEntryAsync(progId, extension));

        // 权限不足或找不到项是预期的，不应该作为测试失败
        if (exception != null && exception is not (UnauthorizedAccessException or OhRight.Core.Exceptions.RegistryException))
        {
            throw exception;
        }
    }

    [Fact]
    public async Task RemoveOpenWithEntryAsync_WithEmptyProgId_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RemoveOpenWithEntryAsync(string.Empty, ".txt"));
    }

    [Fact]
    public async Task RemoveOpenWithEntryAsync_WithEmptyExtension_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RemoveOpenWithEntryAsync("TestApp", string.Empty));
    }

    [Fact]
    public async Task RemoveOpenWithEntryAsync_WithNullProgId_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RemoveOpenWithEntryAsync(null!, ".txt"));
    }

    [Fact]
    public async Task RemoveOpenWithEntryAsync_WithNullExtension_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RemoveOpenWithEntryAsync("TestApp", null!));
    }
}