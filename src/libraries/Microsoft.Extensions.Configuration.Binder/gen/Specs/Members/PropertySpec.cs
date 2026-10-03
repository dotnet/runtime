// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed record PropertySpec : MemberSpec
    {
        public PropertySpec(IPropertySymbol property, TypeRef typeRef, InitOnlySetterSpec? initOnlySetter = null) : base(property, typeRef)
        {
            IMethodSymbol? setMethod = property.SetMethod;
            bool setterIsPublic = setMethod?.DeclaredAccessibility is Accessibility.Public;
            bool isInitOnly = setMethod?.IsInitOnly is true;

            IsStatic = property.IsStatic;
            SetOnInit = setterIsPublic && (property.IsRequired || isInitOnly);
            CanSet = setterIsPublic && (!isInitOnly || initOnlySetter is not null);
            CanGet = property.GetMethod?.DeclaredAccessibility is Accessibility.Public;
            IsInitOnly = setterIsPublic && isInitOnly;
            InitOnlySetter = initOnlySetter;
        }

        public ParameterSpec? MatchingCtorParam { get; set; }

        public bool IsIgnored { get; init; }

        public bool HasTypeConverter { get; init; }

        public bool HasTypeConverterOnBindableProperty { get; init; }

        public TypeRef? AccessDeclaringType { get; init; }

        public bool IsStatic { get; }
        public bool IsInitOnly { get; }
        public InitOnlySetterSpec? InitOnlySetter { get; }

        public bool MatchingCtorParameterTypeMatches { get; set; }

        public bool SetOnInit { get; }

        public override bool CanGet { get; }

        public override bool CanSet { get; }
    }

    public sealed record InitOnlySetterSpec(UnsafeAccessorTypeSpec DeclaringType, TypeRef PropertyType, string PropertyName, string OpenPropertyType);

    public sealed record UnsafeAccessorTypeSpec(TypeRef Type, string OpenType, string? TypeParameters, string? TypeArguments, string? Constraints);

    public sealed record ConstructorAccessorSpec(
        UnsafeAccessorTypeSpec DeclaringType,
        ImmutableEquatableArray<TypeRef> ParameterTypes,
        ImmutableEquatableArray<string> OpenParameterTypes);
}
