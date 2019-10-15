// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata
{
    public interface ISignatureTypeProvider<TType, TGenericContext> : ISimpleTypeProvider<TType>, IConstructedTypeProvider<TType>
    {
        /// <summary>
        /// Gets the a type symbol for the function pointer type of the given method signature.
        /// </summary>
        TType GetFunctionPointerType(MethodSignature<TType> signature);

        /// <summary>
        /// Gets the type symbol for the generic method parameter at the given zero-based index.
        /// </summary>
        TType GetGenericMethodParameter(TGenericContext genericContext, int index);

        /// <summary>
        /// Gets the type symbol for the generic type parameter at the given zero-based index.
        /// </summary>
        TType GetGenericTypeParameter(TGenericContext genericContext, int index);

        /// <summary>
        /// Gets the type symbol for a type with a custom modifier applied.
        /// </summary>
        /// <param name="modifier">The modifier type applied. </param>
        /// <param name="unmodifiedType">The type symbol of the underlying type without modifiers applied.</param>
        /// <param name="isRequired">True if the modifier is required, false if it's optional.</param>
        TType GetModifiedType(TType modifier, TType unmodifiedType, bool isRequired);

        /// <summary>
        /// Gets the type symbol for a local variable type that is marked as pinned.
        /// </summary>
        TType GetPinnedType(TType elementType);

        /// <summary>
        /// Gets the type symbol for a type specification.
        /// </summary>
        /// <param name="reader">
        /// The metadata reader that was passed to the signature decoder. It may be null.
        /// </param>
        /// <param name="genericContext">
        /// The context that was passed to the signature decoder.
        /// </param>
        /// <param name="handle">
        /// The type specification handle.
        /// </param>
        /// <param name="rawTypeKind">
        /// The kind of the type as specified in the signature. To interpret this value use <see cref="Ecma335.MetadataReaderExtensions.ResolveSignatureTypeKind(MetadataReader, EntityHandle, byte)"/>
        /// Note that when the signature comes from a WinMD file additional processing is needed to determine whether the target type is a value type or a reference type.
        /// </param>
        TType GetTypeFromSpecification(MetadataReader reader, TGenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind);
    }
}
