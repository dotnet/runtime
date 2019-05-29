// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipe.h"
#include "eventpipeeventinstance.h"
#include "eventpipebuffer.h"
#include "eventpipebuffermanager.h"

#ifdef FEATURE_PERFTRACING

EventPipeBuffer::EventPipeBuffer(unsigned int bufferSize DEBUG_ARG(EventPipeThread *pWriterThread))
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    m_state = EventPipeBufferState::WRITABLE;
#ifdef DEBUG
    m_pWriterThread = pWriterThread;
#endif
    m_pBuffer = new BYTE[bufferSize];
    memset(m_pBuffer, 0, bufferSize);
    m_pLimit = m_pBuffer + bufferSize;
    m_pCurrent = GetNextAlignedAddress(m_pBuffer);

    QueryPerformanceCounter(&m_creationTimeStamp);
    _ASSERTE(m_creationTimeStamp.QuadPart > 0);
    m_pLastPoppedEvent = NULL;
    m_pPrevBuffer = NULL;
    m_pNextBuffer = NULL;
}

EventPipeBuffer::~EventPipeBuffer()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        // We should never be deleting a buffer that a writer thread might still try to write to
        PRECONDITION(m_state == EventPipeBufferState::READ_ONLY);
    }
    CONTRACTL_END;

    delete[] m_pBuffer;
}

bool EventPipeBuffer::WriteEvent(Thread *pThread, EventPipeSession &session, EventPipeEvent &event, EventPipeEventPayload &payload, LPCGUID pActivityId, LPCGUID pRelatedActivityId, StackContents *pStack)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(((size_t)m_pCurrent % AlignmentSize) == 0);
        // We should never try to write to a buffer that isn't expecting to be written to.
        PRECONDITION(m_state == EventPipeBufferState::WRITABLE);
    }
    CONTRACTL_END;


    // Calculate the size of the event.
    unsigned int eventSize = sizeof(EventPipeEventInstance) + payload.GetSize();

    // Make sure we have enough space to write the event.
    if(m_pCurrent + eventSize > m_pLimit)
        return false;

    bool success = true;
    EX_TRY
    {
        // Calculate the location of the data payload.
        BYTE *pDataDest = payload.GetSize() == 0 ? NULL : m_pCurrent + sizeof(EventPipeEventInstance);

        // Placement-new the EventPipeEventInstance.
        // if pthread is NULL, it's likely we are running in something like a GC thread which is not a Thread object, so it can't have an activity ID set anyway

        StackContents s;
        memset((void *)&s, 0, sizeof(s));
        if (event.NeedStack() && !session.RundownEnabled() && pStack == NULL)
        {
            EventPipe::WalkManagedStackForCurrentThread(s);
            pStack = &s;
        }

        EventPipeEventInstance *pInstance = new (m_pCurrent) EventPipeEventInstance(
            event,
            (pThread == NULL) ? ::GetCurrentThreadId() : pThread->GetOSThreadId(),
            pDataDest,
            payload.GetSize(),
            (pThread == NULL) ? NULL : pActivityId,
            pRelatedActivityId);

        // Copy the stack if a separate stack trace was provided.
        if (pStack != NULL)
        {
            StackContents *pInstanceStack = pInstance->GetStack();
            pStack->CopyTo(pInstanceStack);
        }

        // Write the event payload data to the buffer.
        if (payload.GetSize() > 0)
        {
            payload.CopyData(pDataDest);
        }
    }
    EX_CATCH
    {
        // If a failure occurs, bail out and don't advance the pointer.
        success = false;
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (success)
    {
        // Advance the current pointer past the event.
        m_pCurrent = GetNextAlignedAddress(m_pCurrent + eventSize);
    }

    return success;
}

LARGE_INTEGER EventPipeBuffer::GetCreationTimeStamp() const
{
    LIMITED_METHOD_CONTRACT;

    return m_creationTimeStamp;
}

EventPipeEventInstance *EventPipeBuffer::GetNext(EventPipeEventInstance *pEvent, LARGE_INTEGER beforeTimeStamp)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(m_state == EventPipeBufferState::READ_ONLY);
    }
    CONTRACTL_END;

    EventPipeEventInstance *pNextInstance = NULL;
    // If input is NULL, return the first event if there is one.
    if (pEvent == NULL)
    {
        // If this buffer contains an event, select it.
        BYTE *pFirstAlignedInstance = GetNextAlignedAddress(m_pBuffer);
        if (m_pCurrent > pFirstAlignedInstance)
        {
            pNextInstance = (EventPipeEventInstance *)pFirstAlignedInstance;
        }
        else
        {
            return NULL;
        }
    }
    else
    {
        // Confirm that pEvent is within the used range of the buffer.
        if (((BYTE *)pEvent < m_pBuffer) || ((BYTE *)pEvent >= m_pCurrent))
        {
            _ASSERT(!"Input pointer is out of range.");
            return NULL;
        }

        if (pEvent->GetData())
        {
            // We have a pointer within the bounds of the buffer.
            // Find the next event by skipping the current event with it's data payload immediately after the instance.
            pNextInstance = (EventPipeEventInstance *)GetNextAlignedAddress(const_cast<BYTE *>(pEvent->GetData() + pEvent->GetDataLength()));
        }
        else
        {
            // In case we do not have a payload, the next instance is right after the current instance
            pNextInstance = (EventPipeEventInstance *)GetNextAlignedAddress((BYTE *)(pEvent + 1));
        }

        // Check to see if we've reached the end of the written portion of the buffer.
        if ((BYTE *)pNextInstance >= m_pCurrent)
        {
            return NULL;
        }
    }

    // Ensure that the timestamp is valid.  The buffer is zero'd before use, so a zero timestamp is invalid.
    LARGE_INTEGER nextTimeStamp = *pNextInstance->GetTimeStamp();
    if (nextTimeStamp.QuadPart == 0)
    {
        return NULL;
    }

    // Ensure that the timestamp is earlier than the beforeTimeStamp.
    if (nextTimeStamp.QuadPart >= beforeTimeStamp.QuadPart)
    {
        return NULL;
    }

    return pNextInstance;
}

EventPipeEventInstance *EventPipeBuffer::PeekNext(LARGE_INTEGER beforeTimeStamp)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(m_state == READ_ONLY);
    }
    CONTRACTL_END;

    // Get the next event using the last popped event as a marker.
    return GetNext(m_pLastPoppedEvent, beforeTimeStamp);
}

void EventPipeBuffer::PopNext(EventPipeEventInstance *pNext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(m_state == READ_ONLY);
    }
    CONTRACTL_END;

    if (pNext != NULL)
        m_pLastPoppedEvent = pNext;
}

EventPipeBufferState EventPipeBuffer::GetVolatileState()
{
    LIMITED_METHOD_CONTRACT;
    return m_state.Load();
}

void EventPipeBuffer::ConvertToReadOnly()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_pWriterThread->GetLock()->OwnedByCurrentThread());
    m_state.Store(EventPipeBufferState::READ_ONLY);
}

#ifdef _DEBUG
bool EventPipeBuffer::EnsureConsistency()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Check to see if the buffer is empty.
    if (GetNextAlignedAddress(m_pBuffer) == m_pCurrent)
    {
        // Make sure that the buffer size is greater than zero.
        _ASSERTE(m_pBuffer != m_pLimit);
    }

    // Validate the contents of the filled portion of the buffer.
    BYTE *ptr = GetNextAlignedAddress(m_pBuffer);
    while (ptr < m_pCurrent)
    {
        // Validate the event.
        EventPipeEventInstance *pInstance = (EventPipeEventInstance *)ptr;
        _ASSERTE(pInstance->EnsureConsistency());

        // Validate that payload and length match.
        _ASSERTE((pInstance->GetData() != NULL && pInstance->GetDataLength() > 0) || (pInstance->GetData() == NULL && pInstance->GetDataLength() == 0));

        // Skip the event.
        ptr = GetNextAlignedAddress(ptr + sizeof(*pInstance) + pInstance->GetDataLength());
    }

    // When we're done walking the filled portion of the buffer,
    // ptr should be the same as m_pCurrent.
    _ASSERTE(ptr == m_pCurrent);

    // Walk the rest of the buffer, making sure it is properly zeroed.
    while (ptr < m_pLimit)
    {
        _ASSERTE(*ptr++ == 0);
    }

    return true;
}
#endif // _DEBUG

#endif // FEATURE_PERFTRACING
