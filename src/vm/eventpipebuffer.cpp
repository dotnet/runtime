// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "common.h"
#include "eventpipeeventinstance.h"
#include "eventpipebuffer.h"

#ifdef FEATURE_PERFTRACING

EventPipeBuffer::EventPipeBuffer(unsigned int bufferSize)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pBuffer = new BYTE[bufferSize];
    memset(m_pBuffer, 0, bufferSize);
    m_pCurrent = m_pBuffer;
    m_pLimit = m_pBuffer + bufferSize;

    m_mostRecentTimeStamp.QuadPart = 0;
    m_pLastPoppedEvent = NULL;
    m_pPrevBuffer = NULL;
    m_pNextBuffer = NULL;
}

EventPipeBuffer::~EventPipeBuffer()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(m_pBuffer != NULL)
    {
        delete[] m_pBuffer;
    }
}

bool EventPipeBuffer::WriteEvent(Thread *pThread, EventPipeEvent &event, BYTE *pData, unsigned int dataLength, LPCGUID pActivityId, LPCGUID pRelatedActivityId, StackContents *pStack)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pThread != NULL);
    }
    CONTRACTL_END;

    // Calculate the size of the event.
    unsigned int eventSize = sizeof(EventPipeEventInstance) + dataLength;

    // Make sure we have enough space to write the event.
    if(m_pCurrent + eventSize >= m_pLimit)
    {
        return false;
    }

    // Calculate the location of the data payload.
    BYTE *pDataDest = m_pCurrent + sizeof(EventPipeEventInstance);

    bool success = true;
    EX_TRY
    {
        // Placement-new the EventPipeEventInstance.
        EventPipeEventInstance *pInstance = new (m_pCurrent) EventPipeEventInstance(
            event,
            pThread->GetOSThreadId(),
            pDataDest,
            dataLength,
            pActivityId,
            pRelatedActivityId);

        // Copy the stack if a separate stack trace was provided.
        if(pStack != NULL)
        {
            StackContents *pInstanceStack = pInstance->GetStack();
            pStack->CopyTo(pInstanceStack);
        }

        // Write the event payload data to the buffer.
        if(dataLength > 0)
        {
            memcpy(pDataDest, pData, dataLength);
        }

        // Save the most recent event timestamp.
        m_mostRecentTimeStamp = pInstance->GetTimeStamp();

    }
    EX_CATCH
    {
        // If a failure occurs, bail out and don't advance the pointer.
        success = false;
    }
    EX_END_CATCH(SwallowAllExceptions);

    if(success)
    {
        // Advance the current pointer past the event.
        m_pCurrent += eventSize;
    }

    return success;
}

LARGE_INTEGER EventPipeBuffer::GetMostRecentTimeStamp() const
{
    LIMITED_METHOD_CONTRACT;

    return m_mostRecentTimeStamp;
}

void EventPipeBuffer::Clear()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    memset(m_pBuffer, 0, (size_t)(m_pLimit - m_pBuffer));
    m_pCurrent = m_pBuffer;
    m_mostRecentTimeStamp.QuadPart = 0;
    m_pLastPoppedEvent = NULL;
}

EventPipeEventInstance* EventPipeBuffer::GetNext(EventPipeEventInstance *pEvent, LARGE_INTEGER beforeTimeStamp)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    EventPipeEventInstance *pNextInstance = NULL;
    // If input is NULL, return the first event if there is one.
    if(pEvent == NULL)
    {
        // If this buffer contains an event, select it.
        if(m_pCurrent > m_pBuffer)
        {
            pNextInstance = (EventPipeEventInstance*)m_pBuffer;
        }
        else
        {
            return NULL;
        }
    }
    else
    {
        // Confirm that pEvent is within the used range of the buffer.
        if(((BYTE*)pEvent < m_pBuffer) || ((BYTE*)pEvent >= m_pCurrent))
        {
            _ASSERT(!"Input pointer is out of range.");
            return NULL;
        }

        // We have a pointer within the bounds of the buffer.
        // Find the next event by skipping the current event with it's data payload immediately after the instance.
        pNextInstance = (EventPipeEventInstance *)(pEvent->GetData() + pEvent->GetLength());

        // Check to see if we've reached the end of the written portion of the buffer.
        if((BYTE*)pNextInstance >= m_pCurrent)
        {
            return NULL;
        }
    }

    // Ensure that the timestamp is valid.  The buffer is zero'd before use, so a zero timestamp is invalid.
    LARGE_INTEGER nextTimeStamp = pNextInstance->GetTimeStamp();
    if(nextTimeStamp.QuadPart == 0)
    {
        return NULL;
    }

    // Ensure that the timestamp is earlier than the beforeTimeStamp.
    if(nextTimeStamp.QuadPart >= beforeTimeStamp.QuadPart)
    {
        return NULL;
    }

    return pNextInstance;
}

EventPipeEventInstance* EventPipeBuffer::PeekNext(LARGE_INTEGER beforeTimeStamp)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Get the next event using the last popped event as a marker.
    return GetNext(m_pLastPoppedEvent, beforeTimeStamp);
}

EventPipeEventInstance* EventPipeBuffer::PopNext(LARGE_INTEGER beforeTimeStamp)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Get the next event using the last popped event as a marker.
    EventPipeEventInstance *pNext = PeekNext(beforeTimeStamp);
    if(pNext != NULL)
    {
        m_pLastPoppedEvent = pNext;
    }

    return pNext;
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
    if(m_pBuffer == m_pCurrent)
    {
        // Make sure that the buffer size is greater than zero.
        _ASSERTE(m_pBuffer != m_pLimit);
    }

    // Validate the contents of the filled portion of the buffer.
    BYTE *ptr = m_pBuffer;
    while(ptr < m_pCurrent)
    {
        // Validate the event.
        EventPipeEventInstance *pInstance = (EventPipeEventInstance*)ptr;
        _ASSERTE(pInstance->EnsureConsistency());

        // Validate that payload and length match.
        _ASSERTE((pInstance->GetData() != NULL && pInstance->GetLength() > 0) || (pInstance->GetData() != NULL && pInstance->GetLength() == 0));

        // Skip the event.
        ptr += sizeof(*pInstance) + pInstance->GetLength();
    }

    // When we're done walking the filled portion of the buffer,
    // ptr should be the same as m_pCurrent.
    _ASSERTE(ptr == m_pCurrent);

    // Walk the rest of the buffer, making sure it is properly zeroed.
    while(ptr < m_pLimit)
    {
        _ASSERTE(*ptr++ == 0);
    }

    return true;
}
#endif // _DEBUG

#endif // FEATURE_PERFTRACING
