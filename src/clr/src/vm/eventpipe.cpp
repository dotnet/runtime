// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "clrtypes.h"
#include "safemath.h"
#include "diagnosticsipc.h"
#include "eventpipe.h"
#include "eventpipebuffermanager.h"
#include "eventpipeconfiguration.h"
#include "eventpipesessionprovider.h"
#include "eventpipeevent.h"
#include "eventpipeeventsource.h"
#include "eventpipefile.h"
#include "eventpipeprovider.h"
#include "eventpipesession.h"
#include "eventpipejsonfile.h"
#include "eventtracebase.h"
#include "sampleprofiler.h"
#include "win32threadpool.h"
#include "ceemain.h"

#ifdef FEATURE_PAL
#include "pal.h"
#endif // FEATURE_PAL

#ifdef FEATURE_PERFTRACING

CrstStatic EventPipe::s_configCrst;
Volatile<bool> EventPipe::s_tracingInitialized = false;
EventPipeConfiguration EventPipe::s_config;
EventPipeEventSource *EventPipe::s_pEventSource = NULL;
HANDLE EventPipe::s_fileSwitchTimerHandle = NULL;
VolatilePtr<EventPipeSession> EventPipe::s_pSessions[MaxNumberOfSessions];
ULONGLONG EventPipe::s_lastFlushTime = 0;

#ifdef FEATURE_PAL
// This function is auto-generated from /src/scripts/genEventPipe.py
extern "C" void InitProvidersAndEvents();
#else
void InitProvidersAndEvents();
#endif

EventPipeEventPayload::EventPipeEventPayload(EventData *pEventData, unsigned int eventDataCount)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pData = NULL;
    m_pEventData = pEventData;
    m_eventDataCount = eventDataCount;
    m_allocatedData = false;

    S_UINT32 tmp_size = S_UINT32(0);
    for (unsigned int i = 0; i < m_eventDataCount; i++)
    {
        tmp_size += S_UINT32(m_pEventData[i].Size);
    }

    if (tmp_size.IsOverflow())
    {
        // If there is an overflow, drop the data and create an empty payload
        m_pEventData = NULL;
        m_eventDataCount = 0;
        m_size = 0;
    }
    else
    {
        m_size = tmp_size.Value();
    }
}

EventPipeEventPayload::~EventPipeEventPayload()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_allocatedData && m_pData != NULL)
    {
        delete[] m_pData;
        m_pData = NULL;
    }
}

void EventPipeEventPayload::Flatten()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_size > 0)
    {
        if (!IsFlattened())
        {
            BYTE *tmp_pData = new (nothrow) BYTE[m_size];
            if (tmp_pData != NULL)
            {
                m_allocatedData = true;
                CopyData(tmp_pData);
                m_pData = tmp_pData;
            }
        }
    }
}

void EventPipeEventPayload::CopyData(BYTE *pDst)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_size > 0)
    {
        if (IsFlattened())
        {
            memcpy(pDst, m_pData, m_size);
        }

        else if (m_pEventData != NULL)
        {
            unsigned int offset = 0;
            for (unsigned int i = 0; i < m_eventDataCount; i++)
            {
                memcpy(pDst + offset, (BYTE *)m_pEventData[i].Ptr, m_pEventData[i].Size);
                offset += m_pEventData[i].Size;
            }
        }
    }
}

BYTE *EventPipeEventPayload::GetFlatData()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!IsFlattened())
    {
        Flatten();
    }
    return m_pData;
}

void EventPipeProviderCallbackDataQueue::Enqueue(EventPipeProviderCallbackData *pEventPipeProviderCallbackData)
{
    SListElem<EventPipeProviderCallbackData> *listnode = new SListElem<EventPipeProviderCallbackData>(); // throws
    listnode->m_Value = *pEventPipeProviderCallbackData;
    list.InsertTail(listnode);
}

bool EventPipeProviderCallbackDataQueue::TryDequeue(EventPipeProviderCallbackData *pEventPipeProviderCallbackData)
{
    if (list.IsEmpty())
        return false;

    SListElem<EventPipeProviderCallbackData> *listnode = list.RemoveHead();
    *pEventPipeProviderCallbackData = listnode->m_Value;
    delete listnode;
    return true;
}

void EventPipe::Initialize()
{
    STANDARD_VM_CONTRACT;

    const bool tracingInitialized = s_configCrst.InitNoThrow(
        CrstEventPipe,
        (CrstFlags)(CRST_REENTRANCY | CRST_TAKEN_DURING_SHUTDOWN | CRST_HOST_BREAKABLE));

    // Initialize the session container to nullptr.
    for (VolatilePtr<EventPipeSession> &session : s_pSessions)
        session.Store(nullptr);

    s_config.Initialize();

    s_pEventSource = new EventPipeEventSource();

    // This calls into auto-generated code to initialize the runtime providers
    // and events so that the EventPipe configuration lock isn't taken at runtime
    InitProvidersAndEvents();

    // Set the sampling rate for the sample profiler.
    const unsigned long DefaultProfilerSamplingRateInNanoseconds = 1000000; // 1 msec.
    SampleProfiler::SetSamplingRate(DefaultProfilerSamplingRateInNanoseconds);

    s_tracingInitialized = tracingInitialized;
}

void EventPipe::Shutdown()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        // These 3 pointers are initialized once on EventPipe::Initialize
        //PRECONDITION(s_pEventSource != nullptr);
    }
    CONTRACTL_END;

    if (s_pEventSource == nullptr)
        return;

    if (g_fProcessDetach)
    {
        // If g_fProcessDetach is true, all threads except this got ripped because someone called ExitProcess().
        // This check is an attempt recognize that case and skip all unsafe cleanup work.

        // Since event reading/writing could happen while that ExitProcess happened, the stream is probably
        // screwed anyway. Therefore we do NOT attempt to flush buffer, do rundown. Cleaning up memory at this
        // point is rather meaningless too since the process is going to terminate soon. Therefore we simply
        // quickly exit here

        // TODO: Consider releasing the resources that could last longer than the process (e.g. the files)
        return;
    }

    // Mark tracing as no longer initialized.
    s_tracingInitialized = false;

    // We are shutting down, so if disabling EventPipe throws, we need to move along anyway.
    EX_TRY
    {
        for (uint32_t i = 0; i < MaxNumberOfSessions; ++i)
        {
            EventPipeSession *pSession = s_pSessions[i].Load();
            if (pSession)
                Disable(static_cast<EventPipeSessionID>(1ULL << i));
        }
    }
    EX_CATCH {}
    EX_END_CATCH(SwallowAllExceptions);

    // Remove EventPipeEventSource first since it tries to use the data structures that we remove below.
    // We need to do this after disabling sessions since those try to write to EventPipeEventSource.
    delete s_pEventSource;
    s_pEventSource = nullptr;

    s_config.Shutdown();
}

EventPipeSessionID EventPipe::Enable(
    LPCWSTR strOutputPath,
    uint32_t circularBufferSizeInMB,
    const EventPipeProviderConfiguration *pProviders,
    uint32_t numProviders,
    EventPipeSessionType sessionType,
    IpcStream *const pStream)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(circularBufferSizeInMB > 0);
        PRECONDITION(numProviders > 0 && pProviders != nullptr);
    }
    CONTRACTL_END;

    if (!s_tracingInitialized)
        return 0;

    // If the state or arguments are invalid, bail here.
    if (sessionType == EventPipeSessionType::File && strOutputPath == nullptr)
        return 0;
    if (sessionType == EventPipeSessionType::IpcStream && pStream == nullptr)
        return 0;

    EventPipeSessionID sessionId = 0;
    RunWithCallbackPostponed([&](EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue) {
        EventPipeSession *const pSession = s_config.CreateSession(
            strOutputPath,
            pStream,
            sessionType,
            circularBufferSizeInMB,
            pProviders,
            numProviders);

        if (pSession == nullptr)
            return;
        sessionId = EnableInternal(pSession, pEventPipeProviderCallbackDataQueue);
        if (sessionId == 0)
            delete pSession;
    });

    return sessionId;
}

static uint64_t GetArrayIndex(EventPipeSessionID mask)
{
    for (uint64_t i = 0; i < 64; ++i)
        if ((1ULL << i) & mask)
            return i;
    return UINT64_MAX;
}

EventPipeSessionID EventPipe::EnableInternal(
    EventPipeSession *const pSession,
    EventPipeProviderCallbackDataQueue* pEventPipeProviderCallbackDataQueue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(s_tracingInitialized);
        PRECONDITION(pSession != nullptr && pSession->IsValid());
        PRECONDITION(IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    // If tracing is not initialized or is already enabled, bail here.
    if (!s_tracingInitialized)
        return 0;

    if (pSession == nullptr || !pSession->IsValid())
        return 0;

    // Return if the index is invalid.
    const uint64_t index = GetArrayIndex(pSession->GetId());
    if (index >= 64)
    {
        _ASSERTE(!"Computed index was out of range.");
        return 0;
    }

    // Register the SampleProfiler the very first time.
    SampleProfiler::Initialize(pEventPipeProviderCallbackDataQueue);

    // Enable the EventPipe EventSource.
    s_pEventSource->Enable(pSession);

    // Save the session.
    if (s_pSessions[index].LoadWithoutBarrier() != nullptr)
    {
        _ASSERTE(!"Attempting to override an existing session.");
        return 0;
    }
    s_pSessions[index].Store(pSession);

    // Enable tracing.
    s_config.Enable(*pSession, pEventPipeProviderCallbackDataQueue);

    // Enable the sample profiler
    SampleProfiler::Enable(pEventPipeProviderCallbackDataQueue);

    // Enable the session.
    pSession->Enable();

    // Return the session ID.
    return pSession->GetId();
}

void EventPipe::Disable(EventPipeSessionID id)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    SetupThread();

    if (id == 0)
        return;

    // Don't block GC during clean-up.
    GCX_PREEMP();

    RunWithCallbackPostponed([&](EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue) {
        DisableInternal(id, pEventPipeProviderCallbackDataQueue);
    });
}

static void LogProcessInformationEvent(EventPipeEventSource &eventSource)
{
    // Get the managed command line.
    LPCWSTR pCmdLine = GetManagedCommandLine();

    // Checkout https://github.com/dotnet/coreclr/pull/24433 for more information about this fall back.
    if (pCmdLine == nullptr)
    {
        // Use the result from GetCommandLineW() instead
        pCmdLine = GetCommandLineW();
    }

    // Log the process information event.
    eventSource.SendProcessInfo(pCmdLine);
}

void EventPipe::DisableInternal(EventPipeSessionID id, EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    if (!s_config.Enabled() || !s_config.IsSessionIdValid(id))
        return;

    // Get the specified session ID.
    const uint64_t index = GetArrayIndex(id);
    if (index >= 64)
    {
        _ASSERTE(!"Computed index was out of range.");
        return;
    }

    // If the session was not found, then there is nothing else to do.
    EventPipeSession *const pSession = s_pSessions[index];
    if (pSession == nullptr)
        return;

    // Disable the profiler.
    SampleProfiler::Disable();

    // Log the process information event.
    LogProcessInformationEvent(*s_pEventSource);

    // Log the runtime information event.
    ETW::InfoLog::RuntimeInformation(ETW::InfoLog::InfoStructs::Normal);

    // Disable pSession tracing.
    s_config.Disable(*pSession, pEventPipeProviderCallbackDataQueue);

    // At this point, we should not be writing events to this session anymore
    // This is a good time to remove the session from the array.
    s_pSessions[index].Store(nullptr);

    pSession->Disable(); // Suspend EventPipeBufferManager, and remove providers.

    // Do rundown before fully stopping the session.
    pSession->EnableRundown(); // Set Rundown provider.

    EventPipeThread *const pEventPipeThread = EventPipeThread::GetOrCreate();
    if (pEventPipeThread != nullptr)
    {
        pEventPipeThread->SetAsRundownThread(pSession);
        {
            s_config.Enable(*pSession, pEventPipeProviderCallbackDataQueue);
            pSession->ExecuteRundown();
            s_config.Disable(*pSession, pEventPipeProviderCallbackDataQueue);
        }
        pEventPipeThread->SetAsRundownThread(nullptr);
    }
    else
    {
        _ASSERTE(!"Failed to get or create the EventPipeThread for rundown events.");
        return;
    }

    // Remove the session.
    s_config.DeleteSession(pSession);

    // Delete deferred providers.
    // Providers can't be deleted during tracing because they may be needed when serializing the file.
    s_config.DeleteDeferredProviders();
}

EventPipeSession *EventPipe::GetSession(EventPipeSessionID id)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(s_tracingInitialized);

    if (!s_tracingInitialized)
        return nullptr;

    {
        CrstHolder _crst(GetLock());

        // Attempt to get the specified session ID.
        const uint64_t index = GetArrayIndex(id);
        if (index >= 64)
        {
            _ASSERTE(!"Computed index was out of range.");
            return nullptr;
        }

        return s_pSessions[index];
    }
}

bool EventPipe::Enabled()
{
    LIMITED_METHOD_CONTRACT;
    return s_tracingInitialized && s_config.Enabled();
}

EventPipeProvider *EventPipe::CreateProvider(const SString &providerName, EventPipeCallback pCallbackFunction, void *pCallbackData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    EventPipeProvider *pProvider = NULL;
    RunWithCallbackPostponed([&](EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue) {
        pProvider = CreateProvider(providerName, pCallbackFunction, pCallbackData, pEventPipeProviderCallbackDataQueue);
    });

    return pProvider;
}

EventPipeProvider *EventPipe::CreateProvider(const SString &providerName, EventPipeCallback pCallbackFunction, void *pCallbackData, EventPipeProviderCallbackDataQueue* pEventPipeProviderCallbackDataQueue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    return s_config.CreateProvider(
        providerName,
        pCallbackFunction,
        pCallbackData,
        pEventPipeProviderCallbackDataQueue);
}

EventPipeProvider *EventPipe::GetProvider(const SString &providerName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return s_config.GetProvider(providerName);
}

void EventPipe::DeleteProvider(EventPipeProvider *pProvider)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Take the lock to make sure that we don't have a race
    // between disabling tracing and deleting a provider
    // where we hold a provider after tracing has been disabled.
    CrstHolder _crst(GetLock());

    if (pProvider != NULL)
    {
        if (Enabled())
        {
            // Save the provider until the end of the tracing session.
            pProvider->SetDeleteDeferred();
        }
        else
        {
            // Delete the provider now.
            s_config.DeleteProvider(pProvider);
        }
    }
}

void EventPipe::WriteEvent(EventPipeEvent &event, BYTE *pData, unsigned int length, LPCGUID pActivityId, LPCGUID pRelatedActivityId)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    EventPipeEventPayload payload(pData, length);
    WriteEventInternal(event, payload, pActivityId, pRelatedActivityId);
}

void EventPipe::WriteEvent(EventPipeEvent &event, EventData *pEventData, unsigned int eventDataCount, LPCGUID pActivityId, LPCGUID pRelatedActivityId)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    EventPipeEventPayload payload(pEventData, eventDataCount);
    WriteEventInternal(event, payload, pActivityId, pRelatedActivityId);
}

void EventPipe::WriteEventInternal(EventPipeEvent &event, EventPipeEventPayload &payload, LPCGUID pActivityId, LPCGUID pRelatedActivityId)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // We can't proceed if tracing is not initialized.
    if (!s_tracingInitialized)
        return;

    // Exit early if the event is not enabled.
    if (!event.IsEnabled())
        return;

    // Get the current thread;
    Thread *const pThread = GetThread();

    // If the activity id isn't specified AND we are in a managed thread, pull it from the current thread.
    // If pThread is NULL (we aren't in writing from a managed thread) then pActivityId can be NULL
    if (pActivityId == nullptr && pThread != nullptr)
        pActivityId = pThread->GetActivityId();

    WriteEventInternal(
        pThread,
        event,
        payload,
        pActivityId,
        pRelatedActivityId);
}

void EventPipe::WriteEventInternal(
    Thread *pThread,
    EventPipeEvent &event,
    EventPipeEventPayload &payload,
    LPCGUID pActivityId,
    LPCGUID pRelatedActivityId,
    Thread *pEventThread,
    StackContents *pStack)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(s_tracingInitialized);
    }
    CONTRACTL_END;

    EventPipeThread *const pEventPipeThread = EventPipeThread::GetOrCreate();
    if (pEventPipeThread == nullptr)
    {
        _ASSERTE(!"Failed to get or create an EventPipeThread.");
        return;
    }

    if (pEventPipeThread->IsRundownThread())
    {
        EventPipeSession *const pRundownSession = pEventPipeThread->GetRundownSession();
        _ASSERTE(pRundownSession != nullptr);
        _ASSERTE(pThread != nullptr);

        BYTE *pData = payload.GetFlatData();
        if (pThread != nullptr && pRundownSession != nullptr && pData != nullptr)
        {
            // Write synchronously to the file.
            // We're under lock and blocking the disabling thread.
            // This copy occurs here (rather than at file write) because
            // A) The FastSerializer API would need to change if we waited
            // B) It is unclear there is a benefit to multiple file write calls
            //    as opposed a a buffer copy here
            EventPipeEventInstance instance(
                event,
                pThread->GetOSThreadId(),
                pData,
                payload.GetSize(),
                pActivityId,
                pRelatedActivityId);
            instance.EnsureStack(*pRundownSession);

            // EventPipeFile::WriteEvent needs to allocate a metadata event
            // and can therefore throw. In this context we will silently
            // fail rather than disrupt the caller
            EX_TRY
            {
                _ASSERTE(pRundownSession != nullptr);
                if (pRundownSession != nullptr)
                    pRundownSession->WriteEvent(instance);
            }
            EX_CATCH {}
            EX_END_CATCH(SwallowAllExceptions);
        }
    }
    else
    {
        for (uint64_t i = 0; i < MaxNumberOfSessions; ++i)
        {
            // This read is OK because we aren't derefencing the pointer and if we observe a value that
            // isn't up-to-date (whether null or non-null) that is just the natural race timing of trying to
            // write to a session while it is being concurrently enabled/disabled.
            if (s_pSessions[i].LoadWithoutBarrier() == nullptr)
                continue;

            // Now that we know this session is probably live we pay the perf cost of the memory barriers
            // Setting this flag lets a thread trying to do a concurrent disable that it is not safe to delete
            // session ID i. The if check above also ensures that once the session is unpublished this thread
            // will eventually stop ever storing ID i into the WriteInProgress flag. This is important to
            // guarantee termination of the YIELD_WHILE loop in SuspendWriteEvents.
            pEventPipeThread->SetSessionWriteInProgress(i);
            {
                EventPipeSession *const pSession = s_pSessions[i].Load();

                // The NULL check above may make this check below appear redundant but it is not. Disable is
                // allowed to set s_pSessions[i] = NULL at any time and that may have occured in between
                // the check and the load
                if (pSession != nullptr)
                    pSession->WriteEvent(
                        pThread,
                        event,
                        payload,
                        pActivityId,
                        pRelatedActivityId,
                        pEventThread,
                        pStack);
            }
            // Do not reference pSession past this point, we are signaling Disable() that it is safe to
            // delete it
            pEventPipeThread->SetSessionWriteInProgress(UINT64_MAX);
        }
    }
}

void EventPipe::WriteSampleProfileEvent(Thread *pSamplingThread, EventPipeEvent *pEvent, Thread *pTargetThread, StackContents &stackContents, BYTE *pData, unsigned int length)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pEvent != nullptr);
    }
    CONTRACTL_END;

    // We can't proceed if tracing is not initialized.
    if (!s_tracingInitialized)
        return;

    EventPipeEventPayload payload(pData, length);
    WriteEventInternal(
        pSamplingThread,
        *pEvent,
        payload,
        nullptr /* pActivityId */,
        nullptr /* pRelatedActivityId */,
        pTargetThread,
        &stackContents
    );
}

bool EventPipe::WalkManagedStackForCurrentThread(StackContents &stackContents)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    Thread *pThread = GetThread();
    return (pThread != NULL) ? WalkManagedStackForThread(pThread, stackContents) : false;
}

bool EventPipe::WalkManagedStackForThread(Thread *pThread, StackContents &stackContents)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pThread != NULL);
    }
    CONTRACTL_END;

    // Calling into StackWalkFrames in preemptive mode violates the host contract,
    // but this contract is not used on CoreCLR.
    CONTRACT_VIOLATION(HostViolation);

    stackContents.Reset();

    StackWalkAction swaRet = pThread->StackWalkFrames(
        (PSTACKWALKFRAMESCALLBACK)&StackWalkCallback,
        &stackContents,
        ALLOW_ASYNC_STACK_WALK | FUNCTIONSONLY | HANDLESKIPPEDFRAMES | ALLOW_INVALID_OBJECTS);

    return ((swaRet == SWA_DONE) || (swaRet == SWA_CONTINUE));
}

StackWalkAction EventPipe::StackWalkCallback(CrawlFrame *pCf, StackContents *pData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pCf != NULL);
        PRECONDITION(pData != NULL);
    }
    CONTRACTL_END;

    // Get the IP.
    UINT_PTR controlPC = (UINT_PTR)pCf->GetRegisterSet()->ControlPC;
    if (controlPC == 0)
    {
        if (pData->GetLength() == 0)
        {
            // This happens for pinvoke stubs on the top of the stack.
            return SWA_CONTINUE;
        }
    }

    _ASSERTE(controlPC != 0);

    // Add the IP to the captured stack.
    pData->Append(
        controlPC,
        pCf->GetFunction());

    // Continue the stack walk.
    return SWA_CONTINUE;
}

EventPipeEventInstance *EventPipe::GetNextEvent(EventPipeSessionID sessionID)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(!IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    // Only fetch the next event if a tracing session exists.
    // The buffer manager is not disposed until the process is shutdown.
    EventPipeSession *const pSession = GetSession(sessionID);
    return pSession ? pSession->GetNextEvent() : nullptr;
}

void EventPipe::InvokeCallback(EventPipeProviderCallbackData eventPipeProviderCallbackData)
{
    EventPipeProvider::InvokeCallback(eventPipeProviderCallbackData);
}

EventPipeEventInstance *EventPipe::BuildEventMetadataEvent(EventPipeEventInstance &instance, unsigned int metadataId)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return s_config.BuildEventMetadataEvent(instance, metadataId);
}

#ifdef DEBUG
bool EventPipe::IsLockOwnedByCurrentThread()
{
    return GetLock()->OwnedByCurrentThread();
}
#endif

#endif // FEATURE_PERFTRACING
