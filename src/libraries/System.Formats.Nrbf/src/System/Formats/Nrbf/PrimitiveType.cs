// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Nrbf;

/// <summary>
/// Primitive type.
/// </summary>
/// <remarks>
/// PrimitiveTypeEnumeration enumeration is described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/4e77849f-89e3-49db-8fb9-e77ee4bc7214">[MS-NRBF] 2.1.2.3</see>.
/// </remarks>
internal enum PrimitiveType : byte
{
    Boolean = 1,
    Byte = 2,
    Char = 3,
    // 4 is not used in the protocol
    Decimal = 5,
    Double = 6,
    Int16 = 7,
    Int32 = 8,
    Int64 = 9,
    SByte = 10,
    Single = 11,
    TimeSpan = 12,
    DateTime = 13,
    UInt16 = 14,
    UInt32 = 15,
    UInt64 = 16
    // This internal enum no longer contains Null and String as they were always illegal:
    // - In case of BinaryArray (NRBF 2.4.3.1):
    // "If the BinaryTypeEnum value is Primitive, the PrimitiveTypeEnumeration
    // value in AdditionalTypeInfo MUST NOT be Null (17) or String (18)."
    // - In case of MemberPrimitiveTyped (NRBF 2.5.1):
    // "PrimitiveTypeEnum (1 byte): A PrimitiveTypeEnumeration
    // value that specifies the Primitive Type of data that is being transmitted.
    // This field MUST NOT contain a value of 17 (Null) or 18 (String)."
    // - In case of ArraySinglePrimitive (NRBF 2.4.3.3):
    // "A PrimitiveTypeEnumeration value that identifies the Primitive Type
    // of the items of the Array. The value MUST NOT be 17 (Null) or 18 (String)."
    // - In case of MemberTypeInfo (NRBF 2.3.1.2):
    // "When the BinaryTypeEnum value is Primitive, the PrimitiveTypeEnumeration
    // value in AdditionalInfo MUST NOT be Null (17) or String (18)."
}
