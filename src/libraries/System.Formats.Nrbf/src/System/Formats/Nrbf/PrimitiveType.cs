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
    /// <summary>
    /// Used internally to express no value
    /// </summary>
    None = 0,
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
    UInt64 = 16,
    Null = 17,
    String = 18
}
