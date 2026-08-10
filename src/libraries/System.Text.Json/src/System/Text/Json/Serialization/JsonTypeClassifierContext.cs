// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Provides immutable metadata to a <see cref="JsonTypeClassifierFactory"/> when
    /// creating a <see cref="JsonTypeClassifier"/> delegate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The context carries a <see cref="Kind"/> value indicating whether the classifier
    /// is being created for a union type or a polymorphic type. Exactly one of the two
    /// candidate lists is populated for any given context.
    /// </para>
    /// <para>
    /// Instances are created internally by the serialization infrastructure. Users
    /// interact with the context through a <see cref="JsonTypeClassifierFactory"/>
    /// implementation.
    /// </para>
    /// </remarks>
    public sealed class JsonTypeClassifierContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonTypeClassifierContext"/> class.
        /// </summary>
        /// <param name="kind">The type of classifier metadata being configured.</param>
        /// <param name="declaringType">The type being configured for classification.</param>
        /// <param name="unionCases">The union cases of the declaring type, or an empty list.</param>
        /// <param name="derivedTypes">The derived types of the declaring type, or an empty list.</param>
        /// <param name="typeDiscriminatorPropertyName">The JSON property name used for type discrimination, or <see langword="null"/>.</param>
        internal JsonTypeClassifierContext(
            JsonTypeClassifierKind kind,
            Type declaringType,
            IReadOnlyList<JsonUnionCaseInfo> unionCases,
            IReadOnlyList<JsonDerivedType> derivedTypes,
            string? typeDiscriminatorPropertyName)
        {
            Kind = kind;
            DeclaringType = declaringType;
            UnionCases = unionCases;
            DerivedTypes = derivedTypes;
            TypeDiscriminatorPropertyName = typeDiscriminatorPropertyName;
        }

        /// <summary>
        /// Gets the type of classifier metadata being configured.
        /// </summary>
        public JsonTypeClassifierKind Kind { get; }

        /// <summary>
        /// Gets the type being configured for classification.
        /// </summary>
        /// <remarks>
        /// For polymorphic types, this is the base class (e.g., <c>Animal</c>).
        /// For union types, this is the union type (e.g., <c>IntOrString</c>).
        /// </remarks>
        public Type DeclaringType { get; }

        /// <summary>
        /// Gets the union cases of <see cref="DeclaringType"/>.
        /// </summary>
        /// <remarks>
        /// Non-empty when <see cref="DeclaringType"/> is configured as a union type. The
        /// list mirrors <see cref="JsonTypeInfo.UnionCases"/> on the resolved
        /// <see cref="JsonTypeInfo"/>.
        /// </remarks>
        public IReadOnlyList<JsonUnionCaseInfo> UnionCases { get; }

        /// <summary>
        /// Gets the derived types of <see cref="DeclaringType"/>.
        /// </summary>
        /// <remarks>
        /// Non-empty when <see cref="DeclaringType"/> is configured as a polymorphic
        /// type. The list mirrors
        /// <see cref="Metadata.JsonPolymorphismOptions.DerivedTypes"/>; each entry may
        /// carry a <see cref="JsonDerivedType.TypeDiscriminator"/> string or integer.
        /// </remarks>
        public IReadOnlyList<JsonDerivedType> DerivedTypes { get; }

        /// <summary>
        /// Gets the JSON property name used for type discrimination (e.g., <c>"$type"</c>, <c>"kind"</c>).
        /// </summary>
        /// <remarks>
        /// Populated from <see cref="Metadata.JsonPolymorphismOptions.TypeDiscriminatorPropertyName"/>
        /// for polymorphic types. <see langword="null"/> for union types (unions don't use
        /// discriminator properties by default).
        /// </remarks>
        public string? TypeDiscriminatorPropertyName { get; }
    }
}
