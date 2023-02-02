// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization.Metadata
{
    public static partial class JsonMetadataServices
    {
        /// <summary>
        /// Creates serialization metadata for a type using a simple converter.
        /// </summary>
        private static JsonTypeInfo<T> CreateCore<T>(JsonConverter converter, JsonSerializerOptions options)
        {
            JsonTypeInfo<T> typeInfo = new JsonTypeInfo<T>(converter, options);
            typeInfo.PopulatePolymorphismMetadata();
            typeInfo.MapInterfaceTypesToCallbacks();

            // Plug in any converter configuration -- should be run last.
            converter.ConfigureJsonTypeInfo(typeInfo, options);
            return typeInfo;
        }

        /// <summary>
        /// Creates serialization metadata for an object.
        /// </summary>
        private static JsonTypeInfo<T> CreateCore<T>(JsonSerializerOptions options, JsonObjectInfoValues<T> objectInfo)
        {
            JsonConverter<T> converter = GetConverter(objectInfo);
            JsonTypeInfo<T> typeInfo = new JsonTypeInfo<T>(converter, options);
            if (objectInfo.ObjectWithParameterizedConstructorCreator != null)
            {
                typeInfo.CreateObjectWithArgs = objectInfo.ObjectWithParameterizedConstructorCreator;
                PopulateParameterInfoValues(typeInfo, objectInfo.ConstructorParameterMetadataInitializer);
            }
            else
            {
                typeInfo.SetCreateObjectIfCompatible(objectInfo.ObjectCreator);
                typeInfo.CreateObjectForExtensionDataProperty = ((JsonTypeInfo)typeInfo).CreateObject;
            }

            PopulateProperties(typeInfo, objectInfo.PropertyMetadataInitializer);
            typeInfo.SerializeHandler = objectInfo.SerializeHandler;
            typeInfo.NumberHandling = objectInfo.NumberHandling;
            typeInfo.PopulatePolymorphismMetadata();
            typeInfo.MapInterfaceTypesToCallbacks();

            // Plug in any converter configuration -- should be run last.
            converter.ConfigureJsonTypeInfo(typeInfo, options);
            return typeInfo;
        }

        /// <summary>
        /// Creates serialization metadata for a collection.
        /// </summary>
        private static JsonTypeInfo<T> CreateCore<T>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<T> collectionInfo,
            Func<JsonConverter<T>> converterCreator,
            object? createObjectWithArgs = null,
            object? addFunc = null)
        {
            JsonConverter<T> converter = new JsonMetadataServicesConverter<T>(converterCreator());
            JsonTypeInfo<T> typeInfo = new JsonTypeInfo<T>(converter, options);
            if (collectionInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(collectionInfo));
            }

            typeInfo.KeyTypeInfo = collectionInfo.KeyInfo;
            typeInfo.ElementTypeInfo = collectionInfo.ElementInfo;
            Debug.Assert(typeInfo.Kind != JsonTypeInfoKind.None);
            typeInfo.NumberHandling = collectionInfo.NumberHandling;
            typeInfo.SerializeHandler = collectionInfo.SerializeHandler;
            typeInfo.CreateObjectWithArgs = createObjectWithArgs;
            typeInfo.AddMethodDelegate = addFunc;
            typeInfo.SetCreateObjectIfCompatible(collectionInfo.ObjectCreator);
            typeInfo.PopulatePolymorphismMetadata();
            typeInfo.MapInterfaceTypesToCallbacks();

            // Plug in any converter configuration -- should be run last.
            converter.ConfigureJsonTypeInfo(typeInfo, options);
            return typeInfo;
        }

        private static JsonMetadataServicesConverter<T> GetConverter<T>(JsonObjectInfoValues<T> objectInfo)
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

        private static void PopulateParameterInfoValues(JsonTypeInfo typeInfo, Func<JsonParameterInfoValues[]?>? paramFactory)
        {
            Debug.Assert(typeInfo.Kind is JsonTypeInfoKind.Object);
            Debug.Assert(!typeInfo.IsReadOnly);

            if (paramFactory?.Invoke() is JsonParameterInfoValues[] array)
            {
                typeInfo.ParameterInfoValues = array;
            }
            else
            {
                typeInfo.MetadataSerializationNotSupported = true;
            }
        }

        private static void PopulateProperties(JsonTypeInfo typeInfo, Func<JsonSerializerContext, JsonPropertyInfo[]?>? propInitFunc)
        {
            Debug.Assert(typeInfo.Kind is JsonTypeInfoKind.Object);
            Debug.Assert(!typeInfo.IsReadOnly);

            JsonSerializerContext? context = typeInfo.Options.TypeInfoResolver as JsonSerializerContext;
            if (propInitFunc?.Invoke(context!) is not JsonPropertyInfo[] properties)
            {
                if (typeInfo.Type == JsonTypeInfo.ObjectType)
                {
                    return;
                }

                if (typeInfo.Converter.ElementType != null)
                {
                    // Nullable<> or F# optional converter strategy is set to element strategy
                    return;
                }

                typeInfo.MetadataSerializationNotSupported = true;
                return;
            }

            // TODO update the source generator so that all property
            // hierarchy resolution is happening at compile time.
            JsonTypeInfo.PropertyHierarchyResolutionState state = new();

            foreach (JsonPropertyInfo jsonPropertyInfo in properties)
            {
                if (!jsonPropertyInfo.SrcGen_IsPublic)
                {
                    if (jsonPropertyInfo.SrcGen_HasJsonInclude)
                    {
                        Debug.Assert(jsonPropertyInfo.MemberName != null, "MemberName is not set by source gen");
                        ThrowHelper.ThrowInvalidOperationException_JsonIncludeOnNonPublicInvalid(jsonPropertyInfo.MemberName, jsonPropertyInfo.DeclaringType);
                    }

                    continue;
                }

                if (jsonPropertyInfo.MemberType == MemberTypes.Field && !jsonPropertyInfo.SrcGen_HasJsonInclude && !typeInfo.Options.IncludeFields)
                {
                    continue;
                }

                typeInfo.AddProperty(jsonPropertyInfo, ref state);
            }

            // NB we don't need to sort source gen properties here since they were already sorted at compile time.
        }
    }
}
