// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            var typeInfo = new JsonTypeInfo<T>(converter, options);
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
            var typeInfo = new JsonTypeInfo<T>(converter, options);
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

            if (objectInfo.PropertyMetadataInitializer != null)
            {
                typeInfo.SourceGenDelayedPropertyInitializer = objectInfo.PropertyMetadataInitializer;
            }
            else
            {
                typeInfo.PropertyMetadataSerializationNotSupported = true;
            }

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
            JsonConverter<T> converter,
            object? createObjectWithArgs = null,
            object? addFunc = null)
        {
            if (collectionInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(collectionInfo));
            }

            converter = collectionInfo.SerializeHandler != null
                ? new JsonMetadataServicesConverter<T>(converter)
                : converter;

            JsonTypeInfo<T> typeInfo = new JsonTypeInfo<T>(converter, options);

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

        private static JsonConverter<T> GetConverter<T>(JsonObjectInfoValues<T> objectInfo)
        {
#pragma warning disable CS8714 // Nullability of type argument 'T' doesn't match 'notnull' constraint.
            JsonConverter<T> converter = objectInfo.ObjectWithParameterizedConstructorCreator != null
                ? new LargeObjectWithParameterizedConstructorConverter<T>()
                : new ObjectDefaultConverter<T>();
#pragma warning restore CS8714

            return objectInfo.SerializeHandler != null
                ? new JsonMetadataServicesConverter<T>(converter)
                : converter;
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
                typeInfo.PropertyMetadataSerializationNotSupported = true;
            }
        }

        internal static void PopulateProperties(JsonTypeInfo typeInfo, JsonTypeInfo.JsonPropertyInfoList propertyList, Func<JsonSerializerContext, JsonPropertyInfo[]> propInitFunc)
        {
            Debug.Assert(typeInfo.Kind is JsonTypeInfoKind.Object);
            Debug.Assert(!typeInfo.IsConfigured);
            Debug.Assert(typeInfo.Type != JsonTypeInfo.ObjectType);
            Debug.Assert(typeInfo.Converter.ElementType is null);

            JsonSerializerContext? context = typeInfo.Options.TypeInfoResolver as JsonSerializerContext;
            JsonPropertyInfo[] properties = propInitFunc(context!);

            // Regardless of the source generator we need to re-run the naming conflict resolution algorithm
            // at run time since it is possible that the naming policy or other configs can be different then.
            JsonTypeInfo.PropertyHierarchyResolutionState state = new(typeInfo.Options);

            foreach (JsonPropertyInfo jsonPropertyInfo in properties)
            {
                if (!jsonPropertyInfo.SrcGen_IsPublic)
                {
                    if (jsonPropertyInfo.SrcGen_HasJsonInclude)
                    {
                        Debug.Assert(jsonPropertyInfo.MemberName != null, "MemberName is not set by source gen");
                        ThrowHelper.ThrowInvalidOperationException_JsonIncludeOnInaccessibleProperty(jsonPropertyInfo.MemberName, jsonPropertyInfo.DeclaringType);
                    }

                    continue;
                }

                if (jsonPropertyInfo.MemberType == MemberTypes.Field && !jsonPropertyInfo.SrcGen_HasJsonInclude && !typeInfo.Options.IncludeFields)
                {
                    continue;
                }

                propertyList.AddPropertyWithConflictResolution(jsonPropertyInfo, ref state);
            }

            if (state.IsPropertyOrderSpecified)
            {
                propertyList.SortProperties();
            }
        }

        private static JsonPropertyInfo<T> CreatePropertyInfoCore<T>(JsonPropertyInfoValues<T> propertyInfoValues, JsonSerializerOptions options)
        {
            var propertyInfo = new JsonPropertyInfo<T>(propertyInfoValues.DeclaringType, declaringTypeInfo: null, options);

            DeterminePropertyName(propertyInfo,
                declaredPropertyName: propertyInfoValues.PropertyName,
                declaredJsonPropertyName: propertyInfoValues.JsonPropertyName);

            propertyInfo.MemberName = propertyInfoValues.PropertyName;
            propertyInfo.MemberType = propertyInfoValues.IsProperty ? MemberTypes.Property : MemberTypes.Field;
            propertyInfo.SrcGen_IsPublic = propertyInfoValues.IsPublic;
            propertyInfo.SrcGen_HasJsonInclude = propertyInfoValues.HasJsonInclude;
            propertyInfo.IsExtensionData = propertyInfoValues.IsExtensionData;
            propertyInfo.CustomConverter = propertyInfoValues.Converter;

            if (propertyInfo.IgnoreCondition != JsonIgnoreCondition.Always)
            {
                propertyInfo.Get = propertyInfoValues.Getter!;
                propertyInfo.Set = propertyInfoValues.Setter;
            }

            propertyInfo.IgnoreCondition = propertyInfoValues.IgnoreCondition;
            propertyInfo.JsonTypeInfo = propertyInfoValues.PropertyTypeInfo;
            propertyInfo.NumberHandling = propertyInfoValues.NumberHandling;

            return propertyInfo;
        }

        private static void DeterminePropertyName(
            JsonPropertyInfo propertyInfo,
            string declaredPropertyName,
            string? declaredJsonPropertyName)
        {
            string? name;

            // Property name settings.
            if (declaredJsonPropertyName != null)
            {
                name = declaredJsonPropertyName;
            }
            else if (propertyInfo.Options.PropertyNamingPolicy == null)
            {
                name = declaredPropertyName;
            }
            else
            {
                name = propertyInfo.Options.PropertyNamingPolicy.ConvertName(declaredPropertyName);
            }

            // Compat: We need to do validation before we assign Name so that we get InvalidOperationException rather than ArgumentNullException
            if (name == null)
            {
                ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(propertyInfo);
            }

            propertyInfo.Name = name;
        }
    }
}
