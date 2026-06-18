using Markdig;

namespace KanbanDmz.Web.Helpers
{
    public static class MarkdownHelper
    {
        public static string ToHtml(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return string.Empty;
            }

            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .DisableHtml()
                .Build();

            return Markdown.ToHtml(markdown, pipeline);
        }

        public static string ToPlainText(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return string.Empty;
            }

            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .DisableHtml()
                .Build();

            return Markdown.ToHtml(markdown, pipeline);
        }
    }
}