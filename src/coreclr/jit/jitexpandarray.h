// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

// An array of T that expands automatically (and never shrinks) to accommodate
// any index access. Elements added as a result of automatic expansion are
// value-initialized (that is, they are assigned T()).
template <class T>
class JitExpandArray
{
protected:
    CompAllocator m_alloc;   // The allocator object that should be used to allocate members.
    T*            m_members; // Pointer to the element array.
    unsigned      m_size;    // The size of the element array.
    unsigned      m_minSize; // The minimum size of the element array.

    // Ensure that the element array is large enough for the specified index to be valid.
    void EnsureCoversInd(unsigned idx);

    //------------------------------------------------------------------------
    // InitializeRange: Value-initialize the specified range of elements.
    //
    // Arguments:
    //    low  - inclusive lower bound of the range to initialize
    //    high - exclusive upper bound of the range to initialize
    //
    // Assumptions:
    //    Assumes that the element array has aready been allocated
    //    and that low and high are valid indices. The array is not
    //    expanded to accommodate invalid indices.
    //
    void InitializeRange(unsigned low, unsigned high)
    {
        assert(m_members != nullptr);
        assert((low <= high) && (high <= m_size));
        for (unsigned i = low; i < high; i++)
        {
            m_members[i] = T();
        }
    }

public:
    //------------------------------------------------------------------------
    // JitExpandArray: Construct an empty JitExpandArray object.
    //
    // Arguments:
    //    alloc   - the allocator used to allocate the element array
    //    minSize - the initial size of the element array
    //
    // Notes:
    //    Initially no memory is allocated for the element array. The first
    //    time an array element (having index `idx`) is accessed, an array
    //    of size max(`minSize`, `idx`) is allocated.
    //
    JitExpandArray(CompAllocator alloc, unsigned minSize = 1)
        : m_alloc(alloc), m_members(nullptr), m_size(0), m_minSize(minSize)
    {
        assert(minSize > 0);
    }

    //------------------------------------------------------------------------
    // ~JitExpandArray: Destruct the JitExpandArray object.
    //
    // Notes:
    //    Frees the element array. Destructors of elements stored in the
    //    array are NOT invoked.
    //
    ~JitExpandArray()
    {
        if (m_members != nullptr)
        {
            m_alloc.deallocate(m_members);
        }
    }

    //------------------------------------------------------------------------
    // Init: Re-initialize the array to the empty state.
    //
    // Arguments:
    //    alloc   - the allocator used to allocate the element array
    //    minSize - the initial size of the element array
    //
    // Notes:
    //    This is equivalent to calling the destructor and then constructing
    //    the array again.
    //
    void Init(CompAllocator alloc, unsigned minSize = 1)
    {
        if (m_members != nullptr)
        {
            m_alloc.deallocate(m_members);
        }
        m_alloc   = alloc;
        m_members = nullptr;
        m_size    = 0;
        m_minSize = minSize;
    }

    //------------------------------------------------------------------------
    // Reset: Change the minimum size and value-initialize all the elements.
    //
    // Arguments:
    //    minSize - the initial size of the element array
    //
    // Notes:
    //    Ensures that an element array of at least `minSize` elements
    //    has been allocated.
    //
    void Reset(unsigned minSize)
    {
        m_minSize = minSize;
        Reset();
    }

    //------------------------------------------------------------------------
    // Reset: Value-initialize all the array elements.
    //
    // Notes:
    //    Ensures that an element array of at least `m_minSize` elements
    //    has been allocated.
    //
    void Reset()
    {
        if (m_minSize > m_size)
        {
            EnsureCoversInd(m_minSize - 1);
        }
        InitializeRange(0, m_size);
    }

    //------------------------------------------------------------------------
    // Get: Get a copy of the element at index `idx`.
    //
    // Arguments:
    //    idx - the element index
    //
    // Return Value:
    //    A copy of the element at index `idx`.
    //
    // Notes:
    //    Expands the element array, if necessary, to contain `idx`.
    //    The result will be a value-initialized T if a value wasn't
    //    previously assigned to the specififed index.
    //
    T Get(unsigned idx)
    {
        EnsureCoversInd(idx);
        return m_members[idx];
    }

    //------------------------------------------------------------------------
    // GetRef: Get a reference to the element at index `idx`.
    //
    // Arguments:
    //    idx - the element index
    //
    // Return Value:
    //    A reference to the element at index `idx`.
    //
    // Notes:
    //    Like `Get`, but returns a reference, so suitable for use as
    //    the LHS of an assignment.
    //
    T& GetRef(unsigned idx)
    {
        EnsureCoversInd(idx);
        return m_members[idx];
    }

    //------------------------------------------------------------------------
    // Set: Assign a copy of `val` to the element at index `idx`.
    //
    // Arguments:
    //    idx - the element index
    //    val - the value to assign
    //
    // Notes:
    //    Expands the element array, if necessary, to contain `idx`.
    //
    void Set(unsigned idx, T val)
    {
        EnsureCoversInd(idx);
        m_members[idx] = val;
    }

    //------------------------------------------------------------------------
    // operator[]: Get a reference to the element at index `idx`.
    //
    // Arguments:
    //    idx - the element index
    //
    // Return Value:
    //    A reference to the element at index `idx`.
    //
    // Notes:
    //    Same as `GetRef`.
    //
    T& operator[](unsigned idx)
    {
        EnsureCoversInd(idx);
        return m_members[idx];
    }
};

template <class T>
class JitExpandArrayStack : public JitExpandArray<T>
{
    unsigned m_used; // The stack depth

public:
    //------------------------------------------------------------------------
    // JitExpandArrayStack: Construct an empty JitExpandArrayStack object.
    //
    // Arguments:
    //    alloc   - the allocator used to allocate the element array
    //    minSize - the initial size of the element array
    //
    // Notes:
    //    See JitExpandArray constructor notes.
    //
    JitExpandArrayStack(CompAllocator alloc, unsigned minSize = 1) : JitExpandArray<T>(alloc, minSize), m_used(0)
    {
    }

    //------------------------------------------------------------------------
    // GetRef: Get a reference to the element at index `idx`.
    //
    // Arguments:
    //    idx - the element index
    //
    // Return Value:
    //    A reference to the element at index `idx`.
    //
    // Notes:
    //    Like `Get`, but returns a reference, so suitable for use as
    //    the LHS of an assignment.
    //
    T& GetRef(unsigned idx)
    {
        T& itemRef = JitExpandArray<T>::GetRef(idx);
        m_used     = max((idx + 1), m_used);
        return itemRef;
    }

    //------------------------------------------------------------------------
    // Set: Assign value a copy of `val` to the element at index `idx`.
    //
    // Arguments:
    //    idx - the index of element
    //    val - the value to assign
    //
    // Notes:
    //    Expands the element array, if necessary, to contain `idx`.
    //    If `idx` is larger than the current stack depth then this
    //    is the equivalent of series of Push(T()) followed by a Push(val).
    //
    void Set(unsigned idx, T val)
    {
        JitExpandArray<T>::Set(idx, val);
        m_used = max((idx + 1), m_used);
    }

    //------------------------------------------------------------------------
    // Reset: Remove all the elements from the stack.
    //
    void Reset()
    {
        JitExpandArray<T>::Reset();
        m_used = 0;
    }

    //------------------------------------------------------------------------
    // Push: Push a copy of the specified value onto the stack.
    //
    // Arguments:
    //    val - the value
    //
    // Return Value:
    //    The index of the pushed value.
    //
    unsigned Push(T val)
    {
        unsigned res = m_used;
        JitExpandArray<T>::Set(m_used, val);
        m_used++;
        return res;
    }

    //------------------------------------------------------------------------
    // Pop: Remove the top element of the stack.
    //
    // Return Value:
    //    A copy of the removed element.
    //
    // Assumptions:
    //    The stack must not be empty.
    //
    T Pop()
    {
        assert(Size() > 0);
        m_used--;
        return this->m_members[m_used];
    }

    //------------------------------------------------------------------------
    // Top: Get a copy of the top element.
    //
    // Return Value:
    //    A copy of the top element.
    //
    // Assumptions:
    //    The stack must not be empty.
    //
    T Top() const
    {
        assert(Size() > 0);
        return this->m_members[m_used - 1];
    }

    //------------------------------------------------------------------------
    // TopRef: Get a reference to the top element.
    //
    // Return Value:
    //    A reference to the top element.
    //
    // Assumptions:
    //    The stack must not be empty.
    //
    T& TopRef()
    {
        assert(Size() > 0);
        return this->m_members[m_used - 1];
    }

    //------------------------------------------------------------------------
    // GetNoExpand: Get a copy of the element at index `idx`.
    //
    // Arguments:
    //    idx - the element index
    //
    // Return Value:
    //    A copy of the element at index `idx`.
    //
    // Notes:
    //    Unlike `Get` this does not expand the array if the index is not valid.
    //
    // Assumptions:
    //    The element index does not exceed the current stack depth.
    //
    T GetNoExpand(unsigned idx) const
    {
        assert(idx < m_used);
        return this->m_members[idx];
    }

    //------------------------------------------------------------------------
    // GetRefNoExpand: Get a reference to the element at index `idx`.
    //
    // Arguments:
    //    idx - the element index
    //
    // Return Value:
    //    A reference to the element at index `idx`.
    //
    // Notes:
    //    Unlike `GetRef` this does not expand the array if the index is not valid.
    //
    // Assumptions:
    //    The element index does not exceed the current stack depth.
    //
    T& GetRefNoExpand(unsigned idx)
    {
        assert(idx < m_used);
        return this->m_members[idx];
    }

    //------------------------------------------------------------------------
    // Remove: Remove the element at index `idx`.
    //
    // Arguments:
    //    idx - the element index
    //
    // Notes:
    //    Shifts contents of the array beyond `idx`, if any, to occupy the free
    //    slot created at `idx`. O(n) worst case operation, no memory is allocated.
    //    Elements are bitwise copied, copy constructors are NOT invoked.
    //
    // Assumptions:
    //    The element index does not exceed the current stack depth.
    //
    void Remove(unsigned idx)
    {
        assert(idx < m_used);
        if (idx < m_used - 1)
        {
            memmove(&this->m_members[idx], &this->m_members[idx + 1], (m_used - idx - 1) * sizeof(T));
        }
        m_used--;
    }

    //------------------------------------------------------------------------
    // Size: Get the current stack depth.
    //
    // Return Value:
    //    The stack depth.
    //
    unsigned Size() const
    {
        return m_used;
    }
};

//------------------------------------------------------------------------
// EnsureCoversInd: Ensure that the array is large enough for the specified
// index to be valid.
//
// Arguments:
//    idx - the element index
//
// Notes:
//    If the array is expanded then
//      - the existing elements are bitwise copied (copy constructors are NOT invoked)
//      - the newly added elements are value-initialized
//
template <class T>
void JitExpandArray<T>::EnsureCoversInd(unsigned idx)
{
    if (idx >= m_size)
    {
        unsigned oldSize    = m_size;
        T*       oldMembers = m_members;
        m_size              = max(idx + 1, max(m_minSize, m_size * 2));
        m_members           = m_alloc.allocate<T>(m_size);
        if (oldMembers != nullptr)
        {
            memcpy(m_members, oldMembers, oldSize * sizeof(T));
            m_alloc.deallocate(oldMembers);
        }
        InitializeRange(oldSize, m_size);
    }
}
