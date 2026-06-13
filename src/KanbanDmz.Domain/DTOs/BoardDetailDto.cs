using System.Text.Json.Serialization;

namespace KanbanDmz.Domain.DTOs;

public class BoardDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public List<ColumnDetailDto> Columns { get; set; } = [];
}

public class ColumnDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<CardDetailDto> Cards { get; set; } = [];
}

public class CardDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PublicDescription { get; set; } = string.Empty;
    public string PrivateDescription { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string AssignedTo { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
}

public class DabResponse<T>
{
    [JsonPropertyName("value")]
    public List<T> Value { get; set; } = [];
}
