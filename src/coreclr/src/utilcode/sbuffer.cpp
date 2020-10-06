// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ---------------------------------------------------------------------------

//
// Buffer.cpp
// ---------------------------------------------------------------------------

#include "stdafx.h"
#include "sbuffer.h"
#include "ex.h"
#include "holder.h"
#include "stdmacros.h"

// Try to minimize the performance impact of contracts on CHK build.
#if defined(_MSC_VER)
#pragma inline_depth (20)
#endif

const DWORD g_garbageFillBuffer[GARBAGE_FILL_BUFFER_ITEMS] =
{
        GARBAGE_FILL_DWORD, GARBAGE_FILL_DWORD, GARBAGE_FILL_DWORD, GARBAGE_FILL_DWORD,
        GARBAGE_FILL_DWORD, GARBAGE_FILL_DWORD, GARBAGE_FILL_DWORD, GARBAGE_FILL_DWORD,
        GARBAGE_FILL_DWORD, GARBAGE_FILL_DWORD, GARBAGE_FILL_DWORD, GARBAGE_FILL_DWORD,
        GARBAGE_FILL_DWORD, GARBAGE_FILL_DWORD, GARBAGE_FILL_DWORD, GARBAGE_FILL_DWORD,
};

//----------------------------------------------------------------------------
// ReallocateBuffer
// Low level buffer reallocate routine
//----------------------------------------------------------------------------
void SBuffer::ReallocateBuffer(COUNT_T allocation, Preserve preserve)
{
    CONTRACT_VOID
    {
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckBufferClosed());
        PRECONDITION(CheckAllocation(allocation));
        PRECONDITION(allocation >= m_size);
        POSTCONDITION(m_allocation == allocation);
        if (allocation > 0) THROWS; else NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    BYTE *newBuffer = NULL;
    if (allocation > 0)
    {
        newBuffer = NewBuffer(allocation);

        if (preserve == PRESERVE)
        {
            // Copy the relevant contents of the old buffer over
            DebugMoveBuffer(newBuffer, m_buffer, m_size);
        }
    }

    if (IsAllocated())
        DeleteBuffer(m_buffer, m_allocation);

    m_buffer = newBuffer;
    m_allocation = allocation;

    if (allocation > 0)
        SetAllocated();
    else
        ClearAllocated();

    ClearImmutable();

    RETURN;
}

void SBuffer::Replace(const Iterator &i, COUNT_T deleteSize, COUNT_T insertSize)
{
    CONTRACT_VOID
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(this));
        PRECONDITION(CheckIteratorRange(i, deleteSize));
        SUPPORTS_DAC_HOST_ONLY;
    }
    CONTRACT_END;

    COUNT_T startRange = (COUNT_T) (i.m_ptr - m_buffer);
    // The PRECONDITION(CheckIterationRange(i, deleteSize)) should check this in
    // contract-checking builds, but this ensures we don't integer overflow if someone
    // passes in an egregious deleteSize by capping it to the remaining length in the
    // buffer.
    if ((COUNT_T)(m_buffer + m_size - i.m_ptr) < deleteSize)
    {
        _ASSERTE(!"Trying to replace more bytes than are remaining in the buffer.");
        deleteSize = (COUNT_T)(m_buffer + m_size - i.m_ptr);
    }
    COUNT_T endRange = startRange + deleteSize;
    COUNT_T end = m_size;

    SCOUNT_T delta = insertSize - deleteSize;

    if (delta < 0)
    {
        // Buffer is shrinking

        DebugDestructBuffer(i.m_ptr, deleteSize);

        DebugMoveBuffer(m_buffer + endRange + delta,
                        m_buffer + endRange,
                        end - endRange);

        Resize(m_size+delta, PRESERVE);

        i.Resync(this, m_buffer + startRange);

    }
    else if (delta > 0)
    {
        // Buffer is growing

        ResizePadded(m_size+delta);

        i.Resync(this, m_buffer + startRange);

        DebugDestructBuffer(i.m_ptr, deleteSize);

        DebugMoveBuffer(m_buffer + endRange + delta,
                        m_buffer + endRange,
                        end - endRange);

    }
    else
    {
        // Buffer stays the same size.  We need to DebugDestruct it first to keep
        // the invariant that the new space is clean.

        DebugDestructBuffer(i.m_ptr, insertSize);
    }

    DebugConstructBuffer(i.m_ptr, insertSize);

    RETURN;
}


