using KanbanDmz.Domain;
using Xunit;

namespace KanbanDmz.Domain.Tests;

public class CardTests
{
    [Fact]
    public void Card_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var card = new Card();

        // Assert
        Assert.True(card.IsPublic);
        Assert.Equal(Guid.Empty, card.Id);
        Assert.Equal(Guid.Empty, card.BoardId);
        Assert.Equal(Guid.Empty, card.ColumnId);
        Assert.Equal(string.Empty, card.Title);
        Assert.Equal(string.Empty, card.PublicDescription);
        Assert.Equal(string.Empty, card.PrivateDescription);
        Assert.Equal(0, card.CategoryId);
        Assert.Equal(string.Empty, card.CreatedBy);
        Assert.Equal(string.Empty, card.AssignedTo);
    }

    [Fact]
    public void Card_CanSetProperties()
    {
        // Arrange
        var card = new Card();
        var expectedId = Guid.NewGuid();
        var expectedBoardId = Guid.NewGuid();
        var expectedColumnId = Guid.NewGuid();
        var expectedTitle = "Implement auth";
        var expectedPublicDesc = "Configure Keycloak integration";
        var expectedPrivateDesc = "Admin client credentials client-secret: keycloak-secret-xyz";
        var expectedCategory = 2;
        var expectedCreatedBy = "alice@company.com";
        var expectedAssignedTo = "bob@company.com";
        var expectedIsPublic = false;
        var expectedCreatedAt = DateTime.UtcNow;

        // Act
        card.Id = expectedId;
        card.BoardId = expectedBoardId;
        card.ColumnId = expectedColumnId;
        card.Title = expectedTitle;
        card.PublicDescription = expectedPublicDesc;
        card.PrivateDescription = expectedPrivateDesc;
        card.CategoryId = expectedCategory;
        card.CreatedBy = expectedCreatedBy;
        card.AssignedTo = expectedAssignedTo;
        card.IsPublic = expectedIsPublic;
        card.CreatedAt = expectedCreatedAt;

        // Assert
        Assert.Equal(expectedId, card.Id);
        Assert.Equal(expectedBoardId, card.BoardId);
        Assert.Equal(expectedColumnId, card.ColumnId);
        Assert.Equal(expectedTitle, card.Title);
        Assert.Equal(expectedPublicDesc, card.PublicDescription);
        Assert.Equal(expectedPrivateDesc, card.PrivateDescription);
        Assert.Equal(expectedCategory, card.CategoryId);
        Assert.Equal(expectedCreatedBy, card.CreatedBy);
        Assert.Equal(expectedAssignedTo, card.AssignedTo);
        Assert.Equal(expectedIsPublic, card.IsPublic);
        Assert.Equal(expectedCreatedAt, card.CreatedAt);
    }
}
