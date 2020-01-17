// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        // AggressiveInlining used although a large method it is only called from one locations and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandlePropertyName(
            JsonSerializerOptions options,
            ref Utf8JsonReader reader,
            ref ReadStack state)
        {
            if (state.Current.Drain)
            {
                return;
            }

            Debug.Assert(state.Current.ReturnValue != null || state.Current.TempDictionaryValues != null);
            Debug.Assert(state.Current.JsonClassInfo != null);

            bool isProcessingDictObject = state.Current.IsProcessingObject(ClassType.Dictionary);
            if ((isProcessingDictObject || state.Current.IsProcessingProperty(ClassType.Dictionary)) &&
                state.Current.JsonClassInfo.DataExtensionProperty != state.Current.JsonPropertyInfo)
            {
                if (isProcessingDictObject)
                {
                    state.Current.JsonPropertyInfo = state.Current.JsonClassInfo.PolicyProperty;
                }

                if (options.ReferenceHandling.ShouldReadPreservedReferences())
                {
                    ReadOnlySpan<byte> propertyName = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
                    MetadataPropertyName metadata = GetMetadataProperty(propertyName, ref state, ref reader);
                    ResolveMetadataOnDictionary(metadata, ref state);
                }

                state.Current.KeyName = reader.GetString();
            }
            else
            {
                Debug.Assert(state.Current.JsonClassInfo.ClassType == ClassType.Object);

                state.Current.EndProperty();

                ReadOnlySpan<byte> propertyName = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
                if (options.ReferenceHandling.ShouldReadPreservedReferences())
                {
                    MetadataPropertyName metadata = GetMetadataProperty(propertyName, ref state, ref reader);
                    ResolveMetadataOnObject(propertyName, metadata, ref state, ref reader, options);
                }
                else
                {
                    HandlePropertyNameDefault(propertyName, ref state, ref reader, options);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandlePropertyNameDefault(ReadOnlySpan<byte> propertyName, ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (reader._stringHasEscaping)
            {
                int idx = propertyName.IndexOf(JsonConstants.BackSlash);
                Debug.Assert(idx != -1);
                propertyName = GetUnescapedString(propertyName, idx);
            }

            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonClassInfo!.GetProperty(propertyName, ref state.Current);
            if (jsonPropertyInfo == JsonPropertyInfo.s_missingProperty)
            {
                JsonPropertyInfo? dataExtProperty = state.Current.JsonClassInfo!.DataExtensionProperty;
                if (dataExtProperty == null)
                {
                    state.Current.JsonPropertyInfo = JsonPropertyInfo.s_missingProperty;
                }
                else
                {
                    state.Current.JsonPropertyInfo = dataExtProperty;
                    state.Current.JsonPropertyName = propertyName.ToArray();
                    state.Current.KeyName = JsonHelpers.Utf8GetString(propertyName);
                    state.Current.CollectionPropertyInitialized = true;

                    CreateDataExtensionProperty(dataExtProperty, ref state);
                }
            }
            else
            {
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
            }

            // Increment the PropertyIndex so JsonClassInfo.GetProperty() starts with the next property.
            state.Current.PropertyIndex++;
        }

        private static void CreateDataExtensionProperty(
            JsonPropertyInfo jsonPropertyInfo,
            ref ReadStack state)
        {
            Debug.Assert(jsonPropertyInfo != null);
            Debug.Assert(state.Current.ReturnValue != null);

            IDictionary? extensionData = (IDictionary?)jsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue);
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
                jsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, extensionData);
            }

            // We don't add the value to the dictionary here because we need to support the read-ahead functionality for Streams.
        }

        private static MetadataPropertyName GetMetadataProperty(ReadOnlySpan<byte> propertyName, ref ReadStack state, ref Utf8JsonReader reader)
        {
            if (state.Current.ReferenceId != null)
            {
                ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
            }

            MetadataPropertyName metadata = GetMetadataPropertyName(propertyName, ref state, ref reader);
            return metadata;
        }

        private static void ResolveMetadataOnDictionary(MetadataPropertyName metadata, ref ReadStack state)
        {
            if (metadata == MetadataPropertyName.Id)
            {
                // Check we are not parsing into an immutable dictionary.
                if (state.Current.JsonPropertyInfo!.DictionaryConverter != null)
                {
                    ThrowHelper.ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(state.Current.JsonPropertyInfo.DeclaredPropertyType);
                }

                if (state.Current.KeyName != null)
                {
                    ThrowHelper.ThrowJsonException_MetadataIdIsNotFirstProperty_Dictionary(ref state.Current);
                }
            }
            else if (metadata == MetadataPropertyName.Ref)
            {
                if (state.Current.KeyName != null)
                {
                    ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties_Dictionary(ref state.Current);
                }
            }

            state.Current.LastSeenMetadataProperty = metadata;
        }

        private static void ResolveMetadataOnObject(ReadOnlySpan<byte> propertyName, MetadataPropertyName meta, ref ReadStack state, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (meta == MetadataPropertyName.NoMetadata)
            {
                if (state.Current.IsPreservedArray)
                {
                    ThrowHelper.ThrowJsonException_MetadataPreservedArrayInvalidProperty(reader.GetString()!, GetValuesPropertyInfoFromJsonPreservableArrayRef(ref state.Current).DeclaredPropertyType);
                }

                // Regular property, call main logic for HandlePropertyName.
                HandlePropertyNameDefault(propertyName, ref state, ref reader, options);
            }
            else if (meta == MetadataPropertyName.Id)
            {
                Debug.Assert(propertyName.SequenceEqual(Encoding.UTF8.GetBytes("$id")));

                if (state.Current.PropertyIndex > 0 || state.Current.LastSeenMetadataProperty != MetadataPropertyName.NoMetadata)
                {
                    ThrowHelper.ThrowJsonException_MetadataIdIsNotFirstProperty();
                }

                // TODO: Hook up JsonPropertyInfoAsString here instead.
                // in case read of string value for this property fails.
                JsonPropertyInfo info = JsonPropertyInfo.s_metadataProperty;
                info.JsonPropertyName = ReadStack.s_idMetadataPropertyName;
                state.Current.JsonPropertyInfo = info;
            }
            else if (meta == MetadataPropertyName.Values)
            {
                Debug.Assert(propertyName.SequenceEqual(Encoding.UTF8.GetBytes("$values")));
                // TODO: Hook up JsonPropertyInfoAsString
                // to print $values on the JSON Path in case of failure deeper on the object graph.
                JsonPropertyInfo info = GetValuesPropertyInfoFromJsonPreservableArrayRef(ref state.Current);

                if (info.JsonPropertyName == null)
                {
                    info.JsonPropertyName = ReadStack.s_valuesMetadataPropertyName;
                }

                state.Current.JsonPropertyInfo = info;

                // Throw after setting JsonPropertyInfo to show the correct JSON Path.
                if (state.Current.LastSeenMetadataProperty != MetadataPropertyName.Id)
                {
                    ThrowHelper.ThrowJsonException_MetadataMissingIdBeforeValues();
                }
            }
            else
            {
                Debug.Assert(propertyName.SequenceEqual(Encoding.UTF8.GetBytes("$ref")));
                Debug.Assert(meta == MetadataPropertyName.Ref);

                if (state.Current.JsonClassInfo!.Type.IsValueType)
                {
                    ThrowHelper.ThrowJsonException_MetadataInvalidReferenceToValueType(state.Current.JsonClassInfo.Type);
                }

                if (state.Current.PropertyIndex > 0 || state.Current.LastSeenMetadataProperty != MetadataPropertyName.NoMetadata)
                {
                    ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
                }

                // TODO: Hook up JsonPropertyInfoAsString here instead.
                // in case read of string value for this property fails.

                JsonPropertyInfo info = JsonPropertyInfo.s_metadataProperty;
                info.JsonPropertyName = ReadStack.s_refMetadataPropertyName;
                state.Current.JsonPropertyInfo = info;
            }

            state.Current.LastSeenMetadataProperty = meta;
        }
    }
}
