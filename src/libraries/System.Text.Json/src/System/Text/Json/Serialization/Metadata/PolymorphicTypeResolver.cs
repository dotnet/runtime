// Licensed to the .NET Foundation under one or more agreements.
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
        private readonly ConcurrentDictionary<Type, DerivedJsonTypeInfo?> _typeToDiscriminatorId = new();
        private readonly Dictionary<object, DerivedJsonTypeInfo>? _discriminatorIdtoType;
        private readonly JsonSerializerOptions _options;

        public PolymorphicTypeResolver(JsonSerializerOptions options, JsonPolymorphismOptions polymorphismOptions, Type baseType, bool converterCanHaveMetadata)
        {
            UnknownDerivedTypeHandling = polymorphismOptions.UnknownDerivedTypeHandling;
            IgnoreUnrecognizedTypeDiscriminators = polymorphismOptions.IgnoreUnrecognizedTypeDiscriminators;
            BaseType = baseType;
            _options = options;

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
                if (!converterCanHaveMetadata)
                {
                    ThrowHelper.ThrowNotSupportedException_BaseConverterDoesNotSupportMetadata(BaseType);
                }

                string propertyName = polymorphismOptions.TypeDiscriminatorPropertyName;
                if (!propertyName.Equals(JsonSerializer.TypePropertyName, StringComparison.Ordinal))
                {
                    byte[] utf8EncodedName = Encoding.UTF8.GetBytes(propertyName);

                    // Check if the property name conflicts with other metadata property names
                    if ((JsonSerializer.GetMetadataPropertyName(utf8EncodedName, resolver: null) & ~MetadataPropertyName.Type) != 0)
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidCustomTypeDiscriminatorPropertyName();
                    }

                    CustomTypeDiscriminatorPropertyNameUtf8 = utf8EncodedName;
                    CustomTypeDiscriminatorPropertyNameJsonEncoded = JsonEncodedText.Encode(propertyName, options.Encoder);
                }
            }
        }

        public Type BaseType { get; }
        public JsonUnknownDerivedTypeHandling UnknownDerivedTypeHandling { get; }
        public bool UsesTypeDiscriminators { get; }
        public bool IgnoreUnrecognizedTypeDiscriminators { get; }
        public byte[]? CustomTypeDiscriminatorPropertyNameUtf8 { get; }
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
                jsonTypeInfo = result.GetJsonTypeInfo(_options);
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
                jsonTypeInfo = result.GetJsonTypeInfo(_options);
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

            // First, walk up the class hierarchy for any supported types.
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
        /// Walks the type hierarchy above the current type for any types that use polymorphic configuration.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "The call to GetInterfaces will cross-reference results with interface types " +
                            "already declared as derived types of the polymorphic base type.")]
        internal static JsonTypeInfo? FindNearestPolymorphicBaseType(JsonTypeInfo typeInfo)
        {
            Debug.Assert(typeInfo.IsConfigured);

            if (typeInfo.PolymorphismOptions != null)
            {
                // Type defines its own polymorphic configuration.
                return null;
            }

            JsonTypeInfo? matchingResult = null;

            // First, walk up the class hierarchy for any supported types.
            for (Type? candidate = typeInfo.Type.BaseType; candidate != null; candidate = candidate.BaseType)
            {
                JsonTypeInfo? candidateInfo = ResolveAncestorTypeInfo(candidate, typeInfo.Options);
                if (candidateInfo?.PolymorphismOptions != null)
                {
                    // stop on the first ancestor that has a match
                    matchingResult = candidateInfo;
                    break;
                }
            }

            // Now, walk the interface hierarchy for any polymorphic interface declarations.
            foreach (Type interfaceType in typeInfo.Type.GetInterfaces())
            {
                JsonTypeInfo? candidateInfo = ResolveAncestorTypeInfo(interfaceType, typeInfo.Options);
                if (candidateInfo?.PolymorphismOptions != null)
                {
                    if (matchingResult != null)
                    {
                        // Resolve any conflicting matches.
                        if (matchingResult.Type.IsAssignableFrom(interfaceType))
                        {
                            // interface is more derived than previous match, replace it.
                            matchingResult = candidateInfo;
                        }
                        else if (interfaceType.IsAssignableFrom(matchingResult.Type))
                        {
                            // interface is less derived than previous match, keep the previous one.
                            continue;
                        }
                        else
                        {
                            // Diamond ambiguity, do not report any ancestors.
                            return null;
                        }
                    }
                    else
                    {
                        matchingResult = candidateInfo;
                    }
                }
            }

            return matchingResult;

            static JsonTypeInfo? ResolveAncestorTypeInfo(Type type, JsonSerializerOptions options)
            {
                try
                {
                    return options.GetTypeInfoInternal(type, ensureNotNull: null);
                }
                catch
                {
                    // The resolver produced an exception when resolving the ancestor type.
                    // Eat the exception and report no result instead.
                    return null;
                }
            }
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
                => _jsonTypeInfo ??= options.GetTypeInfoInternal(DerivedType);
        }
    }
}
