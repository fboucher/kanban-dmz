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
        IEnumerable<CardTag>? tags)
    {
        if (board == null)
        {
            throw new ArgumentNullException(nameof(board));
        }

        var categoriesMap = (categories ?? Array.Empty<Category>())
            .Where(c => c != null)
            .ToDictionary(c => c.Id, c => c.Name);

        var tagsGrouped = (tags ?? Array.Empty<CardTag>())
            .Where(t => t != null)
            .GroupBy(t => t.CardId)
            .ToDictionary(g => g.Key, g => g.Select(t => t.Tag).ToList());

        var columnDtos = (columns ?? Array.Empty<Column>())
            .Where(c => c != null)
            .OrderBy(c => c.SortOrder)
            .Select(c => new ColumnDetailDto
            {
                Id = c.Id,
                Name = c.Name,
                SortOrder = c.SortOrder,
                Cards = (cards ?? Array.Empty<Card>())
                    .Where(card => card != null && card.ColumnId == c.Id)
                    .Select(card => new CardDetailDto
                    {
                        Id = card.Id,
                        Title = card.Title,
                        PublicDescription = card.PublicDescription,
                        PrivateDescription = card.PrivateDescription,
                        CategoryName = categoriesMap.TryGetValue(card.CategoryId, out var catName) ? catName : "Uncategorized",
                        Tags = tagsGrouped.TryGetValue(card.Id, out var cardTags) ? cardTags : new List<string>(),
                        AssignedTo = card.AssignedTo,
                        IsPublic = card.IsPublic
                    })
                    .ToList()
            })
            .ToList();

        return new BoardDetailDto
        {
            Id = board.Id,
            Name = board.Name,
            IsPublic = board.IsPublic,
            Columns = columnDtos
        };
    }
}
