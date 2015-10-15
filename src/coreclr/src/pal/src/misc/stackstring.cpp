// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

#include "pal/malloc.hpp"
#include "pal/dbgmsg.h"

SET_DEFAULT_DEBUG_CHANNEL(MISC);

template <SIZE_T STACKCOUNT>
class StackString
{
private:
    WCHAR m_innerBuffer[STACKCOUNT + 1];
    WCHAR * m_buffer;
    SIZE_T m_count; // actual allocated count

    void NullTerminate()
    {
        m_buffer[m_count] = W('\0');
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
        WCHAR * newBuffer = (WCHAR *)PAL_malloc((count + 1) * sizeof(WCHAR));
        if (NULL == newBuffer)
        {
            ERROR("malloc failed\n");
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);

            DeleteBuffer();
            m_count = 0;

            return;
        }

        DeleteBuffer();
        m_buffer = newBuffer;
        m_count = count;

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
                m_buffer = m_innerBuffer;
                m_count = count;
            }
        }
        else if (m_innerBuffer == m_buffer)
        {
            if (count > STACKCOUNT)
                ReallocateBuffer(count);
            else
                m_count = count;
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

    ~StackString()
    {
        DeleteBuffer();
    }

public:
    StackString()
        : m_count(0), m_buffer(m_innerBuffer)
    {
    }

    BOOL Set(const WCHAR * buffer, SIZE_T count)
    {
        Resize(count);
        if (NULL == m_buffer)
            return FALSE;

        CopyMemory(m_buffer, buffer, (count + 1) * sizeof(WCHAR));
        NullTerminate();
        return TRUE;
    }

    BOOL Set(const StackString &s)
    {
        return Set(s.m_buffer, s.m_count);
    }

    SIZE_T Getcount() const
    {
        return m_count;
    }

    CONST WCHAR * GetString() const
    {
        return (const WCHAR *)m_buffer;
    }

    WCHAR * OpenStringBuffer(SIZE_T count)
    {
        Resize(count);
        return (WCHAR *)m_buffer;
    }

    void CloseBuffer(SIZE_T count)
    {
        if (m_count > count)
            m_count = count;

        NullTerminate();
        return;
    }
};
