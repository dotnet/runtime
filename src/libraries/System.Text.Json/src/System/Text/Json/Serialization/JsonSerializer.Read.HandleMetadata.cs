// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Returns true if successful, false is the reader ran out of buffer.
        /// Sets state.Current.ReturnValue to the $ref target for MetadataRefProperty cases.
        /// </summary>
        internal static bool ResolveMetadata(
            JsonConverter converter,
            ref Utf8JsonReader reader,
            ref ReadStack state)
        {
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
                    // An enumerable needs metadata since it starts with StartObject.
                    if (converter.ClassType == ClassType.Enumerable)
                    {
                        ThrowHelper.ThrowJsonException_MetadataPreservedArrayValuesNotFound(converter.TypeToConvert);
                    }

                    // The reader should have detected other invalid cases.
                    Debug.Assert(reader.TokenType == JsonTokenType.EndObject);

                    // Skip the read of the first property name, since we already read it above.
                    state.Current.PropertyState = StackFramePropertyState.ReadName;

                    return true;
                }

                ReadOnlySpan<byte> propertyName = reader.GetSpan();
                MetadataPropertyName metadata = GetMetadataPropertyName(propertyName);
                if (metadata == MetadataPropertyName.Id)
                {
                    state.Current.JsonPropertyName = propertyName.ToArray();
                    if (!converter.CanHaveIdMetadata)
                    {
                        ThrowHelper.ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(converter.TypeToConvert);
                    }

                    state.Current.ObjectState = StackFrameObjectState.ReadAheadIdValue;
                }
                else if (metadata == MetadataPropertyName.Ref)
                {
                    state.Current.JsonPropertyName = propertyName.ToArray();
                    if (converter.IsValueType)
                    {
                        ThrowHelper.ThrowJsonException_MetadataInvalidReferenceToValueType(converter.TypeToConvert);
                    }

                    state.Current.ObjectState = StackFrameObjectState.ReadAheadRefValue;
                }
                else if (metadata == MetadataPropertyName.Values)
                {
                    state.Current.JsonPropertyName = propertyName.ToArray();
                    if (converter.ClassType == ClassType.Enumerable)
                    {
                        ThrowHelper.ThrowJsonException_MetadataMissingIdBeforeValues();
                    }
                    else
                    {
                        ThrowHelper.ThrowJsonException_MetadataInvalidPropertyWithLeadingDollarSign(propertyName, ref state, reader);
                    }
                }
                else
                {
                    Debug.Assert(metadata == MetadataPropertyName.NoMetadata);

                    // Having a StartObject without metadata properties is not allowed.
                    if (converter.ClassType == ClassType.Enumerable)
                    {
                        state.Current.JsonPropertyName = propertyName.ToArray();
                        ThrowHelper.ThrowJsonException_MetadataPreservedArrayInvalidProperty(converter.TypeToConvert, reader);
                    }

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

                string key = reader.GetString()!;

                // todo: https://github.com/dotnet/runtime/issues/32354
                state.Current.ReturnValue = state.ReferenceResolver.ResolveReferenceOnDeserialize(key);
                state.Current.ObjectState = StackFrameObjectState.ReadAheadRefEndObject;
            }
            else if (state.Current.ObjectState == StackFrameObjectState.ReadIdValue)
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
                }

                state.Current.MetadataId = reader.GetString();

                // Clear the MetadataPropertyName since we are done processing Id.
                state.Current.JsonPropertyName = default;

                if (converter.ClassType == ClassType.Enumerable)
                {
                    // Need to Read $values property name.
                    state.Current.ObjectState = StackFrameObjectState.ReadAheadValuesName;
                }
                else
                {
                    // We are done reading metadata.
                    state.Current.ObjectState = StackFrameObjectState.PropertyValue;
                    return true;
                }
            }

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
                    ThrowHelper.ThrowJsonException_MetadataPreservedArrayValuesNotFound(converter.TypeToConvert);
                }

                ReadOnlySpan<byte> propertyName = reader.GetSpan();

                // Remember the property in case we get an exception.
                state.Current.JsonPropertyName = propertyName.ToArray();

                if (GetMetadataPropertyName(propertyName) != MetadataPropertyName.Values)
                {
                    ThrowHelper.ThrowJsonException_MetadataPreservedArrayInvalidProperty(converter.TypeToConvert, reader);
                }

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

                state.Current.ObjectState = StackFrameObjectState.PropertyValue;
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
    }
}
