// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Unit tests for the masks conversion optimization
// Uses vectors as masks and vice versa.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Threading;
using Xunit;

public class EmbeddedLoads
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(T value) { }

    [Fact]
    public static void TestEntryPoint()
    {

        if (Sve.IsSupported)
        {
            int[] array = new int[10];

            Vector<int> op1 = Vector.Create<int>(11);
            Vector<int> op2 = Vector.Create<int>(22);
            Vector<int> op3 = Vector.Create<int>(33);
            Vector<long> opl1 = Vector.Create<long>(44);
            Vector<long> opl2 = Vector.Create<long>(55);

            CndSelectEmbeddedOp3LoadTrueMask(array, op1);
            CndSelectEmbeddedOp3LoadAllBits(array, op1);
            CndSelectEmbeddedOp3LoadFalseMask(array, op1);
            CndSelectEmbeddedOp3LoadZero(array, op1);
        }
    }

    // SVE load operation with embedded mask inside a conditional select

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void CndSelectEmbeddedOp3LoadTrueMask(int[] array, Vector<int> op1) {
        //ARM6-FULL-LINE: ldnf1w  { {{z[0-9]+}}.s }, {{p[0-9]+}}/m, [{{x[0-9]+}}]
        fixed (int* arr_ptr = array)
        {
            var result = Sve.ConditionalSelect(Sve.CreateTrueMaskInt32(), op1, Sve.LoadVectorNonFaulting(arr_ptr));
            Consume(result);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void CndSelectEmbeddedOp3LoadAllBits(int[] array, Vector<int> op1) {
        //ARM6-FULL-LINE: ldnf1w  { {{z[0-9]+}}.s }, {{p[0-9]+}}/m, [{{x[0-9]+}}]
        fixed (int* arr_ptr = array)
        {
            var result = Sve.ConditionalSelect(Vector<int>.AllBitsSet, op1, Sve.LoadVectorNonFaulting(arr_ptr));
            Consume(result);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void CndSelectEmbeddedOp3LoadFalseMask(int[] array, Vector<int> op1) {
        //ARM6-FULL-LINE: ldnf1w  { {{z[0-9]+}}.s }, {{p[0-9]+}}/m, [{{x[0-9]+}}]
        fixed (int* arr_ptr = array)
        {
            var result = Sve.ConditionalSelect(Sve.CreateFalseMaskInt32(), op1, Sve.LoadVectorNonFaulting(arr_ptr));
            Consume(result);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void CndSelectEmbeddedOp3LoadZero(int[] array, Vector<int> op1) {
        //ARM6-FULL-LINE: ldnf1w  { {{z[0-9]+}}.s }, {{p[0-9]+}}/m, [{{x[0-9]+}}]
        fixed (int* arr_ptr = array)
        {
            var result = Sve.ConditionalSelect(Vector<int>.Zero, op1, Sve.LoadVectorNonFaulting(arr_ptr));
            Consume(result);
        }
    }

}
