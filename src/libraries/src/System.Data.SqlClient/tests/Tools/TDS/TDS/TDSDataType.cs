// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// Data types used by TDS server
    /// </summary>
    public enum TDSDataType : byte
    {
        Null = 0x1f,
        Text = 0x23,
        Guid = 0x24,
        VarBinary = 0x25,
        IntN = 0x26,
        VarChar = 0x27,
        Binary = 0x2d,
        Image = 0x22,
        DateN = 0x28,
        TimeN = 0x29,
        DateTime2N = 0x2a,
        DateTimeOffsetN = 0x2b,
        Char = 0x2f,
        Int1 = 0x30,
        Bit = 0x32,
        Int2 = 0x34,
        Decimal = 0x37,
        Int4 = 0x38,
        DateTime4 = 0x3a,
        Float4 = 0x3b,
        Money = 0x3c,
        DateTime = 0x3d,
        Float8 = 0x3e,
        Numeric = 0x3f,
        SSVariant = 0x62,
        NText = 0x63,
        BitN = 0x68,
        DecimalN = 0x6a,
        NumericN = 0x6c,
        FloatN = 0x6d,
        MoneyN = 0x6e,
        DateTimeN = 0x6f,
        Money4 = 0x7a,
        Int8 = 0x7f,
        BigVarBinary = 0xA5,
        BigVarChar = 0xA7,
        BigBinary = 0xAD,
        BigChar = 0xAF,
        NVarChar = 0xe7,
        NChar = 0xef,
        Udt = 0xF0,
        Xml = 0xf1,
        Table = 0xF3
    }
}
