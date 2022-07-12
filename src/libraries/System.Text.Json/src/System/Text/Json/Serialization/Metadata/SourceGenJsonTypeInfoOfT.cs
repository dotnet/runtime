// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Creates and initializes serialization metadata for a type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class SourceGenJsonTypeInfo<T> : JsonTypeInfo<T>
    {
        /// <summary>
        /// Creates serialization metadata for a type using a simple converter.
        /// </summary>
        public SourceGenJsonTypeInfo(JsonConverter converter, JsonSerializerOptions options)
            : base(converter, options)
        {
            PolymorphismOptions = JsonPolymorphismOptions.CreateFromAttributeDeclarations(Type);
            MapInterfaceTypesToCallbacks();
        }

        /// <summary>
        /// Creates serialization metadata for an object.
        /// </summary>
        public SourceGenJsonTypeInfo(JsonSerializerOptions options, JsonObjectInfoValues<T> objectInfo) : this(GetConverter(objectInfo), options)
        {
            if (objectInfo.ObjectWithParameterizedConstructorCreator != null)
            {
                CreateObjectWithArgs = objectInfo.ObjectWithParameterizedConstructorCreator;
                CtorParamInitFunc = objectInfo.ConstructorParameterMetadataInitializer;
            }
            else
            {
                SetCreateObject(objectInfo.ObjectCreator);
                CreateObjectForExtensionDataProperty = ((JsonTypeInfo)this).CreateObject;
            }

            PropInitFunc = objectInfo.PropertyMetadataInitializer;
            SerializeHandler = objectInfo.SerializeHandler;
            NumberHandling = objectInfo.NumberHandling;
        }

        /// <summary>
        /// Creates serialization metadata for a collection.
        /// </summary>
        public SourceGenJsonTypeInfo(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<T> collectionInfo,
            Func<JsonConverter<T>> converterCreator,
            object? createObjectWithArgs = null,
            object? addFunc = null)
            : this(new JsonMetadataServicesConverter<T>(converterCreator()), options)
        {
            if (collectionInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(collectionInfo));
            }

            KeyTypeInfo = collectionInfo.KeyInfo;
            ElementTypeInfo = collectionInfo.ElementInfo;
            Debug.Assert(Kind != JsonTypeInfoKind.None);
            NumberHandling = collectionInfo.NumberHandling;
            SerializeHandler = collectionInfo.SerializeHandler;
            CreateObjectWithArgs = createObjectWithArgs;
            AddMethodDelegate = addFunc;
            CreateObject = collectionInfo.ObjectCreator;
        }

        private static JsonConverter GetConverter(JsonObjectInfoValues<T> objectInfo)
        {
#pragma warning disable CS8714
            // The type cannot be used as type parameter in the generic type or method.
            // Nullability of type argument doesn't match 'notnull' constraint.
            if (objectInfo.ObjectWithParameterizedConstructorCreator != null)
            {
                return new JsonMetadataServicesConverter<T>(
                    () => new LargeObjectWithParameterizedConstructorConverter<T>(),
                    ConverterStrategy.Object);
            }
            else
            {
                return new JsonMetadataServicesConverter<T>(() => new ObjectDefaultConverter<T>(), ConverterStrategy.Object);
            }
#pragma warning restore CS8714
        }

        internal override JsonParameterInfoValues[] GetParameterInfoValues()
        {
            JsonParameterInfoValues[] array;
            if (CtorParamInitFunc == null || (array = CtorParamInitFunc()) == null)
            {
                ThrowHelper.ThrowInvalidOperationException_NoMetadataForTypeCtorParams(Options.TypeInfoResolverSafe, Type);
                return null!;
            }

            return array;
        }

        internal override void LateAddProperties()
        {
            Debug.Assert(!IsConfigured);
            Debug.Assert(PropertyCache is null);

            if (Kind != JsonTypeInfoKind.Object)
            {
                return;
            }

            JsonSerializerContext? context = Options.SerializerContext;
            JsonPropertyInfo[] array;
            if (PropInitFunc == null || (array = PropInitFunc(context!)) == null)
            {
                if (typeof(T) == typeof(object))
                {
                    return;
                }

                if (Converter.ElementType != null)
                {
                    // Nullable<> or F# optional converter's strategy is set to element's strategy
                    return;
                }

                if (SerializeHandler != null && context?.CanUseSerializationLogic == true)
                {
                    ThrowOnDeserialize = true;
                    return;
                }

                ThrowHelper.ThrowInvalidOperationException_NoMetadataForTypeProperties(Options.TypeInfoResolverSafe, Type);
                return;
            }

            Dictionary<string, JsonPropertyInfo>? ignoredMembers = null;
            JsonPropertyDictionary<JsonPropertyInfo> propertyCache = CreatePropertyCache(capacity: array.Length);

            for (int i = 0; i < array.Length; i++)
            {
                JsonPropertyInfo jsonPropertyInfo = array[i];
                bool hasJsonInclude = jsonPropertyInfo.SrcGen_HasJsonInclude;

                if (!jsonPropertyInfo.SrcGen_IsPublic)
                {
                    if (hasJsonInclude)
                    {
                        Debug.Assert(jsonPropertyInfo.MemberName != null, "MemberName is not set by source gen");
                        ThrowHelper.ThrowInvalidOperationException_JsonIncludeOnNonPublicInvalid(jsonPropertyInfo.MemberName, jsonPropertyInfo.DeclaringType);
                    }

                    continue;
                }

                if (jsonPropertyInfo.MemberType == MemberTypes.Field && !hasJsonInclude && !Options.IncludeFields)
                {
                    continue;
                }

                CacheMember(jsonPropertyInfo, propertyCache, ref ignoredMembers);
            }

            PropertyCache = propertyCache;
        }
    }
}
