using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Soenneker.AdaptiveCards.Dtos.Models;
using TUnit.Core;

namespace Soenneker.AdaptiveCards.Util.Tests;

public sealed class AdaptiveCardsUtilTests
{
    [Test]
    public void NestedCardRoundTrips()
    {
        const string json = """
        {"type":"AdaptiveCard","version":"1.5","msteams":{"width":"Full"},"body":[
          {"type":"Container","items":[{"type":"TextBlock","text":"Hello","weight":"bolder","wrap":false,"fallback":"drop"},
            {"type":"Input.Text","id":"name","isRequired":true,"label":"Name"}]},
          {"type":"RichTextBlock","inlines":["Hello",{"type":"TextRun","text":"world"}]}],
          "actions":[{"type":"Action.ShowCard","title":"More","card":{"type":"AdaptiveCard","body":[{"type":"TextBlock","text":"Nested"}]}},
          {"type":"Action.ToggleVisibility","targetElements":["name",{"elementId":"name","isVisible":false}]}]}
        """;
        var util = new AdaptiveCardsUtil();
        AdaptiveCard card = util.Deserialize(json);
        if (card.Body.Value[0].AsVariant3().Items[1].AsVariant10().Id != "name" || !card.Body.Value[0].AsVariant3().Items[1].AsVariant10().IsRequired.Value)
            throw new Exception("Nested elements and inherited input properties were not typed correctly.");
        string indented = util.Serialize(card, writeIndented: true);
        if (!indented.Contains('\n') || !JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(util.Serialize(util.Deserialize(indented)))))
            throw new Exception("Indented card JSON changed during the round trip.");
        if (!JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(util.Serialize(card))))
            throw new Exception("Card JSON changed during the round trip.");
    }

    [Test]
    public void BuildUsesWireNamesAndPreservesFalse()
    {
        var util = new AdaptiveCardsUtil();
        AdaptiveCard card = util.Build("Heading", "Summary", new Dictionary<string, string?> { ["Name"] = "Ada", ["Empty"] = "", ["Missing"] = null });
        card.Body.Value[0].AsVariant16().Wrap = false;
        string json = util.Serialize(card);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement heading = document.RootElement.GetProperty("body")[0];
        if (heading.GetProperty("weight").GetString() != "bolder" || heading.GetProperty("wrap").GetBoolean()
            || heading.TryGetProperty("color", out _))
            throw new Exception("Enums, optional values, or explicit false values were serialized incorrectly.");
        if (util.Deserialize(json).Body.Value[2].AsVariant4().Facts.Count != 1)
            throw new Exception("Facts did not round trip.");
    }

    [Test]
    public void InvalidDiscriminatorsAndMissingRequiredPropertiesFail()
    {
        var util = new AdaptiveCardsUtil();
        foreach (string json in new[]
        {
            "{}", "null", "{\"type\":\"Other\"}",
            "{\"type\":\"AdaptiveCard\",\"body\":[{\"type\":\"Unknown\"}]}",
            "{\"type\":\"AdaptiveCard\",\"body\":[{\"type\":\"TextBlock\"}]}",
            "{\"type\":\"AdaptiveCard\",\"body\":[{\"type\":\"Input.Text\"}]}"
        })
        {
            try { util.Deserialize(json); }
            catch (JsonException) { continue; }
            throw new Exception("Invalid card was accepted: " + json);
        }
    }
    [Test]
    public void TableUsesExplicitSelectorsAndRetainsEmptyHeaders()
    {
        var util = new AdaptiveCardsUtil();
        AdaptiveCardColumn<(string Name, int Count)>[] columns =
        [
            new("Total", row => row.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("Label", row => row.Name),
            new("Empty", _ => null)
        ];
        AdaptiveCard card = util.BuildTable("Report", new[] { ("Ada", 42) }, columns, "Summary");
        using JsonDocument document = JsonDocument.Parse(util.Serialize(card));
        JsonElement root = document.RootElement;
        JsonElement body = root.GetProperty("body");
        if (root.GetProperty("msteams").GetProperty("width").GetString() != "Full" || root.GetProperty("version").GetString() != "1.2")
            throw new Exception("Teams settings are missing.");
        JsonElement headers = body[2].GetProperty("columns");
        JsonElement cells = body[3].GetProperty("columns");
        if (headers[0].GetProperty("items")[0].GetProperty("text").GetString() != "Total"
            || cells[0].GetProperty("items")[0].GetProperty("text").GetString() != "42"
            || cells[1].GetProperty("items")[0].GetProperty("text").GetString() != "Ada"
            || cells[2].GetProperty("items")[0].GetProperty("text").GetString() != "")
            throw new Exception("Explicit column ordering or cell formatting was lost.");

        AdaptiveCard empty = util.BuildTable("Empty", Array.Empty<(string, int)>(), columns);
        if (empty.Body.Value[0].AsVariant16().Text != "Empty" || empty.Body.Value[1].AsVariant2().Columns.Value.Count != 3)
            throw new Exception("Empty table lost its heading or columns.");
    }

    [Test]
    public void BuildIncludesExceptionAndTruncatesWithoutSplittingSurrogates()
    {
        var util = new AdaptiveCardsUtil();
        string large = new string('a', 4999) + "😀" + new string('界', 10000);
        AdaptiveCard card = util.Build("Failure", exception: new InvalidOperationException("Details"), additionalBody: large);
        using JsonDocument document = JsonDocument.Parse(util.Serialize(card));
        JsonElement body = document.RootElement.GetProperty("body");
        if (!body[1].GetProperty("text").GetString()!.Contains("Details") || body[2].GetProperty("text").GetString() != new string('a', 4999))
            throw new Exception("Exception text or Unicode truncation was incorrect.");
        if (body[3].GetProperty("text").GetString() != Environment.MachineName || !body[3].GetProperty("isSubtle").GetBoolean())
            throw new Exception("Diagnostic footer is missing.");
        string timestamp = body[4].GetProperty("text").GetString()!;
        if (!timestamp.EndsWith(" EST") && !timestamp.EndsWith(" EDT"))
            throw new Exception("Timestamp is not labeled with Eastern time.");
    }

    [Test]
    public void TableRejectsMissingColumns()
    {
        var util = new AdaptiveCardsUtil();
        try { util.BuildTable("Report", Array.Empty<int>(), Array.Empty<AdaptiveCardColumn<int>>()); }
        catch (ArgumentException) { return; }
        throw new Exception("A table without columns was accepted.");
    }
}
