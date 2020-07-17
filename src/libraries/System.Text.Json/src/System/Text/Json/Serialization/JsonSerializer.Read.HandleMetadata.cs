// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        internal static readonly byte[] s_idPropertyName
            = new byte[] { (byte)'$', (byte)'i', (byte)'d' };

        internal static readonly byte[] s_refPropertyName
            = new byte[] { (byte)'$', (byte)'r', (byte)'e', (byte)'f' };

        internal static readonly byte[] s_valuesPropertyName
            = new byte[] { (byte)'$', (byte)'v', (byte)'a', (byte)'l', (byte)'u', (byte)'e', (byte)'s' };

        /// <summary>
        /// Returns true if successful, false is the reader ran out of buffer.
        /// Sets state.Current.ReturnValue to the reference target for $ref cases;
        /// Sets state.Current.ReturnValue to a new instance for $id cases.
        /// </summary>
        internal static bool ResolveMetadataForJsonObject(
            ref Utf8JsonReader reader,
            ref ReadStack state,
            JsonSerializerOptions options)
        {
            JsonConverter converter = state.Current.JsonClassInfo.PropertyInfoForClassInfo.ConverterBase;

            if (state.Current.ObjectState < StackFrameObjectState.ReadAheadNameOrEndObject)
            {
                // Read the first metadata property name.
                if (!TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadNameOrEndObject))
                {
                    return false;
                }
            }

            if (state.Current.ObjectState == StackFrameObjectState.ReadNameOrEndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    // Since this was an empty object, we are done reading metadata.
                    state.Current.ObjectState = StackFrameObjectState.PropertyValue;
                    // Skip the read of the first property name, since we already read it above.
                    state.Current.PropertyState = StackFramePropertyState.ReadName;
                    return true;
                }

                ReadOnlySpan<byte> propertyName = reader.GetSpan();
                MetadataPropertyName metadata = GetMetadataPropertyName(propertyName);
                if (metadata == MetadataPropertyName.Id)
                {
                    state.Current.JsonPropertyName = s_idPropertyName;

                    if (!converter.CanHaveIdMetadata)
                    {
                        ThrowHelper.ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(converter.TypeToConvert);
                    }

                    state.Current.ObjectState = StackFrameObjectState.ReadAheadIdValue;
                }
                else if (metadata == MetadataPropertyName.Ref)
                {
                    state.Current.JsonPropertyName = s_refPropertyName;

                    if (converter.IsValueType)
                    {
                        ThrowHelper.ThrowJsonException_MetadataInvalidReferenceToValueType(converter.TypeToConvert);
                    }

                    state.Current.ObjectState = StackFrameObjectState.ReadAheadRefValue;
                }
                else if (metadata == MetadataPropertyName.Values)
                {
                    ThrowHelper.ThrowJsonException_MetadataInvalidPropertyWithLeadingDollarSign(propertyName, ref state, reader);
                }
                else
                {
                    Debug.Assert(metadata == MetadataPropertyName.NoMetadata);
                    // We are done reading metadata, the object didn't contain any.
                    state.Current.ObjectState = StackFrameObjectState.PropertyValue;
                    // Skip the read of the first property name, since we already read it above.
                    state.Current.PropertyState = StackFramePropertyState.ReadName;
                    return true;
                }
            }

            if (state.Current.ObjectState == StackFrameObjectState.ReadAheadRefValue)
            {
                if (!TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadRefValue))
                {
                    return false;
                }
            }
            else if (state.Current.ObjectState == StackFrameObjectState.ReadAheadIdValue)
            {
                if (!TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadIdValue))
                {
                    return false;
                }
            }

            if (state.Current.ObjectState == StackFrameObjectState.ReadRefValue)
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
                }

                string referenceId = reader.GetString()!;

                // todo: https://github.com/dotnet/runtime/issues/32354
                state.Current.ReturnValue = state.ReferenceResolver.ResolveReference(referenceId);

                state.Current.ObjectState = StackFrameObjectState.ReadAheadRefEndObject;
            }
            else if (state.Current.ObjectState == StackFrameObjectState.ReadIdValue)
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
                }

                converter.CreateInstanceForReferenceResolver(ref reader, ref state, options);

                string referenceId = reader.GetString()!;
                state.ReferenceResolver.AddReference(referenceId, state.Current.ReturnValue!);

                // We are done reading metadata plus we instantiated the object.
                state.Current.ObjectState = StackFrameObjectState.CreatedObject;
            }

            // Clear the metadata property name that was set in case of failure on ResolveReference/AddReference.
            state.Current.JsonPropertyName = null;

            if (state.Current.ObjectState == StackFrameObjectState.ReadAheadRefEndObject)
            {
                if (!TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadRefEndObject))
                {
                    return false;
                }
            }

            if (state.Current.ObjectState == StackFrameObjectState.ReadRefEndObject)
            {
                if (reader.TokenType != JsonTokenType.EndObject)
                {
                    // We just read a property. The only valid next tokens are EndObject and PropertyName.
                    Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);

                    ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties(reader.GetSpan(), ref state);
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if successful, false is the reader ran out of buffer.
        /// Sets state.Current.ReturnValue to the reference target for $ref cases;
        /// Sets state.Current.ReturnValue to a new instance for $id cases.
        /// </summary>
        internal static bool ResolveMetadataForJsonArray(
            ref Utf8JsonReader reader,
            ref ReadStack state,
            JsonSerializerOptions options)
        {
            JsonConverter converter = state.Current.JsonClassInfo.PropertyInfoForClassInfo.ConverterBase;

            if (state.Current.ObjectState < StackFrameObjectState.ReadAheadNameOrEndObject)
            {
                // Read the first metadata property name.
                if (!TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadNameOrEndObject))
                {
                    return false;
                }
            }

            if (state.Current.ObjectState == StackFrameObjectState.ReadNameOrEndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    // The reader should have detected other invalid cases.
                    Debug.Assert(reader.TokenType == JsonTokenType.EndObject);

                    // An enumerable needs metadata since it starts with StartObject.
                    ThrowHelper.ThrowJsonException_MetadataPreservedArrayValuesNotFound(ref state, converter.TypeToConvert);
                }


                ReadOnlySpan<byte> propertyName = reader.GetSpan();
                MetadataPropertyName metadata = GetMetadataPropertyName(propertyName);
                if (metadata == MetadataPropertyName.Id)
                {
                    state.Current.JsonPropertyName = s_idPropertyName;

                    if (!converter.CanHaveIdMetadata)
                    {
                        ThrowHelper.ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(converter.TypeToConvert);
                    }

                    state.Current.ObjectState = StackFrameObjectState.ReadAheadIdValue;
                }
                else if (metadata == MetadataPropertyName.Ref)
                {
                    state.Current.JsonPropertyName = s_refPropertyName;

                    if (converter.IsValueType)
                    {
                        ThrowHelper.ThrowJsonException_MetadataInvalidReferenceToValueType(converter.TypeToConvert);
                    }

                    state.Current.ObjectState = StackFrameObjectState.ReadAheadRefValue;
                }
                else if (metadata == MetadataPropertyName.Values)
                {
                    ThrowHelper.ThrowJsonException_MetadataMissingIdBeforeValues(ref state, propertyName);
                }
                else
                {
                    Debug.Assert(metadata == MetadataPropertyName.NoMetadata);

                    // Having a StartObject without metadata properties is not allowed.
                    ThrowHelper.ThrowJsonException_MetadataPreservedArrayInvalidProperty(ref state, converter.TypeToConvert, reader);
                }
            }

            if (state.Current.ObjectState == StackFrameObjectState.ReadAheadRefValue)
            {
                if (!TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadRefValue))
                {
                    return false;
                }
            }
            else if (state.Current.ObjectState == StackFrameObjectState.ReadAheadIdValue)
            {
                if (!TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadIdValue))
                {
                    return false;
                }
            }

            if (state.Current.ObjectState == StackFrameObjectState.ReadRefValue)
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
                }

                string key = reader.GetString()!;

                // todo: https://github.com/dotnet/runtime/issues/32354
                state.Current.ReturnValue = state.ReferenceResolver.ResolveReference(key);
                state.Current.ObjectState = StackFrameObjectState.ReadAheadRefEndObject;
            }
            else if (state.Current.ObjectState == StackFrameObjectState.ReadIdValue)
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
                }

                converter.CreateInstanceForReferenceResolver(ref reader, ref state, options);

                string referenceId = reader.GetString()!;
                state.ReferenceResolver.AddReference(referenceId, state.Current.ReturnValue!);

                // Need to Read $values property name.
                state.Current.ObjectState = StackFrameObjectState.ReadAheadValuesName;
            }

            // Clear the metadata property name that was set in case of failure on ResolverReference/AddReference.
            state.Current.JsonPropertyName = null;

            if (state.Current.ObjectState == StackFrameObjectState.ReadAheadRefEndObject)
            {
                if (!TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadRefEndObject))
                {
                    return false;
                }
            }

            if (state.Current.ObjectState == StackFrameObjectState.ReadRefEndObject)
            {
                if (reader.TokenType != JsonTokenType.EndObject)
                {
                    // We just read a property. The only valid next tokens are EndObject and PropertyName.
                    Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);

                    ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties(reader.GetSpan(), ref state);
                }

                return true;
            }

            if (state.Current.ObjectState == StackFrameObjectState.ReadAheadValuesName)
            {
                if (!TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadValuesName))
                {
                    return false;
                }
            }

            if (state.Current.ObjectState == StackFrameObjectState.ReadValuesName)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    ThrowHelper.ThrowJsonException_MetadataPreservedArrayValuesNotFound(ref state, converter.TypeToConvert);
                }

                ReadOnlySpan<byte> propertyName = reader.GetSpan();

                if (GetMetadataPropertyName(propertyName) != MetadataPropertyName.Values)
                {
                    ThrowHelper.ThrowJsonException_MetadataPreservedArrayInvalidProperty(ref state, converter.TypeToConvert, reader);
                }

                // Remember the property in case we get an exception in one of the array elements.
                state.Current.JsonPropertyName = s_valuesPropertyName;

                state.Current.ObjectState = StackFrameObjectState.ReadAheadValuesStartArray;
            }

            if (state.Current.ObjectState == StackFrameObjectState.ReadAheadValuesStartArray)
            {
                if (!TryReadAheadMetadataAndSetState(ref reader, ref state, StackFrameObjectState.ReadValuesStartArray))
                {
                    return false;
                }
            }

            if (state.Current.ObjectState == StackFrameObjectState.ReadValuesStartArray)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    ThrowHelper.ThrowJsonException_MetadataValuesInvalidToken(reader.TokenType);
                }
                state.Current.ValidateEndTokenOnArray = true;
                state.Current.ObjectState = StackFrameObjectState.CreatedObject;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryReadAheadMetadataAndSetState(ref Utf8JsonReader reader, ref ReadStack state, StackFrameObjectState nextState)
        {
            // If we can't read here, the read will be completed at the root API by asking the stream for more data.
            // Set the state so we know where to resume on re-entry.
            state.Current.ObjectState = nextState;
            return reader.Read();
        }

        internal static MetadataPropertyName GetMetadataPropertyName(ReadOnlySpan<byte> propertyName)
        {
            if (propertyName.Length > 0 && propertyName[0] == '$')
            {
                switch (propertyName.Length)
                {
                    case 3:
                        if (propertyName[1] == 'i' &&
                            propertyName[2] == 'd')
                        {
                            return MetadataPropertyName.Id;
                        }
                        break;

                    case 4:
                        if (propertyName[1] == 'r' &&
                            propertyName[2] == 'e' &&
                            propertyName[3] == 'f')
                        {
                            return MetadataPropertyName.Ref;
                        }
                        break;

                    case 7:
                        if (propertyName[1] == 'v' &&
                            propertyName[2] == 'a' &&
                            propertyName[3] == 'l' &&
                            propertyName[4] == 'u' &&
                            propertyName[5] == 'e' &&
                            propertyName[6] == 's')
                        {
                            return MetadataPropertyName.Values;
                        }
                        break;
                }
            }

            return MetadataPropertyName.NoMetadata;
        }

        internal static bool TryGetReferenceFromJsonElement(
            ref ReadStack state,
            JsonElement element,
            out object? referenceValue)
        {
            bool refMetadataFound = false;
            referenceValue = default;

            if (element.ValueKind == JsonValueKind.Object)
            {
                int propertyCount = 0;
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    propertyCount++;
                    if (refMetadataFound)
                    {
                        // There are more properties in an object with $ref.
                        ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
                    }
                    else if (property.EscapedNameEquals(s_refPropertyName))
                    {
                        if (propertyCount > 1)
                        {
                            // $ref was found but there were other properties before.
                            ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
                        }

                        if (property.Value.ValueKind != JsonValueKind.String)
                        {
                            ThrowHelper.ThrowJsonException_MetadataValueWasNotString(property.Value.ValueKind);
                        }

                        referenceValue = state.ReferenceResolver.ResolveReference(property.Value.GetString()!);
                        refMetadataFound = true;
                    }
                }
            }

            return refMetadataFound;
        }
    }
}
