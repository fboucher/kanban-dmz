using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KanbanDmz.Domain;
using KanbanDmz.Domain.DTOs;
using KanbanDmz.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KanbanDmz.Domain.Tests;

public class CommentTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public Func<HttpRequestMessage, HttpResponseMessage>? ResponseFunc { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (ResponseFunc != null)
            {
                return Task.FromResult(ResponseFunc(request));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    [Fact]
    public async Task GetCommentsAsync_SendsCorrectGetRequest_ReturnsComments()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var cardId = Guid.NewGuid();
        var expectedComments = new List<CardComment>
        {
            new() { Id = Guid.NewGuid(), CardId = cardId, Content = "First comment", CreatedBy = "Alice", CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new() { Id = Guid.NewGuid(), CardId = cardId, Content = "Second comment", CreatedBy = "Bob", CreatedAt = DateTime.UtcNow }
        };

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Get && req.RequestUri!.PathAndQuery == $"/CardComment?$filter=cardid%20eq%20{cardId}&$orderby=createdat")
            {
                var responseObj = new DabResponse<CardComment> { Value = expectedComments };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(responseObj), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.GetCommentsAsync(cardId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("First comment", result[0].Content);
        Assert.Equal("Second comment", result[1].Content);
        Assert.Single(fakeHandler.Requests);

        var request = fakeHandler.Requests[0];
        Assert.Equal(HttpMethod.Get, request.Method);
    }

    [Fact]
    public async Task CreateCommentAsync_SendsCorrectPayload_ReturnsCreatedComment()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var cardId = Guid.NewGuid();
        var commentContent = "Nice progress!";
        var expectedComment = new CardComment
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            Content = commentContent,
            CreatedBy = "Unknown", // since no token is set in mock, defaults to Unknown
            CreatedAt = DateTime.UtcNow
        };

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath == "/CardComment")
            {
                var responseObj = new DabResponse<CardComment> { Value = [expectedComment] };
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(JsonSerializer.Serialize(responseObj), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.CreateCommentAsync(cardId, commentContent);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedComment.Id, result.Id);
        Assert.Equal(commentContent, result.Content);
        Assert.Equal("Unknown", result.CreatedBy);
        Assert.Single(fakeHandler.Requests);

        var request = fakeHandler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/CardComment", request.RequestUri!.AbsolutePath);
    }
}
