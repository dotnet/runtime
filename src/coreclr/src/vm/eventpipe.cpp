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
bool EventPipe::s_tracingInitialized = false;
EventPipeConfiguration *EventPipe::s_pConfig = NULL;
EventPipeSession *EventPipe::s_pSession = NULL;
EventPipeBufferManager *EventPipe::s_pBufferManager = NULL;
EventPipeFile *EventPipe::s_pFile = NULL;
EventPipeEventSource *EventPipe::s_pEventSource = NULL;
HANDLE EventPipe::s_fileSwitchTimerHandle = NULL;
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

void EventPipe::Initialize()
{
    STANDARD_VM_CONTRACT;

    s_tracingInitialized = s_configCrst.InitNoThrow(
        CrstEventPipe,
        (CrstFlags)(CRST_REENTRANCY | CRST_TAKEN_DURING_SHUTDOWN | CRST_HOST_BREAKABLE));

    s_pConfig = new EventPipeConfiguration();
    s_pConfig->Initialize();

    s_pBufferManager = new EventPipeBufferManager();

    s_pEventSource = new EventPipeEventSource();

    // This calls into auto-generated code to initialize the runtime providers
    // and events so that the EventPipe configuration lock isn't taken at runtime
    InitProvidersAndEvents();
}

void EventPipe::Shutdown()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Mark tracing as no longer initialized.
    s_tracingInitialized = false;

    // We are shutting down, so if disabling EventPipe throws, we need to move along anyway.
    EX_TRY
    {
        Disable((EventPipeSessionID)s_pSession);
    }
    EX_CATCH {}
    EX_END_CATCH(SwallowAllExceptions);

    // Save pointers to the configuration and buffer manager.
    EventPipeConfiguration *pConfig = s_pConfig;
    EventPipeBufferManager *pBufferManager = s_pBufferManager;

    // Set the static pointers to NULL so that the rest of the EventPipe knows that they are no longer available.
    // Flush process write buffers to make sure other threads can see the change.
    s_pConfig = NULL;
    s_pBufferManager = NULL;
    FlushProcessWriteBuffers();

    // Free resources.
    delete pConfig;
    delete pBufferManager;
    delete s_pEventSource;
    s_pEventSource = NULL;

}

EventPipeSessionID EventPipe::Enable(
    LPCWSTR strOutputPath,
    uint32_t circularBufferSizeInMB,
    uint64_t profilerSamplingRateInNanoseconds,
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
        PRECONDITION(profilerSamplingRateInNanoseconds > 0);
        PRECONDITION(numProviders > 0 && pProviders != nullptr);
    }
    CONTRACTL_END;

    EventPipeSessionID sessionId;
    EventPipe::RunWithCallbackPostponed(
        [&](EventPipeProviderCallbackDataQueue* pEventPipeProviderCallbackDataQueue)
        {
            // Create a new session.
            SampleProfiler::SetSamplingRate(static_cast<unsigned long>(profilerSamplingRateInNanoseconds));
            EventPipeSession *pSession = s_pConfig->CreateSession(
                sessionType,
                circularBufferSizeInMB,
                pProviders,
                numProviders);

            // Enable the session.
            sessionId = Enable(strOutputPath, pSession, sessionType, pStream, pEventPipeProviderCallbackDataQueue);
        }
    );

    return sessionId;
}

EventPipeSessionID EventPipe::Enable(
    LPCWSTR strOutputPath,
    EventPipeSession *const pSession,
    EventPipeSessionType sessionType,
    IpcStream *const pStream,
    EventPipeProviderCallbackDataQueue* pEventPipeProviderCallbackDataQueue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pSession != nullptr);
        PRECONDITION(GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    // If the state or arguments are invalid, bail here.
    if (pSession == nullptr || !pSession->IsValid())
        return 0;
    if (sessionType == EventPipeSessionType::File && strOutputPath == nullptr)
        return 0;
    if (sessionType == EventPipeSessionType::IpcStream && pStream == nullptr)
        return 0;

    // If tracing is not initialized or is already enabled, bail here.
    if (!s_tracingInitialized || s_pConfig == nullptr || s_pConfig->Enabled())
        return 0;

    // Enable the EventPipe EventSource.
    s_pEventSource->Enable(pSession);

    // Save the session.
    s_pSession = pSession;
    EventPipeSessionID sessionId = reinterpret_cast<EventPipeSessionID>(s_pSession);

    // Create the event pipe file.
    // A NULL output path means that we should not write the results to a file.
    // This is used in the EventListener streaming case.
    switch (sessionType)
    {
        case EventPipeSessionType::File:
            if (strOutputPath != nullptr)
                s_pFile = new EventPipeFile(new FileStreamWriter(SString(strOutputPath)));
            break;

        case EventPipeSessionType::IpcStream:
            s_pFile = new EventPipeFile(new IpcStreamWriter(sessionId, pStream));
            CreateFlushTimerCallback();
            break;

        default:
            s_pFile = nullptr;
            break;
    }

    // Enable tracing.
    s_pConfig->Enable(s_pSession, pEventPipeProviderCallbackDataQueue);

    // Enable the sample profiler
    SampleProfiler::Enable(pEventPipeProviderCallbackDataQueue);

    // Return the session ID.
    return sessionId;
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

    // Only perform the disable operation if the session ID
    // matches the current active session.
    if (id != (EventPipeSessionID)s_pSession)
        return;

    // Don't block GC during clean-up.
    GCX_PREEMP();

    EventPipe::RunWithCallbackPostponed(
        [&](EventPipeProviderCallbackDataQueue* pEventPipeProviderCallbackDataQueue)
        {
            DisableInternal(reinterpret_cast<EventPipeSessionID>(s_pSession), pEventPipeProviderCallbackDataQueue);
        }
    );
}

void EventPipe::DisableInternal(EventPipeSessionID id, EventPipeProviderCallbackDataQueue* pEventPipeProviderCallbackDataQueue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    if (s_pConfig != NULL && s_pConfig->Enabled())
    {
        // Disable the profiler.
        SampleProfiler::Disable();
        
        // Get the managed command line.
        LPCWSTR pCmdLine = GetManagedCommandLine();

        // Checkout https://github.com/dotnet/coreclr/pull/24433 for more information about this fall back.
        if (pCmdLine == nullptr)
        {
            // Use the result from GetCommandLineW() instead
            pCmdLine = GetCommandLineW();
        }

        // Log the process information event.
        s_pEventSource->SendProcessInfo(pCmdLine);

        // Log the runtime information event.
        ETW::InfoLog::RuntimeInformation(ETW::InfoLog::InfoStructs::Normal);

        // Disable tracing.
        s_pConfig->Disable(s_pSession, pEventPipeProviderCallbackDataQueue);

        // Delete the session.
        s_pConfig->DeleteSession(s_pSession);
        s_pSession = NULL;

        // Delete the flush timer.
        DeleteFlushTimerCallback();

        // Force all in-progress writes to either finish or cancel
        // This is required to ensure we can safely flush and delete the buffers
        s_pBufferManager->SuspendWriteEvent();

        // Write to the file.
        if (s_pFile != nullptr)
        {
            LARGE_INTEGER disableTimeStamp;
            QueryPerformanceCounter(&disableTimeStamp);
            s_pBufferManager->WriteAllBuffersToFile(s_pFile, disableTimeStamp);

            if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EventPipeRundown) > 0)
            {
                // Before closing the file, do rundown. We have to re-enable event writing for this.

                s_pBufferManager->ResumeWriteEvent();

                const EventPipeProviderConfiguration RundownProviders[] = {
                    {W("Microsoft-Windows-DotNETRuntime"), 0x80020138, static_cast<unsigned int>(EventPipeEventLevel::Verbose), NULL},       // Public provider.
                    {W("Microsoft-Windows-DotNETRuntimeRundown"), 0x80020138, static_cast<unsigned int>(EventPipeEventLevel::Verbose), NULL} // Rundown provider.
                };

                // The circular buffer size doesn't matter because all events are written synchronously during rundown.
                s_pSession = s_pConfig->CreateSession(
                    EventPipeSessionType::File,
                    1 /* circularBufferSizeInMB */,
                    RundownProviders,
                    sizeof(RundownProviders) / sizeof(EventPipeProviderConfiguration));
                s_pConfig->EnableRundown(s_pSession, pEventPipeProviderCallbackDataQueue);

                // Ask the runtime to emit rundown events.
                if (g_fEEStarted && !g_fEEShutDown)
                    ETW::EnumerationLog::EndRundown();

                // Disable the event pipe now that rundown is complete.
                s_pConfig->Disable(s_pSession, pEventPipeProviderCallbackDataQueue);

                // Delete the rundown session.
                s_pConfig->DeleteSession(s_pSession);
                s_pSession = NULL;

                // Suspend again after rundown session
                s_pBufferManager->SuspendWriteEvent();
            }

            delete s_pFile;
            s_pFile = nullptr;
        }

        // De-allocate buffers.
        s_pBufferManager->DeAllocateBuffers();

        // Delete deferred providers.
        // Providers can't be deleted during tracing because they may be needed when serializing the file.
        s_pConfig->DeleteDeferredProviders();

        // ALlow WriteEvent to begin accepting work again so that sometime in the future
        // we can re-enable events and they will be recorded
        s_pBufferManager->ResumeWriteEvent();
    }
}

void EventPipe::CreateFlushTimerCallback()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END

    if (s_pFile == nullptr)
        return;

    NewHolder<ThreadpoolMgr::TimerInfoContext> timerContextHolder = new (nothrow) ThreadpoolMgr::TimerInfoContext();
    if (timerContextHolder == NULL)
        return;

    timerContextHolder->TimerId = 0;

    // Initialize the last flush time.
    s_lastFlushTime = CLRGetTickCount64();

    bool success = false;
    _ASSERTE(s_fileSwitchTimerHandle == NULL);
    EX_TRY
    {
        if (ThreadpoolMgr::CreateTimerQueueTimer(
                &s_fileSwitchTimerHandle,
                FlushTimer,
                timerContextHolder,
                100, // DueTime (msec)
                100, // Period (msec)
                0 /* flags */))
        {
            _ASSERTE(s_fileSwitchTimerHandle != NULL);
            success = true;
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    if (!success)
    {
        _ASSERTE(s_fileSwitchTimerHandle == NULL);
        return;
    }

    timerContextHolder.SuppressRelease(); // the timer context is automatically deleted by the timer infrastructure
}

void EventPipe::DeleteFlushTimerCallback()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END

    if ((s_fileSwitchTimerHandle != NULL) && (ThreadpoolMgr::DeleteTimerQueueTimer(s_fileSwitchTimerHandle, NULL)))
        s_fileSwitchTimerHandle = NULL;
}

void WINAPI EventPipe::FlushTimer(PVOID parameter, BOOLEAN timerFired)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(timerFired);
    }
    CONTRACTL_END;

    GCX_PREEMP();

    EventPipe::RunWithCallbackPostponed(
        [&](EventPipeProviderCallbackDataQueue* pEventPipeProviderCallbackDataQueue)
        {
            if (s_pSession == nullptr || s_pFile == nullptr)
                return;

            // Make sure that we should actually flush.
            if (!Enabled() || s_pSession->GetSessionType() != EventPipeSessionType::IpcStream)
                return;

            if (CLRGetTickCount64() > (s_lastFlushTime + 100))
            {
                // Get the current time stamp.
                // WriteAllBuffersToFile will use this to ensure that no events after
                // the current timestamp are written into the file.
                LARGE_INTEGER stopTimeStamp;
                QueryPerformanceCounter(&stopTimeStamp);
                s_pBufferManager->WriteAllBuffersToFile(s_pFile, stopTimeStamp);

                s_lastFlushTime = CLRGetTickCount64();
            }

            if (s_pFile->HasErrors())
            {
                EX_TRY
                {
                    DisableInternal(reinterpret_cast<EventPipeSessionID>(s_pSession), pEventPipeProviderCallbackDataQueue);
                }
                EX_CATCH {}
                EX_END_CATCH(SwallowAllExceptions);
            }
        }
    );
}

EventPipeSession *EventPipe::GetSession(EventPipeSessionID id)
{
    LIMITED_METHOD_CONTRACT;

    EventPipeSession *pSession = NULL;
    if ((EventPipeSessionID)s_pSession == id)
    {
        pSession = s_pSession;
    }
    return pSession;
}

bool EventPipe::Enabled()
{
    LIMITED_METHOD_CONTRACT;

    bool enabled = false;
    if (s_pConfig != NULL)
    {
        enabled = s_pConfig->Enabled();
    }

    return enabled;
}

EventPipeProvider *EventPipe::CreateProvider(const SString &providerName, EventPipeCallback pCallbackFunction, void *pCallbackData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    EventPipeProvider *pProvider = NULL;
    EventPipe::RunWithCallbackPostponed(
        [&](EventPipeProviderCallbackDataQueue* pEventPipeProviderCallbackDataQueue)
        {
            pProvider = EventPipe::CreateProvider(providerName, pCallbackFunction, pCallbackData, pEventPipeProviderCallbackDataQueue);
        }
    );

    return pProvider;
}

EventPipeProvider *EventPipe::CreateProvider(const SString &providerName, EventPipeCallback pCallbackFunction, void *pCallbackData, EventPipeProviderCallbackDataQueue* pEventPipeProviderCallbackDataQueue)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    EventPipeProvider *pProvider = NULL;
    if (s_pConfig != NULL)
    {
        pProvider = s_pConfig->CreateProvider(providerName, pCallbackFunction, pCallbackData, pEventPipeProviderCallbackDataQueue);
    }
    return pProvider;
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

    EventPipeProvider *pProvider = NULL;
    if (s_pConfig != NULL)
    {
        pProvider = s_pConfig->GetProvider(providerName);
    }

    return pProvider;
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
            if (s_pConfig != NULL)
            {
                s_pConfig->DeleteProvider(pProvider);
            }
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
    EventPipe::WriteEventInternal(event, payload, pActivityId, pRelatedActivityId);
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
    EventPipe::WriteEventInternal(event, payload, pActivityId, pRelatedActivityId);
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

    // Exit early if the event is not enabled.
    if (!event.IsEnabled())
    {
        return;
    }

    // Get the current thread;
    Thread *pThread = GetThread();

    if (s_pConfig == NULL)
    {
        // We can't procede without a configuration
        return;
    }
    _ASSERTE(s_pSession != NULL);

    // If the activity id isn't specified AND we are in a managed thread, pull it from the current thread.
    // If pThread is NULL (we aren't in writing from a managed thread) then pActivityId can be NULL
    if (pActivityId == NULL && pThread != NULL)
    {
        pActivityId = pThread->GetActivityId();
    }

    if (!s_pConfig->RundownEnabled() && s_pBufferManager != NULL)
    {
        s_pBufferManager->WriteEvent(pThread, *s_pSession, event, payload, pActivityId, pRelatedActivityId);
    }
    else if (s_pConfig->RundownEnabled())
    {
        _ASSERTE(pThread != nullptr);
        BYTE *pData = payload.GetFlatData();
        if (pData != NULL)
        {
            // Write synchronously to the file.
            // We're under lock and blocking the disabling thread.
            // This copy occurs here (rather than at file write) because
            // A) The FastSerializer API would need to change if we waited
            // B) It is unclear there is a benefit to multiple file write calls
            //    as opposed a a buffer copy here
            EventPipeEventInstance instance(
                *s_pSession,
                event,
                pThread->GetOSThreadId(),
                pData,
                payload.GetSize(),
                pActivityId,
                pRelatedActivityId);
            instance.EnsureStack(*s_pSession);

            if (s_pFile != NULL)
            {
                // EventPipeFile::WriteEvent needs to allocate a metadata event
                // and can therefore throw. In this context we will silently
                // fail rather than disrupt the caller
                EX_TRY
                {
                    s_pFile->WriteEvent(instance);
                }
                EX_CATCH {}
                EX_END_CATCH(SwallowAllExceptions);
            }
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
    }
    CONTRACTL_END;

    EventPipeEventPayload payload(pData, length);

    // Write the event to the thread's buffer.
    if (s_pBufferManager != NULL)
    {
        // Specify the sampling thread as the "current thread", so that we select the right buffer.
        // Specify the target thread so that the event gets properly attributed.
        s_pBufferManager->WriteEvent(pSamplingThread, *s_pSession, *pEvent, payload, NULL /* pActivityId */, NULL /* pRelatedActivityId */, pTargetThread, &stackContents);
    }
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
    if (pThread != NULL)
    {
        return WalkManagedStackForThread(pThread, stackContents);
    }

    return false;
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

EventPipeEventInstance *EventPipe::GetNextEvent()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(!GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    EventPipeEventInstance *pInstance = NULL;

    // Only fetch the next event if a tracing session exists.
    // The buffer manager is not disposed until the process is shutdown.
    if (s_pSession != NULL)
    {
        pInstance = s_pBufferManager->GetNextEvent();
    }

    return pInstance;
}

/* static */ void EventPipe::InvokeCallback(EventPipeProviderCallbackData eventPipeProviderCallbackData)
{
    EventPipeProvider::InvokeCallback(eventPipeProviderCallbackData);
}

#ifdef DEBUG
/* static */ bool EventPipe::IsLockOwnedByCurrentThread()
{
    return GetLock()->OwnedByCurrentThread();
}

/* static */ bool EventPipe::IsBufferManagerLockOwnedByCurrentThread()
{
    return s_pBufferManager->IsLockOwnedByCurrentThread();
}
#endif


#endif // FEATURE_PERFTRACING
