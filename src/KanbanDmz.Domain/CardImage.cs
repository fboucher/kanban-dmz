namespace KanbanDmz.Domain;

public class CardImage
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsFeatureImage { get; set; }
    public bool IsPrivate { get; set; }
}
