// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/>, converting each <typeparamref name="TFrom"/>
        /// value to a <typeparamref name="TTo"/> value.
        /// </summary>
        /// <param name="source">The source span from which to copy values.</param>
        /// <param name="destination">The destination span into which the converted values should be written.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = TTo.CreateSaturating(<paramref name="source"/>[i])</c>.
        /// </para>
        /// </remarks>
        public static void ConvertSaturating<TFrom, TTo>(ReadOnlySpan<TFrom> source, Span<TTo> destination)
            where TFrom : INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            if (!TryConvertUniversal(source, destination))
            {
                InvokeSpanIntoSpan<TFrom, TTo, ConvertSaturatingFallbackOperator<TFrom, TTo>>(source, destination);
            }
        }

        /// <summary>T.CreateSaturating(x)</summary>
        internal readonly struct ConvertSaturatingFallbackOperator<TFrom, TTo> : IUnaryOperator<TFrom, TTo> where TFrom : INumberBase<TFrom> where TTo : INumberBase<TTo>
        {
            public static bool Vectorizable => false;

            public static TTo Invoke(TFrom x) => TTo.CreateSaturating(x);
            public static Vector128<TTo> Invoke(Vector128<TFrom> x) => throw new NotSupportedException();
            public static Vector256<TTo> Invoke(Vector256<TFrom> x) => throw new NotSupportedException();
            public static Vector512<TTo> Invoke(Vector512<TFrom> x) => throw new NotSupportedException();
        }
    }
}
