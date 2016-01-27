// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef EXPANDARRAY_H
#define EXPANDARRAY_H

#include "iallocator.h"

// An array of T that expands (and never shrinks) to accomodate references (with default value T() for
// elements newly created by expansion.)
template<class T>
class ExpandArray
{
protected:
    IAllocator* m_alloc;   // The IAllocator object that should be used to allocate members.
    T* m_members;          // Pointer to the element array.
    unsigned m_size;       // The size of "m_members".
    unsigned m_minSize;    // The minimum array size to allocate.

    // Ensures that "m_size" > "idx", and that "m_members" is at least large enough to be
    // indexed by "idx".
    void EnsureCoversInd(unsigned idx);

    // Requires that m_members is not NULL, and that
    // low <= high <= m_size.  Sets elements low to high-1 of m_members to T().
    void InitializeRange(unsigned low, unsigned high)
    {
        assert(m_members != NULL);
        assert(low <= high && high <= m_size);
        for (unsigned i = low; i < high; i++) m_members[i] = T();
    }

public:
    // Initializes "*this" to represent an empty array of size zero.
    // Use "alloc" for allocation of internal objects.  If "minSize" is specified,
    // the allocated size of the internal representation will hold at least that many
    // T's.
    ExpandArray(IAllocator* alloc, unsigned minSize = 1) : 
      m_alloc(alloc), m_members(NULL), m_size(0), m_minSize(minSize)
    {
        assert(minSize > 0);
    }

    ~ExpandArray()
    {
        if (m_members != NULL) m_alloc->Free(m_members);
    }

    // Like the constructor above, to re-initialize to the empty state.
    void Init(IAllocator* alloc, unsigned minSize = 1)
    {
        if (m_members != NULL) m_alloc->Free(m_members);
        m_alloc = alloc;
        m_members = NULL;
        m_size = 0;
        m_minSize = minSize;
    }

    // Resets "*this" to represent an array of size zero, with the given "minSize".
    void Reset(unsigned minSize)
    {
        m_minSize = minSize;
        Reset();
    }

    // Resets "*this" to represent an array of size zero, whose
    // allocated representation can represent at least "m_minSize" T's.
    void Reset()
    {
        if (m_minSize > m_size) EnsureCoversInd(m_minSize-1);
        InitializeRange(0, m_size);
    }

    // Returns the T at index "idx".  Expands the representation, if necessary,
    // to contain "idx" in its domain, so the result will be an all-zero T if
    // it had not previously been set.
    T Get(unsigned idx)
    {
        EnsureCoversInd(idx);
        return m_members[idx];
    }

    // Like "Get", but returns a reference, so suitable for use as the LHS of an assignment.
    T& GetRef(unsigned idx)
    {
        EnsureCoversInd(idx);
        return m_members[idx];
    }

    // Expands the representation, if necessary, to contain "idx" in its domain, and
    // sets the value at "idx" to "val".
    void Set(unsigned idx, T val)
    {
        EnsureCoversInd(idx);
        m_members[idx] = val;
    }

    T& operator[](unsigned idx)
    {
        EnsureCoversInd(idx);
        return m_members[idx];
    }
};

template<class T>
class ExpandArrayStack: public ExpandArray<T>
{
    unsigned m_used;

  public:
    ExpandArrayStack(IAllocator* alloc, unsigned minSize = 1) : ExpandArray<T>(alloc, minSize), m_used(0) {}

    void Set(unsigned idx, T val)
    {
        ExpandArray<T>::Set(idx, val);
        m_used = max((idx + 1), m_used);
    }

    // Resets "*this" to represent an array of size zero, whose
    // allocated representation can represent at least "m_minSize" T's.
    void Reset()
    {
        ExpandArray<T>::Reset();
        m_used = 0;
    }

    // Returns the index at which "val" is stored.
    unsigned Push(T val)
    {
        unsigned res = m_used;
        ExpandArray<T>::Set(m_used, val);
        m_used++;
        return res;
    }

    // Requires Size() > 0
    T Pop()
    {
        assert(Size() > 0);
        m_used--;
        return this->m_members[m_used];
    }

    // Requires Size() > 0
    T Top()
    {
        assert(Size() > 0);
        return this->m_members[m_used-1];
    }

    // Requires that "idx" < "m_used" (asserting this in debug), and returns
    // "Get(idx)" (which is covered, by the invariant that all indices in "[0..m_used)" are
    // covered).
    T GetNoExpand(unsigned idx)
    {
        assert(idx < m_used);
        return this->m_members[idx];
    }

    // Requires that "idx" < "m_used" (asserting this in debug).
    // Removes the element at "idx" and shifts contents of the array beyond "idx", if any,
    // to occupy the free slot created at "idx".
    // O(n) worst case operation, no memory is allocated.
    void Remove(unsigned idx)
    {
        assert(idx < m_used);
        if (idx < m_used - 1)
        {
            memmove(&this->m_members[idx], &this->m_members[idx + 1], (m_used - idx - 1) * sizeof(T));
        }
        m_used--;
    }

    unsigned Size() { return m_used; }
};

template<class T>
void ExpandArray<T>::EnsureCoversInd(unsigned idx)
{
    if (idx >= m_size)
    {
        unsigned oldSize = m_size;
        T* oldMembers = m_members;
        m_size = max(idx + 1,  max(m_minSize, m_size * 2));
        if (sizeof(T) < sizeof(int))
        {
            m_members = (T*)m_alloc->ArrayAlloc(ALIGN_UP(m_size*sizeof(T), sizeof(int)), sizeof(BYTE));
        }
        else
        {
            m_members = (T*)m_alloc->ArrayAlloc(m_size, sizeof(T));
        }
        if (oldMembers != NULL)
        {
            memcpy(m_members, oldMembers, oldSize * sizeof(T));
            m_alloc->Free(oldMembers);
        }
        InitializeRange(oldSize, m_size);
    }
}

#endif // EXPANDARRAY_H
