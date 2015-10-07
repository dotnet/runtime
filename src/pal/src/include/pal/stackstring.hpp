// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

#ifndef __STACKSTRING_H_
#define __STACKSTRING_H_

#include "pal/malloc.hpp"

template <SIZE_T STACKCOUNT, class T>
class StackString
{
private:
    T m_innerBuffer[STACKCOUNT + 1];
    T * m_buffer;
    SIZE_T m_size; // actual allocated size
    SIZE_T m_count; // actual length of string

    void NullTerminate()
    {
        m_buffer[m_count] = 0;
    }

    void DeleteBuffer()
    {
        if (m_innerBuffer != m_buffer)
            PAL_free(m_buffer);

        m_buffer = NULL;
        return;
    }

    void ReallocateBuffer(SIZE_T count)
    {
        // count is always > STACKCOUNT here.
        T * newBuffer = (T *)PAL_malloc((count + 1) * sizeof(T));
        if (NULL == newBuffer)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);

            DeleteBuffer();
            m_count = 0;

            return;
        }

        DeleteBuffer();
        m_buffer = newBuffer;
        m_count = count;
        m_size = count+1;

        return;
    }

    void Resize(SIZE_T count)
    {
        if (NULL == m_buffer)
        {
            if (count > STACKCOUNT)
            {
                ReallocateBuffer(count);
            }
            else
            {
                m_size = STACKCOUNT+1;
                m_buffer = m_innerBuffer;
                m_count = count;
            }
        }
        else if (m_innerBuffer == m_buffer)
        {
            if (count > STACKCOUNT)
            {
                ReallocateBuffer(count);
            }
            else
            {
                m_count = count;
                m_size = STACKCOUNT+1;
            }
        }
        else
        {
            ReallocateBuffer(count);
        }

        return;
    }

    StackString(const StackString &s)
    {
        Set(s);
    }

public:
    StackString()
        : m_buffer(m_innerBuffer), m_count(0)
    {
    }

    BOOL Set(const T * buffer, SIZE_T count)
    {
        Resize(count);
        if (NULL == m_buffer)
            return FALSE;

        CopyMemory(m_buffer, buffer, (count + 1) * sizeof(T));
        NullTerminate();
        return TRUE;
    }

    BOOL Set(const StackString &s)
    {
        return Set(s.m_buffer, s.m_count);
    }

    SIZE_T GetCount() const
    {
        return m_count;
    }
    
    SIZE_T GetSizeOf() const
    {
        return m_size * sizeof(T);
    }

    CONST T * GetString() const
    {
        return (const T *)m_buffer;
    }

    T * OpenStringBuffer(SIZE_T count)
    {
        Resize(count);
        return (T *)m_buffer;
    }

    void CloseBuffer(SIZE_T count)
    {
        if (m_count > count)
            m_count = count;

        NullTerminate();
        return;
    }
    
    ~StackString()
    {
        DeleteBuffer();
    }
};

#if _DEBUG
typedef StackString<32, CHAR> PathCharString;
typedef StackString<32, WCHAR> PathWCharString; 
#else
typedef StackString<260, CHAR> PathCharString;
typedef StackString<260, WCHAR> PathWCharString; 
#endif
#endif
