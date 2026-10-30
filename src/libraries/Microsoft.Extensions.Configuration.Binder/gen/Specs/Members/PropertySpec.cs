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
            // An override that only re-declares one accessor doesn't have a symbol for the other
            // (e.g. a get-only override of a read-write virtual property has a null SetMethod), even
            // though the base accessor is still inherited and invocable through normal member access.
            // Walk the override chain to find the accessor that will actually be used at the call site.
            IMethodSymbol? getMethod = GetEffectiveAccessor(property, getter: true);
            IMethodSymbol? setMethod = GetEffectiveAccessor(property, getter: false);
            bool setterIsPublic = setMethod?.DeclaredAccessibility is Accessibility.Public;
            bool isInitOnly = setMethod?.IsInitOnly is true;

            IsStatic = property.IsStatic;
            SetOnInit = setterIsPublic && (property.IsRequired || isInitOnly);
            CanSet = setterIsPublic && !isInitOnly;
            CanGet = getMethod?.DeclaredAccessibility is Accessibility.Public;
        }

        private static IMethodSymbol? GetEffectiveAccessor(IPropertySymbol property, bool getter)
        {
            for (IPropertySymbol? current = property; current is not null; current = current.OverriddenProperty)
            {
                IMethodSymbol? accessor = getter ? current.GetMethod : current.SetMethod;
                if (accessor is not null)
                {
                    return accessor;
                }
            }

            return null;
        }

        public ParameterSpec? MatchingCtorParam { get; set; }

        public bool IsIgnored { get; init; }

        public bool IsStatic { get; }

        public bool SetOnInit { get; }

        public override bool CanGet { get; }

        public override bool CanSet { get; }
    }
}
