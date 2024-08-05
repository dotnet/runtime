// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed record ObjectSpec : ComplexTypeSpec
    {
        public ObjectSpec(
            INamedTypeSymbol type,
            ObjectInstantiationStrategy instantiationStrategy,
            ImmutableEquatableArray<PropertySpec>? properties,
            ImmutableEquatableArray<ParameterSpec>? constructorParameters,
            string? initExceptionMessage) : base(type)
        {
            InstantiationStrategy = instantiationStrategy;
            Properties = properties;
            ConstructorParameters = constructorParameters;
            InitExceptionMessage = initExceptionMessage;
        }

        public ObjectInstantiationStrategy InstantiationStrategy { get; }

        public ImmutableEquatableArray<PropertySpec>? Properties { get; }

        public ImmutableEquatableArray<ParameterSpec>? ConstructorParameters { get; }

        public string? InitExceptionMessage { get; }
    }

    public enum ObjectInstantiationStrategy
    {
        None = 0,
        ParameterlessConstructor = 1,
        ParameterizedConstructor = 2,
    }
}
