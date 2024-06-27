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

            long count = 0;
            for (int i = 0; i < x.Length; i++)
            {
                count += long.CreateTruncating(T.PopCount(x[i] ^ y[i]));
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
