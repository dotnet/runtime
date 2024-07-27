// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// ArrayStack: A stack, implemented as a growable array

template <class T>
class ArrayStack
{
    static const int builtinSize = 8;

public:
    explicit ArrayStack(CompAllocator alloc, int initialCapacity = builtinSize)
        : m_alloc(alloc)
    {
        if (initialCapacity > builtinSize)
        {
            maxIndex = initialCapacity;
            data     = m_alloc.allocate<T>(initialCapacity);
        }
        else
        {
            maxIndex = builtinSize;
            data     = reinterpret_cast<T*>(builtinData);
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

        new (&data[tosIndex], jitstd::placement_t()) T(std::forward<Args>(args)...);
        tosIndex++;
    }

    void Realloc()
    {
        // get a new chunk 2x the size of the old one
        // and copy over
        T* oldData = data;
        noway_assert(maxIndex * 2 > maxIndex);
        data = m_alloc.allocate<T>(maxIndex * 2);
        for (int i = 0; i < maxIndex; i++)
        {
            data[i] = oldData[i];
        }
        maxIndex *= 2;
    }

    T Pop()
    {
        assert(tosIndex > 0);
        tosIndex--;
        return data[tosIndex];
    }

    // Pop `count` elements from the stack
    void Pop(int count)
    {
        assert(tosIndex >= count);
        tosIndex -= count;
    }

    // Return the i'th element from the top
    T Top(int i = 0)
    {
        assert(tosIndex > i);
        return data[tosIndex - 1 - i];
    }

    // Return a reference to the i'th element from the top
    T& TopRef(int i = 0)
    {
        assert(tosIndex > i);
        return data[tosIndex - 1 - i];
    }

    int Height()
    {
        return tosIndex;
    }

    bool Empty()
    {
        return tosIndex == 0;
    }

    // Return the i'th element from the bottom
    T Bottom(int i = 0)
    {
        assert(tosIndex > i);
        return data[i];
    }

    // Return a reference to the i'th element from the bottom
    T& BottomRef(int i = 0)
    {
        assert(tosIndex > i);
        return data[i];
    }

    void Reset()
    {
        tosIndex = 0;
    }

    T* Data()
    {
        return data;
    }

private:
    CompAllocator m_alloc;
    int           tosIndex; // first free location
    int           maxIndex;
    T*            data;
    // initial allocation
    char builtinData[builtinSize * sizeof(T)];
};

template <class TItem>
class SmallArrayStack
{
private:
    union
    {
        TItem              m_inlineElements[4];
        ArrayStack<TItem>* m_ArrayStack;
    };

    unsigned m_numElements = 0;
    bool     m_heap        = false;

public:
    template <typename TArrayStackAllocator>
    void Push(TItem vn, TArrayStackAllocator allocator)
    {
        // We already switched to the heap
        if (m_heap)
        {
            m_ArrayStack->Push(vn);
            return;
        }

        // It's time to switch to the heap
        if (m_numElements == ArrLen(m_inlineElements))
        {
            ArrayStack<TItem>* arrayStack = allocator();
            for (unsigned i = 0; i < m_numElements; i++)
            {
                arrayStack->Push(m_inlineElements[i]);
            }
            arrayStack->Push(vn);
            m_ArrayStack = arrayStack;
            m_heap       = true;
        }
        else
        {
            // Use the inline array
            m_inlineElements[m_numElements++] = vn;
        }
    }

    int Height() const
    {
        return m_heap ? m_ArrayStack->Height() : m_numElements;
    }

    ValueNum Pop()
    {
        if (m_heap)
        {
            return m_ArrayStack->Pop();
        }
        assert(m_numElements > 0);
        return m_inlineElements[--m_numElements];
    }
};
