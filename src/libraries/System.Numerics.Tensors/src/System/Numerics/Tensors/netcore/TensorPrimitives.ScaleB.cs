// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise product of numbers in the specified tensor and their base-radix raised to the specified power.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="n">The value to which base-radix is raised before multipliying x, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.ILogB(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void ScaleB<T>(ReadOnlySpan<T> x, int n, Span<T> destination)
            where T : IFloatingPointIeee754<T> =>
            InvokeSpanIntoSpan(x, new ScaleBOperator<T>(n), destination);

        /// <summary>T.ScaleB(x, n)</summary>
        private readonly struct ScaleBOperator<T>(int n) : IStatefulUnaryOperator<T> where T : IFloatingPointIeee754<T>
        {
            private readonly int _n = n;
            private readonly T _pow2n = typeof(T) == typeof(float) || typeof(T) == typeof(double) ? T.Pow(T.CreateTruncating(2), T.CreateTruncating(n)) : default!;

            public static bool Vectorizable => typeof(T) == typeof(float) || typeof(T) == typeof(double);

            public T Invoke(T x) => T.ScaleB(x, _n);
            public Vector128<T> Invoke(Vector128<T> x) => x * Vector128.Create(_pow2n);
            public Vector256<T> Invoke(Vector256<T> x) => x * Vector256.Create(_pow2n);
            public Vector512<T> Invoke(Vector512<T> x) => x * Vector512.Create(_pow2n);
        }
    }
}
