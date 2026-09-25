using System;
using System.Collections.Generic;
using Soenneker.AdaptiveCards.Dtos.Models;

namespace Soenneker.AdaptiveCards.Util.Abstract;

/// <summary>Creates and serializes Adaptive Cards using the schema-generated System.Text.Json models.</summary>
public interface IAdaptiveCardsUtil
{
    /// <summary>Creates an empty card with its schema URI, version, body, and actions initialized.</summary>
    AdaptiveCard Create(string version = "1.5");

    /// <summary>Builds a full-width Teams card with a heading, optional summary, nonempty facts, exception details, additional text, and a diagnostic footer.</summary>
    /// <remarks>Uses schema version 1.2. Exception and additional text blocks over 27 KiB in UTF-8 are truncated to at most 5,000 characters.
    /// This is a per-block safeguard, not a total payload size limit. The footer includes configured Environment and ProjectName,
    /// the machine name, and the current Eastern time.</remarks>
    AdaptiveCard Build(string title, string? summary = null, IReadOnlyDictionary<string, string?>? facts = null,
        Exception? exception = null, string? additionalBody = null);

    /// <summary>Builds a full-width Teams card with explicit, ordered columns and a diagnostic footer, without reflection.</summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="title">The card heading.</param>
    /// <param name="items">Rows to render in order. Empty lists retain the title and column headings.</param>
    /// <param name="columns">At least one column, each supplying a heading and typed text selector. Selectors control formatting; null results produce empty cells.</param>
    /// <param name="summary">Optional text beneath the heading.</param>
    /// <returns>A schema version 1.2 card using column sets for the table layout.</returns>
    AdaptiveCard BuildTable<T>(string title, IReadOnlyList<T> items, IReadOnlyList<AdaptiveCardColumn<T>> columns, string? summary = null);

    /// <summary>Serializes a card, preserving polymorphic elements and host-specific properties.</summary>
    string Serialize(AdaptiveCard card, bool writeIndented = false);

    /// <summary>Deserializes an AdaptiveCard and rejects missing or incorrect root type discriminators.</summary>
    /// <remarks>This does not perform full JSON schema or host capability validation.</remarks>
    AdaptiveCard Deserialize(string json);
}
