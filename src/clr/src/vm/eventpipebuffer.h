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

class EventPipeThread;


// Synchronization
//
// EventPipeBuffer starts off writable and accumulates events in a buffer, then at some point converts to be readable and a second thread can
// read back the events which have accumulated. The transition occurs when calling ConvertToReadOnly(). Write methods will assert if the buffer
// isn't writable and read-related methods will assert if it isn't readable. Methods that have no asserts should have immutable results that
// can be used at any point during the buffer's lifetime. The buffer has no internal locks so it is the caller's responsibility to synchronize
// their usage.
// Writing into the buffer and calling ConvertToReadOnly() is always done with EventPipeThread::m_lock held. The eventual reader thread can do
// a few different things to ensure it sees a consistent state:
// 1) Take the writer's EventPipeThread::m_lock at least once after the last time the writer writes events
// 2) Use a memory barrier that prevents reader loads from being re-ordered earlier, such as the one that will occur implicitly by evaluating
//    EventPipeBuffer::GetVolatileState()


enum EventPipeBufferState
{
    // This buffer is currently assigned to a thread and pWriterThread may write events into it
    // at any time
    WRITABLE = 0,

    // This buffer has been returned to the EventPipeBufferManager and pWriterThread is guaranteed
    // to never access it again.
    READ_ONLY = 1
};

class EventPipeBuffer
{

    friend class EventPipeBufferList;
    friend class EventPipeBufferManager;

private:

    // Instances of EventPipeEventInstance in the buffer must be 8-byte aligned.
    // It is OK for the data payloads to be unaligned because they are opaque blobs that are copied via memcpy.
    const size_t AlignmentSize = 8;

    // State transition WRITABLE -> READ_ONLY only occurs while holding the m_pWriterThread->m_lock;
    // It can be read at any time
    Volatile<EventPipeBufferState> m_state;

    // Thread that is/was allowed to write into this buffer when m_state == WRITABLE
    EventPipeThread* m_pWriterThread;

    // The sequence number corresponding to m_pCurrentReadEvent
    // Prior to read iteration it is the sequence number of the first event in the buffer
    unsigned int m_eventSequenceNumber;
    
    // A pointer to the actual buffer.
    BYTE *m_pBuffer;

    // The current write pointer.
    BYTE *m_pCurrent;

    // The max write pointer (end of the buffer).
    BYTE *m_pLimit;

    // The timestamp the buffer was created. If our clock source
    // is monotonic then all events in the buffer should have
    // timestamp >= this one. If not then all bets are off.
    LARGE_INTEGER m_creationTimeStamp;

    // Pointer to the current event being read
    EventPipeEventInstance *m_pCurrentReadEvent;

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
        _ASSERTE(m_pBuffer <= pAddress && pAddress <= m_pLimit);

        pAddress = (BYTE*)ALIGN_UP(pAddress, AlignmentSize);

        _ASSERTE((size_t)pAddress % AlignmentSize == 0);
        return pAddress;
    }

public:

    EventPipeBuffer(unsigned int bufferSize, EventPipeThread* pWriterThread, unsigned int eventSequenceNumber);
    ~EventPipeBuffer();

    // Write an event to the buffer.
    // An optional stack trace can be provided for sample profiler events.
    // Otherwise, if a stack trace is needed, one will be automatically collected.
    // Returns:
    //  - true: The write succeeded.
    //  - false: The write failed.  In this case, the buffer should be considered full.
    bool WriteEvent(Thread *pThread, EventPipeSession &session, EventPipeEvent &event, EventPipeEventPayload &payload, LPCGUID pActivityId, LPCGUID pRelatedActivityId, StackContents *pStack = NULL);

    // Get the timestamp the buffer was created.
    LARGE_INTEGER GetCreationTimeStamp() const;

    // Advances read cursor to the next event or NULL if there aren't any more. When the
    // buffer is first made readable the cursor is automatically positioned on the first
    // event or NULL if there are no events in the buffer.
    void MoveNextReadEvent();

    // Returns the event at the current read cursor. The returned event pointer is valid
    // until the buffer is deleted.
    EventPipeEventInstance* GetCurrentReadEvent();

    // Gets the sequence number of the event corresponding to GetCurrentReadEvent();
    unsigned int GetCurrentSequenceNumber();

    // Get the thread that is (or was) assigned to write to this buffer
    EventPipeThread* GetWriterThread();

    // Check the state of the buffer
    EventPipeBufferState GetVolatileState();

    // Convert the buffer writable to readable
    void ConvertToReadOnly();

#ifdef _DEBUG
    bool EnsureConsistency();
#endif // _DEBUG
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_BUFFER_H__
