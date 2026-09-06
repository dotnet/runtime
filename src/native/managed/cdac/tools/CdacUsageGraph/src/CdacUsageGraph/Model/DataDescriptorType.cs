// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace CdacUsageGraph.Model;

/// <summary>
/// Immutable description of one cDAC <c>IData&lt;TSelf&gt;</c> class: its Roslyn symbol, cDAC
/// names.
/// </summary>
internal sealed class DataDescriptorType
{
    internal DataDescriptorType(
        INamedTypeSymbol symbol,
        IReadOnlyList<string> names)
    {
        Symbol = symbol;
        Names = names;
    }

    public INamedTypeSymbol Symbol { get; }

    /// <summary>
    /// The primary cDAC name, used in reports. This is the first name declared by
    /// <c>CdacType</c>, or the C# type name when no attribute supplies names.
    /// </summary>
    public string Name => Names[0];

    /// <summary>
    /// Ordered cDAC names usable for layout lookup. The C# type name is included as a fallback.
    /// No special handling of the <c>DataType</c> enum is required.
    /// </summary>
    public IReadOnlyList<string> Names { get; }
}
