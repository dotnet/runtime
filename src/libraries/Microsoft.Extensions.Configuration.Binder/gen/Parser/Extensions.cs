﻿// Licensed to the .NET Foundation under one or more agreements.
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
                public string TypeName { get; private init; }
                public MethodsToGen BindingOverload { get; private init; }
                public BinderInvocation BinderInvocation { get; private init; }
                public ContainingTypeDiagnosticInfo? ContainingTypeDiagnosticInfo { get; private init; }

                public static TypeParseInfo Create(ITypeSymbol typeSymbol, MethodsToGen overload, BinderInvocation invocation, ContainingTypeDiagnosticInfo? containingTypeDiagInfo = null) =>
                    new TypeParseInfo
                    {
                        TypeSymbol = typeSymbol,
                        TypeName = typeSymbol.GetName(),
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
                            TypeName = TypeName,
                            Descriptor = diagDescriptor,
                            MemberName = memberName,
                            ContainingTypeInfo = ContainingTypeDiagnosticInfo,
                        };

                    return Create(memberType, BindingOverload, BinderInvocation, diagnosticInfo);
                }
            }

            private sealed class ContainingTypeDiagnosticInfo
            {
                public required string TypeName { get; init; }
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
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.None,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

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

        public static string ToIdentifierCompatibleSubstring(this ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol arrayType)
            {
                int rank = arrayType.Rank;
                string suffix = rank == 1 ? "Array" : $"Array{rank}D"; // Array, Array2D, Array3D, ...
                return ToIdentifierCompatibleSubstring(arrayType.ElementType) + suffix;
            }

            string displayString = type.ContainingType is null
                ? type.Name
                : type.ToDisplayString(s_identifierCompatibleFormat).Replace(".", string.Empty);

            if (type is not INamedTypeSymbol { IsGenericType: true } namedType)
            {
                return displayString;
            }

            StringBuilder sb = new(displayString);

            if (namedType.GetAllTypeArgumentsInScope() is List<ITypeSymbol> typeArgsInScope)
            {
                foreach (ITypeSymbol genericArg in typeArgsInScope)
                {
                    sb.Append(ToIdentifierCompatibleSubstring(genericArg));
                }
            }

            return sb.ToString();
        }

        public static (string? Namespace, string DisplayString, string Name) GetTypeName(this ITypeSymbol type)
        {
            string? @namespace = type.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace ? containingNamespace.ToDisplayString() : null;
            string displayString = type.ToDisplayString(s_minimalDisplayFormat);
            string name = (@namespace is null ? string.Empty : @namespace + ".") + displayString.Replace(".", "+");
            return (@namespace, displayString, name);
        }

        public static string GetName(this ITypeSymbol type) => GetTypeName(type).Name;
    }
}
