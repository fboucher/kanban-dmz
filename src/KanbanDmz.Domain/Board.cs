namespace KanbanDmz.Domain;

public class Board
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
    public string? BackgroundColor { get; set; }
}
