// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal static class ParserExtensions
    {
        private static readonly SymbolDisplayFormat s_minimalDisplayFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public static void RegisterCacheEntry<TKey, TValue, TEntry>(this Dictionary<TKey, TValue> cache, TKey key, TEntry entry)
            where TKey : notnull
            where TValue : ICollection<TEntry>, new()
        {
            if (!cache.TryGetValue(key, out TValue? entryCollection))
            {
                cache[key] = entryCollection = new TValue();
            }

            entryCollection.Add(entry);
        }

        public static void Deconstruct(this KeyValuePair<TypeSpec, List<InterceptorLocationInfo>> source, out ComplexTypeSpec Key, out List<InterceptorLocationInfo> Value)
        {
            Key = (ComplexTypeSpec)source.Key;
            Value = source.Value;
        }

        public static string ToMinimalDisplayString(this ITypeSymbol type) => type.ToDisplayString(s_minimalDisplayFormat);

        public static string ToIdentifierCompatibleSubstring(this ITypeSymbol type, string? displayString = null)
        {
            if (type is IArrayTypeSymbol arrayType)
            {
                int rank = arrayType.Rank;
                string suffix = rank == 1 ? "Array" : $"Array{rank}D"; // Array, Array2D, Array3D, ...
                return ToIdentifierCompatibleSubstring(arrayType.ElementType) + suffix;
            }

            displayString ??= type.ToMinimalDisplayString();
            StringBuilder? sb = null;

            foreach (char c in displayString)
            {
                if (c is '[')
                {
                    GetSb().Append("Array");
                }
                else if (c is ',')
                {
                    GetSb().Append("Comma");
                }
                else if (char.IsLetterOrDigit(c))
                {
                    GetSb().Append(c);
                }

                StringBuilder GetSb() => sb ??= new();
            }

            displayString = sb?.ToString() ?? displayString;

            if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
            {
                return displayString;
            }

            if (sb is null)
            {
                (sb = new()).Append(displayString);
            }

            Debug.Assert(sb.Length > 0);

            if (namedType.GetAllTypeArgumentsInScope() is List<ITypeSymbol> typeArgsInScope)
            {
                foreach (ITypeSymbol genericArg in typeArgsInScope)
                {
                    sb.Append(ToIdentifierCompatibleSubstring(genericArg));
                }
            }

            return sb.ToString();
        }
    }
}
