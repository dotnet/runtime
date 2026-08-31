// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise angle in radians whose cosine is the specifed number and divides the result by Pi.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = <typeparamref name="T"/>.AcosPi(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void AcosPi<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan<T, AcosPiOperator<T>>(x, destination);

        /// <summary>T.AcosPi(x)</summary>
        private readonly struct AcosPiOperator<T> : IUnaryOperator<T, T>
            where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => AcosOperator<T>.Vectorizable;
            public static T Invoke(T x) => T.AcosPi(x);
            public static Vector128<T> Invoke(Vector128<T> x) => AcosOperator<T>.Invoke(x) / Vector128.Create(T.Pi);
            public static Vector256<T> Invoke(Vector256<T> x) => AcosOperator<T>.Invoke(x) / Vector256.Create(T.Pi);
            public static Vector512<T> Invoke(Vector512<T> x) => AcosOperator<T>.Invoke(x) / Vector512.Create(T.Pi);
        }
    }
}
