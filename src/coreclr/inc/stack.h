// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// This is a generic growable stack of T.

#ifndef GENERIC_STACK_H
#define GENERIC_STACK_H 1

#include <math.h>

template<typename T>
class Stack
{
    T* m_elems;
    unsigned m_elemsSize;
    unsigned m_elemsCount;

    static const unsigned InitSize = 8;

    void GrowForPush()
    {
        if (m_elemsCount == m_elemsSize)
        {
            m_elemsSize = max(InitSize, 2*m_elemsSize);
            T* newElems = new T[m_elemsSize];
            if (m_elemsCount != 0)
            {
                _ASSERTE(m_elems != NULL);
                for (unsigned k = 0; k < m_elemsCount; k++) newElems[k] = m_elems[k];
                delete[] m_elems;
            }
            m_elems = newElems;
        }
    }

  public:
    Stack(unsigned sz = 0) : m_elems(NULL), m_elemsSize(sz), m_elemsCount(0)
    {
        if (sz > 0)
        {
            m_elems = new T[sz];
        }
    }

    ~Stack()
    {
        if (m_elems != NULL) delete[] m_elems;
    }

    void Push(T t)
    {
        GrowForPush();
        m_elems[m_elemsCount] = t;
        m_elemsCount++;
    }

    bool IsEmpty()
    {
        return m_elemsCount == 0;
    }

    T Pop()
    {
        _ASSERTE(m_elemsCount > 0);
        m_elemsCount--;
        return m_elems[m_elemsCount];
    }

    T Peek()
    {
        _ASSERTE(m_elemsCount > 0);
        return m_elems[m_elemsCount-1];
    }

    // Caller should take care to only side-effect the return reference if they are *sure*
    // that the stack will not be popped in the interim!
    T& PeekRef()
    {
        _ASSERTE(m_elemsCount > 0);
        return m_elems[m_elemsCount-1];
    }
};

#endif // GENERIC_STACK_H
