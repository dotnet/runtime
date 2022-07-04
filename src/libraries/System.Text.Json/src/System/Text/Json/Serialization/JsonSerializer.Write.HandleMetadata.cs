// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        // Pre-encoded metadata properties.
        internal static readonly JsonEncodedText s_metadataId = JsonEncodedText.Encode(IdPropertyName, encoder: null);
        internal static readonly JsonEncodedText s_metadataRef = JsonEncodedText.Encode(RefPropertyName, encoder: null);
        internal static readonly JsonEncodedText s_metadataType = JsonEncodedText.Encode(TypePropertyName, encoder: null);
        internal static readonly JsonEncodedText s_metadataValues = JsonEncodedText.Encode(ValuesPropertyName, encoder: null);

        internal static MetadataPropertyName WriteMetadataForObject(
            JsonConverter jsonConverter,
            ref WriteStack state,
            Utf8JsonWriter writer)
        {
            Debug.Assert(jsonConverter.CanHaveMetadata);
            Debug.Assert(!state.IsContinuation);
            Debug.Assert(state.CurrentContainsMetadata);

            MetadataPropertyName writtenMetadata = MetadataPropertyName.None;

            if (state.NewReferenceId != null)
            {
                writer.WriteString(s_metadataId, state.NewReferenceId);
                writtenMetadata |= MetadataPropertyName.Id;
                state.NewReferenceId = null;
            }

            if (state.PolymorphicTypeDiscriminator is object discriminator)
            {
                Debug.Assert(state.Parent.JsonPropertyInfo!.JsonTypeInfo.PolymorphicTypeResolver != null);

                JsonEncodedText propertyName =
                    state.Parent.JsonPropertyInfo.JsonTypeInfo.PolymorphicTypeResolver.CustomTypeDiscriminatorPropertyNameJsonEncoded is JsonEncodedText customPropertyName
                    ? customPropertyName
                    : s_metadataType;

                if (discriminator is string stringId)
                {
                    writer.WriteString(propertyName, stringId);
                }
                else
                {
                    Debug.Assert(discriminator is int);
                    writer.WriteNumber(propertyName, (int)discriminator);
                }

                writtenMetadata |= MetadataPropertyName.Type;
                state.PolymorphicTypeDiscriminator = null;
            }

            Debug.Assert(writtenMetadata != MetadataPropertyName.None);
            return writtenMetadata;
        }

        internal static MetadataPropertyName WriteMetadataForCollection(
            JsonConverter jsonConverter,
            ref WriteStack state,
            Utf8JsonWriter writer)
        {
            // For collections with metadata, we nest the array payload within a JSON object.
            writer.WriteStartObject();
            MetadataPropertyName writtenMetadata = WriteMetadataForObject(jsonConverter, ref state, writer);
            writer.WritePropertyName(s_metadataValues); // property name containing nested array values.
            return writtenMetadata;
        }

        /// <summary>
        /// Compute reference id for the next value to be serialized.
        /// </summary>
        internal static bool TryGetReferenceForValue(object currentValue, ref WriteStack state, Utf8JsonWriter writer)
        {
            Debug.Assert(state.NewReferenceId == null);

            string referenceId = state.ReferenceResolver.GetReference(currentValue, out bool alreadyExists);
            Debug.Assert(referenceId != null);

            if (alreadyExists)
            {
                // Instance already serialized, write as { "$ref" : "referenceId" }
                writer.WriteStartObject();
                writer.WriteString(s_metadataRef, referenceId);
                writer.WriteEndObject();

                state.PolymorphicTypeDiscriminator = null; // clear out any polymorphism state.
            }
            else
            {
                // New instance, store computed reference id in the state
                state.NewReferenceId = referenceId;
            }

            return alreadyExists;
        }
    }
}
