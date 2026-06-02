using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using FluentAssertions;
using FlowSharp.Application.Nodes;
using FlowSharp.Nodes.Http;
using FlowSharp.Tests.Fixtures;
using Xunit;

namespace FlowSharp.Tests.Nodes;

public class HttpNodeTests
{
    /// <summary>Verilen yaniti donen ve son istegi yakalayan stub handler.</summary>
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
            {
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }
    }

    [Fact]
    public async Task HttpRequest_get_parses_json_response()
    {
        var stub = new StubHandler(HttpStatusCode.OK, """{"ok":true,"id":7}""");
        var node = new HttpRequestNode();
        var ctx = new FakeNodeExecutionContext(
            parameters: new JsonObject { ["method"] = "GET", ["url"] = "https://api.test/items/7" },
            items: [NodeItem.From(new JsonObject())],
            httpHandler: stub);

        var item = (await node.ExecuteAsync(ctx)).PrimaryItems.Single().Json;

        item["statusCode"]!.GetValue<int>().Should().Be(200);
        item["success"]!.GetValue<bool>().Should().BeTrue();
        item["body"]!["id"]!.GetValue<int>().Should().Be(7);
        stub.LastRequest!.Method.Should().Be(HttpMethod.Get);
    }

    [Fact]
    public async Task HttpRequest_resolves_url_expression_from_item()
    {
        var stub = new StubHandler(HttpStatusCode.OK, "{}");
        var node = new HttpRequestNode();
        var ctx = new FakeNodeExecutionContext(
            parameters: new JsonObject { ["method"] = "GET", ["url"] = "https://api.test/u/{{ $json.id }}" },
            items: [NodeItem.From(new JsonObject { ["id"] = 42 })],
            httpHandler: stub);

        await node.ExecuteAsync(ctx);

        stub.LastRequest!.RequestUri!.ToString().Should().Be("https://api.test/u/42");
    }

    [Fact]
    public async Task HttpRequest_post_sends_json_body()
    {
        var stub = new StubHandler(HttpStatusCode.Created, """{"created":1}""");
        var node = new HttpRequestNode();
        var ctx = new FakeNodeExecutionContext(
            parameters: new JsonObject
            {
                ["method"] = "POST",
                ["url"] = "https://api.test/create",
                ["body"] = new JsonObject { ["name"] = "Ada" }
            },
            items: [NodeItem.From(new JsonObject())],
            httpHandler: stub);

        var item = (await node.ExecuteAsync(ctx)).PrimaryItems.Single().Json;

        item["statusCode"]!.GetValue<int>().Should().Be(201);
        stub.LastRequest!.Method.Should().Be(HttpMethod.Post);
        stub.LastBody.Should().Contain("Ada");
    }

    [Fact]
    public async Task HttpRequest_reports_non_success_status()
    {
        var stub = new StubHandler(HttpStatusCode.NotFound, """{"error":"yok"}""");
        var node = new HttpRequestNode();
        var ctx = new FakeNodeExecutionContext(
            parameters: new JsonObject { ["method"] = "GET", ["url"] = "https://api.test/missing" },
            items: [NodeItem.From(new JsonObject())],
            httpHandler: stub);

        var item = (await node.ExecuteAsync(ctx)).PrimaryItems.Single().Json;
        item["statusCode"]!.GetValue<int>().Should().Be(404);
        item["success"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task HttpRequest_without_url_fails()
    {
        var node = new HttpRequestNode();
        var ctx = new FakeNodeExecutionContext(
            parameters: new JsonObject { ["method"] = "GET" },
            items: [NodeItem.From(new JsonObject())],
            httpHandler: new StubHandler(HttpStatusCode.OK, "{}"));

        // PerItemNodeType istisnayi yutmaz; motor yakalar. Node seviyesinde firlatilir.
        var act = async () => await node.ExecuteAsync(ctx);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
