// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Type codes used to encode types of primitive values in Custom Attribute value blob.
    /// </summary>
    public enum PrimitiveSerializationTypeCode : byte
    {
        Boolean = SignatureTypeCode.Boolean,
        Byte = SignatureTypeCode.Byte,
        SByte = SignatureTypeCode.SByte,
        Char = SignatureTypeCode.Char,
        Int16 = SignatureTypeCode.Int16,
        UInt16 = SignatureTypeCode.UInt16,
        Int32 = SignatureTypeCode.Int32,
        UInt32 = SignatureTypeCode.UInt32,
        Int64 = SignatureTypeCode.Int64,
        UInt64 = SignatureTypeCode.UInt64,
        Single = SignatureTypeCode.Single,
        Double = SignatureTypeCode.Double,
        String = SignatureTypeCode.String,
    }
}

