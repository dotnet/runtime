// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise cosine of the value in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Cos(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Cos<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, CosOperator<T>>(x, destination);

        /// <summary>T.Cos(x)</summary>
        private readonly struct CosOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            // This code is based on `vrs4_cos` and `vrd2_cos` from amd/aocl-libm-ose
            // Copyright (C) 2019-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation notes from amd/aocl-libm-ose:
            // --------------------------------------------
            // To compute cosf(float x)
            // Using the identity,
            // cos(x) = sin(x + pi/2)           (1)
            //
            // 1. Argument Reduction
            //      Now, let x be represented as,
            //          |x| = N * pi + f        (2) | N is an integer,
            //                                        -pi/2 <= f <= pi/2
            //
            //      From (2), N = int( (x + pi/2) / pi) - 0.5
            //                f = |x| - (N * pi)
            //
            // 2. Polynomial Evaluation
            //       From (1) and (2),sin(f) can be calculated using a polynomial
            //       sin(f) = f*(1 + C1*f^2 + C2*f^4 + C3*f^6 + c4*f^8)
            //
            // 3. Reconstruction
            //      Hence, cos(x) = sin(x + pi/2) = (-1)^N * sin(f)

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Cos(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return CosOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return CosOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return CosOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return CosOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return CosOperatorSingle.Invoke(x.AsSingle()).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return CosOperatorDouble.Invoke(x.AsDouble()).As<double, T>();
                }
            }
        }

        /// <summary>float.Cos(x)</summary>
        private readonly struct CosOperatorSingle : IUnaryOperator<float, float>
        {
            internal const uint MaxVectorizedValue = 0x4A989680u;
            internal const uint SignMask = 0x7FFFFFFFu;
            private const float AlmHuge = 1.2582912e7f;
            private const float Pi_Tail1 = 8.742278e-8f;
            private const float Pi_Tail2 = 3.430249e-15f;
            private const float C1 = -0.16666657f;
            private const float C2 = 0.008332962f;
            private const float C3 = -1.9801206e-4f;
            private const float C4 = 2.5867037e-6f;

            public static bool Vectorizable => true;

            public static float Invoke(float x) => float.Cos(x);

            public static Vector128<float> Invoke(Vector128<float> x)
            {
                Vector128<float> uxMasked = Vector128.Abs(x);
                if (Vector128.GreaterThanAny(uxMasked.AsUInt32(), Vector128.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<CosOperatorSingle>(x);
                }

                Vector128<float> almHuge = Vector128.Create(AlmHuge);
                Vector128<float> dn = MultiplyAddEstimateOperator<float>.Invoke(uxMasked + Vector128.Create(float.Pi / 2), Vector128.Create(1 / float.Pi), almHuge);
                Vector128<uint> odd = dn.AsUInt32() << 31;
                dn = dn - almHuge - Vector128.Create(0.5f);

                Vector128<float> f = uxMasked;
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector128.Create(-float.Pi), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector128.Create(Pi_Tail1), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector128.Create(Pi_Tail2), f);

                // POLY_EVAL_ODD_9
                Vector128<float> f2 = f * f;
                Vector128<float> f4 = f2 * f2;
                Vector128<float> a0 = MultiplyAddEstimateOperator<float>.Invoke(Vector128.Create(C2), f2, Vector128.Create(C1));
                Vector128<float> a1 = MultiplyAddEstimateOperator<float>.Invoke(a0, f2, Vector128<float>.One);
                Vector128<float> a2 = MultiplyAddEstimateOperator<float>.Invoke(Vector128.Create(C3), f2, Vector128.Create(C4) * f4);
                Vector128<float> a3 = MultiplyAddEstimateOperator<float>.Invoke(a2, f4, a1);
                Vector128<float> poly = f * a3;

                return (poly.AsUInt32() ^ odd).AsSingle();
            }

            public static Vector256<float> Invoke(Vector256<float> x)
            {
                Vector256<float> uxMasked = Vector256.Abs(x);
                if (Vector256.GreaterThanAny(uxMasked.AsUInt32(), Vector256.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<CosOperatorSingle>(x);
                }

                Vector256<float> almHuge = Vector256.Create(AlmHuge);
                Vector256<float> dn = MultiplyAddEstimateOperator<float>.Invoke(uxMasked + Vector256.Create(float.Pi / 2), Vector256.Create(1 / float.Pi), almHuge);
                Vector256<uint> odd = dn.AsUInt32() << 31;
                dn = dn - almHuge - Vector256.Create(0.5f);

                Vector256<float> f = uxMasked;
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector256.Create(-float.Pi), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector256.Create(Pi_Tail1), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector256.Create(Pi_Tail2), f);

                // POLY_EVAL_ODD_9
                Vector256<float> f2 = f * f;
                Vector256<float> f4 = f2 * f2;
                Vector256<float> a0 = MultiplyAddEstimateOperator<float>.Invoke(Vector256.Create(C2), f2, Vector256.Create(C1));
                Vector256<float> a1 = MultiplyAddEstimateOperator<float>.Invoke(a0, f2, Vector256<float>.One);
                Vector256<float> a2 = MultiplyAddEstimateOperator<float>.Invoke(Vector256.Create(C3), f2, Vector256.Create(C4) * f4);
                Vector256<float> a3 = MultiplyAddEstimateOperator<float>.Invoke(a2, f4, a1);
                Vector256<float> poly = f * a3;

                return (poly.AsUInt32() ^ odd).AsSingle();
            }

            public static Vector512<float> Invoke(Vector512<float> x)
            {
                Vector512<float> uxMasked = Vector512.Abs(x);
                if (Vector512.GreaterThanAny(uxMasked.AsUInt32(), Vector512.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<CosOperatorSingle>(x);
                }

                Vector512<float> almHuge = Vector512.Create(AlmHuge);
                Vector512<float> dn = MultiplyAddEstimateOperator<float>.Invoke(uxMasked + Vector512.Create(float.Pi / 2), Vector512.Create(1 / float.Pi), almHuge);
                Vector512<uint> odd = dn.AsUInt32() << 31;
                dn = dn - almHuge - Vector512.Create(0.5f);

                Vector512<float> f = uxMasked;
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector512.Create(-float.Pi), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector512.Create(Pi_Tail1), f);
                f = MultiplyAddEstimateOperator<float>.Invoke(dn, Vector512.Create(Pi_Tail2), f);

                // POLY_EVAL_ODD_9
                Vector512<float> f2 = f * f;
                Vector512<float> f4 = f2 * f2;
                Vector512<float> a0 = MultiplyAddEstimateOperator<float>.Invoke(Vector512.Create(C2), f2, Vector512.Create(C1));
                Vector512<float> a1 = MultiplyAddEstimateOperator<float>.Invoke(a0, f2, Vector512<float>.One);
                Vector512<float> a2 = MultiplyAddEstimateOperator<float>.Invoke(Vector512.Create(C3), f2, Vector512.Create(C4) * f4);
                Vector512<float> a3 = MultiplyAddEstimateOperator<float>.Invoke(a2, f4, a1);
                Vector512<float> poly = f * a3;

                return (poly.AsUInt32() ^ odd).AsSingle();
            }
        }

        /// <summary>double.Cos(x)</summary>
        private readonly struct CosOperatorDouble : IUnaryOperator<double, double>
        {
            internal const ulong SignMask = 0x7FFFFFFFFFFFFFFFul;
            internal const ulong MaxVectorizedValue = 0x4160000000000000ul;
            private const double AlmHuge = 6.755399441055744E15;
            private const double Pi_Tail2 = -1.2246467991473532E-16;
            private const double Pi_Tail3 = 2.9947698097183397E-33;
            private const double C1 = -0.16666666666666666;
            private const double C2 = 0.008333333333333165;
            private const double C3 = -1.984126984120184E-4;
            private const double C4 = 2.7557319210152756E-6;
            private const double C5 = -2.5052106798274616E-8;
            private const double C6 = 1.6058936490373254E-10;
            private const double C7 = -7.642917806937501E-13;
            private const double C8 = 2.7204790963151784E-15;

            public static bool Vectorizable => true;

            public static double Invoke(double x) => double.Cos(x);

            public static Vector128<double> Invoke(Vector128<double> x)
            {
                Vector128<double> uxMasked = Vector128.Abs(x);
                if (Vector128.GreaterThanAny(uxMasked.AsUInt64(), Vector128.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<CosOperatorDouble>(x);
                }

                Vector128<double> almHuge = Vector128.Create(AlmHuge);
                Vector128<double> dn = (uxMasked * Vector128.Create(1 / double.Pi)) + Vector128.Create(double.Pi / 2) + almHuge;
                Vector128<ulong> odd = dn.AsUInt64() << 63;
                dn = dn - almHuge - Vector128.Create(0.5);

                Vector128<double> f = uxMasked;
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector128.Create(-double.Pi), f);
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector128.Create(Pi_Tail2), f);
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector128.Create(Pi_Tail3), f);

                // POLY_EVAL_ODD_17
                Vector128<double> f2 = f * f;
                Vector128<double> f4 = f2 * f2;
                Vector128<double> f6 = f4 * f2;
                Vector128<double> f10 = f6 * f4;
                Vector128<double> f14 = f10 * f4;
                Vector128<double> a1 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C2), f2, Vector128.Create(C1));
                Vector128<double> a2 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C4), f2, Vector128.Create(C3));
                Vector128<double> a3 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C6), f2, Vector128.Create(C5));
                Vector128<double> a4 = MultiplyAddEstimateOperator<double>.Invoke(Vector128.Create(C8), f2, Vector128.Create(C7));
                Vector128<double> b1 = MultiplyAddEstimateOperator<double>.Invoke(a1, f2, a2 * f6);
                Vector128<double> b2 = MultiplyAddEstimateOperator<double>.Invoke(f10, a3, f14 * a4);
                Vector128<double> poly = MultiplyAddEstimateOperator<double>.Invoke(f, b1 + b2, f);

                return (poly.AsUInt64() ^ odd).AsDouble();
            }

            public static Vector256<double> Invoke(Vector256<double> x)
            {
                Vector256<double> uxMasked = Vector256.Abs(x);
                if (Vector256.GreaterThanAny(uxMasked.AsUInt64(), Vector256.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<CosOperatorDouble>(x);
                }

                Vector256<double> almHuge = Vector256.Create(AlmHuge);
                Vector256<double> dn = (uxMasked * Vector256.Create(1 / double.Pi)) + Vector256.Create(double.Pi / 2) + almHuge;
                Vector256<ulong> odd = dn.AsUInt64() << 63;
                dn = dn - almHuge - Vector256.Create(0.5);

                Vector256<double> f = uxMasked;
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector256.Create(-double.Pi), f);
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector256.Create(Pi_Tail2), f);
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector256.Create(Pi_Tail3), f);

                // POLY_EVAL_ODD_17
                Vector256<double> f2 = f * f;
                Vector256<double> f4 = f2 * f2;
                Vector256<double> f6 = f4 * f2;
                Vector256<double> f10 = f6 * f4;
                Vector256<double> f14 = f10 * f4;
                Vector256<double> a1 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C2), f2, Vector256.Create(C1));
                Vector256<double> a2 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C4), f2, Vector256.Create(C3));
                Vector256<double> a3 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C6), f2, Vector256.Create(C5));
                Vector256<double> a4 = MultiplyAddEstimateOperator<double>.Invoke(Vector256.Create(C8), f2, Vector256.Create(C7));
                Vector256<double> b1 = MultiplyAddEstimateOperator<double>.Invoke(a1, f2, a2 * f6);
                Vector256<double> b2 = MultiplyAddEstimateOperator<double>.Invoke(f10, a3, f14 * a4);
                Vector256<double> poly = MultiplyAddEstimateOperator<double>.Invoke(f, b1 + b2, f);

                return (poly.AsUInt64() ^ odd).AsDouble();
            }

            public static Vector512<double> Invoke(Vector512<double> x)
            {
                Vector512<double> uxMasked = Vector512.Abs(x);
                if (Vector512.GreaterThanAny(uxMasked.AsUInt64(), Vector512.Create(MaxVectorizedValue)))
                {
                    return ApplyScalar<CosOperatorDouble>(x);
                }

                Vector512<double> almHuge = Vector512.Create(AlmHuge);
                Vector512<double> dn = (uxMasked * Vector512.Create(1 / double.Pi)) + Vector512.Create(double.Pi / 2) + almHuge;
                Vector512<ulong> odd = dn.AsUInt64() << 63;
                dn = dn - almHuge - Vector512.Create(0.5);

                Vector512<double> f = uxMasked;
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector512.Create(-double.Pi), f);
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector512.Create(Pi_Tail2), f);
                f = MultiplyAddEstimateOperator<double>.Invoke(dn, Vector512.Create(Pi_Tail3), f);

                // POLY_EVAL_ODD_17
                Vector512<double> f2 = f * f;
                Vector512<double> f4 = f2 * f2;
                Vector512<double> f6 = f4 * f2;
                Vector512<double> f10 = f6 * f4;
                Vector512<double> f14 = f10 * f4;
                Vector512<double> a1 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C2), f2, Vector512.Create(C1));
                Vector512<double> a2 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C4), f2, Vector512.Create(C3));
                Vector512<double> a3 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C6), f2, Vector512.Create(C5));
                Vector512<double> a4 = MultiplyAddEstimateOperator<double>.Invoke(Vector512.Create(C8), f2, Vector512.Create(C7));
                Vector512<double> b1 = MultiplyAddEstimateOperator<double>.Invoke(a1, f2, a2 * f6);
                Vector512<double> b2 = MultiplyAddEstimateOperator<double>.Invoke(f10, a3, f14 * a4);
                Vector512<double> poly = MultiplyAddEstimateOperator<double>.Invoke(f, b1 + b2, f);

                return (poly.AsUInt64() ^ odd).AsDouble();
            }
        }
    }
}
