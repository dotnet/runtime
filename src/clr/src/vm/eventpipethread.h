// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_THREAD_H__
#define __EVENTPIPE_THREAD_H__

#ifdef FEATURE_PERFTRACING

#include "eventpipe.h"
#include "eventpipebuffer.h"
#include "eventpipesession.h"
#include "spinlock.h"

class EventPipeBuffer;
class EventPipeBufferList;
class EventPipeBufferManager;
class EventPipeThread;

void ReleaseEventPipeThreadRef(EventPipeThread* pThread);
void AcquireEventPipeThreadRef(EventPipeThread* pThread);
typedef Wrapper<EventPipeThread*, AcquireEventPipeThreadRef, ReleaseEventPipeThreadRef> EventPipeThreadHolder;

class EventPipeThreadSessionState
{
    // immutable
    EventPipeThreadHolder m_pThread;

    // immutable
    EventPipeSession* m_pSession;

    // The buffer this thread is allowed to write to if non-null, it must
    // match the tail of m_bufferList
    // protected by m_pThread::GetLock()
    EventPipeBuffer* m_pWriteBuffer;

    // The list of buffers that were written to by this thread. This
    // is populated lazily the first time a thread tries to allocate
    // a buffer for this session. It is set back to null when
    // event writing is suspended during session disable.
    // protected by the buffer manager lock
    EventPipeBufferList* m_pBufferList;

#ifdef DEBUG
    // protected by the buffer manager lock
    EventPipeBufferManager* m_pBufferManager;
#endif

    // The number of events that were attempted to be written by this
    // thread. Each event was either succesfully recorded in a buffer
    // or it was dropped.
    //
    // Only updated by the current thread under m_pThread::GetLock(). Other
    // event writer threads are allowed to do unsychronized reads when
    // capturing a sequence point but this does not provide any consistency
    // guarantee. In particular there is no promise that the other thread
    // is observing the most recent sequence number, nor is there a promise
    // that the observable number of events in the write buffer matches the
    // sequence number. A writer thread will always update the sequence
    // number in tandem with an event write or drop, but without a write
    // barrier between those memory writes they might observed out-of-order
    // by the thread capturing the sequence point. The only utility this
    // unsychronized read has is that if some other thread observes a sequence
    // number X, it knows this thread must have attempted to write at least
    // X events prior to the moment in time when the read occured. If the event
    // buffers are later read and there are fewer than X events timestamped
    // prior to the sequence point we can be certain the others were dropped.
    Volatile<unsigned int> m_sequenceNumber;

public:
    EventPipeThreadSessionState(EventPipeThread* pThread, EventPipeSession* pSession DEBUG_ARG(EventPipeBufferManager* pBufferManager));

    EventPipeThread* GetThread();
    EventPipeSession* GetSession();
    EventPipeBuffer *GetWriteBuffer();
    void SetWriteBuffer(EventPipeBuffer *pNewBuffer);
    EventPipeBufferList *GetBufferList();
    void SetBufferList(EventPipeBufferList *pBufferList);
    unsigned int GetVolatileSequenceNumber();
    unsigned int GetSequenceNumber();
    void IncrementSequenceNumber();
};

#ifndef __GNUC__
#define EVENTPIPE_THREAD_LOCAL __declspec(thread)
#else  // !__GNUC__
#define EVENTPIPE_THREAD_LOCAL thread_local
#endif // !__GNUC__

class EventPipeThread
{
    static EVENTPIPE_THREAD_LOCAL EventPipeThreadHolder gCurrentEventPipeThreadHolder;

    ~EventPipeThread();

    // The EventPipeThreadHolder maintains one count while the thread is alive
    // and each session's EventPipeBufferList maintains one count while it
    // exists
    LONG m_refCount;

    // Per-session state.
    // The pointers in this array are only read/written under m_lock
    // Some of the data within the ThreadSessionState object can be accessed
    // without m_lock however, see the fields of that type for details.
    EventPipeThreadSessionState* m_sessionState[EventPipe::MaxNumberOfSessions];

    // This lock is designed to have low contention. Normally it is only taken by this thread,
    // but occasionally it may also be taken by another thread which is trying to collect and drain
    // buffers from all threads.
    SpinLock m_lock;

    // This is initialized when the Thread object is first constructed and remains
    // immutable afterwards
    SIZE_T m_osThreadId;

    // If this is set to a valid id before the corresponding entry of s_pSessions is set to null,
    // that pointer will be protected from deletion. See EventPipe::DisableInternal() and
    // EventPipe::WriteInternal for more detail.
    Volatile<uint32_t> m_writingEventInProgress;

    //
    EventPipeSession *m_pRundownSession = nullptr;

public:
    static EventPipeThread *Get();
    static EventPipeThread* GetOrCreate();

    EventPipeThread();
    void AddRef();
    void Release();
    SpinLock *GetLock();
#ifdef DEBUG
    bool IsLockOwnedByCurrentThread();
#endif

    EventPipeThreadSessionState* GetOrCreateSessionState(EventPipeSession* pSession);
    EventPipeThreadSessionState* GetSessionState(EventPipeSession* pSession);
    void DeleteSessionState(EventPipeSession* pSession);
    SIZE_T GetOSThreadId();

    bool IsRundownThread() const
    {
        LIMITED_METHOD_CONTRACT;
        return (m_pRundownSession != nullptr);
    }

    void SetAsRundownThread(EventPipeSession *pSession)
    {
        LIMITED_METHOD_CONTRACT;
        m_pRundownSession = pSession;
    }

    EventPipeSession *GetRundownSession() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pRundownSession;
    }

    void SetSessionWriteInProgress(uint32_t sessionIndex)
    {
        LIMITED_METHOD_CONTRACT;
        m_writingEventInProgress.Store(sessionIndex);
    }

    uint32_t GetSessionWriteInProgress() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_writingEventInProgress.Load();
    }
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_THREAD_H__
