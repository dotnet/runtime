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

#ifdef FEATURE_PAL
#include "pal.h"
#endif // FEATURE_PAL

#ifdef FEATURE_PERFTRACING

CrstStatic EventPipe::s_configCrst;
bool EventPipe::s_tracingInitialized = false;
EventPipeConfiguration *EventPipe::s_pConfig = NULL;
EventPipeSession *EventPipe::s_pSession = NULL;
EventPipeBufferManager *EventPipe::s_pBufferManager = NULL;
LPCWSTR EventPipe::s_pOutputPath = NULL;
EventPipeFile *EventPipe::s_pFile = NULL;
EventPipeEventSource *EventPipe::s_pEventSource = NULL;
LPCWSTR EventPipe::s_pCommandLine = NULL;
unsigned long EventPipe::s_nextFileIndex;
HANDLE EventPipe::s_fileSwitchTimerHandle = NULL;
ULONGLONG EventPipe::s_lastFlushSwitchTime = 0;
uint64_t EventPipe::s_multiFileTraceLengthInSeconds = 0;

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
    delete[] s_pOutputPath;
    s_pOutputPath = NULL;

    // On Windows, this is just a pointer to the return value from
    // GetCommandLineW(), so don't attempt to free it.
#ifdef FEATURE_PAL
    delete[] s_pCommandLine;
    s_pCommandLine = NULL;
#endif
}

EventPipeSessionID EventPipe::Enable(
    LPCWSTR strOutputPath,
    uint32_t circularBufferSizeInMB,
    uint64_t profilerSamplingRateInNanoseconds,
    const EventPipeProviderConfiguration *pProviders,
    uint32_t numProviders,
    uint64_t multiFileTraceLengthInSeconds)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION((numProviders == 0) || (numProviders > 0 && pProviders != nullptr));
    }
    CONTRACTL_END;

    // Take the lock before enabling tracing.
    CrstHolder _crst(GetLock());

    s_multiFileTraceLengthInSeconds = multiFileTraceLengthInSeconds;

    // Create a new session.
    SampleProfiler::SetSamplingRate((unsigned long)profilerSamplingRateInNanoseconds);
    EventPipeSession *pSession = s_pConfig->CreateSession(
        (strOutputPath != nullptr) ? EventPipeSessionType::File : EventPipeSessionType::Streaming,
        circularBufferSizeInMB,
        pProviders,
        numProviders);

    // Initialize the next file index.
    s_nextFileIndex = 1;

    // Initialize the last file switch time.
    s_lastFlushSwitchTime = CLRGetTickCount64();

    // Create the event pipe file.
    // A NULL output path means that we should not write the results to a file.
    // This is used in the EventListener streaming case.
    if (strOutputPath != NULL)
    {

        // Save the output file path.
        SString outputPath(strOutputPath);
        SIZE_T outputPathLen = outputPath.GetCount();
        WCHAR *pOutputPath = new WCHAR[outputPathLen + 1];
        wcsncpy(pOutputPath, outputPath.GetUnicode(), outputPathLen);
        pOutputPath[outputPathLen] = '\0';
        s_pOutputPath = pOutputPath;

        SString nextTraceFilePath;
        GetNextFilePath(nextTraceFilePath);

        s_pFile = new EventPipeFile(new FileStreamWriter(nextTraceFilePath));
    }

    const DWORD FileSwitchTimerPeriodMS = 1000;
    return Enable(pSession, SwitchToNextFileTimerCallback, FileSwitchTimerPeriodMS, FileSwitchTimerPeriodMS);
}

EventPipeSessionID EventPipe::Enable(
    IpcStream *pStream,
    uint32_t circularBufferSizeInMB,
    uint64_t profilerSamplingRateInNanoseconds,
    const EventPipeProviderConfiguration *pProviders,
    uint32_t numProviders)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pStream != nullptr);
        PRECONDITION((numProviders == 0) || (numProviders > 0 && pProviders != nullptr));
    }
    CONTRACTL_END;

    if (numProviders == 0 || pProviders == nullptr)
        return (EventPipeSessionID) nullptr;

    // Take the lock before enabling tracing.
    CrstHolder _crst(GetLock());

    // Create a new session.
    SampleProfiler::SetSamplingRate((unsigned long)profilerSamplingRateInNanoseconds);
    EventPipeSession *pSession = s_pConfig->CreateSession(
        EventPipeSessionType::IpcStream,
        circularBufferSizeInMB,
        pProviders,
        numProviders);

    // Reply back to client with the SessionId
    uint32_t nBytesWritten = 0;
    EventPipeSessionID sessionId = (EventPipeSessionID) pSession;
    bool fSuccess = pStream->Write(&sessionId, sizeof(sessionId), nBytesWritten);
    if (!fSuccess)
    {
        // TODO: Add error handling.
        s_pConfig->DeleteSession(pSession);

        delete pStream;
        return (EventPipeSessionID) nullptr;
    }

    s_pFile = new EventPipeFile(new IpcStreamWriter(pStream));

    // Enable the session.
    const DWORD FlushTimerPeriodMS = 100; // TODO: Define a good number here for streaming.
    return Enable(pSession, FlushTimer, FlushTimerPeriodMS, FlushTimerPeriodMS);
}

EventPipeSessionID EventPipe::Enable(
    EventPipeSession *const pSession,
    WAITORTIMERCALLBACK callback,
    DWORD dueTime,
    DWORD period)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(pSession != nullptr);
        PRECONDITION(callback != nullptr);
        PRECONDITION(dueTime > 0);
        PRECONDITION(period > 0);
        PRECONDITION(GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    // If tracing is not initialized or is already enabled, bail here.
    if (!s_tracingInitialized || s_pConfig == nullptr || s_pConfig->Enabled())
        return 0;

    // If the state or arguments are invalid, bail here.
    if (pSession == NULL || !pSession->IsValid())
        return 0;

    // Enable the EventPipe EventSource.
    s_pEventSource->Enable(pSession);

    // Save the session.
    s_pSession = pSession;

    // Enable tracing.
    s_pConfig->Enable(s_pSession);

    // Enable the sample profiler
    SampleProfiler::Enable();

    CreateFlushTimerCallback(callback, dueTime, period);

    // Return the session ID.
    return (EventPipeSessionID)s_pSession;
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

    // Only perform the disable operation if the session ID
    // matches the current active session.
    if (id != (EventPipeSessionID)s_pSession)
        return;

    // Don't block GC during clean-up.
    GCX_PREEMP();

    // Take the lock before disabling tracing.
    CrstHolder _crst(GetLock());

    if (s_pConfig != NULL && s_pConfig->Enabled())
    {
        // Disable the profiler.
        SampleProfiler::Disable();

        // Log the process information event.
        s_pEventSource->SendProcessInfo(s_pCommandLine);

        // Log the runtime information event.
        ETW::InfoLog::RuntimeInformation(ETW::InfoLog::InfoStructs::Normal);

        // Disable tracing.
        s_pConfig->Disable(s_pSession);

        s_multiFileTraceLengthInSeconds = 0;

        // Delete the session.
        s_pConfig->DeleteSession(s_pSession);
        s_pSession = NULL;

        // Delete the file switch timer.
        DeleteFlushTimerCallback();

        // Flush all write buffers to make sure that all threads see the change.
        FlushProcessWriteBuffers();

        // Write to the file.
        if (s_pFile != nullptr)
        {
            LARGE_INTEGER disableTimeStamp;
            QueryPerformanceCounter(&disableTimeStamp);
            s_pBufferManager->WriteAllBuffersToFile(s_pFile, disableTimeStamp);

            if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EventPipeRundown) > 0)
            {
                // Before closing the file, do rundown.
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
                s_pConfig->EnableRundown(s_pSession);

                // Ask the runtime to emit rundown events.
                if (g_fEEStarted && !g_fEEShutDown)
                    ETW::EnumerationLog::EndRundown();

                // Disable the event pipe now that rundown is complete.
                s_pConfig->Disable(s_pSession);

                // Delete the rundown session.
                s_pConfig->DeleteSession(s_pSession);
                s_pSession = NULL;
            }

            delete s_pFile;
            s_pFile = nullptr;
        }

        // De-allocate buffers.
        s_pBufferManager->DeAllocateBuffers();

        // Delete deferred providers.
        // Providers can't be deleted during tracing because they may be needed when serializing the file.
        s_pConfig->DeleteDeferredProviders();
    }
}

void EventPipe::CreateFlushTimerCallback(WAITORTIMERCALLBACK callback, DWORD dueTime, DWORD period)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(callback != nullptr);
        PRECONDITION(dueTime > 0);
        PRECONDITION(period > 0);
        PRECONDITION(GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END

    if (s_pFile == nullptr)
        return;

    NewHolder<ThreadpoolMgr::TimerInfoContext> timerContextHolder = new (nothrow) ThreadpoolMgr::TimerInfoContext();
    if (timerContextHolder == NULL)
        return;

    timerContextHolder->TimerId = 0;

    bool success = false;
    _ASSERTE(s_fileSwitchTimerHandle == NULL);
    EX_TRY
    {
        if (ThreadpoolMgr::CreateTimerQueueTimer(
                &s_fileSwitchTimerHandle,
                callback,
                timerContextHolder,
                dueTime,
                period,
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

void WINAPI EventPipe::SwitchToNextFileTimerCallback(PVOID parameter, BOOLEAN timerFired)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(timerFired);
    }
    CONTRACTL_END;

    // Take the lock control lock to make sure that tracing isn't disabled during this operation.
    CrstHolder _crst(GetLock());

    if (s_pSession == nullptr || s_pFile == nullptr)
        return;

    // Make sure that we should actually switch files.
    if (!Enabled() || s_pSession->GetSessionType() != EventPipeSessionType::File || s_multiFileTraceLengthInSeconds == 0)
        return;

    GCX_PREEMP();

    if (CLRGetTickCount64() > (s_lastFlushSwitchTime + (s_multiFileTraceLengthInSeconds * 1000)))
    {
        SwitchToNextFile();
        s_lastFlushSwitchTime = CLRGetTickCount64();
    }
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

    // Take the lock control lock to make sure that tracing isn't disabled during this operation.
    CrstHolder _crst(GetLock());

    if (s_pSession == nullptr || s_pFile == nullptr)
        return;

    // Make sure that we should actually switch files.
    if (!Enabled() || s_pSession->GetSessionType() != EventPipeSessionType::IpcStream)
        return;

    GCX_PREEMP();

    if (CLRGetTickCount64() > (s_lastFlushSwitchTime + 100))
    {
        // Get the current time stamp.
        // WriteAllBuffersToFile will use this to ensure that no events after
        // the current timestamp are written into the file.
        LARGE_INTEGER stopTimeStamp;
        QueryPerformanceCounter(&stopTimeStamp);
        s_pBufferManager->WriteAllBuffersToFile(s_pFile, stopTimeStamp);

        s_lastFlushSwitchTime = CLRGetTickCount64();
    }
}

void EventPipe::SwitchToNextFile()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(s_pFile != nullptr);
        PRECONDITION(GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END

    // Get the current time stamp.
    // WriteAllBuffersToFile will use this to ensure that no events after the current timestamp are written into the file.
    LARGE_INTEGER stopTimeStamp;
    QueryPerformanceCounter(&stopTimeStamp);
    s_pBufferManager->WriteAllBuffersToFile(s_pFile, stopTimeStamp);

    // Open the new file.
    SString nextTraceFilePath;
    GetNextFilePath(nextTraceFilePath);

    StreamWriter *pStreamWriter = new (nothrow) FileStreamWriter(nextTraceFilePath);
    if (pStreamWriter == nullptr)
    {
        // TODO: Add error handling.
        return;
    }

    EventPipeFile *pFile = new (nothrow) EventPipeFile(pStreamWriter);
    if (pFile == NULL)
    {
        // TODO: Add error handling.
        return;
    }

    // Close the previous file.
    delete s_pFile;

    // Swap in the new file.
    s_pFile = pFile;
}

void EventPipe::GetNextFilePath(SString &nextTraceFilePath)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(GetLock()->OwnedByCurrentThread());
    }
    CONTRACTL_END;

    // Set the full path to the requested trace file as the next file path.
    nextTraceFilePath.Set(s_pOutputPath);

    // If multiple files have been requested, then add a sequence number to the trace file name.
    if (s_multiFileTraceLengthInSeconds > 0)
    {
        // Remove the ".netperf" file extension if it exists.
        SString::Iterator netPerfExtension = nextTraceFilePath.End();
        if (nextTraceFilePath.FindBack(netPerfExtension, W(".netperf")))
        {
            nextTraceFilePath.Truncate(netPerfExtension);
        }

        // Add the sequence number and the ".netperf" file extension.
        WCHAR strNextIndex[21];
        swprintf_s(strNextIndex, 21, W(".%u.netperf"), s_nextFileIndex++);
        nextTraceFilePath.Append(strNextIndex);
    }
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
    }
    CONTRACTL_END;

    EventPipeProvider *pProvider = NULL;
    if (s_pConfig != NULL)
    {
        pProvider = s_pConfig->CreateProvider(providerName, pCallbackFunction, pCallbackData);
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
        // It is possible that some events that are enabled on rundown can be emitted from other threads.
        // We're not interested in these events and they can cause corrupted trace files because rundown
        // events are written synchronously and not under lock.
        // If we encounter an event that did not originate on the thread that is doing rundown, ignore it.
        if (pThread == NULL || !s_pConfig->IsRundownThread(pThread))
        {
            return;
        }

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

void EventPipe::SaveCommandLine(LPCWSTR pwzAssemblyPath, int argc, LPCWSTR *argv)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(pwzAssemblyPath != NULL);
        PRECONDITION(argc <= 0 || argv != NULL);
    }
    CONTRACTL_END;

    // Get the command line.
    LPCWSTR osCommandLine = GetCommandLineW();

#ifndef FEATURE_PAL
    // On Windows, osCommandLine contains the executable and all arguments.
    s_pCommandLine = osCommandLine;
#else
    // On UNIX, the PAL doesn't have the command line arguments, so we must build the command line.
    // osCommandLine contains the full path to the executable.
    SString commandLine(osCommandLine);
    commandLine.Append((WCHAR)' ');
    commandLine.Append(pwzAssemblyPath);

    for (int i = 0; i < argc; i++)
    {
        commandLine.Append((WCHAR)' ');
        commandLine.Append(argv[i]);
    }

    // Allocate a new string for the command line.
    SIZE_T commandLineLen = commandLine.GetCount();
    WCHAR *pCommandLine = new WCHAR[commandLineLen + 1];
    wcsncpy(pCommandLine, commandLine.GetUnicode(), commandLineLen);
    pCommandLine[commandLineLen] = '\0';

    s_pCommandLine = pCommandLine;
#endif
}

EventPipeEventInstance *EventPipe::GetNextEvent()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
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

#endif // FEATURE_PERFTRACING
