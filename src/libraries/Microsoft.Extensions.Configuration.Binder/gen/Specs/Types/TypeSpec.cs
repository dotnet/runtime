// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    [DebuggerDisplay("Name={DisplayString}, Kind={SpecKind}")]
    internal abstract record TypeSpec
    {
        private static readonly SymbolDisplayFormat s_minimalDisplayFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public TypeSpec(ITypeSymbol type)
        {
            Namespace = type.ContainingNamespace?.ToDisplayString();
            DisplayString = type.ToDisplayString(s_minimalDisplayFormat);
            Name = (Namespace is null ? string.Empty : Namespace + ".") + DisplayString.Replace(".", "+");
            IdentifierCompatibleSubstring = type.ToIdentifierCompatibleSubstring();
            IsValueType = type.IsValueType;
        }

        public string Name { get; }

        public string DisplayString { get; }

        public string IdentifierCompatibleSubstring { get; }

        public string? Namespace { get; }

        public bool IsValueType { get; }

        public abstract TypeSpecKind SpecKind { get; }

        public abstract bool CanBindTo { get; }

        public abstract bool CanInstantiate { get; }

        public abstract TypeSpec EffectiveType { get; }
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
