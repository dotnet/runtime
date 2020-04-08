// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    /// <summary>
    /// The current state of an object or collection that supports continuation.
    /// The values are typically compared with the less-than operator so the ordering is important.
    /// </summary>
    internal enum StackFrameObjectState : byte
    {
        None = 0,

        StartToken,

        MetadataTryReadName, // Try to move the reader to the the first $id or $ref.
        MetadataReadName, // Read the first $id or $ref.

        MetadataTryReadIdValue, // Try to move the reader to the value for $id.
        MetadataTryReadRefValue, // Try to move the reader to the value for $ref.
        MetadataReadIdValue, // Read value for $id.
        MetadataReadRefValue, // Read value for $ref.
        MetadataRefTryReadEndObject, // Try to move the reader to the EndObject for $ref.
        MetadataRefReadEndObject, // Read the EndObject for $ref.

        MetadataTryReadValuesName, // Try to move the reader to the $values property name.
        MetadataReadValuesName, // Read $values property name.
        MetadataTryReadValuesStartArray, // Try to move the reader to the StartArray for $values.
        MetadataReadValuesStartArray, // Read the StartArray for $values.
        MetadataPropertyValue, // Whether all metadata properties has been read.

        CreatedObject,
        ReadElements,
        EndToken,
        EndTokenValidation,
    }
}
