// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from src/coreclr/gc/introsort.h.
//
// Sorts an array of object addresses in place. The pointer comparisons below are unsigned, which
// matches the C++ original (pointer comparison in both languages is unsigned).

namespace Internal.Runtime.GarbageCollection
{
    internal static unsafe class IntroSort
    {
        private const int SizeThreshold = 64;
        private const int MaxDepth = 100;

        private static void SwapElements(byte** i, byte** j)
        {
            byte* t = *i;
            *i = *j;
            *j = t;
        }

        /// <summary>
        /// Sorts the inclusive range [<paramref name="begin"/>, <paramref name="end"/>].
        /// </summary>
        public static void Sort(byte** begin, byte** end)
        {
            IntroSortLoop(begin, end, MaxDepth);
            InsertionSort(begin, end);
        }

        private static void IntroSortLoop(byte** lo, byte** hi, int depthLimit)
        {
            while (hi - lo >= SizeThreshold)
            {
                if (depthLimit == 0)
                {
                    HeapSort(lo, hi);
                    return;
                }

                byte** p = MedianPartition(lo, hi);
                depthLimit--;
                IntroSortLoop(p, hi, depthLimit);
                hi = p - 1;
            }
        }

        private static byte** MedianPartition(byte** low, byte** high)
        {
            // sort low, middle and high
            if (*(low + ((high - low) / 2)) < *low)
                SwapElements(low + ((high - low) / 2), low);
            if (*high < *low)
                SwapElements(low, high);
            if (*high < *(low + ((high - low) / 2)))
                SwapElements(low + ((high - low) / 2), high);

            SwapElements(low + ((high - low) / 2), high - 1);
            byte* pivot = *(high - 1);
            byte** left = low;
            byte** right = high - 1;
            while (true)
            {
                while (*(--right) > pivot)
                    ;
                while (*(++left) < pivot)
                    ;
                if (left < right)
                {
                    SwapElements(left, right);
                }
                else
                {
                    break;
                }
            }

            SwapElements(left, high - 1);
            return left;
        }

        private static void InsertionSort(byte** lo, byte** hi)
        {
            for (byte** i = lo + 1; i <= hi; i++)
            {
                byte** j = i;
                byte* t = *i;
                while (j > lo && t < *(j - 1))
                {
                    *j = *(j - 1);
                    j--;
                }

                *j = t;
            }
        }

        private static void HeapSort(byte** lo, byte** hi)
        {
            nuint n = (nuint)(hi - lo) + 1;
            for (nuint i = n / 2; i >= 1; i--)
            {
                DownHeap(i, n, lo);
            }

            for (nuint i = n; i > 1; i--)
            {
                SwapElements(lo, lo + i - 1);
                DownHeap(1, i - 1, lo);
            }
        }

        private static void DownHeap(nuint i, nuint n, byte** lo)
        {
            byte* d = *(lo + i - 1);
            while (i <= n / 2)
            {
                nuint child = 2 * i;
                if (child < n && *(lo + child - 1) < *(lo + child))
                {
                    child++;
                }

                if (!(d < *(lo + child - 1)))
                {
                    break;
                }

                *(lo + i - 1) = *(lo + child - 1);
                i = child;
            }

            *(lo + i - 1) = d;
        }
    }
}
