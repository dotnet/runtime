﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Validates and indexes polymorphic type configuration,
    /// providing derived JsonTypeInfo resolution methods
    /// in both serialization and deserialization scenaria.
    /// </summary>
    internal sealed class PolymorphicTypeResolver
    {
        private readonly JsonTypeInfo _declaringTypeInfo;
        private readonly ConcurrentDictionary<Type, DerivedJsonTypeInfo?> _typeToDiscriminatorId = new();
        private readonly Dictionary<object, DerivedJsonTypeInfo>? _discriminatorIdtoType;

        public PolymorphicTypeResolver(JsonTypeInfo jsonTypeInfo)
        {
            Debug.Assert(jsonTypeInfo.PolymorphismOptions != null);

            JsonPolymorphismOptions polymorphismOptions = jsonTypeInfo.PolymorphismOptions;
            UnknownDerivedTypeHandling = polymorphismOptions.UnknownDerivedTypeHandling;
            IgnoreUnrecognizedTypeDiscriminators = polymorphismOptions.IgnoreUnrecognizedTypeDiscriminators;
            _declaringTypeInfo = jsonTypeInfo;

            if (!IsSupportedPolymorphicBaseType(BaseType))
            {
                ThrowHelper.ThrowInvalidOperationException_TypeDoesNotSupportPolymorphism(BaseType);
            }

            bool containsDerivedTypes = false;
            foreach ((Type derivedType, object? typeDiscriminator) in polymorphismOptions.DerivedTypes)
            {
                Debug.Assert(typeDiscriminator is null or int or string);

                if (!IsSupportedDerivedType(BaseType, derivedType) ||
                    (derivedType.IsAbstract && UnknownDerivedTypeHandling != JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor))
                {
                    ThrowHelper.ThrowInvalidOperationException_DerivedTypeNotSupported(BaseType, derivedType);
                }

                var derivedJsonTypeInfo = new DerivedJsonTypeInfo(derivedType, typeDiscriminator);

                if (!_typeToDiscriminatorId.TryAdd(derivedType, derivedJsonTypeInfo))
                {
                    ThrowHelper.ThrowInvalidOperationException_DerivedTypeIsAlreadySpecified(BaseType, derivedType);
                }

                if (typeDiscriminator is not null)
                {
                    if (!(_discriminatorIdtoType ??= new()).TryAdd(typeDiscriminator, derivedJsonTypeInfo))
                    {
                        ThrowHelper.ThrowInvalidOperationException_TypeDicriminatorIdIsAlreadySpecified(BaseType, typeDiscriminator);
                    }

                    UsesTypeDiscriminators = true;
                }

                containsDerivedTypes = true;
            }

            if (!containsDerivedTypes)
            {
                ThrowHelper.ThrowInvalidOperationException_PolymorphicTypeConfigurationDoesNotSpecifyDerivedTypes(BaseType);
            }

            if (UsesTypeDiscriminators)
            {
                if (!jsonTypeInfo.Converter.CanHaveMetadata)
                {
                    ThrowHelper.ThrowNotSupportedException_BaseConverterDoesNotSupportMetadata(BaseType);
                }

                string propertyName = jsonTypeInfo.PolymorphismOptions.TypeDiscriminatorPropertyName;

                JsonEncodedText jsonEncodedName = propertyName == JsonSerializer.TypePropertyName
                    ? JsonSerializer.s_metadataType
                    : JsonEncodedText.Encode(propertyName, jsonTypeInfo.Options.Encoder);

                // Check if the property name conflicts with other metadata property names
                if ((JsonSerializer.GetMetadataPropertyName(jsonEncodedName.EncodedUtf8Bytes, resolver: null) & ~MetadataPropertyName.Type) != 0)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidCustomTypeDiscriminatorPropertyName();
                }

                TypeDiscriminatorPropertyName = propertyName;
                TypeDiscriminatorPropertyNameUtf8 = jsonEncodedName.EncodedUtf8Bytes.ToArray();
                CustomTypeDiscriminatorPropertyNameJsonEncoded = jsonEncodedName;
            }
        }

        public Type BaseType => _declaringTypeInfo.Type;
        public JsonUnknownDerivedTypeHandling UnknownDerivedTypeHandling { get; }
        public bool UsesTypeDiscriminators { get; }
        public bool IgnoreUnrecognizedTypeDiscriminators { get; }
        public string? TypeDiscriminatorPropertyName { get; }
        public byte[]? TypeDiscriminatorPropertyNameUtf8 { get; }
        public JsonEncodedText? CustomTypeDiscriminatorPropertyNameJsonEncoded { get; }

        public bool TryGetDerivedJsonTypeInfo(Type runtimeType, [NotNullWhen(true)] out JsonTypeInfo? jsonTypeInfo, out object? typeDiscriminator)
        {
            Debug.Assert(BaseType.IsAssignableFrom(runtimeType));

            if (!_typeToDiscriminatorId.TryGetValue(runtimeType, out DerivedJsonTypeInfo? result))
            {
                switch (UnknownDerivedTypeHandling)
                {
                    case JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor:
                        // Calculate (and cache the result) of the nearest ancestor for given runtime type.
                        // A `null` result denotes no matching ancestor type, we also cache that.
                        result = CalculateNearestAncestor(runtimeType);
                        _typeToDiscriminatorId[runtimeType] = result;
                        break;
                    case JsonUnknownDerivedTypeHandling.FallBackToBaseType:
                        // Recover the polymorphic contract (i.e. any type discriminators) for the base type, if it exists.
                        _typeToDiscriminatorId.TryGetValue(BaseType, out result);
                        _typeToDiscriminatorId[runtimeType] = result;
                        break;

                    case JsonUnknownDerivedTypeHandling.FailSerialization:
                    default:
                        if (runtimeType != BaseType)
                        {
                            ThrowHelper.ThrowNotSupportedException_RuntimeTypeNotSupported(BaseType, runtimeType);
                        }
                        break;
                }
            }

            if (result is null)
            {
                jsonTypeInfo = null;
                typeDiscriminator = null;
                return false;
            }
            else
            {
                jsonTypeInfo = result.GetJsonTypeInfo(_declaringTypeInfo.Options);
                typeDiscriminator = result.TypeDiscriminator;
                return true;
            }
        }

        public bool TryGetDerivedJsonTypeInfo(object typeDiscriminator, [NotNullWhen(true)] out JsonTypeInfo? jsonTypeInfo)
        {
            Debug.Assert(typeDiscriminator is int or string);
            Debug.Assert(UsesTypeDiscriminators);
            Debug.Assert(_discriminatorIdtoType != null);

            if (_discriminatorIdtoType.TryGetValue(typeDiscriminator, out DerivedJsonTypeInfo? result))
            {
                Debug.Assert(typeDiscriminator.Equals(result.TypeDiscriminator));
                jsonTypeInfo = result.GetJsonTypeInfo(_declaringTypeInfo.Options);
                return true;
            }

            if (!IgnoreUnrecognizedTypeDiscriminators)
            {
                ThrowHelper.ThrowJsonException_UnrecognizedTypeDiscriminator(typeDiscriminator);
            }

            jsonTypeInfo = null;
            return false;
        }

        public static bool IsSupportedPolymorphicBaseType(Type? type) =>
            type != null &&
            (type.IsClass || type.IsInterface) &&
            !type.IsSealed &&
            !type.IsGenericTypeDefinition &&
            !type.IsPointer &&
            type != JsonTypeInfo.ObjectType;

        public static bool IsSupportedDerivedType(Type baseType, Type? derivedType) =>
            baseType.IsAssignableFrom(derivedType) && !derivedType.IsGenericTypeDefinition;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The call to GetInterfaces will cross-reference results with interface types " +
                            "already declared as derived types of the polymorphic base type.")]
        private DerivedJsonTypeInfo? CalculateNearestAncestor(Type type)
        {
            Debug.Assert(!type.IsAbstract);
            Debug.Assert(BaseType.IsAssignableFrom(type));
            Debug.Assert(UnknownDerivedTypeHandling == JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor);

            if (type == BaseType)
            {
                return null;
            }

            DerivedJsonTypeInfo? result = null;

            // First, walk up the class hierarchy for any suported types.
            for (Type? candidate = type.BaseType; BaseType.IsAssignableFrom(candidate); candidate = candidate.BaseType)
            {
                Debug.Assert(candidate != null);

                if (_typeToDiscriminatorId.TryGetValue(candidate, out result))
                {
                    break;
                }
            }

            // Interface hierarchies admit the possibility of diamond ambiguities in type discriminators.
            // Examine all interface implementations and identify potential conflicts.
            if (BaseType.IsInterface)
            {
                foreach (Type interfaceTy in type.GetInterfaces())
                {
                    if (interfaceTy != BaseType && BaseType.IsAssignableFrom(interfaceTy) &&
                        _typeToDiscriminatorId.TryGetValue(interfaceTy, out DerivedJsonTypeInfo? interfaceResult) &&
                        interfaceResult is not null)
                    {
                        if (result is null)
                        {
                            result = interfaceResult;
                        }
                        else
                        {
                            ThrowHelper.ThrowNotSupportedException_RuntimeTypeDiamondAmbiguity(BaseType, type, result.DerivedType, interfaceResult.DerivedType);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Lazy JsonTypeInfo result holder for a derived type.
        /// </summary>
        private sealed class DerivedJsonTypeInfo
        {
            private volatile JsonTypeInfo? _jsonTypeInfo;

            public DerivedJsonTypeInfo(Type type, object? typeDiscriminator)
            {
                Debug.Assert(typeDiscriminator is null or int or string);

                DerivedType = type;
                TypeDiscriminator = typeDiscriminator;
            }

            public Type DerivedType { get; }
            public object? TypeDiscriminator { get; }
            public JsonTypeInfo GetJsonTypeInfo(JsonSerializerOptions options)
                => _jsonTypeInfo ??= options.GetTypeInfoCached(DerivedType);
        }
    }
}
