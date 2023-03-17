// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    [Flags]
    public enum FlagsSByteEnumWithNegativeValues : sbyte
    {
        A = 0b00000000,
        B = 0b00000001,
        C = 0b00000010,
        D = 0b00000100,
        E = 0b00001000,
        F = 0b00010000,
        G = 0b00100000,
        H = 0b01000000,
        I = unchecked((sbyte)0b10000000),
    }

    [Flags]
    public enum FlagsInt32EnumWithOverlappingNegativeValues : int
    {
        A = 0x000F0000,
        B = 0x0000FFFF,
        C = unchecked((int)0xFFFF0000),
    }
}
