// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata.Decoding
{
    public interface ISignatureTypeProvider<TType> : IPrimitiveTypeProvider<TType>, ITypeProvider<TType>, IConstructedTypeProvider<TType>
    {
        /// <summary>
        /// Gets the a type symbol for the function pointer type of the given method signature.
        /// </summary>
        TType GetFunctionPointerType(MethodSignature<TType> signature);

        /// <summary>
        /// Gets the type symbol for the generic method parameter at the given zero-based index.
        /// </summary>
        TType GetGenericMethodParameter(int index);

        /// <summary>
        /// Gets the type symbol for the generic type parameter at the given zero-based index.
        /// </summary>
        TType GetGenericTypeParameter(int index);

        /// <summary>
        /// Gets the type symbol for a type with a custom modifier applied.
        /// </summary>
        /// <param name="reader">The metadata reader that was passed to the <see cref="SignatureDecoder{TType}"/>. It may be null.</param>
        /// <param name="isRequired">True if the modifier is required, false if it's optional.</param>
        /// <param name="modifierTypeHandle">The modifier type applied. A <see cref="TypeDefinitionHandle"/>, <see cref="TypeReferenceHandle"/>, or <see cref="TypeSpecificationHandle"/>. </param>
        /// <param name="unmodifiedType">The type symbol of the underlying type without modifiers applied.</param>
        /// <remarks>
        /// The modifier type is passed as a handle rather than a decoded <typeparamref name="TType"/>.
        ///
        ///   1. It makes (not uncommon) scenarios that skip modifiers cheaper.
        /// 
        ///   2. It is the only valid place where a <see cref="TypeSpecificationHandle"/> can occur within a signature blob. 
        ///      If we were to recurse into the type spec before calling GetModifiedType, it would eliminate scenarios such
        ///      as caching by TypeSpec or deciphering the structure of a signature without resolving any handles.
        /// </remarks>
        TType GetModifiedType(MetadataReader reader, bool isRequired, EntityHandle modifierTypeHandle, TType unmodifiedType);

        /// <summary>
        /// Gets the type symbol for a local variable type that is marked as pinned.
        /// </summary>
        TType GetPinnedType(TType elementType);
    }
}
