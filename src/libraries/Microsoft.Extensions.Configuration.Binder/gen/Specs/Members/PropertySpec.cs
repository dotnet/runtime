// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed record PropertySpec : MemberSpec
    {
        public PropertySpec(IPropertySymbol property, TypeRef typeRef) : base(property, typeRef)
        {
            IMethodSymbol? setMethod = property.SetMethod;
            bool setterIsPublic = setMethod?.DeclaredAccessibility is Accessibility.Public;
            bool isInitOnly = setMethod?.IsInitOnly is true;

            IsStatic = property.IsStatic;
            // Only public setters are considered here, consistent with CanSet. A required or init-only property with a
            // non-public (e.g. internal) setter is therefore not treated as SetOnInit, so the generator does not emit
            // an object initializer for it. A required property in that shape cannot be constructed by the generator;
            // the parser detects it and reports a diagnostic instead of emitting code that would fail with CS9035.
            SetOnInit = setterIsPublic && (property.IsRequired || isInitOnly);
            CanSet = setterIsPublic && !isInitOnly;
            CanGet = property.GetMethod?.DeclaredAccessibility is Accessibility.Public;
        }

        public ParameterSpec? MatchingCtorParam { get; set; }

        public bool IsIgnored { get; init; }

        public bool IsStatic { get; }

        public bool SetOnInit { get; }

        public override bool CanGet { get; }

        public override bool CanSet { get; }
    }
}
