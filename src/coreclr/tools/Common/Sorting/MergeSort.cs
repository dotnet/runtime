// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using ILCompiler.Sorting.Implementation;

namespace ILCompiler
{
    public static class MergeSortApi
    {
        // Parallel sorting api which will sort in parallel when appropriate
        public static void MergeSort<T>(this List<T> listToSort, Comparison<T> comparison)
        {
            MergeSortCore<T, List<T>, ListAccessor<T>, ComparisonWrapper<T>, RequireTotalOrderAssert>.ParallelSortApi(listToSort, new ComparisonWrapper<T>(comparison));
        }

        // Parallel sorting api which will sort in parallel when appropriate
        public static void MergeSortAllowDuplicates<T>(this List<T> listToSort, Comparison<T> comparison)
        {
            MergeSortCore<T, List<T>, ListAccessor<T>, ComparisonWrapper<T>, AllowDuplicates>.ParallelSortApi(listToSort, new ComparisonWrapper<T>(comparison));
        }

        // Parallel sorting api which will sort in parallel when appropriate
        public static void MergeSort<T>(this List<T> listToSort, IComparer<T> comparer)
        {
            MergeSortCore<T, List<T>, ListAccessor<T>, IComparer<T>, RequireTotalOrderAssert>.ParallelSortApi(listToSort, comparer);
        }

        // Parallel sorting api which will sort in parallel when appropriate
        public static void MergeSortAllowDuplicates<T>(this List<T> listToSort, IComparer<T> comparer)
        {
            MergeSortCore<T, List<T>, ListAccessor<T>, IComparer<T>, AllowDuplicates>.ParallelSortApi(listToSort, comparer);
        }

        // Parallel sorting api which will sort in parallel when appropriate
        public static void MergeSort<T>(this T[] arrayToSort, Comparison<T> comparison)
        {
            MergeSortCore<T, T[], ArrayAccessor<T>, ComparisonWrapper<T>, RequireTotalOrderAssert>.ParallelSortApi(arrayToSort, new ComparisonWrapper<T>(comparison));
        }

        // Parallel sorting api which will sort in parallel when appropriate
        public static void MergeSortAllowDuplicates<T>(this T[] arrayToSort, Comparison<T> comparison)
        {
            MergeSortCore<T, T[], ArrayAccessor<T>, ComparisonWrapper<T>, AllowDuplicates>.ParallelSortApi(arrayToSort, new ComparisonWrapper<T>(comparison));
        }

        // Parallel sorting api which will sort in parallel when appropriate
        public static void MergeSort<T>(this T[] arrayToSort, IComparer<T> comparer)
        {
            MergeSortCore<T, T[], ArrayAccessor<T>, IComparer<T>, RequireTotalOrderAssert>.ParallelSortApi(arrayToSort, comparer);
        }

        // Parallel sorting api which will sort in parallel when appropriate
        public static void MergeSortAllowDuplicates<T>(this T[] arrayToSort, IComparer<T> comparer)
        {
            MergeSortCore<T, T[], ArrayAccessor<T>, IComparer<T>, AllowDuplicates>.ParallelSortApi(arrayToSort, comparer);
        }


        // Internal helper struct used to enable use of Comparison<T> delegates instead of IComparer<T> instances
        private struct ComparisonWrapper<T> : IComparer<T>
        {
            private Comparison<T> _comparison;
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
