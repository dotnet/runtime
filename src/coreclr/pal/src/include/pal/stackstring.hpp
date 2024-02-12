// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __STACKSTRING_H_
#define __STACKSTRING_H_

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
            free(m_buffer);

        m_buffer = NULL;
        return;
    }

    BOOL ReallocateBuffer(SIZE_T count)
    {
        // count is always > STACKCOUNT here.
        // We got so far, we will allocate a little extra
        // to prevent frequent allocations
#if _DEBUG
        SIZE_T count_allocated = count;
#else
        SIZE_T count_allocated = count + 100;
#endif //_DEBUG

        BOOL dataOnStack =  m_buffer == m_innerBuffer;
        if( dataOnStack )
        {
            m_buffer = NULL;
        }

        T * newBuffer = (T *)PAL_realloc(m_buffer, (count_allocated + 1) * sizeof(T));
        if (NULL == newBuffer)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);

            DeleteBuffer();
            m_count = 0;
            m_buffer = m_innerBuffer;
            return FALSE;
        }

        if( dataOnStack)
        {
            CopyMemory(newBuffer, m_innerBuffer, (m_count + 1) * sizeof(T));
        }

        m_buffer = newBuffer;
        m_count = count;
        m_size = count_allocated + 1;

        return TRUE;
    }

    BOOL HasAvailableMemory(SIZE_T count)
    {
        return (count < m_size);
    }

    //NOTE: Always call this before modifying the underlying buffer
    BOOL Resize(SIZE_T count)
    {

        if (NULL == m_buffer)
        {
            m_buffer = m_innerBuffer;
        }

        if (HasAvailableMemory(count))
        {
            m_count = count;
        }
        else
        {
            if (count > STACKCOUNT)
            {
                return ReallocateBuffer(count);
            }
            else
            {
                m_count = count;
                m_size = STACKCOUNT+1;
            }
        }

        return TRUE;
    }

    StackString(const StackString &s)
    {
        Set(s);
    }

public:
    StackString()
        : m_buffer(m_innerBuffer), m_size(STACKCOUNT+1), m_count(0)
    {
    }


    BOOL Set(const T * buffer, SIZE_T count)
    {
        if (!Resize(count))
            return FALSE;

        CopyMemory(m_buffer, buffer, (count + 1) * sizeof(T));
        NullTerminate();
        return TRUE;
    }

    BOOL Set(const StackString &s)
    {
        return Set(s.m_buffer, s.m_count);
    }

    template<SIZE_T bufferLength> BOOL Set(const T (&buffer)[bufferLength])
    {
        // bufferLength includes terminator character
        return Set(buffer, bufferLength - 1);
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

    operator const T * () const {  return GetString(); }

    //Always preserves the existing content
    T * OpenStringBuffer(SIZE_T count)
    {
        T * result = NULL;
        if (Resize(count))
        {
            result = (T *)m_buffer;
        }
        return result;
    }

    T * OpenStringBuffer()
    {
        return m_buffer;
    }

    //count should not include the terminating null
    void CloseBuffer(SIZE_T count)
    {
        if (m_count > count)
            m_count = count;

        NullTerminate();
        return;
    }

    //Call this with the best estimate if you want to
    //prevent possible reallocations on further operations
    BOOL Reserve(SIZE_T count)
    {
        SIZE_T endpos = m_count;

        if (!Resize(count))
            return FALSE;

        m_count = endpos;
        NullTerminate();

        return TRUE;
    }

    //count Should not include the terminating null
    BOOL Append(const T * buffer, SIZE_T count)
    {
        SIZE_T endpos = m_count;
        if (!Resize(m_count + count))
            return FALSE;

        CopyMemory(&m_buffer[endpos], buffer, (count + 1) * sizeof(T));
        NullTerminate();
        return TRUE;
    }

    BOOL Append(const StackString &s)
    {
        return Append(s.GetString(), s.GetCount());
    }

    template<SIZE_T bufferLength> BOOL Append(const T (&buffer)[bufferLength])
    {
        // bufferLength includes terminator character
        return Append(buffer, bufferLength - 1);
    }

    BOOL Append(T ch)
    {
        SIZE_T endpos = m_count;
        if (!Resize(m_count + 1))
            return FALSE;

        m_buffer[endpos] = ch;
        NullTerminate();
        return TRUE;
    }

    BOOL IsEmpty()
    {
        return 0 == m_buffer[0];
    }

    void Clear()
    {
        m_count = 0;
        NullTerminate();
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
typedef StackString<MAX_PATH, CHAR> PathCharString;
typedef StackString<MAX_PATH, WCHAR> PathWCharString;
#endif
#endif

// Some Helper Definitions
DWORD
GetCurrentDirectoryA(
         PathCharString& lpBuffer);
