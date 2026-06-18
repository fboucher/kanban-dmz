using KanbanDmz.Web.Helpers;
using Xunit;

namespace KanbanDmz.Domain.Tests;

public class MarkdownHelperTests
{
    [Fact]
    public void ToHtml_ShouldRenderMarkdownToHtml()
    {
        // Arrange
        var markdown = "This is **bold** text and a [link](https://google.com).";

        // Act
        var html = MarkdownHelper.ToHtml(markdown);

        // Assert
        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("<a href=\"https://google.com\">link</a>", html);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToHtml_ShouldReturnEmpty_WhenInputIsEmptyOrWhitespace(string? input)
    {
        // Act
        var html = MarkdownHelper.ToHtml(input!);

        // Assert
        Assert.Equal(string.Empty, html);
    }

    [Fact]
    public void ToHtml_ShouldEscapeRawHtml_ForXssProtection()
    {
        // Arrange
        var markdown = "Some text <script>alert('xss')</script> and <iframe src='malicious'></iframe>.";

        // Act
        var html = MarkdownHelper.ToHtml(markdown);

        // Assert
        // Raw HTML should be escaped, not rendered as active HTML elements
        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("<iframe>", html);
        Assert.Contains("&lt;script&gt;alert('xss')&lt;/script&gt;", html);
        Assert.Contains("&lt;iframe src='malicious'&gt;&lt;/iframe&gt;", html);
    }

    [Fact]
    public void ToPlainText_ShouldStripMarkdownTags()
    {
        // Arrange
        var markdown = "# Heading 1\n\nThis is **bold** text with a [link](https://google.com).\n\n- Item 1\n- Item 2";

        // Act
        var text = MarkdownHelper.ToPlainText(markdown);

        // Assert
        // Should contain raw text, but no markdown syntax or HTML tags
        Assert.Contains("Heading 1", text);
        Assert.Contains("This is bold text with a link.", text);
        Assert.Contains("Item 1", text);
        Assert.Contains("Item 2", text);
        Assert.DoesNotContain("#", text);
        Assert.DoesNotContain("**", text);
        Assert.DoesNotContain("[link]", text);
        Assert.DoesNotContain("href", text);
        Assert.DoesNotContain("<p>", text);
        Assert.DoesNotContain("<li>", text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToPlainText_ShouldReturnEmpty_WhenInputIsEmptyOrWhitespace(string? input)
    {
        // Act
        var text = MarkdownHelper.ToPlainText(input!);

        // Assert
        Assert.Equal(string.Empty, text);
    }
}
