// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#include "ipcstreamfactory.h"
#include "sampleprofiler.h"
#include "win32threadpool.h"
#include "ceemain.h"
#include "configuration.h"

#ifdef TARGET_UNIX
#include "pal.h"
#endif // TARGET_UNIX

#ifdef FEATURE_PERFTRACING

CrstStatic EventPipe::s_configCrst;
Volatile<EventPipeState> EventPipe::s_state(EventPipeState::NotInitialized);
EventPipeConfiguration EventPipe::s_config;
EventPipeEventSource *EventPipe::s_pEventSource = nullptr;
VolatilePtr<EventPipeSession> EventPipe::s_pSessions[MaxNumberOfSessions];
Volatile<uint64_t> EventPipe::s_allowWrite = 0;
#ifndef TARGET_UNIX
unsigned int * EventPipe::s_pProcGroupOffsets = nullptr;
#endif
Volatile<uint32_t> EventPipe::s_numberOfSessions(0);
CQuickArrayList<EventPipeSessionID> EventPipe::s_rgDeferredEnableEventPipeSessionIds = CQuickArrayList<EventPipeSessionID>();
CQuickArrayList<EventPipeSessionID> EventPipe::s_rgDeferredDisableEventPipeSessionIds = CQuickArrayList<EventPipeSessionID>();
bool EventPipe::s_CanStartThreads = false;

// This function is auto-generated from /src/scripts/genEventPipe.py
#ifdef TARGET_UNIX
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

    EventPipeThread::Initialize();

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
#ifndef TARGET_UNIX
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
    EnableViaEnvironmentVariables();
}

// Finish setting up the rest of EventPipe.
void EventPipe::FinishInitialize()
{
    STANDARD_VM_CONTRACT;

    // Enable streaming for any deferred sessions
    {
        CrstHolder _crst(GetLock());

        s_CanStartThreads = true;

        while (s_rgDeferredEnableEventPipeSessionIds.Size() > 0)
        {
            EventPipeSessionID id = s_rgDeferredEnableEventPipeSessionIds.Pop();
            if (IsSessionIdInCollection(id))
            {
                EventPipeSession *pSession = reinterpret_cast<EventPipeSession*>(id);
                pSession->StartStreaming();
            }
        }

        SampleProfiler::CanStartSampling();
    }

    // release lock in case someone tried to disable while we held it
    // s_rgDeferredDisableEventPipeSessionIds is now safe to access without the
    // lock since we've set s_canStartThreads to true inside the lock.  Anyone
    // who was waiting on that lock will see that state and not mutate the defer list
    while (s_rgDeferredDisableEventPipeSessionIds.Size() > 0)
    {
        EventPipeSessionID id = s_rgDeferredDisableEventPipeSessionIds.Pop();
        DisableHelper(id);
    }
}

//
// If EventPipe environment variables are specified, parse them and start a session
//
void EventPipe::EnableViaEnvironmentVariables()
{
    STANDARD_VM_CONTRACT;
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EnableEventPipe) != 0)
    {
        CLRConfigStringHolder eventpipeConfig(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EventPipeConfig));
        CLRConfigStringHolder configOutputPath(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EventPipeOutputPath));
        uint32_t eventpipeCircularBufferMB = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EventPipeCircularMB);
        LPCWSTR outputPath = nullptr;

        if (configOutputPath == NULL)
        {
            outputPath = W("trace.nettrace");
        }
        else
        {
            outputPath = configOutputPath;
        }
        
        LPWSTR configToParse = eventpipeConfig;
        int providerCnt = 0;

        // Create EventPipeProviderConfiguration and start tracing.
        NewArrayHolder<EventPipeProviderConfiguration> pProviders = nullptr;
        NewArrayHolder<XplatEventLoggerConfiguration> pConfigurations = nullptr;

        // If COMPlus_EnableEventPipe is set to 1 but no configuration was specified, enable EventPipe session
        // with the default provider configurations.
        if (configToParse == nullptr || *configToParse == L'\0')
        {
            providerCnt = 3;
            pProviders = new EventPipeProviderConfiguration[providerCnt];
            pProviders[0] = EventPipeProviderConfiguration(W("Microsoft-Windows-DotNETRuntime"), 0x4c14fccbd, 5, nullptr);
            pProviders[1] = EventPipeProviderConfiguration(W("Microsoft-Windows-DotNETRuntimePrivate"), 0x4002000b, 5, nullptr);
            pProviders[2] = EventPipeProviderConfiguration(W("Microsoft-DotNETCore-SampleProfiler"), 0x0, 5, nullptr);
        }
        else
        {
            // Count how many providers there are to parse
            static WCHAR comma = W(',');
            while (*configToParse != '\0')
            {
                providerCnt += 1;
                auto end = wcschr(configToParse, comma);
                if (end == nullptr)
                {
                    break;
                }
                configToParse = end + 1;
            }
            configToParse = eventpipeConfig;
            pProviders = new EventPipeProviderConfiguration[providerCnt];
            pConfigurations = new XplatEventLoggerConfiguration[providerCnt];
            int i = 0;
            while (*configToParse != '\0')
            {
                auto end = wcschr(configToParse, comma);
                pConfigurations[i].Parse(configToParse);

                // if we find any invalid configuration, do not trace.
                if (!pConfigurations[i].IsValid())
                {
                    return;
                }

                pProviders[i] = EventPipeProviderConfiguration(
                    pConfigurations[i].GetProviderName(),
                    pConfigurations[i].GetEnabledKeywordsMask(),
                    pConfigurations[i].GetLevel(),
                    pConfigurations[i].GetArgument()
                );

                ++i;

                if (end == nullptr)
                {
                    break;
                }
                configToParse = end + 1;
            }
        }

        uint64_t sessionID = EventPipe::Enable(
            outputPath,
            eventpipeCircularBufferMB,
            pProviders,
            providerCnt,
            EventPipeSessionType::File,
            EventPipeSerializationFormat::NetTraceV4,
            true,
            nullptr
        );
        EventPipe::StartStreaming(sessionID);
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
    IpcStream *const pStream,
    EventPipeSessionSynchronousCallback callback)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(format < EventPipeSerializationFormat::Count);
        PRECONDITION(sessionType == EventPipeSessionType::Synchronous || circularBufferSizeInMB > 0);
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
            numProviders,
            callback);

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

    if (SessionRequestedSampling(pSession))
    {
        SampleProfiler::Enable();
    }

    return true;
}

void EventPipe::StartStreaming(EventPipeSessionID id)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    CrstHolder _crst(GetLock());

    if (!IsSessionIdInCollection(id))
        return;

    EventPipeSession *const pSession = reinterpret_cast<EventPipeSession *>(id);

    if (s_CanStartThreads)
    {
        pSession->StartStreaming();
    }
    else
    {
        s_rgDeferredEnableEventPipeSessionIds.Push(id);
    }
    
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

    // EventPipe::Disable is called synchronously since the diagnostics server is
    // single threaded.  HOWEVER, if the runtime was suspended in EEStartupHelper,
    // then EventPipe::FinishInitialize might not have executed yet.  Disabling a session
    // needs to either happen before we resume or after initialization.  We briefly take the
    // lock to check s_CanStartThreads to check whether we've finished initialization.  We 
    // also check whether we are still suspended in which case we can safely disable the session
    // without deferral.
    {
        CrstHolder _crst(GetLock());
        if (!s_CanStartThreads && !IpcStreamFactory::AnySuspendedPorts())
        {
            s_rgDeferredDisableEventPipeSessionIds.Push(id);
            return;
        }
    }

    DisableHelper(id);
}

void EventPipe::DisableHelper(EventPipeSessionID id)
{
    if (s_CanStartThreads)
        SetupThread();

    if (id == 0)
        return;

    // Don't block GC during clean-up.
    GCX_PREEMP();

    RunWithCallbackPostponed([&](EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue) {
        if (s_numberOfSessions > 0)
            DisableInternal(id, pEventPipeProviderCallbackDataQueue);
    });

#ifdef DEBUG
    if ((int)s_numberOfSessions == 0)
    {
        _ASSERTE(!MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context.EventPipeProvider.IsEnabled);
        _ASSERTE(!MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context.EventPipeProvider.IsEnabled);
        _ASSERTE(!MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.EventPipeProvider.IsEnabled);
    }
#endif
}

static void LogProcessInformationEvent(EventPipeEventSource &eventSource)
{
    // Get the managed command line.
    LPCWSTR pCmdLine = GetCommandLineForDiagnostics();

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

    if (SessionRequestedSampling(pSession))
    {
        // Disable the profiler.
        SampleProfiler::Disable();
    }

    // Log the process information event.
    LogProcessInformationEvent(*s_pEventSource);

    // Disable pSession tracing.
    s_config.Disable(*pSession, pEventPipeProviderCallbackDataQueue);

    pSession->Disable(); // WriteAllBuffersToFile, and remove providers.

    // Do rundown before fully stopping the session unless rundown wasn't requested
    if (pSession->RundownRequested() && s_CanStartThreads)
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

    // Remove the session from the array before calling SuspendWriteEvent. This way
    // we can guarantee that either the event write got the pointer and will complete
    // the write successfully, or it gets null and will bail.
    _ASSERTE(s_pSessions[pSession->GetIndex()] == pSession);
    s_pSessions[pSession->GetIndex()].Store(nullptr);

    pSession->SuspendWriteEvent();
    bool ignored;
    pSession->WriteAllBuffersToFile(&ignored); // Flush the buffers to the stream/file

    --s_numberOfSessions;

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

bool EventPipe::IsSessionEnabled(EventPipeSessionID id)
{
    LIMITED_METHOD_CONTRACT;

    const EventPipeSession *const pSession = reinterpret_cast<EventPipeSession *>(id);
    return s_pSessions[pSession->GetIndex()].Load() != nullptr;
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

    NotifyProfilerProviderCreated(pProvider);
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

    return s_config.CreateProvider(providerName,
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

void EventPipe::AddProviderToSession(EventPipeSessionProvider *pProvider, EventPipeSession *pSession)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }

    CONTRACTL_END;

    if (pProvider == nullptr || pSession == nullptr)
    {
        return;
    }
    {
        CrstHolder _crst(GetLock());

        pSession->AddSessionProvider(pProvider);
    }
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
                    pRundownSession->WriteEvent(
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
            if ((s_allowWrite & ((uint64_t)1 << i)) == 0)
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

    // Before we call into StackWalkFrames we need to mark GC_ON_TRANSITIONS as FALSE
    // because under GCStress runs (GCStress=0x3), a GC will be triggered for every transition,
    // which will cause the GC to try to walk the stack while we are in the middle of walking the stack.
    bool gcOnTransitions = GC_ON_TRANSITIONS(FALSE);

    StackWalkAction swaRet = pThread->StackWalkFrames(
        (PSTACKWALKFRAMESCALLBACK)&StackWalkCallback,
        &stackContents,
        ALLOW_ASYNC_STACK_WALK | FUNCTIONSONLY | HANDLESKIPPEDFRAMES | ALLOW_INVALID_OBJECTS);

    GC_ON_TRANSITIONS(gcOnTransitions);
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

bool EventPipe::SessionRequestedSampling(EventPipeSession *pSession)
{
    LIMITED_METHOD_CONTRACT;

    EventPipeSessionProviderIterator providerList = pSession->GetProviders();
    EventPipeSessionProvider *pProvider = nullptr;
    while (providerList.Next(&pProvider))
    {
        if (wcscmp(pProvider->GetProviderName(), W("Microsoft-DotNETCore-SampleProfiler")) == 0)
        {
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

HANDLE EventPipe::GetWaitHandle(EventPipeSessionID sessionID)
{
    LIMITED_METHOD_CONTRACT;

    EventPipeSession *const pSession = GetSession(sessionID);
    return pSession ? pSession->GetWaitEvent()->GetHandleUNHOSTED() : 0;
}

void EventPipe::InvokeCallback(EventPipeProviderCallbackData *pEventPipeProviderCallbackData)
{
#if defined(HOST_OSX) && defined(HOST_ARM64)
    auto jitWriteEnableHolder = PAL_JITWriteEnable(false);
#endif // defined(HOST_OSX) && defined(HOST_ARM64)

    EventPipeProvider::InvokeCallback(pEventPipeProviderCallbackData);
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
