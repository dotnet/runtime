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
            // non-public (e.g. internal) setter is therefore not treated as SetOnInit: the generator does not set it
            // (matching the reflection binder, which does not bind non-public members by default), and the member keeps
            // its default value.
            SetOnInit = setterIsPublic && (property.IsRequired || isInitOnly);
            CanSet = setterIsPublic && !isInitOnly;
            // An init-only property can only be assigned at construction time through normal C#. Post-construction the
            // generator sets it through an [UnsafeAccessor] setter (or reflection downlevel), which lets absent config
            // keys preserve the property's default value instead of overwriting it.
            CanSetViaAccessor = setterIsPublic && isInitOnly;
            CanGet = property.GetMethod?.DeclaredAccessibility is Accessibility.Public;
            IsRequired = property.IsRequired;
        }

        public ParameterSpec? MatchingCtorParam { get; set; }

        public bool IsIgnored { get; init; }

        public bool IsStatic { get; }

        public bool SetOnInit { get; }

        public bool IsRequired { get; }

        public override bool CanGet { get; }

        public override bool CanSet { get; }

        /// <summary>
        /// Whether the property has a public init-only setter, so it is assignable post-construction only through an
        /// <c>[UnsafeAccessor]</c> setter (or a reflection fallback downlevel) rather than a direct assignment.
        /// </summary>
        public bool CanSetViaAccessor { get; }

        /// <summary>
        /// The declaring type an accessor for this property must target, when it differs from the type being bound (an
        /// inherited property's setter is declared on a base type, and <c>[UnsafeAccessor]</c> resolves against the exact
        /// type named). <see langword="null"/> when the property is declared on the bound type itself.
        /// </summary>
        public TypeRef? AccessorDeclaringTypeRef { get; init; }

        /// <summary>
        /// Whether the init-only setter accessor for this property can use <c>[UnsafeAccessor]</c> (the framework
        /// supports it and, for a generic declaring type, supports generics). <see langword="false"/> falls back to reflection.
        /// </summary>
        public bool SetterCanUseUnsafeAccessor { get; init; }

        /// <summary>Type-parameter names of the (generic) declaring type when a generic wrapper class is used for the setter (.NET 9+), otherwise <see langword="null"/>.</summary>
        public ImmutableEquatableArray<string>? DeclaringTypeParameterNames { get; init; }
        public string? OpenDeclaringTypeFQN { get; init; }
        public string? OpenPropertyTypeFQN { get; init; }
        public string? DeclaringTypeParameterConstraintClauses { get; init; }
    }
}
