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
#include "eventpipeeventpayload.h"
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
Volatile<EventPipeState> EventPipe::s_state(EventPipeState::NotInitialized);
EventPipeConfiguration EventPipe::s_config;
EventPipeEventSource *EventPipe::s_pEventSource = nullptr;
VolatilePtr<EventPipeSession> EventPipe::s_pSessions[MaxNumberOfSessions];
Volatile<uint64_t> EventPipe::s_allowWrite = 0;
#ifndef FEATURE_PAL
unsigned int * EventPipe::s_pProcGroupOffsets = nullptr;
#endif
Volatile<uint32_t> EventPipe::s_numberOfSessions(0);

// This function is auto-generated from /src/scripts/genEventPipe.py
#ifdef FEATURE_PAL
extern "C"
#endif
void InitProvidersAndEvents();

void EventPipe::Initialize()
{
    STANDARD_VM_CONTRACT;

    if (s_state != EventPipeState::NotInitialized)
    {
        _ASSERTE(!"EventPipe::Initialize was already initialized.");
        return;
    }

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


    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EventPipeProcNumbers) != 0)
    {
#ifndef FEATURE_PAL
        // setup the windows processor group offset table
        WORD numGroups = ::GetActiveProcessorGroupCount();
        s_pProcGroupOffsets = new (nothrow) unsigned int[numGroups];
        if (s_pProcGroupOffsets)
        {
            unsigned int countProcs = 0;
            for (WORD i = 0; i < numGroups; i++)
            {
                s_pProcGroupOffsets[i] = countProcs;
                countProcs += GetActiveProcessorCount(i);
            }
        }
#endif
    }


    {
        CrstHolder _crst(GetLock());
        if (tracingInitialized)
            s_state = EventPipeState::Initialized;
    }
}

void EventPipe::Shutdown()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(s_state != EventPipeState::ShuttingDown);
    }
    CONTRACTL_END;

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

    if (s_state != EventPipeState::Initialized)
        return;

    // We are shutting down, so if disabling EventPipe throws, we need to move along anyway.
    EX_TRY
    {
        {
            CrstHolder _crst(GetLock());
            s_state = EventPipeState::ShuttingDown;
        }

        for (uint32_t i = 0; i < MaxNumberOfSessions; ++i)
        {
            EventPipeSession *pSession = s_pSessions[i].Load();
            if (pSession)
                Disable(reinterpret_cast<EventPipeSessionID>(pSession));
        }

        // dotnet/coreclr: issue 24850: EventPipe shutdown race conditions
        // Deallocating providers/events here might cause AV if a WriteEvent
        // was to occur. Thus, we are not doing this cleanup.

        // // Remove EventPipeEventSource first since it tries to use the data structures that we remove below.
        // // We need to do this after disabling sessions since those try to write to EventPipeEventSource.
        // delete s_pEventSource;
        // s_pEventSource = nullptr;
        // s_config.Shutdown();
    }
    EX_CATCH {}
    EX_END_CATCH(SwallowAllExceptions);
}

EventPipeSessionID EventPipe::Enable(
    LPCWSTR strOutputPath,
    uint32_t circularBufferSizeInMB,
    const EventPipeProviderConfiguration *pProviders,
    uint32_t numProviders,
    EventPipeSessionType sessionType,
    EventPipeSerializationFormat format,
    const bool rundownRequested,
    IpcStream *const pStream)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(format < EventPipeSerializationFormat::Count);
        PRECONDITION(circularBufferSizeInMB > 0);
        PRECONDITION(numProviders > 0 && pProviders != nullptr);
    }
    CONTRACTL_END;

    // If the state or arguments are invalid, bail here.
    if (sessionType == EventPipeSessionType::File && strOutputPath == nullptr)
        return 0;
    if (sessionType == EventPipeSessionType::IpcStream && pStream == nullptr)
        return 0;

    EventPipeSessionID sessionId = 0;
    RunWithCallbackPostponed([&](EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue) {
        if (s_state != EventPipeState::Initialized)
            return;

        const uint32_t SessionIndex = GenerateSessionIndex();
        if (SessionIndex >= EventPipe::MaxNumberOfSessions)
            return;

        EventPipeSession *const pSession = new EventPipeSession(
            SessionIndex,
            strOutputPath,
            pStream,
            sessionType,
            format,
            rundownRequested,
            circularBufferSizeInMB,
            pProviders,
            numProviders);

        const bool fSuccess = EnableInternal(pSession, pEventPipeProviderCallbackDataQueue);
        if (fSuccess)
            sessionId = reinterpret_cast<EventPipeSessionID>(pSession);
        else
            delete pSession;
    });

    return sessionId;
}

bool EventPipe::EnableInternal(
    EventPipeSession *const pSession,
    EventPipeProviderCallbackDataQueue* pEventPipeProviderCallbackDataQueue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(s_state == EventPipeState::Initialized);
        PRECONDITION(pSession != nullptr && pSession->IsValid());
        PRECONDITION(IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    if (pSession == nullptr || !pSession->IsValid())
        return false;

    // Return if the index is invalid.
    if (pSession->GetIndex() >= MaxNumberOfSessions)
    {
        _ASSERTE(!"Session index was out of range.");
        return false;
    }

    if (s_numberOfSessions >= MaxNumberOfSessions)
    {
        _ASSERTE(!"Max number of sessions reached.");
        return false;
    }

    // Register the SampleProfiler the very first time.
    SampleProfiler::Initialize(pEventPipeProviderCallbackDataQueue);

    // Enable the EventPipe EventSource.
    s_pEventSource->Enable(pSession);

    // Save the session.
    if (s_pSessions[pSession->GetIndex()].LoadWithoutBarrier() != nullptr)
    {
        _ASSERTE(!"Attempting to override an existing session.");
        return false;
    }
    s_pSessions[pSession->GetIndex()].Store(pSession);
    s_allowWrite |= pSession->GetMask();
    ++s_numberOfSessions;

    // Enable tracing.
    s_config.Enable(*pSession, pEventPipeProviderCallbackDataQueue);

    // Enable the sample profiler
    SampleProfiler::Enable(pEventPipeProviderCallbackDataQueue);

    // Enable the session.
    pSession->Enable();

    return true;
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
        if (s_numberOfSessions > 0)
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
        PRECONDITION(id != 0);
        PRECONDITION(s_numberOfSessions > 0);
        PRECONDITION(IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    if (!IsSessionIdInCollection(id))
        return;

    // If the session was not found, then there is nothing else to do.
    EventPipeSession *const pSession = reinterpret_cast<EventPipeSession *>(id);

    // Disable the profiler.
    SampleProfiler::Disable();

    // Log the process information event.
    LogProcessInformationEvent(*s_pEventSource);

    // Disable pSession tracing.
    s_config.Disable(*pSession, pEventPipeProviderCallbackDataQueue);

    pSession->Disable(); // WriteAllBuffersToFile, and remove providers.

    // Do rundown before fully stopping the session unless rundown wasn't requested
    if (pSession->RundownRequested())
    {
        pSession->EnableRundown(); // Set Rundown provider.

        EventPipeThread *const pEventPipeThread = EventPipeThread::GetOrCreate();
        if (pEventPipeThread != nullptr)
        {
            pEventPipeThread->SetAsRundownThread(pSession);
            {
                s_config.Enable(*pSession, pEventPipeProviderCallbackDataQueue);
                {
                    pSession->ExecuteRundown();
                }
                s_config.Disable(*pSession, pEventPipeProviderCallbackDataQueue);
            }
            pEventPipeThread->SetAsRundownThread(nullptr);
        }
        else
        {
            _ASSERTE(!"Failed to get or create the EventPipeThread for rundown events.");
        }
    }

    s_allowWrite &= ~(pSession->GetMask()); 
    pSession->SuspendWriteEvent();
    pSession->WriteAllBuffersToFile(); // Flush the buffers to the stream/file

    --s_numberOfSessions;

    // At this point, we should not be writing events to this session anymore
    // This is a good time to remove the session from the array.
    _ASSERTE(s_pSessions[pSession->GetIndex()] == pSession);

    // Remove the session from the array, and mask.
    s_pSessions[pSession->GetIndex()].Store(nullptr);

    // Write a final sequence point to the file now that all events have
    // been emitted.
    pSession->WriteSequencePointUnbuffered();

    delete pSession;

    // Providers can't be deleted during tracing because they may be needed when serializing the file.
    s_config.DeleteDeferredProviders();
}

EventPipeSession *EventPipe::GetSession(EventPipeSessionID id)
{
    LIMITED_METHOD_CONTRACT;

    {
        CrstHolder _crst(GetLock());

        if (s_state == EventPipeState::NotInitialized)
        {
            _ASSERTE(!"EventPipe::GetSession invoked before EventPipe was initialized.");
            return nullptr;
        }

        return IsSessionIdInCollection(id) ?
            reinterpret_cast<EventPipeSession*>(id) : nullptr;
    }
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
    if (s_state == EventPipeState::NotInitialized)
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
    }
    CONTRACTL_END;

    // We can't proceed if tracing is not initialized.
    if (s_state == EventPipeState::NotInitialized)
        return;

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
            // EventPipeFile::WriteEvent needs to allocate a metadata event
            // and can therefore throw. In this context we will silently
            // fail rather than disrupt the caller
            EX_TRY
            {
                _ASSERTE(pRundownSession != nullptr);
                if (pRundownSession != nullptr)
                    pRundownSession->WriteEventBuffered(
                        pThread,
                        event,
                        payload,
                        pActivityId,
                        pRelatedActivityId,
                        pEventThread,
                        pStack);
            }
            EX_CATCH {}
            EX_END_CATCH(SwallowAllExceptions);
        }
    }
    else
    {
        for (uint32_t i = 0; i < MaxNumberOfSessions; ++i)
        {
            if ((s_allowWrite & (1ui64 << i)) == 0)
                continue;

            // Now that we know this session is probably live we pay the perf cost of the memory barriers
            // Setting this flag lets a thread trying to do a concurrent disable that it is not safe to delete
            // session ID i. The if check above also ensures that once the session is unpublished this thread
            // will eventually stop ever storing ID i into the WriteInProgress flag. This is important to
            // guarantee termination of the YIELD_WHILE loop in SuspendWriteEvents.
            pEventPipeThread->SetSessionWriteInProgress(i);
            {
                EventPipeSession *const pSession = s_pSessions[i].Load();

                // Disable is allowed to set s_pSessions[i] = NULL at any time and that may have occured in between
                // the check and the load
                if (pSession != nullptr)
                    pSession->WriteEventBuffered(
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
            pEventPipeThread->SetSessionWriteInProgress(UINT32_MAX);
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

uint32_t EventPipe::GenerateSessionIndex()
{
    LIMITED_METHOD_CONTRACT;
    PRECONDITION(IsLockOwnedByCurrentThread());

    for (uint32_t i = 0; i < MaxNumberOfSessions; ++i)
        if (s_pSessions[i].LoadWithoutBarrier() == nullptr)
            return i;
    return MaxNumberOfSessions;
}

bool EventPipe::IsSessionIdInCollection(EventPipeSessionID id)
{
    LIMITED_METHOD_CONTRACT;
    PRECONDITION(id != 0);
    PRECONDITION(IsLockOwnedByCurrentThread());

    const EventPipeSession *const pSession = reinterpret_cast<EventPipeSession *>(id);
    for (uint32_t i = 0; i < MaxNumberOfSessions; ++i)
    {
        if (s_pSessions[i] == pSession)
        {
            _ASSERTE(i == pSession->GetIndex());
            return true;
        }
    }
    return false;
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
