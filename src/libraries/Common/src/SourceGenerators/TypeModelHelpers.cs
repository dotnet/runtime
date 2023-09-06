// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace SourceGenerators
{
    internal static class TypeModelHelpers
    {
        private static readonly SymbolDisplayFormat s_minimalDisplayFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public static string ToIdentifierCompatibleSubstring(this ITypeSymbol type, bool useUniqueName = false)
        {
            if (type is IArrayTypeSymbol arrayType)
            {
                int rank = arrayType.Rank;
                string suffix = rank == 1 ? "Array" : $"Array{rank}D"; // Array, Array2D, Array3D, ...
                return ToIdentifierCompatibleSubstring(arrayType.ElementType) + suffix;
            }

            string name = GetName(type);

            if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
            {
                return name;
            }

            StringBuilder sb = new();

            sb.Append(name);

            foreach (ITypeSymbol genericArg in namedType.GetAllTypeArgumentsInScope())
            {
                sb.Append(ToIdentifierCompatibleSubstring(genericArg));
            }

            return sb.ToString();

            string GetName(ITypeSymbol type) => useUniqueName
                ? ToIdentifierCompatibleSubstring(type.ToMinimalDisplayString())
                : type.Name;
        }

        /// <summary>
        /// Type name, prefixed with containing type names if it is nested (e.g. ContainingType.NestedType).
        /// </summary>
        /// <returns></returns>
        public static string ToMinimalDisplayString(this ITypeSymbol type) => type.ToDisplayString(s_minimalDisplayFormat);

        private static ITypeSymbol[] GetAllTypeArgumentsInScope(this INamedTypeSymbol type)
        {
            if (!type.IsGenericType)
            {
                return Array.Empty<ITypeSymbol>();
            }

            var args = new List<ITypeSymbol>();
            TraverseContainingTypes(type);
            return args.ToArray();

            void TraverseContainingTypes(INamedTypeSymbol current)
            {
                if (current.ContainingType is INamedTypeSymbol parent)
                {
                    TraverseContainingTypes(parent);
                }

                args.AddRange(current.TypeArguments);
            }
        }

        private static string ToIdentifierCompatibleSubstring(string input)
        {
            StringBuilder sb = new();

            foreach (char c in input)
            {
                if (c is '[')
                {
                    sb.Append("Arr");
                }
                else if (c is ']')
                {
                    sb.Append("ay");
                }
                else if (c is not (',' or ' ' or '.' or '<' or '>' or '_'))
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
