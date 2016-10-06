// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace System.Reflection.Metadata
{
    public interface IConstructedTypeProvider<TType> : ISZArrayTypeProvider<TType>
    {
        /// <summary>
        /// Gets the type symbol for a generic instantiation of the given generic type with the given type arguments.
        /// </summary>
        TType GetGenericInstantiation(TType genericType, ImmutableArray<TType> typeArguments);

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
