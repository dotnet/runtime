// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics;

namespace SourceGenerators
{
    internal static class TypeModelHelper
    {
        private static readonly SymbolDisplayFormat s_minimalDisplayFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public static string ToIdentifierCompatibleSubstring(this ITypeSymbol type, bool useUniqueName)
        {
            if (type is IArrayTypeSymbol arrayType)
            {
                int rank = arrayType.Rank;
                string suffix = rank == 1 ? "Array" : $"Array{rank}D"; // Array, Array2D, Array3D, ...
                return ToIdentifierCompatibleSubstring(arrayType.ElementType, useUniqueName) + suffix;
            }

            StringBuilder? sb = null;
            string? symbolName = null;

            if (useUniqueName)
            {
                string uniqueDisplayString = type.ToMinimalDisplayString();
                PopulateIdentifierCompatibleSubstring(sb = new(), uniqueDisplayString);
            }
            else
            {
                symbolName = type.Name;
            }

#if DEBUG
            bool usingUniqueName = (symbolName is null || sb is not null) && useUniqueName;
            bool usingSymbolName = (symbolName is not null && sb is null) && !useUniqueName;
            bool configuredNameCorrectly = (usingUniqueName && !usingSymbolName) || (!usingUniqueName && usingSymbolName);
            Debug.Assert(configuredNameCorrectly);
#endif

            if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
            {
                return symbolName ?? sb!.ToString();
            }

            if (sb is null)
            {
                (sb = new()).Append(symbolName!);
            }

            Debug.Assert(sb.Length > 0);

            if (namedType.GetAllTypeArgumentsInScope() is List<ITypeSymbol> typeArgsInScope)
            {
                foreach (ITypeSymbol genericArg in typeArgsInScope)
                {
                    sb.Append(ToIdentifierCompatibleSubstring(genericArg, useUniqueName));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Type name, prefixed with containing type names if it is nested (e.g. ContainingType.NestedType).
        /// </summary>
        public static string ToMinimalDisplayString(this ITypeSymbol type) => type.ToDisplayString(s_minimalDisplayFormat);

        public static List<ITypeSymbol>? GetAllTypeArgumentsInScope(this INamedTypeSymbol type)
        {
            if (!type.IsGenericType)
            {
                return null;
            }

            List<ITypeSymbol>? args = null;
            TraverseContainingTypes(type);
            return args;

            void TraverseContainingTypes(INamedTypeSymbol current)
            {
                if (current.ContainingType is INamedTypeSymbol parent)
                {
                    TraverseContainingTypes(parent);
                }

                if (!current.TypeArguments.IsEmpty)
                {
                    (args ??= new()).AddRange(current.TypeArguments);
                }
            }
        }

        private static void PopulateIdentifierCompatibleSubstring(StringBuilder sb, string input)
        {
            foreach (char c in input)
            {
                if (c is '[')
                {
                    sb.Append("Array");
                }
                else if (c is ',')
                {
                    sb.Append("Comma");
                }
                else if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
            }
        }
    }
}
