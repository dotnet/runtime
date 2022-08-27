// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Defines polymorphic configuration for a specified base type.
    /// </summary>
    public class JsonPolymorphismOptions
    {
        private DerivedTypeList? _derivedTypes;
        private bool _ignoreUnrecognizedTypeDiscriminators;
        private JsonUnknownDerivedTypeHandling _unknownDerivedTypeHandling;
        private string? _typeDiscriminatorPropertyName;

        /// <summary>
        /// Gets the list of derived types supported in the current polymorphic type configuration.
        /// </summary>
        public IList<JsonDerivedType> DerivedTypes => _derivedTypes ??= new(this);

        /// <summary>
        /// When set to <see langword="true"/>, instructs the serializer to ignore any
        /// unrecognized type discriminator id's and reverts to the contract of the base type.
        /// Otherwise, it will fail the deserialization.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The parent <see cref="JsonTypeInfo"/> instance has been locked for further modification.
        /// </exception>
        public bool IgnoreUnrecognizedTypeDiscriminators
        {
            get => _ignoreUnrecognizedTypeDiscriminators;
            set
            {
                VerifyMutable();
                _ignoreUnrecognizedTypeDiscriminators = value;
            }
        }

        /// <summary>
        /// Gets or sets the behavior when serializing an undeclared derived runtime type.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The parent <see cref="JsonTypeInfo"/> instance has been locked for further modification.
        /// </exception>
        public JsonUnknownDerivedTypeHandling UnknownDerivedTypeHandling
        {
            get => _unknownDerivedTypeHandling;
            set
            {
                VerifyMutable();
                _unknownDerivedTypeHandling = value;
            }
        }

        /// <summary>
        /// Gets or sets a custom type discriminator property name for the polymorhic type.
        /// Uses the default '$type' property name if left unset.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The parent <see cref="JsonTypeInfo"/> instance has been locked for further modification.
        /// </exception>
        [AllowNull]
        public string TypeDiscriminatorPropertyName
        {
            get => _typeDiscriminatorPropertyName ?? JsonSerializer.TypePropertyName;
            set
            {
                VerifyMutable();
                _typeDiscriminatorPropertyName = value;
            }
        }

        private void VerifyMutable() => DeclaringTypeInfo?.VerifyMutable();

        internal JsonTypeInfo? DeclaringTypeInfo { get; set; }

        private sealed class DerivedTypeList : ConfigurationList<JsonDerivedType>
        {
            private readonly JsonPolymorphismOptions _parent;

            public DerivedTypeList(JsonPolymorphismOptions parent)
            {
                _parent = parent;
            }

            protected override bool IsImmutable => _parent.DeclaringTypeInfo?.IsConfigured == true;
            protected override void VerifyMutable() => _parent.DeclaringTypeInfo?.VerifyMutable();
        }

        internal static JsonPolymorphismOptions? CreateFromAttributeDeclarations(Type baseType)
        {
            JsonPolymorphismOptions? options = null;

            if (baseType.GetCustomAttribute<JsonPolymorphicAttribute>(inherit: false) is JsonPolymorphicAttribute polymorphicAttribute)
            {
                options = new()
                {
                    IgnoreUnrecognizedTypeDiscriminators = polymorphicAttribute.IgnoreUnrecognizedTypeDiscriminators,
                    UnknownDerivedTypeHandling = polymorphicAttribute.UnknownDerivedTypeHandling,
                    TypeDiscriminatorPropertyName = polymorphicAttribute.TypeDiscriminatorPropertyName,
                };
            }

            foreach (JsonDerivedTypeAttribute attr in baseType.GetCustomAttributes<JsonDerivedTypeAttribute>(inherit: false))
            {
                (options ??= new()).DerivedTypes.Add(new JsonDerivedType(attr.DerivedType, attr.TypeDiscriminator));
            }

            return options;
        }
    }
}
