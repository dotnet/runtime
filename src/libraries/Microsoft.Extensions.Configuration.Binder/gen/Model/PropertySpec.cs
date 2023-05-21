// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record PropertySpec
    {
        public PropertySpec(IPropertySymbol property)
        {
            Name = property.Name;
            IsStatic = property.IsStatic;
            CanGet = property.GetMethod is IMethodSymbol { DeclaredAccessibility: Accessibility.Public, IsInitOnly: false };
            CanSet = property.SetMethod is IMethodSymbol { DeclaredAccessibility: Accessibility.Public, IsInitOnly: false };
        }

        public string Name { get; }

        public bool IsStatic { get; }

        public bool CanGet { get; }

        public bool CanSet { get; }

        public required TypeSpec? Type { get; init; }

        public required string ConfigurationKeyName { get; init; }

        public bool ShouldBind() =>
            (CanGet || CanSet) &&
            Type is not null &&
            !(!CanSet && (Type as CollectionSpec)?.ConstructionStrategy is ConstructionStrategy.ParameterizedConstructor);
    }
}
