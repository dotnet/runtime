// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise result of raising 10 to the number powers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.Exp10(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Exp10<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IExponentialFunctions<T>
        {
            if (typeof(T) == typeof(Half) && TryUnaryInvokeHalfAsInt16<T, Exp10Operator<float>>(x, destination))
            {
                return;
            }

            InvokeSpanIntoSpan<T, Exp10Operator<T>>(x, destination);
        }

        /// <summary>T.Exp10(x)</summary>
        private readonly struct Exp10Operator<T> : IUnaryOperator<T, T>
            where T : IExponentialFunctions<T>
        {
            private const double NaturalLog10 = 2.302585092994046;
            private const double Log2Of10 = 3.321928094887362;

            // For float, 10^x is below half the smallest subnormal for x < -45.2 and above float.MaxValue for x > 38.54.
            private const float MinArgument = -46f;
            private const float MaxArgument = 39f;

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public static T Invoke(T x) => T.Exp10(x);
            public static Vector128<T> Invoke(Vector128<T> x) => ExpOperator<T>.InvokeScaled(x, NaturalLog10, Log2Of10, MinArgument, MaxArgument);
            public static Vector256<T> Invoke(Vector256<T> x) => ExpOperator<T>.InvokeScaled(x, NaturalLog10, Log2Of10, MinArgument, MaxArgument);
            public static Vector512<T> Invoke(Vector512<T> x) => ExpOperator<T>.InvokeScaled(x, NaturalLog10, Log2Of10, MinArgument, MaxArgument);
        }
    }
}
