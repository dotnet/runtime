// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public static unsafe class Program
{
    [ConditionalFact(typeof(AdvSimd.Arm64), nameof(AdvSimd.Arm64.IsSupported))]
    public static int TestEntryPoint()
    {
        if (UpperVectorRestoresDoNotPreventConsecutiveOperandReuse() != 100 ||
            TableLookupsPreserveUpperVectorLanesAcrossCall() != 100)
        {
            return 1;
        }
        return 100;
    }

    // The caller must preserve vector locals across this non-inlined call.
    // On ARM64 only the low 64 bits of V8-V15 are callee-saved.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<byte> MakeIndex(byte index) => Vector128.Create(index);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<byte> LookupTwoVectorsAfterCall(byte* p, byte index)
    {
        // Verify upper lanes are saved across the call and restored into the
        // table operands. Capture registers rather than requiring specific choices.
        // ARM64: mov [[SAVE0:v[0-9]+]].d[0], [[TABLE0:v[0-9]+]].d[1]
        // ARM64: mov [[SAVE1:v[0-9]+]].d[0], [[TABLE1:v[0-9]+]].d[1]
        // ARM64: {{bl[r]?}}
        // ARM64: mov [[TABLE0]].d[1], [[SAVE0]].d[0]
        // ARM64: mov [[TABLE1]].d[1], [[SAVE1]].d[0]
        // ARM64: tbl v{{[0-9]+}}.16b, {[[TABLE0]].16b, [[TABLE1]].16b}, v{{[0-9]+}}.16b

        var v0 = AdvSimd.LoadVector128(p + 0);
        var v1 = AdvSimd.LoadVector128(p + 16);
        var indices = MakeIndex(index);
        var result = AdvSimd.Arm64.VectorTableLookup((v0, v1), indices);
        result = AdvSimd.Xor(result, v0);
        result = AdvSimd.Xor(result, v1);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<byte> LookupThreeVectorsAfterCall(byte* p, byte index)
    {
        // Verify upper lanes are saved across the call and restored into the
        // table operands. Capture registers rather than requiring specific choices.
        // ARM64: mov [[SAVE0:v[0-9]+]].d[0], [[TABLE0:v[0-9]+]].d[1]
        // ARM64: mov [[SAVE1:v[0-9]+]].d[0], [[TABLE1:v[0-9]+]].d[1]
        // ARM64: mov [[SAVE2:v[0-9]+]].d[0], [[TABLE2:v[0-9]+]].d[1]
        // ARM64: {{bl[r]?}}
        // ARM64: mov [[TABLE0]].d[1], [[SAVE0]].d[0]
        // ARM64: mov [[TABLE1]].d[1], [[SAVE1]].d[0]
        // ARM64: mov [[TABLE2]].d[1], [[SAVE2]].d[0]
        // ARM64: tbl v{{[0-9]+}}.16b, {[[TABLE0]].16b, [[TABLE1]].16b, [[TABLE2]].16b}, v{{[0-9]+}}.16b

        var v0 = AdvSimd.LoadVector128(p + 0);
        var v1 = AdvSimd.LoadVector128(p + 16);
        var v2 = AdvSimd.LoadVector128(p + 32);
        var indices = MakeIndex(index);
        var result = AdvSimd.Arm64.VectorTableLookup((v0, v1, v2), indices);
        result = AdvSimd.Xor(result, v0);
        result = AdvSimd.Xor(result, v1);
        result = AdvSimd.Xor(result, v2);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<byte> LookupFourVectorsAfterCall(byte* p, byte index)
    {
        // Verify upper lanes are saved across the call and restored into the
        // table operands. Capture registers rather than requiring specific choices.
        // ARM64: mov [[SAVE0:v[0-9]+]].d[0], [[TABLE0:v[0-9]+]].d[1]
        // ARM64: mov [[SAVE1:v[0-9]+]].d[0], [[TABLE1:v[0-9]+]].d[1]
        // ARM64: mov [[SAVE2:v[0-9]+]].d[0], [[TABLE2:v[0-9]+]].d[1]
        // ARM64: mov [[SAVE3:v[0-9]+]].d[0], [[TABLE3:v[0-9]+]].d[1]
        // ARM64: {{bl[r]?}}
        // ARM64: mov [[TABLE0]].d[1], [[SAVE0]].d[0]
        // ARM64: mov [[TABLE1]].d[1], [[SAVE1]].d[0]
        // ARM64: mov [[TABLE2]].d[1], [[SAVE2]].d[0]
        // ARM64: mov [[TABLE3]].d[1], [[SAVE3]].d[0]
        // ARM64: tbl v{{[0-9]+}}.16b, {[[TABLE0]].16b, [[TABLE1]].16b, [[TABLE2]].16b, [[TABLE3]].16b}, v{{[0-9]+}}.16b

        var v0 = AdvSimd.LoadVector128(p + 0);
        var v1 = AdvSimd.LoadVector128(p + 16);
        var v2 = AdvSimd.LoadVector128(p + 32);
        var v3 = AdvSimd.LoadVector128(p + 48);
        var indices = MakeIndex(index);
        var result = AdvSimd.Arm64.VectorTableLookup((v0, v1, v2, v3), indices);
        result = AdvSimd.Xor(result, v0);
        result = AdvSimd.Xor(result, v1);
        result = AdvSimd.Xor(result, v2);
        result = AdvSimd.Xor(result, v3);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<byte> LookupWithConsecutiveOperandReuseAfterCall(byte* p)
    {
        // The last three table operands already occupy consecutive registers.
        // Skip their upper-vector restore entries when looking for that sequence,
        // so only the first operand needs a full-vector copy, rather than all four.
        // ARM64: {{bl[r]?}}
        // ARM64: mov [[FIRST:v[0-9]+]].d[1], v{{[0-9]+}}.d[0]
        // ARM64: mov [[SECOND:v[0-9]+]].d[1], v{{[0-9]+}}.d[0]
        // ARM64: mov [[THIRD:v[0-9]+]].d[1], v{{[0-9]+}}.d[0]
        // ARM64: mov [[FOURTH:v[0-9]+]].d[1], v{{[0-9]+}}.d[0]
        // ARM64: mov [[COPY:v[0-9]+]].16b, [[FIRST]].16b
        // ARM64-NEXT: tbl v{{[0-9]+}}.16b, {[[COPY]].16b, [[SECOND]].16b, [[THIRD]].16b, [[FOURTH]].16b}, v{{[0-9]+}}.16b
        var v0 = AdvSimd.LoadVector128(p + 0);
        var v1 = AdvSimd.LoadVector128(p + 16);
        var v2 = AdvSimd.LoadVector128(p + 32);
        var v3 = AdvSimd.LoadVector128(p + 48);
        var index = MakeIndex(p[64]);
        var result = AdvSimd.Arm64.VectorTableLookup((v3, v0, v1, v2), index);
        result = AdvSimd.Xor(result, v3);
        result = AdvSimd.Xor(result, v0);
        result = AdvSimd.Xor(result, v1);
        result = AdvSimd.Xor(result, v2);
        return result;
    }

    private static int UpperVectorRestoresDoNotPreventConsecutiveOperandReuse()
    {
        byte* p = stackalloc byte[65];
        int[] table = { 3, 0, 1, 2 };
        var random = new Random(44219);
        for (int test = 0; test < 1000; test++)
        {
            for (int i = 0; i < 64; i++)
            {
                p[i] = (byte)random.Next(256);
            }

            byte index = p[64] = (byte)(test & 255);
            var actual = LookupWithConsecutiveOperandReuseAfterCall(p);
            for (int lane = 0; lane < 16; lane++)
            {
                byte expected = index < 64 ? p[table[index / 16] * 16 + index % 16] : (byte)0;
                for (int vector = 0; vector < 4; vector++)
                {
                    expected ^= p[vector * 16 + lane];
                }

                if (actual.GetElement(lane) != expected)
                {
                    Console.WriteLine($"FAIL rotated test={test} lane={lane}");
                    return 1;
                }
            }
        }

        Console.WriteLine("PASS rotated: 1000 lookups, 16000 scalar-checked lanes");
        return 100;
    }

    private static int TableLookupsPreserveUpperVectorLanesAcrossCall()
    {
        byte* p = stackalloc byte[64];
        var random = new Random(1981);
        for (int test = 0; test < 1000; test++)
        {
            for (int i = 0; i < 64; i++)
            {
                p[i] = (byte)random.Next(256);
            }

            byte index = (byte)(test & 255);
            for (int count = 2; count <= 4; count++)
            {
                var actual = count == 2 ? LookupTwoVectorsAfterCall(p, index) :
                             count == 3 ? LookupThreeVectorsAfterCall(p, index) : LookupFourVectorsAfterCall(p, index);
                for (int lane = 0; lane < 16; lane++)
                {
                    byte expected = index < count * 16 ? p[index] : (byte)0;
                    for (int vector = 0; vector < count; vector++)
                    {
                        expected ^= p[vector * 16 + lane];
                    }

                    if (actual.GetElement(lane) != expected)
                    {
                        Console.WriteLine($"FAIL count={count} test={test} lane={lane}");
                        return 1;
                    }
                }
            }
        }

        Console.WriteLine("PASS: 3000 lookups, 48000 scalar-checked lanes");
        return 100;
    }
}
