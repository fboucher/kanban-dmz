namespace KanbanDmz.Domain;

public class Card
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public Guid ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PublicDescription { get; set; } = string.Empty;
    public string PrivateDescription { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
}
