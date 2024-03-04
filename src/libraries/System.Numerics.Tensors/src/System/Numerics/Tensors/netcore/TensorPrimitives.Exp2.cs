﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise result of raising 2 to the number powers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Exp2(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Exp2<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IExponentialFunctions<T> =>
            InvokeSpanIntoSpan<T, Exp2Operator<T>>(x, destination);

        /// <summary>T.Exp2(x)</summary>
        private readonly struct Exp2Operator<T> : IUnaryOperator<T, T>
            where T : IExponentialFunctions<T>
        {
            private const double NaturalLog2 = 0.6931471805599453;

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Exp2(x);
            public static Vector128<T> Invoke(Vector128<T> x) => ExpOperator<T>.Invoke(x * Vector128.Create(T.CreateTruncating(NaturalLog2)));
            public static Vector256<T> Invoke(Vector256<T> x) => ExpOperator<T>.Invoke(x * Vector256.Create(T.CreateTruncating(NaturalLog2)));
            public static Vector512<T> Invoke(Vector512<T> x) => ExpOperator<T>.Invoke(x * Vector512.Create(T.CreateTruncating(NaturalLog2)));
        }
    }
}
