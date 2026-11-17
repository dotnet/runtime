// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

// Shared by Runtime_137717 and the Native AOT smoke test X64Avx512_NoAvx512v3. Both run on hardware with AVX-512
// while the AVX-VNNI, AVX-VNNI-INT8 and AVX-VNNI-INT16 instructions can only be VEX encoded, which cannot address
// ymm16-ymm31, so the JIT must not allocate those registers to any of their operands.
internal static class VnniRegisterPressure
{
    // The number of left and right vectors, and then of results, each worker keeps live at once, so
    // that some of them would be allocated to ymm16-ymm31 on any OS.
    private const int VectorCount = 12;

    public const int ResultCount = VectorCount * 8;

    // Each of these returns how many of the ResultCount results are wrong.
    public static int CountWrongAvxVnniResults(Random random)
    {
        int[] addend = RandomValues(random, 8, -1000, 1000);
        int[] left = RandomValues(random, VectorCount * 32, byte.MinValue, byte.MaxValue);
        int[] right = RandomValues(random, VectorCount * 32, sbyte.MinValue, sbyte.MaxValue);
        int[] actual = new int[ResultCount];

        MultiplyAvxVnni(Vector256.Create(addend), Array.ConvertAll(left, x => (byte)x), Array.ConvertAll(right, x => (sbyte)x), actual);
        return CountWrongResults(actual, addend, left, right);
    }

    public static int CountWrongAvxVnniInt8Results(Random random)
    {
        int[] addend = RandomValues(random, 8, -1000, 1000);
        int[] left = RandomValues(random, VectorCount * 32, sbyte.MinValue, sbyte.MaxValue);
        int[] right = RandomValues(random, VectorCount * 32, sbyte.MinValue, sbyte.MaxValue);
        int[] actual = new int[ResultCount];

        MultiplyAvxVnniInt8(Vector256.Create(addend), Array.ConvertAll(left, x => (sbyte)x), Array.ConvertAll(right, x => (sbyte)x), actual);
        return CountWrongResults(actual, addend, left, right);
    }

    public static int CountWrongAvxVnniInt16Results(Random random)
    {
        // Small enough that no sum overflows, so the saturating and the wrapping forms agree.
        int[] addend = RandomValues(random, 8, -1000, 1000);
        int[] left = RandomValues(random, VectorCount * 16, -10000, 10000);
        int[] right = RandomValues(random, VectorCount * 16, 0, 10000);
        int[] actual = new int[ResultCount];

        MultiplyAvxVnniInt16(Vector256.Create(addend), Array.ConvertAll(left, x => (short)x), Array.ConvertAll(right, x => (ushort)x), actual);
        return CountWrongResults(actual, addend, left, right);
    }

    private static int[] RandomValues(Random random, int count, int minValue, int maxValue)
    {
        int[] values = new int[count];

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = random.Next(minValue, maxValue + 1);
        }

        return values;
    }

    // Each of the VectorCount results has eight int lanes, and each lane is the addend plus the
    // products of the elements of the left and right vectors that belong to that lane.
    private static int CountWrongResults(int[] actual, int[] addend, int[] left, int[] right)
    {
        int elementsPerVector = left.Length / VectorCount;
        int elementsPerLane = elementsPerVector / 8;
        int wrong = 0;

        for (int v = 0; v < VectorCount; v++)
        {
            for (int lane = 0; lane < 8; lane++)
            {
                int expected = addend[lane];

                for (int j = 0; j < elementsPerLane; j++)
                {
                    int i = (v * elementsPerVector) + (lane * elementsPerLane) + j;
                    expected += left[i] * right[i];
                }

                if (actual[(v * 8) + lane] != expected)
                {
                    wrong++;
                }
            }
        }

        return wrong;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void MultiplyAvxVnni(Vector256<int> addend, byte[] left, sbyte[] right, int[] destination)
    {
        Vector256<byte> x0 = Vector256.Create(left, 0);
        Vector256<byte> x1 = Vector256.Create(left, 32);
        Vector256<byte> x2 = Vector256.Create(left, 64);
        Vector256<byte> x3 = Vector256.Create(left, 96);
        Vector256<byte> x4 = Vector256.Create(left, 128);
        Vector256<byte> x5 = Vector256.Create(left, 160);
        Vector256<byte> x6 = Vector256.Create(left, 192);
        Vector256<byte> x7 = Vector256.Create(left, 224);
        Vector256<byte> x8 = Vector256.Create(left, 256);
        Vector256<byte> x9 = Vector256.Create(left, 288);
        Vector256<byte> x10 = Vector256.Create(left, 320);
        Vector256<byte> x11 = Vector256.Create(left, 352);

        Vector256<sbyte> y0 = Vector256.Create(right, 0);
        Vector256<sbyte> y1 = Vector256.Create(right, 32);
        Vector256<sbyte> y2 = Vector256.Create(right, 64);
        Vector256<sbyte> y3 = Vector256.Create(right, 96);
        Vector256<sbyte> y4 = Vector256.Create(right, 128);
        Vector256<sbyte> y5 = Vector256.Create(right, 160);
        Vector256<sbyte> y6 = Vector256.Create(right, 192);
        Vector256<sbyte> y7 = Vector256.Create(right, 224);
        Vector256<sbyte> y8 = Vector256.Create(right, 256);
        Vector256<sbyte> y9 = Vector256.Create(right, 288);
        Vector256<sbyte> y10 = Vector256.Create(right, 320);
        Vector256<sbyte> y11 = Vector256.Create(right, 352);

        Vector256<int> r0 = AvxVnni.MultiplyWideningAndAdd(addend, x0, y0);
        Vector256<int> r1 = AvxVnni.MultiplyWideningAndAddSaturate(addend, x1, y1);
        Vector256<int> r2 = AvxVnni.MultiplyWideningAndAdd(addend, x2, y2);
        Vector256<int> r3 = AvxVnni.MultiplyWideningAndAddSaturate(addend, x3, y3);
        Vector256<int> r4 = AvxVnni.MultiplyWideningAndAdd(addend, x4, y4);
        Vector256<int> r5 = AvxVnni.MultiplyWideningAndAddSaturate(addend, x5, y5);
        Vector256<int> r6 = AvxVnni.MultiplyWideningAndAdd(addend, x6, y6);
        Vector256<int> r7 = AvxVnni.MultiplyWideningAndAddSaturate(addend, x7, y7);
        Vector256<int> r8 = AvxVnni.MultiplyWideningAndAdd(addend, x8, y8);
        Vector256<int> r9 = AvxVnni.MultiplyWideningAndAddSaturate(addend, x9, y9);
        Vector256<int> r10 = AvxVnni.MultiplyWideningAndAdd(addend, x10, y10);
        Vector256<int> r11 = AvxVnni.MultiplyWideningAndAddSaturate(addend, x11, y11);

        r0.CopyTo(destination, 0);
        r1.CopyTo(destination, 8);
        r2.CopyTo(destination, 16);
        r3.CopyTo(destination, 24);
        r4.CopyTo(destination, 32);
        r5.CopyTo(destination, 40);
        r6.CopyTo(destination, 48);
        r7.CopyTo(destination, 56);
        r8.CopyTo(destination, 64);
        r9.CopyTo(destination, 72);
        r10.CopyTo(destination, 80);
        r11.CopyTo(destination, 88);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void MultiplyAvxVnniInt8(Vector256<int> addend, sbyte[] left, sbyte[] right, int[] destination)
    {
        Vector256<sbyte> x0 = Vector256.Create(left, 0);
        Vector256<sbyte> x1 = Vector256.Create(left, 32);
        Vector256<sbyte> x2 = Vector256.Create(left, 64);
        Vector256<sbyte> x3 = Vector256.Create(left, 96);
        Vector256<sbyte> x4 = Vector256.Create(left, 128);
        Vector256<sbyte> x5 = Vector256.Create(left, 160);
        Vector256<sbyte> x6 = Vector256.Create(left, 192);
        Vector256<sbyte> x7 = Vector256.Create(left, 224);
        Vector256<sbyte> x8 = Vector256.Create(left, 256);
        Vector256<sbyte> x9 = Vector256.Create(left, 288);
        Vector256<sbyte> x10 = Vector256.Create(left, 320);
        Vector256<sbyte> x11 = Vector256.Create(left, 352);

        Vector256<sbyte> y0 = Vector256.Create(right, 0);
        Vector256<sbyte> y1 = Vector256.Create(right, 32);
        Vector256<sbyte> y2 = Vector256.Create(right, 64);
        Vector256<sbyte> y3 = Vector256.Create(right, 96);
        Vector256<sbyte> y4 = Vector256.Create(right, 128);
        Vector256<sbyte> y5 = Vector256.Create(right, 160);
        Vector256<sbyte> y6 = Vector256.Create(right, 192);
        Vector256<sbyte> y7 = Vector256.Create(right, 224);
        Vector256<sbyte> y8 = Vector256.Create(right, 256);
        Vector256<sbyte> y9 = Vector256.Create(right, 288);
        Vector256<sbyte> y10 = Vector256.Create(right, 320);
        Vector256<sbyte> y11 = Vector256.Create(right, 352);

        Vector256<int> r0 = AvxVnniInt8.MultiplyWideningAndAdd(addend, x0, y0);
        Vector256<int> r1 = AvxVnniInt8.MultiplyWideningAndAddSaturate(addend, x1, y1);
        Vector256<int> r2 = AvxVnniInt8.MultiplyWideningAndAdd(addend, x2, y2);
        Vector256<int> r3 = AvxVnniInt8.MultiplyWideningAndAddSaturate(addend, x3, y3);
        Vector256<int> r4 = AvxVnniInt8.MultiplyWideningAndAdd(addend, x4, y4);
        Vector256<int> r5 = AvxVnniInt8.MultiplyWideningAndAddSaturate(addend, x5, y5);
        Vector256<int> r6 = AvxVnniInt8.MultiplyWideningAndAdd(addend, x6, y6);
        Vector256<int> r7 = AvxVnniInt8.MultiplyWideningAndAddSaturate(addend, x7, y7);
        Vector256<int> r8 = AvxVnniInt8.MultiplyWideningAndAdd(addend, x8, y8);
        Vector256<int> r9 = AvxVnniInt8.MultiplyWideningAndAddSaturate(addend, x9, y9);
        Vector256<int> r10 = AvxVnniInt8.MultiplyWideningAndAdd(addend, x10, y10);
        Vector256<int> r11 = AvxVnniInt8.MultiplyWideningAndAddSaturate(addend, x11, y11);

        r0.CopyTo(destination, 0);
        r1.CopyTo(destination, 8);
        r2.CopyTo(destination, 16);
        r3.CopyTo(destination, 24);
        r4.CopyTo(destination, 32);
        r5.CopyTo(destination, 40);
        r6.CopyTo(destination, 48);
        r7.CopyTo(destination, 56);
        r8.CopyTo(destination, 64);
        r9.CopyTo(destination, 72);
        r10.CopyTo(destination, 80);
        r11.CopyTo(destination, 88);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void MultiplyAvxVnniInt16(Vector256<int> addend, short[] left, ushort[] right, int[] destination)
    {
        Vector256<short> x0 = Vector256.Create(left, 0);
        Vector256<short> x1 = Vector256.Create(left, 16);
        Vector256<short> x2 = Vector256.Create(left, 32);
        Vector256<short> x3 = Vector256.Create(left, 48);
        Vector256<short> x4 = Vector256.Create(left, 64);
        Vector256<short> x5 = Vector256.Create(left, 80);
        Vector256<short> x6 = Vector256.Create(left, 96);
        Vector256<short> x7 = Vector256.Create(left, 112);
        Vector256<short> x8 = Vector256.Create(left, 128);
        Vector256<short> x9 = Vector256.Create(left, 144);
        Vector256<short> x10 = Vector256.Create(left, 160);
        Vector256<short> x11 = Vector256.Create(left, 176);

        Vector256<ushort> y0 = Vector256.Create(right, 0);
        Vector256<ushort> y1 = Vector256.Create(right, 16);
        Vector256<ushort> y2 = Vector256.Create(right, 32);
        Vector256<ushort> y3 = Vector256.Create(right, 48);
        Vector256<ushort> y4 = Vector256.Create(right, 64);
        Vector256<ushort> y5 = Vector256.Create(right, 80);
        Vector256<ushort> y6 = Vector256.Create(right, 96);
        Vector256<ushort> y7 = Vector256.Create(right, 112);
        Vector256<ushort> y8 = Vector256.Create(right, 128);
        Vector256<ushort> y9 = Vector256.Create(right, 144);
        Vector256<ushort> y10 = Vector256.Create(right, 160);
        Vector256<ushort> y11 = Vector256.Create(right, 176);

        Vector256<int> r0 = AvxVnniInt16.MultiplyWideningAndAdd(addend, x0, y0);
        Vector256<int> r1 = AvxVnniInt16.MultiplyWideningAndAddSaturate(addend, x1, y1);
        Vector256<int> r2 = AvxVnniInt16.MultiplyWideningAndAdd(addend, x2, y2);
        Vector256<int> r3 = AvxVnniInt16.MultiplyWideningAndAddSaturate(addend, x3, y3);
        Vector256<int> r4 = AvxVnniInt16.MultiplyWideningAndAdd(addend, x4, y4);
        Vector256<int> r5 = AvxVnniInt16.MultiplyWideningAndAddSaturate(addend, x5, y5);
        Vector256<int> r6 = AvxVnniInt16.MultiplyWideningAndAdd(addend, x6, y6);
        Vector256<int> r7 = AvxVnniInt16.MultiplyWideningAndAddSaturate(addend, x7, y7);
        Vector256<int> r8 = AvxVnniInt16.MultiplyWideningAndAdd(addend, x8, y8);
        Vector256<int> r9 = AvxVnniInt16.MultiplyWideningAndAddSaturate(addend, x9, y9);
        Vector256<int> r10 = AvxVnniInt16.MultiplyWideningAndAdd(addend, x10, y10);
        Vector256<int> r11 = AvxVnniInt16.MultiplyWideningAndAddSaturate(addend, x11, y11);

        r0.CopyTo(destination, 0);
        r1.CopyTo(destination, 8);
        r2.CopyTo(destination, 16);
        r3.CopyTo(destination, 24);
        r4.CopyTo(destination, 32);
        r5.CopyTo(destination, 40);
        r6.CopyTo(destination, 48);
        r7.CopyTo(destination, 56);
        r8.CopyTo(destination, 64);
        r9.CopyTo(destination, 72);
        r10.CopyTo(destination, 80);
        r11.CopyTo(destination, 88);
    }
}
