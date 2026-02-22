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
        /// Creates an empty <see cref="JsonPolymorphismOptions"/> instance.
        /// </summary>
        public JsonPolymorphismOptions()
        {
        }

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

            public override bool IsReadOnly => _parent.DeclaringTypeInfo?.IsReadOnly == true;
            protected override void OnCollectionModifying() => _parent.DeclaringTypeInfo?.VerifyMutable();
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

            // Transitively collect derived types declared on types in an unbroken
            // chain of [JsonDerivedType] annotations. E.g. if A declares B and B
            // declares C, C is also included as a derived type of A.
            if (options is { DerivedTypes.Count: > 0 })
            {
                CollectTransitiveDerivedTypes(options, baseType);
            }

            return options;
        }

        /// <summary>
        /// Walks the DerivedTypes list (which may grow during iteration) and appends
        /// any additional derived types found via transitive [JsonDerivedType] declarations
        /// on already-known derived types. Types with [JsonPolymorphic] are treated as
        /// boundary nodes and are not traversed. Uses a set to prevent duplicates and cycles.
        /// </summary>
        private static void CollectTransitiveDerivedTypes(JsonPolymorphismOptions options, Type root)
        {
            HashSet<Type>? seen = null;
            for (int i = 0; i < options.DerivedTypes.Count; i++)
            {
                Type child = options.DerivedTypes[i].DerivedType;

                // Do not walk through types that declare their own [JsonPolymorphic] configuration;
                // those define an independent polymorphic scheme and their [JsonDerivedType] entries
                // belong to that scheme, not to the root type's.
                if (child.GetCustomAttribute<JsonPolymorphicAttribute>(inherit: false) is not null)
                {
                    continue;
                }

                foreach (JsonDerivedTypeAttribute a in child.GetCustomAttributes<JsonDerivedTypeAttribute>(inherit: false))
                {
                    if (seen is null)
                    {
                        // Seed with the root type and all types already declared directly
                        // so that we never re-add a type the author already listed.
                        seen = new HashSet<Type> { root };
                        foreach (JsonDerivedType dt in options.DerivedTypes)
                        {
                            seen.Add(dt.DerivedType);
                        }
                    }

                    if (seen.Add(a.DerivedType))
                    {
                        options.DerivedTypes.Add(new JsonDerivedType(a.DerivedType, a.TypeDiscriminator));
                    }
                }
            }
        }
    }
}
