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

        internal static MetadataPropertyName WriteReferenceForObject(
            JsonConverter jsonConverter,
            ref WriteStack state,
            Utf8JsonWriter writer)
        {
            if (state.NewReferenceId != null)
            {
                Debug.Assert(jsonConverter.CanHaveMetadata);
                writer.WriteString(s_metadataId, state.NewReferenceId);
                state.NewReferenceId = null;
                return MetadataPropertyName.Id;
            }

            return MetadataPropertyName.None;
        }

        internal static MetadataPropertyName WriteReferenceForCollection(
            JsonConverter jsonConverter,
            ref WriteStack state,
            Utf8JsonWriter writer)
        {
            if (state.NewReferenceId != null)
            {
                Debug.Assert(jsonConverter.CanHaveMetadata);
                writer.WriteStartObject();
                writer.WriteString(s_metadataId, state.NewReferenceId);
                writer.WriteStartArray(s_metadataValues);
                state.NewReferenceId = null;
                return MetadataPropertyName.Id;
            }

            // If the jsonConverter supports immutable enumerables or value type collections, don't write any metadata
            writer.WriteStartArray();
            return MetadataPropertyName.None;
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
