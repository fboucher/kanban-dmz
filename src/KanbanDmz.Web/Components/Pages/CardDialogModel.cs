using System;
using System.Collections.Generic;
using KanbanDmz.Domain;

namespace KanbanDmz.Web.Components.Pages;

public class CardDialogModel
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public Guid ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PublicDescription { get; set; } = string.Empty;
    public string PrivateDescription { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
    public string? ImageUrl { get; set; }
    public string Tags { get; set; } = string.Empty;
    public bool IsEdit { get; set; }
    public bool IsDelete { get; set; }
    public List<Column> Columns { get; set; } = [];
    public List<Category> Categories { get; set; } = [];
}
