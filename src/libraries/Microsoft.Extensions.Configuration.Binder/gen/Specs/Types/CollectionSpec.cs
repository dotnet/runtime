// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal abstract record CollectionSpec : ComplexTypeSpec
    {
        protected CollectionSpec(ITypeSymbol type) : base(type) { }

        public required TypeRef ElementTypeRef { get; init; }

    }

    internal abstract record CollectionWithCtorInitSpec : CollectionSpec
    {
        protected CollectionWithCtorInitSpec(ITypeSymbol type) : base(type) { }

        public required CollectionInstantiationStrategy InstantiationStrategy { get; init; }

        public required CollectionInstantiationConcreteType InstantiationConcreteType { get; init; }

        public required CollectionPopulationCastType PopulationCastType { get; init; }
    }

    internal sealed record ArraySpec : CollectionSpec
    {
        public ArraySpec(ITypeSymbol type) : base(type) { }
    }

    internal sealed record EnumerableSpec : CollectionWithCtorInitSpec
    {
        public EnumerableSpec(ITypeSymbol type) : base(type) { }
    }

    internal sealed record DictionarySpec : CollectionWithCtorInitSpec
    {
        public DictionarySpec(INamedTypeSymbol type) : base(type) { }

        public required TypeRef KeyTypeRef { get; init; }
    }

    internal enum CollectionInstantiationStrategy
    {
        NotApplicable = 0,
        ParameterlessConstructor = 1,
        CopyConstructor = 2,
        LinqToDictionary = 3,
    }

    internal enum CollectionInstantiationConcreteType
    {
        Self = 0,
        Dictionary = 1,
        List = 2,
        HashSet = 3,
    }

    internal enum CollectionPopulationCastType
    {
        NotApplicable = 0,
        IDictionary = 1,
        ICollection = 2,
        ISet = 3,
    }
}
