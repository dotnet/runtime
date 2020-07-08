// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        ReadAheadNameOrEndObject, // Try to move the reader to the the first $id, $ref, or the EndObject token.
        ReadNameOrEndObject, // Read the first $id, $ref, or the EndObject token.

        ReadAheadIdValue, // Try to move the reader to the value for $id.
        ReadAheadRefValue, // Try to move the reader to the value for $ref.
        ReadIdValue, // Read value for $id.
        ReadRefValue, // Read value for $ref.
        ReadAheadRefEndObject, // Try to move the reader to the EndObject for $ref.
        ReadRefEndObject, // Read the EndObject for $ref.

        ReadAheadValuesName, // Try to move the reader to the $values property name.
        ReadValuesName, // Read $values property name.
        ReadAheadValuesStartArray, // Try to move the reader to the StartArray for $values.
        ReadValuesStartArray, // Read the StartArray for $values.

        PropertyValue, // Whether all metadata properties has been read.

        CreatedObject,
        ReadElements,
        EndToken,
        EndTokenValidation,
    }
}
