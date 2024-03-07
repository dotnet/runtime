// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
            where T : IFloatingPoint<T> =>
            InvokeSpanIntoSpan<T, RoundToEvenOperator<T>>(x, destination);

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
                    InvokeSpanIntoSpan<T, RoundAwayFromZeroOperator<T>>(x, destination);
                    break;

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
            }

            ReadOnlySpan<T> roundPower10;
            if (typeof(T) == typeof(float))
            {
                ReadOnlySpan<float> roundPower10Single = [1e0f, 1e1f, 1e2f, 1e3f, 1e4f, 1e5f, 1e6f];
                roundPower10 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<float, T>(ref MemoryMarshal.GetReference(roundPower10Single)), roundPower10Single.Length);
            }
            else if (typeof(T) == typeof(double))
            {
                Debug.Assert(typeof(T) == typeof(double));
                ReadOnlySpan<double> roundPower10Double = [1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11, 1e12, 1e13, 1e14, 1e15];
                roundPower10 = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<double, T>(ref MemoryMarshal.GetReference(roundPower10Double)), roundPower10Double.Length);
            }
            else
            {
                if ((uint)mode > (uint)MidpointRounding.ToPositiveInfinity)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, typeof(MidpointRounding)), nameof(mode));
                }

                InvokeSpanIntoSpan(x, new RoundFallbackOperator<T>(digits, mode), destination);
                return;
            }

            if ((uint)digits >= (uint)roundPower10.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(digits));
            }

            T power10 = roundPower10[digits];
            switch (mode)
            {
                case MidpointRounding.ToEven:
                    InvokeSpanIntoSpan(x, new MultiplyRoundDivideOperator<T, RoundToEvenOperator<T>>(power10), destination);
                    return;

                case MidpointRounding.AwayFromZero:
                    InvokeSpanIntoSpan(x, new MultiplyRoundDivideOperator<T, RoundAwayFromZeroOperator<T>>(power10), destination);
                    break;

                case MidpointRounding.ToZero:
                    InvokeSpanIntoSpan(x, new MultiplyRoundDivideOperator<T, TruncateOperator<T>>(power10), destination);
                    return;

                case MidpointRounding.ToNegativeInfinity:
                    InvokeSpanIntoSpan(x, new MultiplyRoundDivideOperator<T, FloorOperator<T>>(power10), destination);
                    return;

                case MidpointRounding.ToPositiveInfinity:
                    InvokeSpanIntoSpan(x, new MultiplyRoundDivideOperator<T, CeilingOperator<T>>(power10), destination);
                    return;

                default:
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, typeof(MidpointRounding)), nameof(mode));
            }
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

            private const float SingleBoundary = 8388608.0f; // 2^23
            private const double DoubleBoundary = 4503599627370496.0; // 2^52

            public static Vector128<T> Invoke(Vector128<T> x)
            {
                Vector128<T> boundary = Vector128.Create(typeof(T) == typeof(float) ? T.CreateTruncating(SingleBoundary) : T.CreateTruncating(DoubleBoundary));
                Vector128<T> temp = CopySignOperator<T>.Invoke(boundary, x);
                return Vector128.ConditionalSelect(Vector128.GreaterThan(Vector128.Abs(x), boundary), x, CopySignOperator<T>.Invoke((x + temp) - temp, x));
            }

            public static Vector256<T> Invoke(Vector256<T> x)
            {
                Vector256<T> boundary = Vector256.Create(typeof(T) == typeof(float) ? T.CreateTruncating(SingleBoundary) : T.CreateTruncating(DoubleBoundary));
                Vector256<T> temp = CopySignOperator<T>.Invoke(boundary, x);
                return Vector256.ConditionalSelect(Vector256.GreaterThan(Vector256.Abs(x), boundary), x, CopySignOperator<T>.Invoke((x + temp) - temp, x));
            }

            public static Vector512<T> Invoke(Vector512<T> x)
            {
                Vector512<T> boundary = Vector512.Create(typeof(T) == typeof(float) ? T.CreateTruncating(SingleBoundary) : T.CreateTruncating(DoubleBoundary));
                Vector512<T> temp = CopySignOperator<T>.Invoke(boundary, x);
                return Vector512.ConditionalSelect(Vector512.GreaterThan(Vector512.Abs(x), boundary), x, CopySignOperator<T>.Invoke((x + temp) - temp, x));
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

        /// <summary>(T.Round(x * power10, digits, mode)) / power10</summary>
        private readonly struct MultiplyRoundDivideOperator<T, TDelegatedRound> : IStatefulUnaryOperator<T>
            where T : IFloatingPoint<T>
            where TDelegatedRound : IUnaryOperator<T, T>
        {
            private readonly T _factor;

            public MultiplyRoundDivideOperator(T factor)
            {
                Debug.Assert(typeof(T) == typeof(float) || typeof(T) == typeof(double));
                _factor = factor;
            }

            public static bool Vectorizable => true;

            private const float Single_RoundLimit = 1e8f;
            private const double Double_RoundLimit = 1e16d;

            public T Invoke(T x)
            {
                T limit = typeof(T) == typeof(float) ? T.CreateTruncating(Single_RoundLimit) : T.CreateTruncating(Double_RoundLimit);
                return T.Abs(x) < limit ?
                    TDelegatedRound.Invoke(x * _factor) / _factor :
                    x;
            }

            public Vector128<T> Invoke(Vector128<T> x)
            {
                Vector128<T> limit = Vector128.Create(typeof(T) == typeof(float) ? T.CreateTruncating(Single_RoundLimit) : T.CreateTruncating(Double_RoundLimit));
                return Vector128.ConditionalSelect(Vector128.LessThan(Vector128.Abs(x), limit),
                    TDelegatedRound.Invoke(x * _factor) / _factor,
                    x);
            }

            public Vector256<T> Invoke(Vector256<T> x)
            {
                Vector256<T> limit = Vector256.Create(typeof(T) == typeof(float) ? T.CreateTruncating(Single_RoundLimit) : T.CreateTruncating(Double_RoundLimit));
                return Vector256.ConditionalSelect(Vector256.LessThan(Vector256.Abs(x), limit),
                    TDelegatedRound.Invoke(x * _factor) / _factor,
                    x);
            }

            public Vector512<T> Invoke(Vector512<T> x)
            {
                Vector512<T> limit = Vector512.Create(typeof(T) == typeof(float) ? T.CreateTruncating(Single_RoundLimit) : T.CreateTruncating(Double_RoundLimit));
                return Vector512.ConditionalSelect(Vector512.LessThan(Vector512.Abs(x), limit),
                    TDelegatedRound.Invoke(x * _factor) / _factor,
                    x);
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
