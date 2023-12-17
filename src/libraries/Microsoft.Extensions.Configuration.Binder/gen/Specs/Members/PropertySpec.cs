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
            SetOnInit = setterIsPublic && (property.IsRequired || isInitOnly);
            CanSet = setterIsPublic && !isInitOnly;
            CanGet = property.GetMethod?.DeclaredAccessibility is Accessibility.Public;
        }

        public ParameterSpec? MatchingCtorParam { get; set; }

        public bool IsStatic { get; }

        public bool SetOnInit { get; }

        public override bool CanGet { get; }

        public override bool CanSet { get; }
    }
}
