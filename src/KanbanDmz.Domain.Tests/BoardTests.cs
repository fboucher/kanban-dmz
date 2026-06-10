using KanbanDmz.Domain;
using Xunit;

namespace KanbanDmz.Domain.Tests;

public class BoardTests
{
    [Fact]
    public void Board_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var board = new Board();

        // Assert
        Assert.True(board.IsPublic);
        Assert.Equal(string.Empty, board.Name);
        Assert.Equal(Guid.Empty, board.Id);
    }

    [Fact]
    public void Board_CanSetProperties()
    {
        // Arrange
        var board = new Board();
        var expectedId = Guid.NewGuid();
        var expectedName = "Project Alpha";
        var expectedIsPublic = false;

        // Act
        board.Id = expectedId;
        board.Name = expectedName;
        board.IsPublic = expectedIsPublic;

        // Assert
        Assert.Equal(expectedId, board.Id);
        Assert.Equal(expectedName, board.Name);
        Assert.Equal(expectedIsPublic, board.IsPublic);
    }
}
