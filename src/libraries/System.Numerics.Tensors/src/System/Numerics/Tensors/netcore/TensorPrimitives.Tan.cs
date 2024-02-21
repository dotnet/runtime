// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise tangent of the value in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Tan(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Tan<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, TanOperator<T>>(x, destination);

        /// <summary>T.Tan(x)</summary>
        private readonly struct TanOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            // This code is based on `vrs4_tan` and `vrd2_tan` from amd/aocl-libm-ose
            // Copyright (C) 2019-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation notes from amd/aocl-libm-ose:
            // --------------------------------------------
            // A given x is reduced into the form:
            //          |x| = (N * π/2) + F
            // Where N is an integer obtained using:
            //         N = round(x * 2/π)
            // And F is a fraction part lying in the interval
            //         [-π/4, +π/4];
            // obtained as F = |x| - (N * π/2)
            // Thus tan(x) is given by
            //         tan(x) = tan((N * π/2) + F) = tan(F)
            //         when N is even, = -cot(F) = -1/tan(F)
            //         when N is odd, tan(F) is approximated using a polynomial
            //         obtained from Remez approximation from Sollya.

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Tan(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return TanOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return TanOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return TanOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return TanOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return TanOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return TanOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
            }
        }

        /// <summary>float.Tan(x)</summary>
        private readonly struct TanOperatorSingle : IUnaryOperator<float, float>
        {
            internal const uint SignMask = 0x7FFFFFFFu;
            internal const uint MaxVectorizedValue = 0x49800000u;
            private const float AlmHuge = 1.2582912e7f;
            private const float Pi_Tail2 = 4.371139e-8f;
            private const float Pi_Tail3 = 1.7151245e-15f;
            private const float C1 = 0.33333358f;
            private const float C2 = 0.13332522f;
            private const float C3 = 0.05407107f;
            private const float C4 = 0.021237267f;
            private const float C5 = 0.010932301f;
            private const float C6 = -1.5722344e-5f;
            private const float C7 = 0.0044221194f;

            public static bool Vectorizable => true;

            public static float Invoke(float x) => float.Tan(x);

            public static Vector128<float> Invoke(Vector128<float> x)
            {
                Vector128<float> uxMasked = Vector128.Abs(x);
                if (Vector128.GreaterThanAny(uxMasked.AsUInt32(), Vector128.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<TanOperatorSingle>(x);
                }

                Vector128<float> dn = MultiplyAddEstimateOperator<float>.Invoke(uxMasked, Vector128.Create(2 / float.Pi), Vector128.Create(AlmHuge));
                Vector128<uint> odd = dn.AsUInt32() << 31;
                dn -= Vector128.Create(AlmHuge);

                Vector128<float> f = uxMasked;
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector128.Create(-float.Pi / 2), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector128.Create(Pi_Tail2), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector128.Create(Pi_Tail3), f);

                // POLY_EVAL_ODD_15
                Vector128<float> f2 = f * f;
                Vector128<float> f4 = f2 * f2;
                Vector128<float> f8 = f4 * f4;
                Vector128<float> f12 = f8 * f4;
                Vector128<float> a1 = MultiplyAddEstimateOperator<float>.Invoke(Vector128.Create(C2), f2, Vector128.Create(C1));
                Vector128<float> a2 = MultiplyAddEstimateOperator<float>.Invoke(Vector128.Create(C4), f2, Vector128.Create(C3));
                Vector128<float> a3 = MultiplyAddEstimateOperator<float>.Invoke(Vector128.Create(C6), f2, Vector128.Create(C5));
                Vector128<float> b1 = MultiplyAddEstimateOperator<float>.Invoke(a2, f4, a1);
                Vector128<float> b2 = MultiplyAddEstimateOperator<float>.Invoke(f8, a3, f12 * Vector128.Create(C7));
                Vector128<float> poly = MultiplyAddEstimateOperator<float>.Invoke(f * f2, b1 + b2, f);

                Vector128<float> result = (poly.AsUInt32() ^ (x.AsUInt32() & Vector128.Create(~SignMask))).AsSingle();
                return Vector128.ConditionalSelect(Vector128.Equals(odd, Vector128<uint>.Zero).AsSingle(),
                    result,
                    Vector128.Create(-1f) / result);
            }

            public static Vector256<float> Invoke(Vector256<float> x)
            {
                Vector256<float> uxMasked = Vector256.Abs(x);
                if (Vector256.GreaterThanAny(uxMasked.AsUInt32(), Vector256.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<TanOperatorSingle>(x);
                }

                Vector256<float> dn = MultiplyAddEstimateOperator<float>.Invoke(uxMasked, Vector256.Create(2 / float.Pi), Vector256.Create(AlmHuge));
                Vector256<uint> odd = dn.AsUInt32() << 31;
                dn -= Vector256.Create(AlmHuge);

                Vector256<float> f = uxMasked;
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector256.Create(-float.Pi / 2), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector256.Create(Pi_Tail2), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector256.Create(Pi_Tail3), f);

                // POLY_EVAL_ODD_15
                Vector256<float> f2 = f * f;
                Vector256<float> f4 = f2 * f2;
                Vector256<float> f8 = f4 * f4;
                Vector256<float> f12 = f8 * f4;
                Vector256<float> a1 = MultiplyAddEstimateOperator<float>.Invoke(Vector256.Create(C2), f2, Vector256.Create(C1));
                Vector256<float> a2 = MultiplyAddEstimateOperator<float>.Invoke(Vector256.Create(C4), f2, Vector256.Create(C3));
                Vector256<float> a3 = MultiplyAddEstimateOperator<float>.Invoke(Vector256.Create(C6), f2, Vector256.Create(C5));
                Vector256<float> b1 = MultiplyAddEstimateOperator<float>.Invoke(a2, f4, a1);
                Vector256<float> b2 = MultiplyAddEstimateOperator<float>.Invoke(f8, a3, f12 * Vector256.Create(C7));
                Vector256<float> poly = MultiplyAddEstimateOperator<float>.Invoke(f * f2, b1 + b2, f);

                Vector256<float> result = (poly.AsUInt32() ^ (x.AsUInt32() & Vector256.Create(~SignMask))).AsSingle();
                return Vector256.ConditionalSelect(Vector256.Equals(odd, Vector256<uint>.Zero).AsSingle(),
                    result,
                    Vector256.Create(-1f) / result);
            }

            public static Vector512<float> Invoke(Vector512<float> x)
            {
                Vector512<float> uxMasked = Vector512.Abs(x);
                if (Vector512.GreaterThanAny(uxMasked.AsUInt32(), Vector512.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<TanOperatorSingle>(x);
                }

                Vector512<float> dn = MultiplyAddEstimateOperator<float>.Invoke(uxMasked, Vector512.Create(2 / float.Pi), Vector512.Create(AlmHuge));
                Vector512<uint> odd = dn.AsUInt32() << 31;
                dn -= Vector512.Create(AlmHuge);

                Vector512<float> f = uxMasked;
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector512.Create(-float.Pi / 2), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector512.Create(Pi_Tail2), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector512.Create(Pi_Tail3), f);

                // POLY_EVAL_ODD_15
                Vector512<float> f2 = f * f;
                Vector512<float> f4 = f2 * f2;
                Vector512<float> f8 = f4 * f4;
                Vector512<float> f12 = f8 * f4;
                Vector512<float> a1 = MultiplyAddEstimateOperator<float>.Invoke(Vector512.Create(C2), f2, Vector512.Create(C1));
                Vector512<float> a2 = MultiplyAddEstimateOperator<float>.Invoke(Vector512.Create(C4), f2, Vector512.Create(C3));
                Vector512<float> a3 = MultiplyAddEstimateOperator<float>.Invoke(Vector512.Create(C6), f2, Vector512.Create(C5));
                Vector512<float> b1 = MultiplyAddEstimateOperator<float>.Invoke(a2, f4, a1);
                Vector512<float> b2 = MultiplyAddEstimateOperator<float>.Invoke(f8, a3, f12 * Vector512.Create(C7));
                Vector512<float> poly = MultiplyAddEstimateOperator<float>.Invoke(f * f2, b1 + b2, f);

                Vector512<float> result = (poly.AsUInt32() ^ (x.AsUInt32() & Vector512.Create(~SignMask))).AsSingle();
                return Vector512.ConditionalSelect(Vector512.Equals(odd, Vector512<uint>.Zero).AsSingle(),
                    result,
                    Vector512.Create(-1f) / result);
            }
        }

        /// <summary>double.Tan(x)</summary>
        private readonly struct TanOperatorDouble : IUnaryOperator<double, double>
        {
            internal const ulong SignMask = 0x7FFFFFFFFFFFFFFFul;
            internal const ulong MaxVectorizedValue = 0x4160000000000000ul;
            private const double AlmHuge = 6.755399441055744e15;
            private const double HalfPi2 = 6.123233995736766E-17;
            private const double HalfPi3 = -1.4973849048591698E-33;
            private const double C1 = 0.33333333333332493;
            private const double C3 = 0.133333333334343;
            private const double C5 = 0.0539682539203796;
            private const double C7 = 0.02186948972198256;
            private const double C9 = 0.008863217894198291;
            private const double C11 = 0.003592298593761111;
            private const double C13 = 0.0014547086183165365;
            private const double C15 = 5.952456856028558E-4;
            private const double C17 = 2.2190741289936845E-4;
            private const double C19 = 1.3739809957985104E-4;
            private const double C21 = -2.7500197359895707E-5;
            private const double C23 = 9.038741690184683E-5;
            private const double C25 = -4.534076545538694E-5;
            private const double C27 = 2.0966522562190197E-5;

            public static bool Vectorizable => true;

            public static double Invoke(double x) => double.Tan(x);

            public static Vector128<double> Invoke(Vector128<double> x)
            {
                Vector128<double> uxMasked = Vector128.Abs(x);
                if (Vector128.GreaterThanAny(uxMasked.AsUInt64(), Vector128.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<TanOperatorDouble>(x);
                }

                Vector128<double> dn = MultiplyAddEstimateOperator<double>.Invoke(uxMasked, Vector128.Create(2 / double.Pi), Vector128.Create(AlmHuge));
                Vector128<ulong> odd = dn.AsUInt64() << 63;
                dn -= Vector128.Create(AlmHuge);
                Vector128<double> f = uxMasked.AsDouble() - (dn * (double.Pi / 2)) - (dn * HalfPi2) - (dn * HalfPi3);

                // POLY_EVAL_ODD_29
                Vector128<double> g = f * f;
                Vector128<double> g2 = g * g;
                Vector128<double> g3 = g * g2;
                Vector128<double> g5 = g3 * g2;
                Vector128<double> g7 = g5 * g2;
                Vector128<double> g9 = g7 * g2;
                Vector128<double> g11 = g9 * g2;
                Vector128<double> g13 = g11 * g2;
                Vector128<double> a1 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C3), g, Vector128.Create(C1));
                Vector128<double> a2 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C7), g, Vector128.Create(C5));
                Vector128<double> a3 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C11), g, Vector128.Create(C9));
                Vector128<double> a4 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C15), g, Vector128.Create(C13));
                Vector128<double> a5 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C19), g, Vector128.Create(C17));
                Vector128<double> a6 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C23), g, Vector128.Create(C21));
                Vector128<double> a7 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C27), g, Vector128.Create(C25));
                Vector128<double> b1 = MultiplyAddEstimateOperator<double>.Invoke(g, a1, g3 * a2);
                Vector128<double> b2 = MultiplyAddEstimateOperator<double>.Invoke(g5, a3, g7 * a4);
                Vector128<double> b3 = MultiplyAddEstimateOperator<double>.Invoke(g9, a5, g11 * a6);
                Vector128<double> q = MultiplyAddEstimateOperator<double>.Invoke(g13, a7, b1 + b2 + b3);
                Vector128<double> poly = MultiplyAddEstimateOperator<double>.Invoke(f, q, f);

                Vector128<double> result = (poly.AsUInt64() ^ (x.AsUInt64() & Vector128.Create(~SignMask))).AsDouble();
                return Vector128.ConditionalSelect(Vector128.Equals(odd, Vector128<ulong>.Zero).AsDouble(),
                    result,
                    Vector128.Create(-1.0) / result);
            }

            public static Vector256<double> Invoke(Vector256<double> x)
            {
                Vector256<double> uxMasked = Vector256.Abs(x);
                if (Vector256.GreaterThanAny(uxMasked.AsUInt64(), Vector256.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<TanOperatorDouble>(x);
                }

                Vector256<double> dn = MultiplyAddEstimateOperator<double>.Invoke(uxMasked, Vector256.Create(2 / double.Pi), Vector256.Create(AlmHuge));
                Vector256<ulong> odd = dn.AsUInt64() << 63;
                dn -= Vector256.Create(AlmHuge);
                Vector256<double> f = uxMasked.AsDouble() - (dn * (double.Pi / 2)) - (dn * HalfPi2) - (dn * HalfPi3);

                // POLY_EVAL_ODD_29
                Vector256<double> g = f * f;
                Vector256<double> g2 = g * g;
                Vector256<double> g3 = g * g2;
                Vector256<double> g5 = g3 * g2;
                Vector256<double> g7 = g5 * g2;
                Vector256<double> g9 = g7 * g2;
                Vector256<double> g11 = g9 * g2;
                Vector256<double> g13 = g11 * g2;
                Vector256<double> a1 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C3), g, Vector256.Create(C1));
                Vector256<double> a2 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C7), g, Vector256.Create(C5));
                Vector256<double> a3 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C11), g, Vector256.Create(C9));
                Vector256<double> a4 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C15), g, Vector256.Create(C13));
                Vector256<double> a5 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C19), g, Vector256.Create(C17));
                Vector256<double> a6 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C23), g, Vector256.Create(C21));
                Vector256<double> a7 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C27), g, Vector256.Create(C25));
                Vector256<double> b1 = MultiplyAddEstimateOperator<double>.Invoke(g, a1, g3 * a2);
                Vector256<double> b2 = MultiplyAddEstimateOperator<double>.Invoke(g5, a3, g7 * a4);
                Vector256<double> b3 = MultiplyAddEstimateOperator<double>.Invoke(g9, a5, g11 * a6);
                Vector256<double> q = MultiplyAddEstimateOperator<double>.Invoke(g13, a7, b1 + b2 + b3);
                Vector256<double> poly = MultiplyAddEstimateOperator<double>.Invoke(f, q, f);

                Vector256<double> result = (poly.AsUInt64() ^ (x.AsUInt64() & Vector256.Create(~SignMask))).AsDouble();
                return Vector256.ConditionalSelect(Vector256.Equals(odd, Vector256<ulong>.Zero).AsDouble(),
                    result,
                    Vector256.Create(-1.0) / result);
            }

            public static Vector512<double> Invoke(Vector512<double> x)
            {
                Vector512<double> uxMasked = Vector512.Abs(x);
                if (Vector512.GreaterThanAny(uxMasked.AsUInt64(), Vector512.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<TanOperatorDouble>(x);
                }

                Vector512<double> dn = MultiplyAddEstimateOperator<double>.Invoke(uxMasked, Vector512.Create(2 / double.Pi), Vector512.Create(AlmHuge));
                Vector512<ulong> odd = dn.AsUInt64() << 63;
                dn -= Vector512.Create(AlmHuge);
                Vector512<double> f = uxMasked.AsDouble() - (dn * (double.Pi / 2)) - (dn * HalfPi2) - (dn * HalfPi3);

                // POLY_EVAL_ODD_29
                Vector512<double> g = f * f;
                Vector512<double> g2 = g * g;
                Vector512<double> g3 = g * g2;
                Vector512<double> g5 = g3 * g2;
                Vector512<double> g7 = g5 * g2;
                Vector512<double> g9 = g7 * g2;
                Vector512<double> g11 = g9 * g2;
                Vector512<double> g13 = g11 * g2;
                Vector512<double> a1 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C3), g, Vector512.Create(C1));
                Vector512<double> a2 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C7), g, Vector512.Create(C5));
                Vector512<double> a3 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C11), g, Vector512.Create(C9));
                Vector512<double> a4 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C15), g, Vector512.Create(C13));
                Vector512<double> a5 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C19), g, Vector512.Create(C17));
                Vector512<double> a6 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C23), g, Vector512.Create(C21));
                Vector512<double> a7 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C27), g, Vector512.Create(C25));
                Vector512<double> b1 = MultiplyAddEstimateOperator<double>.Invoke(g, a1, g3 * a2);
                Vector512<double> b2 = MultiplyAddEstimateOperator<double>.Invoke(g5, a3, g7 * a4);
                Vector512<double> b3 = MultiplyAddEstimateOperator<double>.Invoke(g9, a5, g11 * a6);
                Vector512<double> q = MultiplyAddEstimateOperator<double>.Invoke(g13, a7, b1 + b2 + b3);
                Vector512<double> poly = MultiplyAddEstimateOperator<double>.Invoke(f, q, f);

                Vector512<double> result = (poly.AsUInt64() ^ (x.AsUInt64() & Vector512.Create(~SignMask))).AsDouble();
                return Vector512.ConditionalSelect(Vector512.Equals(odd, Vector512<ulong>.Zero).AsDouble(),
                    result,
                    Vector512.Create(-1.0) / result);
            }
        }
    }
}
