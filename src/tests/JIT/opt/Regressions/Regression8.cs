// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

// Keep separate vector locals: replacing these with an array or loop removes
// the register pressure that exercises consecutive-register spill selection.
public static unsafe class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<byte> Run(byte* p)
    {
        // The later XORs keep most vectors live across several four-register
        // table lookups. Different table operands give LSRA multiple chances
        // to select a group that crosses the V31-to-V0 boundary.
        var v0 = AdvSimd.LoadVector128(p + 0);
        var v1 = AdvSimd.LoadVector128(p + 16);
        var v2 = AdvSimd.LoadVector128(p + 32);
        var v3 = AdvSimd.LoadVector128(p + 48);
        var v4 = AdvSimd.LoadVector128(p + 64);
        var v5 = AdvSimd.LoadVector128(p + 80);
        var v6 = AdvSimd.LoadVector128(p + 96);
        var v7 = AdvSimd.LoadVector128(p + 112);
        var v8 = AdvSimd.LoadVector128(p + 128);
        var v9 = AdvSimd.LoadVector128(p + 144);
        var v10 = AdvSimd.LoadVector128(p + 160);
        var v11 = AdvSimd.LoadVector128(p + 176);
        var v12 = AdvSimd.LoadVector128(p + 192);
        var v13 = AdvSimd.LoadVector128(p + 208);
        var v14 = AdvSimd.LoadVector128(p + 224);
        var v15 = AdvSimd.LoadVector128(p + 240);
        var v16 = AdvSimd.LoadVector128(p + 256);
        var v17 = AdvSimd.LoadVector128(p + 272);
        var v18 = AdvSimd.LoadVector128(p + 288);
        var v19 = AdvSimd.LoadVector128(p + 304);
        var v20 = AdvSimd.LoadVector128(p + 320);
        var v21 = AdvSimd.LoadVector128(p + 336);
        var v22 = AdvSimd.LoadVector128(p + 352);
        var v23 = AdvSimd.LoadVector128(p + 368);
        var v24 = AdvSimd.LoadVector128(p + 384);
        var v25 = AdvSimd.LoadVector128(p + 400);
        var v26 = AdvSimd.LoadVector128(p + 416);
        var v27 = AdvSimd.LoadVector128(p + 432);
        var v28 = AdvSimd.LoadVector128(p + 448);
        var v29 = AdvSimd.LoadVector128(p + 464);
        var v30 = AdvSimd.LoadVector128(p + 480);
        var v31 = AdvSimd.LoadVector128(p + 496);
        var v32 = AdvSimd.LoadVector128(p + 512);
        var v33 = AdvSimd.LoadVector128(p + 528);
        var v34 = AdvSimd.LoadVector128(p + 544);
        var v35 = AdvSimd.LoadVector128(p + 560);
        var v36 = AdvSimd.LoadVector128(p + 576);
        var v37 = AdvSimd.LoadVector128(p + 592);
        var v38 = AdvSimd.LoadVector128(p + 608);
        var index = AdvSimd.LoadVector128(p + 656);
        index = AdvSimd.Xor(index, v16);
        index = AdvSimd.Xor(index, v32);
        var result = AdvSimd.Arm64.VectorTableLookup((v3, v1, v27, v25), index);
        result = AdvSimd.Xor(result, AdvSimd.Arm64.VectorTableLookup((v5, v14, v31, v21), index));
        result = AdvSimd.Xor(result, AdvSimd.Arm64.VectorTableLookup((v36, v11, v0, v24), index));
        // XOR is order-independent, but the use order affects LSRA's spill choices.
        result = AdvSimd.Xor(result, v36);
        result = AdvSimd.Xor(result, v19);
        result = AdvSimd.Xor(result, v38);
        result = AdvSimd.Xor(result, v33);
        result = AdvSimd.Xor(result, v17);
        result = AdvSimd.Xor(result, v22);
        result = AdvSimd.Xor(result, v26);
        result = AdvSimd.Xor(result, v28);
        result = AdvSimd.Xor(result, v20);
        result = AdvSimd.Xor(result, v18);
        result = AdvSimd.Xor(result, v8);
        result = AdvSimd.Xor(result, v34);
        result = AdvSimd.Xor(result, v23);
        result = AdvSimd.Xor(result, v29);
        result = AdvSimd.Xor(result, v10);
        result = AdvSimd.Xor(result, v11);
        result = AdvSimd.Xor(result, v27);
        result = AdvSimd.Xor(result, v12);
        result = AdvSimd.Xor(result, v4);
        result = AdvSimd.Xor(result, v31);
        result = AdvSimd.Xor(result, v1);
        result = AdvSimd.Xor(result, v30);
        result = AdvSimd.Xor(result, v25);
        result = AdvSimd.Xor(result, v2);
        result = AdvSimd.Xor(result, v13);
        result = AdvSimd.Xor(result, v0);
        result = AdvSimd.Xor(result, v24);
        result = AdvSimd.Xor(result, v21);
        result = AdvSimd.Xor(result, v5);
        result = AdvSimd.Xor(result, v14);
        result = AdvSimd.Xor(result, v15);
        result = AdvSimd.Xor(result, v7);
        result = AdvSimd.Xor(result, v6);
        result = AdvSimd.Xor(result, v35);
        result = AdvSimd.Xor(result, v37);
        result = AdvSimd.Xor(result, v9);
        result = AdvSimd.Xor(result, v3);
        return result;
    }

    [ConditionalFact(typeof(AdvSimd.Arm64), nameof(AdvSimd.Arm64.IsSupported))]
    public static int TestEntryPoint()
    {
        byte* p = stackalloc byte[672];
        int[] table0 = { 3, 1, 27, 25 };
        int[] table1 = { 5, 14, 31, 21 };
        int[] table2 = { 36, 11, 0, 24 };
        var random = new Random(1981);
        for (int test = 0; test < 1000; test++)
        {
            for (int i = 0; i < 672; i++)
            {
                p[i] = (byte)random.Next(256);
            }

            // Exercise every TBL index, including its zero-on-out-of-range behavior.
            for (int lane = 0; lane < 16; lane++)
            {
                p[656 + lane] = (byte)(((test + lane) & 255) ^ p[256 + lane] ^ p[512 + lane]);
            }

            var actual = Run(p);
            for (int lane = 0; lane < 16; lane++)
            {
                int index = p[656 + lane] ^ p[256 + lane] ^ p[512 + lane];
                byte expected = 0;
                if (index < 64)
                {
                    expected = (byte)(p[table0[index / 16] * 16 + index % 16] ^
                                      p[table1[index / 16] * 16 + index % 16] ^
                                      p[table2[index / 16] * 16 + index % 16]);
                }
                for (int vector = 0; vector < 39; vector++)
                {
                    if (vector != 16 && vector != 32)
                    {
                        expected ^= p[vector * 16 + lane];
                    }
                }

                if (actual.GetElement(lane) != expected)
                {
                    Console.WriteLine($"FAIL test={test} lane={lane}");
                    return 1;
                }
            }
        }
        Console.WriteLine("PASS: 1000 inputs, 16000 scalar-checked lanes");
        return 100;
    }
}
