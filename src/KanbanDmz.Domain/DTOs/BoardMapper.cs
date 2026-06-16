using System;
using System.Collections.Generic;
using System.Linq;

namespace KanbanDmz.Domain.DTOs;

public static class BoardMapper
{
    public static BoardDetailDto MapToDetail(
        Board board,
        IEnumerable<Column>? columns,
        IEnumerable<Card>? cards,
        IEnumerable<Category>? categories,
        IEnumerable<CardTag>? tags,
        IEnumerable<Tag>? allTags = null)
    {
        if (board == null)
        {
            throw new ArgumentNullException(nameof(board));
        }

        var categoriesMap = (categories ?? Array.Empty<Category>())
            .Where(c => c != null)
            .ToDictionary(c => c.Id, c => c);

        var tagColorsMap = (allTags ?? Array.Empty<Tag>())
            .Where(t => t != null)
            .ToDictionary(t => t.Name, t => t.Color, StringComparer.OrdinalIgnoreCase);

        var tagsGrouped = (tags ?? Array.Empty<CardTag>())
            .Where(t => t != null)
            .GroupBy(t => t.CardId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(t => new TagDto
                {
                    Name = t.Tag,
                    Color = tagColorsMap.TryGetValue(t.Tag, out var col) ? col : "#E0E0E0"
                }).ToList()
            );

        var columnDtos = (columns ?? Array.Empty<Column>())
            .Where(c => c != null)
            .OrderBy(c => c.SortOrder)
            .Select(c => new ColumnDetailDto
            {
                Id = c.Id,
                Name = c.Name,
                SortOrder = c.SortOrder,
                Color = c.Color,
                Cards = (cards ?? Array.Empty<Card>())
                    .Where(card => card != null && card.ColumnId == c.Id)
                    .Select(card => new CardDetailDto
                    {
                        Id = card.Id,
                        Title = card.Title,
                        PublicDescription = card.PublicDescription,
                        PrivateDescription = card.PrivateDescription,
                        CategoryName = categoriesMap.TryGetValue(card.CategoryId, out var cat) ? cat.Name : "Uncategorized",
                        CategoryColor = categoriesMap.TryGetValue(card.CategoryId, out var catCol) ? catCol.Color : null,
                        Tags = tagsGrouped.TryGetValue(card.Id, out var cardTags) ? cardTags : new List<TagDto>(),
                        AssignedTo = card.AssignedTo,
                        IsPublic = card.IsPublic,
                        ImageUrl = card.ImageUrl,
                        Color = card.Color
                    })
                    .ToList()
            })
            .ToList();

        return new BoardDetailDto
        {
            Id = board.Id,
            Name = board.Name,
            IsPublic = board.IsPublic,
            BackgroundColor = board.BackgroundColor,
            Columns = columnDtos
        };
    }
}
