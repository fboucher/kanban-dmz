namespace KanbanDmz.Domain;

public class Column
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
