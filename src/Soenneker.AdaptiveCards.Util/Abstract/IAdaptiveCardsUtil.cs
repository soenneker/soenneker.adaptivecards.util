using System.Collections.Generic;
using Soenneker.AdaptiveCards.Dtos.Models;

namespace Soenneker.AdaptiveCards.Util.Abstract;

/// <summary>Creates and serializes Adaptive Cards using the schema-generated System.Text.Json models.</summary>
public interface IAdaptiveCardsUtil
{
    /// <summary>Creates an empty card with its schema URI, version, body, and actions initialized.</summary>
    AdaptiveCard Create(string version = "1.5");

    /// <summary>Builds a card with a heading, optional summary, and optional name/value facts.</summary>
    AdaptiveCard Build(string title, string? summary = null, IReadOnlyDictionary<string, string?>? facts = null);

    /// <summary>Serializes a card, preserving polymorphic elements and host-specific properties.</summary>
    string Serialize(AdaptiveCard card, bool writeIndented = false);

    /// <summary>Deserializes an AdaptiveCard and rejects missing or incorrect root type discriminators.</summary>
    /// <remarks>This does not perform full JSON schema or host capability validation.</remarks>
    AdaptiveCard Deserialize(string json);
}

