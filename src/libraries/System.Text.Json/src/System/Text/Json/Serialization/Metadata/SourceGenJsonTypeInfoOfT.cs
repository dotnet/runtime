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
        }

        /// <summary>
        /// Creates serialization metadata for an object.
        /// </summary>
        public SourceGenJsonTypeInfo(JsonSerializerOptions options, JsonObjectInfoValues<T> objectInfo) : base(GetConverter(objectInfo), options)
        {
            if (objectInfo.ObjectWithParameterizedConstructorCreator != null)
            {
                CreateObjectWithArgs = objectInfo.ObjectWithParameterizedConstructorCreator;
                CtorParamInitFunc = objectInfo.ConstructorParameterMetadataInitializer;
            }
            else
            {
                SetCreateObjectFunc(objectInfo.ObjectCreator);
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
            JsonCollectionInfoValues<T> collectionInfo!!,
            Func<JsonConverter<T>> converterCreator,
            object? createObjectWithArgs = null,
            object? addFunc = null)
            : base(GetConverter(collectionInfo, converterCreator), options)
        {
            KeyTypeInfo = collectionInfo.KeyInfo;
            ElementTypeInfo = collectionInfo.ElementInfo ?? throw new ArgumentNullException(nameof(collectionInfo.ElementInfo));
            NumberHandling = collectionInfo.NumberHandling;
            SerializeHandler = collectionInfo.SerializeHandler;
            CreateObjectWithArgs = createObjectWithArgs;
            AddMethodDelegate = addFunc;
            SetCreateObjectFunc(collectionInfo.ObjectCreator);
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

        private static JsonConverter GetConverter(JsonCollectionInfoValues<T> collectionInfo, Func<JsonConverter<T>> converterCreator)
        {
            ConverterStrategy strategy = collectionInfo.KeyInfo == null ? ConverterStrategy.Enumerable : ConverterStrategy.Dictionary;
            return new JsonMetadataServicesConverter<T>(converterCreator, strategy);
        }

        internal override void LateAddProperties()
        {
            AddPropertiesUsingSourceGenInfo();
        }

        internal override JsonParameterInfoValues[] GetParameterInfoValues()
        {
            JsonSerializerContext? context = Options.JsonSerializerContext;
            JsonParameterInfoValues[] array;
            if (context == null || CtorParamInitFunc == null || (array = CtorParamInitFunc()) == null)
            {
                ThrowHelper.ThrowInvalidOperationException_NoMetadataForTypeCtorParams(context, Type);
                return null!;
            }

            return array;
        }

        internal void AddPropertiesUsingSourceGenInfo()
        {
            if (PropertyInfoForTypeInfo.ConverterStrategy != ConverterStrategy.Object)
            {
                return;
            }

            JsonSerializerContext? context = Options.JsonSerializerContext;
            JsonPropertyInfo[] array;
            if (context == null || PropInitFunc == null || (array = PropInitFunc(context)) == null)
            {
                if (typeof(T) == typeof(object))
                {
                    return;
                }

                if (PropertyInfoForTypeInfo.ConverterBase.ElementType != null)
                {
                    // Nullable<> or F# optional converter's strategy is set to element's strategy
                    return;
                }

                if (SerializeHandler != null && Options.JsonSerializerContext?.CanUseSerializationLogic == true)
                {
                    ThrowOnDeserialize = true;
                    return;
                }

                ThrowHelper.ThrowInvalidOperationException_NoMetadataForTypeProperties(context, Type);
                return;
            }

            Dictionary<string, JsonPropertyInfo>? ignoredMembers = null;
            JsonPropertyDictionary<JsonPropertyInfo> propertyCache = new(Options.PropertyNameCaseInsensitive, array.Length);

            for (int i = 0; i < array.Length; i++)
            {
                JsonPropertyInfo jsonPropertyInfo = array[i];
                bool hasJsonInclude = jsonPropertyInfo.SrcGen_HasJsonInclude;

                if (!jsonPropertyInfo.SrcGen_IsPublic)
                {
                    if (hasJsonInclude)
                    {
                        ThrowHelper.ThrowInvalidOperationException_JsonIncludeOnNonPublicInvalid(jsonPropertyInfo.ClrName!, jsonPropertyInfo.DeclaringType);
                    }

                    continue;
                }

                if (jsonPropertyInfo.MemberType == MemberTypes.Field && !hasJsonInclude && !Options.IncludeFields)
                {
                    continue;
                }

                if (jsonPropertyInfo.SrcGen_IsExtensionData)
                {
                    // Source generator compile-time type inspection has performed this validation for us.
                    // except JsonTypeInfo can be initialized in parallel causing this to be ocassionally re-initialized
                    // Debug.Assert(DataExtensionProperty == null);
                    Debug.Assert(IsValidDataExtensionProperty(jsonPropertyInfo));

                    DataExtensionProperty = jsonPropertyInfo;
                    continue;
                }

                CacheMember(jsonPropertyInfo, propertyCache, ref ignoredMembers);
            }

            PropertyCache = propertyCache;
        }

        private void SetCreateObjectFunc(Func<T>? createObjectFunc)
        {
            if (createObjectFunc != null)
            {
                CreateObject = () => createObjectFunc();
            }
        }
    }
}
