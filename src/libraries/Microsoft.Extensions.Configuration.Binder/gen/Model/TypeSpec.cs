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
            IsValueType = type.IsValueType;
            Namespace = type.ContainingNamespace?.ToDisplayString();
            FullyQualifiedDisplayString = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            MinimalDisplayString = type.ToDisplayString(s_minimalDisplayFormat);
            Name = Namespace + "." + MinimalDisplayString.Replace(".", "+");
        }

        public string Name { get; }

        public string FullyQualifiedDisplayString { get; }

        public string MinimalDisplayString { get; }

        public string? Namespace { get; }

        public bool IsValueType { get; }

        public abstract TypeSpecKind SpecKind { get; }

        public virtual InitializationStrategy InitializationStrategy { get; set; }

        public virtual string? InitExceptionMessage { get; set; }

        public virtual bool CanInitialize => true;

        /// <summary>
        /// Location in the input compilation we picked up a call to Bind, Get, or Configure.
        /// </summary>
        public required Location? Location { get; init; }

        protected bool CanInitCompexType => InitializationStrategy is not InitializationStrategy.None && InitExceptionMessage is null;
    }

    internal enum TypeSpecKind
    {
        Unknown = 0,
        ParsableFromString = 1,
        Object = 2,
        Enumerable = 3,
        Dictionary = 4,
        IConfigurationSection = 5,
        Nullable = 6,
    }
}
