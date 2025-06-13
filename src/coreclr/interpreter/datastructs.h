// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _DATASTRUCTS_H_
#define _DATASTRUCTS_H_

template <typename T>
class TArray
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

    void Grow(int32_t minNewCapacity)
    {
        if (m_capacity)
            m_capacity *= 2;
        else
            m_capacity = 16;

        m_capacity = (m_capacity > minNewCapacity) ? m_capacity : minNewCapacity;

        m_array = (T*)realloc(m_array, m_capacity * sizeof(T));
    }
public:
    TArray()
    {
        m_size = 0;
        m_capacity = 0;
        m_array = NULL;
    }

    // Implicit copies are not permitted to prevent accidental allocation of large arrays.
    TArray(const TArray<T> &other) = delete;
    TArray<T>& operator=(const TArray<T> &other) = delete;

    TArray(TArray<T> &&other)
    {
        m_size = other.m_size;
        m_capacity = other.m_capacity;
        m_array = other.m_array;

        other.m_size = 0;
        other.m_capacity = 0;
        other.m_array = NULL;
    }
    TArray<T>& operator=(TArray<T> &&other)
    {
        if (this != &other)
        {
            if (m_capacity > 0)
                free(m_array);

            m_size = other.m_size;
            m_capacity = other.m_capacity;
            m_array = other.m_array;

            other.m_size = 0;
            other.m_capacity = 0;
            other.m_array = NULL;
        }
        return *this;
    }

    ~TArray()
    {
        if (m_capacity > 0)
            free(m_array);
    }

    int32_t GetSize()
    {
        return m_size;
    }

    int32_t Add(T element)
    {
        if (m_size == m_capacity)
            Grow();
        m_array[m_size] = element;
        return m_size++;
    }

    void Append(const T* pElements, int32_t count)
    {
        int32_t availableCapacity = m_capacity - m_size;
        if (count > availableCapacity)
        {
            // Grow the array if there is not enough space
            Grow(count + m_size);
        }
        for (int32_t i = 0; i < count; i++)
        {
            m_array[m_size + i] = pElements[i];
        }
        m_size += count;
    }

    void GrowBy(int32_t count)
    {
        int32_t availableCapacity = m_capacity - m_size;
        if (count > availableCapacity)
        {
            // Grow the array if there is not enough space
            Grow(count + m_size);
        }
        memset(&m_array[m_size], 0, count * sizeof(T)); // Initialize new elements to zero
        m_size += count;
    }

    // Returns a pointer to the element at the specified index.
    T* GetUnderlyingArray()
    {
        return m_array;
    }

    T Get(int32_t index)
    {
        assert(index < m_size);
        return m_array[index];
    }

    int32_t Find(T element)
    {
        for (int i = 0; i < m_size; i++)
        {
            if (element == m_array[i])
                return i;
        }
        return -1;
    }

    // Assumes elements are unique
    void RemoveAt(int32_t index)
    {
        assert(index < m_size);
        m_size--;
        // Since this entry is removed, move the last entry into it
        if (m_size > 0 && index < m_size)
            m_array[index] = m_array[m_size];
    } 

    // Assumes elements are unique
    void Remove(T element)
    {
        for (int32_t i = 0; i < m_size; i++)
        {
            if (element == m_array[i])
            {
                RemoveAt(i);
                break;
            }
        }
    } 

    void Clear()
    {
        m_size = 0;
    }
};

// Singly linked list, implemented as a stack
template <typename T>
struct TSList
{
    T data;
    TSList *pNext;

    TSList(T data, TSList *pNext)
    {
        this->data = data;
        this->pNext = pNext;
    }

    static TSList* Push(TSList *head, T data)
    {
        TSList *newHead = new TSList(data, head);
        return newHead;
    }

    static TSList* Pop(TSList *head)
    {
        TSList *next = head->pNext;
        delete head;
        return next;
    }

    static void Free(TSList *head)
    {
        while (head != NULL)
            head = Pop(head);
    }
};

#endif
