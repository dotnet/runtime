// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata.Decoding
{
    /// <summary>
    /// Represents a primitive type found in metadata signatures.
    /// </summary>
    public enum PrimitiveTypeCode : byte
    {
        Boolean = SignatureTypeCode.Boolean,
        Byte = SignatureTypeCode.Byte,
        SByte = SignatureTypeCode.SByte,
        Char = SignatureTypeCode.Char,
        Single = SignatureTypeCode.Single,
        Double = SignatureTypeCode.Double,
        Int16 = SignatureTypeCode.Int16,
        Int32 = SignatureTypeCode.Int32,
        Int64 = SignatureTypeCode.Int64,
        UInt16 = SignatureTypeCode.UInt16,
        UInt32 = SignatureTypeCode.UInt32,
        UInt64 = SignatureTypeCode.UInt64,
        IntPtr = SignatureTypeCode.IntPtr,
        UIntPtr = SignatureTypeCode.UIntPtr,
        Object = SignatureTypeCode.Object,
        String = SignatureTypeCode.String,
        TypedReference = SignatureTypeCode.TypedReference,
        Void = SignatureTypeCode.Void,
    }
}
