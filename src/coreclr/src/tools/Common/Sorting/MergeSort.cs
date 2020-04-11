// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using ILCompiler.Sorting.Implementation;

namespace ILCompiler
{
    public static class MergeSortApi
    {
        private const bool UseStandardSort = false; // Set this to true to use List<T>.Sort and Array.Sort instead of the mergesort algorithm
                                                    // Used to measure the performance impact of using this custom sort algorithm
        internal const int ParallelSortThreshold = UseStandardSort ? Int32.MaxValue : 4000; // Number empirically measured. 

        private static class SortAsEqualBehavior<T>
        {
            public static Action<T,T> FailSort = DoFailSort;

            private static void DoFailSort(T t1, T t2)
            {
                throw new InvalidOperationException();
            }
        }

        // Parallel sorting api which will sort in parallel when appropriate
        public static void ParallelSort<T>(this List<T> listToSort, Comparison<T> comparison)
        {
            if (listToSort.Count < ParallelSortThreshold)
            {
                listToSort.MergeSort<T, RequireTotalOrderAssert>(comparison);
            }
            MergeSortCore<T, List<T>, ListAccessor<T>, ComparisonWrapper<T>, RequireTotalOrderAssert>.ParallelSort(listToSort, 0, listToSort.Count, new ComparisonWrapper<T>(comparison)).Wait();
        }

        // Parallel sorting api which will sort in parallel when appropriate
        public static void ParallelSortAllowDuplicates<T>(this List<T> listToSort, Comparison<T> comparison)
        {
            if (listToSort.Count < ParallelSortThreshold)
            {
                listToSort.MergeSort<T, AllowDuplicates>(comparison);
            }
            MergeSortCore<T, List<T>, ListAccessor<T>, ComparisonWrapper<T>, AllowDuplicates>.ParallelSort(listToSort, 0, listToSort.Count, new ComparisonWrapper<T>(comparison)).Wait();
        }

        // Parallel sorting api which will sort in parallel when appropriate
        public static void ParallelSort<T>(this List<T> listToSort, IComparer<T> comparer)
        {
            if (listToSort.Count < ParallelSortThreshold)
            {
                listToSort.MergeSort<T, RequireTotalOrderAssert>(comparer);
            }
            MergeSortCore<T, List<T>, ListAccessor<T>, IComparer<T>, RequireTotalOrderAssert>.ParallelSort(listToSort, 0, listToSort.Count, comparer).Wait();
        }

        // Parallel sorting api which will sort in parallel when appropriate
        public static void ParallelSortAllowDuplicates<T>(this List<T> listToSort, IComparer<T> comparer)
        {
            if (listToSort.Count < ParallelSortThreshold)
            {
                listToSort.MergeSort<T, AllowDuplicates>(comparer);
            }
            MergeSortCore<T, List<T>, ListAccessor<T>, IComparer<T>, AllowDuplicates>.ParallelSort(listToSort, 0, listToSort.Count, comparer).Wait();
        }

        // Parallel sorting api which will sort in parallel when appropriate
        public static void ParallelSort<T>(this T[] arrayToSort, Comparison<T> comparison)
        {
            if (arrayToSort.Length < ParallelSortThreshold)
            {
                arrayToSort.MergeSort<T, RequireTotalOrderAssert>(comparison);
            }
            MergeSortCore<T, T[], ArrayAccessor<T>, ComparisonWrapper<T>, RequireTotalOrderAssert>.ParallelSort(arrayToSort, 0, arrayToSort.Length, new ComparisonWrapper<T>(comparison)).Wait();
        }

        // Parallel sorting api which will sort in parallel when appropriate
        public static void ParallelSortAllowDuplicates<T>(this T[] arrayToSort, Comparison<T> comparison)
        {
            if (arrayToSort.Length < ParallelSortThreshold)
            {
                arrayToSort.MergeSort<T, AllowDuplicates>(comparison);
            }
            MergeSortCore<T, T[], ArrayAccessor<T>, ComparisonWrapper<T>, AllowDuplicates>.ParallelSort(arrayToSort, 0, arrayToSort.Length, new ComparisonWrapper<T>(comparison)).Wait();
        }

        // Parallel sorting api which will sort in parallel when appropriate
        public static void ParallelSort<T>(this T[] arrayToSort, IComparer<T> comparer)
        {
            if (arrayToSort.Length < ParallelSortThreshold)
            {
                arrayToSort.MergeSort<T, RequireTotalOrderAssert>(comparer);
            }
            MergeSortCore<T, T[], ArrayAccessor<T>, IComparer<T>, RequireTotalOrderAssert>.ParallelSort(arrayToSort, 0, arrayToSort.Length, comparer).Wait();
        }

        // Parallel sorting api which will sort in parallel when appropriate
        public static void ParallelSortAllowDuplicates<T>(this T[] arrayToSort, IComparer<T> comparer)
        {
            if (arrayToSort.Length < ParallelSortThreshold)
            {
                arrayToSort.MergeSort<T, AllowDuplicates>(comparer);
            }
            MergeSortCore<T, T[], ArrayAccessor<T>, IComparer<T>, AllowDuplicates>.ParallelSort(arrayToSort, 0, arrayToSort.Length, comparer).Wait();
        }

        // sorting api which will perform a sequential merge sort
        private static void MergeSort<T, TCompareAsEqualAction>(this List<T> arrayToSort, IComparer<T> comparer)
            where TCompareAsEqualAction : ICompareAsEqualAction
        {
#pragma warning disable 0162 // disable warning for unreachable code
            if (UseStandardSort)
            {
                arrayToSort.Sort(comparer);
            }
            else
            {
                MergeSortCore<T, List<T>, ListAccessor<T>, IComparer<T>, TCompareAsEqualAction>.SequentialSort(arrayToSort, 0, arrayToSort.Count, comparer);
            }
#pragma warning restore 0162 // re-enable warning for unreachable code
        }

        // sorting api which will perform a sequential merge sort
        private static void MergeSort<T, TCompareAsEqualAction>(this List<T> arrayToSort, Comparison<T> comparison)
            where TCompareAsEqualAction : ICompareAsEqualAction
        {
#pragma warning disable 0162 // disable warning for unreachable code
            if (UseStandardSort)
            {
                arrayToSort.Sort(comparison);
            }
            else
            {
                MergeSortCore<T, List<T>, ListAccessor<T>, ComparisonWrapper<T>, TCompareAsEqualAction>.SequentialSort(arrayToSort, 0, arrayToSort.Count, new ComparisonWrapper<T>(comparison));
            }
#pragma warning restore 0162 // re-enable warning for unreachable code
        }

        // sorting api which will perform a sequential merge sort
        private static void MergeSort<T, TCompareAsEqualAction>(this T[] arrayToSort, IComparer<T> comparer)
            where TCompareAsEqualAction : ICompareAsEqualAction
        {
#pragma warning disable 0162 // disable warning for unreachable code
            if (UseStandardSort)
            {
                Array.Sort(arrayToSort, comparer);
            }
            else
            {
                MergeSortCore<T, T[], ArrayAccessor<T>, IComparer<T>, TCompareAsEqualAction>.SequentialSort(arrayToSort, 0, arrayToSort.Length, comparer);
            }
#pragma warning restore 0162 // re-enable warning for unreachable code
        }

        // sorting api which will perform a sequential merge sort
        private static void MergeSort<T, TCompareAsEqualAction>(this T[] arrayToSort, Comparison<T> comparison)
            where TCompareAsEqualAction : ICompareAsEqualAction
        {
#pragma warning disable 0162 // disable warning for unreachable code
            if (UseStandardSort)
            {
                Array.Sort(arrayToSort, comparison);
            }
            else
            {
                MergeSortCore<T, T[], ArrayAccessor<T>, ComparisonWrapper<T>, TCompareAsEqualAction>.SequentialSort(arrayToSort, 0, arrayToSort.Length, new ComparisonWrapper<T>(comparison));
            }
#pragma warning restore 0162 // re-enable warning for unreachable code
        }

        // Internal helper struct used to enable use of Comparison<T> delegates instead of IComparer<T> instances
        private struct ComparisonWrapper<T> : IComparer<T>
        {
            Comparison<T> _comparison;
            public ComparisonWrapper(Comparison<T> comparison)
            {
                _comparison = comparison;
            }
            int IComparer<T>.Compare(T x, T y)
            {
                return _comparison(x, y);
            }
        }
    }
}
