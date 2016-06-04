// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
** 
**
**
** Purpose: class to sort arrays
**
** 
===========================================================*/
namespace System.Collections.Generic
{
    using System;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.Contracts;
    using System.Runtime.Versioning;
    
    #region ArraySortHelper for single arrays

    internal interface IArraySortHelper<TKey>
    {
        void Sort(TKey[] keys, int index, int length, IComparer<TKey> comparer);
        int BinarySearch(TKey[] keys, int index, int length, TKey value, IComparer<TKey> comparer);
    }

    internal static class IntrospectiveSortUtilities
    {
        // This is the threshold where Introspective sort switches to Insertion sort.
        // Imperically, 16 seems to speed up most cases without slowing down others, at least for integers.
        // Large value types may benefit from a smaller number.
        internal const int IntrosortSizeThreshold = 16;

        internal const int QuickSortDepthThreshold = 32;

        internal static int FloorLog2(int n)
        {
            int result = 0;
            while (n >= 1)
            {
                result++;
                n = n / 2;
            }
            return result;
        }

        internal static void ThrowOrIgnoreBadComparer(Object comparer) {
            // This is hit when an invarant of QuickSort is violated due to a bad IComparer implementation (for
            // example, imagine an IComparer that returns 0 when items are equal but -1 all other times).
            //
            // We could have thrown this exception on v4, but due to changes in v4.5 around how we partition arrays
            // there are different sets of input where we would throw this exception.  In order to reduce overall risk from
            // an app compat persective, we're changing to never throw on v4.  Instead, we'll return with a partially
            // sorted array.
            if(BinaryCompatibility.TargetsAtLeast_Desktop_V4_5) {
                throw new ArgumentException(Environment.GetResourceString("Arg_BogusIComparer", comparer));
            }
        }

    }

    [TypeDependencyAttribute("System.Collections.Generic.GenericArraySortHelper`1")]     
    internal class ArraySortHelper<T>  
        : IArraySortHelper<T>
    {
        static volatile IArraySortHelper<T> defaultArraySortHelper;
        
        public static IArraySortHelper<T> Default
        {
            get
            {
                IArraySortHelper<T> sorter = defaultArraySortHelper;
                if (sorter == null)
                    sorter = CreateArraySortHelper();

                return sorter;
            }
        }                
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        private static IArraySortHelper<T> CreateArraySortHelper()
        {
            if (typeof(IComparable<T>).IsAssignableFrom(typeof(T)))
            {
                defaultArraySortHelper = (IArraySortHelper<T>)RuntimeTypeHandle.Allocate(typeof(GenericArraySortHelper<string>).TypeHandle.Instantiate(new Type[] { typeof(T) }));
            }
            else
            {
                defaultArraySortHelper = new ArraySortHelper<T>();                        
            }
            return defaultArraySortHelper;
        }

        #region IArraySortHelper<T> Members

        public void Sort(T[] keys, int index, int length, IComparer<T> comparer)
        {
            Contract.Assert(keys != null, "Check the arguments in the caller!");
            Contract.Assert( index >= 0 && length >= 0 && (keys.Length - index >= length), "Check the arguments in the caller!");

            // Add a try block here to detect IComparers (or their
            // underlying IComparables, etc) that are bogus.
            try
            {
                if (comparer == null)
                {
                    comparer = Comparer<T>.Default;
                }

#if FEATURE_CORECLR
                // Since QuickSort and IntrospectiveSort produce different sorting sequence for equal keys the upgrade 
                // to IntrospectiveSort was quirked. However since the phone builds always shipped with the new sort aka 
                // IntrospectiveSort and we would want to continue using this sort moving forward CoreCLR always uses the new sort.

                IntrospectiveSort(keys, index, length, comparer);
#else
                if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                {
                    IntrospectiveSort(keys, index, length, comparer);
                }
                else
                {
                    DepthLimitedQuickSort(keys, index, length + index - 1, comparer, IntrospectiveSortUtilities.QuickSortDepthThreshold);
                }
#endif
            }
            catch (IndexOutOfRangeException)
            {
                IntrospectiveSortUtilities.ThrowOrIgnoreBadComparer(comparer);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
            }
        }

        public int BinarySearch(T[] array, int index, int length, T value, IComparer<T> comparer)
        {
            try
            {
                if (comparer == null)
                {
                    comparer = Comparer<T>.Default;
                }

                return InternalBinarySearch(array, index, length, value, comparer);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
            }
        }

        #endregion

        internal static int InternalBinarySearch(T[] array, int index, int length, T value, IComparer<T> comparer)
        {
            Contract.Requires(array != null, "Check the arguments in the caller!");
            Contract.Requires(index >= 0 && length >= 0 && (array.Length - index >= length), "Check the arguments in the caller!");

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

        private static void SwapIfGreater(T[] keys, IComparer<T> comparer, int a, int b)
        {
            if (a != b)
            {
                if (comparer.Compare(keys[a], keys[b]) > 0)
                {
                    T key = keys[a];
                    keys[a] = keys[b];
                    keys[b] = key;
                }
            }
        }

        private static void Swap(T[] a, int i, int j)
        {
            if(i != j)
            {
                T t = a[i];
                a[i] = a[j];
                a[j] = t;
            }
        }

        internal static void DepthLimitedQuickSort(T[] keys, int left, int right, IComparer<T> comparer, int depthLimit)
        {
            do
            {
                if (depthLimit == 0)
                {
                    Heapsort(keys, left, right, comparer);
                    return;
                }

                int i = left;
                int j = right;

                // pre-sort the low, middle (pivot), and high values in place.
                // this improves performance in the face of already sorted data, or 
                // data that is made up of multiple sorted runs appended together.
                int middle = i + ((j - i) >> 1);
                SwapIfGreater(keys, comparer, i, middle);  // swap the low with the mid point
                SwapIfGreater(keys, comparer, i, j);   // swap the low with the high
                SwapIfGreater(keys, comparer, middle, j); // swap the middle with the high

                T x = keys[middle];
                do
                {
                    while (comparer.Compare(keys[i], x) < 0) i++;
                    while (comparer.Compare(x, keys[j]) < 0) j--;
                    Contract.Assert(i >= left && j <= right, "(i>=left && j<=right)  Sort failed - Is your IComparer bogus?");
                    if (i > j) break;
                    if (i < j)
                    {
                        T key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;
                    }
                    i++;
                    j--;
                } while (i <= j);

                // The next iteration of the while loop is to "recursively" sort the larger half of the array and the
                // following calls recursively sort the smaller half.  So we subtract one from depthLimit here so
                // both sorts see the new value.
                depthLimit--;

                if (j - left <= right - i)
                {
                    if (left < j) DepthLimitedQuickSort(keys, left, j, comparer, depthLimit);
                    left = i;
                }
                else
                {
                    if (i < right) DepthLimitedQuickSort(keys, i, right, comparer, depthLimit);
                    right = j;
                }
            } while (left < right);
        }

        internal static void IntrospectiveSort(T[] keys, int left, int length, IComparer<T> comparer)
        {
            Contract.Requires(keys != null);
            Contract.Requires(comparer != null);
            Contract.Requires(left >= 0);
            Contract.Requires(length >= 0);
            Contract.Requires(length <= keys.Length);
            Contract.Requires(length + left <= keys.Length);

            if (length < 2)
                return;

            IntroSort(keys, left, length + left - 1, 2 * IntrospectiveSortUtilities.FloorLog2(keys.Length), comparer);
        }

        private static void IntroSort(T[] keys, int lo, int hi, int depthLimit, IComparer<T> comparer)
        {
            Contract.Requires(keys != null);
            Contract.Requires(comparer != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi < keys.Length);

            while (hi > lo)
            {
                int partitionSize = hi - lo + 1;
                if (partitionSize <= IntrospectiveSortUtilities.IntrosortSizeThreshold)
                {
                    if (partitionSize == 1)
                    {
                        return;
                    }
                    if (partitionSize == 2)
                    {
                        SwapIfGreater(keys, comparer, lo, hi);
                        return;
                    }
                    if (partitionSize == 3)
                    {
                        SwapIfGreater(keys, comparer, lo, hi-1);
                        SwapIfGreater(keys, comparer, lo, hi);
                        SwapIfGreater(keys, comparer, hi-1, hi);
                        return;
                    }

                    InsertionSort(keys, lo, hi, comparer);
                    return;
                }

                if (depthLimit == 0)
                {
                    Heapsort(keys, lo, hi, comparer);
                    return;
                }
                depthLimit--;

                int p = PickPivotAndPartition(keys, lo, hi, comparer);
                // Note we've already partitioned around the pivot and do not have to move the pivot again.
                IntroSort(keys, p + 1, hi, depthLimit, comparer);
                hi = p - 1;
            }
        }

        private static int PickPivotAndPartition(T[] keys, int lo, int hi, IComparer<T> comparer)
        {
            Contract.Requires(keys != null);
            Contract.Requires(comparer != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi > lo);
            Contract.Requires(hi < keys.Length);
            Contract.Ensures(Contract.Result<int>() >= lo && Contract.Result<int>() <= hi);

            // Compute median-of-three.  But also partition them, since we've done the comparison.
            int middle = lo + ((hi - lo) / 2);

            // Sort lo, mid and hi appropriately, then pick mid as the pivot.
            SwapIfGreater(keys, comparer, lo, middle);  // swap the low with the mid point
            SwapIfGreater(keys, comparer, lo, hi);   // swap the low with the high
            SwapIfGreater(keys, comparer, middle, hi); // swap the middle with the high

            T pivot = keys[middle];
            Swap(keys, middle, hi - 1);
            int left = lo, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

            while (left < right)
            {
                while (comparer.Compare(keys[++left], pivot) < 0) ;
                while (comparer.Compare(pivot, keys[--right]) < 0) ;

                if (left >= right)
                    break;

                Swap(keys, left, right);
            }

            // Put pivot in the right location.
            Swap(keys, left, (hi - 1));
            return left;
        }

        private static void Heapsort(T[] keys, int lo, int hi, IComparer<T> comparer)
        {
            Contract.Requires(keys != null);
            Contract.Requires(comparer != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi > lo);
            Contract.Requires(hi < keys.Length);

            int n = hi - lo + 1;
            for (int i = n / 2; i >= 1; i = i - 1)
            {
                DownHeap(keys, i, n, lo, comparer);
            }
            for (int i = n; i > 1; i = i - 1)
            {
                Swap(keys, lo, lo + i - 1);
                DownHeap(keys, 1, i - 1, lo, comparer);
            }
        }

        private static void DownHeap(T[] keys, int i, int n, int lo, IComparer<T> comparer)
        {
            Contract.Requires(keys != null);
            Contract.Requires(comparer != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(lo < keys.Length);

            T d = keys[lo + i - 1];
            int child;
            while (i <= n / 2)
            {
                child = 2 * i;
                if (child < n && comparer.Compare(keys[lo + child - 1], keys[lo + child]) < 0)
                {
                    child++;
                }
                if (!(comparer.Compare(d, keys[lo + child - 1]) < 0))
                    break;
                keys[lo + i - 1] = keys[lo + child - 1];
                i = child;
            }
            keys[lo + i - 1] = d;
        }

        private static void InsertionSort(T[] keys, int lo, int hi, IComparer<T> comparer)
        {
            Contract.Requires(keys != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi >= lo);
            Contract.Requires(hi <= keys.Length);

            int i, j;
            T t;
            for (i = lo; i < hi; i++)
            {
                j = i;
                t = keys[i + 1];
                while (j >= lo && comparer.Compare(t, keys[j]) < 0)
                {
                    keys[j + 1] = keys[j];
                    j--;
                }
                keys[j + 1] = t;
            }
        }
    }

    [Serializable()]
    internal class GenericArraySortHelper<T>
        : IArraySortHelper<T>
        where T : IComparable<T>
    {
        // Do not add a constructor to this class because ArraySortHelper<T>.CreateSortHelper will not execute it
        
        #region IArraySortHelper<T> Members

        public void Sort(T[] keys, int index, int length, IComparer<T> comparer)
        {
            Contract.Assert(keys != null, "Check the arguments in the caller!");
            Contract.Assert(index >= 0 && length >= 0 && (keys.Length - index >= length), "Check the arguments in the caller!");

            try
            {
                if (comparer == null || comparer == Comparer<T>.Default) {

#if FEATURE_CORECLR
                    // Since QuickSort and IntrospectiveSort produce different sorting sequence for equal keys the upgrade 
                    // to IntrospectiveSort was quirked. However since the phone builds always shipped with the new sort aka 
                    // IntrospectiveSort and we would want to continue using this sort moving forward CoreCLR always uses the new sort.

                    IntrospectiveSort(keys, index, length);
#else
                    // call the faster version of our sort algorithm if the user doesn't provide a comparer
                    if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                    {
                        IntrospectiveSort(keys, index, length);
                    }
                    else
                    {
                        DepthLimitedQuickSort(keys, index, length + index - 1, IntrospectiveSortUtilities.QuickSortDepthThreshold);
                    }
#endif
                }
                else
                {
#if FEATURE_CORECLR
                    // Since QuickSort and IntrospectiveSort produce different sorting sequence for equal keys the upgrade 
                    // to IntrospectiveSort was quirked. However since the phone builds always shipped with the new sort aka 
                    // IntrospectiveSort and we would want to continue using this sort moving forward CoreCLR always uses the new sort.

                    ArraySortHelper<T>.IntrospectiveSort(keys, index, length, comparer);
#else
                    if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                    {
                        ArraySortHelper<T>.IntrospectiveSort(keys, index, length, comparer);
                    }
                    else
                    {
                        ArraySortHelper<T>.DepthLimitedQuickSort(keys, index, length + index - 1, comparer, IntrospectiveSortUtilities.QuickSortDepthThreshold);
                    }
#endif
                }
            }
            catch (IndexOutOfRangeException)
            {
                IntrospectiveSortUtilities.ThrowOrIgnoreBadComparer(comparer);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
            }
        }

        public int BinarySearch(T[] array, int index, int length, T value, IComparer<T> comparer)
        {
            Contract.Assert(array != null, "Check the arguments in the caller!");
            Contract.Assert(index >= 0 && length >= 0 && (array.Length - index >= length), "Check the arguments in the caller!");

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
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
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

        private static void SwapIfGreaterWithItems(T[] keys, int a, int b)
        {
            Contract.Requires(keys != null);
            Contract.Requires(0 <= a && a < keys.Length);
            Contract.Requires(0 <= b && b < keys.Length);

            if (a != b)
            {
                if (keys[a] != null && keys[a].CompareTo(keys[b]) > 0)
                {
                    T key = keys[a];
                    keys[a] = keys[b];
                    keys[b] = key;
                }
            }
        }

        private static void Swap(T[] a, int i, int j)
        {
            if(i!=j)
            {
                T t = a[i];
                a[i] = a[j];
                a[j] = t;
            }
        }

        private static void DepthLimitedQuickSort(T[] keys, int left, int right, int depthLimit)
        {
            Contract.Requires(keys != null);
            Contract.Requires(0 <= left && left < keys.Length);
            Contract.Requires(0 <= right && right < keys.Length);

            // The code in this function looks very similar to QuickSort in ArraySortHelper<T> class.
            // The difference is that T is constrainted to IComparable<T> here.
            // So the IL code will be different. This function is faster than the one in ArraySortHelper<T>.

            do
            {
                if (depthLimit == 0)
                {
                    Heapsort(keys, left, right);
                    return;
                }

                int i = left;
                int j = right;

                // pre-sort the low, middle (pivot), and high values in place.
                // this improves performance in the face of already sorted data, or 
                // data that is made up of multiple sorted runs appended together.
                int middle = i + ((j - i) >> 1);
                SwapIfGreaterWithItems(keys, i, middle); // swap the low with the mid point
                SwapIfGreaterWithItems(keys, i, j);      // swap the low with the high
                SwapIfGreaterWithItems(keys, middle, j); // swap the middle with the high

                T x = keys[middle];
                do
                {
                    if (x == null)
                    {
                        // if x null, the loop to find two elements to be switched can be reduced.
                        while (keys[j] != null) j--;
                    }
                    else
                    {
                        while (x.CompareTo(keys[i]) > 0) i++;
                        while (x.CompareTo(keys[j]) < 0) j--;
                    }
                    Contract.Assert(i >= left && j <= right, "(i>=left && j<=right)  Sort failed - Is your IComparer bogus?");
                    if (i > j) break;
                    if (i < j)
                    {
                        T key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;
                    }
                    i++;
                    j--;
                } while (i <= j);

                // The next iteration of the while loop is to "recursively" sort the larger half of the array and the
                // following calls recursively sort the smaller half.  So we subtract one from depthLimit here so
                // both sorts see the new value.
                depthLimit--;

                if (j - left <= right - i)
                {
                    if (left < j) DepthLimitedQuickSort(keys, left, j, depthLimit);
                    left = i;
                }
                else
                {
                    if (i < right) DepthLimitedQuickSort(keys, i, right, depthLimit);
                    right = j;
                }
            } while (left < right);
        }

        internal static void IntrospectiveSort(T[] keys, int left, int length)
        {
            Contract.Requires(keys != null);
            Contract.Requires(left >= 0);
            Contract.Requires(length >= 0);
            Contract.Requires(length <= keys.Length);
            Contract.Requires(length + left <= keys.Length);

            if (length < 2)
                return;

            IntroSort(keys, left, length + left - 1, 2 * IntrospectiveSortUtilities.FloorLog2(keys.Length));
        }

        private static void IntroSort(T[] keys, int lo, int hi, int depthLimit)
        {
            Contract.Requires(keys != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi < keys.Length);

            while (hi > lo)
            {
                int partitionSize = hi - lo + 1;
                if (partitionSize <= IntrospectiveSortUtilities.IntrosortSizeThreshold)
                {
                    if (partitionSize == 1)
                    {
                        return;
                    }
                    if (partitionSize == 2)
                    {
                        SwapIfGreaterWithItems(keys, lo, hi);
                        return;
                    }
                    if (partitionSize == 3)
                    {
                        SwapIfGreaterWithItems(keys, lo, hi-1);
                        SwapIfGreaterWithItems(keys, lo, hi);
                        SwapIfGreaterWithItems(keys, hi-1, hi);
                        return;
                    }

                    InsertionSort(keys, lo, hi);
                    return;
                }

                if (depthLimit == 0)
                {
                    Heapsort(keys, lo, hi);
                    return;
                }
                depthLimit--;

                int p = PickPivotAndPartition(keys, lo, hi);
                // Note we've already partitioned around the pivot and do not have to move the pivot again.
                IntroSort(keys, p + 1, hi, depthLimit);
                hi = p - 1;
            }
        }

        private static int PickPivotAndPartition(T[] keys, int lo, int hi)
        {
            Contract.Requires(keys != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi > lo);
            Contract.Requires(hi < keys.Length);
            Contract.Ensures(Contract.Result<int>() >= lo && Contract.Result<int>() <= hi);

            // Compute median-of-three.  But also partition them, since we've done the comparison.
            int middle = lo + ((hi - lo) / 2);

            // Sort lo, mid and hi appropriately, then pick mid as the pivot.
            SwapIfGreaterWithItems(keys, lo, middle);  // swap the low with the mid point
            SwapIfGreaterWithItems(keys, lo, hi);   // swap the low with the high
            SwapIfGreaterWithItems(keys, middle, hi); // swap the middle with the high

            T pivot = keys[middle];
            Swap(keys, middle, hi - 1);
            int left = lo, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

            while (left < right)
            {
                if (pivot == null)
                {
                    while (left < (hi - 1) && keys[++left] == null) ;
                    while (right > lo && keys[--right] != null) ;
                }
                else
                {
                    while (pivot.CompareTo(keys[++left]) > 0) ;
                    while (pivot.CompareTo(keys[--right]) < 0) ;
                }

                if (left >= right)
                    break;

                Swap(keys, left, right);
            }

            // Put pivot in the right location.
            Swap(keys, left, (hi - 1));
            return left;
        }

        private static void Heapsort(T[] keys, int lo, int hi)
        {
            Contract.Requires(keys != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi > lo);
            Contract.Requires(hi < keys.Length);

            int n = hi - lo + 1;
            for (int i = n / 2; i >= 1; i = i - 1)
            {
                DownHeap(keys, i, n, lo);
            }
            for (int i = n; i > 1; i = i - 1)
            {
                Swap(keys, lo, lo + i - 1);
                DownHeap(keys, 1, i - 1, lo);
            }
        }

        private static void DownHeap(T[] keys, int i, int n, int lo)
        {
            Contract.Requires(keys != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(lo < keys.Length);

            T d = keys[lo + i - 1];
            int child;
            while (i <= n / 2)
            {
                child = 2 * i;
                if (child < n && (keys[lo + child - 1] == null || keys[lo + child - 1].CompareTo(keys[lo + child]) < 0))
                {
                    child++;
                }
                if (keys[lo + child - 1] == null || keys[lo + child - 1].CompareTo(d) < 0)
                    break;
                keys[lo + i - 1] = keys[lo + child - 1];
                i = child;
            }
            keys[lo + i - 1] = d;
        }

        private static void InsertionSort(T[] keys, int lo, int hi)
        {
            Contract.Requires(keys != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi >= lo);
            Contract.Requires(hi <= keys.Length);

            int i, j;
            T t;
            for (i = lo; i < hi; i++)
            {
                j = i;
                t = keys[i + 1];
                while (j >= lo && (t == null || t.CompareTo(keys[j]) < 0))
                {
                    keys[j + 1] = keys[j];
                    j--;
                }
                keys[j + 1] = t;
            }
        }
    }

    #endregion

    #region ArraySortHelper for paired key and value arrays

    internal interface IArraySortHelper<TKey, TValue>
    {
        void Sort(TKey[] keys, TValue[] values, int index, int length, IComparer<TKey> comparer);
    }

    [TypeDependencyAttribute("System.Collections.Generic.GenericArraySortHelper`2")]
    internal class ArraySortHelper<TKey, TValue>
        : IArraySortHelper<TKey, TValue>
    {
        static volatile IArraySortHelper<TKey, TValue> defaultArraySortHelper;

        public static IArraySortHelper<TKey, TValue> Default
        {
            get
            {
                IArraySortHelper<TKey, TValue> sorter = defaultArraySortHelper;
                if (sorter == null)
                    sorter = CreateArraySortHelper();

                return sorter;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private static IArraySortHelper<TKey, TValue> CreateArraySortHelper()
        {
            if (typeof(IComparable<TKey>).IsAssignableFrom(typeof(TKey)))
            {
                defaultArraySortHelper = (IArraySortHelper<TKey, TValue>)RuntimeTypeHandle.Allocate(typeof(GenericArraySortHelper<string, string>).TypeHandle.Instantiate(new Type[] { typeof(TKey), typeof(TValue) }));
            }
            else
            {
                defaultArraySortHelper = new ArraySortHelper<TKey, TValue>();
            }
            return defaultArraySortHelper;
        }

        public void Sort(TKey[] keys, TValue[] values, int index, int length, IComparer<TKey> comparer)
        {
            Contract.Assert(keys != null, "Check the arguments in the caller!");  // Precondition on interface method
            Contract.Assert(index >= 0 && length >= 0 && (keys.Length - index >= length), "Check the arguments in the caller!");

            // Add a try block here to detect IComparers (or their
            // underlying IComparables, etc) that are bogus.
            try
            {
                if (comparer == null || comparer == Comparer<TKey>.Default)
                {
                    comparer = Comparer<TKey>.Default;
                }

#if FEATURE_CORECLR
                // Since QuickSort and IntrospectiveSort produce different sorting sequence for equal keys the upgrade 
                // to IntrospectiveSort was quirked. However since the phone builds always shipped with the new sort aka 
                // IntrospectiveSort and we would want to continue using this sort moving forward CoreCLR always uses the new sort.

                IntrospectiveSort(keys, values, index, length, comparer);
#else
                if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                {
                    IntrospectiveSort(keys, values, index, length, comparer);
                }
                else
                {
                    DepthLimitedQuickSort(keys, values, index, length + index - 1, comparer, IntrospectiveSortUtilities.QuickSortDepthThreshold);
                }
#endif
            }
            catch (IndexOutOfRangeException)
            {
                IntrospectiveSortUtilities.ThrowOrIgnoreBadComparer(comparer);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
            }
        }

        private static void SwapIfGreaterWithItems(TKey[] keys, TValue[] values, IComparer<TKey> comparer, int a, int b)
        {
            Contract.Requires(keys != null);
            Contract.Requires(values == null || values.Length >= keys.Length);
            Contract.Requires(comparer != null);
            Contract.Requires(0 <= a && a < keys.Length);
            Contract.Requires(0 <= b && b < keys.Length);

            if (a != b)
            {
                if (comparer.Compare(keys[a], keys[b]) > 0)
                {
                    TKey key = keys[a];
                    keys[a] = keys[b];
                    keys[b] = key;
                    if (values != null)
                    {
                        TValue value = values[a];
                        values[a] = values[b];
                        values[b] = value;
                    }
                }
            }
        }

        private static void Swap(TKey[] keys, TValue[] values, int i, int j)
        {
            if(i!=j)
            {
                TKey k = keys[i];
                keys[i] = keys[j];
                keys[j] = k;
                if(values != null)
                {
                    TValue v = values[i];
                    values[i] = values[j];
                    values[j] = v;
                }
            }
        }

        internal static void DepthLimitedQuickSort(TKey[] keys, TValue[] values, int left, int right, IComparer<TKey> comparer, int depthLimit)
        {
            do
            {
                if (depthLimit == 0)
                {
                    Heapsort(keys, values, left, right, comparer);
                    return;
                }

                int i = left;
                int j = right;

                // pre-sort the low, middle (pivot), and high values in place.
                // this improves performance in the face of already sorted data, or 
                // data that is made up of multiple sorted runs appended together.
                int middle = i + ((j - i) >> 1);
                SwapIfGreaterWithItems(keys, values, comparer, i, middle);  // swap the low with the mid point
                SwapIfGreaterWithItems(keys, values, comparer, i, j);   // swap the low with the high
                SwapIfGreaterWithItems(keys, values, comparer, middle, j); // swap the middle with the high

                TKey x = keys[middle];
                do
                {
                    while (comparer.Compare(keys[i], x) < 0) i++;
                    while (comparer.Compare(x, keys[j]) < 0) j--;
                    Contract.Assert(i >= left && j <= right, "(i>=left && j<=right)  Sort failed - Is your IComparer bogus?");
                    if (i > j) break;
                    if (i < j)
                    {
                        TKey key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;
                        if (values != null)
                        {
                            TValue value = values[i];
                            values[i] = values[j];
                            values[j] = value;
                        }
                    }
                    i++;
                    j--;
                } while (i <= j);

                // The next iteration of the while loop is to "recursively" sort the larger half of the array and the
                // following calls recursively sort the smaller half.  So we subtract one from depthLimit here so
                // both sorts see the new value.
                depthLimit--;

                if (j - left <= right - i)
                {
                    if (left < j) DepthLimitedQuickSort(keys, values, left, j, comparer, depthLimit);
                    left = i;
                }
                else
                {
                    if (i < right) DepthLimitedQuickSort(keys, values, i, right, comparer, depthLimit);
                    right = j;
                }
            } while (left < right);
        }

        internal static void IntrospectiveSort(TKey[] keys, TValue[] values, int left, int length, IComparer<TKey> comparer)
        {
            Contract.Requires(keys != null);
            Contract.Requires(values != null);
            Contract.Requires(comparer != null);
            Contract.Requires(left >= 0);
            Contract.Requires(length >= 0);
            Contract.Requires(length <= keys.Length);
            Contract.Requires(length + left <= keys.Length);
            Contract.Requires(length + left <= values.Length);

            if (length < 2)
                return;

            IntroSort(keys, values, left, length + left - 1, 2 * IntrospectiveSortUtilities.FloorLog2(keys.Length), comparer);
        }

        private static void IntroSort(TKey[] keys, TValue[] values, int lo, int hi, int depthLimit, IComparer<TKey> comparer)
        {
            Contract.Requires(keys != null);
            Contract.Requires(values != null);
            Contract.Requires(comparer != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi < keys.Length);

            while (hi > lo)
            {
                int partitionSize = hi - lo + 1;
                if (partitionSize <= IntrospectiveSortUtilities.IntrosortSizeThreshold)
                {
                    if (partitionSize == 1)
                    {
                        return;
                    }
                    if (partitionSize == 2)
                    {
                        SwapIfGreaterWithItems(keys, values, comparer, lo, hi);
                        return;
                    }
                    if (partitionSize == 3)
                    {
                        SwapIfGreaterWithItems(keys, values, comparer, lo, hi-1);
                        SwapIfGreaterWithItems(keys, values, comparer, lo, hi);
                        SwapIfGreaterWithItems(keys, values, comparer, hi-1, hi);
                        return;
                    }

                    InsertionSort(keys, values, lo, hi, comparer);
                    return;
                }

                if (depthLimit == 0)
                {
                    Heapsort(keys, values, lo, hi, comparer);
                    return;
                }
                depthLimit--;

                int p = PickPivotAndPartition(keys, values, lo, hi, comparer);
                // Note we've already partitioned around the pivot and do not have to move the pivot again.
                IntroSort(keys, values, p + 1, hi, depthLimit, comparer);
                hi = p - 1;
            }
        }

        private static int PickPivotAndPartition(TKey[] keys, TValue[] values, int lo, int hi, IComparer<TKey> comparer)
        {
            Contract.Requires(keys != null);
            Contract.Requires(values != null);
            Contract.Requires(comparer != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi > lo);
            Contract.Requires(hi < keys.Length);
            Contract.Ensures(Contract.Result<int>() >= lo && Contract.Result<int>() <= hi);

            // Compute median-of-three.  But also partition them, since we've done the comparison.
            int middle = lo + ((hi - lo) / 2);

            // Sort lo, mid and hi appropriately, then pick mid as the pivot.
            SwapIfGreaterWithItems(keys, values, comparer, lo, middle);  // swap the low with the mid point
            SwapIfGreaterWithItems(keys, values, comparer, lo, hi);   // swap the low with the high
            SwapIfGreaterWithItems(keys, values, comparer, middle, hi); // swap the middle with the high

            TKey pivot = keys[middle];
            Swap(keys, values, middle, hi - 1);
            int left = lo, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

            while (left < right)
            {
                while (comparer.Compare(keys[++left], pivot) < 0) ;
                while (comparer.Compare(pivot, keys[--right]) < 0) ;

                if (left >= right)
                    break;

                Swap(keys, values, left, right);
            }

            // Put pivot in the right location.
            Swap(keys, values, left, (hi - 1));
            return left;
        }

        private static void Heapsort(TKey[] keys, TValue[] values, int lo, int hi, IComparer<TKey> comparer)
        {
            Contract.Requires(keys != null);
            Contract.Requires(values != null);
            Contract.Requires(comparer != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi > lo);
            Contract.Requires(hi < keys.Length);

            int n = hi - lo + 1;
            for (int i = n / 2; i >= 1; i = i - 1)
            {
                DownHeap(keys, values, i, n, lo, comparer);
            }
            for (int i = n; i > 1; i = i - 1)
            {
                Swap(keys, values, lo, lo + i - 1);
                DownHeap(keys, values, 1, i - 1, lo, comparer);
            }
        }

        private static void DownHeap(TKey[] keys, TValue[] values, int i, int n, int lo, IComparer<TKey> comparer)
        {
            Contract.Requires(keys != null);
            Contract.Requires(comparer != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(lo < keys.Length);

            TKey d = keys[lo + i - 1];
            TValue dValue = (values != null) ? values[lo + i - 1] : default(TValue);
            int child;
            while (i <= n / 2)
            {
                child = 2 * i;
                if (child < n && comparer.Compare(keys[lo + child - 1], keys[lo + child]) < 0)
                {
                    child++;
                }
                if (!(comparer.Compare(d, keys[lo + child - 1]) < 0))
                    break;
                keys[lo + i - 1] = keys[lo + child - 1];
                if(values != null)
                    values[lo + i - 1] = values[lo + child - 1];
                i = child;
            }
            keys[lo + i - 1] = d;
            if(values != null)
                values[lo + i - 1] = dValue;
        }

        private static void InsertionSort(TKey[] keys, TValue[] values, int lo, int hi, IComparer<TKey> comparer)
        {
            Contract.Requires(keys != null);
            Contract.Requires(values != null);
            Contract.Requires(comparer != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi >= lo);
            Contract.Requires(hi <= keys.Length);

            int i, j;
            TKey t;
            TValue tValue;
            for (i = lo; i < hi; i++)
            {
                j = i;
                t = keys[i + 1];
                tValue = (values != null) ? values[i + 1] : default(TValue);
                while (j >= lo && comparer.Compare(t, keys[j]) < 0)
                {
                    keys[j + 1] = keys[j];
                    if(values != null)
                        values[j + 1] = values[j];
                    j--;
                }
                keys[j + 1] = t;
                if(values != null)
                    values[j + 1] = tValue;
            }
        }
    }

    internal class GenericArraySortHelper<TKey, TValue>
        : IArraySortHelper<TKey, TValue>
        where TKey : IComparable<TKey>
    {
        public void Sort(TKey[] keys, TValue[] values, int index, int length, IComparer<TKey> comparer)
        {
            Contract.Assert(keys != null, "Check the arguments in the caller!");
            Contract.Assert( index >= 0 && length >= 0 && (keys.Length - index >= length), "Check the arguments in the caller!");
            
            // Add a try block here to detect IComparers (or their
            // underlying IComparables, etc) that are bogus.
            try
            {
                if (comparer == null || comparer == Comparer<TKey>.Default)
                {
#if FEATURE_CORECLR
                    // Since QuickSort and IntrospectiveSort produce different sorting sequence for equal keys the upgrade 
                    // to IntrospectiveSort was quirked. However since the phone builds always shipped with the new sort aka 
                    // IntrospectiveSort and we would want to continue using this sort moving forward CoreCLR always uses the new sort.

                    IntrospectiveSort(keys, values, index, length);
#else
                    // call the faster version of our sort algorithm if the user doesn't provide a comparer
                    if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                    {
                        IntrospectiveSort(keys, values, index, length);
                    }
                    else
                    {
                        DepthLimitedQuickSort(keys, values, index, length + index - 1, IntrospectiveSortUtilities.QuickSortDepthThreshold);
                    }
#endif
                }
                else
                {
#if FEATURE_CORECLR
                    // Since QuickSort and IntrospectiveSort produce different sorting sequence for equal keys the upgrade 
                    // to IntrospectiveSort was quirked. However since the phone builds always shipped with the new sort aka 
                    // IntrospectiveSort and we would want to continue using this sort moving forward CoreCLR always uses the new sort.

                    ArraySortHelper<TKey, TValue>.IntrospectiveSort(keys, values, index, length, comparer);
#else
                    if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                    {
                        ArraySortHelper<TKey, TValue>.IntrospectiveSort(keys, values, index, length, comparer);
                    }
                    else
                    {
                        ArraySortHelper<TKey, TValue>.DepthLimitedQuickSort(keys, values, index, length + index - 1, comparer, IntrospectiveSortUtilities.QuickSortDepthThreshold);
                    }
#endif
                }

            }                    
            catch (IndexOutOfRangeException)
            {
                IntrospectiveSortUtilities.ThrowOrIgnoreBadComparer(comparer);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
            }
        }

        private static void SwapIfGreaterWithItems(TKey[] keys, TValue[] values, int a, int b)
        {
            if (a != b)
            {
                if (keys[a] != null && keys[a].CompareTo(keys[b]) > 0)
                {
                    TKey key = keys[a];
                    keys[a] = keys[b];
                    keys[b] = key;
                    if (values != null)
                    {
                        TValue value = values[a];
                        values[a] = values[b];
                        values[b] = value;
                    }
                }
            }
        }

        private static void Swap(TKey[] keys, TValue[] values, int i, int j)
        {
            if(i != j)
            {
                TKey k = keys[i];
                keys[i] = keys[j];
                keys[j] = k;
                if(values != null)
                {
                    TValue v = values[i];
                    values[i] = values[j];
                    values[j] = v;
                }
            }
        }

        private static void DepthLimitedQuickSort(TKey[] keys, TValue[] values, int left, int right, int depthLimit)
        {
            // The code in this function looks very similar to QuickSort in ArraySortHelper<T> class.
            // The difference is that T is constrainted to IComparable<T> here.
            // So the IL code will be different. This function is faster than the one in ArraySortHelper<T>.

            do
            {
                if (depthLimit == 0)
                {
                    Heapsort(keys, values, left, right);
                    return;
                }

                int i = left;
                int j = right;

                // pre-sort the low, middle (pivot), and high values in place.
                // this improves performance in the face of already sorted data, or 
                // data that is made up of multiple sorted runs appended together.
                int middle = i + ((j - i) >> 1);
                SwapIfGreaterWithItems(keys, values, i, middle); // swap the low with the mid point
                SwapIfGreaterWithItems(keys, values, i, j);      // swap the low with the high
                SwapIfGreaterWithItems(keys, values, middle, j); // swap the middle with the high

                TKey x = keys[middle];
                do
                {
                    if (x == null)
                    {
                        // if x null, the loop to find two elements to be switched can be reduced.
                        while (keys[j] != null) j--;
                    }
                    else
                    {
                        while (x.CompareTo(keys[i]) > 0) i++;
                        while (x.CompareTo(keys[j]) < 0) j--;
                    }
                    Contract.Assert(i >= left && j <= right, "(i>=left && j<=right)  Sort failed - Is your IComparer bogus?");
                    if (i > j) break;
                    if (i < j)
                    {
                        TKey key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;
                        if (values != null)
                        {
                            TValue value = values[i];
                            values[i] = values[j];
                            values[j] = value;
                        }
                    }
                    i++;
                    j--;
                } while (i <= j);

                // The next iteration of the while loop is to "recursively" sort the larger half of the array and the
                // following calls recursively sort the smaller half.  So we subtract one from depthLimit here so
                // both sorts see the new value.
                depthLimit--;

                if (j - left <= right - i)
                {
                    if (left < j) DepthLimitedQuickSort(keys, values, left, j, depthLimit);
                    left = i;
                }
                else
                {
                    if (i < right) DepthLimitedQuickSort(keys, values, i, right, depthLimit);
                    right = j;
                }
            } while (left < right);
        }

        internal static void IntrospectiveSort(TKey[] keys, TValue[] values, int left, int length)
        {
            Contract.Requires(keys != null);
            Contract.Requires(values != null);
            Contract.Requires(left >= 0);
            Contract.Requires(length >= 0);
            Contract.Requires(length <= keys.Length);
            Contract.Requires(length + left <= keys.Length);
            Contract.Requires(length + left <= values.Length);

            if (length < 2)
                return;

            IntroSort(keys, values, left, length + left - 1, 2 * IntrospectiveSortUtilities.FloorLog2(keys.Length));
        }

        private static void IntroSort(TKey[] keys, TValue[] values, int lo, int hi, int depthLimit)
        {
            Contract.Requires(keys != null);
            Contract.Requires(values != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi < keys.Length);

            while (hi > lo)
            {
                int partitionSize = hi - lo + 1;
                if (partitionSize <= IntrospectiveSortUtilities.IntrosortSizeThreshold)
                {
                    if (partitionSize == 1)
                    {
                        return;
                    }
                    if (partitionSize == 2)
                    {
                        SwapIfGreaterWithItems(keys, values, lo, hi);
                        return;
                    }
                    if (partitionSize == 3)
                    {
                        SwapIfGreaterWithItems(keys, values, lo, hi-1);
                        SwapIfGreaterWithItems(keys, values, lo, hi);
                        SwapIfGreaterWithItems(keys, values, hi-1, hi);
                        return;
                    }

                    InsertionSort(keys, values, lo, hi);
                    return;
                }

                if (depthLimit == 0)
                {
                    Heapsort(keys, values, lo, hi);
                    return;
                }
                depthLimit--;

                int p = PickPivotAndPartition(keys, values, lo, hi);
                // Note we've already partitioned around the pivot and do not have to move the pivot again.
                IntroSort(keys, values, p + 1, hi, depthLimit);
                hi = p - 1;
            }
        }

        private static int PickPivotAndPartition(TKey[] keys, TValue[] values, int lo, int hi)
        {
            Contract.Requires(keys != null);
            Contract.Requires(values != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi > lo);
            Contract.Requires(hi < keys.Length);
            Contract.Ensures(Contract.Result<int>() >= lo && Contract.Result<int>() <= hi);

            // Compute median-of-three.  But also partition them, since we've done the comparison.
            int middle = lo + ((hi - lo) / 2);

            // Sort lo, mid and hi appropriately, then pick mid as the pivot.
            SwapIfGreaterWithItems(keys, values, lo, middle);  // swap the low with the mid point
            SwapIfGreaterWithItems(keys, values, lo, hi);   // swap the low with the high
            SwapIfGreaterWithItems(keys, values, middle, hi); // swap the middle with the high

            TKey pivot = keys[middle];
            Swap(keys, values, middle, hi - 1);
            int left = lo, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

            while (left < right)
            {
                if(pivot == null)
                {
                    while (left < (hi - 1) && keys[++left] == null) ;
                    while (right > lo && keys[--right] != null);
                }
                else
                {
                    while (pivot.CompareTo(keys[++left]) > 0) ;
                    while (pivot.CompareTo(keys[--right]) < 0) ;
                }

                if (left >= right)
                    break;

                Swap(keys, values, left, right);
            }

            // Put pivot in the right location.
            Swap(keys, values, left, (hi - 1));
            return left;
        }

        private static void Heapsort(TKey[] keys, TValue[] values, int lo, int hi)
        {
            Contract.Requires(keys != null);
            Contract.Requires(values != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi > lo);
            Contract.Requires(hi < keys.Length);

            int n = hi - lo + 1;
            for (int i = n / 2; i >= 1; i = i - 1)
            {
                DownHeap(keys, values, i, n, lo);
            }
            for (int i = n; i > 1; i = i - 1)
            {
                Swap(keys, values, lo, lo + i - 1);
                DownHeap(keys, values, 1, i - 1, lo);
            }
        }

        private static void DownHeap(TKey[] keys, TValue[] values, int i, int n, int lo)
        {
            Contract.Requires(keys != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(lo < keys.Length);

            TKey d = keys[lo + i - 1];
            TValue dValue = (values != null) ? values[lo + i - 1] : default(TValue);
            int child;
            while (i <= n / 2)
            {
                child = 2 * i;
                if (child < n && (keys[lo + child - 1] == null || keys[lo + child - 1].CompareTo(keys[lo + child]) < 0))
                {
                    child++;
                }
                if (keys[lo + child - 1] == null || keys[lo + child - 1].CompareTo(d) < 0)
                    break;
                keys[lo + i - 1] = keys[lo + child - 1];
                if(values != null)
                    values[lo + i - 1] = values[lo + child - 1];
                i = child;
            }
            keys[lo + i - 1] = d;
            if(values != null)
                values[lo + i - 1] = dValue;
        }

        private static void InsertionSort(TKey[] keys, TValue[] values, int lo, int hi)
        {
            Contract.Requires(keys != null);
            Contract.Requires(values != null);
            Contract.Requires(lo >= 0);
            Contract.Requires(hi >= lo);
            Contract.Requires(hi <= keys.Length);

            int i, j;
            TKey t;
            TValue tValue;
            for (i = lo; i < hi; i++)
            {
                j = i;
                t = keys[i + 1];
                tValue = (values != null)? values[i + 1] : default(TValue);
                while (j >= lo && (t == null || t.CompareTo(keys[j]) < 0))
                {
                    keys[j + 1] = keys[j];
                    if(values != null)
                        values[j + 1] = values[j];
                    j--;
                }
                keys[j + 1] = t;
                if(values != null)
                    values[j + 1] = tValue;
            }
        }
    }

    #endregion
}


