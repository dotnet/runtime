// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace System.Numerics.Tensors
{
    public static unsafe partial class TensorPrimitives
    {
        /// <summary>The operations an IndexOfMin/Max-style search needs from its ordering.</summary>
        /// <remarks>
        /// <see cref="Compare(T, T)"/> returns whether <c>x</c> should replace <c>y</c>: strictly better, or an equal value the
        /// operator orders by sign (-0 before +0 and the like). <see cref="Reduce(T, T)"/> and <see cref="Aggregate(Vector128{T})"/>
        /// must return an element that <see cref="Compare(T, T)"/> ranks no worse than any input, and for floating-point types must
        /// propagate NaN, so that a block containing a NaN reduces to NaN.
        /// </remarks>
        private interface IIndexOfMinMaxOperator<T>
        {
            static abstract T Aggregate(Vector128<T> value);
            static abstract T Aggregate(Vector256<T> value);
            static abstract T Aggregate(Vector512<T> value);
            static abstract T Reduce(T x, T y);
            static abstract Vector128<T> Reduce(Vector128<T> x, Vector128<T> y);
            static abstract Vector256<T> Reduce(Vector256<T> x, Vector256<T> y);
            static abstract Vector512<T> Reduce(Vector512<T> x, Vector512<T> y);
            static abstract bool Compare(T x, T y);
            static abstract Vector128<T> Compare(Vector128<T> x, Vector128<T> y);
            static abstract Vector256<T> Compare(Vector256<T> x, Vector256<T> y);
            static abstract Vector512<T> Compare(Vector512<T> x, Vector512<T> y);
        }

        /// <summary>Number of vectors per block in the block-reduction search (256 ints per block at 256 bits).</summary>
        private const int BlockVectors = 32;

        /// <summary>
        /// Finds the index of the best element of <paramref name="x"/> under <typeparamref name="TOperator"/>, or -1 for an empty span,
        /// with a two-pass block reduction: pass 1 reduces every block to its best element with a pure vector loop (one load and one
        /// <see cref="IIndexOfMinMaxOperator{T}.Reduce(Vector256{T}, Vector256{T})"/> per vector, no index tracking and no blends) and
        /// remembers the first block whose best beats the running result; pass 2 scans only that block for the first element the result
        /// does not beat.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The reduction is what makes this run at memory bandwidth regardless of the input pattern; an index-vector loop needs a compare
        /// and two selects per element. Indices never live in vector lanes, so element sizes need no special handling.
        /// </para>
        /// <para>
        /// Ties: within a block, the reduction picks some tied-best element and the scan then returns the earliest element it does not beat,
        /// which is the earliest tied-best element; across blocks the strict <see cref="IIndexOfMinMaxOperator{T}.Compare(T, T)"/> keeps the
        /// earliest block. For floating-point types a block containing a NaN reduces to NaN (the reductions propagate it), and the index of
        /// the first NaN of that block is returned, which is the first NaN overall because earlier blocks contained none.
        /// </para>
        /// </remarks>
        private static int IndexOfMinMaxCore<T, TOperator>(ReadOnlySpan<T> x)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            if (x.IsEmpty)
            {
                return -1;
            }

            if (Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported && x.Length >= Vector512<T>.Count)
            {
                return IndexOfMinMaxBlocks512<T, TOperator>(x);
            }

            if (Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported && x.Length >= Vector256<T>.Count)
            {
                return IndexOfMinMaxBlocks256<T, TOperator>(x);
            }

            if (Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported && x.Length >= Vector128<T>.Count)
            {
                return IndexOfMinMaxBlocks128<T, TOperator>(x);
            }

            return IndexOfMinMaxFallback<T, TOperator>(x);
        }

        private static int IndexOfMinMaxFallback<T, TOperator>(ReadOnlySpan<T> x)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            T result = x[0];
            int resultIndex = 0;
            if (T.IsNaN(result))
            {
                return resultIndex;
            }

            for (int i = 1; i < x.Length; i++)
            {
                T current = x[i];
                if (T.IsNaN(current))
                {
                    return i;
                }
                if (TOperator.Compare(current, result))
                {
                    result = current;
                    resultIndex = i;
                }
            }

            return resultIndex;
        }

        /// <summary>
        /// Debug-only contract check for <see cref="IIndexOfMinMaxOperator{T}"/>: no element among the first <paramref name="length"/>
        /// elements at <paramref name="xRef"/> beats <paramref name="value"/> under <typeparamref name="TOperator"/>, and none is NaN
        /// (a NaN would have had to propagate into <paramref name="value"/>).
        /// </summary>
        private static bool NoElementBeats<T, TOperator>(ref T xRef, int length, T value)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            for (int i = 0; i < length; i++)
            {
                T element = Unsafe.Add(ref xRef, i);
                if (T.IsNaN(element) || TOperator.Compare(element, value))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>See <see cref="IndexOfMinMaxCore{T, TOperator}(ReadOnlySpan{T})"/>.</summary>
        private static int IndexOfMinMaxBlocks128<T, TOperator>(ReadOnlySpan<T> x)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            Debug.Assert(Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported);
            Debug.Assert(x.Length >= Vector128<T>.Count);
            Debug.Assert(sizeof(T) is 1 or 2 or 4 or 8);

            int blockSize = BlockVectors * Vector128<T>.Count;
            int length = x.Length;
            ref T xRef = ref MemoryMarshal.GetReference(x);

            // Pass 1: reduce every block to its best element; the first block whose best beats the running result wins ties.
            T result = xRef;
            int resultBlock = -1;
            for (int i = 0; ; i += blockSize)
            {
                int blockLength = Math.Min(blockSize, length - i);
                T blockResult = BlockReduce128<T, TOperator>(ref Unsafe.Add(ref xRef, i), blockLength);

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    if (T.IsNaN(blockResult))
                    {
                        return i + IndexOfFirstNaN128(ref Unsafe.Add(ref xRef, i), blockLength);
                    }
                }

                Debug.Assert(NoElementBeats<T, TOperator>(ref Unsafe.Add(ref xRef, i), blockLength, blockResult),
                    "Reduce/Aggregate must return an element no other element of the block beats under Compare, and must propagate NaN.");

                if (resultBlock < 0 || TOperator.Compare(blockResult, result))
                {
                    result = blockResult;
                    resultBlock = i;
                }

                // Subtractive bound: testing i < length after i += blockSize would wrap for lengths above int.MaxValue - blockSize
                // and restart at a negative offset.
                if (i >= length - blockSize)
                {
                    break;
                }
            }

            Debug.Assert(resultBlock >= 0);

            // Pass 2: the first element of the winning block that the result does not beat, i.e. the first tied-best element.
            return resultBlock + IndexOfFirstNotBeaten128<T, TOperator>(ref Unsafe.Add(ref xRef, resultBlock), Math.Min(blockSize, length - resultBlock), result);
        }

        /// <summary>Reduces <paramref name="length"/> elements starting at <paramref name="xRef"/> to their best element (no bounds checks).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T BlockReduce128<T, TOperator>(ref T xRef, int length)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            Debug.Assert(length >= 1);

            nuint count = (nuint)Vector128<T>.Count;
            nuint end = (nuint)length;
            T result;
            nuint i;

            if (end >= 2 * count)
            {
                // Two independent accumulators so that consecutive reductions do not serialize on one register.
                Vector128<T> acc1 = Vector128.LoadUnsafe(ref xRef);
                Vector128<T> acc2 = Vector128.LoadUnsafe(ref xRef, count);
                nuint last = end - 2 * count;
                for (i = 2 * count; i <= last; i += 2 * count)
                {
                    acc1 = TOperator.Reduce(acc1, Vector128.LoadUnsafe(ref xRef, i));
                    acc2 = TOperator.Reduce(acc2, Vector128.LoadUnsafe(ref xRef, i + count));
                }

                if (i <= end - count)
                {
                    acc1 = TOperator.Reduce(acc1, Vector128.LoadUnsafe(ref xRef, i));
                    i += count;
                }

                result = TOperator.Aggregate(TOperator.Reduce(acc1, acc2));
            }
            else if (end >= count)
            {
                result = TOperator.Aggregate(Vector128.LoadUnsafe(ref xRef));
                i = count;
            }
            else
            {
                result = xRef;
                i = 1;
            }

            for (; i < end; i++)
            {
                result = TOperator.Reduce(result, Unsafe.Add(ref xRef, i));
            }

            return result;
        }

        /// <summary>Index of the first NaN among <paramref name="length"/> elements starting at <paramref name="xRef"/>; there must be one.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)] // cold: called at most once per search; keeps the caller within the inlining budget
        private static int IndexOfFirstNaN128<T>(ref T xRef, int length)
            where T : INumber<T>
        {
            int count = Vector128<T>.Count;
            int i = 0;

            for (; i <= length - count; i += count)
            {
                var bits = Vector128.IsNaN(Vector128.LoadUnsafe(ref xRef, (nuint)i)).ExtractMostSignificantBits();
                if (bits != 0) // in the integer domain: a NaN mask compared with a float operator takes NaN semantics
                {
                    return i + BitOperations.TrailingZeroCount(bits);
                }
            }

            for (; i < length; i++)
            {
                if (T.IsNaN(Unsafe.Add(ref xRef, i)))
                {
                    return i;
                }
            }

            Debug.Fail("A NaN was expected in the block.");
            return -1;
        }

        /// <summary>
        /// Index of the first element among <paramref name="length"/> elements starting at <paramref name="xRef"/> that
        /// <paramref name="value"/> does not beat under <typeparamref name="TOperator"/>; <paramref name="value"/> must be the
        /// block's best element, so this is the first element tied with it (equal, or an equal-magnitude tie the operator does not order).
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)] // called once per search; keeps the caller within the inlining budget
        private static int IndexOfFirstNotBeaten128<T, TOperator>(ref T xRef, int length, T value)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            int count = Vector128<T>.Count;
            Vector128<T> best = Vector128.Create(value);
            int i = 0;

            for (; i <= length - count; i += count)
            {
                var bits = (~TOperator.Compare(best, Vector128.LoadUnsafe(ref xRef, (nuint)i))).ExtractMostSignificantBits();
                if (bits != 0)
                {
                    return i + BitOperations.TrailingZeroCount(bits);
                }
            }

            for (; i < length; i++)
            {
                if (!TOperator.Compare(value, Unsafe.Add(ref xRef, i)))
                {
                    return i;
                }
            }

            Debug.Fail("The block's best element was expected in the block.");
            return -1;
        }

        /// <summary>See <see cref="IndexOfMinMaxCore{T, TOperator}(ReadOnlySpan{T})"/>.</summary>
        private static int IndexOfMinMaxBlocks256<T, TOperator>(ReadOnlySpan<T> x)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            Debug.Assert(Vector256.IsHardwareAccelerated && Vector256<T>.IsSupported);
            Debug.Assert(x.Length >= Vector256<T>.Count);
            Debug.Assert(sizeof(T) is 1 or 2 or 4 or 8);

            int blockSize = BlockVectors * Vector256<T>.Count;
            int length = x.Length;
            ref T xRef = ref MemoryMarshal.GetReference(x);

            // Pass 1: reduce every block to its best element; the first block whose best beats the running result wins ties.
            T result = xRef;
            int resultBlock = -1;
            for (int i = 0; ; i += blockSize)
            {
                int blockLength = Math.Min(blockSize, length - i);
                T blockResult = BlockReduce256<T, TOperator>(ref Unsafe.Add(ref xRef, i), blockLength);

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    if (T.IsNaN(blockResult))
                    {
                        return i + IndexOfFirstNaN256(ref Unsafe.Add(ref xRef, i), blockLength);
                    }
                }

                Debug.Assert(NoElementBeats<T, TOperator>(ref Unsafe.Add(ref xRef, i), blockLength, blockResult),
                    "Reduce/Aggregate must return an element no other element of the block beats under Compare, and must propagate NaN.");

                if (resultBlock < 0 || TOperator.Compare(blockResult, result))
                {
                    result = blockResult;
                    resultBlock = i;
                }

                // Subtractive bound: testing i < length after i += blockSize would wrap for lengths above int.MaxValue - blockSize
                // and restart at a negative offset.
                if (i >= length - blockSize)
                {
                    break;
                }
            }

            Debug.Assert(resultBlock >= 0);

            // Pass 2: the first element of the winning block that the result does not beat, i.e. the first tied-best element.
            return resultBlock + IndexOfFirstNotBeaten256<T, TOperator>(ref Unsafe.Add(ref xRef, resultBlock), Math.Min(blockSize, length - resultBlock), result);
        }

        /// <summary>Reduces <paramref name="length"/> elements starting at <paramref name="xRef"/> to their best element (no bounds checks).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T BlockReduce256<T, TOperator>(ref T xRef, int length)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            Debug.Assert(length >= 1);

            nuint count = (nuint)Vector256<T>.Count;
            nuint end = (nuint)length;
            T result;
            nuint i;

            if (end >= 2 * count)
            {
                // Two independent accumulators so that consecutive reductions do not serialize on one register.
                Vector256<T> acc1 = Vector256.LoadUnsafe(ref xRef);
                Vector256<T> acc2 = Vector256.LoadUnsafe(ref xRef, count);
                nuint last = end - 2 * count;
                for (i = 2 * count; i <= last; i += 2 * count)
                {
                    acc1 = TOperator.Reduce(acc1, Vector256.LoadUnsafe(ref xRef, i));
                    acc2 = TOperator.Reduce(acc2, Vector256.LoadUnsafe(ref xRef, i + count));
                }

                if (i <= end - count)
                {
                    acc1 = TOperator.Reduce(acc1, Vector256.LoadUnsafe(ref xRef, i));
                    i += count;
                }

                result = TOperator.Aggregate(TOperator.Reduce(acc1, acc2));
            }
            else if (end >= count)
            {
                result = TOperator.Aggregate(Vector256.LoadUnsafe(ref xRef));
                i = count;
            }
            else
            {
                result = xRef;
                i = 1;
            }

            for (; i < end; i++)
            {
                result = TOperator.Reduce(result, Unsafe.Add(ref xRef, i));
            }

            return result;
        }

        /// <summary>Index of the first NaN among <paramref name="length"/> elements starting at <paramref name="xRef"/>; there must be one.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)] // cold: called at most once per search; keeps the caller within the inlining budget
        private static int IndexOfFirstNaN256<T>(ref T xRef, int length)
            where T : INumber<T>
        {
            int count = Vector256<T>.Count;
            int i = 0;

            for (; i <= length - count; i += count)
            {
                var bits = Vector256.IsNaN(Vector256.LoadUnsafe(ref xRef, (nuint)i)).ExtractMostSignificantBits();
                if (bits != 0) // in the integer domain: a NaN mask compared with a float operator takes NaN semantics
                {
                    return i + BitOperations.TrailingZeroCount(bits);
                }
            }

            for (; i < length; i++)
            {
                if (T.IsNaN(Unsafe.Add(ref xRef, i)))
                {
                    return i;
                }
            }

            Debug.Fail("A NaN was expected in the block.");
            return -1;
        }

        /// <summary>
        /// Index of the first element among <paramref name="length"/> elements starting at <paramref name="xRef"/> that
        /// <paramref name="value"/> does not beat under <typeparamref name="TOperator"/>; <paramref name="value"/> must be the
        /// block's best element, so this is the first element tied with it (equal, or an equal-magnitude tie the operator does not order).
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)] // called once per search; keeps the caller within the inlining budget
        private static int IndexOfFirstNotBeaten256<T, TOperator>(ref T xRef, int length, T value)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            int count = Vector256<T>.Count;
            Vector256<T> best = Vector256.Create(value);
            int i = 0;

            for (; i <= length - count; i += count)
            {
                var bits = (~TOperator.Compare(best, Vector256.LoadUnsafe(ref xRef, (nuint)i))).ExtractMostSignificantBits();
                if (bits != 0)
                {
                    return i + BitOperations.TrailingZeroCount(bits);
                }
            }

            for (; i < length; i++)
            {
                if (!TOperator.Compare(value, Unsafe.Add(ref xRef, i)))
                {
                    return i;
                }
            }

            Debug.Fail("The block's best element was expected in the block.");
            return -1;
        }

        /// <summary>See <see cref="IndexOfMinMaxCore{T, TOperator}(ReadOnlySpan{T})"/>.</summary>
        private static int IndexOfMinMaxBlocks512<T, TOperator>(ReadOnlySpan<T> x)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            Debug.Assert(Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported);
            Debug.Assert(x.Length >= Vector512<T>.Count);
            Debug.Assert(sizeof(T) is 1 or 2 or 4 or 8);

            int blockSize = BlockVectors * Vector512<T>.Count;
            int length = x.Length;
            ref T xRef = ref MemoryMarshal.GetReference(x);

            // Pass 1: reduce every block to its best element; the first block whose best beats the running result wins ties.
            T result = xRef;
            int resultBlock = -1;
            for (int i = 0; ; i += blockSize)
            {
                int blockLength = Math.Min(blockSize, length - i);
                T blockResult = BlockReduce512<T, TOperator>(ref Unsafe.Add(ref xRef, i), blockLength);

                if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    if (T.IsNaN(blockResult))
                    {
                        return i + IndexOfFirstNaN512(ref Unsafe.Add(ref xRef, i), blockLength);
                    }
                }

                Debug.Assert(NoElementBeats<T, TOperator>(ref Unsafe.Add(ref xRef, i), blockLength, blockResult),
                    "Reduce/Aggregate must return an element no other element of the block beats under Compare, and must propagate NaN.");

                if (resultBlock < 0 || TOperator.Compare(blockResult, result))
                {
                    result = blockResult;
                    resultBlock = i;
                }

                // Subtractive bound: testing i < length after i += blockSize would wrap for lengths above int.MaxValue - blockSize
                // and restart at a negative offset.
                if (i >= length - blockSize)
                {
                    break;
                }
            }

            Debug.Assert(resultBlock >= 0);

            // Pass 2: the first element of the winning block that the result does not beat, i.e. the first tied-best element.
            return resultBlock + IndexOfFirstNotBeaten512<T, TOperator>(ref Unsafe.Add(ref xRef, resultBlock), Math.Min(blockSize, length - resultBlock), result);
        }

        /// <summary>Reduces <paramref name="length"/> elements starting at <paramref name="xRef"/> to their best element (no bounds checks).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T BlockReduce512<T, TOperator>(ref T xRef, int length)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            Debug.Assert(length >= 1);

            nuint count = (nuint)Vector512<T>.Count;
            nuint end = (nuint)length;
            T result;
            nuint i;

            if (end >= 2 * count)
            {
                // Two independent accumulators so that consecutive reductions do not serialize on one register.
                Vector512<T> acc1 = Vector512.LoadUnsafe(ref xRef);
                Vector512<T> acc2 = Vector512.LoadUnsafe(ref xRef, count);
                nuint last = end - 2 * count;
                for (i = 2 * count; i <= last; i += 2 * count)
                {
                    acc1 = TOperator.Reduce(acc1, Vector512.LoadUnsafe(ref xRef, i));
                    acc2 = TOperator.Reduce(acc2, Vector512.LoadUnsafe(ref xRef, i + count));
                }

                if (i <= end - count)
                {
                    acc1 = TOperator.Reduce(acc1, Vector512.LoadUnsafe(ref xRef, i));
                    i += count;
                }

                result = TOperator.Aggregate(TOperator.Reduce(acc1, acc2));
            }
            else if (end >= count)
            {
                result = TOperator.Aggregate(Vector512.LoadUnsafe(ref xRef));
                i = count;
            }
            else
            {
                result = xRef;
                i = 1;
            }

            for (; i < end; i++)
            {
                result = TOperator.Reduce(result, Unsafe.Add(ref xRef, i));
            }

            return result;
        }

        /// <summary>Index of the first NaN among <paramref name="length"/> elements starting at <paramref name="xRef"/>; there must be one.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)] // cold: called at most once per search; keeps the caller within the inlining budget
        private static int IndexOfFirstNaN512<T>(ref T xRef, int length)
            where T : INumber<T>
        {
            int count = Vector512<T>.Count;
            int i = 0;

            for (; i <= length - count; i += count)
            {
                var bits = Vector512.IsNaN(Vector512.LoadUnsafe(ref xRef, (nuint)i)).ExtractMostSignificantBits();
                if (bits != 0) // in the integer domain: a NaN mask compared with a float operator takes NaN semantics
                {
                    return i + BitOperations.TrailingZeroCount(bits);
                }
            }

            for (; i < length; i++)
            {
                if (T.IsNaN(Unsafe.Add(ref xRef, i)))
                {
                    return i;
                }
            }

            Debug.Fail("A NaN was expected in the block.");
            return -1;
        }

        /// <summary>
        /// Index of the first element among <paramref name="length"/> elements starting at <paramref name="xRef"/> that
        /// <paramref name="value"/> does not beat under <typeparamref name="TOperator"/>; <paramref name="value"/> must be the
        /// block's best element, so this is the first element tied with it (equal, or an equal-magnitude tie the operator does not order).
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)] // called once per search; keeps the caller within the inlining budget
        private static int IndexOfFirstNotBeaten512<T, TOperator>(ref T xRef, int length, T value)
            where T : INumber<T> where TOperator : struct, IIndexOfMinMaxOperator<T>
        {
            int count = Vector512<T>.Count;
            Vector512<T> best = Vector512.Create(value);
            int i = 0;

            for (; i <= length - count; i += count)
            {
                var bits = (~TOperator.Compare(best, Vector512.LoadUnsafe(ref xRef, (nuint)i))).ExtractMostSignificantBits();
                if (bits != 0)
                {
                    return i + BitOperations.TrailingZeroCount(bits);
                }
            }

            for (; i < length; i++)
            {
                if (!TOperator.Compare(value, Unsafe.Add(ref xRef, i)))
                {
                    return i;
                }
            }

            Debug.Fail("The block's best element was expected in the block.");
            return -1;
        }

    }
}
