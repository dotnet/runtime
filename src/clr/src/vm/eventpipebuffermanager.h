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

#ifdef _DEBUG
    // For debugging purposes.
    unsigned int m_numBuffersAllocated;
    unsigned int m_numBuffersStolen;
    unsigned int m_numBuffersLeaked;
    Volatile<LONG> m_numEventsStored;
    Volatile<LONG> m_numEventsDropped;
    LONG m_numEventsWritten;
#endif // _DEBUG

    // Allocate a new buffer for the specified thread.
    // This function will store the buffer in the thread's buffer list for future use and also return it here.
    // A NULL return value means that a buffer could not be allocated.
    EventPipeBuffer* AllocateBufferForThread(EventPipeSession &session, Thread *pThread, unsigned int requestSize);

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
#endif // _DEBUG
};

// Represents a list of buffers associated with a specific thread.
class EventPipeBufferList
{
private:

    // The buffer manager that owns this list.
    EventPipeBufferManager *m_pManager;

    // Buffers are stored in an intrusive linked-list from oldest to newest.
    // Head is the oldest buffer.  Tail is the newest (and currently used) buffer.
    EventPipeBuffer *m_pHeadBuffer;
    EventPipeBuffer *m_pTailBuffer;

    // The number of buffers in the list.
    unsigned int m_bufferCount;

    // The current read buffer (used when processing events on tracing stop).
    EventPipeBuffer *m_pReadBuffer;

    // True if this thread is owned by a thread.
    // If it is false, then this buffer can be de-allocated after it is drained.
    Volatile<bool> m_ownedByThread;

#ifdef _DEBUG
    // For diagnostics, keep the thread pointer.
    Thread *m_pCreatingThread;
#endif // _DEBUG

public:

    EventPipeBufferList(EventPipeBufferManager *pManager);

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

    // Get the next event as long as it is before the specified timestamp.
    EventPipeEventInstance* PeekNextEvent(LARGE_INTEGER beforeTimeStamp, EventPipeBuffer **pContainingBuffer);

    // Get the next event as long as it is before the specified timestamp, and also mark it as read.
    EventPipeEventInstance* PopNextEvent(LARGE_INTEGER beforeTimeStamp);

    // True if a thread owns this list.
    bool OwnedByThread();

    // Set whether or not this list is owned by a thread.
    // If it is not owned by a thread, then it can be de-allocated
    // after the buffer is drained.
    // The default value is true.
    void SetOwnedByThread(bool value);

#ifdef _DEBUG
    // Get the thread associated with this list.
    Thread* GetThread();

    // Validate the consistency of the list.
    // This function will assert if the list is in an inconsistent state.
    bool EnsureConsistency();
#endif // _DEBUG
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_BUFFERMANAGER_H__
