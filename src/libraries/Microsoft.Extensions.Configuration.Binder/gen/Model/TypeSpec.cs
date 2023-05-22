// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal abstract record TypeSpec
    {
        private static readonly SymbolDisplayFormat s_minimalDisplayFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public TypeSpec(ITypeSymbol type)
        {
            FullyQualifiedDisplayString = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            MinimalDisplayString = type.ToDisplayString(s_minimalDisplayFormat);
            Namespace = type.ContainingNamespace?.ToDisplayString();
            IsValueType = type.IsValueType;
        }

        public string FullyQualifiedDisplayString { get; }

        public string MinimalDisplayString { get; }

        public string? Namespace { get; }

        public bool IsValueType { get; }

        public abstract TypeSpecKind SpecKind { get; }

        public virtual ConstructionStrategy ConstructionStrategy { get; init; }

        /// <summary>
        /// Where in the input compilation we picked up a call to Bind, Get, or Configure.
        /// </summary>
        public required Location? Location { get; init; }
    }

    internal enum TypeSpecKind
    {
        Unknown = 0,
        ParsableFromString = 1,
        Object = 2,
        Array = 3,
        Enumerable = 4,
        Dictionary = 5,
        IConfigurationSection = 6,
        Nullable = 7,
    }
}
