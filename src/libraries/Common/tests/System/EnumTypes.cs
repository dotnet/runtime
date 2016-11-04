// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public enum SimpleEnum
    {
        Red = 1,
        Blue = 2,
        Green = 3,
        Green_a = 3,
        Green_b = 3,
        B = 4
    }

    public enum ByteEnum : byte
    {
        Min = byte.MinValue,
        One = 1,
        Two = 2,
        Max = byte.MaxValue,
    }

    public enum SByteEnum : sbyte
    {
        Min = sbyte.MinValue,
        One = 1,
        Two = 2,
        Max = sbyte.MaxValue,
    }

    public enum UInt16Enum : ushort
    {
        Min = ushort.MinValue,
        One = 1,
        Two = 2,
        Max = ushort.MaxValue,
    }

    public enum Int16Enum : short
    {
        Min = short.MinValue,
        One = 1,
        Two = 2,
        Max = short.MaxValue,
    }

    public enum UInt32Enum : uint
    {
        Min = uint.MinValue,
        One = 1,
        Two = 2,
        Max = uint.MaxValue,
    }

    public enum Int32Enum : int
    {
        Min = int.MinValue,
        One = 1,
        Two = 2,
        Max = int.MaxValue,
    }

    public enum UInt64Enum : ulong
    {
        Min = ulong.MinValue,
        One = 1,
        Two = 2,
        Max = ulong.MaxValue,
    }

    public enum Int64Enum : long
    {
        Min = long.MinValue,
        One = 1,
        Two = 2,
        Max = long.MaxValue,
    }
}
