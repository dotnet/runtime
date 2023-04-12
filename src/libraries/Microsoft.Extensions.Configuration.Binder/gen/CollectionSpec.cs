// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal abstract record CollectionSpec : TypeSpec
    {
        public CollectionSpec(ITypeSymbol type) : base(type)
        {
            IsReadOnly = type.IsReadOnly;
            IsInterface = type is INamedTypeSymbol { TypeKind: TypeKind.Interface };
        }

        public required TypeSpec ElementType { get; init; }

        public bool IsReadOnly { get; }

        public bool IsInterface { get; }

        public CollectionSpec? ConcreteType { get; init; }
    }

    internal sealed record ArraySpec : CollectionSpec
    {
        public ArraySpec(ITypeSymbol type) : base(type) { }

        public override TypeSpecKind SpecKind => TypeSpecKind.Array;
    }

    internal sealed record EnumerableSpec : CollectionSpec
    {
        public EnumerableSpec(ITypeSymbol type) : base(type) { }

        public override TypeSpecKind SpecKind => TypeSpecKind.Enumerable;
    }

    internal sealed record DictionarySpec : CollectionSpec
    {
        public DictionarySpec(INamedTypeSymbol type) : base(type) { }

        public override TypeSpecKind SpecKind => TypeSpecKind.Dictionary;

        public required ParsableFromStringTypeSpec KeyType { get; init; }
    }
}
