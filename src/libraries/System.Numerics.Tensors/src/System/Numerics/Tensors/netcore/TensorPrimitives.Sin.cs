// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise sine of the value in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Sin(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians(System.Single)"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Sin<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            if (typeof(T) == typeof(Half) && TryUnaryInvokeHalfAsInt16<T, SinOperator<float>>(x, destination))
            {
                return;
            }

            InvokeSpanIntoSpan<T, SinOperator<T>>(x, destination);
        }

        /// <summary>T.Sin(x)</summary>
        internal readonly struct SinOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            // This code is based on `vrs4_sin` and `vrd2_sin` from amd/aocl-libm-ose
            // Copyright (C) 2019-2020 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            // Implementation notes from amd/aocl-libm-ose:
            // -----------------------------------------------------------------
            // Convert given x into the form
            // |x| = N * pi + f where N is an integer and f lies in [-pi/2,pi/2]
            // N is obtained by : N = round(x/pi)
            // f is obtained by : f = abs(x)-N*pi
            // sin(x) = sin(N * pi + f) = sin(N * pi)*cos(f) + cos(N*pi)*sin(f)
            // sin(x) = sign(x)*sin(f)*(-1)**N
            //
            // The term sin(f) can be approximated by using a polynomial

            public static bool Vectorizable => (typeof(T) == typeof(float))
                                            || (typeof(T) == typeof(double));

            public static T Invoke(T x) => T.Sin(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Sin(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Sin(x.AsSingle()).As<float, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Sin(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Sin(x.AsSingle()).As<float, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Sin(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Sin(x.AsSingle()).As<float, T>();
                }
            }
        }

        // These are still used by SinPiOperator

        private readonly struct SinOperatorSingle
        {
            internal const uint MaxVectorizedValue = 0x49800000u;
            internal const uint SignMask = 0x7FFFFFFFu;
        }

        private readonly struct SinOperatorDouble
        {
            internal const ulong SignMask = 0x7FFFFFFFFFFFFFFFul;
            internal const ulong MaxVectorizedValue = 0x4160000000000000ul;
        }
    }
}
