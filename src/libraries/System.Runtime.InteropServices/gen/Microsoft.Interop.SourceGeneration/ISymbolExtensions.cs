// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
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

        /// <summary>
        /// Gets the fully qualified metadata name for a given symbol.
        /// </summary>
        /// <param name="symbol">The input <see cref="ITypeSymbol"/> instance.</param>
        public static string GetFullyQualifiedMetadataName(this ITypeSymbol symbol)
        {
            StringBuilder builder = new();

            static void BuildFrom(ISymbol? symbol, StringBuilder builder)
            {
                switch (symbol)
                {
                    // Namespaces that are nested also append a leading '.'
                    case INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: false }:
                        BuildFrom(symbol.ContainingNamespace, builder);
                        builder.Append('.');
                        builder.Append(symbol.MetadataName);
                        break;

                    // Other namespaces (ie. the one right before global) skip the leading '.'
                    case INamespaceSymbol { IsGlobalNamespace: false }:
                        builder.Append(symbol.MetadataName);
                        break;

                    // Types with no namespace just have their metadata name directly written
                    case ITypeSymbol { ContainingSymbol: INamespaceSymbol { IsGlobalNamespace: true } }:
                        builder.Append(symbol.MetadataName);
                        break;

                    // Types with a containing non-global namespace also append a leading '.'
                    case ITypeSymbol { ContainingSymbol: INamespaceSymbol namespaceSymbol }:
                        BuildFrom(namespaceSymbol, builder);
                        builder.Append('.');
                        builder.Append(symbol.MetadataName);
                        break;

                    // Nested types append a leading '+'
                    case ITypeSymbol { ContainingSymbol: ITypeSymbol typeSymbol }:
                        BuildFrom(typeSymbol, builder);
                        builder.Append('+');
                        builder.Append(symbol.MetadataName);
                        break;
                    default:
                        break;
                }
            }

            BuildFrom(symbol, builder);
            return builder.ToString();
        }
    }
}
