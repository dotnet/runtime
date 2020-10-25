// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <string.h>

namespace templatized_sort
{
    inline void swap(size_t size, char* first, char* second)
    {
        char tempSpace[128];
        while (size >= 0)
        {
            size_t swapamount = size > sizeof(tempSpace) ? sizeof(tempSpace): size;

            memcpy(tempSpace, first, swapamount);
            memcpy(first, second, swapamount);
            memcpy(second, tempSpace, swapamount);
            first += swapamount;
            second += swapamount;
            size -= swapamount;
        }
    }

    template <class SizeOf_t>
    void swap(SizeOf_t sizeOfValueType, char* first, char* second) 
    {
        char tempSpace[256];
        if (sizeOfValueType.Value() <= sizeof(tempSpace))
        {
            memcpy(tempSpace, first, sizeOfValueType.Value());
            memcpy(first, second, sizeOfValueType.Value());
            memcpy(second, tempSpace, sizeOfValueType.Value());
        }
        else
        {
            swap(sizeOfValueType.Value(), first, second);
        }
    }

    template <class SizeOf_t, class Pr>
    void insertion_sort(char* ptr, size_t count, SizeOf_t size, Pr Pred)
    {
        size_t i = 1;
        while (i < count)
        {
            size_t j = i;
            while ((j > 0) && (Pred(ptr + size.Value() * j, ptr + size.Value() * (j - 1))))
            {
                swap(size, ptr + size.Value() * j, ptr + size.Value() * (j - 1));
                j = j - 1;
            }
            i = i + 1;
        }
    }

    template <class SizeOf_t, class Pr>
    void merge_sort_worker(char*ptr, size_t count, SizeOf_t size, Pr Pred, char* working)
    {
        if (count <= 1)
            return;

        size_t half = count - count / 2;
        merge_sort_worker(ptr, half, size, Pred, working);
        merge_sort_worker(ptr + size.Value() * half, count - half, size, Pred, working);
        memcpy(working, ptr, size.Value() * half);

        char* firstPart = working;
        char* firstPartEnd = working + size.Value() * half;
        char* secondPart = ptr + size.Value() * half;
        char* secondPartEnd = ptr + size.Value() * count;
        while (firstPart < firstPartEnd && secondPart < secondPartEnd)
        {
            if (Pred(secondPart, firstPart))
            {
                memcpy(ptr, secondPart, size.Value());
                secondPart += size.Value();
            }
            else
            {
                memcpy(ptr, firstPart, size.Value());
                firstPart += size.Value();
            }
            ptr += size.Value();
        }

        while (firstPart < firstPartEnd)
        {
            memcpy(ptr, firstPart, size.Value());
            firstPart += size.Value();
            ptr += size.Value();
        }

        while (secondPart < secondPartEnd)
        {
            memcpy(ptr, secondPart, size.Value());
            secondPart += size.Value();
            ptr += size.Value();
        }
    }

    template <class SizeOf_t, class Pr>
    void stable_sort(void* ptr, size_t count, SizeOf_t size, Pr Pred) {
        size_t half = count - count / 2;
        double tempBufferLocal[256];
        size_t tempBufferSize = half * size.Value();
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

    class SizeOf
    {
        size_t typeSize;
    public:
        SizeOf(size_t typeSize)
        {
            this->typeSize = typeSize;
        }

        size_t Value()
        {
            return typeSize;
        }
    };

}

inline void stable_sort(void* ptr, size_t count, size_t size, int (comp)(const void*, const void*))
{
    auto comparator = [comp](const void* a, const void* b) { return comp(a, b) < 0; };
    templatized_sort::stable_sort((char*)ptr, count, templatized_sort::SizeOf(size), comparator);
}
