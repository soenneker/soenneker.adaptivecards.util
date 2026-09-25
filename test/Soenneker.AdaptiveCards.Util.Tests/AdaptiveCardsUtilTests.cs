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
        if (!JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(util.Serialize(card))))
            throw new Exception("Card JSON changed during the round trip.");
    }

    [Test]
    public void BuildUsesWireNamesAndPreservesFalse()
    {
        var util = new AdaptiveCardsUtil();
        AdaptiveCard card = util.Build("Heading", "Summary", new Dictionary<string, string?> { ["Name"] = null });
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
}

