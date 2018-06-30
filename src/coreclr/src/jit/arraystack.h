// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// ArrayStack: A stack, implemented as a growable array

template <class T>
class ArrayStack
{
    static const int builtinSize = 8;

public:
    ArrayStack(CompAllocator alloc, int initialSize = builtinSize) : m_alloc(alloc)
    {
        if (initialSize > builtinSize)
        {
            maxIndex = initialSize;
            data     = new (alloc) T[initialSize];
        }
        else
        {
            maxIndex = builtinSize;
            data     = builtinData;
        }

        tosIndex = 0;
    }

    void Push(T item)
    {
        if (tosIndex == maxIndex)
        {
            Realloc();
        }

        data[tosIndex] = item;
        tosIndex++;
    }

    template <typename... Args>
    void Emplace(Args&&... args)
    {
        if (tosIndex == maxIndex)
        {
            Realloc();
        }

        new (&data[tosIndex], jitstd::placement_t()) T(jitstd::forward<Args>(args)...);
        tosIndex++;
    }

    void Realloc()
    {
        // get a new chunk 2x the size of the old one
        // and copy over
        T* oldData = data;
        noway_assert(maxIndex * 2 > maxIndex);
        data = new (m_alloc) T[maxIndex * 2];
        for (int i = 0; i < maxIndex; i++)
        {
            data[i] = oldData[i];
        }
        maxIndex *= 2;
    }

    // reverse the top N in the stack
    void ReverseTop(int number)
    {
        if (number < 2)
        {
            return;
        }

        assert(number <= tosIndex);

        int start  = tosIndex - number;
        int offset = 0;
        while (offset < number / 2)
        {
            T   temp;
            int index        = start + offset;
            int otherIndex   = tosIndex - 1 - offset;
            temp             = data[index];
            data[index]      = data[otherIndex];
            data[otherIndex] = temp;

            offset++;
        }
    }

    T Pop()
    {
        assert(tosIndex > 0);
        tosIndex--;
        return data[tosIndex];
    }

    T Top()
    {
        assert(tosIndex > 0);
        return data[tosIndex - 1];
    }

    T& TopRef()
    {
        assert(tosIndex > 0);
        return data[tosIndex - 1];
    }

    // return the i'th from the top
    T Index(int idx)
    {
        assert(tosIndex > idx);
        return data[tosIndex - 1 - idx];
    }

    // return a reference to the i'th from the top
    T& IndexRef(int idx)
    {
        assert(tosIndex > idx);
        return data[tosIndex - 1 - idx];
    }

    int Height()
    {
        return tosIndex;
    }

    // return the bottom of the stack
    T Bottom()
    {
        assert(tosIndex > 0);
        return data[0];
    }

    // return the i'th from the bottom
    T Bottom(int indx)
    {
        assert(tosIndex > indx);
        return data[indx];
    }

    void Reset()
    {
        tosIndex = 0;
    }

private:
    CompAllocator m_alloc;
    int           tosIndex; // first free location
    int           maxIndex;
    T*            data;
    // initial allocation
    T builtinData[builtinSize];
};
