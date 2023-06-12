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

            bool setterIsPublic = property.SetMethod?.DeclaredAccessibility is Accessibility.Public;
            IsInitOnly = property.SetMethod?.IsInitOnly == true;
            IsRequired = property.IsRequired;
            SetOnInit = setterIsPublic && (IsInitOnly || IsRequired);
            CanSet = setterIsPublic && !IsInitOnly;
            CanGet = property.GetMethod?.DeclaredAccessibility is Accessibility.Public;
        }

        public required TypeSpec Type { get; init; }

        public ParameterSpec? MatchingCtorParam { get; set; }

        public string Name { get; }

        public bool IsStatic { get; }

        public bool IsRequired { get; }

        public bool IsInitOnly { get; }

        public bool SetOnInit { get; }

        public bool CanGet { get; }

        public bool CanSet { get; }

        public required string ConfigurationKeyName { get; init; }

        public bool ShouldBind() =>
            (CanGet || CanSet) &&
            !(!CanSet && (Type as CollectionSpec)?.InitializationStrategy is InitializationStrategy.ParameterizedConstructor);
    }
}
