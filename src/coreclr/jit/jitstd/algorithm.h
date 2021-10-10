// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#pragma once

namespace jitstd
{

namespace
{
// Sort the elements in range [first, last] using insertion sort, an efficient alternative
// to quick sort when the range to be sorted is small enough.
template <typename RandomAccessIterator, typename Less>
void insertion_sort(RandomAccessIterator first, RandomAccessIterator last, Less less)
{
    for (RandomAccessIterator i = first; i < last; ++i)
    {
        RandomAccessIterator j = i;
        auto temp = *(j + 1);

        for (; (j >= first) && less(temp, *j); --j)
        {
            *(j + 1) = *j;
        }

        *(j + 1) = temp;
    }
}

// Sort the elements in range [first, last] using quick sort.
template <typename RandomAccessIterator, typename Less>
void quick_sort(RandomAccessIterator first, RandomAccessIterator last, Less less)
{
    // Avoid real recursion as it can be slower, at least due to the extra "less"
    // parameter that needs to be passed around. It's also likely to need more
    // stack space. Use "manual" tail recursion for one partition and push the other
    // partition on a stack. Assuming a proper implementation (sorting the smaller
    // partition using tail recursion and push the larger partition) then the
    // maximum stack depth should not exceed log2(n). So a depth of 32 should be
    // more than enough to sort INT32_MAX elements which is then far more than the
    // JIT should ever need.
    RandomAccessIterator firstStack[32];
    RandomAccessIterator lastStack[32];
    size_t depth = 0;

    for (;;)
    {
        size_t count = (last - first) + 1;

        // Switch to insertion sort if we have only a few elements to sort.
        if (count <= 8)
        {
            insertion_sort(first, last, less);

            if (depth == 0)
            {
                // If there's nothing left on the stack then we're done.
                break;
            }

            depth--;
            first = firstStack[depth];
            last = lastStack[depth];
            continue;
        }

        RandomAccessIterator pivot = first + count / 2;

        // The usual median of 3 pivot, we'll have *first <= *pivot <= *last.
        if (less(*pivot, *first))
        {
            std::swap(*pivot, *first);
        }

        if (less(*last, *pivot))
        {
            std::swap(*pivot, *last);

            if (less(*pivot, *first))
            {
                std::swap(*pivot, *first);
            }
        }

        // Partition the [first, last] range into [first, newLast) and [newLast, last].
        // Note that first and last have alreay been partitioned so the loops below
        // start by moving the iterator to the next position of interest.
        RandomAccessIterator newFirst = first;
        RandomAccessIterator newLast = last;

        for (;;)
        {
            // Find newFirst such that *newFirst >= *pivot.
            //
            // less(*pivot, *pivot) is expected to return false so we should stop
            // if we reach the pivot. However, the current JIT uses of std::sort
            // have relatively expensive "less" predicates while the iterators are
            // just pointers so they are cheap to compare. Thus it's best to check
            // for the newFirst == pivot case and skip a "less" call.
            //
            // It's not possible for newFirst to go past the end of the sort range:
            //   - If newFirst reaches the pivot before newLast then the pivot is
            //     swapped to the right and we'll stop again when we reach it.
            //   - If newLast reaches the pivot before newFirst then the pivot is
            //     swapped to the left and the value at newFirst will take its place
            //     to the right so less(newFirst, pivot) will again be false when the
            //     old pivot's position is reached.
            do
            {
                ++newFirst;
            } while ((newFirst != pivot) && less(*newFirst, *pivot));

            // Find newLast such that *newLast <= *pivot.
            //
            // Like above, this stops when the pivot is reached and also does not
            // go before the start of the sort range.
            do
            {
                --newLast;
            } while ((newLast != pivot) && less(*pivot, *newLast));

            // If newFirst reaches newLast then we're done.
            if (newFirst >= newLast)
            {
                break;
            }

            // We now have *newLast <= *pivot <= *newFirst so we need to swap
            // *newFirst and *newLast.
            std::swap(*newFirst, *newLast);

            // pivot is an iterator and not the actual value, if the value gets
            // swapped we need to update the iterator to point to the new place.
            if (pivot == newFirst)
            {
                pivot = newLast;
            }
            else if (pivot == newLast)
            {
                pivot = newFirst;
            }
        }

        RandomAccessIterator leftFirst = first;
        RandomAccessIterator leftLast = newLast;
        RandomAccessIterator rightFirst = newLast + 1;
        RandomAccessIterator rightLast = last;

        assert(depth < _countof(firstStack));

        // Ideally, the 2 partitions should have the same size, that would guarantee
        // log2(n) stack space. If that's not the case then push the larger partition
        // onto the stack and sort the smaller one using "manual" tail recursion.
        if ((leftLast - leftFirst) < (rightLast - rightFirst))
        {
            firstStack[depth] = rightFirst;
            lastStack[depth] = rightLast;

            first = leftFirst;
            last = leftLast;
        }
        else
        {
            firstStack[depth] = leftFirst;
            lastStack[depth] = leftLast;

            first = rightFirst;
            last = rightLast;
        }

        depth++;
    }
}
}

// Sort the elements in range [first, last) in ascending order, where the order
// is defined by the specified "less" predicate. This implementation does not
// use a stable sort algorithm.
template<typename RandomAccessIterator, typename Less>
void sort(RandomAccessIterator first, RandomAccessIterator last, Less less)
{
    assert(first <= last);
    assert((last - first) < INT32_MAX);

    if (first != last)
    {
        // For convenience, quick_sort sorts the [first, last] range
        // so "last" needs to be adjusted accordingly.
        quick_sort(first, last - 1, less);

#ifdef DEBUG
        for (RandomAccessIterator i = first; i != last - 1; ++i)
        {
            assert(!less(*(first + 1), *first));
        }
#endif // DEBUG
    }
}
}
