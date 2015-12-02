// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata.Decoding
{
    public interface ISZArrayTypeProvider<TType>
    {
        /// <summary>
        /// Gets the type symbol for a single-dimensional array with zero lower bounds of the given element type.
        /// </summary>
        TType GetSZArrayType(TType elementType);
    }
}
