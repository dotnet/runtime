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
            object currentValue,
            ref WriteStack state,
            Utf8JsonWriter writer)
        {
            MetadataPropertyName writtenMetadataName;

            if (state.BoxedStructReferenceId != null)
            {
                // We're serializing a struct that has been handled by a polymorphic converter;
                // emit the reference id that was recorded for the boxed instance.

                Debug.Assert(jsonConverter.IsValueType && jsonConverter.CanHaveIdMetadata);
                writer.WriteString(s_metadataId, state.BoxedStructReferenceId);
                writtenMetadataName = MetadataPropertyName.Id;
                state.BoxedStructReferenceId = null;
            }
            else if (!jsonConverter.CanHaveIdMetadata || jsonConverter.IsValueType)
            {
                // If the jsonConverter supports immutable dictionaries or value types, don't write any metadata
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
                    writer.WriteString(s_metadataId, referenceId);
                    writtenMetadataName = MetadataPropertyName.Id;
                }
            }

            return writtenMetadataName;
        }

        internal static MetadataPropertyName WriteReferenceForCollection(
            JsonConverter jsonConverter,
            object currentValue,
            ref WriteStack state,
            Utf8JsonWriter writer)
        {
            MetadataPropertyName writtenMetadataName;

            if (state.BoxedStructReferenceId != null)
            {
                // We're serializing a struct that has been handled by a polymorphic converter;
                // emit the reference id that was recorded for the boxed instance.

                Debug.Assert(jsonConverter.IsValueType && jsonConverter.CanHaveIdMetadata);

                writer.WriteStartObject();
                writer.WriteString(s_metadataId, state.BoxedStructReferenceId);
                writer.WriteStartArray(s_metadataValues);
                writtenMetadataName = MetadataPropertyName.Id;
                state.BoxedStructReferenceId = null;
            }
            else if (!jsonConverter.CanHaveIdMetadata || jsonConverter.IsValueType)
            {
                // If the jsonConverter supports immutable enumerables or value type collections, don't write any metadata
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
                    writer.WriteStartObject();
                    writer.WriteString(s_metadataId, referenceId);
                    writer.WriteStartArray(s_metadataValues);
                    writtenMetadataName = MetadataPropertyName.Id;
                }
            }

            return writtenMetadataName;
        }

        /// <summary>
        /// Used by polymorphic converters that are handling references for values that are boxed structs.
        /// </summary>
        internal static bool TryWriteReferenceForBoxedStruct(object currentValue, ref WriteStack state, Utf8JsonWriter writer)
        {
            Debug.Assert(state.BoxedStructReferenceId == null);
            Debug.Assert(currentValue.GetType().IsValueType);

            string referenceId = state.ReferenceResolver.GetReference(currentValue, out bool alreadyExists);
            Debug.Assert(referenceId != null);

            if (alreadyExists)
            {
                writer.WriteStartObject();
                writer.WriteString(s_metadataRef, referenceId);
                writer.WriteEndObject();
            }
            else
            {
                // Since we cannot run `ReferenceResolver.GetReference` twice for newly encountered instances,
                // need to store the reference id for use by the subtype converter we're dispatching to.
                state.BoxedStructReferenceId = referenceId;
            }

            return alreadyExists;
        }
    }
}
