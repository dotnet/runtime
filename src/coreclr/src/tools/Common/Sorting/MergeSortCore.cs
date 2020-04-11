// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ILCompiler.Sorting.Implementation
{
    internal static class MergeSortCore<T, TDataStructure, TDataStructureAccessor, TComparer, TCompareAsEqualAction>
        where TDataStructureAccessor:ISortableDataStructureAccessor<T, TDataStructure>
        where TComparer:IComparer<T>
        where TCompareAsEqualAction : ICompareAsEqualAction
    {
        internal const int ParallelSortThreshold = MergeSortApi.ParallelSortThreshold;

        // Parallelized merge sort algorithm. Uses Task infrastructure to spread sort across available resources
        public static async Task ParallelSort(TDataStructure arrayToSort, int index, int length, TComparer comparer)
        {
            if (length < ParallelSortThreshold)
            {
                SequentialSort(arrayToSort, index, length, comparer);
            }
            else
            {
                TDataStructureAccessor accessor = default(TDataStructureAccessor);
                int halfLen = length / 2;

                TaskCompletionSource<bool> rightSortComplete = new System.Threading.Tasks.TaskCompletionSource<bool>();
                _ = Task.Run(async () =>
                {
                    await ParallelSort(arrayToSort, index + halfLen, length - halfLen, comparer);
                    rightSortComplete.SetResult(true);
                });

                T[] localCopyOfHalfOfArray = new T[halfLen];
                accessor.Copy(arrayToSort, index, localCopyOfHalfOfArray, 0, halfLen);
                await MergeSortCore<T, T[], ArrayAccessor<T>, TComparer, TCompareAsEqualAction>.ParallelSort(localCopyOfHalfOfArray, 0, halfLen, comparer);
                await rightSortComplete.Task;
                Merge(localCopyOfHalfOfArray, arrayToSort, index, halfLen, length, comparer);
            }
        }

        // Normal non-parallel merge sort
        // Allocates length/2 in scratch space
        public static void SequentialSort(TDataStructure arrayToSort, int index, int length, TComparer comparer)
        {
            TDataStructureAccessor accessor = default(TDataStructureAccessor);
            T[] scratchSpace = new T[accessor.GetLength(arrayToSort) / 2];
            MergeSortHelper(arrayToSort, index, length, comparer, scratchSpace);
        }

        // Non-parallel merge sort, used once the region to be sorted is small enough
        // scratchSpace must be at least length/2 in size
        private static void MergeSortHelper(TDataStructure arrayToSort, int index, int length, TComparer comparer, T[] scratchSpace)
        {
            if (length <= 1)
            {
                return;
            }
            TDataStructureAccessor accessor = default(TDataStructureAccessor);
            if (length == 2)
            {
                if (comparer.Compare(accessor.GetElement(arrayToSort, index), accessor.GetElement(arrayToSort, index + 1)) > 0)
                {
                    accessor.SwapElements(arrayToSort, index, index + 1);
                }
                return;
            }

            int halfLen = length / 2;
            MergeSortHelper(arrayToSort, index, halfLen, comparer, scratchSpace);
            MergeSortHelper(arrayToSort, index + halfLen, length - halfLen, comparer, scratchSpace);
            accessor.Copy(arrayToSort, index, scratchSpace, 0, halfLen);
            Merge(scratchSpace, arrayToSort, index, halfLen, length, comparer);
        }

        // Shared merge algorithm used in both parallel and sequential variants of the mergesort
        private static void Merge(T[] localCopyOfHalfOfArray, TDataStructure arrayToSort, int index, int halfLen, int length, TComparer comparer)
        {
            TDataStructureAccessor accessor = default(TDataStructureAccessor);
            int leftHalfIndex = 0;
            int rightHalfIndex = index + halfLen;
            int rightHalfEnd = index + length;
            for (int i = 0; i < length; i++)
            {
                if (leftHalfIndex == halfLen)
                {
                    // All of the remaining elements must be from the right half, and thus must already be in position
                    break;
                }
                if (rightHalfIndex == rightHalfEnd)
                {
                    // Copy remaining elements from the local copy
                    accessor.Copy(localCopyOfHalfOfArray, leftHalfIndex, arrayToSort, index + i, length - i);
                    break;
                }

                int comparisonResult = comparer.Compare(localCopyOfHalfOfArray[leftHalfIndex], accessor.GetElement(arrayToSort, rightHalfIndex));
                if (comparisonResult == 0)
                {
                    default(TCompareAsEqualAction).CompareAsEqual();
                }
                if (comparisonResult <= 0)
                {
                    accessor.SetElement(arrayToSort, i + index, localCopyOfHalfOfArray[leftHalfIndex++]);
                }
                else
                {
                    accessor.SetElement(arrayToSort, i + index, accessor.GetElement(arrayToSort, rightHalfIndex++));
                }
            }
        }
    }
}