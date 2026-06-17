// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class PredicateInstructions
{
    private static readonly float[] s_floatValues = new float[64];
    private static readonly double[] s_doubleValues = new double[64];

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Fact]
    public static void TestPredicateInstructions()
    {
        if (Sve.IsSupported)
        {
            Vector<sbyte>  vecsb = Vector.Create<sbyte>(2);
            Vector<short>  vecs  = Vector.Create<short>(2);
            Vector<ushort> vecus = Vector.Create<ushort>(2);
            Vector<int>    veci  = Vector.Create<int>(3);
            Vector<uint>   vecui = Vector.Create<uint>(5);
            Vector<long>   vecl  = Vector.Create<long>(7);

            ZipLowMask(vecs, vecs);
            ZipHighMask(vecui, vecui);
            UnzipOddMask(vecs, vecs);
            UnzipEvenMask(vecsb, vecsb);
            TransposeEvenMask(vecl, vecl);
            TransposeOddMask(vecs, vecs);
            ReverseElementMask(vecs, vecs);
            AndMask(vecs, vecs);
            BitwiseClearMask(vecs, vecs);
            XorMask(veci, veci);
            OrMask(vecs, vecs);
            ConditionalSelectMask(veci, veci, veci);

            UnzipEvenZipLowMask(vecs, vecs);
            TransposeEvenAndMask(vecs, vecs, vecs);

            PredicateCastFloatLoad(s_floatValues, 0, s_floatValues.Length);
            PredicateCastFloatLocalLoad(s_floatValues, 0, s_floatValues.Length);
            PointerCastFloatLoad(s_floatValues, 0, s_floatValues.Length);
            WhileLessThanSingleFloatLoad(s_floatValues, 0, s_floatValues.Length);
            PredicateCastFloatLoop(s_floatValues, s_floatValues, s_floatValues.Length);

            PredicateCastDoubleLoad(s_doubleValues, 0, s_doubleValues.Length);
            PredicateCastDoubleLocalLoad(s_doubleValues, 0, s_doubleValues.Length);
            PointerCastDoubleLoad(s_doubleValues, 0, s_doubleValues.Length);
            WhileLessThanDoubleLoad(s_doubleValues, 0, s_doubleValues.Length);
            PredicateCastDoubleLoop(s_doubleValues, s_doubleValues, s_doubleValues.Length);
        }
    }

    // These should use the predicate variants.
    // Sve intrinsics that return masks (Compare) or use mask arguments (CreateBreakAfterMask) are used
    // to ensure masks are used.


    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> ZipLowMask(Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: zip1 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.ZipLow(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<uint> ZipHighMask(Vector<uint> a, Vector<uint> b)
    {
        //ARM64-FULL-LINE: zip2 {{p[0-9]+}}.s, {{p[0-9]+}}.s, {{p[0-9]+}}.s
        return Sve.CreateBreakAfterMask(Sve.ZipHigh(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)), Sve.CreateTrueMaskUInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<sbyte> UnzipEvenMask(Vector<sbyte> a, Vector<sbyte> b)
    {
        //ARM64-FULL-LINE: uzp1 {{p[0-9]+}}.b, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.UnzipEven(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> UnzipOddMask(Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: uzp2 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.CreateBreakAfterMask(Sve.UnzipOdd(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)), Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<long> TransposeEvenMask(Vector<long> a, Vector<long> b)
    {
        //ARM64-FULL-LINE: trn1 {{p[0-9]+}}.d, {{p[0-9]+}}.d, {{p[0-9]+}}.d
        return Sve.CreateBreakAfterMask(Sve.TransposeEven(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)), Sve.CreateFalseMaskInt64());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> TransposeOddMask(Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: trn2 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.TransposeOdd(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> ReverseElementMask(Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: rev {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.CreateBreakAfterMask(Sve.ReverseElement(Sve.CompareGreaterThan(a, b)), Sve.CreateFalseMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> AndMask(Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: and {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.CreateBreakAfterMask(
            Sve.ConditionalSelect(
                Sve.CreateTrueMaskInt16(),
                Sve.And(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)),
                Vector<short>.Zero),
            Sve.CreateFalseMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> BitwiseClearMask(Vector<short> a, Vector<short> b)
    {
        //TODO-SVE: Restore check for SVE once >128bits is supported
        //ARM64-FULL-LINE: {{bic .*}}
        // {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.ConditionalSelect(
                Sve.CreateTrueMaskInt16(),
                Sve.BitwiseClear(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)),
                Vector<short>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> XorMask(Vector<int> a, Vector<int> b)
    {
        //ARM64-FULL-LINE: eor {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.CreateBreakAfterMask(
            Sve.ConditionalSelect(
                Sve.CreateTrueMaskInt32(),
                Sve.Xor(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)),
                Vector<int>.Zero),
            Sve.CreateFalseMaskInt32());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> OrMask(Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: orr {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.ConditionalSelect(
                Sve.CreateTrueMaskInt16(),
                Sve.Or(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)),
                Vector<short>.Zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> ConditionalSelectMask(Vector<int> v, Vector<int> a, Vector<int> b)
    {
        // Use a passed in vector for the mask to prevent optimising away the select
        //ARM64-FULL-LINE: sel {{p[0-9]+}}.b, {{p[0-9]+}}, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        return Sve.CreateBreakAfterMask(
            Sve.ConditionalSelect(v, Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)),
            Sve.CreateFalseMaskInt32());
    }

    // These have multiple uses of the predicate variants

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> UnzipEvenZipLowMask(Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: zip1 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        //ARM64-FULL-LINE: uzp1 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.CreateBreakAfterMask(
            Sve.UnzipEven(
                Sve.ZipLow(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)),
                Sve.CompareLessThan(a, b)),
            Sve.CreateTrueMaskInt16());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<short> TransposeEvenAndMask(Vector<short> v, Vector<short> a, Vector<short> b)
    {
        //ARM64-FULL-LINE: and {{p[0-9]+}}.b, {{p[0-9]+}}/z, {{p[0-9]+}}.b, {{p[0-9]+}}.b
        //ARM64-FULL-LINE: trn1 {{p[0-9]+}}.h, {{p[0-9]+}}.h, {{p[0-9]+}}.h
        return Sve.TransposeEven(
                Sve.CompareGreaterThan(a, b),
                Sve.ConditionalSelect(
                    Sve.CreateTrueMaskInt16(),
                    Sve.And(Sve.CompareGreaterThan(a, b), Sve.CompareEqual(a, b)),
                    Sve.CompareLessThan(a, b)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe Vector<float> PredicateCastFloatLoad(float[] values, int index, int length)
    {
        //ARM64-FULL-LINE: whilelt {{p[0-9]+}}.s, {{w[0-9]+}}, {{w[0-9]+}}
        //ARM64-NOT: mov {{z[0-9]+}}.s, {{p[0-9]+}}/z, #1
        //ARM64-NOT: cmpne {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, #0
        //ARM64-FULL-LINE: ld1w { {{z[0-9]+}}.s }, {{p[0-9]+}}/z, [{{x[0-9]+}}]
        fixed (float* ptr = values)
        {
            Vector<uint> mask = Sve.CreateWhileLessThanMaskUInt32(index, length);
            return Sve.LoadVector((Vector<float>)mask, ptr + index);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe Vector<float> PointerCastFloatLoad(float[] values, int index, int length)
    {
        //ARM64-FULL-LINE: whilelt {{p[0-9]+}}.s, {{w[0-9]+}}, {{w[0-9]+}}
        //ARM64-NOT: mov {{z[0-9]+}}.s, {{p[0-9]+}}/z, #1
        //ARM64-NOT: cmpne {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, #0
        //ARM64-FULL-LINE: ld1w { {{z[0-9]+}}.s }, {{p[0-9]+}}/z, [{{x[0-9]+}}]
        fixed (float* ptr = values)
        {
            Vector<uint> mask = Sve.CreateWhileLessThanMaskUInt32(index, length);
            return (Vector<float>)Sve.LoadVector(mask, (uint*)(ptr + index));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe Vector<float> PredicateCastFloatLocalLoad(float[] values, int index, int length)
    {
        //ARM64-FULL-LINE: whilelt {{p[0-9]+}}.s, {{w[0-9]+}}, {{w[0-9]+}}
        //ARM64-NOT: mov {{z[0-9]+}}.s, {{p[0-9]+}}/z, #1
        //ARM64-NOT: cmpne {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, #0
        //ARM64-FULL-LINE: ld1w { {{z[0-9]+}}.s }, {{p[0-9]+}}/z, [{{x[0-9]+}}]
        fixed (float* ptr = values)
        {
            Vector<uint> uintMask = Sve.CreateWhileLessThanMaskUInt32(index, length);
            Vector<float> floatMask = (Vector<float>)uintMask;
            return Sve.LoadVector(floatMask, ptr + index);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe Vector<float> WhileLessThanSingleFloatLoad(float[] values, int index, int length)
    {
        //ARM64-FULL-LINE: whilelt {{p[0-9]+}}.s, {{w[0-9]+}}, {{w[0-9]+}}
        //ARM64-NOT: mov {{z[0-9]+}}.s, {{p[0-9]+}}/z, #1
        //ARM64-NOT: cmpne {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, #0
        //ARM64-FULL-LINE: ld1w { {{z[0-9]+}}.s }, {{p[0-9]+}}/z, [{{x[0-9]+}}]
        fixed (float* ptr = values)
        {
            Vector<float> mask = Sve.CreateWhileLessThanMaskSingle(index, length);
            return Sve.LoadVector(mask, ptr + index);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void PredicateCastFloatLoop(float[] input, float[] output, int length)
    {
        //ARM64-FULL-LINE: whilelt {{p[0-9]+}}.s, {{w[0-9]+}}, {{w[0-9]+}}
        //ARM64-NOT: mov {{z[0-9]+}}.s, {{p[0-9]+}}/z, #1
        //ARM64-NOT: cmpne {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, #0
        //ARM64-FULL-LINE: ld1w { {{z[0-9]+}}.s }, {{p[0-9]+}}/z, [{{x[0-9]+}}]
        //ARM64-NOT: mov {{z[0-9]+}}.s, {{p[0-9]+}}/z, #1
        //ARM64-NOT: cmpne {{p[0-9]+}}.s, {{p[0-9]+}}/z, {{z[0-9]+}}.s, #0
        //ARM64-FULL-LINE: st1w { {{z[0-9]+}}.s }, {{p[0-9]+}}, [{{x[0-9]+}}]
        fixed (float* inputPtr = input, outputPtr = output)
        {
            int i = 0;
            int count = (int)Sve.Count32BitElements();

            while (i < length)
            {
                Vector<uint> loopMask = Sve.CreateWhileLessThanMaskUInt32(i, length);
                Vector<float> floatMask = (Vector<float>)loopMask;
                Vector<float> value = Sve.LoadVector(floatMask, inputPtr + i);
                Sve.StoreAndZip(floatMask, outputPtr + i, value);

                i += count;
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe Vector<double> PredicateCastDoubleLoad(double[] values, int index, int length)
    {
        //ARM64-FULL-LINE: whilelt {{p[0-9]+}}.d, {{w[0-9]+}}, {{w[0-9]+}}
        //ARM64-NOT: mov {{z[0-9]+}}.d, {{p[0-9]+}}/z, #1
        //ARM64-NOT: cmpne {{p[0-9]+}}.d, {{p[0-9]+}}/z, {{z[0-9]+}}.d, #0
        //ARM64-FULL-LINE: ld1d { {{z[0-9]+}}.d }, {{p[0-9]+}}/z, [{{x[0-9]+}}]
        fixed (double* ptr = values)
        {
            Vector<ulong> mask = Sve.CreateWhileLessThanMaskUInt64(index, length);
            return Sve.LoadVector((Vector<double>)mask, ptr + index);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe Vector<double> PointerCastDoubleLoad(double[] values, int index, int length)
    {
        //ARM64-FULL-LINE: whilelt {{p[0-9]+}}.d, {{w[0-9]+}}, {{w[0-9]+}}
        //ARM64-NOT: mov {{z[0-9]+}}.d, {{p[0-9]+}}/z, #1
        //ARM64-NOT: cmpne {{p[0-9]+}}.d, {{p[0-9]+}}/z, {{z[0-9]+}}.d, #0
        //ARM64-FULL-LINE: ld1d { {{z[0-9]+}}.d }, {{p[0-9]+}}/z, [{{x[0-9]+}}]
        fixed (double* ptr = values)
        {
            Vector<ulong> mask = Sve.CreateWhileLessThanMaskUInt64(index, length);
            return (Vector<double>)Sve.LoadVector(mask, (ulong*)(ptr + index));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe Vector<double> PredicateCastDoubleLocalLoad(double[] values, int index, int length)
    {
        //ARM64-FULL-LINE: whilelt {{p[0-9]+}}.d, {{w[0-9]+}}, {{w[0-9]+}}
        //ARM64-NOT: mov {{z[0-9]+}}.d, {{p[0-9]+}}/z, #1
        //ARM64-NOT: cmpne {{p[0-9]+}}.d, {{p[0-9]+}}/z, {{z[0-9]+}}.d, #0
        //ARM64-FULL-LINE: ld1d { {{z[0-9]+}}.d }, {{p[0-9]+}}/z, [{{x[0-9]+}}]
        fixed (double* ptr = values)
        {
            Vector<ulong> ulongMask = Sve.CreateWhileLessThanMaskUInt64(index, length);
            Vector<double> doubleMask = (Vector<double>)ulongMask;
            return Sve.LoadVector(doubleMask, ptr + index);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe Vector<double> WhileLessThanDoubleLoad(double[] values, int index, int length)
    {
        //ARM64-FULL-LINE: whilelt {{p[0-9]+}}.d, {{w[0-9]+}}, {{w[0-9]+}}
        //ARM64-NOT: mov {{z[0-9]+}}.d, {{p[0-9]+}}/z, #1
        //ARM64-NOT: cmpne {{p[0-9]+}}.d, {{p[0-9]+}}/z, {{z[0-9]+}}.d, #0
        //ARM64-FULL-LINE: ld1d { {{z[0-9]+}}.d }, {{p[0-9]+}}/z, [{{x[0-9]+}}]
        fixed (double* ptr = values)
        {
            Vector<double> mask = Sve.CreateWhileLessThanMaskDouble(index, length);
            return Sve.LoadVector(mask, ptr + index);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void PredicateCastDoubleLoop(double[] input, double[] output, int length)
    {
        //ARM64-FULL-LINE: whilelt {{p[0-9]+}}.d, {{w[0-9]+}}, {{w[0-9]+}}
        //ARM64-NOT: mov {{z[0-9]+}}.d, {{p[0-9]+}}/z, #1
        //ARM64-NOT: cmpne {{p[0-9]+}}.d, {{p[0-9]+}}/z, {{z[0-9]+}}.d, #0
        //ARM64-FULL-LINE: ld1d { {{z[0-9]+}}.d }, {{p[0-9]+}}/z, [{{x[0-9]+}}]
        //ARM64-NOT: mov {{z[0-9]+}}.d, {{p[0-9]+}}/z, #1
        //ARM64-NOT: cmpne {{p[0-9]+}}.d, {{p[0-9]+}}/z, {{z[0-9]+}}.d, #0
        //ARM64-FULL-LINE: st1d { {{z[0-9]+}}.d }, {{p[0-9]+}}, [{{x[0-9]+}}]
        fixed (double* inputPtr = input, outputPtr = output)
        {
            int i = 0;
            int count = (int)Sve.Count64BitElements();

            while (i < length)
            {
                Vector<ulong> loopMask = Sve.CreateWhileLessThanMaskUInt64(i, length);
                Vector<double> doubleMask = (Vector<double>)loopMask;
                Vector<double> value = Sve.LoadVector(doubleMask, inputPtr + i);
                Sve.StoreAndZip(doubleMask, outputPtr + i, value);

                i += count;
            }
        }
    }
}
