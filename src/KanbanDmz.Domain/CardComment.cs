namespace KanbanDmz.Domain;

public class CardComment
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsPublic { get; set; } = true;
}
