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
    uint32_t index,
    LPCWSTR strOutputPath,
    IpcStream *const pStream,
    EventPipeSessionType sessionType,
    EventPipeSerializationFormat format,
    bool rundownSwitch,
    uint32_t circularBufferSizeInMB,
    const EventPipeProviderConfiguration *pProviders,
    uint32_t numProviders,
    bool rundownEnabled) : m_index(index),
                           m_pProviderList(new EventPipeSessionProviderList(pProviders, numProviders)),
                           m_rundownEnabled(rundownEnabled),
                           m_SessionType(sessionType),
                           m_format(format),
                           m_rundownRequested(rundownSwitch)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(index < EventPipe::MaxNumberOfSessions);
        PRECONDITION(format < EventPipeSerializationFormat::Count);
        PRECONDITION(circularBufferSizeInMB > 0);
        PRECONDITION(numProviders > 0 && pProviders != nullptr);
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    size_t sequencePointAllocationBudget = 0;
    // Hard coded 10MB for now, we'll probably want to make
    // this configurable later.
    if (GetSessionType() != EventPipeSessionType::Listener &&
        GetSerializationFormat() >= EventPipeSerializationFormat::NetTraceV4)
    {
        sequencePointAllocationBudget = 10 * 1024 * 1024;
    }

    m_pBufferManager = new EventPipeBufferManager(this, static_cast<size_t>(circularBufferSizeInMB) << 20, sequencePointAllocationBudget);

    // Create the event pipe file.
    // A NULL output path means that we should not write the results to a file.
    // This is used in the EventListener case.
    m_pFile = nullptr;
    switch (sessionType)
    {
    case EventPipeSessionType::File:
        if (strOutputPath != nullptr)
            m_pFile = new EventPipeFile(new FileStreamWriter(SString(strOutputPath)), format);
        break;

    case EventPipeSessionType::IpcStream:
        m_pFile = new EventPipeFile(new IpcStreamWriter(reinterpret_cast<uint64_t>(this), pStream), format);
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
        PRECONDITION(!m_ipcStreamingEnabled);
    }
    CONTRACTL_END;

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

    Thread *const pThisThread = pEventPipeSession->GetIpcStreamingThread();
    bool fSuccess = true;

    {
        GCX_PREEMP();
        EX_TRY
        {
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
        }
        EX_CATCH
        {
            pEventPipeSession->SetThreadShutdownEvent();
            // TODO: STRESS_LOG ?
            // TODO: Notify `EventPipe` itself to remove this session from the list.
        }
        EX_END_CATCH(SwallowAllExceptions);
    }

    EX_TRY
    {
        if (!fSuccess)
            EventPipe::Disable(reinterpret_cast<EventPipeSessionID>(pEventPipeSession));
    }
    EX_CATCH
    {
        // TODO: STRESS_LOG ?
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (pThisThread != nullptr)
        ::DestroyThread(pThisThread);

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

bool EventPipeSession::IsValid() const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(EventPipe::IsLockOwnedByCurrentThread());
    }
    CONTRACTL_END;

    return !m_pProviderList->IsEmpty();
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

EventPipeSessionProvider *EventPipeSession::GetSessionProvider(const EventPipeProvider *pProvider) const
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

bool EventPipeSession::WriteEventBuffered(
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
    return event.IsEnabled(GetMask()) ?
        m_pBufferManager->WriteEvent(pThread, *this, event, payload, pActivityId, pRelatedActivityId, pEventThread, pStack) :
        false;
}

void EventPipeSession::WriteSequencePointUnbuffered()
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
    EventPipeSequencePoint sequencePoint;
    m_pBufferManager->InitSequencePointThreadList(&sequencePoint);
    m_pFile->WriteSequencePoint(&sequencePoint);
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

    // update the provider context here since the callback doesn't happen till we actually try to do rundown.
    MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.EventPipeProvider.Level = VerboseLoggingLevel;
    MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.EventPipeProvider.EnabledKeywordsBitmask = Keywords;
    MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.EventPipeProvider.IsEnabled = true;

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

    m_rundownEnabled = true;
}

void EventPipeSession::DisableIpcStreamingThread()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(m_SessionType == EventPipeSessionType::IpcStream);
        PRECONDITION(m_ipcStreamingEnabled);
    }
    CONTRACTL_END;

    _ASSERTE(!g_fProcessDetach);

    // The IPC streaming thread will watch this value and exit
    // when profiling is disabled.
    m_ipcStreamingEnabled = false;

    // Wait for the sampling thread to clean itself up.
    m_threadShutdownEvent.Wait(INFINITE, FALSE /* bAlertable */);
    m_threadShutdownEvent.CloseEvent();
}

void EventPipeSession::Disable()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if ((m_SessionType == EventPipeSessionType::IpcStream) && m_ipcStreamingEnabled)
        DisableIpcStreamingThread();

    WriteAllBuffersToFile();
    m_pProviderList->Clear();
}

void EventPipeSession::SuspendWriteEvent()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // Force all in-progress writes to either finish or cancel
    // This is required to ensure we can safely flush and delete the buffers
    m_pBufferManager->SuspendWriteEvent(GetIndex());
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
