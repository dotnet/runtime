// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        internal sealed partial class Parser
        {
            private readonly struct TypeParseInfo
            {
                public ITypeSymbol TypeSymbol { get; private init; }

                /// <summary>
                /// <see cref="System.Type.FullName"/> like rendering of the symbol name.
                /// </summary>
                public string FullName { get; private init; }
                public MethodsToGen BindingOverload { get; private init; }
                public BinderInvocation BinderInvocation { get; private init; }
                public ContainingTypeDiagnosticInfo? ContainingTypeDiagnosticInfo { get; private init; }

                public static TypeParseInfo Create(ITypeSymbol typeSymbol, MethodsToGen overload, BinderInvocation invocation, ContainingTypeDiagnosticInfo? containingTypeDiagInfo = null) =>
                    new TypeParseInfo
                    {
                        TypeSymbol = typeSymbol,
                        FullName = typeSymbol.GetFullName(),
                        BindingOverload = overload,
                        BinderInvocation = invocation,
                        ContainingTypeDiagnosticInfo = containingTypeDiagInfo,
                    };

                public TypeParseInfo ToTransitiveTypeParseInfo(ITypeSymbol memberType, DiagnosticDescriptor? diagDescriptor = null, string? memberName = null)
                {
                    ContainingTypeDiagnosticInfo? diagnosticInfo = diagDescriptor is null
                        ? null
                        : new()
                        {
                            FullName = FullName,
                            Descriptor = diagDescriptor,
                            MemberName = memberName,
                            ContainingTypeInfo = ContainingTypeDiagnosticInfo,
                        };

                    return Create(memberType, BindingOverload, BinderInvocation, diagnosticInfo);
                }
            }

            private sealed class ContainingTypeDiagnosticInfo
            {
                /// <summary>
                /// <see cref="System.Type.FullName"/> like rendering of the symbol name.
                /// </summary>
                public required string FullName { get; init; }
                public required string? MemberName { get; init; }
                public required DiagnosticDescriptor Descriptor { get; init; }
                public required ContainingTypeDiagnosticInfo? ContainingTypeInfo { get; init; }
            }
        }
    }

    internal static class ParserExtensions
    {
        private static readonly SymbolDisplayFormat s_identifierCompatibleFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.None,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.None);

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

        /// <summary>
        /// Returns unique identifier compatible name based on the fully qualified type name, including fully qualified type arguments.
        /// Names are truncated at 1000 characters.
        /// This leaves 23 characters for custom name prefix.
        /// </summary>
        /// <remarks>
        /// When names are longer than 1000 characters a hash code of the original string is appended to maintain identifier uniqueness
        /// </remarks>
        public static string ToIdentifierCompatibleSubstring(this ITypeSymbol type)
        {
            StringBuilder sb = new StringBuilder(50);
            ToIdentifierCompatibleSubstringBuilder(type, sb);

            sb.Replace(".", "").Replace("[", "").Replace("]", "");

            if (sb.Length > 1000)
            {
                string hash = sb.ToString().GetHashCode().ToString().Replace('-', '_');
                sb.Remove(989, sb.Length - 989).Append(hash);
            }
            return sb.ToString();
        }


        private static void ToIdentifierCompatibleSubstringBuilder(ITypeSymbol type, StringBuilder displaySubstring)
        {
            if (type is IArrayTypeSymbol arrayType)
            {
                int rank = arrayType.Rank;
                string suffix = rank == 1 ? "Array" : $"Array{rank}D"; // Array, Array2D, Array3D, ...
                ToIdentifierCompatibleSubstringBuilder(arrayType.ElementType, displaySubstring);
                displaySubstring.Append(suffix);
            }

            displaySubstring.Append(type.ToDisplayString(s_identifierCompatibleFormat));

            if (type is not INamedTypeSymbol { IsGenericType: true } namedType)
            {
                return;
            }

            if (namedType.GetAllTypeArgumentsInScope() is List<ITypeSymbol> typeArgsInScope)
            {
                foreach (ITypeSymbol genericArg in typeArgsInScope)
                {
                    ToIdentifierCompatibleSubstringBuilder(genericArg, displaySubstring);
                }
            }

            return;
        }

        public static (string DisplayString, string FullName) GetTypeNames(this ITypeSymbol type)
        {
            string? @namespace = type.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace ? containingNamespace.ToDisplayString() : null;
            string displayString = type.ToDisplayString(s_minimalDisplayFormat);
            string fullname = (@namespace is null ? string.Empty : @namespace + ".") + displayString.Replace(".", "+");
            return (displayString, fullname);
        }

        public static string GetFullName(this ITypeSymbol type) => GetTypeNames(type).FullName;
    }
}
