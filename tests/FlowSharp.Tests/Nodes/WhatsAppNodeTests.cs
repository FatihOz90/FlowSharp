using System.Text.Json.Nodes;
using FluentAssertions;
using FlowSharp.Application.Nodes;
using FlowSharp.Domain.Nodes;
using FlowSharp.Nodes.Communication.WhatsApp;
using FlowSharp.Tests.Fixtures;
using Xunit;

namespace FlowSharp.Tests.Nodes;

public class WhatsAppNodeTests
{
    [Fact]
    public void Message_definition_is_a_communication_action_with_whatsapp_credential()
    {
        var node = new WhatsAppMessageNode();

        node.Definition.Key.Should().Be("whatsapp.message");
        node.Definition.Category.Should().Be(NodeCategory.Communication);
        node.Definition.Kind.Should().Be(NodeKind.Action);
        node.Definition.CredentialKeys.Should().Contain(WhatsAppMessageNode.CredentialTypeKey);

        var schema = node.CredentialSchemas.Should().ContainSingle().Subject;
        schema.Type.Should().Be(WhatsAppMessageNode.CredentialTypeKey);
        schema.Fields.Select(f => f.Key).Should().Contain(["accessToken", "phoneNumberId"]);
    }

    [Fact]
    public void Trigger_definition_is_a_trigger()
    {
        var node = new WhatsAppTriggerNode();
        node.Definition.Key.Should().Be("whatsapp.trigger");
        node.Definition.Kind.Should().Be(NodeKind.Trigger);
        node.Definition.Category.Should().Be(NodeCategory.Trigger);
    }

    [Theory]
    [InlineData("messages,statuses", new[] { "messages", "statuses" })]
    [InlineData("statuses", new[] { "statuses" })]
    [InlineData("messages", new[] { "messages" })]
    [InlineData("", new[] { "messages" })] // bos secim -> varsayilan tek port
    [InlineData("statuses,messages", new[] { "messages", "statuses" })] // kanonik sira korunur
    public void Trigger_outputs_one_port_per_selected_event(string events, string[] expectedPorts)
    {
        var node = new WhatsAppTriggerNode();
        var ports = node.GetOutputs(new Dictionary<string, string> { ["events"] = events });
        ports.Select(p => p.Name).Should().Equal(expectedPorts);
    }

    [Fact]
    public async Task Trigger_routes_messages_and_statuses_to_separate_ports_per_item()
    {
        var node = new WhatsAppTriggerNode();
        var payload = new JsonObject
        {
            ["source"] = "whatsapp",
            ["whatsapp"] = new JsonObject
            {
                ["messages"] = new JsonArray
                {
                    new JsonObject { ["from"] = "9055", ["text"] = "selam" },
                    new JsonObject { ["from"] = "9056", ["text"] = "merhaba" }
                },
                ["statuses"] = new JsonArray { new JsonObject { ["status"] = "delivered" } }
            }
        };
        var ctx = new FakeNodeExecutionContext(
            parameters: new JsonObject { ["events"] = "messages,statuses" },
            items: [NodeItem.From(payload)]);

        var result = await node.ExecuteAsync(ctx);

        result.Outputs.Should().HaveCount(2);
        // Port 0 = messages: item basina bir oge
        result.Outputs[0].Should().HaveCount(2);
        result.Outputs[0][0].Json["text"]!.GetValue<string>().Should().Be("selam");
        // Port 1 = statuses
        result.Outputs[1].Should().ContainSingle();
        result.Outputs[1][0].Json["status"]!.GetValue<string>().Should().Be("delivered");
    }

    [Fact]
    public async Task Trigger_leaves_messages_port_empty_for_status_only_event()
    {
        var node = new WhatsAppTriggerNode();
        var payload = new JsonObject
        {
            ["source"] = "whatsapp",
            ["whatsapp"] = new JsonObject
            {
                ["messages"] = new JsonArray(),
                ["statuses"] = new JsonArray { new JsonObject { ["status"] = "read" } }
            }
        };
        var ctx = new FakeNodeExecutionContext(
            parameters: new JsonObject { ["events"] = "messages,statuses" },
            items: [NodeItem.From(payload)]);

        var result = await node.ExecuteAsync(ctx);

        result.Outputs[0].Should().BeEmpty();   // messages portu bos -> downstream atlanir
        result.Outputs[1].Should().ContainSingle();
    }

    [Fact]
    public void BuildPayload_text_message()
    {
        var ctx = new FakeNodeExecutionContext(parameters: new JsonObject
        {
            ["to"] = "905551112233",
            ["messageType"] = "text",
            ["text"] = "merhaba",
            ["previewUrl"] = "true"
        });

        var payload = WhatsAppMessageNode.BuildPayload(ctx, 0);

        payload["type"]!.GetValue<string>().Should().Be("text");
        payload["to"]!.GetValue<string>().Should().Be("905551112233");
        payload["text"]!["body"]!.GetValue<string>().Should().Be("merhaba");
        payload["text"]!["preview_url"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public void BuildPayload_template_with_body_params()
    {
        var ctx = new FakeNodeExecutionContext(parameters: new JsonObject
        {
            ["to"] = "9055",
            ["messageType"] = "template",
            ["templateName"] = "welcome",
            ["languageCode"] = "tr",
            ["bodyParams"] = "[\"Ali\",\"42\"]"
        });

        var payload = WhatsAppMessageNode.BuildPayload(ctx, 0);

        payload["type"]!.GetValue<string>().Should().Be("template");
        payload["template"]!["name"]!.GetValue<string>().Should().Be("welcome");
        payload["template"]!["language"]!["code"]!.GetValue<string>().Should().Be("tr");
        var parameters = payload["template"]!["components"]![0]!["parameters"]!.AsArray();
        parameters.Should().HaveCount(2);
        parameters[0]!["text"]!.GetValue<string>().Should().Be("Ali");
    }

    [Fact]
    public void BuildPayload_interactive_buttons()
    {
        var ctx = new FakeNodeExecutionContext(parameters: new JsonObject
        {
            ["to"] = "9055",
            ["messageType"] = "interactiveButtons",
            ["bodyText"] = "Onayliyor musun?",
            ["buttons"] = "[{\"id\":\"yes\",\"title\":\"Evet\"},{\"id\":\"no\",\"title\":\"Hayir\"}]"
        });

        var payload = WhatsAppMessageNode.BuildPayload(ctx, 0);

        payload["type"]!.GetValue<string>().Should().Be("interactive");
        payload["interactive"]!["type"]!.GetValue<string>().Should().Be("button");
        var buttons = payload["interactive"]!["action"]!["buttons"]!.AsArray();
        buttons.Should().HaveCount(2);
        buttons[0]!["reply"]!["id"]!.GetValue<string>().Should().Be("yes");
    }

    [Fact]
    public void BuildPayload_document_media()
    {
        var ctx = new FakeNodeExecutionContext(parameters: new JsonObject
        {
            ["to"] = "9055",
            ["messageType"] = "document",
            ["mediaLink"] = "https://x/y.pdf",
            ["filename"] = "fatura.pdf",
            ["caption"] = "Faturaniz"
        });

        var payload = WhatsAppMessageNode.BuildPayload(ctx, 0);

        payload["type"]!.GetValue<string>().Should().Be("document");
        payload["document"]!["link"]!.GetValue<string>().Should().Be("https://x/y.pdf");
        payload["document"]!["filename"]!.GetValue<string>().Should().Be("fatura.pdf");
    }

    [Fact]
    public void BuildPayload_invalid_json_throws()
    {
        var ctx = new FakeNodeExecutionContext(parameters: new JsonObject
        {
            ["to"] = "9055",
            ["messageType"] = "interactiveButtons",
            ["bodyText"] = "x",
            ["buttons"] = "{not json"
        });

        var act = () => WhatsAppMessageNode.BuildPayload(ctx, 0);
        act.Should().Throw<InvalidOperationException>();
    }
}
