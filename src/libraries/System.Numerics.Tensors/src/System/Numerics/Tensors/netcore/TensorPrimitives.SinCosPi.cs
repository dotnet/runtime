// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise sine and cosine of the value in the specified tensor that has been multiplied by Pi.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="sinPiDestination">The destination tensor for the element-wise sine result, represented as a span.</param>
        /// <param name="cosPiDestination">The destination tensor for the element-wise cosine result, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="sinPiDestination"/> or <paramref name="cosPiDestination" /> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c>(<paramref name="sinPiDestination" />[i], <paramref name="cosPiDestination" />[i]) = <typeparamref name="T"/>.SinCos(<paramref name="x" />[i])</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void SinCosPi<T>(ReadOnlySpan<T> x, Span<T> sinPiDestination, Span<T> cosPiDestination)
            where T : ITrigonometricFunctions<T> =>
            InvokeSpanIntoSpan_TwoOutputs<T, SinCosPiOperator<T>>(x, sinPiDestination, cosPiDestination);

        /// <summary>T.SinCosPi(x)</summary>
        private readonly struct SinCosPiOperator<T> : IUnaryInputBinaryOutput<T> where T : ITrigonometricFunctions<T>
        {
            public static bool Vectorizable => false; // TODO: vectorize

            public static (T, T) Invoke(T x) => T.SinCosPi(x);
            public static (Vector128<T> First, Vector128<T> Second) Invoke(Vector128<T> x) => throw new NotSupportedException();
            public static (Vector256<T> First, Vector256<T> Second) Invoke(Vector256<T> x) => throw new NotSupportedException();
            public static (Vector512<T> First, Vector512<T> Second) Invoke(Vector512<T> x) => throw new NotSupportedException();
        }
    }
}
