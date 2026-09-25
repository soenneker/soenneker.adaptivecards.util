using System;

namespace Soenneker.AdaptiveCards.Util;

/// <summary>Defines a table column without discovering properties at runtime.</summary>
/// <typeparam name="T">The row type.</typeparam>
public sealed class AdaptiveCardColumn<T>
{
    /// <summary>The heading displayed for this column.</summary>
    public string Title { get; }

    /// <summary>Selects and formats the cell text. Null results render as empty cells.</summary>
    public Func<T, string?> ValueSelector { get; }

    /// <summary>Creates a column with an explicit heading and a typed cell selector.</summary>
    /// <param name="title">The nonempty column heading.</param>
    /// <param name="valueSelector">A delegate that selects and formats each row's cell text.</param>
    public AdaptiveCardColumn(string title, Func<T, string?> valueSelector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(valueSelector);
        Title = title;
        ValueSelector = valueSelector;
    }
}
