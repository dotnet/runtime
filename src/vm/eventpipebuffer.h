// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_BUFFER_H__
#define __EVENTPIPE_BUFFER_H__

#ifdef FEATURE_PERFTRACING

#include "eventpipe.h"
#include "eventpipeevent.h"
#include "eventpipeeventinstance.h"
#include "eventpipesession.h"

class EventPipeBuffer
{

    friend class EventPipeBufferList;
    friend class EventPipeBufferManager;

private:

    // Instances of EventPipeEventInstance in the buffer must be 8-byte aligned.
    // It is OK for the data payloads to be unaligned because they are opaque blobs that are copied via memcpy.
    const size_t AlignmentSize = 8;

    // A pointer to the actual buffer.
    BYTE *m_pBuffer;

    // The current write pointer.
    BYTE *m_pCurrent;

    // The max write pointer (end of the buffer).
    BYTE *m_pLimit;

    // The timestamp of the most recent event in the buffer.
    LARGE_INTEGER m_mostRecentTimeStamp;

    // Used by PopNext as input to GetNext.
    // If NULL, no events have been popped.
    // The event will still remain in the buffer after it is popped, but PopNext will not return it again.
    EventPipeEventInstance *m_pLastPoppedEvent;

    // Each buffer will become part of a per-thread linked list of buffers.
    // The linked list is invasive, thus we declare the pointers here.
    EventPipeBuffer *m_pPrevBuffer;
    EventPipeBuffer *m_pNextBuffer;

    unsigned int GetSize() const
    {
        LIMITED_METHOD_CONTRACT;
        return (unsigned int)(m_pLimit - m_pBuffer);
    }

    EventPipeBuffer* GetPrevious() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pPrevBuffer;
    }

    EventPipeBuffer* GetNext() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pNextBuffer;
    }

    void SetPrevious(EventPipeBuffer *pBuffer)
    {
        LIMITED_METHOD_CONTRACT;
        m_pPrevBuffer = pBuffer;
    }

    void SetNext(EventPipeBuffer *pBuffer)
    {
        LIMITED_METHOD_CONTRACT;
        m_pNextBuffer = pBuffer;
    }

    FORCEINLINE BYTE* GetNextAlignedAddress(BYTE *pAddress)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_pBuffer <= pAddress && m_pLimit > pAddress);

        pAddress = (BYTE*)ALIGN_UP(pAddress, AlignmentSize);

        _ASSERTE((size_t)pAddress % AlignmentSize == 0);
        return pAddress;
    }

public:

    EventPipeBuffer(unsigned int bufferSize);
    ~EventPipeBuffer();

    // Write an event to the buffer.
    // An optional stack trace can be provided for sample profiler events.
    // Otherwise, if a stack trace is needed, one will be automatically collected.
    // Returns:
    //  - true: The write succeeded.
    //  - false: The write failed.  In this case, the buffer should be considered full.
    bool WriteEvent(Thread *pThread, EventPipeSession &session, EventPipeEvent &event, EventPipeEventPayload &payload, LPCGUID pActivityId, LPCGUID pRelatedActivityId, StackContents *pStack = NULL);

    // Get the timestamp of the most recent event in the buffer.
    LARGE_INTEGER GetMostRecentTimeStamp() const;

    // Clear the buffer.
    void Clear();

    // Get the next event from the buffer as long as it is before the specified timestamp.
    // Input of NULL gets the first event.
    EventPipeEventInstance* GetNext(EventPipeEventInstance *pEvent, LARGE_INTEGER beforeTimeStamp);

    // Get the next event from the buffer, but don't mark it read.
    EventPipeEventInstance* PeekNext(LARGE_INTEGER beforeTimeStamp);

    // Get the next event from the buffer and mark it as read.
    EventPipeEventInstance* PopNext(LARGE_INTEGER beforeTimeStamp);

#ifdef _DEBUG
    bool EnsureConsistency();
#endif // _DEBUG
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_BUFFER_H__
