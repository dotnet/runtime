// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal abstract record CollectionSpec : TypeSpec
    {
        public CollectionSpec(ITypeSymbol type) : base(type) { }

        public required TypeSpec ElementType { get; init; }

        public CollectionSpec? ConcreteType { get; set; }

        public CollectionSpec? PopulationCastType { get; set; }

        public required CollectionPopulationStrategy PopulationStrategy { get; init; }

        public required string? ToEnumerableMethodCall { get; init; }
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

    internal enum CollectionPopulationStrategy
    {
        Unknown,
        Array,
        Add,
        Cast_Then_Add,
    }
}
