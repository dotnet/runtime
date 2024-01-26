// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Reflection;
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
            object? obj,
            ReadOnlySpan<byte> unescapedPropertyName,
            ref ReadStack state,
            JsonSerializerOptions options,
            out bool useExtensionProperty,
            bool createExtensionProperty = true)
        {
            JsonTypeInfo jsonTypeInfo = state.Current.JsonTypeInfo;
#if DEBUG
            if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object)
            {
                string objTypeName = obj?.GetType().FullName ?? "<null>";
                Debug.Fail($"obj.GetType() => {objTypeName}; {jsonTypeInfo.GetPropertyDebugInfo(unescapedPropertyName)}");
            }
#endif

            useExtensionProperty = false;

            JsonPropertyInfo jsonPropertyInfo = jsonTypeInfo.GetProperty(
                unescapedPropertyName,
                ref state.Current,
                out byte[] utf8PropertyName);

            // Increment PropertyIndex so GetProperty() checks the next property first when called again.
            state.Current.PropertyIndex++;

            // For case insensitive and missing property support of JsonPath, remember the value on the temporary stack.
            state.Current.JsonPropertyName = utf8PropertyName;

            // Handle missing properties
            if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
            {
                if (jsonTypeInfo.EffectiveUnmappedMemberHandling is JsonUnmappedMemberHandling.Disallow)
                {
                    Debug.Assert(jsonTypeInfo.ExtensionDataProperty is null, "jsonTypeInfo.Configure() should have caught conflicting configuration.");
                    string stringPropertyName = JsonHelpers.Utf8GetString(unescapedPropertyName);
                    ThrowHelper.ThrowJsonException_UnmappedJsonProperty(jsonTypeInfo.Type, stringPropertyName);
                }

                // Determine if we should use the extension property.
                if (jsonTypeInfo.ExtensionDataProperty is JsonPropertyInfo { HasGetter: true, HasSetter: true } dataExtProperty)
                {
                    state.Current.JsonPropertyNameAsString = JsonHelpers.Utf8GetString(unescapedPropertyName);

                    if (createExtensionProperty)
                    {
                        Debug.Assert(obj != null, "obj is null");
                        CreateExtensionDataProperty(obj, dataExtProperty, options);
                    }

                    jsonPropertyInfo = dataExtProperty;
                    useExtensionProperty = true;
                }
            }

            state.Current.JsonPropertyInfo = jsonPropertyInfo;
            state.Current.NumberHandling = jsonPropertyInfo.EffectiveNumberHandling;
            return jsonPropertyInfo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<byte> GetPropertyName(
            scoped ref ReadStack state,
            ref Utf8JsonReader reader,
            JsonSerializerOptions options,
            out bool isAlreadyReadMetadataProperty)
        {
            ReadOnlySpan<byte> propertyName = reader.GetUnescapedSpan();
            isAlreadyReadMetadataProperty = false;

            if (state.Current.CanContainMetadata)
            {
                if (IsMetadataPropertyName(propertyName, state.Current.BaseJsonTypeInfo.PolymorphicTypeResolver))
                {
                    if (options.AllowOutOfOrderMetadataProperties)
                    {
                        isAlreadyReadMetadataProperty = true;
                    }
                    else
                    {
                        ThrowHelper.ThrowUnexpectedMetadataException(propertyName, ref reader, ref state);
                    }
                }
            }

            return propertyName;
        }

        internal static void CreateExtensionDataProperty(
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
                Type underlyingIDictionaryType = jsonPropertyInfo.PropertyType.GetCompatibleGenericInterface(typeof(IDictionary<,>))!;
                Type[] genericArgs = underlyingIDictionaryType.GetGenericArguments();

                Debug.Assert(underlyingIDictionaryType.IsGenericType);
                Debug.Assert(genericArgs.Length == 2);
                Debug.Assert(genericArgs[0].UnderlyingSystemType == typeof(string));
                Debug.Assert(
                    genericArgs[1].UnderlyingSystemType == JsonTypeInfo.ObjectType ||
                    genericArgs[1].UnderlyingSystemType == typeof(JsonElement) ||
                    genericArgs[1].UnderlyingSystemType == typeof(Nodes.JsonNode));
#endif

                Func<object>? createObjectForExtensionDataProp = jsonPropertyInfo.JsonTypeInfo.CreateObject
                    ?? jsonPropertyInfo.JsonTypeInfo.CreateObjectForExtensionDataProperty;

                if (createObjectForExtensionDataProp == null)
                {
                    // Avoid a reference to the JsonNode type for trimming
                    if (jsonPropertyInfo.PropertyType.FullName == JsonTypeInfo.JsonObjectTypeName)
                    {
                        ThrowHelper.ThrowInvalidOperationException_NodeJsonObjectCustomConverterNotAllowedOnExtensionProperty();
                    }
                    else
                    {
                        ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(jsonPropertyInfo.PropertyType);
                    }
                }

                extensionData = createObjectForExtensionDataProp();
                Debug.Assert(jsonPropertyInfo.Set != null);
                jsonPropertyInfo.Set(obj, extensionData);
            }

            // We don't add the value to the dictionary here because we need to support the read-ahead functionality for Streams.
        }
    }
}
