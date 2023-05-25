// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Interop
{
    /// <summary>
    /// Contains data required to reconstruct a <see cref="Location"/> without keeping any symbols or references to a <see cref="Compilation"/>
    /// </summary>
    internal sealed record LocationInfo(
        LinePositionSpan LinePositionSpan,
        string FilePath,
        TextSpan TextSpan)
    {
        public Location AsLocation() => Location.Create(FilePath, TextSpan, LinePositionSpan);

        public static LocationInfo From(ISymbol symbol)
        {
            var location = symbol.Locations[0];
            var lineSpan = location.GetLineSpan().Span;
            var filePath = location.SourceTree.FilePath;
            var textSpan = location.SourceSpan;

            return new LocationInfo(lineSpan, filePath, textSpan);
        }
    }
}
