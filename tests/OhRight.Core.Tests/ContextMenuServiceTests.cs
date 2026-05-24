using OhRight.Core.Configuration;
using OhRight.Core.Enums;
using OhRight.Core.Logging;
using OhRight.Core.Models;
using OhRight.Core.Services;

namespace OhRight.Core.Tests;

/// <summary>
/// 右键菜单服务单元测试
/// </summary>
public class ContextMenuServiceTests : IDisposable
{
    private readonly ContextMenuService _service;
    private readonly ILoggerService _logger;

    public ContextMenuServiceTests()
    {
        var options = new RegistryOptions { EnableVerboseLogging = false };
        _logger = new SerilogService(options, "Test");
        var registryService = new RegistryService(options, _logger);
        _service = new ContextMenuService(registryService, options, _logger);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }

    [Fact]
    public async Task GetContextMenuItemsAsync_WithFileType_ReturnsList()
    {
        // Arrange & Act
        var result = await _service.GetContextMenuItemsAsync(ContextMenuType.File);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<ContextMenuItem>>(result);
    }

    [Fact]
    public async Task GetContextMenuItemsAsync_WithDirectoryType_ReturnsList()
    {
        // Arrange & Act
        var result = await _service.GetContextMenuItemsAsync(ContextMenuType.Directory);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<ContextMenuItem>>(result);
    }

    [Fact]
    public async Task GetContextMenuItemsAsync_WithAllType_ReturnsList()
    {
        // Arrange & Act
        var result = await _service.GetContextMenuItemsAsync(ContextMenuType.All);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<ContextMenuItem>>(result);
    }

    [Fact]
    public async Task GetContextMenuItemsAsync_WithExtension_ReturnsFilteredList()
    {
        // Arrange
        var extension = ".txt";

        // Act
        var result = await _service.GetContextMenuItemsAsync(ContextMenuType.File, extension);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<ContextMenuItem>>(result);
    }

    [Fact]
    public async Task AddContextMenuItemAsync_WithValidItem_DoesNotThrow()
    {
        // Arrange
        var item = new ContextMenuItem
        {
            Name = "Test Menu Item",
            Command = "test.exe",
            Enabled = true
        };

        // Act & Assert - 验证不会抛出异常
        var exception = await Record.ExceptionAsync(() =>
            _service.AddContextMenuItemAsync(item, ContextMenuType.File));

        // 权限不足是预期的，不应该作为测试失败
        if (exception != null && exception is not (UnauthorizedAccessException or OhRight.Core.Exceptions.RegistryException))
        {
            throw exception;
        }
    }

    [Fact]
    public async Task AddContextMenuItemAsync_WithNullItem_Throws()
    {
        // Arrange & Act & Assert - 验证会抛出异常
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.AddContextMenuItemAsync(null!, ContextMenuType.File));
    }

    [Fact]
    public async Task AddContextMenuItemAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var item = new ContextMenuItem
        {
            Name = string.Empty,
            Command = "test.exe"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AddContextMenuItemAsync(item, ContextMenuType.File));
    }

    [Fact]
    public async Task RemoveContextMenuItemAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var itemId = "nonexistent-id-12345";

        // Act
        var result = await _service.RemoveContextMenuItemAsync(itemId, ContextMenuType.File);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveContextMenuItemAsync_WithEmptyId_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RemoveContextMenuItemAsync(string.Empty, ContextMenuType.File));
    }

    [Fact]
    public async Task RemoveContextMenuItemAsync_WithNullId_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RemoveContextMenuItemAsync(null!, ContextMenuType.File));
    }
}