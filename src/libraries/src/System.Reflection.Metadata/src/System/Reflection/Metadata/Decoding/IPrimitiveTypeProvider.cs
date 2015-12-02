// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata.Decoding
{
    public interface IPrimitiveTypeProvider<TType>
    {
        /// <summary>
        /// Gets the type symbol for a primitive type.
        /// </summary>
        TType GetPrimitiveType(PrimitiveTypeCode typeCode);
    }
}
