// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata.Decoding
{
    public interface IReferenceOrDefinitionTypeProvider<TType>
    {
        /// <summary>
        /// Gets the type symbol for a type definition.
        /// </summary>
        /// <returns></returns>
        TType GetTypeFromDefinition(MetadataReader/*?*/ reader, TypeDefinitionHandle handle, SignatureTypeHandleCode code);

        /// <summary>
        /// Gets the type symbol for a type reference.
        /// </summary>
        TType GetTypeFromReference(MetadataReader/*?*/ reader, TypeReferenceHandle handle, SignatureTypeHandleCode code);
    }
}
