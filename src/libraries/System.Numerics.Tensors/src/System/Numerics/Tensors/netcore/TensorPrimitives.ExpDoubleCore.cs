// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        // 2^x and 10^x for vectors of double, used by Exp2Operator and Exp10Operator.
        //
        // Computing b^x as Exp(x * ln(b)) rounds the argument x * ln(b), and Exp turns that rounding into a relative
        // error of several hundred ulps for large |x|. These kernels instead write x * log2(b) = n + f, with n an
        // integer and |f| <= 1/2, and evaluate e^r for r = f * ln(2):
        //
        //   2^x:  n = round(x), r = (x - n) * ln(2). x - n is exact.
        //   10^x: n = round(x * log2(10)), r = (x - n * log10(2)) * ln(10). log10(2) is split into Log10Of2Hi and
        //         Log10Of2Lo. Log10Of2Hi has 40 significant bits, so n * Log10Of2Hi is exact for |n| <= 1100 even
        //         without a fused multiply-add. Log10Of2Lo holds the rest.
        //
        // e^r uses the polynomial of VectorMath.ExpDouble (Vector128.Exp for double). When every lane is within
        // conservative bounds (|x| <= 1020 for 2^x, |x| <= 307 for 10^x), every result is a normal double, and 2^n is
        // applied by adding n to the exponent field.
        // Otherwise, ScaleWide multiplies by 2^n1 * 2^n2, where n1 + n2 = n. This rounds a subnormal result once
        // and makes an overflowing result infinity. Inputs are clamped first to keep n1 and n2 in range.
        // NaN inputs are returned unchanged.
        //
        // The polynomial coefficients are those of VectorMath.ExpDouble, which is based on `vrd2_exp` from
        // amd/aocl-libm-ose.
        // Copyright (C) 2019-2020 Advanced Micro Devices, Inc. All rights reserved.
        // Licensed under the BSD 3-Clause "New" or "Revised" License. See THIRD-PARTY-NOTICES.TXT for the full license text.
        private static class ExpDoubleCore
        {
            // Adding 1.5 * 2^52 to a double of magnitude below 2^51 rounds it to an integer held in the low mantissa bits.
            private const double Huge = 6755399441055744;

            private const double Ln2 = 0.6931471805599453;
            private const double Ln10 = 2.302585092994046;
            private const double Log2Of10 = 3.321928094887362;
            private const double Log10Of2Hi = 0.30102999566361177;      // 0x3FD34413509F6000
            private const double Log10Of2Lo = 3.694239077158931E-13;

            // Inputs up to these magnitudes give n within [-1020, 1020], so every result is a normal double.
            private const ulong Exp2NormalMax = 0x408FE00000000000;     // 1020.0
            private const ulong Exp10NormalMax = 0x4073300000000000;    // 307.0

            // Past these the result is 0 or +infinity; clamping to them keeps n within [-1100, 1030].
            private const double Exp2ClampMin = -1100.0;
            private const double Exp2ClampMax = 1030.0;
            private const double Exp10ClampMin = -330.0;
            private const double Exp10ClampMax = 310.0;

            private const double C03 = 0.5000000000000018;
            private const double C04 = 0.1666666666666617;
            private const double C05 = 0.04166666666649277;
            private const double C06 = 0.008333333333559272;
            private const double C07 = 0.001388888895122404;
            private const double C08 = 0.00019841269432677495;
            private const double C09 = 2.4801486521374483E-05;
            private const double C10 = 2.7557622532543023E-06;
            private const double C11 = 2.7632293298250954E-07;
            private const double C12 = 2.499430431958571E-08;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<double> Exp2(Vector128<double> x)
            {
                if (Vector128.LessThanOrEqualAll(Vector128.Abs(x).AsUInt64(), Vector128.Create(Exp2NormalMax)))
                {
                    Vector128<double> dn = x + Vector128.Create(Huge);
                    Vector128<ulong> n = dn.AsUInt64();
                    dn -= Vector128.Create(Huge);
                    Vector128<double> r = (x - dn) * Vector128.Create(Ln2);
                    return (Polynomial(r).AsUInt64() + (n << 52)).AsDouble();
                }

                return Exp2Wide(x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<double> Exp10(Vector128<double> x)
            {
                if (Vector128.LessThanOrEqualAll(Vector128.Abs(x).AsUInt64(), Vector128.Create(Exp10NormalMax)))
                {
                    Vector128<double> dn = Vector128.MultiplyAddEstimate(x, Vector128.Create(Log2Of10), Vector128.Create(Huge));
                    Vector128<ulong> n = dn.AsUInt64();
                    dn -= Vector128.Create(Huge);
                    Vector128<double> r = ReduceBase10(x, dn);
                    return (Polynomial(r).AsUInt64() + (n << 52)).AsDouble();
                }

                return Exp10Wide(x);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static Vector128<double> Exp2Wide(Vector128<double> x)
            {
                Vector128<double> clamped = Vector128.Min(Vector128.Max(x, Vector128.Create(Exp2ClampMin)), Vector128.Create(Exp2ClampMax));
                Vector128<double> dn = clamped + Vector128.Create(Huge);
                Vector128<double> r = (clamped - (dn - Vector128.Create(Huge))) * Vector128.Create(Ln2);
                return Vector128.ConditionalSelect(Vector128.Equals(x, x), ScaleWide(Polynomial(r), dn), x);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static Vector128<double> Exp10Wide(Vector128<double> x)
            {
                Vector128<double> clamped = Vector128.Min(Vector128.Max(x, Vector128.Create(Exp10ClampMin)), Vector128.Create(Exp10ClampMax));
                Vector128<double> dn = Vector128.MultiplyAddEstimate(clamped, Vector128.Create(Log2Of10), Vector128.Create(Huge));
                Vector128<double> r = ReduceBase10(clamped, dn - Vector128.Create(Huge));
                return Vector128.ConditionalSelect(Vector128.Equals(x, x), ScaleWide(Polynomial(r), dn), x);
            }

            // (x - n * log10(2)) * ln(10). Because n * Log10Of2Hi is exact, the subtraction from x rounds only once,
            // so the reduction is accurate whether or not MultiplyAddEstimate fuses.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<double> ReduceBase10(Vector128<double> x, Vector128<double> n)
            {
                Vector128<double> s = Vector128.MultiplyAddEstimate(n, Vector128.Create(-Log10Of2Hi), x);
                s = Vector128.MultiplyAddEstimate(n, Vector128.Create(-Log10Of2Lo), s);
                return s * Vector128.Create(Ln10);
            }

            // e^r for |r| <= ln(2) / 2, as in VectorMath.ExpDouble.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<double> Polynomial(Vector128<double> r)
            {
                Vector128<double> r2 = r * r;
                Vector128<double> r4 = r2 * r2;
                Vector128<double> r8 = r4 * r4;

                Vector128<double> a = Vector128.MultiplyAddEstimate(Vector128.Create(C12), r, Vector128.Create(C11));
                Vector128<double> b = Vector128.MultiplyAddEstimate(Vector128.Create(C10), r, Vector128.Create(C09));
                Vector128<double> c = Vector128.MultiplyAddEstimate(Vector128.Create(C08), r, Vector128.Create(C07));
                Vector128<double> d = Vector128.MultiplyAddEstimate(Vector128.Create(C06), r, Vector128.Create(C05));
                Vector128<double> e = Vector128.MultiplyAddEstimate(Vector128.Create(C04), r, Vector128.Create(C03));
                Vector128<double> f = r + Vector128<double>.One;

                Vector128<double> high = Vector128.MultiplyAddEstimate(a, r2, b);
                Vector128<double> middle = Vector128.MultiplyAddEstimate(Vector128.MultiplyAddEstimate(c, r2, d), r4, Vector128.MultiplyAddEstimate(e, r2, f));
                return Vector128.MultiplyAddEstimate(high, r8, middle);
            }

            // p * 2^n, where n is the integer in the low bits of nPlusHuge (n + Huge).
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<double> ScaleWide(Vector128<double> p, Vector128<double> nPlusHuge)
            {
                Vector128<long> n = nPlusHuge.AsInt64() - Vector128.Create(Huge).AsInt64();
                Vector128<long> n1 = n >> 1;
                Vector128<long> n2 = n - n1;
                Vector128<long> bias = Vector128.Create(1023L);
                return p * ((n1 + bias) << 52).AsDouble() * ((n2 + bias) << 52).AsDouble();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<double> Exp2(Vector256<double> x)
            {
                if (Vector256.LessThanOrEqualAll(Vector256.Abs(x).AsUInt64(), Vector256.Create(Exp2NormalMax)))
                {
                    Vector256<double> dn = x + Vector256.Create(Huge);
                    Vector256<ulong> n = dn.AsUInt64();
                    dn -= Vector256.Create(Huge);
                    Vector256<double> r = (x - dn) * Vector256.Create(Ln2);
                    return (Polynomial(r).AsUInt64() + (n << 52)).AsDouble();
                }

                return Exp2Wide(x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<double> Exp10(Vector256<double> x)
            {
                if (Vector256.LessThanOrEqualAll(Vector256.Abs(x).AsUInt64(), Vector256.Create(Exp10NormalMax)))
                {
                    Vector256<double> dn = Vector256.MultiplyAddEstimate(x, Vector256.Create(Log2Of10), Vector256.Create(Huge));
                    Vector256<ulong> n = dn.AsUInt64();
                    dn -= Vector256.Create(Huge);
                    Vector256<double> r = ReduceBase10(x, dn);
                    return (Polynomial(r).AsUInt64() + (n << 52)).AsDouble();
                }

                return Exp10Wide(x);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static Vector256<double> Exp2Wide(Vector256<double> x)
            {
                Vector256<double> clamped = Vector256.Min(Vector256.Max(x, Vector256.Create(Exp2ClampMin)), Vector256.Create(Exp2ClampMax));
                Vector256<double> dn = clamped + Vector256.Create(Huge);
                Vector256<double> r = (clamped - (dn - Vector256.Create(Huge))) * Vector256.Create(Ln2);
                return Vector256.ConditionalSelect(Vector256.Equals(x, x), ScaleWide(Polynomial(r), dn), x);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static Vector256<double> Exp10Wide(Vector256<double> x)
            {
                Vector256<double> clamped = Vector256.Min(Vector256.Max(x, Vector256.Create(Exp10ClampMin)), Vector256.Create(Exp10ClampMax));
                Vector256<double> dn = Vector256.MultiplyAddEstimate(clamped, Vector256.Create(Log2Of10), Vector256.Create(Huge));
                Vector256<double> r = ReduceBase10(clamped, dn - Vector256.Create(Huge));
                return Vector256.ConditionalSelect(Vector256.Equals(x, x), ScaleWide(Polynomial(r), dn), x);
            }

            // (x - n * log10(2)) * ln(10). Because n * Log10Of2Hi is exact, the subtraction from x rounds only once,
            // so the reduction is accurate whether or not MultiplyAddEstimate fuses.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<double> ReduceBase10(Vector256<double> x, Vector256<double> n)
            {
                Vector256<double> s = Vector256.MultiplyAddEstimate(n, Vector256.Create(-Log10Of2Hi), x);
                s = Vector256.MultiplyAddEstimate(n, Vector256.Create(-Log10Of2Lo), s);
                return s * Vector256.Create(Ln10);
            }

            // e^r for |r| <= ln(2) / 2, as in VectorMath.ExpDouble.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<double> Polynomial(Vector256<double> r)
            {
                Vector256<double> r2 = r * r;
                Vector256<double> r4 = r2 * r2;
                Vector256<double> r8 = r4 * r4;

                Vector256<double> a = Vector256.MultiplyAddEstimate(Vector256.Create(C12), r, Vector256.Create(C11));
                Vector256<double> b = Vector256.MultiplyAddEstimate(Vector256.Create(C10), r, Vector256.Create(C09));
                Vector256<double> c = Vector256.MultiplyAddEstimate(Vector256.Create(C08), r, Vector256.Create(C07));
                Vector256<double> d = Vector256.MultiplyAddEstimate(Vector256.Create(C06), r, Vector256.Create(C05));
                Vector256<double> e = Vector256.MultiplyAddEstimate(Vector256.Create(C04), r, Vector256.Create(C03));
                Vector256<double> f = r + Vector256<double>.One;

                Vector256<double> high = Vector256.MultiplyAddEstimate(a, r2, b);
                Vector256<double> middle = Vector256.MultiplyAddEstimate(Vector256.MultiplyAddEstimate(c, r2, d), r4, Vector256.MultiplyAddEstimate(e, r2, f));
                return Vector256.MultiplyAddEstimate(high, r8, middle);
            }

            // p * 2^n, where n is the integer in the low bits of nPlusHuge (n + Huge).
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<double> ScaleWide(Vector256<double> p, Vector256<double> nPlusHuge)
            {
                Vector256<long> n = nPlusHuge.AsInt64() - Vector256.Create(Huge).AsInt64();
                Vector256<long> n1 = n >> 1;
                Vector256<long> n2 = n - n1;
                Vector256<long> bias = Vector256.Create(1023L);
                return p * ((n1 + bias) << 52).AsDouble() * ((n2 + bias) << 52).AsDouble();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<double> Exp2(Vector512<double> x)
            {
                if (Vector512.LessThanOrEqualAll(Vector512.Abs(x).AsUInt64(), Vector512.Create(Exp2NormalMax)))
                {
                    Vector512<double> dn = x + Vector512.Create(Huge);
                    Vector512<ulong> n = dn.AsUInt64();
                    dn -= Vector512.Create(Huge);
                    Vector512<double> r = (x - dn) * Vector512.Create(Ln2);
                    return (Polynomial(r).AsUInt64() + (n << 52)).AsDouble();
                }

                return Exp2Wide(x);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<double> Exp10(Vector512<double> x)
            {
                if (Vector512.LessThanOrEqualAll(Vector512.Abs(x).AsUInt64(), Vector512.Create(Exp10NormalMax)))
                {
                    Vector512<double> dn = Vector512.MultiplyAddEstimate(x, Vector512.Create(Log2Of10), Vector512.Create(Huge));
                    Vector512<ulong> n = dn.AsUInt64();
                    dn -= Vector512.Create(Huge);
                    Vector512<double> r = ReduceBase10(x, dn);
                    return (Polynomial(r).AsUInt64() + (n << 52)).AsDouble();
                }

                return Exp10Wide(x);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static Vector512<double> Exp2Wide(Vector512<double> x)
            {
                Vector512<double> clamped = Vector512.Min(Vector512.Max(x, Vector512.Create(Exp2ClampMin)), Vector512.Create(Exp2ClampMax));
                Vector512<double> dn = clamped + Vector512.Create(Huge);
                Vector512<double> r = (clamped - (dn - Vector512.Create(Huge))) * Vector512.Create(Ln2);
                return Vector512.ConditionalSelect(Vector512.Equals(x, x), ScaleWide(Polynomial(r), dn), x);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static Vector512<double> Exp10Wide(Vector512<double> x)
            {
                Vector512<double> clamped = Vector512.Min(Vector512.Max(x, Vector512.Create(Exp10ClampMin)), Vector512.Create(Exp10ClampMax));
                Vector512<double> dn = Vector512.MultiplyAddEstimate(clamped, Vector512.Create(Log2Of10), Vector512.Create(Huge));
                Vector512<double> r = ReduceBase10(clamped, dn - Vector512.Create(Huge));
                return Vector512.ConditionalSelect(Vector512.Equals(x, x), ScaleWide(Polynomial(r), dn), x);
            }

            // (x - n * log10(2)) * ln(10). Because n * Log10Of2Hi is exact, the subtraction from x rounds only once,
            // so the reduction is accurate whether or not MultiplyAddEstimate fuses.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<double> ReduceBase10(Vector512<double> x, Vector512<double> n)
            {
                Vector512<double> s = Vector512.MultiplyAddEstimate(n, Vector512.Create(-Log10Of2Hi), x);
                s = Vector512.MultiplyAddEstimate(n, Vector512.Create(-Log10Of2Lo), s);
                return s * Vector512.Create(Ln10);
            }

            // e^r for |r| <= ln(2) / 2, as in VectorMath.ExpDouble.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<double> Polynomial(Vector512<double> r)
            {
                Vector512<double> r2 = r * r;
                Vector512<double> r4 = r2 * r2;
                Vector512<double> r8 = r4 * r4;

                Vector512<double> a = Vector512.MultiplyAddEstimate(Vector512.Create(C12), r, Vector512.Create(C11));
                Vector512<double> b = Vector512.MultiplyAddEstimate(Vector512.Create(C10), r, Vector512.Create(C09));
                Vector512<double> c = Vector512.MultiplyAddEstimate(Vector512.Create(C08), r, Vector512.Create(C07));
                Vector512<double> d = Vector512.MultiplyAddEstimate(Vector512.Create(C06), r, Vector512.Create(C05));
                Vector512<double> e = Vector512.MultiplyAddEstimate(Vector512.Create(C04), r, Vector512.Create(C03));
                Vector512<double> f = r + Vector512<double>.One;

                Vector512<double> high = Vector512.MultiplyAddEstimate(a, r2, b);
                Vector512<double> middle = Vector512.MultiplyAddEstimate(Vector512.MultiplyAddEstimate(c, r2, d), r4, Vector512.MultiplyAddEstimate(e, r2, f));
                return Vector512.MultiplyAddEstimate(high, r8, middle);
            }

            // p * 2^n, where n is the integer in the low bits of nPlusHuge (n + Huge).
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<double> ScaleWide(Vector512<double> p, Vector512<double> nPlusHuge)
            {
                Vector512<long> n = nPlusHuge.AsInt64() - Vector512.Create(Huge).AsInt64();
                Vector512<long> n1 = n >> 1;
                Vector512<long> n2 = n - n1;
                Vector512<long> bias = Vector512.Create(1023L);
                return p * ((n1 + bias) << 52).AsDouble() * ((n2 + bias) << 52).AsDouble();
            }
        }
    }
}
