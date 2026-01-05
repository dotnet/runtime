// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#ifndef __GCENV_STRUCTS_INCLUDED__
#define __GCENV_STRUCTS_INCLUDED__
//
// Structs shared between the GC and the environment
//

struct GCSystemInfo
{
    uint32_t        dwNumberOfProcessors;
    uint32_t        dwPageSize;
    uint32_t        dwAllocationGranularity;
};

typedef void * HANDLE;

#ifdef TARGET_UNIX

class EEThreadId
{
    pthread_t m_id;
    // Indicates whether the m_id is valid or not. pthread_t doesn't have any
    // portable "invalid" value.
    bool m_isValid;

public:
    bool IsCurrentThread()
    {
        return m_isValid && pthread_equal(m_id, pthread_self());
    }

    void SetToCurrentThread()
    {
        m_id = pthread_self();
        m_isValid = true;
    }

    void Clear()
    {
        m_isValid = false;
    }
};

#else // TARGET_UNIX

class EEThreadId
{
    uint64_t m_uiId;
public:

    bool IsCurrentThread()
    {
        return m_uiId == ::GetCurrentThreadId();
    }

    void SetToCurrentThread()
    {
        m_uiId = ::GetCurrentThreadId();
    }

    void Clear()
    {
        m_uiId = 0;
    }
};

#endif // TARGET_UNIX

#endif // __GCENV_STRUCTS_INCLUDED__
