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

        internal static MetadataPropertyName GetResolvedReferenceHandling(
            JsonConverter converter,
            object value,
            ref WriteStack state,
            out string? referenceId)
        {
            if (!converter.CanHaveIdMetadata || converter.TypeToConvert.IsValueType)
            {
                referenceId = default;
                return MetadataPropertyName.NoMetadata;
            }

            if (state.ReferenceResolver.TryGetOrAddReferenceOnSerialize(value, out referenceId))
            {
                return MetadataPropertyName.Ref;
            }

            return MetadataPropertyName.Id;
        }

        internal static MetadataPropertyName WriteReferenceForObject(
            JsonConverter jsonConverter,
            object currentValue,
            ref WriteStack state,
            Utf8JsonWriter writer)
        {
            MetadataPropertyName metadataToWrite = GetResolvedReferenceHandling(jsonConverter, currentValue, ref state, out string? referenceId);

            if (metadataToWrite == MetadataPropertyName.Ref)
            {
                writer.WriteString(s_metadataRef, referenceId!);
                writer.WriteEndObject();
            }
            else if (metadataToWrite == MetadataPropertyName.Id)
            {
                writer.WriteString(s_metadataId, referenceId!);
            }

            return metadataToWrite;
        }

        internal static MetadataPropertyName WriteReferenceForCollection(
            JsonConverter jsonConverter,
            object currentValue,
            ref WriteStack state,
            Utf8JsonWriter writer)
        {
            MetadataPropertyName metadataToWrite = GetResolvedReferenceHandling(jsonConverter, currentValue, ref state, out string? referenceId);

            if (metadataToWrite == MetadataPropertyName.NoMetadata)
            {
                writer.WriteStartArray();
            }
            else if (metadataToWrite == MetadataPropertyName.Id)
            {
                writer.WriteStartObject();
                writer.WriteString(s_metadataId, referenceId!);
                writer.WriteStartArray(s_metadataValues);
            }
            else
            {
                Debug.Assert(metadataToWrite == MetadataPropertyName.Ref);
                writer.WriteStartObject();
                writer.WriteString(s_metadataRef, referenceId!);
                writer.WriteEndObject();
            }

            return metadataToWrite;
        }
    }
}
