// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Round(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void Round<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IFloatingPoint<T>
        {
            if (typeof(T) == typeof(Half) && TryUnaryInvokeHalfAsInt16<T, RoundToEvenOperator<float>>(x, destination))
            {
                return;
            }

            InvokeSpanIntoSpan<T, RoundToEvenOperator<T>>(x, destination);
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="mode">The mode under which <paramref name="x" /> should be rounded.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Round(<paramref name="x" />[i], <paramref name="mode"/>)</c>.
        /// </para>
        /// </remarks>
        public static void Round<T>(ReadOnlySpan<T> x, MidpointRounding mode, Span<T> destination)
            where T : IFloatingPoint<T>
        {
            switch (mode)
            {
                case MidpointRounding.ToEven:
                    Round(x, destination);
                    return;

                case MidpointRounding.AwayFromZero:
                    if (typeof(T) == typeof(Half) && TryUnaryInvokeHalfAsInt16<T, RoundAwayFromZeroOperator<float>>(x, destination))
                    {
                        return;
                    }

                    InvokeSpanIntoSpan<T, RoundAwayFromZeroOperator<T>>(x, destination);
                    return;

                case MidpointRounding.ToZero:
                    Truncate(x, destination);
                    return;

                case MidpointRounding.ToNegativeInfinity:
                    Floor(x, destination);
                    return;

                case MidpointRounding.ToPositiveInfinity:
                    Ceiling(x, destination);
                    return;

                default:
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, typeof(MidpointRounding)), nameof(mode));
            }
        }

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="digits">The number of fractional digits to which the numbers in <paramref name="x" /> should be rounded.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Round(<paramref name="x" />[i], <paramref name="digits"/>)</c>.
        /// </para>
        /// </remarks>
        public static void Round<T>(ReadOnlySpan<T> x, int digits, Span<T> destination) where T : IFloatingPoint<T> =>
            Round(x, digits, MidpointRounding.ToEven, destination);

        /// <summary>Computes the element-wise rounding of the numbers in the specified tensor</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="digits">The number of fractional digits to which the numbers in <paramref name="x" /> should be rounded.</param>
        /// <param name="mode">The mode under which <paramref name="x" /> should be rounded.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="digits"/> is invalid.</exception>
        /// <exception cref="ArgumentException"><paramref name="mode"/> is invalid.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.Round(<paramref name="x" />[i], <paramref name="digits"/>, <paramref name="mode"/>)</c>.
        /// </para>
        /// </remarks>
        public static void Round<T>(ReadOnlySpan<T> x, int digits, MidpointRounding mode, Span<T> destination)
            where T : IFloatingPoint<T>
        {
            if (digits == 0)
            {
                Round(x, mode, destination);
                return;
            }

            if (digits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(digits));
            }

            if ((uint)mode > (uint)MidpointRounding.ToPositiveInfinity)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, typeof(MidpointRounding)), nameof(mode));
            }

            // The digit-based rounding defers to the scalar `T.Round` for every element, which accepts any
            // non-negative `digits` (matching the scalar API). A correctly-rounded vectorized implementation
            // needs the exact (e.g. double-double or arbitrary precision) scaled value to match the scalar
            // result at the midpoints, so that acceleration is left as a future improvement.
            InvokeSpanIntoSpan(x, new RoundFallbackOperator<T>(digits, mode), destination);
        }

        /// <summary>T.Round(x)</summary>
        private readonly struct RoundToEvenOperator<T> : IUnaryOperator<T, T> where T : IFloatingPoint<T>
        {
            // This code is based on `nearbyint` from amd/aocl-libm-ose
            // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
            //
            // Licensed under the BSD 3-Clause "New" or "Revised" License
            // See THIRD-PARTY-NOTICES.TXT for the full license text

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Round(x);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return Vector128.Round(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector128.Round(x.AsSingle()).As<float, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return Vector256.Round(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector256.Round(x.AsSingle()).As<float, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(double))
                {
                    return Vector512.Round(x.AsDouble()).As<double, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(float));
                    return Vector512.Round(x.AsSingle()).As<float, T>();
                }
            }
        }

        /// <summary>T.Round(x, MidpointRounding.AwayFromZero)</summary>
        private readonly struct RoundAwayFromZeroOperator<T> : IUnaryOperator<T, T> where T : IFloatingPoint<T>
        {
            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Round(x, MidpointRounding.AwayFromZero);

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    if (AdvSimd.IsSupported)
                    {
                        return AdvSimd.RoundAwayFromZero(x.AsSingle()).As<float, T>();
                    }

                    return TruncateOperator<float>.Invoke(x.AsSingle() + CopySignOperator<float>.Invoke(Vector128.Create(0.49999997f), x.AsSingle())).As<float, T>();
                }
                else
                {
                    if (AdvSimd.Arm64.IsSupported)
                    {
                        return AdvSimd.Arm64.RoundAwayFromZero(x.AsDouble()).As<double, T>();
                    }

                    Debug.Assert(typeof(T) == typeof(double));
                    return TruncateOperator<double>.Invoke(x.AsDouble() + CopySignOperator<double>.Invoke(Vector128.Create(0.49999999999999994), x.AsDouble())).As<double, T>();
                }
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return TruncateOperator<float>.Invoke(x.AsSingle() + CopySignOperator<float>.Invoke(Vector256.Create(0.49999997f), x.AsSingle())).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return TruncateOperator<double>.Invoke(x.AsDouble() + CopySignOperator<double>.Invoke(Vector256.Create(0.49999999999999994), x.AsDouble())).As<double, T>();
                }
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                if (typeof(T) == typeof(float))
                {
                    return TruncateOperator<float>.Invoke(x.AsSingle() + CopySignOperator<float>.Invoke(Vector512.Create(0.49999997f), x.AsSingle())).As<float, T>();
                }
                else
                {
                    Debug.Assert(typeof(T) == typeof(double));
                    return TruncateOperator<double>.Invoke(x.AsDouble() + CopySignOperator<double>.Invoke(Vector512.Create(0.49999999999999994), x.AsDouble())).As<double, T>();
                }
            }
        }

        /// <summary>T.Round(x, digits, mode)</summary>
        private readonly struct RoundFallbackOperator<T>(int digits, MidpointRounding mode) : IStatefulUnaryOperator<T>
            where T : IFloatingPoint<T>
        {
            private readonly int _digits = digits;
            private readonly MidpointRounding _mode = mode;

            public static bool Vectorizable => false;

            public T Invoke(T x) => T.Round(x, _digits, _mode);

            public Vector128<T> Invoke(Vector128<T> x) => throw new NotSupportedException();
            public Vector256<T> Invoke(Vector256<T> x) => throw new NotSupportedException();
            public Vector512<T> Invoke(Vector512<T> x) => throw new NotSupportedException();
        }
    }
}
