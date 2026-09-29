// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// When placed on a type, indicates that the type should be serialized polymorphically.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class JsonPolymorphicAttribute : JsonAttribute
    {
        /// <summary>
        /// Gets or sets a custom type discriminator property name for the polymorphic type.
        /// Uses the default '$type' property name if left unset.
        /// </summary>
        public string? TypeDiscriminatorPropertyName { get; set; }

        /// <summary>
        /// Gets or sets the behavior when serializing an undeclared derived runtime type.
        /// </summary>
        public JsonUnknownDerivedTypeHandling UnknownDerivedTypeHandling { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether derived type registrations should be inferred
        /// from compiler-provided metadata for a closed type hierarchy.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to infer derived type registrations; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// Setting this property overrides <see cref="JsonSerializerOptions.InferClosedTypePolymorphism"/>
        /// for the annotated type, so an explicit <see langword="false"/> suppresses inference even when it is
        /// enabled globally. When the property is left unset the globally configured value is used.
        /// If the annotated type declares one or more <see cref="JsonDerivedTypeAttribute"/> registrations,
        /// inference is skipped for that type and only the explicitly registered derived types are used.
        /// Closed derived types are expanded recursively and only terminal derived types are registered.
        /// Polymorphism configuration declared on derived types applies to their respective contracts and does
        /// not affect inference for the annotated type.
        /// </remarks>
        public bool InferClosedTypePolymorphism
        {
            get => _inferClosedTypePolymorphism ?? false;
            set => _inferClosedTypePolymorphism = value;
        }

        /// <summary>
        /// Gets the explicitly configured <see cref="InferClosedTypePolymorphism"/> value,
        /// or <see langword="null"/> if the property has not been set.
        /// </summary>
        internal bool? InferClosedTypePolymorphismOrNull => _inferClosedTypePolymorphism;

        private bool? _inferClosedTypePolymorphism;

        /// <summary>
        /// When set to <see langword="true"/>, instructs the deserializer to ignore any
        /// unrecognized type discriminator id's and reverts to the contract of the base type.
        /// Otherwise, it will fail the deserialization.
        /// </summary>
        public bool IgnoreUnrecognizedTypeDiscriminators { get; set; }

        /// <summary>
        /// Gets or sets the type of a <see cref="JsonTypeClassifierFactory"/> implementation
        /// used to classify JSON payloads during deserialization instead of relying on
        /// the standard type discriminator property.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When set, the classifier is invoked before any discriminator-based resolution.
        /// The classifier receives a <see cref="Utf8JsonReader"/> positioned at the start of
        /// the JSON object and returns the resolved <see cref="Type"/>. Returning
        /// <see langword="null"/> fails deserialization.
        /// </para>
        /// <para>
        /// The specified type must derive from <see cref="JsonTypeClassifierFactory"/>
        /// and have a public parameterless constructor.
        /// </para>
        /// </remarks>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public Type? TypeClassifier { get; set; }
    }
}
