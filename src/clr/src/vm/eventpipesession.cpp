// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipe.h"
#include "eventpipebuffermanager.h"
#include "eventpipefile.h"
#include "eventpipeprovider.h"
#include "eventpipesession.h"
#include "eventpipesessionprovider.h"

#ifdef FEATURE_PERFTRACING

EventPipeSession::EventPipeSession(
    EventPipeSessionID id,
    LPCWSTR strOutputPath,
    IpcStream *const pStream,
    EventPipeSessionType sessionType,
    unsigned int circularBufferSizeInMB,
    const EventPipeProviderConfiguration *pProviders,
    uint32_t numProviders,
    bool rundownEnabled) : m_Id(id),
                           m_pProviderList(new EventPipeSessionProviderList(pProviders, numProviders)),
                           m_CircularBufferSizeInBytes(static_cast<size_t>(circularBufferSizeInMB) << 20),
                           m_pBufferManager(new EventPipeBufferManager()),
                           m_rundownEnabled(rundownEnabled),
                           m_SessionType(sessionType)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(circularBufferSizeInMB > 0);
        PRECONDITION(numProviders > 0 && pProviders != nullptr);
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    // Create the event pipe file.
    // A NULL output path means that we should not write the results to a file.
    // This is used in the EventListener case.
    m_pFile = nullptr;
    switch (sessionType)
    {
    case EventPipeSessionType::File:
        if (strOutputPath != nullptr)
            m_pFile = new EventPipeFile(new FileStreamWriter(SString(strOutputPath)));
        break;

    case EventPipeSessionType::IpcStream:
        m_pFile = new EventPipeFile(new IpcStreamWriter(m_Id, pStream));
        break;

    default:
        m_pFile = nullptr;
        break;
    }

    GetSystemTimeAsFileTime(&m_sessionStartTime);
    QueryPerformanceCounter(&m_sessionStartTimeStamp);
}

EventPipeSession::~EventPipeSession()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // TODO: Stop streaming thread? Review synchronization.
    delete m_pProviderList;
    delete m_pBufferManager;
    delete m_pFile;
}

bool EventPipeSession::HasIpcStreamingStarted()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    return m_pIpcStreamingThread != nullptr ? m_pIpcStreamingThread->HasStarted() : false;
}

void EventPipeSession::SetThreadShutdownEvent()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // Signal Disable() that the thread has been destroyed.
    m_threadShutdownEvent.Set();
}

void EventPipeSession::DestroyIpcStreamingThread()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (m_pIpcStreamingThread != nullptr)
        ::DestroyThread(m_pIpcStreamingThread);
    m_pIpcStreamingThread = nullptr;
}

static void PlatformSleep()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // Wait until it's time to sample again.
    const uint32_t PeriodInNanoSeconds = 100000000; // 100 msec.

#ifdef FEATURE_PAL
    PAL_nanosleep(PeriodInNanoSeconds);
#else  //FEATURE_PAL
    const uint32_t NUM_NANOSECONDS_IN_1_MS = 1000000;
    ClrSleepEx(PeriodInNanoSeconds / NUM_NANOSECONDS_IN_1_MS, FALSE);
#endif //FEATURE_PAL
}

DWORD WINAPI EventPipeSession::ThreadProc(void *args)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(args != nullptr);
    }
    CONTRACTL_END;

    if (args == nullptr)
        return 1;

    EventPipeSession *const pEventPipeSession = reinterpret_cast<EventPipeSession *>(args);
    if (pEventPipeSession->GetSessionType() != EventPipeSessionType::IpcStream)
        return 1;

    if (!pEventPipeSession->HasIpcStreamingStarted())
        return 1;

    {
        GCX_PREEMP();
        EX_TRY
        {
            bool fSuccess = true;
            while (pEventPipeSession->IsIpcStreamingEnabled())
            {
                if (!pEventPipeSession->WriteAllBuffersToFile())
                {
                    fSuccess = false;
                    break;
                }

                // Wait until it's time to sample again.
                PlatformSleep();
            }

            pEventPipeSession->SetThreadShutdownEvent();

            if (!fSuccess)
            {
                EventPipe::RunWithCallbackPostponed([pEventPipeSession](EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue){pEventPipeSession->Disable();});
            }
        }
        EX_CATCH
        {
            pEventPipeSession->SetThreadShutdownEvent();
            // TODO: STRESS_LOG ?
            // TODO: Should we notify EventPipe itself to remove this session from the list.
        }
        EX_END_CATCH(SwallowAllExceptions);
    }

    pEventPipeSession->DestroyIpcStreamingThread();
    return 0;
}

void EventPipeSession::CreateIpcStreamingThread()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    m_ipcStreamingEnabled = true;
    m_pIpcStreamingThread = SetupUnstartedThread();
    if (m_pIpcStreamingThread->CreateNewThread(0, ThreadProc, this))
    {
        m_pIpcStreamingThread->SetBackground(TRUE);
        m_pIpcStreamingThread->StartThread();
    }
    else
    {
        _ASSERT(!"Unable to create IPC stream flushing thread.");
    }
    m_threadShutdownEvent.CreateManualEvent(FALSE);
}

bool EventPipeSession::IsValid()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    return (m_pProviderList != nullptr) && (!m_pProviderList->IsEmpty());
}

void EventPipeSession::AddSessionProvider(EventPipeSessionProvider *pProvider)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    m_pProviderList->AddSessionProvider(pProvider);
}

EventPipeSessionProvider *EventPipeSession::GetSessionProvider(EventPipeProvider *pProvider)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    return m_pProviderList->GetSessionProvider(pProvider);
}

bool EventPipeSession::WriteAllBuffersToFile()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if (m_pFile == nullptr)
        return true;

    // Get the current time stamp.
    // EventPipeBufferManager::WriteAllBuffersToFile will use this to ensure that no events after
    // the current timestamp are written into the file.
    LARGE_INTEGER stopTimeStamp;
    QueryPerformanceCounter(&stopTimeStamp);
    m_pBufferManager->WriteAllBuffersToFile(m_pFile, stopTimeStamp);
    return !m_pFile->HasErrors();
}

bool EventPipeSession::WriteEvent(
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

    // Filter events specific to "this" session based on precomputed flag on provider/events.
    return event.IsEnabled(GetId()) ?
        m_pBufferManager->WriteEvent(pThread, *this, event, payload, pActivityId, pRelatedActivityId) :
        false;
}

void EventPipeSession::WriteEvent(EventPipeEventInstance &instance)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pFile == nullptr)
        return;
    m_pFile->WriteEvent(instance);
}

EventPipeEventInstance *EventPipeSession::GetNextEvent()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(!EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    return m_pBufferManager->GetNextEvent();
}

void EventPipeSession::Enable()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        // Lock must be held by EventPipe::Enable.
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    if (m_SessionType == EventPipeSessionType::IpcStream)
        CreateIpcStreamingThread();
}

void EventPipeSession::EnableRundown()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        // Lock must be held by EventPipe::Enable.
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    //! The keywords below seems to correspond to:
    //!  LoaderKeyword                      (0x00000008)
    //!  JitKeyword                         (0x00000010)
    //!  NgenKeyword                        (0x00000020)
    //!  unused_keyword                     (0x00000100)
    //!  JittedMethodILToNativeMapKeyword   (0x00020000)
    //!  ThreadTransferKeyword              (0x80000000)
    const UINT64 Keywords = 0x80020138;
    const UINT32 VerboseLoggingLevel = static_cast<UINT32>(EventPipeEventLevel::Verbose);
    const EventPipeProviderConfiguration RundownProviders[] = {
        {W("Microsoft-Windows-DotNETRuntime"), Keywords, VerboseLoggingLevel, NULL},       // Public provider.
        {W("Microsoft-Windows-DotNETRuntimeRundown"), Keywords, VerboseLoggingLevel, NULL} // Rundown provider.
    };
    const uint32_t RundownProvidersSize = sizeof(RundownProviders) / sizeof(EventPipeProviderConfiguration);

    // Update provider list with rundown configuration.
    for (uint32_t i = 0; i < RundownProvidersSize; ++i)
    {
        const EventPipeProviderConfiguration &Config = RundownProviders[i];
        m_pProviderList->AddSessionProvider(new EventPipeSessionProvider(
            Config.GetProviderName(),
            Config.GetKeywords(),
            (EventPipeEventLevel)Config.GetLevel(),
            Config.GetFilterData()));
    }

    m_pRundownThread = GetThread();
    m_rundownEnabled = true;
}

void EventPipeSession::DisableIpcStreamingThread()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    if ((m_SessionType == EventPipeSessionType::IpcStream) && m_ipcStreamingEnabled)
    {
        _ASSERTE(!g_fProcessDetach);

        // The IPC streaming thread will watch this value and exit
        // when profiling is disabled.
        m_ipcStreamingEnabled = false;

        // Wait for the sampling thread to clean itself up.
        m_threadShutdownEvent.Wait(INFINITE, FALSE /* bAlertable */);
        m_threadShutdownEvent.CloseEvent();
    }
}

void EventPipeSession::Disable()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        // Lock must be held by EventPipe::Disable.
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    if (m_pFile == nullptr)
        return;

    DisableIpcStreamingThread();

    // Force all in-progress writes to either finish or cancel
    // This is required to ensure we can safely flush and delete the buffers
    m_pBufferManager->SuspendWriteEvent();
    {
        WriteAllBuffersToFile();
        m_pProviderList->Clear();
    }
}

void EventPipeSession::ExecuteRundown()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        // Lock must be held by EventPipe::Disable.
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    if (m_pFile == nullptr)
        return;

    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EventPipeRundown) > 0)
    {
        // Ask the runtime to emit rundown events.
        if (g_fEEStarted && !g_fEEShutDown)
            ETW::EnumerationLog::EndRundown();
    }
}

#endif // FEATURE_PERFTRACING
