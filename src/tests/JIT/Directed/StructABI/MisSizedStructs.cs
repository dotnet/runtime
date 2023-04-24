// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class MisSizedStructs
{
    public const byte ByteValue = 0xC1;

    [Fact]
    public static int TestEntryPoint()
    {
        const int BytesSize = 256;
        var bytes = stackalloc byte[BytesSize];
        Unsafe.InitBlock(bytes, ByteValue, BytesSize);

        if (ProblemWithStructWithThreeBytes(bytes))
        {
            return 101;
        }

        if (ProblemWithStructWithFiveBytes(bytes))
        {
            return 102;
        }

        if (ProblemWithStructWithSixBytes(bytes))
        {
            return 103;
        }

        if (ProblemWithStructWithSevenBytes(bytes))
        {
            return 104;
        }

        if (ProblemWithStructWithElevenBytes(bytes))
        {
            return 105;
        }

        if (ProblemWithStructWithThirteenBytes(bytes))
        {
            return 106;
        }

        if (ProblemWithStructWithFourteenBytes(bytes))
        {
            return 107;
        }

        if (ProblemWithStructWithFifteenBytes(bytes))
        {
            return 108;
        }

        if (ProblemWithStructWithNineteenBytes(bytes))
        {
            return 109;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithStructWithThreeBytes(byte* bytes)
    {
        return CallForStructWithThreeBytes(default, *(StructWithThreeBytes*)bytes) != ByteValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithStructWithFiveBytes(byte* bytes)
    {
        return CallForStructWithFiveBytes(default, *(StructWithFiveBytes*)bytes) != ByteValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithStructWithSixBytes(byte* bytes)
    {
        return CallForStructWithSixBytes(default, *(StructWithSixBytes*)bytes) != ByteValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithStructWithSevenBytes(byte* bytes)
    {
        return CallForStructWithSevenBytes(default, *(StructWithSevenBytes*)bytes) != ByteValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithStructWithElevenBytes(byte* bytes)
    {
        return CallForStructWithElevenBytes(default, *(StructWithElevenBytes*)bytes) != ByteValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithStructWithThirteenBytes(byte* bytes)
    {
        return CallForStructWithThirteenBytes(default, *(StructWithThirteenBytes*)bytes) != ByteValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithStructWithFourteenBytes(byte* bytes)
    {
        return CallForStructWithFourteenBytes(default, *(StructWithFourteenBytes*)bytes) != ByteValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithStructWithFifteenBytes(byte* bytes)
    {
        return CallForStructWithFifteenBytes(default, *(StructWithFifteenBytes*)bytes) != ByteValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithStructWithNineteenBytes(byte* bytes)
    {
        return CallForStructWithNineteenBytes(default, *(StructWithNineteenBytes*)bytes) != ByteValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStructWithThreeBytes(ForceStackUsage fs, StructWithThreeBytes value) => value.Bytes[2];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStructWithFiveBytes(ForceStackUsage fs, StructWithFiveBytes value) => value.Bytes[4];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStructWithSixBytes(ForceStackUsage fs, StructWithSixBytes value) => value.Bytes[5];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStructWithSevenBytes(ForceStackUsage fs, StructWithSevenBytes value) => value.Bytes[6];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStructWithElevenBytes(ForceStackUsage fs, StructWithElevenBytes value) => value.Bytes[10];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStructWithThirteenBytes(ForceStackUsage fs, StructWithThirteenBytes value) => value.Bytes[12];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStructWithFourteenBytes(ForceStackUsage fs, StructWithFourteenBytes value) => value.Bytes[13];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStructWithFifteenBytes(ForceStackUsage fs, StructWithFifteenBytes value) => value.Bytes[14];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte CallForStructWithNineteenBytes(ForceStackUsage fs, StructWithNineteenBytes value) => value.Bytes[18];

    struct ForceStackUsage
    {
        public fixed byte Bytes[40];
    }

    struct StructWithThreeBytes
    {
        public fixed byte Bytes[3];
    }

    struct StructWithFiveBytes
    {
        public fixed byte Bytes[5];
    }

    struct StructWithSixBytes
    {
        public fixed byte Bytes[6];
    }

    struct StructWithSevenBytes
    {
        public fixed byte Bytes[7];
    }

    struct StructWithElevenBytes
    {
        public fixed byte Bytes[11];
    }

    struct StructWithThirteenBytes
    {
        public fixed byte Bytes[13];
    }

    struct StructWithFourteenBytes
    {
        public fixed byte Bytes[14];
    }

    struct StructWithFifteenBytes
    {
        public fixed byte Bytes[15];
    }

    struct StructWithNineteenBytes
    {
        public fixed byte Bytes[19];
    }
}
