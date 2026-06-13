using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KanbanDmz.Domain;
using KanbanDmz.Domain.DTOs;
using KanbanDmz.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KanbanDmz.Domain.Tests;

public class CardCrudTests
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
    public async Task CreateCardAsync_SendsCorrectPayload_ReturnsCreatedCard()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var cardToCreate = new Card
        {
            BoardId = Guid.NewGuid(),
            ColumnId = Guid.NewGuid(),
            Title = "New Task",
            PublicDescription = "Public text",
            PrivateDescription = "Private text",
            CategoryId = 1,
            CreatedBy = "Frank",
            AssignedTo = "Bob",
            IsPublic = true
        };

        var expectedCard = new Card
        {
            Id = Guid.NewGuid(),
            BoardId = cardToCreate.BoardId,
            ColumnId = cardToCreate.ColumnId,
            Title = cardToCreate.Title,
            PublicDescription = cardToCreate.PublicDescription,
            PrivateDescription = cardToCreate.PrivateDescription,
            CategoryId = cardToCreate.CategoryId,
            CreatedBy = cardToCreate.CreatedBy,
            AssignedTo = cardToCreate.AssignedTo,
            IsPublic = cardToCreate.IsPublic,
            CreatedAt = DateTime.UtcNow
        };

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath == "/Card")
            {
                var responseObj = new DabResponse<Card> { Value = [expectedCard] };
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(JsonSerializer.Serialize(responseObj), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.CreateCardAsync(cardToCreate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedCard.Id, result.Id);
        Assert.Equal(expectedCard.Title, result.Title);
        Assert.Single(fakeHandler.Requests);
        
        var request = fakeHandler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/Card", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpdateCardAsync_SendsPatchRequest_ReturnsTrue()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var cardId = Guid.NewGuid();
        var cardToUpdate = new Card
        {
            Id = cardId,
            BoardId = Guid.NewGuid(),
            ColumnId = Guid.NewGuid(),
            Title = "Updated Task",
            PublicDescription = "Updated Public",
            PrivateDescription = "Updated Private",
            CategoryId = 2,
            AssignedTo = "Alice",
            IsPublic = false
        };

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Patch && req.RequestUri!.AbsolutePath == $"/Card/id/{cardId}")
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.UpdateCardAsync(cardToUpdate);

        // Assert
        Assert.True(result);
        Assert.Single(fakeHandler.Requests);
        
        var request = fakeHandler.Requests[0];
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal($"/Card/id/{cardId}", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task DeleteCardAsync_SendsDeleteRequest_ReturnsTrue()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var cardId = Guid.NewGuid();

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Delete && req.RequestUri!.AbsolutePath == $"/Card/id/{cardId}")
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.DeleteCardAsync(cardId);

        // Assert
        Assert.True(result);
        Assert.Single(fakeHandler.Requests);
        
        var request = fakeHandler.Requests[0];
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal($"/Card/id/{cardId}", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpdateCardTagsAsync_SynchronizesTagsCorrectly()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var cardId = Guid.NewGuid();
        var existingTags = new List<CardTag>
        {
            new() { CardId = cardId, Tag = "tag1" },
            new() { CardId = cardId, Tag = "tag2" }
        };

        // We want to update tags to: ["tag2", "tag3"]
        // This means "tag1" is deleted, "tag3" is added, and "tag2" remains untouched.
        var newTags = new List<string> { "tag2", "tag3" };

        fakeHandler.ResponseFunc = (req) =>
        {
            // Get existing tags
            if (req.Method == HttpMethod.Get && Uri.UnescapeDataString(req.RequestUri!.Query).Contains($"cardid eq {cardId}"))
            {
                var responseObj = new DabResponse<CardTag> { Value = existingTags };
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(responseObj), System.Text.Encoding.UTF8, "application/json")
                };
            }
            // Delete tag1
            if (req.Method == HttpMethod.Delete && req.RequestUri!.AbsolutePath == $"/CardTag/cardid/{cardId}/tag/tag1")
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            // Add tag3
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath == "/CardTag")
            {
                return new HttpResponseMessage(HttpStatusCode.Created);
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        await service.UpdateCardTagsAsync(cardId, newTags);

        // Assert
        // Requests should include: GET tags, DELETE tag1, POST tag3
        Assert.Equal(3, fakeHandler.Requests.Count);
        
        var getReq = fakeHandler.Requests[0];
        Assert.Equal(HttpMethod.Get, getReq.Method);
        Assert.Contains($"cardid eq {cardId}", Uri.UnescapeDataString(getReq.RequestUri!.Query));

        var deleteReq = fakeHandler.Requests[1];
        Assert.Equal(HttpMethod.Delete, deleteReq.Method);
        Assert.Equal($"/CardTag/cardid/{cardId}/tag/tag1", deleteReq.RequestUri!.AbsolutePath);

        var postReq = fakeHandler.Requests[2];
        Assert.Equal(HttpMethod.Post, postReq.Method);
        Assert.Equal("/CardTag", postReq.RequestUri!.AbsolutePath);
    }

    [Fact(Skip = "Debug tool to inspect assembly types")]
    public void InspectAssemblyTypes()
    {
        var assembly = typeof(Microsoft.FluentUI.AspNetCore.Components.FluentButton).Assembly;
        var types = assembly.GetTypes()
            .Select(t => t.FullName)
            .ToList();
        
        // System.IO.File.WriteAllLines("/home/frank/gh/kanban-dmz/types.txt", types!);
        Assert.True(false);
    }
}

