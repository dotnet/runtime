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
        /// The angles in x must be in radians. Use <see cref="M:System.Single.DegreesToRadians(System.Single)"/> or multiply by <typeparamref name="T"/>.Pi/180 to convert degrees to radians.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Cos<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T>
        {
            if (typeof(T) == typeof(Half) && TryUnaryInvokeHalfAsInt16<T, CosOperator<float>>(x, destination))
            {
                return;
            }

            InvokeSpanIntoSpan<T, CosOperator<T>>(x, destination);
        }

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

            public static bool Vectorizable => (typeof(T) == typeof(float))
                                            || (typeof(T) == typeof(double));

            public static T Invoke(T x) => T.Cos(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Cos(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Cos(x.AsSingle()).As<float, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Cos(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Cos(x.AsSingle()).As<float, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Cos(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Cos(x.AsSingle()).As<float, T>();
                }
            }
        }

        // These are still used by CosPiOperator

        private readonly struct CosOperatorSingle
        {
            internal const uint MaxVectorizedValue = 0x4A989680u;
            internal const uint SignMask = 0x7FFFFFFFu;
        }

        private readonly struct CosOperatorDouble
        {
            internal const ulong SignMask = 0x7FFFFFFFFFFFFFFFul;
            internal const ulong MaxVectorizedValue = 0x4160000000000000ul;
        }
    }
}
