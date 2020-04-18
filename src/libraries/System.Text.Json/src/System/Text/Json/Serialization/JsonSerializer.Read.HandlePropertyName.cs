// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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
            out bool useExtensionProperty,
            bool createExtensionProperty = true)
        {
            Debug.Assert(state.Current.JsonClassInfo.ClassType == ClassType.Object);

            ReadOnlySpan<byte> unescapedPropertyName = GetPropertyName(ref state, ref reader, options);

            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonClassInfo.GetProperty(unescapedPropertyName, ref state.Current);

            // Increment PropertyIndex so GetProperty() starts with the next property the next time this function is called.
            state.Current.PropertyIndex++;

            // Determine if we should use the extension property.
            if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
            {
                JsonPropertyInfo? dataExtProperty = state.Current.JsonClassInfo.DataExtensionProperty;
                if (dataExtProperty != null && dataExtProperty.HasGetter && dataExtProperty.HasSetter)
                {
                    state.Current.JsonPropertyNameAsString = JsonHelpers.Utf8GetString(unescapedPropertyName);

                    if (createExtensionProperty)
                    {
                        CreateDataExtensionProperty(ref state, dataExtProperty);
                    }

                    jsonPropertyInfo = dataExtProperty;
                    useExtensionProperty = true;
                }
                else
                {
                    useExtensionProperty = false;
                }

                state.Current.JsonPropertyInfo = jsonPropertyInfo;
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

            if (options.ReferenceHandling.ShouldReadPreservedReferences())
            {
                if (propertyName.Length > 0 && propertyName[0] == '$')
                {
                    ThrowHelper.ThrowUnexpectedMetadataException(propertyName, ref reader, ref state);
                }
            }

            return unescapedPropertyName;
        }

        internal static void CreateDataExtensionProperty(
            ref ReadStack state,
            JsonPropertyInfo jsonPropertyInfo)
        {
            Debug.Assert(jsonPropertyInfo != null);

            object? extensionData = state.Current.DataExtensionData;
            if (extensionData == null)
            {
                Type? underlyingIDictionaryType = jsonPropertyInfo.DeclaredPropertyType.GetCompatibleGenericInterface(typeof(IDictionary<,>));
                if (underlyingIDictionaryType is null)
                {
                    underlyingIDictionaryType = jsonPropertyInfo.DeclaredPropertyType.GetCompatibleGenericInterface(typeof(IReadOnlyDictionary<,>))!;
                }

                Type[] genericArgs = underlyingIDictionaryType.GetGenericArguments();
                // Create the appropriate dictionary type. We already verified the types.
#if DEBUG
                Debug.Assert(underlyingIDictionaryType.IsGenericType);
                Debug.Assert(genericArgs.Length == 2);
                Debug.Assert(genericArgs[0].UnderlyingSystemType == typeof(string));
                Debug.Assert(
                    genericArgs[1].UnderlyingSystemType == typeof(object) ||
                    genericArgs[1].UnderlyingSystemType == typeof(JsonElement));
#endif
                if (jsonPropertyInfo.RuntimeClassInfo.CreateObject == null)
                {
                    // Special case for immutable dictionaries since we build up a dictionary and convert
                    if (!jsonPropertyInfo.DeclaredPropertyType.IsImmutableDictionaryType())
                    {
                        ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(jsonPropertyInfo.DeclaredPropertyType);
                    }

                    extensionData = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(genericArgs));
                }
                else
                {
                    extensionData = jsonPropertyInfo.RuntimeClassInfo.CreateObject();
                }

                state.Current.DataExtensionData = extensionData;
            }

            // We don't add the value to the dictionary here because we need to support the read-ahead functionality for Streams.
        }
    }
}
