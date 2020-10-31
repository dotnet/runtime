// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Metadata
{
    public interface ISZArrayTypeProvider<TType>
    {
        /// <summary>
        /// Gets the type symbol for a single-dimensional array with zero lower bounds of the given element type.
        /// </summary>
        TType GetSZArrayType(TType elementType);
    }
}
