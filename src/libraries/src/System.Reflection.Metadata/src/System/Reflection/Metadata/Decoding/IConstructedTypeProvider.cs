// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace System.Reflection.Metadata.Decoding
{
    public interface IConstructedTypeProvider<TType> : ISZArrayTypeProvider<TType>
    {
        /// <summary>
        /// Gets the type symbol for a generic instantiation of the given generic type with the given type arguments.
        /// </summary>
        TType GetGenericInstance(TType genericType, ImmutableArray<TType> typeArguments);

        /// <summary>
        /// Gets the type symbol for a generalized array of the given element type and shape. 
        /// </summary>
        TType GetArrayType(TType elementType, ArrayShape shape);

        /// <summary>
        /// Gets the type symbol for a managed pointer to the given element type.
        /// </summary>
        TType GetByReferenceType(TType elementType);

        /// <summary>
        /// Gets the type symbol for an unmanaged pointer to the given element ty
        /// </summary>
        TType GetPointerType(TType elementType);
    }
}
