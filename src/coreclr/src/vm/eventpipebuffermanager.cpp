// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipe.h"
#include "eventpipeconfiguration.h"
#include "eventpipebuffer.h"
#include "eventpipebuffermanager.h"

#ifdef FEATURE_PERFTRACING

EventPipeBufferManager::EventPipeBufferManager()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pPerThreadBufferList = new SList<SListElem<EventPipeBufferList*>>();
    m_sizeOfAllBuffers = 0;
    m_lock.Init(LOCK_TYPE_DEFAULT);

#ifdef _DEBUG
    m_numBuffersAllocated = 0;
    m_numBuffersStolen = 0;
    m_numBuffersLeaked = 0;
    m_numEventsStored = 0;
    m_numEventsDropped = 0;
    m_numEventsWritten = 0;
#endif // _DEBUG
}

EventPipeBufferManager::~EventPipeBufferManager()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(m_pPerThreadBufferList != NULL)
    {
        SListElem<EventPipeBufferList*> *pElem = m_pPerThreadBufferList->GetHead();
        while(pElem != NULL)
        {
            SListElem<EventPipeBufferList*> *pCurElem = pElem;

            EventPipeBufferList *pThreadBufferList = pCurElem->GetValue();
            if (!pThreadBufferList->OwnedByThread())
            {
                Thread *pThread = NULL;
                while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
                {
                    if (pThread->GetEventPipeBufferList() == pThreadBufferList)
                    {
                        pThread->SetEventPipeBufferList(NULL);
                        break;
                    }
                }

                // We don't delete buffers themself because they can be in-use
                delete(pThreadBufferList);
            }

            pElem = m_pPerThreadBufferList->GetNext(pElem);
            delete(pCurElem);
        }

        delete(m_pPerThreadBufferList);
        m_pPerThreadBufferList = NULL;
    }
}

EventPipeBuffer* EventPipeBufferManager::AllocateBufferForThread(EventPipeSession &session, Thread *pThread, unsigned int requestSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pThread != NULL);
        PRECONDITION(requestSize > 0);
    }
    CONTRACTL_END;

    // Allocating a buffer requires us to take the lock.
    SpinLockHolder _slh(&m_lock);

    // Determine if the requesting thread has at least one buffer.
    // If not, we guarantee that each thread gets at least one (to prevent thrashing when the circular buffer size is too small).
    bool allocateNewBuffer = false;
    EventPipeBufferList *pThreadBufferList = pThread->GetEventPipeBufferList();
    if(pThreadBufferList == NULL)
    {
        pThreadBufferList = new (nothrow) EventPipeBufferList(this);
        if (pThreadBufferList == NULL)
        {
            return NULL;
        }

        SListElem<EventPipeBufferList*> *pElem = new (nothrow) SListElem<EventPipeBufferList*>(pThreadBufferList);
        if (pElem == NULL)
        {
            return NULL;
        }

        m_pPerThreadBufferList->InsertTail(pElem);
        pThread->SetEventPipeBufferList(pThreadBufferList);
        allocateNewBuffer = true;
    }

    // Determine if policy allows us to allocate another buffer, or if we need to steal one
    // from another thread.
    if(!allocateNewBuffer)
    {
        EventPipeConfiguration *pConfig = EventPipe::GetConfiguration();
        if(pConfig == NULL)
        {
            return NULL;
        }

        size_t circularBufferSizeInBytes = pConfig->GetCircularBufferSize();
        if(m_sizeOfAllBuffers < circularBufferSizeInBytes)
        {
            // We don't worry about the fact that a new buffer could put us over the circular buffer size.
            // This is OK, and we won't do it again if we actually go over.
            allocateNewBuffer = true;
        }
    }

    // Only steal buffers from other threads if the session being written to is a
    // file-based session.  Streaming sessions will simply drop events.
    // TODO: Add dropped events telemetry here.
    EventPipeBuffer *pNewBuffer = NULL;
    if(!allocateNewBuffer && (session.GetSessionType() == EventPipeSessionType::File))
    {
        // We can't allocate a new buffer.
        // Find the oldest buffer, de-allocate it, and re-purpose it for this thread.

        // Find the thread that contains the oldest stealable buffer, and get its list of buffers.
        EventPipeBufferList *pListToStealFrom = FindThreadToStealFrom();
        if(pListToStealFrom != NULL)
        {
            // Assert that the buffer we're stealing is not the only buffer in the list.
            // This invariant is enforced by FindThreadToStealFrom.
            _ASSERTE((pListToStealFrom->GetHead() != NULL) && (pListToStealFrom->GetHead()->GetNext() != NULL));

            // Remove the oldest buffer from the list.
            pNewBuffer = pListToStealFrom->GetAndRemoveHead();

            // De-allocate the buffer.  We do this because buffers are variable sized
            // based on how much volume is coming from the thread.
            DeAllocateBuffer(pNewBuffer);
            pNewBuffer = NULL;

            // Set that we want to allocate a new buffer.
            allocateNewBuffer = true;

#ifdef _DEBUG
            m_numBuffersStolen++;
#endif // _DEBUG

        }
        else
        {
            // This only happens when # of threads == # of buffers.
            // We'll allocate one more buffer, and then this won't happen again.
            allocateNewBuffer = true;
        }
    }

    if(allocateNewBuffer)
    {
        // Pick a buffer size by multiplying the base buffer size by the number of buffers already allocated for this thread.
        unsigned int sizeMultiplier = pThreadBufferList->GetCount() + 1;

        // Pick the base buffer size based.  Debug builds have a smaller size to stress the allocate/steal path more.
        unsigned int baseBufferSize =
#ifdef _DEBUG
            30 * 1024; // 30K
#else
            100 * 1024; // 100K
#endif
        unsigned int bufferSize = baseBufferSize * sizeMultiplier;

        // Make sure that buffer size >= request size so that the buffer size does not
        // determine the max event size.
        if(bufferSize < requestSize)
        {
            bufferSize = requestSize;
        }

        // Don't allow the buffer size to exceed 1MB.
        const unsigned int maxBufferSize = 1024 * 1024;
        if(bufferSize > maxBufferSize)
        {
            bufferSize = maxBufferSize;
        }

        // EX_TRY is used here as opposed to new (nothrow) because
        // the constructor also allocates a private buffer, which
        // could throw, and cannot be easily checked
        EX_TRY
        {
            pNewBuffer = new EventPipeBuffer(bufferSize);
        }
        EX_CATCH
        {
            pNewBuffer = NULL;
        }
        EX_END_CATCH(SwallowAllExceptions);

        if (pNewBuffer == NULL)
        {
            return NULL;
        }

        m_sizeOfAllBuffers += bufferSize;
#ifdef _DEBUG
        m_numBuffersAllocated++;
#endif // _DEBUG
    }

    // Set the buffer on the thread.
    if(pNewBuffer != NULL)
    {
        pThreadBufferList->InsertTail(pNewBuffer);
        return pNewBuffer;
    }

    return NULL;
}

EventPipeBufferList* EventPipeBufferManager::FindThreadToStealFrom()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(m_lock.OwnedByCurrentThread());
    }
    CONTRACTL_END;

    // Find the thread buffer list containing the buffer whose most recent event is the oldest as long as the buffer is not
    // the current buffer for the thread (e.g. it's next pointer is non-NULL).
    // This means that the thread must also have multiple buffers, so that we don't steal its only buffer.
    EventPipeBufferList *pOldestContainingList = NULL;

    SListElem<EventPipeBufferList*> *pElem = m_pPerThreadBufferList->GetHead();
    while(pElem != NULL)
    {
        EventPipeBufferList *pCandidate = pElem->GetValue();

        // The current candidate has more than one buffer (otherwise it is disqualified).
        if(pCandidate->GetHead()->GetNext() != NULL)
        {
            // If we haven't seen any candidates, this one automatically becomes the oldest candidate.
            if(pOldestContainingList == NULL)
            {
                pOldestContainingList = pCandidate;
            }
            // Otherwise, to replace the existing candidate, this candidate must have an older timestamp in its oldest buffer.
            else if((pOldestContainingList->GetHead()->GetMostRecentTimeStamp().QuadPart) > 
                      (pCandidate->GetHead()->GetMostRecentTimeStamp().QuadPart))
            {
                pOldestContainingList = pCandidate;
            }
        }

        pElem = m_pPerThreadBufferList->GetNext(pElem);
    }

    return pOldestContainingList;
}

void EventPipeBufferManager::DeAllocateBuffer(EventPipeBuffer *pBuffer)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(pBuffer != NULL)
    {
        m_sizeOfAllBuffers -= pBuffer->GetSize();
        delete(pBuffer);
#ifdef _DEBUG
        m_numBuffersAllocated--;
#endif // _DEBUG
    }
}

bool EventPipeBufferManager::WriteEvent(Thread *pThread, EventPipeSession &session, EventPipeEvent &event, EventPipeEventPayload &payload, LPCGUID pActivityId, LPCGUID pRelatedActivityId, Thread *pEventThread, StackContents *pStack)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        // The input thread must match the current thread because no lock is taken on the buffer.
        PRECONDITION(pThread == GetThread());
    }
    CONTRACTL_END;

    _ASSERTE(pThread == GetThread());

    // Check to see an event thread was specified.  If not, then use the current thread.
    if(pEventThread == NULL)
    {
        pEventThread = pThread;
    }

    // Before we pick a buffer, make sure the event is enabled.
    if(!event.IsEnabled())
    {
        return false;
    }

    // The event is still enabled.  Mark that the thread is now writing an event.
    pThread->SetEventWriteInProgress(true);

    // Check one more time to make sure that the event is still enabled.
    // We do this because we might be trying to disable tracing and free buffers, so we
    // must make sure that the event is enabled after we mark that we're writing to avoid
    // races with the destructing thread.
    if(!event.IsEnabled())
    {
        return false;
    }

    // See if the thread already has a buffer to try.
    bool allocNewBuffer = false;
    EventPipeBuffer *pBuffer = NULL;
    EventPipeBufferList *pThreadBufferList = pThread->GetEventPipeBufferList();
    if(pThreadBufferList == NULL)
    {
        allocNewBuffer = true;
    }
    else
    {
        // The thread already has a buffer list.  Select the newest buffer and attempt to write into it.
        pBuffer = pThreadBufferList->GetTail();
        if(pBuffer == NULL)
        {
            // This should never happen.  If the buffer list exists, it must contain at least one entry.
            _ASSERT(!"Thread buffer list with zero entries encountered.");
            return false;
        }
        else
        {
            // Attempt to write the event to the buffer.  If this fails, we should allocate a new buffer.
            allocNewBuffer = !pBuffer->WriteEvent(pEventThread, session, event, payload, pActivityId, pRelatedActivityId, pStack);
        }
    }

    // Check to see if we need to allocate a new buffer, and if so, do it here.
    if(allocNewBuffer)
    {
        // We previously switched to preemptive mode here, however, this is not safe and can cause deadlocks.
        // When a GC is started, and background threads are created (for the first BGC), a thread creation event is fired.
        // When control gets here the buffer is allocated, but then the thread hangs waiting for the GC to complete
        // (it was marked as started before creating threads) so that it can switch back to cooperative mode.
        // However, the GC is waiting on this call to return so that it can make forward progress.  Thus it is not safe
        // to switch to preemptive mode here.

        unsigned int requestSize = sizeof(EventPipeEventInstance) + payload.GetSize();
        pBuffer = AllocateBufferForThread(session, pThread, requestSize);
    }

    // Try to write the event after we allocated (or stole) a buffer.
    // This is the first time if the thread had no buffers before the call to this function.
    // This is the second time if this thread did have one or more buffers, but they were full.
    if(allocNewBuffer && pBuffer != NULL)
    {
        allocNewBuffer = !pBuffer->WriteEvent(pEventThread, session, event, payload, pActivityId, pRelatedActivityId, pStack);
    }

    // Mark that the thread is no longer writing an event.
     pThread->SetEventWriteInProgress(false);

#ifdef _DEBUG
    if(!allocNewBuffer)
    {
        InterlockedIncrement(&m_numEventsStored);
    }
    else
    {
        InterlockedIncrement(&m_numEventsDropped);
    }
#endif // _DEBUG
    return !allocNewBuffer;
}

void EventPipeBufferManager::WriteAllBuffersToFile(EventPipeFile *pFile, LARGE_INTEGER stopTimeStamp)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pFile != NULL);
    }
    CONTRACTL_END;

    // TODO: Better version of merge sort.
    // 1. Iterate through all of the threads, adding each buffer to a temporary list.
    // 2. While iterating, get the lowest most recent timestamp.  This is the timestamp that we want to process up to.
    // 3. Process up to the lowest most recent timestamp for the set of buffers.
    // 4. When we get NULLs from each of the buffers on PopNext(), we're done.
    // 5. While iterating if PopNext() == NULL && Empty() == NULL, remove the buffer from the list.  It's empty.
    // 6. While iterating, grab the next lowest most recent timestamp.
    // 7. Walk through the list again and look for any buffers that have a lower most recent timestamp than the next most recent timestamp.
    // 8. If we find one, add it to the list and select its most recent timestamp as the lowest.
    // 9. Process again (go to 3).
    // 10. Continue until there are no more buffers to process.

    // Take the lock before walking the buffer list.
    SpinLockHolder _slh(&m_lock);

    // Naively walk the circular buffer, writing the event stream in timestamp order.
    while(true)
    {
        EventPipeEventInstance *pOldestInstance = NULL;
        EventPipeBuffer *pOldestContainingBuffer = NULL;
        EventPipeBufferList *pOldestContainingList = NULL;
        SListElem<EventPipeBufferList*> *pElem = m_pPerThreadBufferList->GetHead();
        while(pElem != NULL)
        {
            EventPipeBufferList *pBufferList = pElem->GetValue();

            // Peek the next event out of the list.
            EventPipeBuffer *pContainingBuffer = NULL;
            EventPipeEventInstance *pNext = pBufferList->PeekNextEvent(stopTimeStamp, &pContainingBuffer);
            if(pNext != NULL)
            {
                // If it's the oldest event we've seen, then save it.
                if((pOldestInstance == NULL) ||
                   (pOldestInstance->GetTimeStamp()->QuadPart > pNext->GetTimeStamp()->QuadPart)) 
                {
                    pOldestInstance = pNext;
                    pOldestContainingBuffer = pContainingBuffer;
                    pOldestContainingList = pBufferList;
                }
            }

            pElem = m_pPerThreadBufferList->GetNext(pElem);
        }

        if(pOldestInstance == NULL)
        {
            // We're done.  There are no more events.
            break;
        }

        // Write the oldest event.
        pFile->WriteEvent(*pOldestInstance);
#ifdef _DEBUG
        m_numEventsWritten++;
#endif // _DEBUG

        // Pop the event from the buffer.
        pOldestContainingList->PopNextEvent(stopTimeStamp);
    }
}

EventPipeEventInstance* EventPipeBufferManager::GetNextEvent()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Take the lock before walking the buffer list.
    SpinLockHolder _slh(&m_lock);

    // Naively walk the circular buffer, getting the event stream in timestamp order.
    LARGE_INTEGER stopTimeStamp;
    QueryPerformanceCounter(&stopTimeStamp);
    while (true)
    {
        EventPipeEventInstance *pOldestInstance = NULL;
        EventPipeBuffer *pOldestContainingBuffer = NULL;
        EventPipeBufferList *pOldestContainingList = NULL;
        SListElem<EventPipeBufferList*> *pElem = m_pPerThreadBufferList->GetHead();
        while (pElem != NULL)
        {
            EventPipeBufferList *pBufferList = pElem->GetValue();

            // Peek the next event out of the list.
            EventPipeBuffer *pContainingBuffer = NULL;
            EventPipeEventInstance *pNext = pBufferList->PeekNextEvent(stopTimeStamp, &pContainingBuffer);
            if (pNext != NULL)
            {
                // If it's the oldest event we've seen, then save it.
                if ((pOldestInstance == NULL) ||
                    (pOldestInstance->GetTimeStamp()->QuadPart > pNext->GetTimeStamp()->QuadPart))
                {
                    pOldestInstance = pNext;
                    pOldestContainingBuffer = pContainingBuffer;
                    pOldestContainingList = pBufferList;
                }
            }

            pElem = m_pPerThreadBufferList->GetNext(pElem);
        }

        if (pOldestInstance == NULL)
        {
            // We're done.  There are no more events.
            return NULL;
        }

        // Pop the event from the buffer.
        pOldestContainingList->PopNextEvent(stopTimeStamp);

        // Return the oldest event that hasn't yet been processed.
        return pOldestInstance;
    }
}

void EventPipeBufferManager::DeAllocateBuffers()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(EnsureConsistency());

    // Take the thread store lock because we're going to iterate through the thread list.
    {
        ThreadStoreLockHolder tsl;

        // Take the buffer manager manipulation lock.
        SpinLockHolder _slh(&m_lock);

        Thread *pThread = NULL;
        while ((pThread = ThreadStore::GetThreadList(pThread)) != NULL)
        {
            // Get the thread's buffer list.
            EventPipeBufferList *pBufferList = pThread->GetEventPipeBufferList();
            if(pBufferList != NULL)
            {
                // Attempt to free the buffer list.
                // If the thread is using its buffer list skip it.
                // This means we will leak a single buffer, but if tracing is re-enabled, that buffer can be used again.
                if(!pThread->GetEventWriteInProgress())
                {
                    EventPipeBuffer *pBuffer = pBufferList->GetAndRemoveHead();
                    while(pBuffer != NULL)
                    {
                        DeAllocateBuffer(pBuffer);
                        pBuffer = pBufferList->GetAndRemoveHead();
                    }

                    // Remove the list entry from the per thread buffer list.
                    SListElem<EventPipeBufferList*> *pElem = m_pPerThreadBufferList->GetHead();
                    while(pElem != NULL)
                    {
                        EventPipeBufferList* pEntry = pElem->GetValue();
                        if(pEntry == pBufferList)
                        {
                            pElem = m_pPerThreadBufferList->FindAndRemove(pElem);

                            // In DEBUG, make sure that the element was found and removed.
                            _ASSERTE(pElem != NULL);

                            SListElem<EventPipeBufferList*> *pCurElem = pElem;
                            pElem = m_pPerThreadBufferList->GetNext(pElem);
                            delete(pCurElem);
                        }
                        else
                        {
                            pElem = m_pPerThreadBufferList->GetNext(pElem);
                        }
                    }

                    // Remove the list reference from the thread.
                    pThread->SetEventPipeBufferList(NULL);

                    // Now that all of the list elements have been freed, free the list itself.
                    delete(pBufferList);
                    pBufferList = NULL;
                }
#ifdef _DEBUG
                else
                {
                    // We can't deallocate the buffers.
                    m_numBuffersLeaked += pBufferList->GetCount();
                }
#endif // _DEBUG            
            }
        }
    }

    // Now that we've walked through all of the threads, let's see if there are any other buffers
    // that belonged to threads that died during tracing.  We can free these now.

    // Take the buffer manager manipulation lock
    SpinLockHolder _slh(&m_lock);

    SListElem<EventPipeBufferList*> *pElem = m_pPerThreadBufferList->GetHead();
    while(pElem != NULL)
    {
        // Get the list and determine if we can free it.
        EventPipeBufferList *pBufferList = pElem->GetValue();
        if(!pBufferList->OwnedByThread())
        {
            // Iterate over all nodes in the list and de-allocate them.
            EventPipeBuffer *pBuffer = pBufferList->GetAndRemoveHead();
            while(pBuffer != NULL)
            {
                DeAllocateBuffer(pBuffer);
                pBuffer = pBufferList->GetAndRemoveHead();
            }

            // Remove the buffer list from the per-thread buffer list.
            pElem = m_pPerThreadBufferList->FindAndRemove(pElem);
            _ASSERTE(pElem != NULL);

            SListElem<EventPipeBufferList*> *pCurElem = pElem;
            pElem = m_pPerThreadBufferList->GetNext(pElem);
            delete(pCurElem);

            // Now that all of the list elements have been freed, free the list itself.
            delete(pBufferList);
            pBufferList = NULL;
        }
        else
        {
            pElem = m_pPerThreadBufferList->GetNext(pElem);
        }
    } 
}

#ifdef _DEBUG
bool EventPipeBufferManager::EnsureConsistency()
{
    LIMITED_METHOD_CONTRACT;

    SListElem<EventPipeBufferList*> *pElem = m_pPerThreadBufferList->GetHead();
    while(pElem != NULL)
    {
        EventPipeBufferList *pBufferList = pElem->GetValue();

        _ASSERTE(pBufferList->EnsureConsistency());

        pElem = m_pPerThreadBufferList->GetNext(pElem);
    }

    return true;
}
#endif // _DEBUG

EventPipeBufferList::EventPipeBufferList(EventPipeBufferManager *pManager)
{
    LIMITED_METHOD_CONTRACT;

    m_pManager = pManager;
    m_pHeadBuffer = NULL;
    m_pTailBuffer = NULL;
    m_bufferCount = 0;
    m_pReadBuffer = NULL;
    m_ownedByThread = true;

#ifdef _DEBUG
    m_pCreatingThread = GetThread();
#endif // _DEBUG
}

EventPipeBuffer* EventPipeBufferList::GetHead()
{
    LIMITED_METHOD_CONTRACT;

    return m_pHeadBuffer;
}

EventPipeBuffer* EventPipeBufferList::GetTail()
{
    LIMITED_METHOD_CONTRACT;

    return m_pTailBuffer;
}

void EventPipeBufferList::InsertTail(EventPipeBuffer *pBuffer)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pBuffer != NULL);
    }
    CONTRACTL_END;

    _ASSERTE(EnsureConsistency());

    // Ensure that the input buffer didn't come from another list that was improperly cleaned up.
    _ASSERTE((pBuffer->GetNext() == NULL) && (pBuffer->GetPrevious() == NULL));

    // First node in the list.
    if(m_pTailBuffer == NULL)
    {
        m_pHeadBuffer = m_pTailBuffer = pBuffer;
    }
    else
    {
        // Set links between the old and new tail nodes.
        m_pTailBuffer->SetNext(pBuffer);
        pBuffer->SetPrevious(m_pTailBuffer);

        // Set the new tail node.
        m_pTailBuffer = pBuffer;
    }

    m_bufferCount++;

    _ASSERTE(EnsureConsistency());
}

EventPipeBuffer* EventPipeBufferList::GetAndRemoveHead()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(EnsureConsistency());

    EventPipeBuffer *pRetBuffer = NULL;
    if(m_pHeadBuffer != NULL)
    {
        // Save the head node.
        pRetBuffer = m_pHeadBuffer;

        // Set the new head node.
        m_pHeadBuffer = m_pHeadBuffer->GetNext();

        // Update the head node's previous pointer.
        if(m_pHeadBuffer != NULL)
        {
            m_pHeadBuffer->SetPrevious(NULL);
        }
        else
        {
            // We just removed the last buffer from the list.
            // Make sure both head and tail pointers are NULL.
            m_pTailBuffer = NULL;
        }

        // Clear the next pointer of the old head node.
        pRetBuffer->SetNext(NULL);

        // Ensure that the old head node has no dangling references.
        _ASSERTE((pRetBuffer->GetNext() == NULL) && (pRetBuffer->GetPrevious() == NULL));

        // Decrement the count of buffers in the list.
        m_bufferCount--;
    }

    _ASSERTE(EnsureConsistency());

    return pRetBuffer;
}

unsigned int EventPipeBufferList::GetCount() const
{
    LIMITED_METHOD_CONTRACT;

    return m_bufferCount;
}

EventPipeEventInstance* EventPipeBufferList::PeekNextEvent(LARGE_INTEGER beforeTimeStamp, EventPipeBuffer **pContainingBuffer)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Get the current read buffer.
    // If it's not set, start with the head buffer.
    if(m_pReadBuffer == NULL)
    {
        m_pReadBuffer = m_pHeadBuffer;
    }

    // If the read buffer is still NULL, then this list contains no buffers.
    if(m_pReadBuffer == NULL)
    {
        return NULL;
    }

    // Get the next event in the buffer.
    EventPipeEventInstance *pNext = m_pReadBuffer->PeekNext(beforeTimeStamp);

    // If the next event is NULL, then go to the next buffer.
    if(pNext == NULL)
    {
        m_pReadBuffer = m_pReadBuffer->GetNext();
        if(m_pReadBuffer != NULL)
        {
            pNext = m_pReadBuffer->PeekNext(beforeTimeStamp);
        }
    }

    // Set the containing buffer.
    if(pNext != NULL && pContainingBuffer != NULL)
    {
        *pContainingBuffer = m_pReadBuffer;
    }

    // Make sure pContainingBuffer is properly set.
    _ASSERTE((pNext == NULL) || (pNext != NULL && pContainingBuffer == NULL) || (pNext != NULL && *pContainingBuffer == m_pReadBuffer));
    return pNext;
}

EventPipeEventInstance* EventPipeBufferList::PopNextEvent(LARGE_INTEGER beforeTimeStamp)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Get the next event.
    EventPipeBuffer *pContainingBuffer = NULL;
    EventPipeEventInstance *pNext = PeekNextEvent(beforeTimeStamp, &pContainingBuffer);

    // Check to see if we need to clean-up the buffer that contained the previously popped event.
    if(pContainingBuffer->GetPrevious() != NULL)
    {
            // Remove the previous node.  The previous node should always be the head node.
            EventPipeBuffer *pRemoved = GetAndRemoveHead();
            _ASSERTE(pRemoved != pContainingBuffer);
            _ASSERTE(pContainingBuffer == GetHead());

            // De-allocate the buffer.
            m_pManager->DeAllocateBuffer(pRemoved);
    }

    // If the event is non-NULL, pop it.
    if(pNext != NULL && pContainingBuffer != NULL)
    {
        pContainingBuffer->PopNext(beforeTimeStamp);
    }

    return pNext;
}

bool EventPipeBufferList::OwnedByThread()
{
    LIMITED_METHOD_CONTRACT;
    return m_ownedByThread;
}

void EventPipeBufferList::SetOwnedByThread(bool value)
{
    LIMITED_METHOD_CONTRACT;
    m_ownedByThread = value;
}

#ifdef _DEBUG
Thread* EventPipeBufferList::GetThread()
{
    LIMITED_METHOD_CONTRACT;

    return m_pCreatingThread;
}

bool EventPipeBufferList::EnsureConsistency()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Either the head and tail nodes are both NULL or both are non-NULL.
    _ASSERTE((m_pHeadBuffer == NULL && m_pTailBuffer == NULL) || (m_pHeadBuffer != NULL && m_pTailBuffer != NULL));

    // If the list is NULL, check the count and return.
    if(m_pHeadBuffer == NULL)
    {
        _ASSERTE(m_bufferCount == 0);
        return true;
    }

    // If the list is non-NULL, walk the list forward until we get to the end.
    unsigned int nodeCount = (m_pHeadBuffer != NULL) ? 1 : 0;
    EventPipeBuffer *pIter = m_pHeadBuffer;
    while(pIter->GetNext() != NULL)
    {
        pIter = pIter->GetNext();
        nodeCount++;

        // Check for consistency of the buffer itself.
        // NOTE: We can't check the last buffer because the owning thread could
        // be writing to it, which could result in false asserts.
        if(pIter->GetNext() != NULL)
        {
            _ASSERTE(pIter->EnsureConsistency());
        }

        // Check for cycles.
        _ASSERTE(nodeCount <= m_bufferCount);
    }

    // When we're done with the walk, pIter must point to the tail node.
    _ASSERTE(pIter == m_pTailBuffer);

    // Node count must equal the buffer count.
    _ASSERTE(nodeCount == m_bufferCount);

    // Now, walk the list in reverse.
    pIter = m_pTailBuffer;
    nodeCount = (m_pTailBuffer != NULL) ? 1 : 0;
    while(pIter->GetPrevious() != NULL)
    {
        pIter = pIter->GetPrevious();
        nodeCount++;

        // Check for cycles.
        _ASSERTE(nodeCount <= m_bufferCount);
    }

    // When we're done with the reverse walk, pIter must point to the head node.
    _ASSERTE(pIter == m_pHeadBuffer);

    // Node count must equal the buffer count.
    _ASSERTE(nodeCount == m_bufferCount);

    // We're done.
    return true;
}
#endif // _DEBUG

#endif // FEATURE_PERFTRACING
