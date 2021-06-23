// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// Allocator and holders for double mapped executable memory
//

#pragma once

#include "utilcode.h"
#include "ex.h"

// Holder class to map read-execute memory as read-write so that it can be modified without using read-write-execute mapping.
// At the moment the implementation is dummy, returning the same addresses for both cases and expecting them to be read-write-execute.
// The class uses the move semantics to ensure proper unmapping in case of re-assigning of the holder value.
template<typename T>
class ExecutableWriterHolder
{
    T *m_addressRX;
    T *m_addressRW;

    void Move(ExecutableWriterHolder& other)
    {
        m_addressRX = other.m_addressRX;
        m_addressRW = other.m_addressRW;
        other.m_addressRX = NULL;
        other.m_addressRW = NULL;
    }

    void Unmap()
    {
        if (m_addressRX != NULL)
        {
            // TODO: mapping / unmapping for targets using double memory mapping  will be added with the double mapped allocator addition 
#if defined(HOST_OSX) && defined(HOST_ARM64) && !defined(DACCESS_COMPILE)
            PAL_JitWriteProtect(false);
#endif
        }
    }

public:
    ExecutableWriterHolder(const ExecutableWriterHolder& other) = delete;
    ExecutableWriterHolder& operator=(const ExecutableWriterHolder& other) = delete;

    ExecutableWriterHolder(ExecutableWriterHolder&& other)
    {
        Move(other);
    }

    ExecutableWriterHolder& operator=(ExecutableWriterHolder&& other)
    {
        Unmap();
        Move(other);
        return *this;
    }

    ExecutableWriterHolder() : m_addressRX(nullptr), m_addressRW(nullptr)
    {
    }

    ExecutableWriterHolder(T* addressRX, size_t size)
    {
        m_addressRX = addressRX;
        m_addressRW = addressRX;
#if defined(HOST_OSX) && defined(HOST_ARM64) && !defined(DACCESS_COMPILE)
        PAL_JitWriteProtect(true);
#endif
    }

    ~ExecutableWriterHolder()
    {
        Unmap();
    }

    // Get the writeable address
    inline T *GetRW() const
    {
        return m_addressRW;
    }
};
