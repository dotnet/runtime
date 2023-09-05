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
                int remaining = x.Length;

                if (Vector.IsHardwareAccelerated && remaining >= Vector<float>.Count)
                {
                    do
                    {
                        *(Vector<float>*)pd = *(Vector<float>*)px + *(Vector<float>*)py;

                        px += Vector<float>.Count;
                        py += Vector<float>.Count;
                        pd += Vector<float>.Count;
                        remaining -= Vector<float>.Count;
                    }
                    while (remaining >= Vector<float>.Count);
                }

                while (remaining != 0)
                {
                    *pd = *px + *py;

                    px++;
                    py++;
                    pd++;
                    remaining--;
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
                int remaining = x.Length;

                if (Vector.IsHardwareAccelerated && remaining >= Vector<float>.Count)
                {
                    Vector<float> yVec = new Vector<float>(y);
                    do
                    {
                        *(Vector<float>*)pd = *(Vector<float>*)px + yVec;

                        px += Vector<float>.Count;
                        pd += Vector<float>.Count;
                        remaining -= Vector<float>.Count;
                    }
                    while (remaining >= Vector<float>.Count);
                }

                while (remaining != 0)
                {
                    *pd = *px + y;

                    px++;
                    pd++;
                    remaining--;
                }
            }
        }
    }
}
