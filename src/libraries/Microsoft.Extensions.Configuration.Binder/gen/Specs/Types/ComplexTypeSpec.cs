// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal abstract record ComplexTypeSpec : TypeSpec
    {
        public ComplexTypeSpec(ITypeSymbol type) : base(type) { }

        public InstantiationStrategy InstantiationStrategy { get; set; }

        public sealed override bool CanBindTo => CanInstantiate || HasBindableMembers;

        public sealed override TypeSpec EffectiveType => this;

        public abstract bool HasBindableMembers { get; }
    }

    internal enum InstantiationStrategy
    {
        None = 0,
        ParameterlessConstructor = 1,
        ParameterizedConstructor = 2,
        ToEnumerableMethod = 3,
        Array = 4,
    }
}
