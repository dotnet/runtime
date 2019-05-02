// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipe.h"
#include "eventpipeconfiguration.h"
#include "eventpipebuffer.h"
#include "eventpipebuffermanager.h"

#ifdef FEATURE_PERFTRACING

void ReleaseEventPipeThreadRef(EventPipeThread* pThread) { LIMITED_METHOD_CONTRACT; pThread->Release(); }
void AcquireEventPipeThreadRef(EventPipeThread* pThread) { LIMITED_METHOD_CONTRACT; pThread->AddRef(); } 

#ifndef __GNUC__
__declspec(thread) EventPipeThreadHolder EventPipeThread::gCurrentEventPipeThreadHolder;;
#else // !__GNUC__
thread_local EventPipeThreadHolder EventPipeThread::gCurrentEventPipeThreadHolder;
#endif // !__GNUC__

EventPipeThread::EventPipeThread()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    m_lock.Init(LOCK_TYPE_DEFAULT);
    m_refCount = 0;
}

EventPipeThread::~EventPipeThread()
{
    _ASSERTE(m_pWriteBuffer == nullptr);
    _ASSERTE(m_pBufferList == nullptr);
}

/*static */ EventPipeThread* EventPipeThread::Get()
{
    LIMITED_METHOD_CONTRACT;
    return gCurrentEventPipeThreadHolder;
}

/*static */ void EventPipeThread::Set(EventPipeThread* pThread)
{
    LIMITED_METHOD_CONTRACT;
    gCurrentEventPipeThreadHolder = pThread;
}

void EventPipeThread::AddRef()
{
    LIMITED_METHOD_CONTRACT;
    FastInterlockIncrement(&m_refCount);
}

void EventPipeThread::Release()
{
    LIMITED_METHOD_CONTRACT;
    if (FastInterlockDecrement(&m_refCount) == 0)
    {
        delete this;
    }
}

SpinLock* EventPipeThread::GetLock()
{
    LIMITED_METHOD_CONTRACT;
    return &m_lock;
}

EventPipeBuffer* EventPipeThread::GetWriteBuffer()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_lock.OwnedByCurrentThread());
    _ASSERTE(m_pWriteBuffer == nullptr || m_pWriteBuffer->GetVolatileState() == EventPipeBufferState::WRITABLE);
    return m_pWriteBuffer;
}

void EventPipeThread::SetWriteBuffer(EventPipeBuffer* pNewBuffer)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_lock.OwnedByCurrentThread());
    _ASSERTE(m_pWriteBuffer == nullptr || m_pWriteBuffer->GetVolatileState() == EventPipeBufferState::WRITABLE);
    _ASSERTE(pNewBuffer == nullptr || pNewBuffer->GetVolatileState() == EventPipeBufferState::WRITABLE);
    if (m_pWriteBuffer)
    {
        m_pWriteBuffer->ConvertToReadOnly();
    }
    m_pWriteBuffer = pNewBuffer;
}

EventPipeBufferList* EventPipeThread::GetBufferList()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(EventPipe::IsBufferManagerLockOwnedByCurrentThread());
    return m_pBufferList;
}

void EventPipeThread::SetBufferList(EventPipeBufferList* pNewBufferList)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(EventPipe::IsBufferManagerLockOwnedByCurrentThread());
    m_pBufferList = pNewBufferList;
}


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
    m_writeEventSuspending = FALSE;

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

    // setting this true should have no practical effect other than satisfying asserts at this point.
    m_writeEventSuspending = TRUE;
    DeAllocateBuffers();
}

#ifdef DEBUG
bool EventPipeBufferManager::IsLockOwnedByCurrentThread()
{
    return m_lock.OwnedByCurrentThread();
}
#endif

EventPipeBuffer* EventPipeBufferManager::AllocateBufferForThread(EventPipeSession &session, unsigned int requestSize, BOOL & writeSuspended)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(requestSize > 0);
    }
    CONTRACTL_END;

    // Allocating a buffer requires us to take the lock.
    SpinLockHolder _slh(&m_lock);

    // if we are deallocating then give up, see the comments in SuspendWriteEvents() for why this is important.
    if (m_writeEventSuspending.Load())
    {
        writeSuspended = TRUE;
        return NULL;
    }

    // Determine if the requesting thread has at least one buffer.
    // If not, we guarantee that each thread gets at least one (to prevent thrashing when the circular buffer size is too small).
    bool allocateNewBuffer = false;

    EventPipeThread *pEventPipeThread = EventPipeThread::Get();

    if (pEventPipeThread == NULL)
    {
        EX_TRY
        {
            pEventPipeThread = new EventPipeThread();
            EventPipeThread::Set(pEventPipeThread);
        }
        EX_CATCH
        {
            pEventPipeThread = NULL;
        }
        EX_END_CATCH(SwallowAllExceptions);

        if (pEventPipeThread == NULL)
        {
            return NULL;
        }
    }

    EventPipeBufferList *pThreadBufferList = pEventPipeThread->GetBufferList();
    if (pThreadBufferList == NULL)
    {
        pThreadBufferList = new (nothrow) EventPipeBufferList(this, pEventPipeThread);

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
        pEventPipeThread->SetBufferList(pThreadBufferList);
        allocateNewBuffer = true;
    }

    // Determine if policy allows us to allocate another buffer
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
    EventPipeBuffer* pNewBuffer = NULL;
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
            pNewBuffer = new EventPipeBuffer(bufferSize DEBUG_ARG(pEventPipeThread));
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

    // Check one more time to make sure that the event is still enabled.
    // We do this because we might be trying to disable tracing and free buffers, so we
    // must make sure that the event is enabled after we mark that we're writing to avoid
    // races with the destructing thread.
    if(!event.IsEnabled())
    {
        return false;
    }

    StackContents stackContents;
    if (pStack == NULL && event.NeedStack() && !session.RundownEnabled())
    {
        EventPipe::WalkManagedStackForCurrentThread(stackContents);
        pStack = &stackContents;
    }

    // See if the thread already has a buffer to try.
    bool allocNewBuffer = false;
    EventPipeBuffer *pBuffer = NULL;

    EventPipeThread *pEventPipeThread  = EventPipeThread::Get();

    if(pEventPipeThread  == NULL)
    {
        allocNewBuffer = true;
    }
    else
    {
        SpinLockHolder _slh(pEventPipeThread->GetLock());
        pBuffer = pEventPipeThread->GetWriteBuffer();

        if(pBuffer == NULL)
        {
            allocNewBuffer = true;
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
        BOOL writeSuspended = FALSE;
        pBuffer = AllocateBufferForThread(session, requestSize, writeSuspended);
        if (pBuffer == NULL)
        {
            // We treat this as the WriteEvent() call occurring after this session stopped listening for events, effectively the
            // same as if event.IsEnabled() test above returned false.
            if (writeSuspended)
            {
                return false;
            }
        }
        else
        {
            EventPipeThread *pEventPipeThread = EventPipeThread::Get();
            _ASSERTE(pEventPipeThread != NULL);
            {
                SpinLockHolder _slh(pEventPipeThread->GetLock());
                if (m_writeEventSuspending.Load())
                {
                    // After leaving the manager's lock in AllocateBufferForThread some other thread decided to suspend writes.
                    // We need to immediately return the buffer we just took without storing it or writing to it.
                    // SuspendWriteEvent() is spinning waiting for this buffer to be relinquished.
                    pBuffer->ConvertToReadOnly();

                    // We treat this as the WriteEvent() call occurring after this session stopped listening for events, effectively the 
                    // same as if event.IsEnabled() returned false.
                    return false;
                }
                else
                {
                    pEventPipeThread->SetWriteBuffer(pBuffer);

                    // Try to write the event after we allocated a buffer.
                    // This is the first time if the thread had no buffers before the call to this function.
                    // This is the second time if this thread did have one or more buffers, but they were full.
                    allocNewBuffer = !pBuffer->WriteEvent(pEventThread, session, event, payload, pActivityId, pRelatedActivityId, pStack);
                }
            }
        }
    }


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
        PRECONDITION(pFile != nullptr);
        PRECONDITION(EventPipe::GetLock()->OwnedByCurrentThread());
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

    // Naively walk the circular buffer, writing the event stream in timestamp order.
    m_numEventsWritten = 0;
    while(true)
    {
        EventPipeEventInstance *pOldestInstance = NULL;
        EventPipeBuffer *pOldestContainingBuffer = NULL;
        EventPipeBufferList *pOldestContainingList = NULL;

        CQuickArrayList<EventPipeBuffer*> bufferList;
        CQuickArrayList<EventPipeBufferList*> bufferListList;
        {
            // Take the lock before walking the buffer list.
            SpinLockHolder _slh(&m_lock);
            SListElem<EventPipeBufferList*> *pElem = m_pPerThreadBufferList->GetHead();
            while(pElem != NULL)
            {
                EventPipeBufferList* pBufferList = pElem->GetValue();
                EventPipeBuffer* pBuffer = pBufferList->TryGetBuffer(stopTimeStamp);
                if (pBuffer != nullptr)
                {
                    bufferListList.Push(pBufferList);
                    bufferList.Push(pBuffer);
                }
                pElem = m_pPerThreadBufferList->GetNext(pElem);
            }
        }

        for (size_t i = 0 ; i < bufferList.Size(); i++)
        {
            EventPipeBufferList* pBufferList = bufferListList[i];
            EventPipeBuffer* pBuffer = bufferList[i];
            pBufferList->ConvertBufferToReadOnly(pBuffer);

            // Peek the next event out of the buffer.
            EventPipeBuffer *pContainingBuffer = pBuffer;
            EventPipeEventInstance *pNext = pBuffer->PeekNext(stopTimeStamp);
            if (pNext != NULL)
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
        }

        if(pOldestInstance == NULL)
        {
            // We're done.  There are no more events.
            break;
        }

        // Write the oldest event.
        pFile->WriteEvent(*pOldestInstance);

        m_numEventsWritten++;

        {
            SpinLockHolder _slh(&m_lock);
            // Pop the event from the buffer.
            pOldestContainingList->PopNextEvent(pOldestContainingBuffer, pOldestInstance);
        }
    }

    if (m_numEventsWritten > 0)
        pFile->Flush();
}

EventPipeEventInstance* EventPipeBufferManager::GetNextEvent()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(!EventPipe::GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    LARGE_INTEGER stopTimeStamp;
    QueryPerformanceCounter(&stopTimeStamp);

    EventPipeEventInstance *pOldestInstance = NULL;
    EventPipeBuffer *pOldestContainingBuffer = NULL;
    EventPipeBufferList *pOldestContainingList = NULL;

    CQuickArrayList<EventPipeBuffer*> bufferList;
    CQuickArrayList<EventPipeBufferList*> bufferListList;
    {
        // Take the lock before walking the buffer list.
        SpinLockHolder _slh(&m_lock);
        SListElem<EventPipeBufferList*> *pElem = m_pPerThreadBufferList->GetHead();
        while(pElem != NULL)
        {
            EventPipeBufferList* pBufferList = pElem->GetValue();
            EventPipeBuffer* pBuffer = pBufferList->TryGetBuffer(stopTimeStamp);
            if (pBuffer != nullptr)
            {
                bufferListList.Push(pBufferList);
                bufferList.Push(pBuffer);
            }
            pElem = m_pPerThreadBufferList->GetNext(pElem);
        }
    }

    for (size_t i = 0 ; i < bufferList.Size(); i++)
    {
        EventPipeBufferList* pBufferList = bufferListList[i];
        EventPipeBuffer* pBuffer = bufferList[i];
        pBufferList->ConvertBufferToReadOnly(pBuffer);

        // Peek the next event out of the buffer.
        EventPipeBuffer *pContainingBuffer = pBuffer;
        
        // PERF: This may be too aggressive? If this method is being called frequently enough to keep pace with the
        // writing threads we could be in a state of high lock contention and lots of churning buffers. Each writer
        // would take several locks, allocate a new buffer, write one event into it, then the reader would take the
        // lock, convert the buffer to read-only and read the single event out of it. Allowing more events to accumulate
        // in the buffers before converting between writable and read-only amortizes a lot of the overhead. One way 
        // to achieve that would be picking a stopTimeStamp that was Xms in the past. This would let Xms of events
        // to accumulate in the write buffer before we converted it and forced the writer to allocate another. Other more
        // sophisticated approaches would probably build a low overhead synchronization mechanism to read and write the 
        // buffer at the same time.
        EventPipeEventInstance *pNext = pBuffer->PeekNext(stopTimeStamp);
        if (pNext != NULL)
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
    }

    if(pOldestInstance == NULL)
    {
        // We're done.  There are no more events.
        return nullptr;
    }

    {
        SpinLockHolder _slh(&m_lock);
        // Pop the event from the buffer.
        pOldestContainingList->PopNextEvent(pOldestContainingBuffer, pOldestInstance);
    }
    
    // Return the oldest event that hasn't yet been processed.
    return pOldestInstance;
}

void EventPipeBufferManager::SuspendWriteEvent()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(EnsureConsistency());

    // All calls to this method must be synchronized by our caller
    _ASSERTE(EventPipe::IsLockOwnedByCurrentThread());

    CQuickArrayList<EventPipeThread*> threadList;
    {
        SpinLockHolder _slh(&m_lock);
        m_writeEventSuspending.Store(TRUE);
        // From this point until m_writeEventSuspending is reset to FALSE it is impossible
        // for new EventPipeBufferLists to be added to the m_pPerThreadBufferList. The only
        // way AllocateBufferForThread is allowed to add one is by:
        // 1) take m_lock - AllocateBufferForThread can't own it now because this thread owns it,
        //                  but after this thread gives it up lower in this function it could be acquired.
        // 2) observe m_writeEventSuspending = False - that won't happen, acquiring m_lock
        //                  guarantees AllocateBufferForThread will observe all the memory changes this
        //                  thread made prior to releasing m_lock and we've already set it TRUE.
        // This ensures that we iterate over the list of threads below we've got the complete list.
        SListElem<EventPipeBufferList*> *pElem = m_pPerThreadBufferList->GetHead();
        while(pElem != NULL)
        {
            threadList.Push(pElem->GetValue()->GetThread());
            pElem = m_pPerThreadBufferList->GetNext(pElem);
        }
    }

    // Iterate through all the threads, forcing them to finish writes in progress inside EventPipeThread::m_lock,
    // relinquish any buffers stored in EventPipeThread::m_pWriteBuffer and prevent storing new ones.
    for (size_t i = 0 ; i < threadList.Size(); i++)
    {
        EventPipeThread* pThread = threadList[i];
        {
            SpinLockHolder _slh(pThread->GetLock());
            pThread->SetWriteBuffer(nullptr);
            // From this point until m_writeEventSuspending is reset to FALSE it is impossible
            // for new EventPipeBufferLists to be added to the m_pPerThreadBufferList. The only
            // way AllocateBufferForThread is allowed to add one is by:
            // 1) take m_lock - AllocateBufferForThread can't own it now because this thread owns it,
            //                  but after this thread gives it up lower in this function it could be acquired.
            // 2) observe m_writeEventSuspending = False - that won't happen, acquiring m_lock
            //                  guarantees AllocateBufferForThread will observe all the memory changes this
            //                  thread made prior to releasing m_lock and we've already set it TRUE.        
        }
    }

    // Wait for any straggler WriteEvent threads that may have already allocated a buffer but
    // hadn't yet relinquished it.
    {
        SpinLockHolder _slh(&m_lock);
        SListElem<EventPipeBufferList*> *pElem = m_pPerThreadBufferList->GetHead();
        while (pElem != NULL)
        {
            // Get the list and remove it from the thread.
            EventPipeBufferList *pBufferList = pElem->GetValue();
            for (EventPipeBuffer* pBuffer = pBufferList->GetHead(); pBuffer != nullptr; pBuffer = pBuffer->GetNext())
            {
                // Above we guaranteed that other threads wouldn't acquire new buffers or keep the ones they
                // already have indefinitely, but we haven't quite guaranteed the buffer has been relinquished 
                // back to us. It's possible the WriteEvent thread allocated the buffer before we took m_lock
                // above, but it hasn't yet acquired EventPipeThread::m_lock in order to observe that it needs
                // to relinquish the buffer. In this state, it has a pointer to the buffer stored in registers
                // or on the stack. If the thread is in that tiny window, all we have to do is wait for it.
                YIELD_WHILE(pBuffer->GetVolatileState() != EventPipeBufferState::READ_ONLY);
            }
            pElem = m_pPerThreadBufferList->GetNext(pElem);
        }
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
    _ASSERTE(m_writeEventSuspending);

    // Take the buffer manager manipulation lock
    SpinLockHolder _slh(&m_lock);

    SListElem<EventPipeBufferList*> *pElem = m_pPerThreadBufferList->GetHead();
    while(pElem != NULL)
    {
        // Get the list and determine if we can free it.
        EventPipeBufferList *pBufferList = pElem->GetValue();
        EventPipeThread *pThread = pBufferList->GetThread();
        pThread->SetBufferList(nullptr);

        // Iterate over all nodes in the list and deallocate them.
        EventPipeBuffer *pBuffer = pBufferList->GetAndRemoveHead();
        while (pBuffer != NULL)
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

        // Now that all the list elements have been freed, free the list itself.
        delete(pBufferList);
        pBufferList = NULL;
    }
}

void EventPipeBufferManager::ResumeWriteEvent()
{
    LIMITED_METHOD_CONTRACT;

    // All calls to this method must be synchronized by our caller.

    _ASSERTE(EventPipe::IsLockOwnedByCurrentThread());
    _ASSERTE(EnsureConsistency());

    m_writeEventSuspending.Store(FALSE);

    // At this point threads are allowed to again allocate new BufferLists and Buffers. However our caller
    // presumablyh disabled all the events and until events are re-enabled no thread is going to get past
    // the event.IsEnabled() checks in WriteEvent() to make any of those allocations happen.
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

EventPipeBufferList::EventPipeBufferList(EventPipeBufferManager *pManager, EventPipeThread* pThread)
{
    LIMITED_METHOD_CONTRACT;

    m_pManager = pManager;
    m_pThread = pThread;
    m_pHeadBuffer = NULL;
    m_pTailBuffer = NULL;
    m_bufferCount = 0;
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

EventPipeBuffer* EventPipeBufferList::TryGetBuffer(LARGE_INTEGER beforeTimeStamp)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(EventPipe::IsBufferManagerLockOwnedByCurrentThread());
    /**
     * There are 4 cases we need to handle in this function:
     * 1) There is no buffer in the list, in this case, return nullptr
     * 2) The head buffer is written to but not read yet, in this case, return that buffer
     *    2.1) It is possible that the head buffer is the only buffer that is created and is empty, or
     *    2.2) The head buffer is written to but not read
     *    We cannot differentiate the two cases without reading it - but it is okay, in both cases, the buffer represents the head of the buffer list. 
     *    Note that writing to the buffer can happen after we return from this function, and it is also okay.
     * 3.) The head buffer is read but not completely reading, and
     * 4.) The head buffer is read completely.
     *     This case requires special attention because it is possible that the next buffer in the list contain the oldest event. Fortunately, it is 
     *     already read so it is safe to read it to determine this case.
     *
     * In any case, if the desired buffer is created after beforeTimeStamp, then we can stop.
     */

    if (this->m_pHeadBuffer == nullptr)
    {
        // Case 1
        return nullptr;
    }
    
    EventPipeBuffer* candidate = nullptr;
    EventPipeBufferState bufferState = this->m_pHeadBuffer->GetVolatileState();
    if (bufferState != EventPipeBufferState::READ_ONLY)
    {
        // Case 2 (2.1 or 2.2)
        candidate = this->m_pHeadBuffer;
    }
    else
    {
        if (this->m_pHeadBuffer->PeekNext(beforeTimeStamp))
        {
            // Case 3
            candidate = this->m_pHeadBuffer;
        }
        else
        {
            // Case 4
            candidate = this->m_pHeadBuffer->GetNext();
        }
    }

    if (candidate == nullptr || candidate->GetCreationTimeStamp().QuadPart >= beforeTimeStamp.QuadPart)
    {
        // If the oldest buffer is still newer than the beforeTimeStamp, we can stop.
        return nullptr;
    }

    return candidate;
}

void EventPipeBufferList::ConvertBufferToReadOnly(EventPipeBuffer* pNewReadBuffer)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pNewReadBuffer != nullptr);
    _ASSERTE(!EventPipe::IsBufferManagerLockOwnedByCurrentThread());
    {
        SpinLockHolder _slh(m_pThread->GetLock());
        if (m_pThread->GetWriteBuffer() == pNewReadBuffer)
        {
            m_pThread->SetWriteBuffer(nullptr);
        }
    }
}

void EventPipeBufferList::PopNextEvent(EventPipeBuffer *pContainingBuffer, EventPipeEventInstance *pNext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

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
        pContainingBuffer->PopNext(pNext);
    }
}

EventPipeThread* EventPipeBufferList::GetThread()
{
    LIMITED_METHOD_CONTRACT;
    return m_pThread;
}

#ifdef _DEBUG
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
