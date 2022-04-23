// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Collections.Generic.SortUtils;

#pragma warning disable CA1822

namespace System.Collections.Generic
{
    #region ArraySortHelper for single arrays

    internal sealed partial class ArraySortHelper<T>
    {
        #region IArraySortHelper<T> Members

        public void Sort(Span<T> keys, IComparer<T>? comparer)
        {
            // Add a try block here to detect IComparers (or their
            // underlying IComparables, etc) that are bogus.
            try
            {
                comparer ??= Comparer<T>.Default;
                PatternDefeatingQuickSort(keys, comparer.Compare);
            }
            catch (IndexOutOfRangeException)
            {
                ThrowHelper.ThrowArgumentException_BadComparer(comparer);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
            }
        }

        public int BinarySearch(T[] array, int index, int length, T value, IComparer<T>? comparer)
        {
            try
            {
                comparer ??= Comparer<T>.Default;
                return InternalBinarySearch(array, index, length, value, comparer);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
                return 0;
            }
        }

        #endregion

        internal static void Sort(Span<T> keys, Comparison<T> comparer)
        {
            Debug.Assert(comparer != null, "Check the arguments in the caller!");

            // Add a try block here to detect bogus comparisons
            try
            {
                PatternDefeatingQuickSort(keys, comparer);
            }
            catch (IndexOutOfRangeException)
            {
                ThrowHelper.ThrowArgumentException_BadComparer(comparer);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
            }
        }

        internal static int InternalBinarySearch(T[] array, int index, int length, T value, IComparer<T> comparer)
        {
            Debug.Assert(array != null, "Check the arguments in the caller!");
            Debug.Assert(index >= 0 && length >= 0 && (array.Length - index >= length), "Check the arguments in the caller!");

            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order = comparer.Compare(array[i], value);

                if (order == 0) return i;
                if (order < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return ~lo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Compare(T left, T right, Comparison<T> comparer)
        {
            return comparer(left, right) < 0;
        }

        private static void HeapSort(Span<T> keys, Comparison<T> comparer)
        {
            if (keys.Length == 0)
            {
                return;
            }

            int n = keys.Length;
            for (int i = n >> 1; i >= 1; i--)
            {
                DownHeap(keys, i, n, comparer);
            }

            for (int i = n; i > 1; i--)
            {
                Swap(ref UnguardedAccess(keys, 0), ref UnguardedAccess(keys, i - 1));
                DownHeap(keys, 1, i - 1, comparer);
            }
        }

        private static void DownHeap(Span<T> keys, int i, int n, Comparison<T> comparer)
        {
            T d = UnguardedAccess(keys, i - 1);
            while (i <= n >> 1)
            {
                int child = 2 * i;
                if (child < n && Compare(UnguardedAccess(keys, child - 1), UnguardedAccess(keys, child), comparer))
                {
                    child++;
                }

                if (!Compare(d, UnguardedAccess(keys, child - 1), comparer))
                {
                    break;
                }

                UnguardedAccess(keys, i - 1) = UnguardedAccess(keys, child - 1);
                i = child;
            }

            UnguardedAccess(keys, i - 1) = d;
        }

        // Sorts [begin, end) using insertion sort with the given comparison function.
        private static void InsertionSort(Span<T> keys, int begin, int end, Comparison<T> comparer)
        {
            if (begin == end)
            {
                return;
            }

            for (var i = begin + 1; i < end; i++)
            {
                var sift = i;
                var sift1 = i - 1;

                // Compare first so we can avoid 2 moves for an element already positioned correctly.
                if (Compare(UnguardedAccess(keys, sift), UnguardedAccess(keys, sift1), comparer))
                {
                    var tmp = UnguardedAccess(keys, sift);
                    do
                    {
                        UnguardedAccess(keys, sift--) = UnguardedAccess(keys, sift1);
                    }
                    while (sift != begin && Compare(tmp, UnguardedAccess(keys, --sift1), comparer));

                    UnguardedAccess(keys, sift) = tmp;
                }
            }
        }

        // Sorts [begin, end) using insertion sort with the given comparison function. Assumes
        // keys[begin - 1] is an element smaller than or equal to any element in [begin, end).
        private static void UnguardedInsertionSort(Span<T> keys, int begin, int end, Comparison<T> comparer)
        {
            if (begin == end)
            {
                return;
            }

            for (var i = begin + 1; i < end; i++)
            {
                var sift = i;
                var sift1 = i - 1;

                // Compare first so we can avoid 2 moves for an element already positioned correctly.
                if (Compare(UnguardedAccess(keys, sift), UnguardedAccess(keys, sift1), comparer))
                {
                    var tmp = UnguardedAccess(keys, sift);
                    do
                    {
                        UnguardedAccess(keys, sift--) = UnguardedAccess(keys, sift1);
                    }
                    while (Compare(tmp, UnguardedAccess(keys, --sift1), comparer));

                    UnguardedAccess(keys, sift) = tmp;
                }
            }
        }

        // Attempts to use insertion sort on [begin, end). Will return false if more than
        // partial_insertion_sort_limit elements were moved, and abort sorting. Otherwise it will
        // successfully sort and return true.
        private static bool PartialInsertionSort(Span<T> keys, int begin, int end, Comparison<T> comparer)
        {
            if (begin == end)
            {
                return true;
            }

            var limit = 0;
            for (var i = begin + 1; i < end; i++)
            {
                var sift = i;
                var sift1 = i - 1;

                // Compare first so we can avoid 2 moves for an element already positioned correctly.
                if (Compare(UnguardedAccess(keys, sift), UnguardedAccess(keys, sift1), comparer))
                {
                    var tmp = UnguardedAccess(keys, sift);
                    do
                    {
                        UnguardedAccess(keys, sift--) = UnguardedAccess(keys, sift1);
                    }
                    while (sift != begin && Compare(tmp, UnguardedAccess(keys, --sift1), comparer));

                    UnguardedAccess(keys, sift) = tmp;
                    limit += i - sift;
                }

                if (limit > PartialInsertionSortLimit) return false;
            }

            return true;
        }

        // Partitions [begin, end) around pivot keys[begin] using comparison function comp. Elements equal
        // to the pivot are put in the right-hand partition. Returns the position of the pivot after
        // partitioning and whether the passed sequence already was correctly partitioned. Assumes the
        // pivot is a median of at least 3 elements and that [begin, end) is at least
        // insertion_sort_threshold long.
        private static (int Pivot, bool HasPartitioned) PartitionRight(Span<T> keys, int begin, int end, Comparison<T> comparer)
        {
            // Move pivot into local for speed.
            var pivot = UnguardedAccess(keys, begin);
            var first = begin;
            var last = end;

            // Find the first element greater than or equal than the pivot (the median of 3 guarantees
            // this exists).
            while (Compare(UnguardedAccess(keys, ++first), pivot, comparer)) { }

            // Find the first element strictly smaller than the pivot. We have to guard this search if
            // there was no element before *first.
            if (first - 1 == 0)
            {
                while (first < last && !Compare(UnguardedAccess(keys, --last), pivot, comparer)) { }
            }
            else
            {
                while (!Compare(UnguardedAccess(keys, --last), pivot, comparer)) { }
            }

            // If the first pair of elements that should be swapped to partition are the same element,
            // the passed in sequence already was correctly partitioned.
            bool hasPartitioned = first >= last;

            // Keep swapping pairs of elements that are on the wrong side of the pivot. Previously
            // swapped pairs guard the searches, which is why the first iteration is special-cased
            // above.
            while (first < last)
            {
                Swap(ref UnguardedAccess(keys, first), ref UnguardedAccess(keys, last));
                while (Compare(UnguardedAccess(keys, ++first), pivot, comparer)) { }
                while (!Compare(UnguardedAccess(keys, --last), pivot, comparer)) { }
            }

            // Put the pivot in the right place.
            var pivotPosition = first - 1;
            UnguardedAccess(keys, begin) = UnguardedAccess(keys, pivotPosition);
            UnguardedAccess(keys, pivotPosition) = pivot;

            return (pivotPosition, hasPartitioned);
        }

        // Similar function to the one above, except elements equal to the pivot are put to the left of
        // the pivot and it doesn't check or return if the passed sequence already was partitioned.
        // Since this is rarely used (the many equal case), and in that case pdqsort already has O(n)
        // performance, no block quicksort is applied here for simplicity.
        private static int PartitionLeft(Span<T> keys, int begin, int end, Comparison<T> comparer)
        {
            var pivot = UnguardedAccess(keys, begin);
            var first = begin;
            var last = end;

            while (Compare(pivot, UnguardedAccess(keys, --last), comparer)) { }
            if (last + 1 == end)
            {
                while (first < last && !Compare(pivot, UnguardedAccess(keys, ++first), comparer)) { }
            }
            else
            {
                while (!Compare(pivot, UnguardedAccess(keys, ++first), comparer)) { }
            }

            while (first < last)
            {
                Swap(ref UnguardedAccess(keys, first), ref UnguardedAccess(keys, last));
                while (Compare(pivot, UnguardedAccess(keys, --last), comparer)) { }
                while (!Compare(pivot, UnguardedAccess(keys, ++first), comparer)) { }
            }

            var pivotPosition = last;
            UnguardedAccess(keys, begin) = UnguardedAccess(keys, pivotPosition);
            UnguardedAccess(keys, pivotPosition) = pivot;

            return pivotPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(ref T a, ref T b)
        {
            var t = a;
            a = b;
            b = t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Sort2(ref T a, ref T b, Comparison<T> comparer)
        {
            if (Compare(b, a, comparer))
            {
                Swap(ref a, ref b);
            }
        }

        // Sorts the elements a, b and c using comparison function comparer.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Sort3(ref T a, ref T b, ref T c, Comparison<T> comparer)
        {
            Sort2(ref a, ref b, comparer);
            Sort2(ref b, ref c, comparer);
            Sort2(ref a, ref b, comparer);
        }

        private static void PdqSort(Span<T> keys, int begin, int end, Comparison<T> comparer, int badAllowed, bool leftmost = true)
        {
            Debug.Assert(begin >= 0);
            Debug.Assert(begin <= end);
            Debug.Assert(end <= keys.Length);
            Debug.Assert(comparer != null);
            Debug.Assert(badAllowed > 0);

            while (true)
            {
                var size = end - begin;

                // Insertion sort is faster for small arrays.
                if (size < InsertionSortThreshold)
                {
                    if (leftmost)
                    {
                        InsertionSort(keys, begin, end, comparer);
                    }
                    else
                    {
                        UnguardedInsertionSort(keys, begin, end, comparer);
                    }
                    return;
                }

                // Choose pivot as median of 3 or pseudomedian of 9.
                var mid = size / 2;
                if (size > NintherThreshold)
                {
                    Sort3(ref UnguardedAccess(keys, begin), ref UnguardedAccess(keys, begin + mid), ref UnguardedAccess(keys, end - 1), comparer);
                    Sort3(ref UnguardedAccess(keys, begin + 1), ref UnguardedAccess(keys, begin + mid - 1), ref UnguardedAccess(keys, end - 2), comparer);
                    Sort3(ref UnguardedAccess(keys, begin + 2), ref UnguardedAccess(keys, begin + mid + 1), ref UnguardedAccess(keys, end - 3), comparer);
                    Sort3(ref UnguardedAccess(keys, begin + mid - 1), ref UnguardedAccess(keys, begin + mid), ref UnguardedAccess(keys, begin + mid + 1), comparer);
                    Swap(ref UnguardedAccess(keys, begin), ref UnguardedAccess(keys, begin + mid));
                }
                else
                {
                    Sort3(ref UnguardedAccess(keys, begin + mid), ref UnguardedAccess(keys, begin), ref UnguardedAccess(keys, end - 1), comparer);
                }

                // If keys[begin - 1] is the end of the right partition of a previous partition operation
                // there is no element in [begin, end) that is smaller than keys[begin - 1]. Then if our
                // pivot compares equal to keys[begin - 1] we change strategy, putting equal elements in
                // the left partition, greater elements in the right partition. We do not have to
                // recurse on the left partition, since it's sorted (all equal).
                if (!leftmost && !Compare(UnguardedAccess(keys, begin - 1), UnguardedAccess(keys, begin), comparer))
                {
                    begin = PartitionLeft(keys, begin, end, comparer) + 1;
                    continue;
                }

                var (pivot, hasPartitioned) = PartitionRight(keys, begin, end, comparer);

                // Check for a highly unbalanced partition.
                var leftSize = pivot - begin;
                var rightSize = end - (pivot + 1);
                var highlyUnbalanced = leftSize < size / 8 || rightSize < size / 8;

                // If we got a highly unbalanced partition we shuffle elements to break many patterns.
                if (highlyUnbalanced)
                {
                    // If we had too many bad partitions, switch to heapsort to guarantee O(n log n).
                    if (--badAllowed == 0)
                    {
                        HeapSort(UnguardedSlice(keys, begin, end), comparer);
                        return;
                    }

                    if (leftSize >= InsertionSortThreshold)
                    {
                        Swap(ref UnguardedAccess(keys, begin), ref UnguardedAccess(keys, begin + leftSize / 4));
                        Swap(ref UnguardedAccess(keys, pivot - 1), ref UnguardedAccess(keys, pivot - leftSize / 4));

                        if (leftSize > NintherThreshold)
                        {
                            Swap(ref UnguardedAccess(keys, begin + 1), ref UnguardedAccess(keys, begin + leftSize / 4 + 1));
                            Swap(ref UnguardedAccess(keys, begin + 2), ref UnguardedAccess(keys, begin + leftSize / 4 + 2));
                            Swap(ref UnguardedAccess(keys, pivot - 2), ref UnguardedAccess(keys, pivot - (leftSize / 4 + 1)));
                            Swap(ref UnguardedAccess(keys, pivot - 3), ref UnguardedAccess(keys, pivot - (leftSize / 4 + 2)));
                        }
                    }

                    if (rightSize >= InsertionSortThreshold)
                    {
                        Swap(ref UnguardedAccess(keys, pivot + 1), ref UnguardedAccess(keys, pivot + 1 + rightSize / 4));
                        Swap(ref UnguardedAccess(keys, end - 1), ref UnguardedAccess(keys, end - rightSize / 4));

                        if (rightSize > NintherThreshold)
                        {
                            Swap(ref UnguardedAccess(keys, pivot + 2), ref UnguardedAccess(keys, pivot + 2 + rightSize / 4));
                            Swap(ref UnguardedAccess(keys, pivot + 3), ref UnguardedAccess(keys, pivot + 3 + rightSize / 4));
                            Swap(ref UnguardedAccess(keys, end - 2), ref UnguardedAccess(keys, end - (1 + rightSize / 4)));
                            Swap(ref UnguardedAccess(keys, end - 3), ref UnguardedAccess(keys, end - (2 + rightSize / 4)));
                        }
                    }
                }
                else
                {
                    // If we were decently balanced and we tried to sort an already partitioned
                    // sequence try to use insertion sort.
                    if (hasPartitioned &&
                        PartialInsertionSort(keys, begin, pivot, comparer) &&
                        PartialInsertionSort(keys, pivot + 1, end, comparer))
                    {
                        return;
                    }
                }

                // Sort the left partition first using recursion and do tail recursion elimination for
                // the right-hand partition.
                PdqSort(keys, begin, pivot, comparer, badAllowed, leftmost);
                begin = pivot + 1;
                leftmost = false;
            }
        }

        internal static void PatternDefeatingQuickSort(Span<T> keys, Comparison<T> comparer)
        {
            Debug.Assert(comparer != null);

            PdqSort(keys, 0, keys.Length, comparer, BitOperations.Log2((uint)keys.Length));
        }
    }

    internal sealed partial class GenericArraySortHelper<T>
        where T : IComparable<T>
    {
        // Do not add a constructor to this class because ArraySortHelper<T>.CreateSortHelper will not execute it

        #region IArraySortHelper<T> Members

        public void Sort(Span<T> keys, IComparer<T>? comparer)
        {
            try
            {
                if (comparer == null || comparer == Comparer<T>.Default)
                {
                    if (keys.Length > 1)
                    {
                        // For floating-point, do a pre-pass to move all NaNs to the beginning
                        // so that we can do an optimized comparison as part of the actual sort
                        // on the remainder of the values.
                        if (typeof(T) == typeof(double) ||
                            typeof(T) == typeof(float) ||
                            typeof(T) == typeof(Half))
                        {
                            int nanLeft = MoveNansToFront(keys, default(Span<byte>));
                            if (nanLeft == keys.Length)
                            {
                                return;
                            }
                            keys = keys.Slice(nanLeft);
                        }

                        PatternDefeatingQuickSort(keys);
                    }
                }
                else
                {
                    ArraySortHelper<T>.PatternDefeatingQuickSort(keys, comparer.Compare);
                }
            }
            catch (IndexOutOfRangeException)
            {
                ThrowHelper.ThrowArgumentException_BadComparer(comparer);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
            }
        }

        public int BinarySearch(T[] array, int index, int length, T value, IComparer<T>? comparer)
        {
            Debug.Assert(array != null, "Check the arguments in the caller!");
            Debug.Assert(index >= 0 && length >= 0 && (array.Length - index >= length), "Check the arguments in the caller!");

            try
            {
                if (comparer == null || comparer == Comparer<T>.Default)
                {
                    return BinarySearch(array, index, length, value);
                }
                else
                {
                    return ArraySortHelper<T>.InternalBinarySearch(array, index, length, value, comparer);
                }
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
                return 0;
            }
        }

        #endregion

        // This function is called when the user doesn't specify any comparer.
        // Since T is constrained here, we can call IComparable<T>.CompareTo here.
        // We can avoid boxing for value type and casting for reference types.
        private static int BinarySearch(T[] array, int index, int length, T value)
        {
            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order;
                if (array[i] == null)
                {
                    order = (value == null) ? 0 : -1;
                }
                else
                {
                    order = array[i].CompareTo(value);
                }

                if (order == 0)
                {
                    return i;
                }

                if (order < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return ~lo;
        }

        // - These methods exist for use in sorting, where the additional operations present in
        //   the CompareTo methods that would otherwise be used on these primitives add non-trivial overhead,
        //   in particular for floating point where the CompareTo methods need to factor in NaNs.
        // - The floating-point comparisons here assume no NaNs, which is valid only because the sorting routines
        //   themselves special-case NaN with a pre-pass that ensures none are present in the values being sorted
        //   by moving them all to the front first and then sorting the rest.
        // - The `? true : false` is to work-around poor codegen: https://github.com/dotnet/runtime/issues/37904#issuecomment-644180265.
        // - These are duplicated here rather than being on a helper type due to current limitations around generic inlining.

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // compiles to a single comparison or method call
        private static bool LessThan(T left, T right)
        {
            if (typeof(T) == typeof(byte)) return (byte)(object)left < (byte)(object)right ? true : false;
            if (typeof(T) == typeof(sbyte)) return (sbyte)(object)left < (sbyte)(object)right ? true : false;
            if (typeof(T) == typeof(ushort)) return (ushort)(object)left < (ushort)(object)right ? true : false;
            if (typeof(T) == typeof(short)) return (short)(object)left < (short)(object)right ? true : false;
            if (typeof(T) == typeof(uint)) return (uint)(object)left < (uint)(object)right ? true : false;
            if (typeof(T) == typeof(int)) return (int)(object)left < (int)(object)right ? true : false;
            if (typeof(T) == typeof(ulong)) return (ulong)(object)left < (ulong)(object)right ? true : false;
            if (typeof(T) == typeof(long)) return (long)(object)left < (long)(object)right ? true : false;
            if (typeof(T) == typeof(nuint)) return (nuint)(object)left < (nuint)(object)right ? true : false;
            if (typeof(T) == typeof(nint)) return (nint)(object)left < (nint)(object)right ? true : false;
            if (typeof(T) == typeof(float)) return (float)(object)left < (float)(object)right ? true : false;
            if (typeof(T) == typeof(double)) return (double)(object)left < (double)(object)right ? true : false;
            if (typeof(T) == typeof(Half)) return (Half)(object)left < (Half)(object)right ? true : false;
            return left.CompareTo(right) < 0 ? true : false;
        }

        private static void HeapSort(Span<T> keys)
        {
            if (keys.Length == 0)
            {
                return;
            }

            int n = keys.Length;
            for (int i = n >> 1; i >= 1; i--)
            {
                DownHeap(keys, i, n);
            }

            for (int i = n; i > 1; i--)
            {
                Swap(ref UnguardedAccess(keys, 0), ref UnguardedAccess(keys, i - 1));
                DownHeap(keys, 1, i - 1);
            }
        }

        private static void DownHeap(Span<T> keys, int i, int n)
        {
            T d = UnguardedAccess(keys, i - 1);
            while (i <= n >> 1)
            {
                int child = 2 * i;
                if (child < n && LessThan(UnguardedAccess(keys, child - 1), UnguardedAccess(keys, child)))
                {
                    child++;
                }

                if (!LessThan(d, UnguardedAccess(keys, child - 1)))
                {
                    break;
                }

                UnguardedAccess(keys, i - 1) = UnguardedAccess(keys, child - 1);
                i = child;
            }

            UnguardedAccess(keys, i - 1) = d;
        }

        // Sorts [begin, end) using insertion sort with the given comparison function.
        private static void InsertionSort(Span<T> keys, int begin, int end)
        {
            if (begin == end)
            {
                return;
            }

            for (var i = begin + 1; i < end; i++)
            {
                var sift = i;
                var sift1 = i - 1;

                // Compare first so we can avoid 2 moves for an element already positioned correctly.
                if (LessThan(UnguardedAccess(keys, sift), UnguardedAccess(keys, sift1)))
                {
                    var tmp = UnguardedAccess(keys, sift);
                    do
                    {
                        UnguardedAccess(keys, sift--) = UnguardedAccess(keys, sift1);
                    }
                    while (sift != begin && LessThan(tmp, UnguardedAccess(keys, --sift1)));

                    UnguardedAccess(keys, sift) = tmp;
                }
            }
        }

        // Sorts [begin, end) using insertion sort with the given comparison function. Assumes
        // keys[begin - 1] is an element smaller than or equal to any element in [begin, end).
        private static void UnguardedInsertionSort(Span<T> keys, int begin, int end)
        {
            if (begin == end)
            {
                return;
            }

            for (var i = begin + 1; i < end; i++)
            {
                var sift = i;
                var sift1 = i - 1;

                // Compare first so we can avoid 2 moves for an element already positioned correctly.
                if (LessThan(UnguardedAccess(keys, sift), UnguardedAccess(keys, sift1)))
                {
                    var tmp = UnguardedAccess(keys, sift);
                    do
                    {
                        UnguardedAccess(keys, sift--) = UnguardedAccess(keys, sift1);
                    }
                    while (LessThan(tmp, UnguardedAccess(keys, --sift1)));

                    UnguardedAccess(keys, sift) = tmp;
                }
            }
        }

        // Attempts to use insertion sort on [begin, end). Will return false if more than
        // partial_insertion_sort_limit elements were moved, and abort sorting. Otherwise it will
        // successfully sort and return true.
        private static bool PartialInsertionSort(Span<T> keys, int begin, int end)
        {
            if (begin == end)
            {
                return true;
            }

            var limit = 0;
            for (var i = begin + 1; i < end; i++)
            {
                var sift = i;
                var sift1 = i - 1;

                // Compare first so we can avoid 2 moves for an element already positioned correctly.
                if (LessThan(UnguardedAccess(keys, sift), UnguardedAccess(keys, sift1)))
                {
                    var tmp = UnguardedAccess(keys, sift);
                    do
                    {
                        UnguardedAccess(keys, sift--) = UnguardedAccess(keys, sift1);
                    }
                    while (sift != begin && LessThan(tmp, UnguardedAccess(keys, --sift1)));

                    UnguardedAccess(keys, sift) = tmp;
                    limit += i - sift;
                }

                if (limit > PartialInsertionSortLimit) return false;
            }

            return true;
        }

        // Partitions [begin, end) around pivot keys[begin] using comparison function comp. Elements equal
        // to the pivot are put in the right-hand partition. Returns the position of the pivot after
        // partitioning and whether the passed sequence already was correctly partitioned. Assumes the
        // pivot is a median of at least 3 elements and that [begin, end) is at least
        // insertion_sort_threshold long.
        private static (int Pivot, bool HasPartitioned) PartitionRight(Span<T> keys, int begin, int end)
        {
            // Move pivot into local for speed.
            var pivot = UnguardedAccess(keys, begin);
            var first = begin;
            var last = end;

            // Find the first element greater than or equal than the pivot (the median of 3 guarantees
            // this exists).
            while (LessThan(UnguardedAccess(keys, ++first), pivot)) { }

            // Find the first element strictly smaller than the pivot. We have to guard this search if
            // there was no element before *first.
            if (first - 1 == 0)
            {
                while (first < last && !LessThan(UnguardedAccess(keys, --last), pivot)) { }
            }
            else
            {
                while (!LessThan(UnguardedAccess(keys, --last), pivot)) { }
            }

            // If the first pair of elements that should be swapped to partition are the same element,
            // the passed in sequence already was correctly partitioned.
            bool hasPartitioned = first >= last;

            // Keep swapping pairs of elements that are on the wrong side of the pivot. Previously
            // swapped pairs guard the searches, which is why the first iteration is special-cased
            // above.
            while (first < last)
            {
                Swap(ref UnguardedAccess(keys, first), ref UnguardedAccess(keys, last));
                while (LessThan(UnguardedAccess(keys, ++first), pivot)) { }
                while (!LessThan(UnguardedAccess(keys, --last), pivot)) { }
            }

            // Put the pivot in the right place.
            var pivotPosition = first - 1;
            UnguardedAccess(keys, begin) = UnguardedAccess(keys, pivotPosition);
            UnguardedAccess(keys, pivotPosition) = pivot;

            return (pivotPosition, hasPartitioned);
        }

        // Similar function to the one above, except elements equal to the pivot are put to the left of
        // the pivot and it doesn't check or return if the passed sequence already was partitioned.
        // Since this is rarely used (the many equal case), and in that case pdqsort already has O(n)
        // performance, no block quicksort is applied here for simplicity.
        private static int PartitionLeft(Span<T> keys, int begin, int end)
        {
            var pivot = UnguardedAccess(keys, begin);
            var first = begin;
            var last = end;

            while (LessThan(pivot, UnguardedAccess(keys, --last))) { }
            if (last + 1 == end)
            {
                while (first < last && !LessThan(pivot, UnguardedAccess(keys, ++first))) { }
            }
            else
            {
                while (!LessThan(pivot, UnguardedAccess(keys, ++first))) { }
            }

            while (first < last)
            {
                Swap(ref UnguardedAccess(keys, first), ref UnguardedAccess(keys, last));
                while (LessThan(pivot, UnguardedAccess(keys, --last))) { }
                while (!LessThan(pivot, UnguardedAccess(keys, ++first))) { }
            }

            var pivotPosition = last;
            UnguardedAccess(keys, begin) = UnguardedAccess(keys, pivotPosition);
            UnguardedAccess(keys, pivotPosition) = pivot;

            return pivotPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(ref T a, ref T b)
        {
            var t = a;
            a = b;
            b = t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Sort2(ref T a, ref T b)
        {
            if (LessThan(b, a))
            {
                Swap(ref a, ref b);
            }
        }

        // Sorts the elements a, b and c using comparison function comparer.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Sort3(ref T a, ref T b, ref T c)
        {
            Sort2(ref a, ref b);
            Sort2(ref b, ref c);
            Sort2(ref a, ref b);
        }

        private static void PdqSort(Span<T> keys, int begin, int end, int badAllowed, bool leftmost = true)
        {
            Debug.Assert(begin >= 0);
            Debug.Assert(begin <= end);
            Debug.Assert(end <= keys.Length);
            Debug.Assert(badAllowed > 0);

            while (true)
            {
                var size = end - begin;

                // Insertion sort is faster for small arrays.
                if (size < InsertionSortThreshold)
                {
                    if (leftmost)
                    {
                        InsertionSort(keys, begin, end);
                    }
                    else
                    {
                        UnguardedInsertionSort(keys, begin, end);
                    }
                    return;
                }

                // Choose pivot as median of 3 or pseudomedian of 9.
                var mid = size / 2;
                if (size > NintherThreshold)
                {
                    Sort3(ref UnguardedAccess(keys, begin), ref UnguardedAccess(keys, begin + mid), ref UnguardedAccess(keys, end - 1));
                    Sort3(ref UnguardedAccess(keys, begin + 1), ref UnguardedAccess(keys, begin + mid - 1), ref UnguardedAccess(keys, end - 2));
                    Sort3(ref UnguardedAccess(keys, begin + 2), ref UnguardedAccess(keys, begin + mid + 1), ref UnguardedAccess(keys, end - 3));
                    Sort3(ref UnguardedAccess(keys, begin + mid - 1), ref UnguardedAccess(keys, begin + mid), ref UnguardedAccess(keys, begin + mid + 1));
                    Swap(ref UnguardedAccess(keys, begin), ref UnguardedAccess(keys, begin + mid));
                }
                else
                {
                    Sort3(ref UnguardedAccess(keys, begin + mid), ref UnguardedAccess(keys, begin), ref UnguardedAccess(keys, end - 1));
                }

                // If keys[begin - 1] is the end of the right partition of a previous partition operation
                // there is no element in [begin, end) that is smaller than keys[begin - 1]. Then if our
                // pivot compares equal to keys[begin - 1] we change strategy, putting equal elements in
                // the left partition, greater elements in the right partition. We do not have to
                // recurse on the left partition, since it's sorted (all equal).
                if (!leftmost && !LessThan(UnguardedAccess(keys, begin - 1), UnguardedAccess(keys, begin)))
                {
                    begin = PartitionLeft(keys, begin, end) + 1;
                    continue;
                }

                var (pivot, hasPartitioned) = PartitionRight(keys, begin, end);

                // Check for a highly unbalanced partition.
                var leftSize = pivot - begin;
                var rightSize = end - (pivot + 1);
                var highlyUnbalanced = leftSize < size / 8 || rightSize < size / 8;

                // If we got a highly unbalanced partition we shuffle elements to break many patterns.
                if (highlyUnbalanced)
                {
                    // If we had too many bad partitions, switch to heapsort to guarantee O(n log n).
                    if (--badAllowed == 0)
                    {
                        HeapSort(UnguardedSlice(keys, begin, end));
                        return;
                    }

                    if (leftSize >= InsertionSortThreshold)
                    {
                        Swap(ref UnguardedAccess(keys, begin), ref UnguardedAccess(keys, begin + leftSize / 4));
                        Swap(ref UnguardedAccess(keys, pivot - 1), ref UnguardedAccess(keys, pivot - leftSize / 4));

                        if (leftSize > NintherThreshold)
                        {
                            Swap(ref UnguardedAccess(keys, begin + 1), ref UnguardedAccess(keys, begin + leftSize / 4 + 1));
                            Swap(ref UnguardedAccess(keys, begin + 2), ref UnguardedAccess(keys, begin + leftSize / 4 + 2));
                            Swap(ref UnguardedAccess(keys, pivot - 2), ref UnguardedAccess(keys, pivot - (leftSize / 4 + 1)));
                            Swap(ref UnguardedAccess(keys, pivot - 3), ref UnguardedAccess(keys, pivot - (leftSize / 4 + 2)));
                        }
                    }

                    if (rightSize >= InsertionSortThreshold)
                    {
                        Swap(ref UnguardedAccess(keys, pivot + 1), ref UnguardedAccess(keys, pivot + 1 + rightSize / 4));
                        Swap(ref UnguardedAccess(keys, end - 1), ref UnguardedAccess(keys, end - rightSize / 4));

                        if (rightSize > NintherThreshold)
                        {
                            Swap(ref UnguardedAccess(keys, pivot + 2), ref UnguardedAccess(keys, pivot + 2 + rightSize / 4));
                            Swap(ref UnguardedAccess(keys, pivot + 3), ref UnguardedAccess(keys, pivot + 3 + rightSize / 4));
                            Swap(ref UnguardedAccess(keys, end - 2), ref UnguardedAccess(keys, end - (1 + rightSize / 4)));
                            Swap(ref UnguardedAccess(keys, end - 3), ref UnguardedAccess(keys, end - (2 + rightSize / 4)));
                        }
                    }
                }
                else
                {
                    // If we were decently balanced and we tried to sort an already partitioned
                    // sequence try to use insertion sort.
                    if (hasPartitioned &&
                        PartialInsertionSort(keys, begin, pivot) &&
                        PartialInsertionSort(keys, pivot + 1, end))
                    {
                        return;
                    }
                }

                // Sort the left partition first using recursion and do tail recursion elimination for
                // the right-hand partition.
                PdqSort(keys, begin, pivot, badAllowed, leftmost);
                begin = pivot + 1;
                leftmost = false;
            }
        }

        internal static void PatternDefeatingQuickSort(Span<T> keys)
        {
            PdqSort(keys, 0, keys.Length, BitOperations.Log2((uint)keys.Length));
        }
    }

    #endregion

    #region ArraySortHelper for paired key and value arrays

    internal sealed partial class ArraySortHelper<TKey, TValue>
    {
        public void Sort(Span<TKey> keys, Span<TValue> values, IComparer<TKey>? comparer)
        {
            // Add a try block here to detect IComparers (or their
            // underlying IComparables, etc) that are bogus.
            try
            {
                PatternDefeatingQuickSort(keys, values, comparer ?? Comparer<TKey>.Default);
            }
            catch (IndexOutOfRangeException)
            {
                ThrowHelper.ThrowArgumentException_BadComparer(comparer);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Compare(TKey left, TKey right, IComparer<TKey> comparer)
        {
            return comparer.Compare(left, right) < 0;
        }

        private static void HeapSort(Span<TKey> keys, Span<TValue> values, IComparer<TKey> comparer)
        {
            if (keys.Length == 0)
            {
                return;
            }

            int n = keys.Length;
            for (int i = n >> 1; i >= 1; i--)
            {
                DownHeap(keys, values, i, n, comparer);
            }

            for (int i = n; i > 1; i--)
            {
                Swap(ref UnguardedAccess(keys, 0), ref UnguardedAccess(keys, i - 1), ref UnguardedAccess(values, 0), ref UnguardedAccess(values, i - 1));
                DownHeap(keys, values, 1, i - 1, comparer);
            }
        }

        private static void DownHeap(Span<TKey> keys, Span<TValue> values, int i, int n, IComparer<TKey> comparer)
        {
            TKey d = UnguardedAccess(keys, i - 1);
            TValue dValue = UnguardedAccess(values, i - 1);
            while (i <= n >> 1)
            {
                int child = 2 * i;
                if (child < n && Compare(UnguardedAccess(keys, child - 1), UnguardedAccess(keys, child), comparer))
                {
                    child++;
                }

                if (!Compare(d, UnguardedAccess(keys, child - 1), comparer))
                {
                    break;
                }

                UnguardedAccess(keys, i - 1) = UnguardedAccess(keys, child - 1);
                UnguardedAccess(values, i - 1) = UnguardedAccess(values, child - 1);
                i = child;
            }

            UnguardedAccess(keys, i - 1) = d;
            UnguardedAccess(values, i - 1) = dValue;
        }

        // Sorts [begin, end) using insertion sort with the given comparison function.
        private static void InsertionSort(Span<TKey> keys, Span<TValue> values, int begin, int end, IComparer<TKey> comparer)
        {
            if (begin == end)
            {
                return;
            }

            for (var i = begin + 1; i < end; i++)
            {
                var sift = i;
                var sift1 = i - 1;

                // Compare first so we can avoid 2 moves for an element already positioned correctly.
                if (Compare(UnguardedAccess(keys, sift), UnguardedAccess(keys, sift1), comparer))
                {
                    var tmp = UnguardedAccess(keys, sift);
                    var tmpValue = UnguardedAccess(values, sift);
                    do
                    {
                        UnguardedAccess(keys, sift) = UnguardedAccess(keys, sift1);
                        UnguardedAccess(values, sift) = UnguardedAccess(values, sift1);
                        sift--;
                    }
                    while (sift != begin && Compare(tmp, UnguardedAccess(keys, --sift1), comparer));

                    UnguardedAccess(keys, sift) = tmp;
                    UnguardedAccess(values, sift) = tmpValue;
                }
            }
        }

        // Sorts [begin, end) using insertion sort with the given comparison function. Assumes
        // keys[begin - 1] is an element smaller than or equal to any element in [begin, end).
        private static void UnguardedInsertionSort(Span<TKey> keys, Span<TValue> values, int begin, int end, IComparer<TKey> comparer)
        {
            if (begin == end)
            {
                return;
            }

            for (var i = begin + 1; i < end; i++)
            {
                var sift = i;
                var sift1 = i - 1;

                // Compare first so we can avoid 2 moves for an element already positioned correctly.
                if (Compare(UnguardedAccess(keys, sift), UnguardedAccess(keys, sift1), comparer))
                {
                    var tmp = UnguardedAccess(keys, sift);
                    var tmpValue = UnguardedAccess(values, sift);
                    do
                    {
                        UnguardedAccess(keys, sift) = UnguardedAccess(keys, sift1);
                        UnguardedAccess(values, sift) = UnguardedAccess(values, sift1);
                        sift--;
                    }
                    while (Compare(tmp, UnguardedAccess(keys, --sift1), comparer));

                    UnguardedAccess(keys, sift) = tmp;
                    UnguardedAccess(values, sift) = tmpValue;
                }
            }
        }

        // Attempts to use insertion sort on [begin, end). Will return false if more than
        // partial_insertion_sort_limit elements were moved, and abort sorting. Otherwise it will
        // successfully sort and return true.
        private static bool PartialInsertionSort(Span<TKey> keys, Span<TValue> values, int begin, int end, IComparer<TKey> comparer)
        {
            if (begin == end)
            {
                return true;
            }

            var limit = 0;
            for (var i = begin + 1; i < end; i++)
            {
                var sift = i;
                var sift1 = i - 1;

                // Compare first so we can avoid 2 moves for an element already positioned correctly.
                if (Compare(UnguardedAccess(keys, sift), UnguardedAccess(keys, sift1), comparer))
                {
                    var tmp = UnguardedAccess(keys, sift);
                    var tmpValue = UnguardedAccess(values, sift);
                    do
                    {
                        UnguardedAccess(keys, sift) = UnguardedAccess(keys, sift1);
                        UnguardedAccess(values, sift) = UnguardedAccess(values, sift1);
                        sift--;
                    }
                    while (sift != begin && Compare(tmp, UnguardedAccess(keys, --sift1), comparer));

                    UnguardedAccess(keys, sift) = tmp;
                    UnguardedAccess(values, sift) = tmpValue;
                    limit += i - sift;
                }

                if (limit > PartialInsertionSortLimit) return false;
            }

            return true;
        }

        // Partitions [begin, end) around pivot keys[begin] using comparison function comp. Elements equal
        // to the pivot are put in the right-hand partition. Returns the position of the pivot after
        // partitioning and whether the passed sequence already was correctly partitioned. Assumes the
        // pivot is a median of at least 3 elements and that [begin, end) is at least
        // insertion_sort_threshold long.
        private static (int Pivot, bool HasPartitioned) PartitionRight(Span<TKey> keys, Span<TValue> values, int begin, int end, IComparer<TKey> comparer)
        {
            // Move pivot into local for speed.
            var pivot = UnguardedAccess(keys, begin);
            var pivotValue = UnguardedAccess(values, begin);
            var first = begin;
            var last = end;

            // Find the first element greater than or equal than the pivot (the median of 3 guarantees
            // this exists).
            while (Compare(UnguardedAccess(keys, ++first), pivot, comparer)) { }

            // Find the first element strictly smaller than the pivot. We have to guard this search if
            // there was no element before *first.
            if (first - 1 == 0)
            {
                while (first < last && !Compare(UnguardedAccess(keys, --last), pivot, comparer)) { }
            }
            else
            {
                while (!Compare(UnguardedAccess(keys, --last), pivot, comparer)) { }
            }

            // If the first pair of elements that should be swapped to partition are the same element,
            // the passed in sequence already was correctly partitioned.
            bool hasPartitioned = first >= last;

            // Keep swapping pairs of elements that are on the wrong side of the pivot. Previously
            // swapped pairs guard the searches, which is why the first iteration is special-cased
            // above.
            while (first < last)
            {
                Swap(ref UnguardedAccess(keys, first), ref UnguardedAccess(keys, last), ref UnguardedAccess(values, first), ref UnguardedAccess(values, last));
                while (Compare(UnguardedAccess(keys, ++first), pivot, comparer)) { }
                while (!Compare(UnguardedAccess(keys, --last), pivot, comparer)) { }
            }

            // Put the pivot in the right place.
            var pivotPosition = first - 1;
            UnguardedAccess(keys, begin) = UnguardedAccess(keys, pivotPosition);
            UnguardedAccess(values, begin) = UnguardedAccess(values, pivotPosition);
            UnguardedAccess(keys, pivotPosition) = pivot;
            UnguardedAccess(values, pivotPosition) = pivotValue;

            return (pivotPosition, hasPartitioned);
        }

        // Similar function to the one above, except elements equal to the pivot are put to the left of
        // the pivot and it doesn't check or return if the passed sequence already was partitioned.
        // Since this is rarely used (the many equal case), and in that case pdqsort already has O(n)
        // performance, no block quicksort is applied here for simplicity.
        private static int PartitionLeft(Span<TKey> keys, Span<TValue> values, int begin, int end, IComparer<TKey> comparer)
        {
            var pivot = UnguardedAccess(keys, begin);
            var pivotValue = UnguardedAccess(values, begin);
            var first = begin;
            var last = end;

            while (Compare(pivot, UnguardedAccess(keys, --last), comparer)) { }
            if (last + 1 == end)
            {
                while (first < last && !Compare(pivot, UnguardedAccess(keys, ++first), comparer)) { }
            }
            else
            {
                while (!Compare(pivot, UnguardedAccess(keys, ++first), comparer)) { }
            }

            while (first < last)
            {
                Swap(ref UnguardedAccess(keys, first), ref UnguardedAccess(keys, last), ref UnguardedAccess(values, first), ref UnguardedAccess(values, last));
                while (Compare(pivot, UnguardedAccess(keys, --last), comparer)) { }
                while (!Compare(pivot, UnguardedAccess(keys, ++first), comparer)) { }
            }

            var pivotPosition = last;
            UnguardedAccess(keys, begin) = UnguardedAccess(keys, pivotPosition);
            UnguardedAccess(values, begin) = UnguardedAccess(values, pivotPosition);
            UnguardedAccess(keys, pivotPosition) = pivot;
            UnguardedAccess(values, pivotPosition) = pivotValue;

            return pivotPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(ref TKey a, ref TKey b, ref TValue x, ref TValue y)
        {
            var t1 = a;
            a = b;
            b = t1;

            var t2 = x;
            x = y;
            y = t2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Sort2(ref TKey a, ref TKey b, ref TValue x, ref TValue y, IComparer<TKey> comparer)
        {
            if (Compare(b, a, comparer))
            {
                Swap(ref a, ref b, ref x, ref y);
            }
        }

        // Sorts the elements a, b and c using comparison function comparer.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Sort3(ref TKey a, ref TKey b, ref TKey c, ref TValue x, ref TValue y, ref TValue z, IComparer<TKey> comparer)
        {
            Sort2(ref a, ref b, ref x, ref y, comparer);
            Sort2(ref b, ref c, ref y, ref z, comparer);
            Sort2(ref a, ref b, ref x, ref y, comparer);
        }

        private static void PdqSort(Span<TKey> keys, Span<TValue> values, int begin, int end, IComparer<TKey> comparer, int badAllowed, bool leftmost = true)
        {
            Debug.Assert(keys.Length == values.Length);
            Debug.Assert(begin >= 0);
            Debug.Assert(begin <= end);
            Debug.Assert(end <= keys.Length);
            Debug.Assert(comparer != null);
            Debug.Assert(badAllowed > 0);

            while (true)
            {
                var size = end - begin;

                // Insertion sort is faster for small arrays.
                if (size < InsertionSortThreshold)
                {
                    if (leftmost)
                    {
                        InsertionSort(keys, values, begin, end, comparer);
                    }
                    else
                    {
                        UnguardedInsertionSort(keys, values, begin, end, comparer);
                    }
                    return;
                }

                // Choose pivot as median of 3 or pseudomedian of 9.
                var mid = size / 2;
                if (size > NintherThreshold)
                {
                    Sort3(ref UnguardedAccess(keys, begin), ref UnguardedAccess(keys, begin + mid), ref UnguardedAccess(keys, end - 1),
                        ref UnguardedAccess(values, begin), ref UnguardedAccess(values, begin + mid), ref UnguardedAccess(values, end - 1), comparer);
                    Sort3(ref UnguardedAccess(keys, begin + 1), ref UnguardedAccess(keys, begin + mid - 1), ref UnguardedAccess(keys, end - 2),
                        ref UnguardedAccess(values, begin + 1), ref UnguardedAccess(values, begin + mid - 1), ref UnguardedAccess(values, end - 2), comparer);
                    Sort3(ref UnguardedAccess(keys, begin + 2), ref UnguardedAccess(keys, begin + mid + 1), ref UnguardedAccess(keys, end - 3),
                        ref UnguardedAccess(values, begin + 2), ref UnguardedAccess(values, begin + mid + 1), ref UnguardedAccess(values, end - 3), comparer);
                    Sort3(ref UnguardedAccess(keys, begin + mid - 1), ref UnguardedAccess(keys, begin + mid), ref UnguardedAccess(keys, begin + mid + 1),
                        ref UnguardedAccess(values, begin + mid - 1), ref UnguardedAccess(values, begin + mid), ref UnguardedAccess(values, begin + mid + 1), comparer);
                    Swap(ref UnguardedAccess(keys, begin), ref UnguardedAccess(keys, begin + mid), ref UnguardedAccess(values, begin), ref UnguardedAccess(values, begin + mid));
                }
                else
                {
                    Sort3(ref UnguardedAccess(keys, begin + mid), ref UnguardedAccess(keys, begin), ref UnguardedAccess(keys, end - 1),
                        ref UnguardedAccess(values, begin + mid), ref UnguardedAccess(values, begin), ref UnguardedAccess(values, end - 1), comparer);
                }

                // If keys[begin - 1] is the end of the right partition of a previous partition operation
                // there is no element in [begin, end) that is smaller than keys[begin - 1]. Then if our
                // pivot compares equal to keys[begin - 1] we change strategy, putting equal elements in
                // the left partition, greater elements in the right partition. We do not have to
                // recurse on the left partition, since it's sorted (all equal).
                if (!leftmost && !Compare(UnguardedAccess(keys, begin - 1), UnguardedAccess(keys, begin), comparer))
                {
                    begin = PartitionLeft(keys, values, begin, end, comparer) + 1;
                    continue;
                }

                var (pivot, hasPartitioned) = PartitionRight(keys, values, begin, end, comparer);

                // Check for a highly unbalanced partition.
                var leftSize = pivot - begin;
                var rightSize = end - (pivot + 1);
                var highlyUnbalanced = leftSize < size / 8 || rightSize < size / 8;

                // If we got a highly unbalanced partition we shuffle elements to break many patterns.
                if (highlyUnbalanced)
                {
                    // If we had too many bad partitions, switch to heapsort to guarantee O(n log n).
                    if (--badAllowed == 0)
                    {
                        HeapSort(UnguardedSlice(keys, begin, end), UnguardedSlice(values, begin, end), comparer);
                        return;
                    }

                    if (leftSize >= InsertionSortThreshold)
                    {
                        Swap(ref UnguardedAccess(keys, begin), ref UnguardedAccess(keys, begin + leftSize / 4), ref UnguardedAccess(values, begin), ref UnguardedAccess(values, begin + leftSize / 4));
                        Swap(ref UnguardedAccess(keys, pivot - 1), ref UnguardedAccess(keys, pivot - leftSize / 4), ref UnguardedAccess(values, pivot - 1), ref UnguardedAccess(values, pivot - leftSize / 4));

                        if (leftSize > NintherThreshold)
                        {
                            Swap(ref UnguardedAccess(keys, begin + 1), ref UnguardedAccess(keys, begin + leftSize / 4 + 1), ref UnguardedAccess(values, begin + 1), ref UnguardedAccess(values, begin + leftSize / 4 + 1));
                            Swap(ref UnguardedAccess(keys, begin + 2), ref UnguardedAccess(keys, begin + leftSize / 4 + 2), ref UnguardedAccess(values, begin + 2), ref UnguardedAccess(values, begin + leftSize / 4 + 2));
                            Swap(ref UnguardedAccess(keys, pivot - 2), ref UnguardedAccess(keys, pivot - (leftSize / 4 + 1)), ref UnguardedAccess(values, pivot - 2), ref UnguardedAccess(values, pivot - (leftSize / 4 + 1)));
                            Swap(ref UnguardedAccess(keys, pivot - 3), ref UnguardedAccess(keys, pivot - (leftSize / 4 + 2)), ref UnguardedAccess(values, pivot - 3), ref UnguardedAccess(values, pivot - (leftSize / 4 + 2)));
                        }
                    }

                    if (rightSize >= InsertionSortThreshold)
                    {
                        Swap(ref UnguardedAccess(keys, pivot + 1), ref UnguardedAccess(keys, pivot + 1 + rightSize / 4), ref UnguardedAccess(values, pivot + 1), ref UnguardedAccess(values, pivot + 1 + rightSize / 4));
                        Swap(ref UnguardedAccess(keys, end - 1), ref UnguardedAccess(keys, end - rightSize / 4), ref UnguardedAccess(values, end - 1), ref UnguardedAccess(values, end - rightSize / 4));

                        if (rightSize > NintherThreshold)
                        {
                            Swap(ref UnguardedAccess(keys, pivot + 2), ref UnguardedAccess(keys, pivot + 2 + rightSize / 4), ref UnguardedAccess(values, pivot + 2), ref UnguardedAccess(values, pivot + 2 + rightSize / 4));
                            Swap(ref UnguardedAccess(keys, pivot + 3), ref UnguardedAccess(keys, pivot + 3 + rightSize / 4), ref UnguardedAccess(values, pivot + 3), ref UnguardedAccess(values, pivot + 3 + rightSize / 4));
                            Swap(ref UnguardedAccess(keys, end - 2), ref UnguardedAccess(keys, end - (1 + rightSize / 4)), ref UnguardedAccess(values, end - 2), ref UnguardedAccess(values, end - (1 + rightSize / 4)));
                            Swap(ref UnguardedAccess(keys, end - 3), ref UnguardedAccess(keys, end - (2 + rightSize / 4)), ref UnguardedAccess(values, end - 3), ref UnguardedAccess(values, end - (2 + rightSize / 4)));
                        }
                    }
                }
                else
                {
                    // If we were decently balanced and we tried to sort an already partitioned
                    // sequence try to use insertion sort.
                    if (hasPartitioned &&
                        PartialInsertionSort(keys, values, begin, pivot, comparer) &&
                        PartialInsertionSort(keys, values, pivot + 1, end, comparer))
                    {
                        return;
                    }
                }

                // Sort the left partition first using recursion and do tail recursion elimination for
                // the right-hand partition.
                PdqSort(keys, values, begin, pivot, comparer, badAllowed, leftmost);
                begin = pivot + 1;
                leftmost = false;
            }
        }

        internal static void PatternDefeatingQuickSort(Span<TKey> keys, Span<TValue> values, IComparer<TKey> comparer)
        {
            Debug.Assert(keys.Length == values.Length);
            Debug.Assert(comparer != null);

            PdqSort(keys, values, 0, keys.Length, comparer, BitOperations.Log2((uint)keys.Length));
        }
    }

    internal sealed partial class GenericArraySortHelper<TKey, TValue>
        where TKey : IComparable<TKey>
    {
        public void Sort(Span<TKey> keys, Span<TValue> values, IComparer<TKey>? comparer)
        {
            // Add a try block here to detect IComparers (or their
            // underlying IComparables, etc) that are bogus.
            try
            {
                if (comparer == null || comparer == Comparer<TKey>.Default)
                {
                    if (keys.Length > 1)
                    {
                        // For floating-point, do a pre-pass to move all NaNs to the beginning
                        // so that we can do an optimized comparison as part of the actual sort
                        // on the remainder of the values.
                        if (typeof(TKey) == typeof(double) ||
                            typeof(TKey) == typeof(float) ||
                            typeof(TKey) == typeof(Half))
                        {
                            int nanLeft = MoveNansToFront(keys, values);
                            if (nanLeft == keys.Length)
                            {
                                return;
                            }
                            keys = keys.Slice(nanLeft);
                            values = values.Slice(nanLeft);
                        }

                        PatternDefeatingQuickSort(keys, values);
                    }
                }
                else
                {
                    ArraySortHelper<TKey, TValue>.PatternDefeatingQuickSort(keys, values, comparer);
                }
            }
            catch (IndexOutOfRangeException)
            {
                ThrowHelper.ThrowArgumentException_BadComparer(comparer);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
            }
        }

        // - These methods exist for use in sorting, where the additional operations present in
        //   the CompareTo methods that would otherwise be used on these primitives add non-trivial overhead,
        //   in particular for floating point where the CompareTo methods need to factor in NaNs.
        // - The floating-point comparisons here assume no NaNs, which is valid only because the sorting routines
        //   themselves special-case NaN with a pre-pass that ensures none are present in the values being sorted
        //   by moving them all to the front first and then sorting the rest.
        // - The `? true : false` is to work-around poor codegen: https://github.com/dotnet/runtime/issues/37904#issuecomment-644180265.
        // - These are duplicated here rather than being on a helper type due to current limitations around generic inlining.

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // compiles to a single comparison or method call
        private static bool LessThan(TKey left, TKey right)
        {
            if (typeof(TKey) == typeof(byte)) return (byte)(object)left < (byte)(object)right ? true : false;
            if (typeof(TKey) == typeof(sbyte)) return (sbyte)(object)left < (sbyte)(object)right ? true : false;
            if (typeof(TKey) == typeof(ushort)) return (ushort)(object)left < (ushort)(object)right ? true : false;
            if (typeof(TKey) == typeof(short)) return (short)(object)left < (short)(object)right ? true : false;
            if (typeof(TKey) == typeof(uint)) return (uint)(object)left < (uint)(object)right ? true : false;
            if (typeof(TKey) == typeof(int)) return (int)(object)left < (int)(object)right ? true : false;
            if (typeof(TKey) == typeof(ulong)) return (ulong)(object)left < (ulong)(object)right ? true : false;
            if (typeof(TKey) == typeof(long)) return (long)(object)left < (long)(object)right ? true : false;
            if (typeof(TKey) == typeof(nuint)) return (nuint)(object)left < (nuint)(object)right ? true : false;
            if (typeof(TKey) == typeof(nint)) return (nint)(object)left < (nint)(object)right ? true : false;
            if (typeof(TKey) == typeof(float)) return (float)(object)left < (float)(object)right ? true : false;
            if (typeof(TKey) == typeof(double)) return (double)(object)left < (double)(object)right ? true : false;
            if (typeof(TKey) == typeof(Half)) return (Half)(object)left < (Half)(object)right ? true : false;
            return left.CompareTo(right) < 0 ? true : false;
        }

        private static void HeapSort(Span<TKey> keys, Span<TValue> values)
        {
            if (keys.Length == 0)
            {
                return;
            }

            int n = keys.Length;
            for (int i = n >> 1; i >= 1; i--)
            {
                DownHeap(keys, values, i, n);
            }

            for (int i = n; i > 1; i--)
            {
                Swap(ref UnguardedAccess(keys, 0), ref UnguardedAccess(keys, i - 1), ref UnguardedAccess(values, 0), ref UnguardedAccess(values, i - 1));
                DownHeap(keys, values, 1, i - 1);
            }
        }

        private static void DownHeap(Span<TKey> keys, Span<TValue> values, int i, int n)
        {
            TKey d = UnguardedAccess(keys, i - 1);
            TValue dValue = UnguardedAccess(values, i - 1);
            while (i <= n >> 1)
            {
                int child = 2 * i;
                if (child < n && LessThan(UnguardedAccess(keys, child - 1), UnguardedAccess(keys, child)))
                {
                    child++;
                }

                if (!LessThan(d, UnguardedAccess(keys, child - 1)))
                {
                    break;
                }

                UnguardedAccess(keys, i - 1) = UnguardedAccess(keys, child - 1);
                UnguardedAccess(values, i - 1) = UnguardedAccess(values, child - 1);
                i = child;
            }

            UnguardedAccess(keys, i - 1) = d;
            UnguardedAccess(values, i - 1) = dValue;
        }

        // Sorts [begin, end) using insertion sort with the given comparison function.
        private static void InsertionSort(Span<TKey> keys, Span<TValue> values, int begin, int end)
        {
            if (begin == end)
            {
                return;
            }

            for (var i = begin + 1; i < end; i++)
            {
                var sift = i;
                var sift1 = i - 1;

                // Compare first so we can avoid 2 moves for an element already positioned correctly.
                if (LessThan(UnguardedAccess(keys, sift), UnguardedAccess(keys, sift1)))
                {
                    var tmp = UnguardedAccess(keys, sift);
                    var tmpValue = UnguardedAccess(values, sift);
                    do
                    {
                        UnguardedAccess(keys, sift) = UnguardedAccess(keys, sift1);
                        UnguardedAccess(values, sift) = UnguardedAccess(values, sift1);
                        sift--;
                    }
                    while (sift != begin && LessThan(tmp, UnguardedAccess(keys, --sift1)));

                    UnguardedAccess(keys, sift) = tmp;
                    UnguardedAccess(values, sift) = tmpValue;
                }
            }
        }

        // Sorts [begin, end) using insertion sort with the given comparison function. Assumes
        // keys[begin - 1] is an element smaller than or equal to any element in [begin, end).
        private static void UnguardedInsertionSort(Span<TKey> keys, Span<TValue> values, int begin, int end)
        {
            if (begin == end)
            {
                return;
            }

            for (var i = begin + 1; i < end; i++)
            {
                var sift = i;
                var sift1 = i - 1;

                // Compare first so we can avoid 2 moves for an element already positioned correctly.
                if (LessThan(UnguardedAccess(keys, sift), UnguardedAccess(keys, sift1)))
                {
                    var tmp = UnguardedAccess(keys, sift);
                    var tmpValue = UnguardedAccess(values, sift);
                    do
                    {
                        UnguardedAccess(keys, sift) = UnguardedAccess(keys, sift1);
                        UnguardedAccess(values, sift) = UnguardedAccess(values, sift1);
                        sift--;
                    }
                    while (LessThan(tmp, UnguardedAccess(keys, --sift1)));

                    UnguardedAccess(keys, sift) = tmp;
                    UnguardedAccess(values, sift) = tmpValue;
                }
            }
        }

        // Attempts to use insertion sort on [begin, end). Will return false if more than
        // partial_insertion_sort_limit elements were moved, and abort sorting. Otherwise it will
        // successfully sort and return true.
        private static bool PartialInsertionSort(Span<TKey> keys, Span<TValue> values, int begin, int end)
        {
            if (begin == end)
            {
                return true;
            }

            var limit = 0;
            for (var i = begin + 1; i < end; i++)
            {
                var sift = i;
                var sift1 = i - 1;

                // Compare first so we can avoid 2 moves for an element already positioned correctly.
                if (LessThan(UnguardedAccess(keys, sift), UnguardedAccess(keys, sift1)))
                {
                    var tmp = UnguardedAccess(keys, sift);
                    var tmpValue = UnguardedAccess(values, sift);
                    do
                    {
                        UnguardedAccess(keys, sift) = UnguardedAccess(keys, sift1);
                        UnguardedAccess(values, sift) = UnguardedAccess(values, sift1);
                        sift--;
                    }
                    while (sift != begin && LessThan(tmp, UnguardedAccess(keys, --sift1)));

                    UnguardedAccess(keys, sift) = tmp;
                    UnguardedAccess(values, sift) = tmpValue;
                    limit += i - sift;
                }

                if (limit > PartialInsertionSortLimit) return false;
            }

            return true;
        }

        // Partitions [begin, end) around pivot keys[begin] using comparison function comp. Elements equal
        // to the pivot are put in the right-hand partition. Returns the position of the pivot after
        // partitioning and whether the passed sequence already was correctly partitioned. Assumes the
        // pivot is a median of at least 3 elements and that [begin, end) is at least
        // insertion_sort_threshold long.
        private static (int Pivot, bool HasPartitioned) PartitionRight(Span<TKey> keys, Span<TValue> values, int begin, int end)
        {
            // Move pivot into local for speed.
            var pivot = UnguardedAccess(keys, begin);
            var pivotValue = UnguardedAccess(values, begin);
            var first = begin;
            var last = end;

            // Find the first element greater than or equal than the pivot (the median of 3 guarantees
            // this exists).
            while (LessThan(UnguardedAccess(keys, ++first), pivot)) { }

            // Find the first element strictly smaller than the pivot. We have to guard this search if
            // there was no element before *first.
            if (first - 1 == 0)
            {
                while (first < last && !LessThan(UnguardedAccess(keys, --last), pivot)) { }
            }
            else
            {
                while (!LessThan(UnguardedAccess(keys, --last), pivot)) { }
            }

            // If the first pair of elements that should be swapped to partition are the same element,
            // the passed in sequence already was correctly partitioned.
            bool hasPartitioned = first >= last;

            // Keep swapping pairs of elements that are on the wrong side of the pivot. Previously
            // swapped pairs guard the searches, which is why the first iteration is special-cased
            // above.
            while (first < last)
            {
                Swap(ref UnguardedAccess(keys, first), ref UnguardedAccess(keys, last), ref UnguardedAccess(values, first), ref UnguardedAccess(values, last));
                while (LessThan(UnguardedAccess(keys, ++first), pivot)) { }
                while (!LessThan(UnguardedAccess(keys, --last), pivot)) { }
            }

            // Put the pivot in the right place.
            var pivotPosition = first - 1;
            UnguardedAccess(keys, begin) = UnguardedAccess(keys, pivotPosition);
            UnguardedAccess(values, begin) = UnguardedAccess(values, pivotPosition);
            UnguardedAccess(keys, pivotPosition) = pivot;
            UnguardedAccess(values, pivotPosition) = pivotValue;

            return (pivotPosition, hasPartitioned);
        }

        // Similar function to the one above, except elements equal to the pivot are put to the left of
        // the pivot and it doesn't check or return if the passed sequence already was partitioned.
        // Since this is rarely used (the many equal case), and in that case pdqsort already has O(n)
        // performance, no block quicksort is applied here for simplicity.
        private static int PartitionLeft(Span<TKey> keys, Span<TValue> values, int begin, int end)
        {
            var pivot = UnguardedAccess(keys, begin);
            var pivotValue = UnguardedAccess(values, begin);
            var first = begin;
            var last = end;

            while (LessThan(pivot, UnguardedAccess(keys, --last))) { }
            if (last + 1 == end)
            {
                while (first < last && !LessThan(pivot, UnguardedAccess(keys, ++first))) { }
            }
            else
            {
                while (!LessThan(pivot, UnguardedAccess(keys, ++first))) { }
            }

            while (first < last)
            {
                Swap(ref UnguardedAccess(keys, first), ref UnguardedAccess(keys, last), ref UnguardedAccess(values, first), ref UnguardedAccess(values, last));
                while (LessThan(pivot, UnguardedAccess(keys, --last))) { }
                while (!LessThan(pivot, UnguardedAccess(keys, ++first))) { }
            }

            var pivotPosition = last;
            UnguardedAccess(keys, begin) = UnguardedAccess(keys, pivotPosition);
            UnguardedAccess(values, begin) = UnguardedAccess(values, pivotPosition);
            UnguardedAccess(keys, pivotPosition) = pivot;
            UnguardedAccess(values, pivotPosition) = pivotValue;

            return pivotPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(ref TKey a, ref TKey b, ref TValue x, ref TValue y)
        {
            var t1 = a;
            a = b;
            b = t1;

            var t2 = x;
            x = y;
            y = t2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Sort2(ref TKey a, ref TKey b, ref TValue x, ref TValue y)
        {
            if (LessThan(b, a))
            {
                Swap(ref a, ref b, ref x, ref y);
            }
        }

        // Sorts the elements a, b and c using comparison function comparer.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Sort3(ref TKey a, ref TKey b, ref TKey c, ref TValue x, ref TValue y, ref TValue z)
        {
            Sort2(ref a, ref b, ref x, ref y);
            Sort2(ref b, ref c, ref y, ref z);
            Sort2(ref a, ref b, ref x, ref y);
        }

        private static void PdqSort(Span<TKey> keys, Span<TValue> values, int begin, int end, int badAllowed, bool leftmost = true)
        {
            Debug.Assert(keys.Length == values.Length);
            Debug.Assert(begin >= 0);
            Debug.Assert(begin <= end);
            Debug.Assert(end <= keys.Length);
            Debug.Assert(badAllowed > 0);

            while (true)
            {
                var size = end - begin;

                // Insertion sort is faster for small arrays.
                if (size < InsertionSortThreshold)
                {
                    if (leftmost)
                    {
                        InsertionSort(keys, values, begin, end);
                    }
                    else
                    {
                        UnguardedInsertionSort(keys, values, begin, end);
                    }
                    return;
                }

                // Choose pivot as median of 3 or pseudomedian of 9.
                var mid = size / 2;
                if (size > NintherThreshold)
                {
                    Sort3(ref UnguardedAccess(keys, begin), ref UnguardedAccess(keys, begin + mid), ref UnguardedAccess(keys, end - 1),
                        ref UnguardedAccess(values, begin), ref UnguardedAccess(values, begin + mid), ref UnguardedAccess(values, end - 1));
                    Sort3(ref UnguardedAccess(keys, begin + 1), ref UnguardedAccess(keys, begin + mid - 1), ref UnguardedAccess(keys, end - 2),
                        ref UnguardedAccess(values, begin + 1), ref UnguardedAccess(values, begin + mid - 1), ref UnguardedAccess(values, end - 2));
                    Sort3(ref UnguardedAccess(keys, begin + 2), ref UnguardedAccess(keys, begin + mid + 1), ref UnguardedAccess(keys, end - 3),
                        ref UnguardedAccess(values, begin + 2), ref UnguardedAccess(values, begin + mid + 1), ref UnguardedAccess(values, end - 3));
                    Sort3(ref UnguardedAccess(keys, begin + mid - 1), ref UnguardedAccess(keys, begin + mid), ref UnguardedAccess(keys, begin + mid + 1),
                        ref UnguardedAccess(values, begin + mid - 1), ref UnguardedAccess(values, begin + mid), ref UnguardedAccess(values, begin + mid + 1));
                    Swap(ref UnguardedAccess(keys, begin), ref UnguardedAccess(keys, begin + mid), ref UnguardedAccess(values, begin), ref UnguardedAccess(values, begin + mid));
                }
                else
                {
                    Sort3(ref UnguardedAccess(keys, begin + mid), ref UnguardedAccess(keys, begin), ref UnguardedAccess(keys, end - 1),
                        ref UnguardedAccess(values, begin + mid), ref UnguardedAccess(values, begin), ref UnguardedAccess(values, end - 1));
                }

                // If keys[begin - 1] is the end of the right partition of a previous partition operation
                // there is no element in [begin, end) that is smaller than keys[begin - 1]. Then if our
                // pivot compares equal to keys[begin - 1] we change strategy, putting equal elements in
                // the left partition, greater elements in the right partition. We do not have to
                // recurse on the left partition, since it's sorted (all equal).
                if (!leftmost && !LessThan(UnguardedAccess(keys, begin - 1), UnguardedAccess(keys, begin)))
                {
                    begin = PartitionLeft(keys, values, begin, end) + 1;
                    continue;
                }

                var (pivot, hasPartitioned) = PartitionRight(keys, values, begin, end);

                // Check for a highly unbalanced partition.
                var leftSize = pivot - begin;
                var rightSize = end - (pivot + 1);
                var highlyUnbalanced = leftSize < size / 8 || rightSize < size / 8;

                // If we got a highly unbalanced partition we shuffle elements to break many patterns.
                if (highlyUnbalanced)
                {
                    // If we had too many bad partitions, switch to heapsort to guarantee O(n log n).
                    if (--badAllowed == 0)
                    {
                        HeapSort(UnguardedSlice(keys, begin, end), UnguardedSlice(values, begin, end));
                        return;
                    }

                    if (leftSize >= InsertionSortThreshold)
                    {
                        Swap(ref UnguardedAccess(keys, begin), ref UnguardedAccess(keys, begin + leftSize / 4), ref UnguardedAccess(values, begin), ref UnguardedAccess(values, begin + leftSize / 4));
                        Swap(ref UnguardedAccess(keys, pivot - 1), ref UnguardedAccess(keys, pivot - leftSize / 4), ref UnguardedAccess(values, pivot - 1), ref UnguardedAccess(values, pivot - leftSize / 4));

                        if (leftSize > NintherThreshold)
                        {
                            Swap(ref UnguardedAccess(keys, begin + 1), ref UnguardedAccess(keys, begin + leftSize / 4 + 1), ref UnguardedAccess(values, begin + 1), ref UnguardedAccess(values, begin + leftSize / 4 + 1));
                            Swap(ref UnguardedAccess(keys, begin + 2), ref UnguardedAccess(keys, begin + leftSize / 4 + 2), ref UnguardedAccess(values, begin + 2), ref UnguardedAccess(values, begin + leftSize / 4 + 2));
                            Swap(ref UnguardedAccess(keys, pivot - 2), ref UnguardedAccess(keys, pivot - (leftSize / 4 + 1)), ref UnguardedAccess(values, pivot - 2), ref UnguardedAccess(values, pivot - (leftSize / 4 + 1)));
                            Swap(ref UnguardedAccess(keys, pivot - 3), ref UnguardedAccess(keys, pivot - (leftSize / 4 + 2)), ref UnguardedAccess(values, pivot - 3), ref UnguardedAccess(values, pivot - (leftSize / 4 + 2)));
                        }
                    }

                    if (rightSize >= InsertionSortThreshold)
                    {
                        Swap(ref UnguardedAccess(keys, pivot + 1), ref UnguardedAccess(keys, pivot + 1 + rightSize / 4), ref UnguardedAccess(values, pivot + 1), ref UnguardedAccess(values, pivot + 1 + rightSize / 4));
                        Swap(ref UnguardedAccess(keys, end - 1), ref UnguardedAccess(keys, end - rightSize / 4), ref UnguardedAccess(values, end - 1), ref UnguardedAccess(values, end - rightSize / 4));

                        if (rightSize > NintherThreshold)
                        {
                            Swap(ref UnguardedAccess(keys, pivot + 2), ref UnguardedAccess(keys, pivot + 2 + rightSize / 4), ref UnguardedAccess(values, pivot + 2), ref UnguardedAccess(values, pivot + 2 + rightSize / 4));
                            Swap(ref UnguardedAccess(keys, pivot + 3), ref UnguardedAccess(keys, pivot + 3 + rightSize / 4), ref UnguardedAccess(values, pivot + 3), ref UnguardedAccess(values, pivot + 3 + rightSize / 4));
                            Swap(ref UnguardedAccess(keys, end - 2), ref UnguardedAccess(keys, end - (1 + rightSize / 4)), ref UnguardedAccess(values, end - 2), ref UnguardedAccess(values, end - (1 + rightSize / 4)));
                            Swap(ref UnguardedAccess(keys, end - 3), ref UnguardedAccess(keys, end - (2 + rightSize / 4)), ref UnguardedAccess(values, end - 3), ref UnguardedAccess(values, end - (2 + rightSize / 4)));
                        }
                    }
                }
                else
                {
                    // If we were decently balanced and we tried to sort an already partitioned
                    // sequence try to use insertion sort.
                    if (hasPartitioned &&
                        PartialInsertionSort(keys, values, begin, pivot) &&
                        PartialInsertionSort(keys, values, pivot + 1, end))
                    {
                        return;
                    }
                }

                // Sort the left partition first using recursion and do tail recursion elimination for
                // the right-hand partition.
                PdqSort(keys, values, begin, pivot, badAllowed, leftmost);
                begin = pivot + 1;
                leftmost = false;
            }
        }

        internal static void PatternDefeatingQuickSort(Span<TKey> keys, Span<TValue> values)
        {
            Debug.Assert(keys.Length == values.Length);

            PdqSort(keys, values, 0, keys.Length, BitOperations.Log2((uint)keys.Length));
        }
    }

    #endregion

    /// <summary>Helper methods for use in array/span sorting routines.</summary>
    internal static class SortUtils
    {
        // Partitions below this size are sorted using insertion sort.
        internal const int InsertionSortThreshold = 24;
        // Partitions above this size use Tukey's ninther to select the pivot.
        internal const int NintherThreshold = 128;
        // When we detect an already sorted partition, attempt an insertion sort that allows this
        // amount of element moves before giving up.
        internal const int PartialInsertionSortLimit = 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ref T UnguardedAccess<T>(Span<T> span, int index)
        {
            // return ref Unsafe.Add(ref MemoryMarshal.GetReference(span), index);
            // TODO: investigate where can we use the above line with confidence instead of this.
            return ref span[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Span<T> UnguardedSlice<T>(Span<T> span, int begin, int end)
        {
            return MemoryMarshal.CreateSpan(ref UnguardedAccess(span, begin), end - begin);
        }

        public static int MoveNansToFront<TKey, TValue>(Span<TKey> keys, Span<TValue> values) where TKey : notnull
        {
            Debug.Assert(typeof(TKey) == typeof(double) || typeof(TKey) == typeof(float));

            int left = 0;

            for (int i = 0; i < keys.Length; i++)
            {
                if ((typeof(TKey) == typeof(double) && double.IsNaN((double)(object)keys[i])) ||
                    (typeof(TKey) == typeof(float) && float.IsNaN((float)(object)keys[i])) ||
                    (typeof(TKey) == typeof(Half) && Half.IsNaN((Half)(object)keys[i])))
                {
                    TKey temp = keys[left];
                    keys[left] = keys[i];
                    keys[i] = temp;

                    if ((uint)i < (uint)values.Length) // check to see if we have values
                    {
                        TValue tempValue = values[left];
                        values[left] = values[i];
                        values[i] = tempValue;
                    }

                    left++;
                }
            }

            return left;
        }
    }
}
