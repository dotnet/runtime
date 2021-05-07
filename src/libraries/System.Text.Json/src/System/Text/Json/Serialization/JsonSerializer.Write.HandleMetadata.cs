// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        // Pre-encoded metadata properties.
        internal static readonly JsonEncodedText s_metadataId = JsonEncodedText.Encode("$id", encoder: null);
        internal static readonly JsonEncodedText s_metadataRef = JsonEncodedText.Encode("$ref", encoder: null);
        internal static readonly JsonEncodedText s_metadataValues = JsonEncodedText.Encode("$values", encoder: null);
        internal static readonly JsonEncodedText s_metadataType = JsonEncodedText.Encode("$type", encoder: null);

        internal static MetadataPropertyName WriteMetadataForObject(
            JsonConverter jsonConverter,
            object currentValue,
            ref WriteStack state,
            Utf8JsonWriter writer,
            ReferenceHandlingStrategy referenceHandlingStrategy)
        {
            Debug.Assert(referenceHandlingStrategy == ReferenceHandlingStrategy.Preserve || state.PolymorphicTypeDiscriminator is not null);
            MetadataPropertyName writtenMetadataName;

            if (referenceHandlingStrategy == ReferenceHandlingStrategy.Preserve)
            {
                // If the jsonConverter supports immutable dictionaries or value types, don't write any metadata
                if (!jsonConverter.CanHaveIdMetadata || jsonConverter.IsValueType)
                {
                    writtenMetadataName = MetadataPropertyName.NoMetadata;
                }
                else
                {
                    string referenceId = state.ReferenceResolver.GetReference(currentValue, out bool alreadyExists);
                    Debug.Assert(referenceId != null);

                    if (alreadyExists)
                    {
                        writer.WriteString(s_metadataRef, referenceId);
                        writer.WriteEndObject();
                        writtenMetadataName = MetadataPropertyName.Ref;
                    }
                    else
                    {
                        writtenMetadataName = MetadataPropertyName.Id;

                        // write the typeId ahead of the referenceId
                        if (state.PolymorphicTypeDiscriminator is string typeId)
                        {
                            writer.WriteString(s_TypeIdPropertyName, typeId);
                            writtenMetadataName |= MetadataPropertyName.Type;
                            state.PolymorphicTypeDiscriminator = null;
                        }

                        writer.WriteString(s_metadataId, referenceId);
                    }
                }
            }
            else
            {
                Debug.Assert(state.PolymorphicTypeDiscriminator is not null);
                writer.WriteString(s_TypeIdPropertyName, state.PolymorphicTypeDiscriminator);
                writtenMetadataName = MetadataPropertyName.Type;
                state.PolymorphicTypeDiscriminator = null;
            }

            return writtenMetadataName;
        }

        internal static MetadataPropertyName WriteMetadataForCollection(
            JsonConverter jsonConverter,
            object currentValue,
            ref WriteStack state,
            Utf8JsonWriter writer,
            ReferenceHandlingStrategy referenceHandlingStrategy)
        {
            Debug.Assert(referenceHandlingStrategy == ReferenceHandlingStrategy.Preserve || state.PolymorphicTypeDiscriminator is not null);
            MetadataPropertyName writtenMetadataName;

            if (referenceHandlingStrategy == ReferenceHandlingStrategy.Preserve)
            {
                // If the jsonConverter supports immutable enumerables or value type collections, don't write any metadata
                if (!jsonConverter.CanHaveIdMetadata || jsonConverter.IsValueType)
                {
                    writer.WriteStartArray();
                    writtenMetadataName = MetadataPropertyName.NoMetadata;
                }
                else
                {
                    string referenceId = state.ReferenceResolver.GetReference(currentValue, out bool alreadyExists);
                    Debug.Assert(referenceId != null);

                    if (alreadyExists)
                    {
                        writer.WriteStartObject();
                        writer.WriteString(s_metadataRef, referenceId);
                        writer.WriteEndObject();
                        writtenMetadataName = MetadataPropertyName.Ref;
                    }
                    else
                    {
                        writtenMetadataName = MetadataPropertyName.Id;
                        writer.WriteStartObject();

                        // write the typeId ahead of the referenceId
                        if (state.PolymorphicTypeDiscriminator is string typeId)
                        {
                            writer.WriteString(s_TypeIdPropertyName, typeId);
                            writtenMetadataName |= MetadataPropertyName.Type;
                            state.PolymorphicTypeDiscriminator = null;
                        }

                        writer.WriteString(s_metadataId, referenceId);
                        writer.WriteStartArray(s_metadataValues);
                    }
                }
            }
            else
            {
                Debug.Assert(state.PolymorphicTypeDiscriminator is not null);

                writer.WriteStartObject();
                writer.WriteString(s_TypeIdPropertyName, state.PolymorphicTypeDiscriminator);
                writer.WriteStartArray(s_metadataValues);
                state.PolymorphicTypeDiscriminator = null;
                writtenMetadataName = MetadataPropertyName.Type;
            }

            return writtenMetadataName;
        }

        internal static void WriteTypeMetata(Utf8JsonWriter writer, string typeId)
        {
            writer.WriteString(s_metadataType, typeId);
        }
    }
}
