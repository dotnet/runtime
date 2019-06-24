// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipe.h"
#include "eventpipebuffer.h"
#include "eventpipebuffermanager.h"

#ifdef FEATURE_PERFTRACING

EventPipeThreadSessionState::EventPipeThreadSessionState(EventPipeThread* pThread, EventPipeSession* pSession DEBUG_ARG(EventPipeBufferManager* pBufferManager)) :
    m_pThread(pThread),
    m_pSession(pSession),
    m_pWriteBuffer(nullptr),
    m_pBufferList(nullptr),
#ifdef DEBUG
    m_pBufferManager(pBufferManager),
#endif
    m_sequenceNumber(1)
{
}

EventPipeThread* EventPipeThreadSessionState::GetThread()
{
    LIMITED_METHOD_CONTRACT;
    return m_pThread;
}

EventPipeSession* EventPipeThreadSessionState::GetSession()
{
    LIMITED_METHOD_CONTRACT;
    return m_pSession;
}

EventPipeBuffer *EventPipeThreadSessionState::GetWriteBuffer()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_pThread->IsLockOwnedByCurrentThread());

    _ASSERTE((m_pWriteBuffer == nullptr) || (m_pWriteBuffer->GetVolatileState() == EventPipeBufferState::WRITABLE));
    return m_pWriteBuffer;
}

void EventPipeThreadSessionState::SetWriteBuffer(EventPipeBuffer *pNewBuffer)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_pThread->IsLockOwnedByCurrentThread());
    _ASSERTE((pNewBuffer == nullptr) || pNewBuffer->GetVolatileState() == EventPipeBufferState::WRITABLE);

    _ASSERTE((m_pWriteBuffer == nullptr) || (m_pWriteBuffer->GetVolatileState() == EventPipeBufferState::WRITABLE));
    if (m_pWriteBuffer != nullptr)
        m_pWriteBuffer->ConvertToReadOnly();
    m_pWriteBuffer = pNewBuffer;
}

EventPipeBufferList *EventPipeThreadSessionState::GetBufferList()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_pBufferManager->IsLockOwnedByCurrentThread());
    return m_pBufferList;
}

void EventPipeThreadSessionState::SetBufferList(EventPipeBufferList *pNewBufferList)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_pBufferManager->IsLockOwnedByCurrentThread());
    m_pBufferList = pNewBufferList;
}

unsigned int EventPipeThreadSessionState::GetVolatileSequenceNumber()
{
    LIMITED_METHOD_CONTRACT;
    return m_sequenceNumber.LoadWithoutBarrier();
}

unsigned int EventPipeThreadSessionState::GetSequenceNumber()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_pThread->IsLockOwnedByCurrentThread());
    return m_sequenceNumber.LoadWithoutBarrier();
}

void EventPipeThreadSessionState::IncrementSequenceNumber()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_pThread->IsLockOwnedByCurrentThread());
    m_sequenceNumber++;
}



void ReleaseEventPipeThreadRef(EventPipeThread *pThread)
{
    LIMITED_METHOD_CONTRACT;
    pThread->Release();
}

void AcquireEventPipeThreadRef(EventPipeThread *pThread)
{
    LIMITED_METHOD_CONTRACT;
    pThread->AddRef();
}

#ifndef __GNUC__
__declspec(thread)
#else // !__GNUC__
thread_local
#endif // !__GNUC__
EventPipeThreadHolder EventPipeThread::gCurrentEventPipeThreadHolder;

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

#ifdef FEATURE_PAL
    m_osThreadId = ::PAL_GetCurrentOSThreadId();
#else
    m_osThreadId = ::GetCurrentThreadId();
#endif
    memset(m_sessionState, 0, sizeof(EventPipeThreadSessionState*) * EventPipe::MaxNumberOfSessions);
}

EventPipeThread::~EventPipeThread()
{
    LIMITED_METHOD_CONTRACT;
#ifdef DEBUG
    for (uint32_t i = 0; i < EventPipe::MaxNumberOfSessions; i++)
    {
        _ASSERTE(m_sessionState[i] == NULL);
    }
#endif
}

/*static */ EventPipeThread *EventPipeThread::Get()
{
    LIMITED_METHOD_CONTRACT;
    return gCurrentEventPipeThreadHolder;
}

/*static */ EventPipeThread* EventPipeThread::GetOrCreate()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (gCurrentEventPipeThreadHolder == nullptr)
    {
        EX_TRY
        {
            gCurrentEventPipeThreadHolder = new EventPipeThread();
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(SwallowAllExceptions);
    }
    return gCurrentEventPipeThreadHolder;
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
        // https://isocpp.org/wiki/faq/freestore-mgmt#delete-this
        // As long as you're careful, it's okay (not evil) for an object to commit suicide (delete this).
        delete this;
    }
}

EventPipeThreadSessionState *EventPipeThread::GetOrCreateSessionState(EventPipeSession *pSession)
{
    LIMITED_METHOD_CONTRACT;
    PRECONDITION(pSession != nullptr);
    PRECONDITION(pSession->GetIndex() < EventPipe::MaxNumberOfSessions);
    PRECONDITION(IsLockOwnedByCurrentThread());

    EventPipeThreadSessionState *pState = m_sessionState[pSession->GetIndex()];
    if (pState == nullptr)
    {
        pState = new (nothrow) EventPipeThreadSessionState(this, pSession DEBUG_ARG(pSession->GetBufferManager()));
        m_sessionState[pSession->GetIndex()] = pState;
    }
    return pState;
}

EventPipeThreadSessionState *EventPipeThread::GetSessionState(EventPipeSession *pSession)
{
    LIMITED_METHOD_CONTRACT;
    PRECONDITION(pSession != nullptr);
    PRECONDITION(pSession->GetIndex() < EventPipe::MaxNumberOfSessions);
    PRECONDITION(IsLockOwnedByCurrentThread());

    EventPipeThreadSessionState *const pState = m_sessionState[pSession->GetIndex()];
    _ASSERTE(pState != nullptr);
    return pState;
}

void EventPipeThread::DeleteSessionState(EventPipeSession* pSession)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pSession != nullptr);
    _ASSERTE(IsLockOwnedByCurrentThread());

    unsigned int index = pSession->GetIndex();
    _ASSERTE(index < EventPipe::MaxNumberOfSessions);
    EventPipeThreadSessionState* pState = m_sessionState[index];

    _ASSERTE(pState != nullptr);
    delete pState;
    m_sessionState[index] = nullptr;
}

SpinLock* EventPipeThread::GetLock()
{
    LIMITED_METHOD_CONTRACT;
    return &m_lock;
}

#ifdef DEBUG
bool EventPipeThread::IsLockOwnedByCurrentThread()
{
    LIMITED_METHOD_CONTRACT;
    return m_lock.OwnedByCurrentThread();
}
#endif

SIZE_T EventPipeThread::GetOSThreadId()
{
    LIMITED_METHOD_CONTRACT;
    return m_osThreadId;
}

#endif // FEATURE_PERFTRACING
