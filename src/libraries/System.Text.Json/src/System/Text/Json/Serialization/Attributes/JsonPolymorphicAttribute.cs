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
