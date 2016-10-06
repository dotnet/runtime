// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata
{
    public interface ISimpleTypeProvider<TType>
    {
        /// <summary>
        /// Gets the type symbol for a primitive type.
        /// </summary>
        TType GetPrimitiveType(PrimitiveTypeCode typeCode);

        /// <summary>
        /// Gets the type symbol for a type definition.
        /// </summary>
        /// <param name="reader">
        /// The metadata reader that was passed to the signature decoder. It may be null.
        /// </param>
        /// <param name="handle">
        /// The type definition handle.
        /// </param>
        /// <param name="rawTypeKind">
        /// The kind of the type as specified in the signature. To interpret this value use <see cref="Ecma335.MetadataReaderExtensions.ResolveSignatureTypeKind(MetadataReader, EntityHandle, byte)"/>
        /// Note that when the signature comes from a WinMD file additional processing is needed to determine whether the target type is a value type or a reference type.
        /// </param>
        TType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind);

        /// <summary>
        /// Gets the type symbol for a type reference.
        /// </summary>
        /// <param name="reader">
        /// The metadata reader that was passed to the signature decoder. It may be null.
        /// </param>
        /// <param name="handle">
        /// The type definition handle.
        /// </param>
        /// <param name="rawTypeKind">
        /// The kind of the type as specified in the signature. To interpret this value use <see cref="Ecma335.MetadataReaderExtensions.ResolveSignatureTypeKind(MetadataReader, EntityHandle, byte)"/>
        /// Note that when the signature comes from a WinMD file additional processing is needed to determine whether the target type is a value type or a reference type.
        /// </param>
        TType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind);
    }
}
