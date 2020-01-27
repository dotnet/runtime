// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Lookup the property given its name in the reader.
        /// </summary>
        // AggressiveInlining used although a large method it is only called from two locations and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LookupProperty(
            object obj,
            ref Utf8JsonReader reader,
            JsonSerializerOptions options,
            ref ReadStack state,
            out JsonPropertyInfo jsonPropertyInfo,
            out bool useExtensionProperty)
        {
            Debug.Assert(state.Current.JsonClassInfo.ClassType == ClassType.Object);

            ReadOnlySpan<byte> unescapedPropertyName = GetSpan(ref reader);
            ReadOnlySpan<byte> propertyName;

            if (reader._stringHasEscaping)
            {
                int idx = unescapedPropertyName.IndexOf(JsonConstants.BackSlash);
                Debug.Assert(idx != -1);
                propertyName = GetUnescapedString(unescapedPropertyName, idx);
            }
            else
            {
                propertyName = unescapedPropertyName;
            }

            jsonPropertyInfo = state.Current.JsonClassInfo.GetProperty(propertyName, ref state.Current);

            // Increment PropertyIndex so GetProperty() starts with the next property the next time this function is called.
            state.Current.PropertyIndex++;

            // Determine if we should use the extension property.
            if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
            {
                if (options.ReferenceHandling.ShouldReadPreservedReferences())
                {
                    if (unescapedPropertyName.Length > 0 && unescapedPropertyName[0] == '$')
                    {
                        // Ensure JsonPath doesn't attempt to use the previous property.
                        state.Current.JsonPropertyInfo = null!;

                        ThrowHelper.ThrowJsonException_MetadataInvalidPropertyWithLeadingDollarSign(unescapedPropertyName, ref state, reader);
                    }
                }

                JsonPropertyInfo? dataExtProperty = state.Current.JsonClassInfo.DataExtensionProperty;
                if (dataExtProperty != null)
                {
                    state.Current.JsonPropertyName = propertyName.ToArray();
                    state.Current.KeyName = JsonHelpers.Utf8GetString(propertyName);
                    CreateDataExtensionProperty(obj, dataExtProperty);
                    jsonPropertyInfo = dataExtProperty;
                }

                state.Current.JsonPropertyInfo = jsonPropertyInfo;
                useExtensionProperty = true;
                return;
            }

            // Support JsonException.Path.
            Debug.Assert(
                jsonPropertyInfo.JsonPropertyName == null ||
                options.PropertyNameCaseInsensitive ||
                propertyName.SequenceEqual(jsonPropertyInfo.JsonPropertyName));

            state.Current.JsonPropertyInfo = jsonPropertyInfo;

            if (jsonPropertyInfo.JsonPropertyName == null)
            {
                byte[] propertyNameArray = propertyName.ToArray();
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
        }

        internal static void CreateDataExtensionProperty(
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
