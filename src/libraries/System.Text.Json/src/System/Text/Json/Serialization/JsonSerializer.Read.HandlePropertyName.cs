// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Lookup the property given its name (obtained from the reader) and return it.
        /// Also sets state.Current.JsonPropertyInfo to a non-null value.
        /// </summary>
        // AggressiveInlining used although a large method it is only called from two locations and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static JsonPropertyInfo LookupProperty(
            object obj,
            ref Utf8JsonReader reader,
            JsonSerializerOptions options,
            ref ReadStack state,
            out bool useExtensionProperty)
        {
            Debug.Assert(state.Current.JsonClassInfo.ClassType == ClassType.Object);

            JsonPropertyInfo jsonPropertyInfo;

            ReadOnlySpan<byte> unescapedPropertyName;
            ReadOnlySpan<byte> propertyName = reader.GetSpan();

            if (reader._stringHasEscaping)
            {
                int idx = propertyName.IndexOf(JsonConstants.BackSlash);
                Debug.Assert(idx != -1);
                unescapedPropertyName = GetUnescapedString(propertyName, idx);
            }
            else
            {
                unescapedPropertyName = propertyName;
            }

            if (options.ReferenceHandling.ShouldReadPreservedReferences())
            {
                if (propertyName.Length > 0 && propertyName[0] == '$')
                {
                    ThrowHelper.ThrowUnexpectedMetadataException(propertyName, ref reader, ref state);
                }
            }

            jsonPropertyInfo = state.Current.JsonClassInfo.GetProperty(unescapedPropertyName, ref state.Current);

            // Increment PropertyIndex so GetProperty() starts with the next property the next time this function is called.
            state.Current.PropertyIndex++;

            // Determine if we should use the extension property.
            if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
            {
                JsonPropertyInfo? dataExtProperty = state.Current.JsonClassInfo.DataExtensionProperty;
                if (dataExtProperty != null)
                {
                    state.Current.JsonPropertyNameAsString = JsonHelpers.Utf8GetString(unescapedPropertyName);
                    CreateDataExtensionProperty(obj, dataExtProperty);
                    jsonPropertyInfo = dataExtProperty;
                }

                state.Current.JsonPropertyInfo = jsonPropertyInfo;
                useExtensionProperty = true;
                return jsonPropertyInfo;
            }

            // Support JsonException.Path.
            Debug.Assert(
                jsonPropertyInfo.JsonPropertyName == null ||
                options.PropertyNameCaseInsensitive ||
                unescapedPropertyName.SequenceEqual(jsonPropertyInfo.JsonPropertyName));

            state.Current.JsonPropertyInfo = jsonPropertyInfo;

            if (jsonPropertyInfo.JsonPropertyName == null)
            {
                byte[] propertyNameArray = unescapedPropertyName.ToArray();
                if (options.PropertyNameCaseInsensitive)
                {
                    // Each payload can have a different name here; remember the value on the temporary stack.
                    state.Current.JsonPropertyName = propertyNameArray;
                }
                else
                {
                    // Prevent future allocs by caching globally on the JsonPropertyInfo which is specific to a Type+PropertyName
                    // so it will match the incoming payload except when case insensitivity is enabled (which is handled above).
                    state.Current.JsonPropertyInfo.JsonPropertyName = propertyNameArray;
                }
            }

            state.Current.JsonPropertyInfo = jsonPropertyInfo;
            useExtensionProperty = false;
            return jsonPropertyInfo;
        }

        private static void CreateDataExtensionProperty(
            object obj,
            JsonPropertyInfo jsonPropertyInfo)
        {
            Debug.Assert(jsonPropertyInfo != null);

            IDictionary? extensionData = (IDictionary?)jsonPropertyInfo.GetValueAsObject(obj);
            if (extensionData == null)
            {
                // Create the appropriate dictionary type. We already verified the types.
                Debug.Assert(jsonPropertyInfo.DeclaredPropertyType.IsGenericType);
                Debug.Assert(jsonPropertyInfo.DeclaredPropertyType.GetGenericArguments().Length == 2);
                Debug.Assert(jsonPropertyInfo.DeclaredPropertyType.GetGenericArguments()[0].UnderlyingSystemType == typeof(string));
                Debug.Assert(
                    jsonPropertyInfo.DeclaredPropertyType.GetGenericArguments()[1].UnderlyingSystemType == typeof(object) ||
                    jsonPropertyInfo.DeclaredPropertyType.GetGenericArguments()[1].UnderlyingSystemType == typeof(JsonElement));

                Debug.Assert(jsonPropertyInfo.RuntimeClassInfo.CreateObject != null);
                extensionData = (IDictionary?)jsonPropertyInfo.RuntimeClassInfo.CreateObject();
                jsonPropertyInfo.SetValueAsObject(obj, extensionData);
            }

            // We don't add the value to the dictionary here because we need to support the read-ahead functionality for Streams.
        }
    }
}
