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

public class ColumnCrudTests
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
    public async Task CreateColumnAsync_SendsCorrectPayload_ReturnsCreatedColumn()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var columnToCreate = new Column
        {
            BoardId = Guid.NewGuid(),
            Name = "New Column",
            SortOrder = 5
        };

        var expectedColumn = new Column
        {
            Id = Guid.NewGuid(),
            BoardId = columnToCreate.BoardId,
            Name = columnToCreate.Name,
            SortOrder = columnToCreate.SortOrder
        };

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Post && req.RequestUri!.AbsolutePath == "/Column")
            {
                var responseObj = new DabResponse<Column> { Value = [expectedColumn] };
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(JsonSerializer.Serialize(responseObj), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.CreateColumnAsync(columnToCreate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedColumn.Id, result.Id);
        Assert.Equal(expectedColumn.Name, result.Name);
        Assert.Equal(expectedColumn.SortOrder, result.SortOrder);
        Assert.Single(fakeHandler.Requests);
        
        var request = fakeHandler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/Column", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task UpdateColumnAsync_SendsPatchRequest_ReturnsTrue()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var columnId = Guid.NewGuid();
        var columnToUpdate = new Column
        {
            Id = columnId,
            BoardId = Guid.NewGuid(),
            Name = "Updated Name",
            SortOrder = 2
        };

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Patch && req.RequestUri!.AbsolutePath == $"/Column/id/{columnId}")
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.UpdateColumnAsync(columnToUpdate);

        // Assert
        Assert.True(result);
        Assert.Single(fakeHandler.Requests);
        
        var request = fakeHandler.Requests[0];
        Assert.Equal(HttpMethod.Patch, request.Method);
        Assert.Equal($"/Column/id/{columnId}", request.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task DeleteColumnAsync_SendsDeleteRequest_ReturnsTrue()
    {
        // Arrange
        var fakeHandler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("http://fake-api/") };
        var service = new KanbanService(httpClient, NullLogger<KanbanService>.Instance);

        var columnId = Guid.NewGuid();

        fakeHandler.ResponseFunc = (req) =>
        {
            if (req.Method == HttpMethod.Delete && req.RequestUri!.AbsolutePath == $"/Column/id/{columnId}")
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await service.DeleteColumnAsync(columnId);

        // Assert
        Assert.True(result);
        Assert.Single(fakeHandler.Requests);
        
        var request = fakeHandler.Requests[0];
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal($"/Column/id/{columnId}", request.RequestUri!.AbsolutePath);
    }
}
