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
                while ((j > 0) && (Pred(ptr + (size_t)size * j, ptr + (size_t)size * (j - 1)) < 0))
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
                if (Pred(secondPart, firstPart) < 0)
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

            memcpy(ptr, firstPart, firstPartEnd - firstPart);
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
    }
}

#ifdef NON_TEMPLATIZED_STABLE_SORT
inline void stable_sort_clr(void* ptr, size_t count, size_t size, int (comp)(const void*, const void*))
{
    auto comparator = [comp](const void* a, const void* b) { return comp(a, b); };
    templatized_sort::stable_sort((char*)ptr, count, templatized_sort::SizeOf(size), comparator);
}
#else // NON_TEMPLATIZED_STABLE_SORT
template <class T, class Pred>
inline void stable_sort_clr_template(T* ptr, size_t count, size_t size, Pred comp)
{
    if (size == sizeof(T))
    {
        templatized_sort::stable_sort((char*)ptr, count, templatized_sort::ConstSizeOf<sizeof(T)>(), comp);
    }
    else
    {
        templatized_sort::stable_sort((char*)ptr, count, templatized_sort::SizeOf(size), comp);
    }
}

#define stable_sort_clr(ptr, count, size, comp) stable_sort_clr_template(ptr, count, size, [&](const void* left, const void* right) { return comp(left, right); } )
#endif // NON_TEMPLATIZED_STABLE_SORT

#endif // STABLE_SORT_H
