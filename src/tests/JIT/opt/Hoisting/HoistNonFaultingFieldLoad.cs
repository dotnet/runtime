// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    // https://github.com/dotnet/runtime/issues/134352: loads of loop-invariant fields must be hoisted out of the
    // loop even though local assertion prop marked them non-faulting (GTF_IND_NONFAULTING | GTF_ORDER_SIDEEFF)
    // because an earlier dereference of the same object precedes them.
    //
    // The checks avoid AVX-specific spellings ("{{v?}}movsd", "addsd" also matches "vaddsd") so that they hold
    // with EnableAVX=0, and every check is anchored to the loop: a hoisted load is matched before the loop
    // label, and the loop body (label .. back edge) is checked for the absence or presence of the load.
    public static class HoistNonFaultingFieldLoad
    {
        public sealed class Data
        {
            public double[] Values;
            public int Count;
            public double Scale;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static double Sum(Data d)
        {
            // `Count` is loaded for the zero-trip test and `Values` once before the loop. The only memory
            // operands in the loop are the array length and the array element, indexed through the hoisted
            // array reference: no load through the object in the loop.

            // X64: mov      [[COUNT:e[a-z0-9]+]], dword ptr [{{r[a-z0-9]+}}+0x{{[0-9A-F]+}}]
            // X64: test     [[COUNT]], [[COUNT]]
            // X64: mov      {{r[a-z0-9]+}}, gword ptr [{{r[a-z0-9]+}}+0x{{[0-9A-F]+}}]

            // X64: [[LOOP:G_M[0-9]+_IG[0-9]+]]:
            // X64-NOT: gword ptr [{{r[a-z0-9]+}}+0x{{[0-9A-F]+}}]
            // X64: jl       SHORT [[LOOP]]

            double sum = 0;
            for (int i = 0; i < d.Count; i++)
            {
                sum += d.Values[i];
            }
            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static double SumScaled(Data d)
        {
            // `Scale` is loaded once before the loop; the loop has no memory operand at all.

            // X64: mov      [[COUNT:e[a-z0-9]+]], dword ptr [{{r[a-z0-9]+}}+0x{{[0-9A-F]+}}]
            // X64: test     [[COUNT]], [[COUNT]]
            // X64: {{v?}}movsd {{xmm[0-9]+}}, qword ptr [{{r[a-z0-9]+}}+0x{{[0-9A-F]+}}]

            // X64: [[LOOP:G_M[0-9]+_IG[0-9]+]]:
            // X64-NOT: ptr [
            // X64: jl       SHORT [[LOOP]]

            double sum = 0;
            for (int i = 0; i < d.Count; i++)
            {
                sum += d.Scale / (i + 1);
            }
            return sum;
        }

        // The 28 integer arguments use up the integer hoisting budget on every target (the largest budget is 28
        // registers, x64 with APX), so the `d.Count` load below stays in the loop even though it is invariant.
        // The loops are written as do/while so that the body is the loop header regardless of its size (a for
        // loop this large is not inverted).
        [MethodImpl(MethodImplOptions.NoInlining)]
        static double SumAfterUnhoistedSameObjectCheck(Data d, int n,
            int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13,
            int a14, int a15, int a16, int a17, int a18, int a19, int a20, int a21, int a22, int a23, int a24, int a25, int a26, int a27)
        {
            // `d.Count` is the first thing that can throw and stays in the loop; it is a null check of `d`, so
            // the `d.Scale` load it proved may still leave the loop. If `d` is null, the loop throws on
            // `d.Count` before anything else happens, and so does the hoisted copy.

            // X64: {{v?}}movsd {{xmm[0-9]+}}, qword ptr [{{r[a-z0-9]+}}+0x{{[0-9A-F]+}}]
            // X64: [[LOOP:G_M[0-9]+_IG[0-9]+]]:
            // X64: add      {{e[a-z0-9]+}}, dword ptr [{{r[a-z0-9]+}}+0x{{[0-9A-F]+}}]
            // X64-NOT: qword ptr [
            // X64: jl       [[LOOP]]

            int count = 0;
            double sum = 0;
            int i = 0;
            do
            {
                count += d.Count;
                sum += d.Scale;
                count += (i ^ a0) + (i ^ a1) + (i ^ a2) + (i ^ a3) + (i ^ a4) + (i ^ a5) + (i ^ a6) + (i ^ a7)
                    + (i ^ a8) + (i ^ a9) + (i ^ a10) + (i ^ a11) + (i ^ a12) + (i ^ a13) + (i ^ a14) + (i ^ a15)
                    + (i ^ a16) + (i ^ a17) + (i ^ a18) + (i ^ a19) + (i ^ a20) + (i ^ a21) + (i ^ a22) + (i ^ a23)
                    + (i ^ a24) + (i ^ a25) + (i ^ a26) + (i ^ a27);
                i++;
            }
            while (i < n);
            return sum + count;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static double SumAfterOtherObjectCheck(Data d, int[] other, int n,
            int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13,
            int a14, int a15, int a16, int a17, int a18, int a19, int a20, int a21, int a22, int a23, int a24, int a25, int a26, int a27)
        {
            // `other[0]` can throw IndexOutOfRangeException before `d` is dereferenced. Its bounds check is
            // never hoisted (bounds checks have no value to CSE), so no optimization pass may move `d.Scale`
            // ahead of it: with a null `d` and an empty `other`, the loop must throw for `other`. The load
            // stays in the loop, folded into the add.

            // X64: align
            // X64: [[LOOP:G_M[0-9]+_IG[0-9]+]]:
            // X64: jbe
            // X64: addsd {{.*}}qword ptr [{{r[a-z0-9]+}}+0x{{[0-9A-F]+}}]
            // X64: jl       [[LOOP]]

            int count = 0;
            double sum = 0;
            int i = 0;
            do
            {
                count += other[0];
                count += d.Count;
                sum += d.Scale;
                count += (i ^ a0) + (i ^ a1) + (i ^ a2) + (i ^ a3) + (i ^ a4) + (i ^ a5) + (i ^ a6) + (i ^ a7)
                    + (i ^ a8) + (i ^ a9) + (i ^ a10) + (i ^ a11) + (i ^ a12) + (i ^ a13) + (i ^ a14) + (i ^ a15)
                    + (i ^ a16) + (i ^ a17) + (i ^ a18) + (i ^ a19) + (i ^ a20) + (i ^ a21) + (i ^ a22) + (i ^ a23)
                    + (i ^ a24) + (i ^ a25) + (i ^ a26) + (i ^ a27);
                i++;
            }
            while (i < n);
            return sum + count;
        }

        [Fact]
        public static void TestEntryPoint()
        {
            var d = new Data { Values = new double[] { 1, 2, 3, 4 }, Count = 4, Scale = 12 };
            Assert.Equal(10.0, Sum(d));
            Assert.Equal(25.0, SumScaled(d));
            Assert.Equal(1266.0, SumAfterUnhoistedSameObjectCheck(d, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28));
            Assert.Equal(1272.0, SumAfterOtherObjectCheck(d, new int[] { 2 }, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28));
            Assert.Throws<NullReferenceException>(() => SumAfterUnhoistedSameObjectCheck(null, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28));
            Assert.Throws<IndexOutOfRangeException>(() => SumAfterOtherObjectCheck(null, new int[0], 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28));
        }
    }
}
