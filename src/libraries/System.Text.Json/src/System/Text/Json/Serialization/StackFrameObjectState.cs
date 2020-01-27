// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    internal enum StackFrameObjectState : byte
    {
        None = 0,

        StartToken,

        MetadataPropertyName, // Read the first $id or $ref.
        MetadataIdProperty, // Read value for $id.
        MetadataRefProperty, // Read value for $ref.
        MetadataRefPropertyEndObject, // Read EndObject for $ref.
        MetadataValuesPropertyName, // Read $values property name.
        MetadataValuesPropertyStartArray, // Read StartArray for $values.
        MetataPropertyValue, // Whether all metadata properties has been read.

        CreatedObject,
        ReadElements,
        EndToken,
        EndTokenValidation,
    }
}
