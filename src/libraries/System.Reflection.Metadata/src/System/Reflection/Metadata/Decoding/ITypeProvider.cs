// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata.Decoding
{
    public interface ITypeProvider<TType>
    {
        /// <summary>
        /// Gets the type symbol for a type definition.
        /// </summary>
        /// <param name="reader">
        /// The metadata reader that was passed to the<see cref= "SignatureDecoder{TType}" />. It may be null.
        /// </param>
        /// <param name="handle">
        /// The type definition handle.
        /// </param>
        /// <param name="code">
        /// When <see cref="SignatureDecoderOptions.DifferentiateClassAndValueTypes"/> is used indicates whether
        /// the type reference is to class or value type. Otherwise <see cref="SignatureTypeHandleCode.Unresolved"/>
        /// will be passed.
        /// </param>
        TType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, SignatureTypeHandleCode code);

        /// <summary>
        /// Gets the type symbol for a type reference.
        /// </summary>
        /// <param name="reader">
        /// The metadata reader that was passed to the <see cref= "SignatureDecoder{TType}" />. It may be null.
        /// </param>
        /// <param name="handle">
        /// The type definition handle.
        /// </param>
        /// <param name="code">
        /// When <see cref="SignatureDecoderOptions.DifferentiateClassAndValueTypes"/> is used indicates whether
        /// the type reference is to class or value type. Otherwise <see cref="SignatureTypeHandleCode.Unresolved"/>
        /// will be passed.
        /// </param>
        TType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, SignatureTypeHandleCode code);


        /// <summary>
        /// Gets the type symbol for a type specification.
        /// </summary>
        /// <param name="reader">
        /// The metadata reader that was passed to the <see cref= "SignatureDecoder{TType}" />. It may be null.
        /// </param>
        /// <param name="handle">
        /// The type specification handle.
        /// </param>
        /// <param name="code">
        /// When <see cref="SignatureDecoderOptions.DifferentiateClassAndValueTypes"/> is used indicates whether
        /// the type reference is to class or value type. Otherwise <see cref="SignatureTypeHandleCode.Unresolved"/>
        /// will be passed.
        /// </param>
        TType GetTypeFromSpecification(MetadataReader reader, TypeSpecificationHandle handle, SignatureTypeHandleCode code);
    }
}
