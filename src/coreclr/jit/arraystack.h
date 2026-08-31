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
        INDEBUG(m_version = 0);
    }

    void Push(T item)
    {
        if (tosIndex == maxIndex)
        {
            Realloc();
        }

        data[tosIndex] = item;
        tosIndex++;
        INDEBUG(m_version++);
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
        INDEBUG(m_version++);
    }

    T Pop()
    {
        assert(tosIndex > 0);
        tosIndex--;
        INDEBUG(m_version++);
        return data[tosIndex];
    }

    // Pop `count` elements from the stack
    void Pop(int count)
    {
        assert(tosIndex >= count);
        tosIndex -= count;
        INDEBUG(m_version++);
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
        INDEBUG(m_version++);
    }

    T* Data()
    {
        return data;
    }

    // Reverse iterator for top-down traversal.
    class ReverseIterator
    {
        T* m_ptr;

    public:
        ReverseIterator(T* ptr)
            : m_ptr(ptr)
        {
        }

        T& operator*() const
        {
            return *(m_ptr - 1);
        }

        ReverseIterator& operator++()
        {
            --m_ptr;
            return *this;
        }

        bool operator!=(const ReverseIterator& other) const
        {
            return m_ptr != other.m_ptr;
        }
    };

    // Iterable view for bottom-to-top traversal (Bottom(0) -> Top(0)).
    class BottomUpView
    {
        T* m_begin;
        T* m_end;
        INDEBUG(unsigned m_version);
        INDEBUG(const unsigned* m_pStackVersion);

    public:
        BottomUpView(T* begin, T* end DEBUGARG(unsigned version) DEBUGARG(const unsigned* pStackVersion))
            : m_begin(begin)
            , m_end(end)
        {
            INDEBUG(m_version = version);
            INDEBUG(m_pStackVersion = pStackVersion);
        }

#ifdef DEBUG
        ~BottomUpView()
        {
            assert(m_version == *m_pStackVersion && "ArrayStack was modified during BottomUpOrder iteration");
        }
#endif

        T* begin() const
        {
            return m_begin;
        }

        T* end() const
        {
            return m_end;
        }
    };

    // Iterable view for top-to-bottom traversal (Top(0) -> Bottom(0)).
    class TopDownView
    {
        T* m_begin;
        T* m_end;
        INDEBUG(unsigned m_version);
        INDEBUG(const unsigned* m_pStackVersion);

    public:
        TopDownView(T* begin, T* end DEBUGARG(unsigned version) DEBUGARG(const unsigned* pStackVersion))
            : m_begin(begin)
            , m_end(end)
        {
            INDEBUG(m_version = version);
            INDEBUG(m_pStackVersion = pStackVersion);
        }

#ifdef DEBUG
        ~TopDownView()
        {
            assert(m_version == *m_pStackVersion && "ArrayStack was modified during TopDownOrder iteration");
        }
#endif

        ReverseIterator begin() const
        {
            return ReverseIterator(m_begin);
        }

        ReverseIterator end() const
        {
            return ReverseIterator(m_end);
        }
    };

    BottomUpView BottomUpOrder()
    {
        return BottomUpView(data, data + tosIndex DEBUGARG(m_version) DEBUGARG(&m_version));
    }

    TopDownView TopDownOrder()
    {
        return TopDownView(data + tosIndex, data DEBUGARG(m_version) DEBUGARG(&m_version));
    }

private:
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

    CompAllocator m_alloc;
    int           tosIndex; // first free location
    int           maxIndex;
    T*            data;
    // initial allocation
    char builtinData[builtinSize * sizeof(T)];
    INDEBUG(unsigned m_version);
};
