// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_BUFFERMANAGER_H__
#define __EVENTPIPE_BUFFERMANAGER_H__

#ifdef FEATURE_PERFTRACING

#include "eventpipe.h"
#include "eventpipefile.h"
#include "eventpipebuffer.h"
#include "eventpipesession.h"
#include "spinlock.h"

class EventPipeBufferList;
class EventPipeBufferManager;
class EventPipeThread;

void ReleaseEventPipeThreadRef(EventPipeThread* pThread);
void AcquireEventPipeThreadRef(EventPipeThread* pThread);
typedef Wrapper<EventPipeThread*, AcquireEventPipeThreadRef, ReleaseEventPipeThreadRef> EventPipeThreadHolder;

typedef MapSHashWithRemove<EventPipeBufferManager *, EventPipeBuffer *> EventPipeWriteBuffers;
typedef MapSHashWithRemove<EventPipeBufferManager *, EventPipeBufferList *> EventPipeBufferLists;

class EventPipeThread
{
#ifndef __GNUC__
    __declspec(thread) static
#else  // !__GNUC__
    thread_local static
#endif // !__GNUC__
        EventPipeThreadHolder gCurrentEventPipeThreadHolder;

    ~EventPipeThread();

    // The EventPipeThreadHolder maintains one count while the thread is alive
    // and each session's EventPipeBufferList maintains one count while it
    // exists
    LONG m_refCount;

    // this is a dictionary of { buffer-manager, buffer } this thread is
    // allowed to write to if exists or non-null, it must match the tail of the
    // m_bufferList
    // this pointer is protected by m_lock
    EventPipeWriteBuffers *m_pWriteBuffers = nullptr;

    // this is a dictionary of { buffer-manager, list of buffers } that were
    // written to by this thread
    // it is protected by EventPipeBufferManager::m_lock
    EventPipeBufferLists *m_pBufferLists = nullptr;

    // This lock is designed to have low contention. Normally it is only taken by this thread,
    // but occasionally it may also be taken by another thread which is trying to collect and drain
    // buffers from all threads.
    SpinLock m_lock;

#ifdef DEBUG
    template <typename T>
    static bool AllValuesAreNull(T &map)
    {
        LIMITED_METHOD_CONTRACT;
        for (typename T::Iterator iter = map.Begin(); iter != map.End(); ++iter)
            if (iter->Value() != nullptr)
                return false;
        return true;
    }
#endif // DEBUG

public:
    static EventPipeThread *Get();
    static void Set(EventPipeThread *pThread);

    EventPipeThread();
    void AddRef();
    void Release();
    SpinLock *GetLock();

    EventPipeBuffer *GetWriteBuffer(EventPipeBufferManager *pBufferManager);
    void SetWriteBuffer(EventPipeBufferManager *pBufferManager, EventPipeBuffer *pNewBuffer);
    EventPipeBufferList *GetBufferList(EventPipeBufferManager *pBufferManager);
    void SetBufferList(EventPipeBufferManager *pBufferManager, EventPipeBufferList *pBufferList);
    void Remove(EventPipeBufferManager *pBufferManager);
};

class EventPipeBufferManager
{

    // Declare friends.
    friend class EventPipeBufferList;

private:

    // A list of linked-lists of buffer objects.
    // Each entry in this list represents a set of buffers owned by a single thread.
    // The actual Thread object has a pointer to the object contained in this list.  This ensures that
    // each thread can access its own list, while at the same time, ensuring that when
    // a thread is destroyed, we keep the buffers around without having to perform any
    // migration or book-keeping.
    SList<SListElem<EventPipeBufferList*>> *m_pPerThreadBufferList;

    // The total allocation size of buffers under management.
    size_t m_sizeOfAllBuffers;

    // Lock to protect access to the per-thread buffer list and total allocation size.
    SpinLock m_lock;
    Volatile<BOOL> m_writeEventSuspending;

#ifdef _DEBUG
    // For debugging purposes.
    unsigned int m_numBuffersAllocated;
    unsigned int m_numBuffersStolen;
    unsigned int m_numBuffersLeaked;
    Volatile<LONG> m_numEventsStored;
    Volatile<LONG> m_numEventsDropped;
#endif // _DEBUG

    unsigned long m_numEventsWritten;
    // Allocate a new buffer for the specified thread.
    // This function will store the buffer in the thread's buffer list for future use and also return it here.
    // A NULL return value means that a buffer could not be allocated.
    EventPipeBuffer* AllocateBufferForThread(EventPipeSession &session, unsigned int requestSize, BOOL & writeSuspended);

    // Add a buffer to the thread buffer list.
    void AddBufferToThreadBufferList(EventPipeBufferList *pThreadBuffers, EventPipeBuffer *pBuffer);

    // Find the thread that owns the oldest buffer that is eligible to be stolen.
    EventPipeBufferList* FindThreadToStealFrom();

    // De-allocates the input buffer.
    void DeAllocateBuffer(EventPipeBuffer *pBuffer);

public:

    EventPipeBufferManager();
    ~EventPipeBufferManager();

    // Write an event to the input thread's current event buffer.
    // An optional eventThread can be provided for sample profiler events.
    // This is because the thread that writes the events is not the same as the "event thread".
    // An optional stack trace can be provided for sample profiler events.
    // Otherwise, if a stack trace is needed, one will be automatically collected.
    bool WriteEvent(Thread *pThread, EventPipeSession &session, EventPipeEvent &event, EventPipeEventPayload &payload, LPCGUID pActivityId, LPCGUID pRelatedActivityId, Thread *pEventThread = NULL, StackContents *pStack = NULL);

    // Ends the suspension period created by SuspendWriteEvent(). After this call returns WriteEvent()
    // can again be called succesfully, new BufferLists and Buffers may be allocated.
    // The caller is required to synchronize all calls to SuspendWriteEvent() and ResumeWriteEvent()
    void ResumeWriteEvent();

    // From the time this function returns until ResumeWriteEvent() is called a suspended state will
    // be in effect that blocks all WriteEvent activity. All existing buffers will be in the
    // READ_ONLY state and no new EventPipeBuffers or EventPipeBufferLists can be created. Calls to
    // WriteEvent that start during the suspension period or were in progress but hadn't yet recorded
    // their event into a buffer before the start of the suspension period will return false and the
    // event will not be recorded. Any events that not recorded as a result of this suspension will be
    // treated the same as events that were not recorded due to configuration.
    // EXPECTED USAGE: First the caller will disable all events via configuration, then call
    // SuspendWriteEvent() to force any WriteEvent calls that may still be in progress to either
    // finish or cancel. After that all BufferLists and Buffers can be safely drained and/or deleted.
    // The caller is required to synchronize all calls to SuspendWriteEvent() and ResumeWriteEvent()
    void SuspendWriteEvent();

    // Write the contents of the managed buffers to the specified file.
    // The stopTimeStamp is used to determine when tracing was stopped to ensure that we
    // skip any events that might be partially written due to races when tracing is stopped.
    void WriteAllBuffersToFile(EventPipeFile *pFile, LARGE_INTEGER stopTimeStamp);

    // Attempt to de-allocate resources as best we can.  It is possible for some buffers to leak because
    // threads can be in the middle of a write operation and get blocked, and we may not get an opportunity
    // to free their buffer for a very long time.
    void DeAllocateBuffers();

    // Get next event.  This is used to dispatch events to EventListener.
    EventPipeEventInstance* GetNextEvent();

#ifdef _DEBUG
    bool EnsureConsistency();
    bool IsLockOwnedByCurrentThread();
#endif // _DEBUG
};

// Represents a list of buffers associated with a specific thread.
class EventPipeBufferList
{
private:

    // The buffer manager that owns this list.
    EventPipeBufferManager *m_pManager;

    // The thread which writes to the buffers in this list
    EventPipeThreadHolder m_pThread;

    // Buffers are stored in an intrusive linked-list from oldest to newest.
    // Head is the oldest buffer.  Tail is the newest (and currently used) buffer.
    EventPipeBuffer *m_pHeadBuffer;
    EventPipeBuffer *m_pTailBuffer;

    // The number of buffers in the list.
    unsigned int m_bufferCount;

public:

    EventPipeBufferList(EventPipeBufferManager *pManager, EventPipeThread* pThread);

    // Get the head node of the list.
    EventPipeBuffer* GetHead();

    // Get the tail node of the list.
    EventPipeBuffer* GetTail();

    // Insert a new buffer at the tail of the list.
    void InsertTail(EventPipeBuffer *pBuffer);

    // Remove the head node of the list.
    EventPipeBuffer* GetAndRemoveHead();

    // Get the count of buffers in the list.
    unsigned int GetCount() const;

    // Pop the event from the buffer, and potentially clean-up the previous buffer
    // pNext is expected to be the last event returned from TryGetBuffer()->PeekNext()
    void PopNextEvent(EventPipeBuffer *pContainingBuffer, EventPipeEventInstance *pNext);

    // Get the thread associated with this list.
    EventPipeThread* GetThread();

    // Get the first buffer that might contain the oldest event
    EventPipeBuffer* TryGetBuffer(LARGE_INTEGER beforeTimeStamp);

    // Convert the buffer into read only
    void ConvertBufferToReadOnly(EventPipeBuffer *pNewReadBuffer);

#ifdef _DEBUG
    // Validate the consistency of the list.
    // This function will assert if the list is in an inconsistent state.
    bool EnsureConsistency();
#endif // _DEBUG

#ifdef DEBUG
    bool IsBufferManagerLockOwnedByCurrentThread();
#endif // DEBUG
};


#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_BUFFERMANAGER_H__
