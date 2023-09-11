// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal abstract record CollectionSpec : ComplexTypeSpec
    {
        public CollectionSpec(ITypeSymbol type) : base(type) { }

        public sealed override bool CanInstantiate => TypeToInstantiate?.CanInstantiate ?? InstantiationStrategy is not InstantiationStrategy.None;

        public required TypeSpec ElementType { get; init; }

        public required CollectionPopulationStrategy PopulationStrategy { get; init; }

        public required CollectionSpec? TypeToInstantiate { get; init; }

        public required CollectionSpec? PopulationCastType { get; init; }
    }

    internal sealed record EnumerableSpec : CollectionSpec
    {
        public EnumerableSpec(ITypeSymbol type) : base(type) { }

        public override TypeSpecKind SpecKind => TypeSpecKind.Enumerable;

        public override bool HasBindableMembers => PopulationStrategy is not CollectionPopulationStrategy.Unknown && ElementType.CanBindTo;
    }

    internal sealed record DictionarySpec : CollectionSpec
    {
        public DictionarySpec(INamedTypeSymbol type) : base(type) { }

        public override TypeSpecKind SpecKind => TypeSpecKind.Dictionary;

        public override bool HasBindableMembers => PopulationStrategy is not CollectionPopulationStrategy.Unknown;

        public required ParsableFromStringSpec KeyType { get; init; }
    }

    internal enum CollectionPopulationStrategy
    {
        Unknown = 0,
        Add = 1,
        Cast_Then_Add = 2,
    }
}
