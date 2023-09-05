// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Numerics.Tensors
{
    /// <summary>Performs primitive tensor operations over spans of memory.</summary>
    public static unsafe partial class TensorPrimitives
    {
        /// <summary>Computes the element-wise result of: <c><paramref name="x" /> + <paramref name="y" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Length of '<paramref name="x" />' must be same as length of '<paramref name="y" />'.</exception>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <paramref name="x" />[i] + <paramref name="y" />[i]</c>.</remarks>
        public static void Add(ReadOnlySpan<float> x, ReadOnlySpan<float> y, Span<float> destination)
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength(nameof(x), nameof(y));
            }

            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            fixed (float* xPtr = &MemoryMarshal.GetReference(x), yPtr = &MemoryMarshal.GetReference(y), destPtr = &MemoryMarshal.GetReference(destination))
            {
                float* px = xPtr, py = yPtr, pd = destPtr;
                int i = 0, oneVectorFromEnd;

                if (Vector.IsHardwareAccelerated)
                {
                    oneVectorFromEnd = x.Length - Vector<float>.Count;
                    if (oneVectorFromEnd >= 0)
                    {
                        do
                        {
                            *(Vector<float>*)(pd + i) = *(Vector<float>*)(px + i) + *(Vector<float>*)(py + i);

                            i += Vector<float>.Count;
                        }
                        while (i <= oneVectorFromEnd);
                    }
                }

                while (i < x.Length)
                {
                    *(pd + i) = *(px + i) + *(py + i);

                    i++;
                }
            }
        }

        /// <summary>Computes the element-wise result of: <c><paramref name="x" /> + <paramref name="y" /></c>.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a scalar.</param>
        /// <param name="destination">The destination tensor, represented as a span.</param>
        /// <exception cref="ArgumentException">Destination is too short.</exception>
        /// <remarks>This method effectively does <c><paramref name="destination" />[i] = <paramref name="x" />[i] + <paramref name="y" /></c>.</remarks>
        public static void Add(ReadOnlySpan<float> x, float y, Span<float> destination)
        {
            if (x.Length > destination.Length)
            {
                ThrowHelper.ThrowArgument_DestinationTooShort();
            }

            fixed (float* xPtr = &MemoryMarshal.GetReference(x), destPtr = &MemoryMarshal.GetReference(destination))
            {
                float* px = xPtr, pd = destPtr;
                int i = 0, oneVectorFromEnd;

                if (Vector.IsHardwareAccelerated)
                {
                    oneVectorFromEnd = x.Length - Vector<float>.Count;
                    if (oneVectorFromEnd >= 0)
                    {
                        Vector<float> yVec = new Vector<float>(y);
                        do
                        {
                            *(Vector<float>*)(pd + i) = *(Vector<float>*)(px + i) + yVec;

                            i += Vector<float>.Count;
                        }
                        while (i <= oneVectorFromEnd);
                    }
                }

                while (i < x.Length)
                {
                    *(pd + i) = *(px + i) + y;

                    i++;
                }
            }
        }
    }
}
