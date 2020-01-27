// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        internal static bool ResolveMetadata<T>(
            this JsonConverter converter,
            ref Utf8JsonReader reader,
            ref ReadStack state,
            out T value)
        {
            if (state.Current.ObjectState < StackFrameObjectState.MetadataPropertyName)
            {
                // Read the first metadata property name.
                if (!reader.Read())
                {
                    value = default!;
                    return false;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    if (converter.ClassType == ClassType.Enumerable)
                    {
                        ThrowHelper.ThrowJsonException_MetadataPreservedArrayValuesNotFound(converter.TypeToConvert);
                    }
                    else
                    {
                        ThrowHelper.ThrowJsonException_MetadataIdIsNotFirstProperty();
                    }
                }

                ReadOnlySpan<byte> propertyName = GetSpan(ref reader);
                MetadataPropertyName metadata = GetMetadataPropertyName(propertyName);
                state.Current.MetadataPropertyName = metadata;
                if (metadata == MetadataPropertyName.Ref)
                {
                    if (!converter.CanHaveMetadata)
                    {
                        ThrowHelper.ThrowJsonException_MetadataInvalidPropertyWithLeadingDollarSign(propertyName, ref state, reader);
                    }

                    state.Current.ObjectState = StackFrameObjectState.MetadataRefProperty;
                }
                else if (metadata == MetadataPropertyName.Id)
                {
                    if (!converter.CanHaveMetadata)
                    {
                        if ((converter.ClassType & (ClassType.Dictionary | ClassType.Enumerable)) != 0)
                        {
                            ThrowHelper.ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(converter.TypeToConvert);
                        }
                        else
                        {
                            ThrowHelper.ThrowJsonException_MetadataInvalidReferenceToValueType(state.Current.JsonClassInfo.Type);
                        }
                    }

                    state.Current.ObjectState = StackFrameObjectState.MetadataIdProperty;
                }
                else if (metadata == MetadataPropertyName.Values)
                {
                    ThrowHelper.ThrowJsonException_MetadataMissingIdBeforeValues();
                }
                else
                {
                    Debug.Assert(metadata == MetadataPropertyName.NoMetadata);

                    // Having a StartObject without metadata properties is not allowed.
                    if (converter.ClassType == ClassType.Enumerable)
                    {
                        ThrowHelper.ThrowJsonException_MetadataPreservedArrayInvalidProperty(converter.TypeToConvert, reader, ref state);
                    }

                    // Skip the read of the first property name, since we already read it above.
                    state.Current.PropertyState = StackFramePropertyState.ReadName;
                    value = default!;
                    return true;
                }
            }

            if (state.Current.ObjectState == StackFrameObjectState.MetadataRefProperty)
            {
                if (!reader.Read())
                {
                    value = default!;
                    return false;
                }

                if (reader.TokenType != JsonTokenType.String)
                {
                    ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
                }

                string key = reader.GetString()!;

                state.Current.ReturnValue = state.ReferenceResolver.ResolveReferenceOnDeserialize(key!)!;
                state.Current.ObjectState = StackFrameObjectState.MetadataRefPropertyEndObject;
            }
            else if (state.Current.ObjectState == StackFrameObjectState.MetadataIdProperty)
            {
                if (!reader.Read())
                {
                    value = default!;
                    return false;
                }

                if (reader.TokenType != JsonTokenType.String)
                {
                    ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
                }

                string id = reader.GetString()!;
                state.Current.MetadataId = id;

                // Clear the MetadataPropertyName since we are done processing Id.
                state.Current.MetadataPropertyName = MetadataPropertyName.NoMetadata;

                if (converter.ClassType == ClassType.Enumerable && converter.CanHaveValuesMetadata)
                {
                    // Need to Read $values property name.
                    state.Current.ObjectState = StackFrameObjectState.MetadataValuesPropertyName;
                }
                else
                {
                    // We are done reading metadata.
                    state.Current.ObjectState = StackFrameObjectState.MetataPropertyValue;
                }
            }

            if (state.Current.ObjectState == StackFrameObjectState.MetadataRefPropertyEndObject)
            {
                if (!reader.Read())
                {
                    value = default!;
                    return false;
                }

                if (reader.TokenType != JsonTokenType.EndObject)
                {
                    ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(converter.TypeToConvert);
                }

                // Clear the MetadataPropertyName since we are done processing Ref.
                state.Current.MetadataPropertyName = MetadataPropertyName.NoMetadata;

                value = (T)state.Current.ReturnValue!;
                return true;
            }

            if (state.Current.ObjectState == StackFrameObjectState.MetadataValuesPropertyName)
            {
                if (!reader.Read())
                {
                    value = default!;
                    return false;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    ThrowHelper.ThrowJsonException_MetadataPreservedArrayValuesNotFound(converter.TypeToConvert);
                }

                if (reader.GetString() != "$values")
                {
                    ThrowHelper.ThrowJsonException_MetadataPreservedArrayValuesNotFound(converter.TypeToConvert);
                }

                state.Current.MetadataPropertyName = MetadataPropertyName.Values;
                state.Current.ObjectState = StackFrameObjectState.MetadataValuesPropertyStartArray;
            }

            if (state.Current.ObjectState == StackFrameObjectState.MetadataValuesPropertyStartArray)
            {
                if (!reader.Read())
                {
                    value = default!;
                    return false;
                }

                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    ThrowHelper.ThrowJsonException_MetadataPreservedArrayValuesNotFound(converter.TypeToConvert);
                }

                state.Current.ObjectState = StackFrameObjectState.MetataPropertyValue;
            }

            value = default!;
            return true;
        }

        internal static string? GetMetadataPropertyName(in ReadStackFrame frame)
        {
            switch (frame.MetadataPropertyName)
            {
                case MetadataPropertyName.Id:
                    return "$id";

                case MetadataPropertyName.Ref:
                    return "$ref";

                case MetadataPropertyName.Values:
                    return "$values";
            }

            return null;
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
