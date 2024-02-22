// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        internal const string IdPropertyName = "$id";
        internal const string RefPropertyName = "$ref";
        internal const string TypePropertyName = "$type";
        internal const string ValuesPropertyName = "$values";

        private static readonly byte[] s_idPropertyName = "$id"u8.ToArray();
        private static readonly byte[] s_refPropertyName = "$ref"u8.ToArray();
        private static readonly byte[] s_typePropertyName = "$type"u8.ToArray();
        private static readonly byte[] s_valuesPropertyName = "$values"u8.ToArray();

        internal static bool TryReadMetadata(JsonConverter converter, JsonTypeInfo jsonTypeInfo, ref Utf8JsonReader reader, scoped ref ReadStack state)
        {
            Debug.Assert(state.Current.ObjectState == StackFrameObjectState.StartToken);
            Debug.Assert(state.Current.CanContainMetadata);

            Utf8JsonReader checkpoint;
            bool allowOutOfOrderMetadata = jsonTypeInfo.Options.AllowOutOfOrderMetadataProperties;
            bool isReadingAheadOfNonMetadataProperties = false;

            if (allowOutOfOrderMetadata && !reader.IsFinalBlock)
            {
                // When reading ahead for metadata properties, we need to make sure
                // that the entire object has been buffered so that we can roll back
                // the reader state when the read-ahead has been completed.
                checkpoint = reader;
                if (!checkpoint.TrySkipPartial())
                {
                    return false;
                }
            }
            else
            {
                // Checkpointing not used in this mode.
                checkpoint = default;
            }

            while (true)
            {
                if (state.Current.PropertyState == StackFramePropertyState.None)
                {
                    // Read the property name.
                    if (!reader.Read())
                    {
                        Debug.Assert(!allowOutOfOrderMetadata, "Object must have already been buffered in this mode.");
                        return false;
                    }

                    state.Current.PropertyState = StackFramePropertyState.ReadName;
                }

                if (state.Current.PropertyState < StackFramePropertyState.Name)
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        // Read the entire object while parsing for metadata.
                        goto Done;
                    }

                    // We just read a property. The only valid next tokens are EndObject and PropertyName.
                    Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);

                    if ((state.Current.MetadataPropertyNames & MetadataPropertyName.Ref) != 0)
                    {
                        // No properties whatsoever should follow a $ref property.
                        ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties(reader.GetUnescapedSpan(), ref state);
                    }

                    ReadOnlySpan<byte> propertyName = reader.GetUnescapedSpan();
                    switch (state.Current.LatestMetadataPropertyName = GetMetadataPropertyName(propertyName, jsonTypeInfo.PolymorphicTypeResolver))
                    {
                        case MetadataPropertyName.Id:
                            state.Current.JsonPropertyName = s_idPropertyName;

                            if (state.ReferenceResolver is null)
                            {
                                // Found an $id property in a type that doesn't support reference preservation
                                ThrowHelper.ThrowJsonException_MetadataUnexpectedProperty(propertyName, ref state);
                            }
                            if ((state.Current.MetadataPropertyNames & (MetadataPropertyName.Id | MetadataPropertyName.Ref)) != 0)
                            {
                                // No $id or $ref properties should precede $id properties.
                                ThrowHelper.ThrowJsonException_MetadataIdCannotBeCombinedWithRef(propertyName, ref state);
                            }
                            if (!converter.CanHaveMetadata)
                            {
                                // Should not be permitted unless the converter is capable of handling metadata.
                                ThrowHelper.ThrowJsonException_MetadataCannotParsePreservedObjectIntoImmutable(converter.Type!);
                            }

                            break;

                        case MetadataPropertyName.Ref:
                            state.Current.JsonPropertyName = s_refPropertyName;

                            if (state.ReferenceResolver is null)
                            {
                                // Found a $ref property in a type that doesn't support reference preservation
                                ThrowHelper.ThrowJsonException_MetadataUnexpectedProperty(propertyName, ref state);
                            }
                            if (converter.IsValueType)
                            {
                                // Should not be permitted if the converter is a struct.
                                ThrowHelper.ThrowJsonException_MetadataInvalidReferenceToValueType(converter.Type!);
                            }
                            if (state.Current.MetadataPropertyNames != 0 || isReadingAheadOfNonMetadataProperties)
                            {
                                // No properties should precede a $ref property.
                                ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties(reader.GetUnescapedSpan(), ref state);
                            }

                            break;

                        case MetadataPropertyName.Type:
                            state.Current.JsonPropertyName = jsonTypeInfo.PolymorphicTypeResolver?.CustomTypeDiscriminatorPropertyNameUtf8 ?? s_typePropertyName;

                            if (jsonTypeInfo.PolymorphicTypeResolver is null)
                            {
                                // Found a $type property in a type that doesn't support polymorphism
                                ThrowHelper.ThrowJsonException_MetadataUnexpectedProperty(propertyName, ref state);
                            }
                            if (state.PolymorphicTypeDiscriminator != null)
                            {
                                // Found a duplicate $type property.
                                ThrowHelper.ThrowJsonException_DuplicateMetadataProperty(state.Current.JsonPropertyName);
                            }

                            break;

                        case MetadataPropertyName.Values:
                            state.Current.JsonPropertyName = s_valuesPropertyName;

                            if ((state.Current.MetadataPropertyNames & MetadataPropertyName.Values) != 0)
                            {
                                // Found a duplicate $values property.
                                ThrowHelper.ThrowJsonException_DuplicateMetadataProperty(s_valuesPropertyName);
                            }

                            if (isReadingAheadOfNonMetadataProperties)
                            {
                                // Cannot combine a $values property with other non-metadata properties.
                                ThrowHelper.ThrowJsonException_MetadataInvalidPropertyInArrayMetadata(ref state, jsonTypeInfo.Type, reader);
                            }

                            break;

                        default:
                            Debug.Assert(state.Current.LatestMetadataPropertyName == MetadataPropertyName.None);

                            // Encountered a non-metadata property
                            if (allowOutOfOrderMetadata)
                            {
                                if (!isReadingAheadOfNonMetadataProperties)
                                {
                                    // This is the first non-metadata property, checkpoint the current reader state.
                                    isReadingAheadOfNonMetadataProperties = true;
                                    checkpoint = reader;
                                }

                                if ((state.Current.MetadataPropertyNames & MetadataPropertyName.Values) != 0)
                                {
                                    // Cannot combine a $values property with other non-metadata properties.
                                    ThrowHelper.ThrowJsonException_MetadataInvalidPropertyInArrayMetadata(ref state, jsonTypeInfo.Type, reader);
                                }

                                if (IsMetadataPropertyName(propertyName, resolver: null))
                                {
                                    // properties starting with '$' are reserved for metadata
                                    ThrowHelper.ThrowUnexpectedMetadataException(propertyName, ref reader, ref state);
                                }

                                break;
                            }
                            else
                            {
                                // Exit immediately if no read-ahead is enabled.
                                goto Done;
                            }
                    }

                    state.Current.PropertyState = StackFramePropertyState.Name;
                }

                if (state.Current.PropertyState < StackFramePropertyState.ReadValue)
                {
                    // Read the property value.
                    if (!reader.Read())
                    {
                        Debug.Assert(!allowOutOfOrderMetadata, "Object must have already been buffered in this mode.");
                        return false;
                    }

                    state.Current.PropertyState = StackFramePropertyState.ReadValue;
                }

                Debug.Assert(state.Current.PropertyState == StackFramePropertyState.ReadValue);

                switch (state.Current.LatestMetadataPropertyName)
                {
                    case MetadataPropertyName.Id:
                        if (reader.TokenType != JsonTokenType.String)
                        {
                            ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
                        }

                        if (state.ReferenceId != null)
                        {
                            ThrowHelper.ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotSupported(s_refPropertyName, ref reader, ref state);
                        }

                        state.ReferenceId = reader.GetString();
                        break;

                    case MetadataPropertyName.Ref:
                        if (reader.TokenType != JsonTokenType.String)
                        {
                            ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
                        }

                        if (state.ReferenceId != null)
                        {
                            ThrowHelper.ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotSupported(s_refPropertyName, ref reader, ref state);
                        }

                        state.ReferenceId = reader.GetString();
                        break;

                    case MetadataPropertyName.Type:
                        Debug.Assert(state.PolymorphicTypeDiscriminator == null);

                        switch (reader.TokenType)
                        {
                            case JsonTokenType.String:
                                state.PolymorphicTypeDiscriminator = reader.GetString();
                                break;
                            case JsonTokenType.Number:
                                state.PolymorphicTypeDiscriminator = reader.GetInt32();
                                break;
                            default:
                                ThrowHelper.ThrowJsonException_MetadataValueWasNotString(reader.TokenType);
                                break;
                        }

                        break;

                    case MetadataPropertyName.Values:

                        if (reader.TokenType != JsonTokenType.StartArray)
                        {
                            ThrowHelper.ThrowJsonException_MetadataValuesInvalidToken(reader.TokenType);
                        }

                        if (allowOutOfOrderMetadata)
                        {
                            // The $values property contains the collection payload,
                            // checkpoint the current reader and continue the read-ahead.
                            Debug.Assert(!isReadingAheadOfNonMetadataProperties, "must have already verified if non-metadata properties precede $values");
                            isReadingAheadOfNonMetadataProperties = true;
                            checkpoint = reader;
                            reader.SkipWithVerify();
                        }
                        else
                        {
                            state.Current.MetadataPropertyNames |= MetadataPropertyName.Values;
                            state.Current.PropertyState = StackFramePropertyState.None;
                            goto Done; // "$values" property contains the nested payload, exit the metadata reader now.
                        }

                        break;

                    default:
                        Debug.Assert(state.Current.LatestMetadataPropertyName == MetadataPropertyName.None);
                        Debug.Assert(allowOutOfOrderMetadata, "should only be reached if reading ahead is required.");
                        reader.SkipWithVerify();

                        break;
                }

                state.Current.MetadataPropertyNames |= state.Current.LatestMetadataPropertyName;
                state.Current.PropertyState = StackFramePropertyState.None;
                state.Current.JsonPropertyName = null;
            }

        Done:
            if (state.Current.MetadataPropertyNames is MetadataPropertyName.Values)
            {
                // Cannot have a $values property unless there are other metadata properties.
                ThrowHelper.ThrowJsonException_MetadataStandaloneValuesProperty(ref state, s_valuesPropertyName);
            }

            if (isReadingAheadOfNonMetadataProperties)
            {
                // Roll back the reader state to the first non-metadata property.
                reader = checkpoint;
            }

            return true;
        }

        internal static bool IsMetadataPropertyName(ReadOnlySpan<byte> propertyName, PolymorphicTypeResolver? resolver)
        {
            return
                (propertyName.Length > 0 && propertyName[0] == '$') ||
                (resolver?.CustomTypeDiscriminatorPropertyNameUtf8?.AsSpan().SequenceEqual(propertyName) == true);
        }

        internal static MetadataPropertyName GetMetadataPropertyName(ReadOnlySpan<byte> propertyName, PolymorphicTypeResolver? resolver)
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

                    case 5 when resolver?.CustomTypeDiscriminatorPropertyNameUtf8 is null:
                        if (propertyName[1] == 't' &&
                            propertyName[2] == 'y' &&
                            propertyName[3] == 'p' &&
                            propertyName[4] == 'e')
                        {
                            return MetadataPropertyName.Type;
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

            if (resolver?.CustomTypeDiscriminatorPropertyNameUtf8 is byte[] customTypeDiscriminator &&
                propertyName.SequenceEqual(customTypeDiscriminator))
            {
                return MetadataPropertyName.Type;
            }

            return MetadataPropertyName.None;
        }

        internal static bool TryHandleReferenceFromJsonElement(
            ref Utf8JsonReader reader,
            scoped ref ReadStack state,
            JsonElement element,
            [NotNullWhen(true)] out object? referenceValue)
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
                    else if (property.EscapedNameEquals(s_idPropertyName))
                    {
                        if (state.ReferenceId != null)
                        {
                            ThrowHelper.ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotSupported(s_refPropertyName, ref reader, ref state);
                        }

                        if (property.Value.ValueKind != JsonValueKind.String)
                        {
                            ThrowHelper.ThrowJsonException_MetadataValueWasNotString(property.Value.ValueKind);
                        }

                        object boxedElement = element;
                        state.ReferenceResolver.AddReference(property.Value.GetString()!, boxedElement);
                        referenceValue = boxedElement;
                        return true;
                    }
                    else if (property.EscapedNameEquals(s_refPropertyName))
                    {
                        if (state.ReferenceId != null)
                        {
                            ThrowHelper.ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotSupported(s_refPropertyName, ref reader, ref state);
                        }

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

        internal static bool TryHandleReferenceFromJsonNode(
            ref Utf8JsonReader reader,
            scoped ref ReadStack state,
            JsonNode? jsonNode,
            [NotNullWhen(true)] out object? referenceValue)
        {
            bool refMetadataFound = false;
            referenceValue = default;

            if (jsonNode is JsonObject jsonObject)
            {
                int propertyCount = 0;
                foreach (KeyValuePair<string, JsonNode?> property in jsonObject)
                {
                    propertyCount++;
                    if (refMetadataFound)
                    {
                        // There are more properties in an object with $ref.
                        ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
                    }
                    else if (property.Key == "$id")
                    {
                        if (state.ReferenceId != null)
                        {
                            ThrowHelper.ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotSupported(s_refPropertyName, ref reader, ref state);
                        }

                        string referenceId = ReadAsStringMetadataValue(property.Value);
                        state.ReferenceResolver.AddReference(referenceId, jsonNode);
                        referenceValue = jsonNode;
                        return true;
                    }
                    else if (property.Key == "$ref")
                    {
                        if (state.ReferenceId != null)
                        {
                            ThrowHelper.ThrowNotSupportedException_ObjectWithParameterizedCtorRefMetadataNotSupported(s_refPropertyName, ref reader, ref state);
                        }

                        if (propertyCount > 1)
                        {
                            // $ref was found but there were other properties before.
                            ThrowHelper.ThrowJsonException_MetadataReferenceObjectCannotContainOtherProperties();
                        }

                        string referenceId = ReadAsStringMetadataValue(property.Value);
                        referenceValue = state.ReferenceResolver.ResolveReference(referenceId);
                        refMetadataFound = true;
                    }

                    static string ReadAsStringMetadataValue(JsonNode? jsonNode)
                    {
                        if (jsonNode is JsonValue jsonValue &&
                            jsonValue.TryGetValue(out string? value) &&
                            value is not null)
                        {
                            return value;
                        }

                        JsonValueKind metadataValueKind = jsonNode switch
                        {
                            null => JsonValueKind.Null,
                            JsonObject => JsonValueKind.Object,
                            JsonArray => JsonValueKind.Array,
                            JsonValue<JsonElement> element => element.Value.ValueKind,
                            _ => JsonValueKind.Undefined,
                        };

                        Debug.Assert(metadataValueKind != JsonValueKind.Undefined);
                        ThrowHelper.ThrowJsonException_MetadataValueWasNotString(metadataValueKind);
                        return null!;
                    }
                }
            }

            return refMetadataFound;
        }

        internal static void ValidateMetadataForObjectConverter(ref ReadStack state)
        {
            if ((state.Current.MetadataPropertyNames & MetadataPropertyName.Values) != 0)
            {
                // Object converters do not support $values metadata.
                ThrowHelper.ThrowJsonException_MetadataUnexpectedProperty(s_valuesPropertyName, ref state);
            }
        }

        internal static void ValidateMetadataForArrayConverter(JsonConverter converter, ref Utf8JsonReader reader, scoped ref ReadStack state)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartArray:
                    Debug.Assert(state.Current.MetadataPropertyNames is MetadataPropertyName.None || state.Current.MetadataPropertyNames.HasFlag(MetadataPropertyName.Values));
                    break;

                case JsonTokenType.EndObject:
                    if (state.Current.MetadataPropertyNames != MetadataPropertyName.Ref)
                    {
                        // Read the entire JSON object while parsing for metadata: for collection converters this is only legal for $ref nodes.
                        ThrowHelper.ThrowJsonException_MetadataPreservedArrayValuesNotFound(ref state, converter.Type!);
                    }
                    break;

                default:
                    Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
                    // Do not tolerate non-metadata properties in collection converters.
                    ThrowHelper.ThrowJsonException_MetadataInvalidPropertyInArrayMetadata(ref state, converter.Type!, reader);
                    break;
            }
        }

        internal static T ResolveReferenceId<T>(ref ReadStack state)
        {
            Debug.Assert(!typeof(T).IsValueType);
            Debug.Assert(state.ReferenceId != null);

            string referenceId = state.ReferenceId;
            object value = state.ReferenceResolver.ResolveReference(referenceId);
            state.ReferenceId = null;

            try
            {
                return (T)value;
            }
            catch (InvalidCastException)
            {
                ThrowHelper.ThrowInvalidOperationException_MetadataReferenceOfTypeCannotBeAssignedToType(
                    referenceId, value.GetType(), typeof(T));
                return default!;
            }
        }
    }
}
