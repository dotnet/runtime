// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public enum ConstantTypeCode : byte
    {
        // II.22.9 Constant : 0x0B
        // Type shall be exactly one of: ELEMENT_TYPE_BOOLEAN, ELEMENT_TYPE_CHAR,
        // ELEMENT_TYPE_I1, ELEMENT_TYPE_U1, ELEMENT_TYPE_I2, ELEMENT_TYPE_U2,
        // ELEMENT_TYPE_I4, ELEMENT_TYPE_U4, ELEMENT_TYPE_I8, ELEMENT_TYPE_U8,
        // ELEMENT_TYPE_R4, ELEMENT_TYPE_R8, or ELEMENT_TYPE_STRING; or
        // ELEMENT_TYPE_CLASS with a Value of zero (Section II.23.1.16)
        Invalid = 0,

        Boolean = CorElementType.ELEMENT_TYPE_BOOLEAN,
        Char = CorElementType.ELEMENT_TYPE_CHAR,
        SByte = CorElementType.ELEMENT_TYPE_I1,
        Byte = CorElementType.ELEMENT_TYPE_U1,
        Int16 = CorElementType.ELEMENT_TYPE_I2,
        UInt16 = CorElementType.ELEMENT_TYPE_U2,
        Int32 = CorElementType.ELEMENT_TYPE_I4,
        UInt32 = CorElementType.ELEMENT_TYPE_U4,
        Int64 = CorElementType.ELEMENT_TYPE_I8,
        UInt64 = CorElementType.ELEMENT_TYPE_U8,
        Single = CorElementType.ELEMENT_TYPE_R4,
        Double = CorElementType.ELEMENT_TYPE_R8,
        String = CorElementType.ELEMENT_TYPE_STRING,
        NullReference = CorElementType.ELEMENT_TYPE_CLASS,
    }
}
