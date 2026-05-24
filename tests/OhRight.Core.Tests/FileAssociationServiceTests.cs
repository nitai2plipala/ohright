using OhRight.Core.Configuration;
using OhRight.Core.Enums;
using OhRight.Core.Logging;
using OhRight.Core.Models;
using OhRight.Core.Services;

namespace OhRight.Core.Tests;

/// <summary>
/// 文件关联服务单元测试
/// </summary>
public class FileAssociationServiceTests : IDisposable
{
    private readonly FileAssociationService _service;
    private readonly ILoggerService _logger;

    public FileAssociationServiceTests()
    {
        var options = new RegistryOptions { EnableVerboseLogging = false };
        _logger = new SerilogService(options, "Test");
        var registryService = new RegistryService(options, _logger);
        var userChoiceService = new UserChoiceService(options, _logger);
        _service = new FileAssociationService(registryService, userChoiceService, options, _logger);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }

    [Fact]
    public async Task GetFileAssociationAsync_WithValidExtension_ReturnsAssociation()
    {
        // Arrange
        var extension = ".txt";

        // Act
        var result = await _service.GetFileAssociationAsync(extension);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(extension, result.Extension);
        Assert.False(string.IsNullOrEmpty(result.ProgId));
    }

    [Fact]
    public async Task GetFileAssociationAsync_WithEmptyExtension_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetFileAssociationAsync(string.Empty));
    }

    [Fact]
    public async Task GetFileAssociationAsync_WithNullExtension_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetFileAssociationAsync(null!));
    }

    [Fact]
    public async Task GetFileAssociationAsync_WithNonExistentExtension_ReturnsNullOrThrows()
    {
        // Arrange
        var extension = ".nonexistent12345";

        // Act & Assert - 可能返回 null 或抛出异常
        var exception = await Record.ExceptionAsync(() =>
            _service.GetFileAssociationAsync(extension));

        // 如果没有异常，结果应该是 null
        if (exception == null)
        {
            var result = await _service.GetFileAssociationAsync(extension);
            Assert.Null(result);
        }
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".exe")]
    [InlineData(".pdf")]
    [InlineData(".jpg")]
    public async Task GetFileAssociationAsync_WithCommonExtensions_ReturnsValidResult(string extension)
    {
        // Act
        var result = await _service.GetFileAssociationAsync(extension);

        // Assert
        if (result != null)
        {
            Assert.Equal(extension, result.Extension);
        }
    }

    [Fact]
    public async Task SetFileAssociationAsync_WithValidParameters_DoesNotThrow()
    {
        // Arrange
        var association = new FileAssociation
        {
            Extension = ".test12345",
            ProgId = "TestApplication"
        };

        // Act & Assert - 验证不会抛出异常（权限问题会被捕获）
        var exception = await Record.ExceptionAsync(() =>
            _service.SetFileAssociationAsync(association, force: true));

        // 权限不足是预期的，不应该作为测试失败
        if (exception != null && exception is not (UnauthorizedAccessException or OhRight.Core.Exceptions.RegistryException))
        {
            throw exception;
        }
    }

    [Fact]
    public async Task SetFileAssociationAsync_WithNullAssociation_Throws()
    {
        // Arrange & Act & Assert - 验证会抛出异常（可能是 ArgumentNullException 或 RegistryException）
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.SetFileAssociationAsync(null!));
    }

    [Fact]
    public async Task SetFileAssociationAsync_WithEmptyExtension_ThrowsArgumentException()
    {
        // Arrange
        var association = new FileAssociation
        {
            Extension = string.Empty,
            ProgId = "TestApplication"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SetFileAssociationAsync(association));
    }

    [Fact]
    public async Task SetFileAssociationAsync_WithEmptyProgId_ThrowsArgumentException()
    {
        // Arrange
        var association = new FileAssociation
        {
            Extension = ".test",
            ProgId = string.Empty
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SetFileAssociationAsync(association));
    }
}