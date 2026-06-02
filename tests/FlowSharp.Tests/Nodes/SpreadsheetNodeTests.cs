using System.Text;
using System.Text.Json.Nodes;
using ClosedXML.Excel;
using FluentAssertions;
using FlowSharp.Application.Nodes;
using FlowSharp.Nodes.Data;
using FlowSharp.Tests.Fixtures;
using Xunit;

namespace FlowSharp.Tests.Nodes;

public class SpreadsheetNodeTests
{
    /// <summary>Designer'in yukledigi { fileName, content(base64) } yapisini uretir.</summary>
    private static FakeNodeExecutionContext Ctx(string fileName, byte[] content, bool hasHeader = true, string? sheet = null)
    {
        var file = new JsonObject
        {
            ["fileName"] = fileName,
            ["content"] = Convert.ToBase64String(content)
        }.ToJsonString();

        var parameters = new JsonObject { ["file"] = file, ["hasHeader"] = hasHeader };
        if (sheet is not null) parameters["sheet"] = sheet;
        return new FakeNodeExecutionContext(parameters);
    }

    private static byte[] Xlsx(Action<IXLWorksheet> build)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");
        build(ws);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    [Fact]
    public async Task Reads_excel_rows_with_header()
    {
        var bytes = Xlsx(ws =>
        {
            ws.Cell(1, 1).Value = "name"; ws.Cell(1, 2).Value = "age";
            ws.Cell(2, 1).Value = "Ada";  ws.Cell(2, 2).Value = "30";
            ws.Cell(3, 1).Value = "Eray"; ws.Cell(3, 2).Value = "25";
        });

        var result = await new SpreadsheetNode().ExecuteAsync(Ctx("data.xlsx", bytes));

        result.Succeeded.Should().BeTrue();
        result.PrimaryItems.Should().HaveCount(2);
        result.PrimaryItems[0].Json["name"]!.GetValue<string>().Should().Be("Ada");
        result.PrimaryItems[1].Json["age"]!.GetValue<string>().Should().Be("25");
    }

    [Fact]
    public async Task Reads_excel_without_header_uses_column_names()
    {
        var bytes = Xlsx(ws =>
        {
            ws.Cell(1, 1).Value = "x"; ws.Cell(1, 2).Value = "y";
        });

        var result = await new SpreadsheetNode().ExecuteAsync(Ctx("data.xlsx", bytes, hasHeader: false));

        result.PrimaryItems.Should().ContainSingle();
        result.PrimaryItems[0].Json["column1"]!.GetValue<string>().Should().Be("x");
        result.PrimaryItems[0].Json["column2"]!.GetValue<string>().Should().Be("y");
    }

    [Fact]
    public async Task Reads_csv_with_header()
    {
        var csv = Encoding.UTF8.GetBytes("name,age\nAda,30\nEray,25\n");
        var result = await new SpreadsheetNode().ExecuteAsync(Ctx("data.csv", csv));

        result.PrimaryItems.Should().HaveCount(2);
        result.PrimaryItems[0].Json["name"]!.GetValue<string>().Should().Be("Ada");
    }

    [Fact]
    public async Task Reads_csv_without_header()
    {
        var csv = Encoding.UTF8.GetBytes("a,b\nc,d\n");
        var result = await new SpreadsheetNode().ExecuteAsync(Ctx("data.csv", csv, hasHeader: false));

        result.PrimaryItems.Should().HaveCount(2);
        result.PrimaryItems[0].Json["column1"]!.GetValue<string>().Should().Be("a");
    }

    [Fact]
    public async Task Fails_when_no_file()
    {
        var result = await new SpreadsheetNode().ExecuteAsync(new FakeNodeExecutionContext());
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Fails_on_invalid_base64_content()
    {
        var parameters = new JsonObject
        {
            ["file"] = new JsonObject { ["fileName"] = "x.csv", ["content"] = "***not-base64***" }.ToJsonString()
        };
        var result = await new SpreadsheetNode().ExecuteAsync(new FakeNodeExecutionContext(parameters));
        result.Succeeded.Should().BeFalse();
    }
}
