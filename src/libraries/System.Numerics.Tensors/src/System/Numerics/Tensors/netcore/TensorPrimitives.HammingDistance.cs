// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static partial class TensorPrimitives
    {
        /// <summary>Computes the bitwise Hamming distance between two equal-length tensors of values.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The number of bits that differ between the two spans.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="x" /> and <paramref name="y" /> must not be empty.</exception>
        public static long HammingBitDistance<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y) where T : IBinaryInteger<T>
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            // Single-byte elements benefit from counting bits a vector at a time: the scalar loop
            // pays one population count per byte, where the vectorized path covers a whole vector.
            // Wider elements already amortize the population count over 2, 4 or 8 bytes, and
            // measured slower when vectorized this way, so they keep the scalar loop.
            if ((typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte)) &&
                Vector128.IsHardwareAccelerated &&
                x.Length >= Vector128<byte>.Count)
            {
                return HammingBitDistanceCore(
                    MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(x)), x.Length),
                    MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(y)), y.Length));
            }

            long count = 0;
            for (int i = 0; i < x.Length; i++)
            {
                count += long.CreateTruncating(T.PopCount(x[i] ^ y[i]));
            }

            return count;
        }

        /// <summary>Counts the bits that differ between two equal-length byte spans.</summary>
        private static long HammingBitDistanceCore(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
        {
            Vector128<byte> nibbleCounts = Vector128.Create((byte)0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4);
            Vector128<byte> lowMask = Vector128.Create((byte)0x0F);

            long count = 0;
            int i = 0;

            while (x.Length - i >= Vector128<byte>.Count)
            {
                // A byte lane holds at most 8 per vector, so it can absorb 31 vectors before overflowing.
                int block = Math.Min(31, (x.Length - i) / Vector128<byte>.Count);
                Vector128<byte> counts = Vector128<byte>.Zero;

                for (int step = 0; step < block; step++)
                {
                    Vector128<byte> difference =
                        Vector128.Create(x.Slice(i, Vector128<byte>.Count)) ^
                        Vector128.Create(y.Slice(i, Vector128<byte>.Count));

                    counts += Vector128.Shuffle(nibbleCounts, difference & lowMask) +
                              Vector128.Shuffle(nibbleCounts, (difference >>> 4) & lowMask);

                    i += Vector128<byte>.Count;
                }

                Vector128<ushort> lower = Vector128.WidenLower(counts);
                Vector128<ushort> upper = Vector128.WidenUpper(counts);
                count += Vector128.Sum(
                    Vector128.WidenLower(lower) + Vector128.WidenUpper(lower) +
                    Vector128.WidenLower(upper) + Vector128.WidenUpper(upper));
            }

            for (; i < x.Length; i++)
            {
                count += BitOperations.PopCount((uint)(byte)(x[i] ^ y[i]));
            }

            return count;
        }

        /// <summary>Computes the Hamming distance between two equal-length tensors of values.</summary>
        /// <param name="x">The first tensor, represented as a span.</param>
        /// <param name="y">The second tensor, represented as a span.</param>
        /// <returns>The number of elements that differ between the two spans.</returns>
        /// <exception cref="ArgumentException">Length of <paramref name="x" /> must be same as length of <paramref name="y" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="x" /> and <paramref name="y" /> must not be empty.</exception>
        /// <remarks>
        /// <para>
        /// This method computes the number of locations <c>i</c> where <c>!EqualityComparer&gt;T&lt;.Default.Equal(x[i], y[i])</c>.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HammingDistance<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y)
        {
            if (typeof(T) == typeof(char))
            {
                // Special-case char, as it's reasonable for someone to want to use HammingDistance on strings,
                // and we want that accelerated. This can be removed if/when VectorXx<T> supports char.
                return CountUnequalElements<ushort>(
                    MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, ushort>(ref MemoryMarshal.GetReference(x)), x.Length),
                    MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, ushort>(ref MemoryMarshal.GetReference(y)), y.Length));
            }

            return CountUnequalElements(x, y);
        }

        /// <summary>Counts the number of elements that are pair-wise different between the two spans.</summary>
        private static int CountUnequalElements<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y)
        {
            if (x.Length != y.Length)
            {
                ThrowHelper.ThrowArgument_SpansMustHaveSameLength();
            }

            // TODO: This has a very similar structure to CosineSimilarity, which is also open-coded rather than
            // using a shared routine plus operator, as we don't have one implemented that exactly fits. We should
            // look at refactoring these to share the core logic.

            int count = 0;
            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && x.Length >= Vector128<T>.Count)
            {
                if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && x.Length >= Vector256<T>.Count)
                {
                    if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && x.Length >= Vector512<T>.Count)
                    {
                        ref T xRef = ref MemoryMarshal.GetReference(x);
                        ref T yRef = ref MemoryMarshal.GetReference(y);

                        int oneVectorFromEnd = x.Length - Vector512<T>.Count;
                        int i = 0;
                        do
                        {
                            Vector512<T> xVec = Vector512.LoadUnsafe(ref xRef, (uint)i);
                            Vector512<T> yVec = Vector512.LoadUnsafe(ref yRef, (uint)i);

                            count += BitOperations.PopCount((~Vector512.Equals(xVec, yVec)).ExtractMostSignificantBits());

                            i += Vector512<T>.Count;
                        }
                        while (i <= oneVectorFromEnd);

                        // Process the last vector in the span, masking off elements already processed.
                        if (i != x.Length)
                        {
                            Vector512<T> xVec = Vector512.LoadUnsafe(ref xRef, (uint)(x.Length - Vector512<T>.Count));
                            Vector512<T> yVec = Vector512.LoadUnsafe(ref yRef, (uint)(x.Length - Vector512<T>.Count));

                            Vector512<T> remainderMask = CreateRemainderMaskVector512<T>(x.Length - i);
                            xVec &= remainderMask;
                            yVec &= remainderMask;

                            count += BitOperations.PopCount((~Vector512.Equals(xVec, yVec)).ExtractMostSignificantBits());
                        }
                    }
                    else
                    {
                        ref T xRef = ref MemoryMarshal.GetReference(x);
                        ref T yRef = ref MemoryMarshal.GetReference(y);

                        // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                        int oneVectorFromEnd = x.Length - Vector256<T>.Count;
                        int i = 0;
                        do
                        {
                            Vector256<T> xVec = Vector256.LoadUnsafe(ref xRef, (uint)i);
                            Vector256<T> yVec = Vector256.LoadUnsafe(ref yRef, (uint)i);

                            count += BitOperations.PopCount((~Vector256.Equals(xVec, yVec)).ExtractMostSignificantBits());

                            i += Vector256<T>.Count;
                        }
                        while (i <= oneVectorFromEnd);

                        // Process the last vector in the span, masking off elements already processed.
                        if (i != x.Length)
                        {
                            Vector256<T> xVec = Vector256.LoadUnsafe(ref xRef, (uint)(x.Length - Vector256<T>.Count));
                            Vector256<T> yVec = Vector256.LoadUnsafe(ref yRef, (uint)(x.Length - Vector256<T>.Count));

                            Vector256<T> remainderMask = CreateRemainderMaskVector256<T>(x.Length - i);
                            xVec &= remainderMask;
                            yVec &= remainderMask;

                            count += BitOperations.PopCount((~Vector256.Equals(xVec, yVec)).ExtractMostSignificantBits());
                        }
                    }
                }
                else
                {
                    ref T xRef = ref MemoryMarshal.GetReference(x);
                    ref T yRef = ref MemoryMarshal.GetReference(y);

                    // Process vectors, summing their dot products and squares, as long as there's a vector's worth remaining.
                    int oneVectorFromEnd = x.Length - Vector128<T>.Count;
                    int i = 0;
                    do
                    {
                        Vector128<T> xVec = Vector128.LoadUnsafe(ref xRef, (uint)i);
                        Vector128<T> yVec = Vector128.LoadUnsafe(ref yRef, (uint)i);

                        count += BitOperations.PopCount((~Vector128.Equals(xVec, yVec)).ExtractMostSignificantBits());

                        i += Vector128<T>.Count;
                    }
                    while (i <= oneVectorFromEnd);

                    // Process the last vector in the span, masking off elements already processed.
                    if (i != x.Length)
                    {
                        Vector128<T> xVec = Vector128.LoadUnsafe(ref xRef, (uint)(x.Length - Vector128<T>.Count));
                        Vector128<T> yVec = Vector128.LoadUnsafe(ref yRef, (uint)(x.Length - Vector128<T>.Count));

                        Vector128<T> remainderMask = CreateRemainderMaskVector128<T>(x.Length - i);
                        xVec &= remainderMask;
                        yVec &= remainderMask;

                        count += BitOperations.PopCount((~Vector128.Equals(xVec, yVec)).ExtractMostSignificantBits());
                    }
                }
            }
            else if (typeof(T).IsValueType)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    if (!EqualityComparer<T>.Default.Equals(x[i], y[i]))
                    {
                        count++;
                    }
                }
            }
            else
            {
                EqualityComparer<T> comparer = EqualityComparer<T>.Default;
                for (int i = 0; i < x.Length; i++)
                {
                    if (!comparer.Equals(x[i], y[i]))
                    {
                        count++;
                    }
                }
            }

            Debug.Assert(count >= 0 && count <= x.Length, $"Expected count to be in the range [0, {x.Length}], got {count}.");
            return count;
        }
    }
}
