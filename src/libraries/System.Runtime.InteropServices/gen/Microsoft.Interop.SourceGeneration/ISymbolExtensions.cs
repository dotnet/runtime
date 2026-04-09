// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public static class ISymbolExtensions
    {
        /// <summary>
        /// Returns true if <paramref name="symbol"/> is as accessible or more accessible than <paramref name="accessibility"/>
        /// </summary>
        public static bool IsAccessibleFromFileScopedClass(this INamedTypeSymbol symbol, [NotNullWhen(false)] out string? details)
        {
            // a higher enum value is more accessible
            if (symbol.DeclaredAccessibility - Accessibility.Internal < 0)
            {
                details = string.Format(SR.TypeAccessibilityDetails, symbol.ToDisplayString(), symbol.DeclaredAccessibility.ToString().ToLowerInvariant());
                return false;
            }
            for (ISymbol current = symbol.ContainingSymbol; current is INamedTypeSymbol currentType; current = currentType.ContainingSymbol)
            {
                // a higher enum value is more accessible
                if (current.DeclaredAccessibility - Accessibility.Internal < 0)
                {
                    details = string.Format(SR.ContainingTypeAccessibilityDetails, current.ToDisplayString(), current.DeclaredAccessibility.ToString().ToLowerInvariant());
                    return false;
                }
            }
            details = null;
            return true;
        }
    }
}
