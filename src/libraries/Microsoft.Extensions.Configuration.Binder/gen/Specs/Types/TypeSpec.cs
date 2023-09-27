// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    [DebuggerDisplay("Name={DisplayString}, Kind={SpecKind}")]
    public abstract record TypeSpec
    {
        public TypeSpec(ITypeSymbol type)
        {
            TypeRef = new TypeRef(type);
            EffectiveTypeRef = TypeRef; // Overriden by NullableSpec.
            (Namespace, DisplayString, Name) = type.GetTypeName();
            IdentifierCompatibleSubstring = type.ToIdentifierCompatibleSubstring();
            IsValueType = type.IsValueType;
        }

        public TypeRef TypeRef { get; }

        public TypeRef EffectiveTypeRef { get; protected init; }

        public string Name { get; }

        public string DisplayString { get; }

        public string IdentifierCompatibleSubstring { get; }

        public string? Namespace { get; }

        public bool IsValueType { get; }
    }

    public abstract record ComplexTypeSpec : TypeSpec
    {
        protected ComplexTypeSpec(ITypeSymbol type) : base(type) { }
    }

    internal sealed record NullableSpec : TypeSpec
    {
        public NullableSpec(ITypeSymbol type, TypeRef underlyingTypeRef) : base(type) =>
            EffectiveTypeRef = underlyingTypeRef;
    }

    internal sealed record UnsupportedTypeSpec : TypeSpec
    {
        public UnsupportedTypeSpec(ITypeSymbol type) : base(type) { }

        public required NotSupportedReason NotSupportedReason { get; init; }
    }

    public enum NotSupportedReason
    {
        UnknownType = 1,
        MissingPublicInstanceConstructor = 2,
        CollectionNotSupported = 3,
        DictionaryKeyNotSupported = 4,
        ElementTypeNotSupported = 5,
        MultipleParameterizedConstructors = 6,
        MultiDimArraysNotSupported = 7,
        NullableUnderlyingTypeNotSupported = 8,
    }
}
