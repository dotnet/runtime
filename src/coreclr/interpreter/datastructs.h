// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _DATASTRUCTS_H_
#define _DATASTRUCTS_H_

template <typename T>
class PtrArray
{
private:
    int32_t m_size, m_capacity;
    T *m_array;

    void Grow()
    {
        if (m_capacity)
            m_capacity *= 2;
        else
            m_capacity = 16;

        m_array = (T*)realloc(m_array, m_capacity * sizeof(T));
    }
public:
    PtrArray()
    {
        m_size = 0;
        m_capacity = 0;
        m_array = NULL;
    }

    ~PtrArray()
    {
        if (m_capacity > 0)
            free(m_array);
    }

    int32_t GetSize()
    {
        return m_size;
    }

    void Add(T element)
    {
        if (m_size == m_capacity)
            Grow();
        m_array[m_size] = element;
        m_size++;
    }

    T Get(int32_t index)
    {
        assert(index < m_size);
        return m_array[index];
    }
};

#endif
