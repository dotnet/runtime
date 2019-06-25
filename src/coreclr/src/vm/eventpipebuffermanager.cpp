// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipe.h"
#include "eventpipeconfiguration.h"
#include "eventpipebuffer.h"
#include "eventpipebuffermanager.h"
#include "eventpipeeventpayload.h"
#include "eventpipefile.h"
#include "eventpipethread.h"
#include "eventpipesession.h"


#ifdef FEATURE_PERFTRACING

template <typename T>
T Clamp(T min, T value, T max)
{
    STATIC_CONTRACT_LEAF;
    return Min(Max(min, value), max);
}

EventPipeBufferManager::EventPipeBufferManager(EventPipeSession* pSession, size_t maxSizeOfAllBuffers, size_t sequencePointAllocationBudget)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pSession = pSession;
    m_pThreadSessionStateList = new SList<SListElem<EventPipeThreadSessionState *>>();
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

    m_pCurrentEvent = nullptr;
    m_pCurrentBuffer = nullptr;
    m_pCurrentBufferList = nullptr;

    m_maxSizeOfAllBuffers = Clamp((size_t)100 * 1024, maxSizeOfAllBuffers, (size_t)ULONG_MAX);

    if (sequencePointAllocationBudget == 0)
    {
        // sequence points disabled
        m_sequencePointAllocationBudget = 0;
        m_remainingSequencePointAllocationBudget = 0;
    }
    else
    {
        m_sequencePointAllocationBudget = Clamp((size_t)1024 * 1024, sequencePointAllocationBudget, (size_t)1024 * 1024 * 1024);
        m_remainingSequencePointAllocationBudget = m_sequencePointAllocationBudget;
    }
    m_sequencePoints.Init();
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

EventPipeBuffer* EventPipeBufferManager::AllocateBufferForThread(EventPipeThreadSessionState* pSessionState,
                                                                 unsigned int requestSize,
                                                                 BOOL & writeSuspended)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pSessionState != NULL);
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

    bool allocateNewBuffer = false;

    EventPipeBufferList *pThreadBufferList = pSessionState->GetBufferList();
    if (pThreadBufferList == NULL)
    {
        pThreadBufferList = new (nothrow) EventPipeBufferList(this, pSessionState->GetThread());
        if (pThreadBufferList == NULL)
        {
            return NULL;
        }

        SListElem<EventPipeThreadSessionState *> *pElem = new (nothrow) SListElem<EventPipeThreadSessionState *>(pSessionState);
        if (pElem == NULL)
        {
            delete pThreadBufferList;
            return NULL;
        }

        m_pThreadSessionStateList->InsertTail(pElem);
        pSessionState->SetBufferList(pThreadBufferList);
    }

    // Determine if policy allows us to allocate another buffer
    size_t availableBufferSize = m_maxSizeOfAllBuffers - m_sizeOfAllBuffers;
    if (requestSize <= availableBufferSize)
    {
        allocateNewBuffer = true;
    }

    EventPipeBuffer *pNewBuffer = NULL;
    if (allocateNewBuffer)
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
        _ASSERTE(requestSize <= availableBufferSize);
        bufferSize = Max(requestSize, bufferSize);
        bufferSize = Min((unsigned int)bufferSize, (unsigned int)availableBufferSize);

        // Don't allow the buffer size to exceed 1MB.
        const unsigned int maxBufferSize = 1024 * 1024;
        bufferSize = Min(bufferSize, maxBufferSize);

        // EX_TRY is used here as opposed to new (nothrow) because
        // the constructor also allocates a private buffer, which
        // could throw, and cannot be easily checked
        EX_TRY
        {
            // The sequence counter is exclusively mutated on this thread so this is a thread-local
            // read.
            unsigned int sequenceNumber = pSessionState->GetVolatileSequenceNumber();
            pNewBuffer = new EventPipeBuffer(bufferSize, pSessionState->GetThread(), sequenceNumber);
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
        if (m_sequencePointAllocationBudget != 0)
        {
            // sequence point bookkeeping
            if (bufferSize >= m_remainingSequencePointAllocationBudget)
            {
                EventPipeSequencePoint* pSequencePoint = new (nothrow) EventPipeSequencePoint();
                if (pSequencePoint != NULL)
                {
                    InitSequencePointThreadListHaveLock(pSequencePoint);
                    EnqueueSequencePoint(pSequencePoint);
                }
                m_remainingSequencePointAllocationBudget = m_sequencePointAllocationBudget;
            }
            else
            {
                m_remainingSequencePointAllocationBudget -= bufferSize;
            }
        }
#ifdef _DEBUG
        m_numBuffersAllocated++;
#endif // _DEBUG
    }

    // Set the buffer on the thread.
    if (pNewBuffer != NULL)
    {
        pThreadBufferList->InsertTail(pNewBuffer);
        return pNewBuffer;
    }

    return NULL;
}

void EventPipeBufferManager::EnqueueSequencePoint(EventPipeSequencePoint* pSequencePoint)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(m_lock.OwnedByCurrentThread());
    }
    CONTRACTL_END;

    m_sequencePoints.InsertTail(pSequencePoint);
}

void EventPipeBufferManager::InitSequencePointThreadList(EventPipeSequencePoint* pSequencePoint)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(!IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    SpinLockHolder __slh(&m_lock);
    InitSequencePointThreadListHaveLock(pSequencePoint);
}

void EventPipeBufferManager::InitSequencePointThreadListHaveLock(EventPipeSequencePoint* pSequencePoint)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    SListElem<EventPipeThreadSessionState*> *pElem = m_pThreadSessionStateList->GetHead();
    while (pElem != NULL)
    {
        EventPipeThreadSessionState* pSessionState = pElem->GetValue();

        // The sequence number captured here is not guaranteed to be the most recent sequence number, nor
        // is it guaranteed to match the number of events we would observe in the thread's write buffer
        // memory. This is only used as a lower bound on the number of events the thread has attempted to
        // write at the timestamp we will capture below.
        //
        // The sequence number is the value that will be used by the next event, so the last written
        // event is one less. Sequence numbers are allowed to overflow, so going backwards is allowed to
        // underflow.
        unsigned int sequenceNumber = pSessionState->GetVolatileSequenceNumber() - 1;
        EX_TRY
        {
            pSequencePoint->ThreadSequenceNumbers.Add(pSessionState, sequenceNumber);
            pSessionState->GetThread()->AddRef();
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(SwallowAllExceptions);

        pElem = m_pThreadSessionStateList->GetNext(pElem);
    }

    // This needs to come after querying the thread sequence numbers to ensure that any recorded
    // sequence number is <= the actual sequence number at this timestamp
    PRECONDITION(m_lock.OwnedByCurrentThread());
    QueryPerformanceCounter(&pSequencePoint->TimeStamp);
}

void EventPipeBufferManager::DequeueSequencePoint()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(m_lock.OwnedByCurrentThread());
    }
    CONTRACTL_END;

    delete m_sequencePoints.RemoveHead();
}

bool EventPipeBufferManager::TryPeekSequencePoint(EventPipeSequencePoint** ppSequencePoint)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(m_lock.OwnedByCurrentThread());
    }
    CONTRACTL_END;

    *ppSequencePoint = m_sequencePoints.GetHead();
    return *ppSequencePoint != NULL;
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

    if (pBuffer != NULL)
    {
        m_sizeOfAllBuffers -= pBuffer->GetSize();
        delete (pBuffer);
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

    // Check to see an event thread was specified.  If not, then use the current thread.
    if (pEventThread == NULL)
    {
        pEventThread = pThread;
    }

    // Before we pick a buffer, make sure the event is enabled.
    if (!event.IsEnabled())
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

    EventPipeThread *pEventPipeThread = EventPipeThread::Get();

    if (pEventPipeThread == NULL)
    {
        return false;
    }

    EventPipeThreadSessionState* pSessionState = NULL;
    {
        SpinLockHolder _slh(pEventPipeThread->GetLock());
        if (m_writeEventSuspending.LoadWithoutBarrier())
        {
            // This session is suspending, we need to avoid initializing any session state and exit
            return false;
        }
        pSessionState = pEventPipeThread->GetOrCreateSessionState(m_pSession);
        if (pSessionState == NULL)
        {
            return false;
        }
        pBuffer = pSessionState->GetWriteBuffer();
        if (pBuffer == NULL)
        {
            allocNewBuffer = true;
        }
        else
        {
            // Attempt to write the event to the buffer.  If this fails, we should allocate a new buffer.
            if (pBuffer->WriteEvent(pEventThread, session, event, payload, pActivityId, pRelatedActivityId, pStack))
            {
                pSessionState->IncrementSequenceNumber();
            }
            else
            {
                allocNewBuffer = true;
            }
        }
    }

    // Check to see if we need to allocate a new buffer, and if so, do it here.
    if (allocNewBuffer)
    {
        // We previously switched to preemptive mode here, however, this is not safe and can cause deadlocks.
        // When a GC is started, and background threads are created (for the first BGC), a thread creation event is fired.
        // When control gets here the buffer is allocated, but then the thread hangs waiting for the GC to complete
        // (it was marked as started before creating threads) so that it can switch back to cooperative mode.
        // However, the GC is waiting on this call to return so that it can make forward progress.  Thus it is not safe
        // to switch to preemptive mode here.

        unsigned int requestSize = sizeof(EventPipeEventInstance) + payload.GetSize();
        BOOL writeSuspended = FALSE;
        pBuffer = AllocateBufferForThread(pSessionState, requestSize, writeSuspended);
        if (pBuffer == NULL)
        {
            // We treat this as the WriteEvent() call occurring after this session stopped listening for events, effectively the
            // same as if event.IsEnabled() test above returned false.
            if (writeSuspended)
                return false;

            // This lock looks unnecessary for the sequence number, but didn't want to
            // do a broader refactoring to take it out. If it shows up as a perf
            // problem then we should.
            SpinLockHolder _slh(pEventPipeThread->GetLock());
            pSessionState->IncrementSequenceNumber();
        }
        else
        {
            pEventPipeThread = EventPipeThread::Get();
            _ASSERTE(pEventPipeThread != NULL);
            {
                SpinLockHolder _slh(pEventPipeThread->GetLock());
                if (m_writeEventSuspending.LoadWithoutBarrier())
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
                    pSessionState->SetWriteBuffer(pBuffer);

                    // Try to write the event after we allocated a buffer.
                    // This is the first time if the thread had no buffers before the call to this function.
                    // This is the second time if this thread did have one or more buffers, but they were full.
                    allocNewBuffer = !pBuffer->WriteEvent(pEventThread, session, event, payload, pActivityId, pRelatedActivityId, pStack);
                    _ASSERTE(!allocNewBuffer);
                    pSessionState->IncrementSequenceNumber();
                }
            }
        }
    }

#ifdef _DEBUG
    if (!allocNewBuffer)
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
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pFile != nullptr);
        PRECONDITION(GetCurrentEvent() == nullptr);
    }
    CONTRACTL_END;

    // The V4 format doesn't require full event sorting as V3 did
    // See the comments in WriteAllBufferToFileV4 for more details
    if (pFile->GetSerializationFormat() >= EventPipeSerializationFormat::NetTraceV4)
    {
        WriteAllBuffersToFileV4(pFile, stopTimeStamp);
    }
    else
    {
        WriteAllBuffersToFileV3(pFile, stopTimeStamp);
    }
}

void EventPipeBufferManager::WriteAllBuffersToFileV3(EventPipeFile *pFile, LARGE_INTEGER stopTimeStamp)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(pFile != nullptr);
        PRECONDITION(GetCurrentEvent() == nullptr);
    }
    CONTRACTL_END;

    // Naively walk the circular buffer, writing the event stream in timestamp order.
    MoveNextEventAnyThread(stopTimeStamp);
    while (GetCurrentEvent() != nullptr)
    {
        pFile->WriteEvent(*GetCurrentEvent(), /*CaptureThreadId=*/0, /*sequenceNumber=*/0, /*IsSorted=*/TRUE);
        MoveNextEventAnyThread(stopTimeStamp);
    }
    pFile->Flush();
}

void EventPipeBufferManager::WriteAllBuffersToFileV4(EventPipeFile *pFile, LARGE_INTEGER stopTimeStamp)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(pFile != nullptr);
        PRECONDITION(GetCurrentEvent() == nullptr);
    }
    CONTRACTL_END;

    //
    // In V3 of the format this code does a full timestamp order sort on the events which made the file easier to consume,
    // but the perf implications for emitting the file are less desirable. Imagine an application with 500 threads emitting
    // 10 events per sec per thread (granted this is a questionable number of threads to use in an app, but that isn't
    // under our control). A nieve sort of 500 ordered lists is going to pull the oldest event from each of 500 lists,
    // compare all the timestamps, then emit the oldest one. This could easily add a thousand CPU cycles per-event. A
    // better implementation could maintain a min-heap so that we scale O(log(N)) instead of O(N)but fundamentally sorting
    // has a cost and we didn't want a file format that forces the runtime to pay it on every event.
    //
    // We minimize sorting using two mechanisms:
    // 1) Explicit sequence points - Every X MB of buffer space that is distributed to threads we record the current
    // timestamp. We ensure when writing events in the file that all events before the sequence point time are written
    // prior to the sequence point and all events with later timestamps are written afterwards. For example assume
    // two threads emitted events like this(B_14 = event on thread B with timestamp 14):
    //
    //                    Time --->
    //   Thread A events: A_1     A_4     A_9 A_10 A_11 A_12 A_13      A_15
    //   Thread B events:     B_2     B_6                         B_14      B_20
    //                                             /|\.
    //                                              |
    //                                            Assume sequence point was triggered here
    // Then we promise that events A_1, A_4, A_9, A_10, B_2_ and B_6 will be written in one or more event blocks,
    // (not necessarily in sorted order) then a sequence point block is written, then events A_11, A_12, A_13, B_14,
    // A_15, and B_20 will be written. The reader can cache all the events between sequence points, sort them, and
    // then emit them in a total order. Triggering sequence points based on buffer allocation ensures that we won't
    // need an arbitrarily large cache in the reader to store all the events, however there is a fair amount of slop
    // in the current scheme. In the worst case you could imagine N threads, each of which was already allocated a
    // max size buffer (currently 1MB) but only an insignificant portion has been used. Even if the trigger
    // threshhold is a modest amount such as 10MB, the threads could first write 1MB * N bytes to the stream
    // beforehand. I'm betting on these extreme cases being very rare and even something like 1GB isn't an unreasonable
    // amount of virtual memory to use on to parse an extreme trace. However if I am wrong we can control
    // both the allocation policy and the triggering instrumentation. Nothing requires us to give out 1MB buffers to
    // 1000 threads simulatneously, nor are we prevented from observing buffer usage at finer granularity than we
    // allocated.
    //
    // 2) We mark which events are the oldest ones in the stream at the time we emit them and we do this at regular
    // intervals of time. When we emit all the events every X ms, there will be at least one event in there with
    // a marker showing that all events older than that one have already been emitted. As soon as the reader sees
    // this it can sort the events which have older timestamps and emit them.
    //
    // Why have both mechanisms? The sequence points in #1 worked fine to guarantee that given the whole trace you
    // could  sort it with a bounded cache, but it doesn't help much for real-time usage. Imagine that we have two
    // threads emitting 1KB/sec of events and sequence points occur every 10MB. The reader would need to wait for
    // 10,000 seconds to accumulate all the events before it could sort and process them. On the other hand if we
    // only had mechanism #2 the reader can generate the sort quickly in real-time, but it is messy to do the buffer
    // management. The reader reads in a bunch of event block buffers and starts emitting events from sub-sections
    // of each of them and needs to know when each buffer can be released. The explicit sequence point makes that
    // very easy - every sequence point all buffers can be released and no further bookkeeping is required.

    EventPipeSequencePoint* pSequencePoint;
    LARGE_INTEGER curTimestampBoundary;
    curTimestampBoundary.QuadPart = stopTimeStamp.QuadPart;
    {
        SpinLockHolder _slh(&m_lock);
        if (TryPeekSequencePoint(&pSequencePoint))
        {
            curTimestampBoundary.QuadPart = Min(curTimestampBoundary.QuadPart, pSequencePoint->TimeStamp.QuadPart);
        }
    }

    while(true) // loop across sequence points
    {
        while (true) // loop across events within a sequence point boundary
        {
            // pick the thread that has the oldest event
            MoveNextEventAnyThread(curTimestampBoundary);
            if (GetCurrentEvent() == nullptr)
            {
                break;
            }
            ULONGLONG captureThreadId = GetCurrentEventBuffer()->GetWriterThread()->GetOSThreadId();
            EventPipeBufferList* pBufferList = GetCurrentEventBufferList();

            // loop across events on this thread
            bool eventsWritten = false;
            unsigned int sequenceNumber = 0;
            while (GetCurrentEvent() != nullptr)
            {
                // The first event emitted on each thread (detected by !eventsWritten) is guaranteed to
                // be the oldest  event cached in our buffers so we mark it. This implements mechanism #2
                // in the big comment above.

                sequenceNumber = GetCurrentSequenceNumber();
                pFile->WriteEvent(*GetCurrentEvent(), captureThreadId, sequenceNumber, !eventsWritten);
                eventsWritten = true;
                MoveNextEventSameThread(curTimestampBoundary);
            }
            pBufferList->SetLastReadSequenceNumber(sequenceNumber);
        }

        // This finishes any current partially filled EventPipeBlock, and flushes it to the stream
        pFile->Flush();

        // there are no more events prior to curTimestampBoundary
        if (curTimestampBoundary.QuadPart == stopTimeStamp.QuadPart)
        {
            // We are done
            break;
        }
        else // (curTimestampBoundary.QuadPart < stopTimeStamp.QuadPart)
        {
            // stopped at sequence point case

            // the sequence point captured a lower bound for sequence number on each thread, but iterating
            // through the events we may have observed that a higher numbered event was recorded. If so we
            // should adjust the sequence numbers upwards to ensure the data in the stream is consistent.
            {
                SpinLockHolder _slh(&m_lock);

                SListElem<EventPipeThreadSessionState*> *pElem = m_pThreadSessionStateList->GetHead();
                while (pElem != NULL)
                {
                    EventPipeThreadSessionState* pSessionState = pElem->GetValue();
                    unsigned int threadSequenceNumber = 0;
                    pSequencePoint->ThreadSequenceNumbers.Lookup(pSessionState, &threadSequenceNumber);
                    unsigned int lastReadSequenceNumber = pSessionState->GetBufferList()->GetLastReadSequenceNumber();
                    // Sequence numbers can overflow so we can't use a direct lastRead > sequenceNumber comparison
                    // If a thread is able to drop more than 0x80000000 events in between sequence points then we will
                    // miscategorize it, but that seems unlikely.
                    unsigned int lastReadDelta = lastReadSequenceNumber - threadSequenceNumber;
                    if (0 < lastReadDelta && lastReadDelta < 0x80000000)
                    {
                        pSequencePoint->ThreadSequenceNumbers.AddOrReplace(ThreadSequenceNumberMap::element_t(pSessionState, lastReadSequenceNumber));
                    }
                    pElem = m_pThreadSessionStateList->GetNext(pElem);
                }
            }

            // emit the sequence point into the file
            pFile->WriteSequencePoint(pSequencePoint);

            // move to the next sequence point if any
            {
                SpinLockHolder _slh(&m_lock);

                // advance to the next sequence point, if any
                DequeueSequencePoint();
                curTimestampBoundary.QuadPart = stopTimeStamp.QuadPart;
                if (TryPeekSequencePoint(&pSequencePoint))
                {
                    curTimestampBoundary.QuadPart = Min(curTimestampBoundary.QuadPart, pSequencePoint->TimeStamp.QuadPart);
                }
            }
        }
    }
}

EventPipeEventInstance* EventPipeBufferManager::GetNextEvent()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(!EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    // PERF: This may be too aggressive? If this method is being called frequently enough to keep pace with the
    // writing threads we could be in a state of high lock contention and lots of churning buffers. Each writer
    // would take several locks, allocate a new buffer, write one event into it, then the reader would take the
    // lock, convert the buffer to read-only and read the single event out of it. Allowing more events to accumulate
    // in the buffers before converting between writable and read-only amortizes a lot of the overhead. One way
    // to achieve that would be picking a stopTimeStamp that was Xms in the past. This would let Xms of events
    // to accumulate in the write buffer before we converted it and forced the writer to allocate another. Other more
    // sophisticated approaches would probably build a low overhead synchronization mechanism to read and write the
    // buffer at the same time.
    LARGE_INTEGER stopTimeStamp;
    QueryPerformanceCounter(&stopTimeStamp);
    MoveNextEventAnyThread(stopTimeStamp);
    return GetCurrentEvent();
}

EventPipeEventInstance* EventPipeBufferManager::GetCurrentEvent()
{
    LIMITED_METHOD_CONTRACT;
    return m_pCurrentEvent;
}

unsigned int EventPipeBufferManager::GetCurrentSequenceNumber()
{
    LIMITED_METHOD_CONTRACT;
    return m_pCurrentBuffer->GetCurrentSequenceNumber();
}

EventPipeBuffer* EventPipeBufferManager::GetCurrentEventBuffer()
{
    LIMITED_METHOD_CONTRACT;
    return m_pCurrentBuffer;
}

EventPipeBufferList* EventPipeBufferManager::GetCurrentEventBufferList()
{
    LIMITED_METHOD_CONTRACT;
    return m_pCurrentBufferList;
}

void EventPipeBufferManager::MoveNextEventAnyThread(LARGE_INTEGER stopTimeStamp)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(!m_lock.OwnedByCurrentThread());
    }
    CONTRACTL_END;

    if (m_pCurrentEvent != nullptr)
    {
        m_pCurrentBuffer->MoveNextReadEvent();
    }
    m_pCurrentEvent = nullptr;
    m_pCurrentBuffer = nullptr;
    m_pCurrentBufferList = nullptr;

    // We need to do this in two steps because we can't hold m_lock and EventPipeThread::m_lock
    // at the same time.

    // Step 1 - while holding m_lock get the oldest buffer from each thread
    CQuickArrayList<EventPipeBuffer *> bufferList;
    CQuickArrayList<EventPipeBufferList *> bufferListList;
    {
        SpinLockHolder _slh(&m_lock);
        SListElem<EventPipeThreadSessionState *> *pElem = m_pThreadSessionStateList->GetHead();
        while (pElem != NULL)
        {
            EventPipeBufferList *pBufferList = pElem->GetValue()->GetBufferList();
            EventPipeBuffer *pBuffer = pBufferList->GetHead();
            if (pBuffer != nullptr &&
                pBuffer->GetCreationTimeStamp().QuadPart < stopTimeStamp.QuadPart)
            {
                bufferListList.Push(pBufferList);
                bufferList.Push(pBuffer);
            }
            pElem = m_pThreadSessionStateList->GetNext(pElem);
        }
    }

    // Step 2 - iterate the cached list to find the one with the oldest event. This may require
    // converting some of the buffers from writable to readable, and that in turn requires
    // taking the associated EventPipeThread::m_lock for thread that was writing to that buffer.
    LARGE_INTEGER curOldestTime = stopTimeStamp;
    for (size_t i = 0; i < bufferList.Size(); i++)
    {
        EventPipeBufferList *pBufferList = bufferListList[i];
        EventPipeBuffer *pHeadBuffer = bufferList[i];
        EventPipeBuffer *pBuffer = AdvanceToNonEmptyBuffer(pBufferList, pHeadBuffer, stopTimeStamp);
        if (pBuffer == nullptr)
        {
            // there weren't any non-empty buffers in that list prior to stopTimeStamp
            continue;
        }
        // Peek the next event out of the buffer.
        EventPipeEventInstance *pNext = pBuffer->GetCurrentReadEvent();
        if (pNext != NULL)
        {
            // If it's the oldest event we've seen, then save it.
            if (pNext->GetTimeStamp()->QuadPart < curOldestTime.QuadPart)
            {
                m_pCurrentEvent = pNext;
                m_pCurrentBuffer = pBuffer;
                m_pCurrentBufferList = pBufferList;
                curOldestTime = *(m_pCurrentEvent->GetTimeStamp());
            }
        }
    }
}

void EventPipeBufferManager::MoveNextEventSameThread(LARGE_INTEGER beforeTimeStamp)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(m_pCurrentEvent != nullptr);
        PRECONDITION(m_pCurrentBuffer != nullptr);
        PRECONDITION(m_pCurrentBufferList != nullptr);
        PRECONDITION(!m_lock.OwnedByCurrentThread());
    }
    CONTRACTL_END;

    //advance past the current event
    m_pCurrentEvent = nullptr;
    m_pCurrentBuffer->MoveNextReadEvent();

    // Find the first buffer in the list, if any, which has an event in it
    m_pCurrentBuffer = AdvanceToNonEmptyBuffer(m_pCurrentBufferList, m_pCurrentBuffer, beforeTimeStamp);
    if (m_pCurrentBuffer == nullptr)
    {
        // no more buffers prior to stopTimeStamp
        _ASSERTE(m_pCurrentEvent == nullptr);
        _ASSERTE(m_pCurrentBuffer == nullptr);
        m_pCurrentBufferList = nullptr;
        return;
    }

    // get the event from that buffer
    EventPipeEventInstance* pNextEvent = m_pCurrentBuffer->GetCurrentReadEvent();
    LARGE_INTEGER nextTimeStamp = *pNextEvent->GetTimeStamp();
    if (nextTimeStamp.QuadPart >= beforeTimeStamp.QuadPart)
    {
        // event exists, but isn't early enough
        m_pCurrentEvent = nullptr;
        m_pCurrentBuffer = nullptr;
        m_pCurrentBufferList = nullptr;
    }
    else
    {
        // event is early enough, set the new cursor
        m_pCurrentEvent = pNextEvent;
        _ASSERTE(m_pCurrentBuffer != nullptr);
        _ASSERTE(m_pCurrentBufferList != nullptr);
    }
}

EventPipeBuffer* EventPipeBufferManager::AdvanceToNonEmptyBuffer(EventPipeBufferList* pBufferList,
    EventPipeBuffer* pBuffer,
    LARGE_INTEGER beforeTimeStamp)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(!m_lock.OwnedByCurrentThread());
        PRECONDITION(pBufferList != nullptr);
        PRECONDITION(pBuffer != nullptr);
        PRECONDITION(pBufferList->GetHead() == pBuffer);
    }
    CONTRACTL_END;

    EventPipeBuffer* pCurrentBuffer = pBuffer;
    while (true)
    {
        if (!TryConvertBufferToReadOnly(pCurrentBuffer))
        {
            // the writer thread hasn't yet stored this buffer into the m_pWriteBuffer
            // field (there is a small time window after allocation in this state).
            // This should be the only buffer remaining in the list and it has no
            // events written into it so we are done iterating.
            return nullptr;
        }
        if (pCurrentBuffer->GetCurrentReadEvent() != nullptr)
        {
            // found a non-empty buffer
            return pCurrentBuffer;
        }
        {
            SpinLockHolder _slh(&m_lock);

            // delete the empty buffer
            EventPipeBuffer *pRemoved = pBufferList->GetAndRemoveHead();
            _ASSERTE(pCurrentBuffer == pRemoved);
            DeAllocateBuffer(pRemoved);

            // get the next buffer
            pCurrentBuffer = pBufferList->GetHead();
            if (pCurrentBuffer == nullptr ||
                pCurrentBuffer->GetCreationTimeStamp().QuadPart >= beforeTimeStamp.QuadPart)
            {
                // no more buffers in the list before this timestamp, we're done
                return nullptr;
            }
        }
    }
}

bool EventPipeBufferManager::TryConvertBufferToReadOnly(EventPipeBuffer* pNewReadBuffer)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(pNewReadBuffer != nullptr);
        PRECONDITION(!m_lock.OwnedByCurrentThread());
    }
    CONTRACTL_END;

    // if already readable, nothing to do
    if (pNewReadBuffer->GetVolatileState() == EventPipeBufferState::READ_ONLY)
    {
        return true;
    }

    // if not yet readable, disable the thread from writing to it which causes
    // it to become readable
    {
        EventPipeThread* pThread = pNewReadBuffer->GetWriterThread();
        SpinLockHolder _slh(pThread->GetLock());
        EventPipeThreadSessionState *const pSessionState = pThread->GetSessionState(m_pSession);
        if (pSessionState->GetWriteBuffer() == pNewReadBuffer)
        {
            pSessionState->SetWriteBuffer(nullptr);
            _ASSERTE(pNewReadBuffer->GetVolatileState() == EventPipeBufferState::READ_ONLY);
            return true;
        }
    }

    // It is possible that EventPipeBufferList::TryGetBuffer(...) returns a writable buffer
    // yet it is not returned as EventPipeThread::GetWriteBuffer(...). This is because
    // EventPipeBufferManager::AllocateBufferForThread() insert the new writable buffer into
    // the EventPipeBufferList first, and then it is added to the writable buffer hash table
    // by EventPipeThread::SetWriteBuffer() next. The two operations are not atomic so it is possible
    // to observe this partial state.
    return pNewReadBuffer->GetVolatileState() == EventPipeBufferState::READ_ONLY;
}

void EventPipeBufferManager::SuspendWriteEvent(uint32_t sessionIndex)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        // All calls to this method must be synchronized by our caller
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    CQuickArrayList<EventPipeThread *> threadList;
    {
        SpinLockHolder _slh(&m_lock);
        _ASSERTE(EnsureConsistency());

        m_writeEventSuspending.Store(TRUE);
        // From this point until m_writeEventSuspending is reset to FALSE it is impossible
        // for new EventPipeThreadSessionStates to be added to the m_pThreadSessionStateList or
        // for new EventBuffers to be added to an existing EventPipeBufferList. The only
        // way AllocateBufferForThread is allowed to add one is by:
        // 1) take m_lock - AllocateBufferForThread can't own it now because this thread owns it,
        //                  but after this thread gives it up lower in this function it could be acquired.
        // 2) observe m_writeEventSuspending = False - that won't happen, acquiring m_lock
        //                  guarantees AllocateBufferForThread will observe all the memory changes this
        //                  thread made prior to releasing m_lock and we've already set it TRUE.
        // This ensures that we iterate over the list of threads below we've got the complete list.
        SListElem<EventPipeThreadSessionState *> *pElem = m_pThreadSessionStateList->GetHead();
        while (pElem != NULL)
        {
            threadList.Push(pElem->GetValue()->GetThread());
            pElem = m_pThreadSessionStateList->GetNext(pElem);
        }
    }

    // Iterate through all the threads, forcing them to finish writes in progress inside EventPipeThread::m_lock,
    // relinquish any buffers stored in EventPipeThread::m_pWriteBuffer and prevent storing new ones.
    for (size_t i = 0; i < threadList.Size(); i++)
    {
        EventPipeThread *pThread = threadList[i];
        {
            SpinLockHolder _slh(pThread->GetLock());
            EventPipeThreadSessionState *const pSessionState = pThread->GetSessionState(m_pSession);
            pSessionState->SetWriteBuffer(nullptr);
            // From this point until m_writeEventSuspending is reset to FALSE it is impossible
            // for this thread to set the write buffer to a non-null value which in turn means
            // it can't write events into any buffer. To do this it would need to both:
            // 1) Acquire the thread lock - it can't right now but it will be able to do so after
            //                              we release the lock below
            // 2) Observe m_writeEventSuspending = false - that won't happen, acquiring the thread
            //                              lock guarantees WriteEvent will observe all the memory
            //                              changes this thread made prior to releasing the thread
            //                              lock and we already set it TRUE.
        }
    }

    // Wait for any straggler WriteEvent threads that may have already allocated a buffer but
    // hadn't yet relinquished it.
    {
        SpinLockHolder _slh(&m_lock);
        SListElem<EventPipeThreadSessionState *> *pElem = m_pThreadSessionStateList->GetHead();
        while (pElem != NULL)
        {
            // Get the list and remove it from the thread.
            EventPipeBufferList *const pBufferList = pElem->GetValue()->GetBufferList();
            if (pBufferList != nullptr)
            {
                EventPipeThread *const pEventPipeThread = pBufferList->GetThread();
                if (pEventPipeThread != nullptr)
                {
                    YIELD_WHILE(pEventPipeThread->GetSessionWriteInProgress() == sessionIndex);
                    // It still guarantees that the thread has returned its buffer, but it also now guarantees that
                    // that the thread has returned from Session::WriteEvent() and has relinquished the session pointer
                    // This yield is guaranteed to eventually finish because threads will eventually exit WriteEvent()
                    // setting the flag back to -1. If the thread could quickly re-enter WriteEvent and set the flag
                    // back to this_session_id we could theoretically get unlucky and never observe the gap, but
                    // setting s_pSessions[this_session_id] = NULL above guaranteed that can't happen indefinately.
                    // Sooner or later the thread is going to see the NULL value and once it does it won't store
                    // this_session_id into the flag again.
                }
            }
            pElem = m_pThreadSessionStateList->GetNext(pElem);
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

    CQuickArrayList<EventPipeThreadSessionState*> threadSessionStatesToRemove;

    {
        // Take the buffer manager manipulation lock
        SpinLockHolder _slh(&m_lock);

        _ASSERTE(EnsureConsistency());
        _ASSERTE(m_writeEventSuspending);

        // This m_writeEventSuspending flag + locks ensures that no thread will touch any of the
        // state we are dismantling here. This includes:
        //   a) EventPipeThread m_sessions[session_id]
        //   b) EventPipeThreadSessionState
        //   c) EventPipeBufferList
        //   d) EventPipeBuffer
        //   e) EventPipeBufferManager.m_pThreadSessionStateList

        SListElem<EventPipeThreadSessionState*> *pElem = m_pThreadSessionStateList->GetHead();
        while (pElem != NULL)
        {
            // Get the list and determine if we can free it.
            EventPipeThreadSessionState *pSessionState = pElem->GetValue();
            EventPipeBufferList *pBufferList = pSessionState->GetBufferList();
            EventPipeThread *pThread = pSessionState->GetThread();
            pSessionState->SetBufferList(nullptr);

            // Iterate over all nodes in the buffer list and deallocate them.
            EventPipeBuffer *pBuffer = pBufferList->GetAndRemoveHead();
            while (pBuffer != NULL)
            {
                DeAllocateBuffer(pBuffer);
                pBuffer = pBufferList->GetAndRemoveHead();
            }

            // Now that all the buffer list elements have been freed, free the list itself.
            delete(pBufferList);
            pBufferList = NULL;

            // Remove the session state from the session state list.
            pElem = m_pThreadSessionStateList->FindAndRemove(pElem);
            _ASSERTE(pElem != NULL);

            SListElem<EventPipeThreadSessionState *> *pCurElem = pElem;
            pElem = m_pThreadSessionStateList->GetNext(pElem);
            delete (pCurElem);

            // And finally queue the removal of the SessionState from the thread
            EX_TRY
            {
                threadSessionStatesToRemove.Push(pSessionState);
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(SwallowAllExceptions);
        }
    }

    // remove and delete the session state
    for (size_t i = 0; i < threadSessionStatesToRemove.Size(); i++)
    {
        EventPipeThreadSessionState* pThreadSessionState = threadSessionStatesToRemove[i];
        // The strong reference from session state -> thread might be the very last reference
        // We need to ensure the thread doesn't die until we can release the lock
        EventPipeThreadHolder pThread = pThreadSessionState->GetThread();
        {
            SpinLockHolder _slh(pThreadSessionState->GetThread()->GetLock());
            pThreadSessionState->GetThread()->DeleteSessionState(pThreadSessionState->GetSession());
        }
    }
}

#ifdef _DEBUG
bool EventPipeBufferManager::EnsureConsistency()
{
    LIMITED_METHOD_CONTRACT;

    SListElem<EventPipeThreadSessionState *> *pElem = m_pThreadSessionStateList->GetHead();
    while (pElem != NULL)
    {
        EventPipeBufferList *pBufferList = pElem->GetValue()->GetBufferList();

        _ASSERTE(pBufferList->EnsureConsistency());

        pElem = m_pThreadSessionStateList->GetNext(pElem);
    }

    return true;
}
#endif // _DEBUG

EventPipeBufferList::EventPipeBufferList(EventPipeBufferManager *pManager, EventPipeThread *pThread)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pManager != nullptr);
    _ASSERTE(pThread != nullptr); // TODO: Is this the correct assumption?

    m_pManager = pManager;
    m_pThread = pThread;
    m_pHeadBuffer = NULL;
    m_pTailBuffer = NULL;
    m_bufferCount = 0;
    m_lastReadSequenceNumber = 0;
}

EventPipeBuffer *EventPipeBufferList::GetHead()
{
    LIMITED_METHOD_CONTRACT;
    return m_pHeadBuffer;
}

EventPipeBuffer *EventPipeBufferList::GetTail()
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
        PRECONDITION(EnsureConsistency());
        // Ensure that the input buffer didn't come from another list that was improperly cleaned up.
        PRECONDITION((pBuffer->GetNext() == NULL) && (pBuffer->GetPrevious() == NULL));
    }
    CONTRACTL_END;

    // First node in the list.
    if (m_pTailBuffer == NULL)
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

EventPipeBuffer *EventPipeBufferList::GetAndRemoveHead()
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
    if (m_pHeadBuffer != NULL)
    {
        // Save the head node.
        pRetBuffer = m_pHeadBuffer;

        // Set the new head node.
        m_pHeadBuffer = m_pHeadBuffer->GetNext();

        // Update the head node's previous pointer.
        if (m_pHeadBuffer != NULL)
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

EventPipeThread *EventPipeBufferList::GetThread()
{
    LIMITED_METHOD_CONTRACT;
    return m_pThread;
}

unsigned int EventPipeBufferList::GetLastReadSequenceNumber()
{
    LIMITED_METHOD_CONTRACT;
    return m_lastReadSequenceNumber;
}

void EventPipeBufferList::SetLastReadSequenceNumber(unsigned int sequenceNumber)
{
    LIMITED_METHOD_CONTRACT;
    m_lastReadSequenceNumber = sequenceNumber;
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
    if (m_pHeadBuffer == NULL)
    {
        _ASSERTE(m_bufferCount == 0);
        return true;
    }

    // If the list is non-NULL, walk the list forward until we get to the end.
    unsigned int nodeCount = (m_pHeadBuffer != NULL) ? 1 : 0;
    EventPipeBuffer *pIter = m_pHeadBuffer;
    while (pIter->GetNext() != NULL)
    {
        pIter = pIter->GetNext();
        nodeCount++;

        // Check for consistency of the buffer itself.
        // NOTE: We can't check the last buffer because the owning thread could
        // be writing to it, which could result in false asserts.
        if (pIter->GetNext() != NULL)
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
    while (pIter->GetPrevious() != NULL)
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

#ifdef DEBUG
bool EventPipeBufferList::IsBufferManagerLockOwnedByCurrentThread()
{
    return m_pManager->IsLockOwnedByCurrentThread();
}
#endif

#endif // FEATURE_PERFTRACING
