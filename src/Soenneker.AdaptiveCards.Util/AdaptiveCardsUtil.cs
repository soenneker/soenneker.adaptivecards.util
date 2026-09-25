using System;
using System.Collections.Generic;
using System.Text.Json;
using Soenneker.AdaptiveCards.Dtos;
using Soenneker.AdaptiveCards.Dtos.Models;
using Soenneker.AdaptiveCards.Util.Abstract;

namespace Soenneker.AdaptiveCards.Util;

public sealed class AdaptiveCardsUtil : IAdaptiveCardsUtil
{
    private static readonly JsonSerializerOptions _compact = new() { TypeInfoResolver = SchemaJsonContext.Default };
    private static readonly JsonSerializerOptions _indented = new() { WriteIndented = true, TypeInfoResolver = SchemaJsonContext.Default };

    public AdaptiveCard Create(string version = "1.5")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        return new AdaptiveCard
        {
            Type = AdaptiveCardType.AdaptiveCard,
            Schema = "https://adaptivecards.io/schemas/adaptive-card.json",
            Version = version,
            Body = new List<ImplementationsOfElement>(),
            Actions = new List<ImplementationsOfAction>()
        };
    }

    public AdaptiveCard Build(string title, string? summary = null, IReadOnlyDictionary<string, string?>? facts = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        AdaptiveCard card = Create();
        card.Body.Value.Add(ImplementationsOfElement.FromVariant16(new TextBlock { Type = TextBlockType.TextBlock, Text = title, Size = FontSize.FromVariant1(FontSizeVariant1.Large), Weight = FontWeight.FromVariant1(FontWeightVariant1.Bolder), Wrap = true }));
        if (!string.IsNullOrWhiteSpace(summary))
            card.Body.Value.Add(ImplementationsOfElement.FromVariant16(new TextBlock { Type = TextBlockType.TextBlock, Text = summary, Wrap = true }));
        if (facts is { Count: > 0 })
        {
            var values = new List<Fact>(facts.Count);
            foreach ((string name, string? value) in facts)
                values.Add(new Fact { Title = name, Value = value ?? string.Empty });
            card.Body.Value.Add(ImplementationsOfElement.FromVariant4(new FactSet { Type = FactSetType.FactSet, Facts = values }));
        }
        return card;
    }

    public string Serialize(AdaptiveCard card, bool writeIndented = false)
    {
        ArgumentNullException.ThrowIfNull(card);
        return JsonSerializer.Serialize(card, writeIndented ? _indented : _compact);
    }

    public AdaptiveCard Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("type", out JsonElement type)
            || type.ValueKind != JsonValueKind.String || type.GetString() != "AdaptiveCard")
            throw new JsonException("The root type must be AdaptiveCard.");
        return root.Deserialize<AdaptiveCard>(_compact)!;
    }
}

