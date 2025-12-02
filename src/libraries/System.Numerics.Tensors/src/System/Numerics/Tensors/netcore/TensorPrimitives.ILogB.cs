// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise integer logarithm of numbers in the specified tensor.</summary>
        /// <param name="x">The tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <exception cref="ArgumentException"><paramref name="x"/> and <paramref name="destination"/> reference overlapping memory locations and do not begin at the same location.</exception>
        /// <remarks>
        /// <para>
        /// This method effectively computes <c><paramref name="destination" />[i] = T.ILogB(<paramref name="x" />[i])</c>.
        /// </para>
        /// </remarks>
        public static void ILogB<T>(ReadOnlySpan<T> x, Span<int> destination)
            where T : IFloatingPointIeee754<T>
        {
            if (typeof(T) == typeof(double))
            {
                // Special-case double as the only vectorizable floating-point type whose size != sizeof(int).
                InvokeSpanIntoSpan_2to1<double, int, ILogBDoubleOperator>(Rename<T, double>(x), destination);
            }
            else
            {
                InvokeSpanIntoSpan<T, int, ILogBOperator<T>>(x, destination);
            }
        }

        /// <summary>T.ILogB(x)</summary>
        private readonly struct ILogBOperator<T> : IUnaryOperator<T, int> where T : IFloatingPointIeee754<T>
        {
            public static bool Vectorizable => false; // TODO: vectorize for float

            public static int Invoke(T x) => T.ILogB(x);
            public static Vector128<int> Invoke(Vector128<T> x) => throw new NotSupportedException();
            public static Vector256<int> Invoke(Vector256<T> x) => throw new NotSupportedException();
            public static Vector512<int> Invoke(Vector512<T> x) => throw new NotSupportedException();
        }

        /// <summary>double.ILogB(x)</summary>
        private readonly struct ILogBDoubleOperator : IUnaryTwoToOneOperator<double, int>
        {
            public static bool Vectorizable => false; // TODO: vectorize

            public static int Invoke(double x) => double.ILogB(x);
            public static Vector128<int> Invoke(Vector128<double> lower, Vector128<double> upper) => throw new NotSupportedException();
            public static Vector256<int> Invoke(Vector256<double> lower, Vector256<double> upper) => throw new NotSupportedException();
            public static Vector512<int> Invoke(Vector512<double> lower, Vector512<double> upper) => throw new NotSupportedException();
        }
    }
}
