// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    // Loads of loop-invariant fields must be hoisted out of the loop even though local assertion prop marked
    // them non-faulting (GTF_IND_NONFAULTING | GTF_ORDER_SIDEEFF) because the loop condition already
    // dereferenced the object.
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
            // X64: vmovsd   {{xmm[0-9]+}}, qword ptr [{{r[a-z0-9]+}}+0x{{[0-9A-F]+}}]

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        static double SumAfterSameObjectCheck(Data d, int n)
        {
            // `d.Count` is a null check of `d` and the only thing that can throw before `d.Scale`, so hoisting
            // `Scale` cannot move an exception ahead of another: both loads leave the loop.

            // X64: vmovsd   {{xmm[0-9]+}}, qword ptr [{{r[a-z0-9]+}}+0x{{[0-9A-F]+}}]
            // X64-NOT: vaddsd   {{xmm[0-9]+}}, {{xmm[0-9]+}}, qword ptr [

            double sum = 0;
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                count += d.Count;
                sum += d.Scale;
            }
            return sum + count;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static double SumAfterOtherObjectCheck(Data d, int[] other, int n)
        {
            // `other.Length` can throw for a different object before `d.Scale` is reached. Hoisting `Scale` would
            // let a null `d` throw NullReferenceException where the loop throws for `other` first, so the load
            // must stay in the loop.

            // X64-NOT: vmovsd   {{xmm[0-9]+}}, qword ptr [
            // X64: vaddsd   {{xmm[0-9]+}}, {{xmm[0-9]+}}, qword ptr [{{r[a-z0-9]+}}+0x{{[0-9A-F]+}}]

            double sum = 0;
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                count += other.Length;
                count += d.Count;
                sum += d.Scale;
            }
            return sum + count;
        }

        [Fact]
        public static void TestEntryPoint()
        {
            var d = new Data { Values = new double[] { 1, 2, 3, 4 }, Count = 4, Scale = 12 };
            Assert.Equal(10.0, Sum(d));
            Assert.Equal(25.0, SumScaled(d));
            Assert.Equal(3 * 12.0 + 3 * 4, SumAfterSameObjectCheck(d, 3));
            Assert.Equal(3 * 12.0 + 3 * (2 + 4), SumAfterOtherObjectCheck(d, new int[2], 3));
        }
    }
}
