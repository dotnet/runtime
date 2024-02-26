// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise sigmoid function on the specified non-empty tensor of numbers.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x" /> must not be empty.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = 1f / (1f + <typeparamref name="T"/>.Exp(-<paramref name="x" />[i]))</c>.
        /// </para>
        /// <para>
        /// This method may call into the underlying C runtime or employ instructions specific to the current architecture. Exact results may differ between different
        /// operating systems or architectures.
        /// </para>
        /// </remarks>
        public static void Sigmoid<T>(ReadOnlySpan<T> x, Span<T> destination)
            where T : IExponentialFunctions<T>
        {
            if (x.IsEmpty)
            {
                ThrowHelper.ThrowArgument_SpansMustBeNonEmpty();
            }

            InvokeSpanIntoSpan<T, SigmoidOperator<T>>(x, destination);
        }

        /// <summary>1 / (1 + T.Exp(-x))</summary>
        internal readonly struct SigmoidOperator<T> : IUnaryOperator<T, T> where T : IExponentialFunctions<T>
        {
            public static bool Vectorizable => ExpOperator<T>.Vectorizable;
            public static T Invoke(T x) => T.One / (T.One + T.Exp(-x));
            public static Vector128<T> Invoke(Vector128<T> x) => Vector128.Create(T.One) / (Vector128.Create(T.One) + ExpOperator<T>.Invoke(-x));
            public static Vector256<T> Invoke(Vector256<T> x) => Vector256.Create(T.One) / (Vector256.Create(T.One) + ExpOperator<T>.Invoke(-x));
            public static Vector512<T> Invoke(Vector512<T> x) => Vector512.Create(T.One) / (Vector512.Create(T.One) + ExpOperator<T>.Invoke(-x));
        }
    }
}
