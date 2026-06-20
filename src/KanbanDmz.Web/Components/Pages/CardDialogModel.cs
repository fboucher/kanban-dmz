using System;
using System.Collections.Generic;
using KanbanDmz.Domain;
using KanbanDmz.Web.Helpers;

namespace KanbanDmz.Web.Components.Pages;

public class CardDialogModel
{
    private string _publicDescription = string.Empty;
    private string? _publicDescriptionHtml;
    private string? _publicDescriptionPreview;

    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public Guid ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;

    public string PublicDescription
    {
        get => _publicDescription;
        set
        {
            if (_publicDescription != value)
            {
                _publicDescription = value;
                _publicDescriptionHtml = null;
                _publicDescriptionPreview = null;
            }
        }
    }

    public string PrivateDescription { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
    public string? ImageUrl { get; set; }
    public List<CardImage> Images { get; set; } = [];
    public string Tags { get; set; } = string.Empty;
    public string? Color { get; set; }
    public bool IsEdit { get; set; }
    public bool IsDelete { get; set; }
    public bool IsViewMode { get; set; } = true;
    public bool IsAuthenticated { get; set; }
    public List<Column> Columns { get; set; } = [];
    public List<Category> Categories { get; set; } = [];

    public string PublicDescriptionPreview => _publicDescriptionPreview ??= MarkdownHelper.ToPlainText(PublicDescription);
    public string PublicDescriptionHtml => _publicDescriptionHtml ??= MarkdownHelper.ToHtml(PublicDescription);
}

