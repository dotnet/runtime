// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Maps attribute-based polymorphism configuration to IJsonPolymorphicTypeConfiguration
    /// </summary>
    internal sealed class AttributePolymorphicTypeConfiguration : IJsonPolymorphicTypeConfiguration
    {
#pragma warning disable CA2252 // This API requires opting into preview features
        private readonly JsonPolymorphicAttribute? _polymorphicTypeAttribute;
        private readonly IEnumerable<JsonDerivedTypeAttribute> _derivedTypeAttributes;

        private AttributePolymorphicTypeConfiguration(Type baseType, JsonPolymorphicAttribute? polymorphicTypeAttribute, IEnumerable<JsonDerivedTypeAttribute> derivedTypeAttributes)
        {
            BaseType = baseType;
            _polymorphicTypeAttribute = polymorphicTypeAttribute;
            _derivedTypeAttributes = derivedTypeAttributes;
        }

        public static AttributePolymorphicTypeConfiguration? Create(Type baseType)
        {
            JsonPolymorphicAttribute? polymorphicTypeAttribute = baseType.GetCustomAttribute<JsonPolymorphicAttribute>(inherit: false);
            IEnumerable<JsonDerivedTypeAttribute> derivedTypeAttributes = baseType.GetCustomAttributes<JsonDerivedTypeAttribute>(inherit: false);

            if (polymorphicTypeAttribute is null && IsEmpty(derivedTypeAttributes))
            {
                return null;
            }

            return new AttributePolymorphicTypeConfiguration(baseType, polymorphicTypeAttribute, derivedTypeAttributes);

            static bool IsEmpty<T>(IEnumerable<T> source)
            {
                using IEnumerator<T> enumerator = source.GetEnumerator();
                return !enumerator.MoveNext();
            }
        }

        public Type BaseType { get; }

        public string? TypeDiscriminatorPropertyName => _polymorphicTypeAttribute?.TypeDiscriminatorPropertyName;

        public JsonUnknownDerivedTypeHandling UnknownDerivedTypeHandling => _polymorphicTypeAttribute?.UnknownDerivedTypeHandling ?? default;

        public bool IgnoreUnrecognizedTypeDiscriminators => _polymorphicTypeAttribute?.IgnoreUnrecognizedTypeDiscriminators ?? false;

        public IEnumerable<(Type DerivedType, object? TypeDiscriminator)> GetSupportedDerivedTypes()
        {
            foreach (JsonDerivedTypeAttribute attribute in _derivedTypeAttributes)
            {
                yield return (attribute.DerivedType, attribute.TypeDiscriminator);
            }
        }
#pragma warning restore CA2252 // This API requires opting into preview features
    }
}
