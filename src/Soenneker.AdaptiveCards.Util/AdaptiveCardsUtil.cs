using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.AdaptiveCards.Dtos;
using Soenneker.AdaptiveCards.Dtos.Models;
using Soenneker.AdaptiveCards.Util.Abstract;
using Soenneker.Utils.Json;

namespace Soenneker.AdaptiveCards.Util;

public sealed class AdaptiveCardsUtil : IAdaptiveCardsUtil
{
    private static readonly SchemaJsonContext _compact = new(new JsonSerializerOptions());
    private static readonly SchemaJsonContext _indented = new(new JsonSerializerOptions { WriteIndented = true });

    private readonly ILogger<AdaptiveCardsUtil>? _logger;
    private readonly string? _environment;
    private readonly string? _projectName;
    private static readonly JsonElement _msTeams = CreateTeamsProperties();
    private const int _maxTextBytes = 27 * 1024;
    private const int _truncateChars = 5000;

    public AdaptiveCardsUtil(ILogger<AdaptiveCardsUtil>? logger = null, IConfiguration? config = null)
    {
        _logger = logger;
        _environment = config?["Environment"];
        _projectName = config?["ProjectName"];
    }

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

    public AdaptiveCard Build(string title, string? summary = null, IReadOnlyDictionary<string, string?>? facts = null,
        Exception? exception = null, string? additionalBody = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        AdaptiveCard card = CreateTeamsCard();
        AddHeader(card, title, summary);

        if (facts is { Count: > 0 })
        {
            var values = new List<Fact>(facts.Count);
            foreach ((string name, string? value) in facts)
            {
                if (!string.IsNullOrEmpty(value))
                    values.Add(new Fact { Title = name, Value = value });
            }

            if (values.Count > 0)
                card.Body.Value.Add(ImplementationsOfElement.FromVariant4(new FactSet { Type = FactSetType.FactSet, Facts = values }));
        }

        AddTextBlock(card, exception?.ToString());
        AddTextBlock(card, additionalBody);
        AddFooter(card);
        return card;
    }

    public AdaptiveCard BuildTable<T>(string title, IReadOnlyList<T> items, IReadOnlyList<AdaptiveCardColumn<T>> columns,
        string? summary = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(columns);
        if (columns.Count == 0)
            throw new ArgumentException("At least one column is required.", nameof(columns));

        foreach (AdaptiveCardColumn<T> column in columns)
            ArgumentNullException.ThrowIfNull(column);

        AdaptiveCard card = CreateTeamsCard();
        AddHeader(card, title, summary);

        var header = new ColumnSet
        {
            Type = ColumnSetType.ColumnSet,
            Spacing = Spacing.FromVariant1(SpacingVariant1.ExtraLarge),
            Columns = new List<Column>(columns.Count)
        };
        foreach (AdaptiveCardColumn<T> column in columns)
            header.Columns.Value.Add(CreateColumn(column.Title, true));
        card.Body.Value.Add(ImplementationsOfElement.FromVariant2(header));

        foreach (T item in items)
        {
            var row = new ColumnSet { Type = ColumnSetType.ColumnSet, Columns = new List<Column>(columns.Count) };
            foreach (AdaptiveCardColumn<T> column in columns)
                row.Columns.Value.Add(CreateColumn(column.ValueSelector(item) ?? string.Empty, false));
            card.Body.Value.Add(ImplementationsOfElement.FromVariant2(row));
        }

        AddFooter(card);
        return card;
    }

    private static JsonElement CreateTeamsProperties()
    {
        using JsonDocument document = JsonDocument.Parse("""{"width":"Full"}""");
        return document.RootElement.Clone();
    }

    private AdaptiveCard CreateTeamsCard()
    {
        AdaptiveCard card = Create("1.2");
        card.AdditionalProperties = new Dictionary<string, JsonElement> { ["msteams"] = _msTeams };
        return card;
    }

    private static TextBlock CreateText(string text, FontSizeVariant1 size = FontSizeVariant1.Small) => new()
    {
        Type = TextBlockType.TextBlock,
        Text = text,
        Size = FontSize.FromVariant1(size),
        Wrap = true
    };

    private static void AddHeader(AdaptiveCard card, string title, string? summary)
    {
        TextBlock heading = CreateText(title, FontSizeVariant1.Medium);
        heading.Weight = FontWeight.FromVariant1(FontWeightVariant1.Bolder);
        card.Body.Value.Add(ImplementationsOfElement.FromVariant16(heading));
        if (!string.IsNullOrEmpty(summary))
            card.Body.Value.Add(ImplementationsOfElement.FromVariant16(CreateText(summary)));
    }

    private static Column CreateColumn(string text, bool header)
    {
        var block = new TextBlock { Type = TextBlockType.TextBlock, Text = text, Wrap = true };
        if (header)
            block.Weight = FontWeight.FromVariant1(FontWeightVariant1.Bolder);
        return new Column { Type = ColumnType.Column, Items = new List<ImplementationsOfElement> { ImplementationsOfElement.FromVariant16(block) } };
    }

    private void AddTextBlock(AdaptiveCard card, string? content)
    {
        if (string.IsNullOrEmpty(content))
            return;

        if (content.Length > _maxTextBytes / 4 && Encoding.UTF8.GetByteCount(content) > _maxTextBytes)
        {
            _logger?.LogError("Truncating large text block for MS Teams. Content length: {Length} chars", content.Length);
            int length = Math.Min(_truncateChars, content.Length);
            if (char.IsHighSurrogate(content[length - 1]) && length < content.Length && char.IsLowSurrogate(content[length]))
                length--;
            content = content[..length];
        }

        card.Body.Value.Add(ImplementationsOfElement.FromVariant16(CreateText(content)));
    }

    private void AddFooter(AdaptiveCard card)
    {
        AddFooterText(card, _environment);
        AddFooterText(card, _projectName);
        try
        {
            AddFooterText(card, Environment.MachineName);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving machine name");
        }

        TimeZoneInfo eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        DateTimeOffset timestamp = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, eastern);
        AddFooterText(card, timestamp.ToString("MM/dd/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture) + (eastern.IsDaylightSavingTime(timestamp) ? " EDT" : " EST"));
    }

    private static void AddFooterText(AdaptiveCard card, string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;
        TextBlock block = CreateText(text);
        block.IsSubtle = true;
        block.Spacing = Spacing.FromVariant1(SpacingVariant1.Small);
        card.Body.Value.Add(ImplementationsOfElement.FromVariant16(block));
    }

    public string Serialize(AdaptiveCard card, bool writeIndented = false)
    {
        ArgumentNullException.ThrowIfNull(card);
        return JsonUtil.Serialize(card, (writeIndented ? _indented : _compact).AdaptiveCard);
    }

    public AdaptiveCard Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("type", out JsonElement type)
            || type.ValueKind != JsonValueKind.String || type.GetString() != "AdaptiveCard")
            throw new JsonException("The root type must be AdaptiveCard.");
        return JsonUtil.Deserialize(json, _compact.AdaptiveCard)!;
    }
}
