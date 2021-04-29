// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Lookup the property given its name (obtained from the reader) and return it.
        /// Also sets state.Current.JsonPropertyInfo to a non-null value.
        /// </summary>
        internal static JsonPropertyInfo LookupProperty(
            object obj,
            ReadOnlySpan<byte> unescapedPropertyName,
            ref ReadStack state,
            JsonSerializerOptions options,
            out bool useExtensionProperty,
            bool createExtensionProperty = true)
        {
            Debug.Assert(state.Current.JsonTypeInfo.PropertyInfoForTypeInfo.ConverterStrategy == ConverterStrategy.Object);

            useExtensionProperty = false;

            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonTypeInfo.GetProperty(
                unescapedPropertyName,
                ref state.Current,
                out byte[] utf8PropertyName);

            // Increment PropertyIndex so GetProperty() checks the next property first when called again.
            state.Current.PropertyIndex++;

            // For case insensitive and missing property support of JsonPath, remember the value on the temporary stack.
            state.Current.JsonPropertyName = utf8PropertyName;

            // Determine if we should use the extension property.
            if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
            {
                JsonPropertyInfo? dataExtProperty = state.Current.JsonTypeInfo.DataExtensionProperty;
                if (dataExtProperty != null && dataExtProperty.HasGetter && dataExtProperty.HasSetter)
                {
                    state.Current.JsonPropertyNameAsString = JsonHelpers.Utf8GetString(unescapedPropertyName);

                    if (createExtensionProperty)
                    {
                        CreateDataExtensionProperty(obj, dataExtProperty, options);
                    }

                    jsonPropertyInfo = dataExtProperty;
                    useExtensionProperty = true;
                }
            }

            state.Current.JsonPropertyInfo = jsonPropertyInfo;
            state.Current.NumberHandling = jsonPropertyInfo.NumberHandling;
            return jsonPropertyInfo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<byte> GetPropertyName(
            ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonSerializerOptions options)
        {
            ReadOnlySpan<byte> unescapedPropertyName;
            ReadOnlySpan<byte> propertyName = reader.GetSpan();

            if (reader._stringHasEscaping)
            {
                int idx = propertyName.IndexOf(JsonConstants.BackSlash);
                Debug.Assert(idx != -1);
                unescapedPropertyName = JsonReaderHelper.GetUnescapedSpan(propertyName, idx);
            }
            else
            {
                unescapedPropertyName = propertyName;
            }

            if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve)
            {
                if (propertyName.Length > 0 && propertyName[0] == '$')
                {
                    ThrowHelper.ThrowUnexpectedMetadataException(propertyName, ref reader, ref state);
                }
            }

            return unescapedPropertyName;
        }

        internal static void CreateDataExtensionProperty(
            object obj,
            JsonPropertyInfo jsonPropertyInfo,
            JsonSerializerOptions options)
        {
            Debug.Assert(jsonPropertyInfo != null);

            object? extensionData = jsonPropertyInfo.GetValueAsObject(obj);
            if (extensionData == null)
            {
                // Create the appropriate dictionary type. We already verified the types.
#if DEBUG
                Type underlyingIDictionaryType = jsonPropertyInfo.DeclaredPropertyType.GetCompatibleGenericInterface(typeof(IDictionary<,>))!;
                Type[] genericArgs = underlyingIDictionaryType.GetGenericArguments();

                Debug.Assert(underlyingIDictionaryType.IsGenericType);
                Debug.Assert(genericArgs.Length == 2);
                Debug.Assert(genericArgs[0].UnderlyingSystemType == typeof(string));
                Debug.Assert(
                    genericArgs[1].UnderlyingSystemType == JsonTypeInfo.ObjectType ||
                    genericArgs[1].UnderlyingSystemType == typeof(JsonElement) ||
                    genericArgs[1].UnderlyingSystemType == typeof(Node.JsonNode));
#endif
                if (jsonPropertyInfo.RuntimeTypeInfo.CreateObject == null)
                {
                    // Avoid a reference to the JsonNode type for trimming
                    if (jsonPropertyInfo.DeclaredPropertyType.FullName == JsonTypeInfo.JsonObjectTypeName)
                    {
                        extensionData = jsonPropertyInfo.ConverterBase.CreateObject(options);
                    }
                    else
                    {
                        ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(jsonPropertyInfo.DeclaredPropertyType);
                    }
                }
                else
                {
                    extensionData = jsonPropertyInfo.RuntimeTypeInfo.CreateObject();
                }

                jsonPropertyInfo.SetExtensionDictionaryAsObject(obj, extensionData);
            }

            // We don't add the value to the dictionary here because we need to support the read-ahead functionality for Streams.
        }
    }
}
