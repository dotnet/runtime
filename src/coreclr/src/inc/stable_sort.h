// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <string.h>

#ifndef STABLE_SORT_H
#define STABLE_SORT_H

namespace templatized_sort
{
    namespace
    {
        class SizeOf
        {
            size_t typeSize;
        public:
            SizeOf(size_t typeSize)
            {
                this->typeSize = typeSize;
            }

            operator size_t() const { return typeSize; }
        };

        template<unsigned Size>
        class ConstSizeOf
        {
        public:
            operator size_t() const { return Size; }
        };

        template <unsigned Size>
        void swap(typename ConstSizeOf<Size> size, char* first, char* second)
        {
            char tempSpace[Size];
            memcpy(tempSpace, first, Size);
            memcpy(first, second, Size);
            memcpy(second, tempSpace, Size);
        }

        template <class SizeOf_t>
        void swap(SizeOf_t size, char* first, char* second)
        {
            size_t sizeLocal = (size_t)size;
            do
            {
                char tmp = *first;
                *first = *second;
                *second = tmp;
                first++;
                second++;
                sizeLocal--;
            } while (sizeLocal > 0);
        }

        template <class SizeOf_t, class Pr>
        void insertion_sort(char* ptr, size_t count, SizeOf_t size, Pr Pred)
        {
            size_t i = 1;
            while (i < count)
            {
                size_t j = i;
                while ((j > 0) && (Pred(ptr + (size_t)size * j, ptr + (size_t)size * (j - 1))))
                {
                    swap(size, ptr + (size_t)size * j, ptr + (size_t)size * (j - 1));
                    j = j - 1;
                }
                i = i + 1;
            }
        }

        template <class SizeOf_t, class Pr>
        void merge_sort_worker(char* ptr, size_t count, SizeOf_t size, Pr Pred, char* working)
        {
            if (count <= 1)
                return;

            if (count <= 4)
            {
                insertion_sort((char*)ptr, count, size, Pred);
                return;
            }

            size_t half = count - count / 2;
            merge_sort_worker(ptr, half, size, Pred, working);
            merge_sort_worker(ptr + (size_t)size * half, count - half, size, Pred, working);
            memcpy(working, ptr, (size_t)size * half);

            char* firstPart = working;
            char* firstPartEnd = working + (size_t)size * half;
            char* secondPart = ptr + (size_t)size * half;
            char* secondPartEnd = ptr + (size_t)size * count;
            while (firstPart < firstPartEnd && secondPart < secondPartEnd)
            {
                if (Pred(secondPart, firstPart))
                {
                    memcpy(ptr, secondPart, (size_t)size);
                    secondPart += (size_t)size;
                }
                else
                {
                    memcpy(ptr, firstPart, (size_t)size);
                    firstPart += (size_t)size;
                }
                ptr += (size_t)size;
            }

            while (firstPart < firstPartEnd)
            {
                memcpy(ptr, firstPart, (size_t)size);
                firstPart += (size_t)size;
                ptr += (size_t)size;
            }
        }

        template <class SizeOf_t, class Pr>
        void stable_sort(void* ptr, size_t count, SizeOf_t size, Pr Pred) {
            size_t half = count - count / 2;
            double tempBufferLocal[256];
            size_t tempBufferSize = half * (size_t)size;
            char* tempBuffer;
            if (tempBufferSize <= sizeof(tempBufferLocal))
            {
                tempBuffer = (char*)&tempBufferLocal[0];
            }
            else
            {
                tempBuffer = (char*)malloc(tempBufferSize);
                if (tempBuffer == nullptr)
                {
                    insertion_sort((char*)ptr, count, size, Pred);
                    return;
                }
            }

            merge_sort_worker((char*)ptr, count, size, Pred, tempBuffer);
            if (tempBuffer != (char*)&tempBufferLocal[0])
            {
                free(tempBuffer);
            }
        }


        // Sort the elements in range [first, last] using quick sort.
        template <class SizeOf_t, class Pr>
        void quick_sort(void* ptrInput, size_t count, SizeOf_t size, Pr Pred)
        {
            char* ptr = (char*)ptrInput;
            // Avoid real recursion as it can be slower, at least due to the extra "less"
            // parameter that needs to be passed around. It's also likely to need more
            // stack space. Use "manual" tail recursion for one partition and push the other
            // partition on a stack. Assuming a proper implementation (sorting the smaller
            // partition using tail recursion and push the larger partition) then the
            // maximum stack depth should not exceed log2(n). So a depth of 32 should be
            // more than enough to sort INT32_MAX elements which is then far more than the
            // JIT should ever need.
            char* firstStack[32];
            size_t countStack[32];
            char* first = ptr;
            size_t depth = 0;

            for (;;)
            {
                // Switch to insertion sort if we have only a few elements to sort.
                if (count <= 4)
                {
                    insertion_sort(ptr, count, size, Pred);

                    if (depth == 0)
                    {
                        // If there's nothing left on the stack then we're done.
                        break;
                    }

                    depth--;
                    first = firstStack[depth];
                    count = countStack[depth];
                    continue;
                }

                size_t pivotIndex = (count / 2);
                char* pivot = first + pivotIndex * (size_t)size;
                char* last = first + count * (size_t)size;

                // The usual median of 3 pivot, we'll have *first <= *pivot <= *last.
                if (Pred(pivot, first))
                {
                    swap(size, pivot, first);
                }

                if (Pred(last, pivot))
                {
                    swap(size, pivot, last);

                    if (Pred(pivot, first))
                    {
                        swap(size, pivot, first);
                    }
                }

                // Partition the [first, last] range into [first, newLast) and [newLast, last].
                // Note that first and last have alreay been partitioned so the loops below
                // start by moving the iterator to the next position of interest.
                char* newFirst = first;
                char* newLast = last;
                size_t newLastCount = 0;

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
                    //	   swapped to the right and we'll stop again when we reach it.
                    //   - If newLast reaches the pivot before newFirst then the pivot is
                    //	   swapped to the left and the value at newFirst will take its place
                    //     to the right so less(newFirst, pivot) will again be false when the
                    //     old pivot's position is reached.
                    do
                    {
                        newFirst += (size_t)size;
                    } while ((newFirst != pivot) && Pred(newFirst, pivot));

                    // Find newLast such that *newLast <= *pivot.
                    //
                    // Like above, this stops when the pivot is reached and also does not
                    // go before the start of the sort range.
                    do
                    {
                        newLast -= (size_t)size;
                        newLastCount++;
                    } while ((newLast != pivot) && Pred(pivot, newLast));

                    // If newFirst reaches newLast then we're done.
                    if (newFirst >= newLast)
                    {
                        break;
                    }

                    // We now have *newLast <= *pivot <= *newFirst so we need to swap
                    // *newFirst and *newLast.
                    swap(size, newFirst, newLast);

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

                char* leftFirst = first;
                char* leftLast = newLast;
                char* rightFirst = newLast + 1;

                assert(depth < _countof(firstStack));

                // Ideally, the 2 partitions should have the same size, that would guarantee
                // log2(n) stack space. If that's not the case then push the larger partition
                // onto the stack and sort the smaller one using "manual" tail recursion.
                if ((count - newLastCount) < (newLastCount))
                {
                    firstStack[depth] = rightFirst;
                    countStack[depth] = newLastCount;

                    first = leftFirst;
                    count = count - newLastCount;
                }
                else
                {
                    firstStack[depth] = leftFirst;
                    countStack[depth] = count - newLastCount;

                    first = rightFirst;
                    count = newLastCount;
                }

                depth++;
            }
        }
    }
}

inline void stable_sort(void* ptr, size_t count, size_t size, int (comp)(const void*, const void*))
{
    auto comparator = [comp](const void* a, const void* b) { return comp(a, b) < 0; };
    templatized_sort::stable_sort((char*)ptr, count, templatized_sort::SizeOf(size), comparator);
}

inline void qsort_clr(void* ptr, size_t count, size_t size, int (comp)(const void*, const void*))
{
    auto comparator = [comp](const void* a, const void* b) { return comp(a, b) < 0; };
    templatized_sort::quick_sort((char*)ptr, count, templatized_sort::SizeOf(size), comparator);
}

#endif // STABLE_SORT_H
