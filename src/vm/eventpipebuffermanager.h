// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_BUFFERMANAGER_H__
#define __EVENTPIPE_BUFFERMANAGER_H__

#ifdef FEATURE_PERFTRACING

#include "eventpipe.h"
#include "eventpipeeventinstance.h"
#include "eventpipethread.h"
#include "spinlock.h"

class EventPipeBuffer;
class EventPipeBufferList;
class EventPipeBufferManager;
class EventPipeFile;
class EventPipeSession;
class EventPipeThread;
struct EventPipeSequencePoint;

class EventPipeBufferManager
{

    // Declare friends.
    friend class EventPipeBufferList;

private:
    // The session this buffer manager belongs to
    EventPipeSession* m_pSession;

    // A list of per-thread session state
    // Each entry in this list represents the session state owned by a single thread
    // which includes the list of buffers the thread has written and its current
    // event sequence number. The EventPipeThread object also has a pointer to the
    // session state contained in this list.  This ensures that each thread can access
    // its own data, while at the same time, ensuring that when a thread is destroyed,
    // we keep the buffers around without having to perform any migration or
    // book-keeping.
    SList<SListElem<EventPipeThreadSessionState*>> *m_pThreadSessionStateList;

    // The total allocation size of buffers under management.
    size_t m_sizeOfAllBuffers;

    // The maximum allowable size of buffers under management.
    // Attempted allocations above this threshold result in
    // dropped events.
    size_t m_maxSizeOfAllBuffers;

    // The amount of allocations we can do at this moment before
    // triggering a sequence point
    size_t m_remainingSequencePointAllocationBudget;

    // The total amount of allocations we can do after one sequence
    // point before triggering the next one
    size_t m_sequencePointAllocationBudget;

    // A queue of sequence points.
    SList<EventPipeSequencePoint> m_sequencePoints;

    // Lock to protect access to the per-thread buffer list and total allocation size.
    SpinLock m_lock;
    Volatile<BOOL> m_writeEventSuspending;

    // Iterator state for reader thread
    // These are not protected by m_lock and expected to only be used on the reader thread
    EventPipeEventInstance* m_pCurrentEvent;
    EventPipeBuffer* m_pCurrentBuffer;
    EventPipeBufferList* m_pCurrentBufferList;

#ifdef _DEBUG
    // For debugging purposes.
    unsigned int m_numBuffersAllocated;
    unsigned int m_numBuffersStolen;
    unsigned int m_numBuffersLeaked;
    Volatile<LONG> m_numEventsStored;
    Volatile<LONG> m_numEventsDropped;
    unsigned long m_numEventsWritten;
#endif // _DEBUG

    // Allocate a new buffer for the specified thread.
    // This function will store the buffer in the thread's buffer list for future use and also return it here.
    // A NULL return value means that a buffer could not be allocated.
    EventPipeBuffer* AllocateBufferForThread(EventPipeThreadSessionState* pSessionState, unsigned int requestSize, BOOL & writeSuspended);

    // Add a buffer to the thread buffer list.
    void AddBufferToThreadBufferList(EventPipeBufferList *pThreadBuffers, EventPipeBuffer *pBuffer);

    // Enqueue a sequence point into the queue.
    void EnqueueSequencePoint(EventPipeSequencePoint* pEnqueuedSequencePoint);

    // Dequeue a sequence point from the queue. This is a no-op if the queue is empty.
    void DequeueSequencePoint();

    // Peek the first sequence point in the queue. Returns FALSE if the queue is empty.
    bool TryPeekSequencePoint(EventPipeSequencePoint** ppSequencePoint);

    // Inits a sequence point that has the list of current threads and sequence
    // numbers (Requires m_lock is already held)
    void InitSequencePointThreadListHaveLock(EventPipeSequencePoint* pSequencePoint);

    // De-allocates the input buffer.
    void DeAllocateBuffer(EventPipeBuffer *pBuffer);

    // Detaches this buffer from an active writer thread and marks it read-only so that the reader
    // thread can use it. If the writer thread has not yet stored the buffer into its thread-local
    // slot it will not be converted, but such buffers have no events in them so there is no reason
    // to read them.
    bool TryConvertBufferToReadOnly(EventPipeBuffer* pNewReadBuffer);

    // Finds the first buffer in EventPipeBufferList that has a readable event prior to beforeTimeStamp,
    // starting with pBuffer
    EventPipeBuffer* AdvanceToNonEmptyBuffer(EventPipeBufferList* pBufferList,
                                             EventPipeBuffer* pBuffer,
                                             LARGE_INTEGER beforeTimeStamp);

    //  -------------- Reader Iteration API ----------------
    // An iterator that can enumerate all the events which have been written into this buffer manager.
    // Initially the iterator starts uninitialized and GetCurrentEvent() returns NULL. Calling MoveNextXXX()
    // attempts to advance the cursor to the next event. If there is no event prior to stopTimeStamp then
    // the GetCurrentEvent() again returns NULL, otherwise it returns that event. The event pointer returned
    // by GetCurrentEvent() is valid until MoveNextXXX() is called again. Once all events in a buffer have
    // been read the iterator will delete that buffer from the pool.

    // Moves to the next oldest event searching across all threads. If there is no event older than
    // stopTimeStamp then GetCurrentEvent() will return NULL.
    void MoveNextEventAnyThread(LARGE_INTEGER stopTimeStamp);

    // Moves to the next oldest event from the same thread as the current event. If there is no event
    // older than stopTimeStamp then GetCurrentEvent() will return NULL. This should only be called
    // when GetCurrentEvent() is non-null (because we need to know what thread's events to iterate)
    void MoveNextEventSameThread(LARGE_INTEGER stopTimeStamp);

    // Returns the current event the iteration cursor is on, or NULL if the iteration is unitialized/
    // the last call to MoveNextXXX() didn't find any suitable event.
    EventPipeEventInstance* GetCurrentEvent();

    // Gets the sequence number corresponding to event from GetCurrentEvent()
    unsigned int GetCurrentSequenceNumber();

    // Gets the buffer corresponding to event from GetCurrentEvent()
    EventPipeBuffer* GetCurrentEventBuffer();

    // Gets the buffer list corresponding to event from GetCurrentEvent()
    EventPipeBufferList* GetCurrentEventBufferList();

public:

    EventPipeBufferManager(EventPipeSession* pEventSession, size_t maxSizeOfAllBuffers, size_t sequencePointAllocationBudget);
    ~EventPipeBufferManager();

    // Write an event to the input thread's current event buffer.
    // An optional eventThread can be provided for sample profiler events.
    // This is because the thread that writes the events is not the same as the "event thread".
    // An optional stack trace can be provided for sample profiler events.
    // Otherwise, if a stack trace is needed, one will be automatically collected.
    bool WriteEvent(Thread *pThread, EventPipeSession &session, EventPipeEvent &event, EventPipeEventPayload &payload, LPCGUID pActivityId, LPCGUID pRelatedActivityId, Thread *pEventThread = NULL, StackContents *pStack = NULL);

    // Inits a sequence point that has the list of current threads and sequence
    // numbers
    void InitSequencePointThreadList(EventPipeSequencePoint* pSequencePoint);

    // READ_ONLY state and no new EventPipeBuffers or EventPipeBufferLists can be created. Calls to
    // WriteEvent that start during the suspension period or were in progress but hadn't yet recorded
    // their event into a buffer before the start of the suspension period will return false and the
    // event will not be recorded. Any events that not recorded as a result of this suspension will be
    // treated the same as events that were not recorded due to configuration.
    // EXPECTED USAGE: First the caller will disable all events via configuration, then call
    // SuspendWriteEvent() to force any WriteEvent calls that may still be in progress to either
    // finish or cancel. After that all BufferLists and Buffers can be safely drained and/or deleted.
    void SuspendWriteEvent(uint32_t sessionIndex);

    // Write the contents of the managed buffers to the specified file.
    // The stopTimeStamp is used to determine when tracing was stopped to ensure that we
    // skip any events that might be partially written due to races when tracing is stopped.
    void WriteAllBuffersToFile(EventPipeFile *pFile, LARGE_INTEGER stopTimeStamp);
    void WriteAllBuffersToFileV3(EventPipeFile *pFastSerializableObject, LARGE_INTEGER stopTimeStamp);
    void WriteAllBuffersToFileV4(EventPipeFile *pFastSerializableObject, LARGE_INTEGER stopTimeStamp);

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

    // The sequence number of the last event that was read, only
    // updated/read by the reader thread.
    unsigned int m_lastReadSequenceNumber;

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

    // Get the thread associated with this list.
    EventPipeThread* GetThread();

    // Read/Write the last read sequence number
    unsigned int GetLastReadSequenceNumber();
    void SetLastReadSequenceNumber(unsigned int sequenceNumber);

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
