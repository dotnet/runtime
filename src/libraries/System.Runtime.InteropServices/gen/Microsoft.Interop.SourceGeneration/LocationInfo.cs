// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Interop
{
    /// <summary>
    /// Contains data required to reconstruct a <see cref="Location"/> without keeping any symbols or references to a <see cref="Compilation"/>
    /// </summary>
    public sealed record LocationInfo
    {
        public required LinePositionSpan LinePositionSpan { get; init; }
        public required string FilePath { get; init; }
        public required TextSpan TextSpan { get; init; }
        public Location AsLocation() => Location.Create(FilePath, TextSpan, LinePositionSpan);

        public static LocationInfo FromSymbol(ISymbol symbol)
        {
            var location = symbol.Locations[0];
            return FromLocation(location);
        }

        public static LocationInfo FromLocation(Location location)
        {
            var lineSpan = location.GetLineSpan().Span;
            var filePath = location.SourceTree.FilePath;
            var textSpan = location.SourceSpan;
            return new LocationInfo()
            {
                LinePositionSpan = lineSpan,
                FilePath = filePath,
                TextSpan = textSpan
            };
        }
    }
}
