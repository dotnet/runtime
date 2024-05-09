// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise base 10 logarithm of numbers in the specified tensor plus 1.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Log10P1(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// If a value equals 0, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.NegativeInfinity"/>.
        /// If a value is negative or equal to <see cref="IFloatingPointIeee754{TSelf}.NaN"/>, the result stored into the corresponding destination location is set to NaN.
        /// If a value is positive infinity, the result stored into the corresponding destination location is set to <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity"/>.
        /// Otherwise, if a value is positive, its base 10 logarithm plus 1 is stored into the corresponding destination location.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Log10P1<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ILogarithmicFunctions<T> =>
            InvokeSpanIntoSpan<T, Log10P1Operator<T>>(x, destination);

        /// <summary>T.Log10P1(x)</summary>
        private readonly struct Log10P1Operator<T> : IUnaryOperator<T, T>
            where T : ILogarithmicFunctions<T>
        {
            public static bool Vectorizable => Log10Operator<T>.Vectorizable;
            public static T Invoke(T x) => T.Log10P1(x);
            public static Vector128<T> Invoke(Vector128<T> x) => Log10Operator<T>.Invoke(x + Vector128<T>.One);
            public static Vector256<T> Invoke(Vector256<T> x) => Log10Operator<T>.Invoke(x + Vector256<T>.One);
            public static Vector512<T> Invoke(Vector512<T> x) => Log10Operator<T>.Invoke(x + Vector512<T>.One);
        }
    }
}
