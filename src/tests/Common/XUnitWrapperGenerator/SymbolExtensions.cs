// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace XUnitWrapperGenerator;

internal static class SymbolExtensions
{
    public static IEnumerable<AttributeData> GetAttributesOnSelfAndContainingSymbols(this ISymbol symbol)
    {
        for (ISymbol? containing = symbol; containing is not null; containing = symbol.ContainingSymbol)
        {
            foreach (var attr in symbol.GetAttributes())
            {
                yield return attr;
            }
        }
    }
}
