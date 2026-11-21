// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise result of raising <c>e</c> to the number powers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Exp(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals <see cref="IFloatingPointIeee754{TSelf}.NaN"/> or <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value equals <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>, the result stored into the corresponding destination location is set to 0.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Exp<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IExponentialFunctions<T>
        {
            if (typeof(T) == typeof(Half) && TryUnaryInvokeHalfAsInt16<T, ExpOperator<float>>(x, destination))
            {
                return;
            }

            InvokeSpanIntoSpan<T, ExpOperator<T>>(x, destination);
        }

        /// <summary>T.Exp(x)</summary>
        internal readonly struct ExpOperator<T> : IUnaryOperator<T, T>
            where T : IExponentialFunctions<T>
        {
            // Pow2Core uses the rounding constant 1.5 * 2^52 and the degree-5 polynomial coefficients for 2^r from
            // VectorMath.ExpSingle (Vector128.Exp for float). That implementation is based on `vrs4_expf` from
            // amd/aocl-libm-ose.
            // Copyright (C) 2019-2022 Advanced Micro Devices, Inc. All rights reserved.
            // Licensed under the BSD 3-Clause "New" or "Revised" License. See THIRD-PARTY-NOTICES.TXT for the full license text.
            private const double Pow2Huge = 6755399441055744;
            private const double Pow2C1 = 1.0000000754895704;
            private const double Pow2C2 = 0.6931472254087585;
            private const double Pow2C3 = 0.2402210737432219;
            private const double Pow2C4 = 0.05550297297702539;
            private const double Pow2C5 = 0.009676036358193323;
            private const double Pow2C6 = 0.001341000536524434;

            public static bool Vectorizable => (typeof(T) == typeof(double))
                                            || (typeof(T) == typeof(float));

            public static T Invoke(T x) => T.Exp(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Exp(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Exp(x.AsSingle()).As<float, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Exp(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Exp(x.AsSingle()).As<float, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Exp(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Exp(x.AsSingle()).As<float, T>();
                }
            }

            // b^x, where lnBase is ln(b) and log2Base is log2(b).
            //
            // For double, this is T.Exp(x * lnBase).
            //
            // For float, the scaled argument must not be rounded to float: that rounding costs tens of ulps in the
            // result and, just below the overflow threshold, turns a finite result into infinity (#135718). So x is
            // widened, z = x * log2Base is formed in double, and Pow2Core evaluates 2^z using the same polynomial as
            // Vector128.Exp for float. The cost is close to that of Vector128.Exp on float. Inputs below
            // minArgument give 0 and inputs above maxArgument give +infinity. These masks also replace invalid
            // intermediate results for extreme inputs outside Pow2Core's exponent range. NaN inputs remain NaN.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<T> InvokeScaled(Vector128<T> x, double lnBase, double log2Base, float minArgument, float maxArgument)
            {
                if (typeof(T) != typeof(float))
                {
                    return Invoke(x * Vector128.Create(T.CreateTruncating(lnBase)));
                }

                Vector128<float> xf = x.AsSingle();
                (Vector128<double> lower, Vector128<double> upper) = Vector128.Widen(xf);
                Vector128<double> scale = Vector128.Create(log2Base);
                Vector128<float> result = Vector128.Narrow(Pow2Core(lower * scale), Pow2Core(upper * scale));

                result = Vector128.ConditionalSelect(Vector128.GreaterThan(xf, Vector128.Create(maxArgument)), Vector128.Create(float.PositiveInfinity), result);
                result = Vector128.AndNot(result, Vector128.LessThan(xf, Vector128.Create(minArgument)));
                return result.As<float, T>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<T> InvokeScaled(Vector256<T> x, double lnBase, double log2Base, float minArgument, float maxArgument)
            {
                if (typeof(T) != typeof(float))
                {
                    return Invoke(x * Vector256.Create(T.CreateTruncating(lnBase)));
                }

                Vector256<float> xf = x.AsSingle();
                (Vector256<double> lower, Vector256<double> upper) = Vector256.Widen(xf);
                Vector256<double> scale = Vector256.Create(log2Base);
                Vector256<float> result = Vector256.Narrow(Pow2Core(lower * scale), Pow2Core(upper * scale));

                result = Vector256.ConditionalSelect(Vector256.GreaterThan(xf, Vector256.Create(maxArgument)), Vector256.Create(float.PositiveInfinity), result);
                result = Vector256.AndNot(result, Vector256.LessThan(xf, Vector256.Create(minArgument)));
                return result.As<float, T>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<T> InvokeScaled(Vector512<T> x, double lnBase, double log2Base, float minArgument, float maxArgument)
            {
                if (typeof(T) != typeof(float))
                {
                    return Invoke(x * Vector512.Create(T.CreateTruncating(lnBase)));
                }

                Vector512<float> xf = x.AsSingle();
                (Vector512<double> lower, Vector512<double> upper) = Vector512.Widen(xf);
                Vector512<double> scale = Vector512.Create(log2Base);
                Vector512<float> result = Vector512.Narrow(Pow2Core(lower * scale), Pow2Core(upper * scale));

                result = Vector512.ConditionalSelect(Vector512.GreaterThan(xf, Vector512.Create(maxArgument)), Vector512.Create(float.PositiveInfinity), result);
                result = Vector512.AndNot(result, Vector512.LessThan(xf, Vector512.Create(minArgument)));
                return result.As<float, T>();
            }

            // 2^z in double for z within about [-1022, 1023], to the accuracy of a float result. This is the
            // computation VectorMath.ExpSingle performs after it has formed z = x * log2(e).
            //
            // Write z = n + r with n an integer and |r| <= 1/2. Adding 1.5 * 2^52 to z leaves n in the low mantissa bits
            // of the sum, so r = z - (sum - 1.5 * 2^52). 2^r is a degree-5 polynomial in r, and 2^n is applied by adding
            // n to the exponent field of the polynomial's bits.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<double> Pow2Core(Vector128<double> z)
            {
                Vector128<double> huge = Vector128.Create(Pow2Huge);
                Vector128<double> dn = z + huge;
                Vector128<ulong> n = dn.AsUInt64();
                Vector128<double> r = z - (dn - huge);
                Vector128<double> r2 = r * r;
                Vector128<double> r4 = r2 * r2;

                Vector128<double> poly = Vector128.MultiplyAddEstimate(
                    r4,
                    Vector128.MultiplyAddEstimate(Vector128.Create(Pow2C6), r, Vector128.Create(Pow2C5)),
                    Vector128.MultiplyAddEstimate(
                        r2,
                        Vector128.MultiplyAddEstimate(Vector128.Create(Pow2C4), r, Vector128.Create(Pow2C3)),
                        Vector128.MultiplyAddEstimate(Vector128.Create(Pow2C2), r, Vector128.Create(Pow2C1))));

                return (poly.AsUInt64() + (n << 52)).AsDouble();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<double> Pow2Core(Vector256<double> z)
            {
                Vector256<double> huge = Vector256.Create(Pow2Huge);
                Vector256<double> dn = z + huge;
                Vector256<ulong> n = dn.AsUInt64();
                Vector256<double> r = z - (dn - huge);
                Vector256<double> r2 = r * r;
                Vector256<double> r4 = r2 * r2;

                Vector256<double> poly = Vector256.MultiplyAddEstimate(
                    r4,
                    Vector256.MultiplyAddEstimate(Vector256.Create(Pow2C6), r, Vector256.Create(Pow2C5)),
                    Vector256.MultiplyAddEstimate(
                        r2,
                        Vector256.MultiplyAddEstimate(Vector256.Create(Pow2C4), r, Vector256.Create(Pow2C3)),
                        Vector256.MultiplyAddEstimate(Vector256.Create(Pow2C2), r, Vector256.Create(Pow2C1))));

                return (poly.AsUInt64() + (n << 52)).AsDouble();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector512<double> Pow2Core(Vector512<double> z)
            {
                Vector512<double> huge = Vector512.Create(Pow2Huge);
                Vector512<double> dn = z + huge;
                Vector512<ulong> n = dn.AsUInt64();
                Vector512<double> r = z - (dn - huge);
                Vector512<double> r2 = r * r;
                Vector512<double> r4 = r2 * r2;

                Vector512<double> poly = Vector512.MultiplyAddEstimate(
                    r4,
                    Vector512.MultiplyAddEstimate(Vector512.Create(Pow2C6), r, Vector512.Create(Pow2C5)),
                    Vector512.MultiplyAddEstimate(
                        r2,
                        Vector512.MultiplyAddEstimate(Vector512.Create(Pow2C4), r, Vector512.Create(Pow2C3)),
                        Vector512.MultiplyAddEstimate(Vector512.Create(Pow2C2), r, Vector512.Create(Pow2C1))));

                return (poly.AsUInt64() + (n << 52)).AsDouble();
            }
        }
    }
}
