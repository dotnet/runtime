// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Implementation of <cref>JsonObjectConverter{T}</cref> that supports the deserialization
    /// of JSON objects using parameterized constructors.
    /// </summary>
    internal abstract partial class ObjectWithParameterizedConstructorConverter<T> : JsonObjectConverter<T>
    {
        // Whether the extension data is typeof(object) or typoef(JsonElement).
        private bool _dataExtensionIsObject;

        // All of the serializable properties on a POCO (except the optional extension property) keyed on property name.
        private volatile Dictionary<string, JsonPropertyInfo> _propertyCache = null!;

        // All of the serializable properties on a POCO including the optional extension property.
        // Used for performance during serialization instead of 'PropertyCache' above.
        private volatile JsonPropertyInfo[]? _propertyCacheArray;

        protected ConstructorInfo ConstructorInfo = null!;

        protected JsonPropertyInfo? DataExtensionProperty;

        protected volatile Dictionary<string, JsonParameterInfo> ParameterCache = null!;

        protected int ParameterCount { get; private set; }

        internal override void Initialize(ConstructorInfo constructor, JsonSerializerOptions options)
        {
            ConstructorInfo = constructor;

            // Properties must be initialized first.
            InitializeProperties(options);
            InitializeConstructorParameters(options);
        }

        private void InitializeProperties(JsonSerializerOptions options)
        {
            PropertyInfo[] properties = TypeToConvert.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            Dictionary<string, JsonPropertyInfo> propertyCache = CreatePropertyCache(properties.Length, options);

            foreach (PropertyInfo propertyInfo in properties)
            {
                // Ignore indexers
                if (propertyInfo.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                // For now we only support public getters\setters
                if (propertyInfo.GetMethod?.IsPublic == true ||
                    propertyInfo.SetMethod?.IsPublic == true)
                {
                    JsonPropertyInfo jsonPropertyInfo = JsonClassInfo.AddProperty(propertyInfo.PropertyType, propertyInfo, TypeToConvert, options);
                    Debug.Assert(jsonPropertyInfo != null && jsonPropertyInfo.NameAsString != null);

                    // If the JsonPropertyNameAttribute or naming policy results in collisions, throw an exception.
                    if (!JsonHelpers.TryAdd(propertyCache, jsonPropertyInfo.NameAsString, jsonPropertyInfo))
                    {
                        JsonPropertyInfo other = propertyCache[jsonPropertyInfo.NameAsString];

                        if (other.ShouldDeserialize == false && other.ShouldSerialize == false)
                        {
                            // Overwrite the one just added since it has [JsonIgnore].
                            propertyCache[jsonPropertyInfo.NameAsString] = jsonPropertyInfo;
                        }
                        else if (jsonPropertyInfo.ShouldDeserialize == true || jsonPropertyInfo.ShouldSerialize == true)
                        {
                            ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameConflict(TypeToConvert, jsonPropertyInfo);
                        }
                        // else ignore jsonPropertyInfo since it has [JsonIgnore].
                    }
                }
            }

            JsonPropertyInfo[] cacheArray;

            JsonPropertyInfo? dataExtensionProperty;
            if (JsonClassInfo.TryDetermineExtensionDataProperty(TypeToConvert, propertyCache, options, out dataExtensionProperty))
            {
                Debug.Assert(dataExtensionProperty != null);
                DataExtensionProperty = dataExtensionProperty;

                // Remove from propertyCache since it is handled independently.
                propertyCache.Remove(DataExtensionProperty.NameAsString!);

                cacheArray = new JsonPropertyInfo[propertyCache.Count + 1];

                // Set the last element to the extension property.
                cacheArray[propertyCache.Count] = DataExtensionProperty;
            }
            else
            {
                cacheArray = new JsonPropertyInfo[propertyCache.Count];
            }

            if (DataExtensionProperty != null)
            {
                _dataExtensionIsObject = typeof(IDictionary<string, object>).IsAssignableFrom(DataExtensionProperty.RuntimeClassInfo.Type);
            }

            propertyCache.Values.CopyTo(cacheArray, 0);

            _propertyCache = propertyCache;
            _propertyCacheArray = cacheArray;
        }

        public void InitializeConstructorParameters(JsonSerializerOptions options)
        {
            ParameterInfo[] parameters = ConstructorInfo.GetParameters();
            Dictionary<string, JsonParameterInfo> parameterCache = CreateParameterCache(parameters.Length, options);

            foreach (ParameterInfo parameterInfo in parameters)
            {
                PropertyInfo? firstMatch = null;
                bool isBound = false;

                foreach (JsonPropertyInfo jsonPropertyInfo in _propertyCache.Values)
                {
                    // This is not null because it is an actual
                    // property on a type, not a "policy property".
                    PropertyInfo propertyInfo = jsonPropertyInfo.PropertyInfo!;

                    string camelCasePropName = JsonNamingPolicy.CamelCase.ConvertName(propertyInfo.Name);

                    if (parameterInfo.Name == camelCasePropName &&
                        parameterInfo.ParameterType == propertyInfo.PropertyType)
                    {
                        if (isBound)
                        {
                            Debug.Assert(firstMatch != null);

                            // Multiple object properties cannot bind to the same
                            // constructor parameter.
                            ThrowHelper.ThrowInvalidOperationException_MultiplePropertiesBindToConstructorParameters(
                                TypeToConvert,
                                parameterInfo,
                                firstMatch,
                                propertyInfo,
                                ConstructorInfo);
                        }

                        JsonParameterInfo jsonParameterInfo = AddConstructorParameter(parameterInfo, jsonPropertyInfo, options);

                        // One object property cannot map to multiple constructor
                        // arguments (ConvertName above can't return multiple strings).
                        parameterCache.Add(jsonParameterInfo.NameAsString, jsonParameterInfo);

                        isBound = true;
                        firstMatch = propertyInfo;
                    }
                }
            }

            // It is invalid for the extension data property to bind with a constructor argument.
            if (DataExtensionProperty != null &&
                parameterCache.ContainsKey(DataExtensionProperty.NameAsString!))
            {
                throw new InvalidOperationException();
            }

            ParameterCache = parameterCache;
            ParameterCount = parameters.Length;
        }

        public Dictionary<string, JsonPropertyInfo> CreatePropertyCache(int capacity, JsonSerializerOptions options)
        {
            if (options.PropertyNameCaseInsensitive)
            {
                return new Dictionary<string, JsonPropertyInfo>(capacity, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                return new Dictionary<string, JsonPropertyInfo>(capacity);
            }
        }

        public Dictionary<string, JsonParameterInfo> CreateParameterCache(int capacity, JsonSerializerOptions options)
        {
            if (options.PropertyNameCaseInsensitive)
            {
                return new Dictionary<string, JsonParameterInfo>(capacity, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                return new Dictionary<string, JsonParameterInfo>(capacity);
            }
        }

        private static JsonParameterInfo AddConstructorParameter(
            ParameterInfo parameterInfo,
            JsonPropertyInfo jsonPropertyInfo,
            JsonSerializerOptions options)
        {
            string matchingPropertyName = jsonPropertyInfo.NameAsString!;

            if (jsonPropertyInfo.IsIgnored)
            {
                return JsonParameterInfo.CreateIgnoredParameterPlaceholder(matchingPropertyName, parameterInfo, options);
            }

            JsonConverter converter = jsonPropertyInfo.ConverterBase;

            JsonParameterInfo jsonParameterInfo = converter.CreateJsonParameterInfo();
            jsonParameterInfo.Initialize(
                matchingPropertyName,
                jsonPropertyInfo.DeclaredPropertyType,
                jsonPropertyInfo.RuntimePropertyType!,
                parameterInfo,
                converter,
                options);

            return jsonParameterInfo;
        }


        internal override bool ConstructorIsParameterized => true;
    }
}
