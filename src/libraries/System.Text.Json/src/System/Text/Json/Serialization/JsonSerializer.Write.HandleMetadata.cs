// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            object currentValue,
            ref WriteStack state,
            Utf8JsonWriter writer)
        {
            MetadataPropertyName metadataToWrite;

            // If the jsonConverter supports immutable dictionaries or value types, don't write any metadata
            if (!jsonConverter.CanHaveIdMetadata || jsonConverter.IsValueType)
            {
                metadataToWrite = MetadataPropertyName.NoMetadata;
            }
            else if (state.ReferenceResolver.TryGetOrAddReferenceOnSerialize(currentValue, out string referenceId))
            {
                Debug.Assert(referenceId != null);
                writer.WriteString(s_metadataRef, referenceId);
                writer.WriteEndObject();
                metadataToWrite = MetadataPropertyName.Ref;
            }
            else
            {
                Debug.Assert(referenceId != null);
                writer.WriteString(s_metadataId, referenceId);
                metadataToWrite = MetadataPropertyName.Id;
            }

            return metadataToWrite;
        }

        internal static MetadataPropertyName WriteReferenceForCollection(
            JsonConverter jsonConverter,
            object currentValue,
            ref WriteStack state,
            Utf8JsonWriter writer)
        {
            MetadataPropertyName metadataToWrite;

            // If the jsonConverter supports immutable enumerables or value type collections, don't write any metadata
            if (!jsonConverter.CanHaveIdMetadata || jsonConverter.IsValueType)
            {
                writer.WriteStartArray();
                metadataToWrite = MetadataPropertyName.NoMetadata;
            }
            else if (state.ReferenceResolver.TryGetOrAddReferenceOnSerialize(currentValue, out string referenceId))
            {
                Debug.Assert(referenceId != null);
                writer.WriteStartObject();
                writer.WriteString(s_metadataRef, referenceId);
                writer.WriteEndObject();
                metadataToWrite = MetadataPropertyName.Ref;
            }
            else
            {
                Debug.Assert(referenceId != null);
                writer.WriteStartObject();
                writer.WriteString(s_metadataId, referenceId);
                writer.WriteStartArray(s_metadataValues);
                metadataToWrite = MetadataPropertyName.Id;
            }

            return metadataToWrite;
        }
    }
}
