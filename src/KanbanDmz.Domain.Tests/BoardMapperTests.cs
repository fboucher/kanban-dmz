using System;
using System.Collections.Generic;
using System.Linq;
using KanbanDmz.Domain;
using KanbanDmz.Domain.DTOs;
using Xunit;

namespace KanbanDmz.Domain.Tests;

public class BoardMapperTests
{
    [Fact]
    public void MapToDetail_ShouldThrowArgumentNullException_WhenBoardIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            BoardMapper.MapToDetail(null!, null, null, null, null));
    }

    [Fact]
    public void MapToDetail_ShouldMapBoardPropertiesCorrectly()
    {
        // Arrange
        var board = new Board
        {
            Id = Guid.NewGuid(),
            Name = "Development Board",
            IsPublic = false
        };

        // Act
        var result = BoardMapper.MapToDetail(board, null, null, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(board.Id, result.Id);
        Assert.Equal(board.Name, result.Name);
        Assert.Equal(board.IsPublic, result.IsPublic);
        Assert.Empty(result.Columns);
    }

    [Fact]
    public void MapToDetail_ShouldMapAndSortColumnsCorrectly()
    {
        // Arrange
        var board = new Board { Id = Guid.NewGuid(), Name = "Board" };
        var columns = new List<Column>
        {
            new() { Id = Guid.NewGuid(), BoardId = board.Id, Name = "In Progress", SortOrder = 2 },
            new() { Id = Guid.NewGuid(), BoardId = board.Id, Name = "Backlog", SortOrder = 0 },
            new() { Id = Guid.NewGuid(), BoardId = board.Id, Name = "To Do", SortOrder = 1 }
        };

        // Act
        var result = BoardMapper.MapToDetail(board, columns, null, null, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Columns.Count);
        Assert.Equal("Backlog", result.Columns[0].Name);
        Assert.Equal(0, result.Columns[0].SortOrder);
        Assert.Equal("To Do", result.Columns[1].Name);
        Assert.Equal(1, result.Columns[1].SortOrder);
        Assert.Equal("In Progress", result.Columns[2].Name);
        Assert.Equal(2, result.Columns[2].SortOrder);
    }

    [Fact]
    public void MapToDetail_ShouldAssociateCardsAndTagsAndCategoriesCorrectly()
    {
        // Arrange
        var board = new Board { Id = Guid.NewGuid(), Name = "Board" };
        var colId = Guid.NewGuid();
        var columns = new List<Column>
        {
            new() { Id = colId, BoardId = board.Id, Name = "Backlog", SortOrder = 0 }
        };

        var cardId1 = Guid.NewGuid();
        var cardId2 = Guid.NewGuid();
        var cards = new List<Card>
        {
            new()
            {
                Id = cardId1,
                BoardId = board.Id,
                ColumnId = colId,
                Title = "First Card",
                PublicDescription = "Public info 1",
                CategoryId = 2,
                AssignedTo = "Frank",
                IsPublic = true,
                ImageUrl = "http://example.com/image.png"
            },
            new()
            {
                Id = cardId2,
                BoardId = board.Id,
                ColumnId = colId,
                Title = "Second Card",
                PublicDescription = "Public info 2",
                CategoryId = 99, // Unknown category
                AssignedTo = "",
                IsPublic = true,
                ImageUrl = null
            }
        };

        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Bug" },
            new() { Id = 2, Name = "Feature" }
        };

        var tags = new List<CardTag>
        {
            new() { CardId = cardId1, Tag = "tag-a" },
            new() { CardId = cardId1, Tag = "tag-b" },
            new() { CardId = cardId2, Tag = "tag-c" }
        };

        // Act
        var result = BoardMapper.MapToDetail(board, columns, cards, categories, tags);

        // Assert
        Assert.NotNull(result);
        var column = result.Columns.First();
        Assert.Equal(2, column.Cards.Count);

        var card1 = column.Cards.First(c => c.Id == cardId1);
        Assert.Equal("First Card", card1.Title);
        Assert.Equal("Public info 1", card1.PublicDescription);
        Assert.Equal("Feature", card1.CategoryName);
        Assert.Equal(2, card1.Tags.Count);
        Assert.Contains("tag-a", card1.Tags.Select(t => t.Name));
        Assert.Contains("tag-b", card1.Tags.Select(t => t.Name));
        Assert.Equal("Frank", card1.AssignedTo);
        Assert.True(card1.IsPublic);
        Assert.Equal("http://example.com/image.png", card1.ImageUrl);

        var card2 = column.Cards.First(c => c.Id == cardId2);
        Assert.Equal("Second Card", card2.Title);
        Assert.Equal("Uncategorized", card2.CategoryName);
        Assert.Single(card2.Tags);
        Assert.Contains("tag-c", card2.Tags.Select(t => t.Name));
        Assert.Equal("", card2.AssignedTo);
        Assert.Null(card2.ImageUrl);
    }

    [Fact]
    public void MapToDetail_ShouldSetImageUrlToFeatureImage_WhenFeatureImageExists()
    {
        // Arrange
        var board = new Board { Id = Guid.NewGuid(), Name = "Board" };
        var colId = Guid.NewGuid();
        var columns = new List<Column>
        {
            new() { Id = colId, BoardId = board.Id, Name = "Backlog", SortOrder = 0 }
        };

        var cardId = Guid.NewGuid();
        var cards = new List<Card>
        {
            new()
            {
                Id = cardId,
                BoardId = board.Id,
                ColumnId = colId,
                Title = "Test Card",
                ImageUrl = "http://example.com/fallback.png"
            }
        };

        var images = new List<CardImage>
        {
            new() { Id = Guid.NewGuid(), CardId = cardId, ImageUrl = "http://example.com/regular.png", IsFeatureImage = false },
            new() { Id = Guid.NewGuid(), CardId = cardId, ImageUrl = "http://example.com/feature.png", IsFeatureImage = true }
        };

        // Act
        var result = BoardMapper.MapToDetail(board, columns, cards, null, null, null, images);

        // Assert
        Assert.NotNull(result);
        var mappedCard = result.Columns.First().Cards.First();
        Assert.Equal("http://example.com/feature.png", mappedCard.ImageUrl);
    }
}
