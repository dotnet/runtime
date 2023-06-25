// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Extensions.Options.Generators
{
    internal static class ParserUtilities
    {
        internal static AttributeData? GetSymbolAttributeAnnotationOrDefault(ISymbol? attribute, ISymbol symbol)
        {
            if (attribute is null)
            {
                return null;
            }

            var attrs = symbol.GetAttributes();
            foreach (var item in attrs)
            {
                if (SymbolEqualityComparer.Default.Equals(attribute, item.AttributeClass) && item.AttributeConstructor != null)
                {
                    return item;
                }
            }

            return null;
        }

        internal static bool PropertyHasModifier(ISymbol property, SyntaxKind modifierToSearch, CancellationToken token)
            => property
                .DeclaringSyntaxReferences
                .Any(x =>
                    x.GetSyntax(token) is PropertyDeclarationSyntax syntax &&
                    syntax.Modifiers.Any(m => m.IsKind(modifierToSearch)));

        internal static Location? GetLocation(this ISymbol symbol)
        {
            if (symbol is null)
            {
                return null;
            }

            return symbol.Locations.IsDefaultOrEmpty
                ? null
                : symbol.Locations[0];
        }

        internal static bool IsBaseOrIdentity(ITypeSymbol source, ITypeSymbol dest, Compilation comp)
        {
            var conversion = comp.ClassifyConversion(source, dest);
            return conversion.IsIdentity || (conversion.IsReference && conversion.IsImplicit);
        }

        internal static bool ImplementsInterface(this ITypeSymbol type, ITypeSymbol interfaceType)
        {
            foreach (var iface in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(interfaceType, iface))
                {
                    return true;
                }
            }

            return false;
        }

        // Check if parameter has either simplified (i.e. "int?") or explicit (Nullable<int>) nullable type declaration:
        internal static bool IsNullableOfT(this ITypeSymbol type)
            => type.SpecialType == SpecialType.System_Nullable_T || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
    }
}
