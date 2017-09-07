// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "clrtypes.h"
#include "safemath.h"
#include "common.h"
#include "eventpipe.h"
#include "eventpipebuffermanager.h"
#include "eventpipeconfiguration.h"
#include "eventpipeevent.h"
#include "eventpipefile.h"
#include "eventpipeprovider.h"
#include "eventpipejsonfile.h"
#include "sampleprofiler.h"

#ifdef FEATURE_PAL
#include "pal.h"
#endif // FEATURE_PAL

#ifdef FEATURE_PERFTRACING

CrstStatic EventPipe::s_configCrst;
bool EventPipe::s_tracingInitialized = false;
EventPipeConfiguration* EventPipe::s_pConfig = NULL;
EventPipeBufferManager* EventPipe::s_pBufferManager = NULL;
EventPipeFile* EventPipe::s_pFile = NULL;
#ifdef _DEBUG
EventPipeFile* EventPipe::s_pSyncFile = NULL;
EventPipeJsonFile* EventPipe::s_pJsonFile = NULL;
#endif // _DEBUG

#ifdef FEATURE_PAL
// This function is auto-generated from /src/scripts/genEventPipe.py
extern "C" void InitProvidersAndEvents();
#endif

#ifdef FEATURE_PAL
// This function is auto-generated from /src/scripts/genEventPipe.py
extern "C" void InitProvidersAndEvents();
#endif

EventPipeEventPayload::EventPipeEventPayload(BYTE *pData, unsigned int length)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pData = pData;
    m_pEventData = NULL;
    m_eventDataCount = 0;
    m_allocatedData = false;

    m_size = length;
}

EventPipeEventPayload::EventPipeEventPayload(EventData **pEventData, unsigned int eventDataCount)
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
    for (unsigned int i=0; i<m_eventDataCount; i++)
    {
        tmp_size += S_UINT32((*m_pEventData)[i].Size);
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
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(m_allocatedData && m_pData != NULL)
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
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(m_size > 0)
    {
        if (!IsFlattened())
        {
            BYTE* tmp_pData = new (nothrow) BYTE[m_size];
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

    if(m_size > 0)
    {
        if(IsFlattened())
        {
            memcpy(pDst, m_pData, m_size);
        }

        else if(m_pEventData != NULL)
        {
            unsigned int offset = 0;
            for(unsigned int i=0; i<m_eventDataCount; i++)
            {
                memcpy(pDst + offset, (BYTE*)(*m_pEventData)[i].Ptr, (*m_pEventData)[i].Size);
                offset += (*m_pEventData)[i].Size;
            }
        }
    }
}

BYTE* EventPipeEventPayload::GetFlatData()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
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

#ifdef FEATURE_PAL
    // This calls into auto-generated code to initialize the runtime providers
    // and events so that the EventPipe configuration lock isn't taken at runtime
    InitProvidersAndEvents();
#endif
}

void EventPipe::EnableOnStartup()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Test COMPLUS variable to enable tracing at start-up.
    if((CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PerformanceTracing) & 1) == 1)
    {
        SString outputPath;
        outputPath.Printf("Process-%d.netperf", GetCurrentProcessId());
        Enable(
            outputPath.GetUnicode(),
            1024 /* 1 GB circular buffer */,
            NULL /* pProviders */,
            0 /* numProviders */);
    }
}

void EventPipe::Shutdown()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    Disable();

    if(s_pConfig != NULL)
    {
        delete(s_pConfig);
        s_pConfig = NULL;
    }
    if(s_pBufferManager != NULL)
    {
        delete(s_pBufferManager);
        s_pBufferManager = NULL;
    }
}

void EventPipe::Enable(
    LPCWSTR strOutputPath,
    unsigned int circularBufferSizeInMB,
    EventPipeProviderConfiguration *pProviders,
    int numProviders)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If tracing is not initialized or is already enabled, bail here.
    if(!s_tracingInitialized || s_pConfig->Enabled())
    {
        return;
    }

    // Take the lock before enabling tracing.
    CrstHolder _crst(GetLock());

    // Create the event pipe file.
    SString eventPipeFileOutputPath(strOutputPath);
    s_pFile = new EventPipeFile(eventPipeFileOutputPath);

#ifdef _DEBUG
    if((CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PerformanceTracing) & 2) == 2)
    {
        // Create a synchronous file.
        SString eventPipeSyncFileOutputPath;
        eventPipeSyncFileOutputPath.Printf("Process-%d.sync.netperf", GetCurrentProcessId());
        s_pSyncFile = new EventPipeFile(eventPipeSyncFileOutputPath);

        // Create a JSON file.
        SString outputFilePath;
        outputFilePath.Printf("Process-%d.PerfView.json", GetCurrentProcessId());
        s_pJsonFile = new EventPipeJsonFile(outputFilePath);
    }
#endif // _DEBUG

    // Enable tracing.
    s_pConfig->Enable(circularBufferSizeInMB, pProviders, numProviders);

    // Enable the sample profiler
    SampleProfiler::Enable();
}

void EventPipe::Disable()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Don't block GC during clean-up.
    GCX_PREEMP();

    // Take the lock before disabling tracing.
    CrstHolder _crst(GetLock());

    if(s_pConfig->Enabled())
    {
        // Disable the profiler.
        SampleProfiler::Disable();

        // Disable tracing.
        s_pConfig->Disable();

        // Flush all write buffers to make sure that all threads see the change.
        FlushProcessWriteBuffers();

        // Write to the file.
        LARGE_INTEGER disableTimeStamp;
        QueryPerformanceCounter(&disableTimeStamp);
        s_pBufferManager->WriteAllBuffersToFile(s_pFile, disableTimeStamp);

        // Before closing the file, do rundown.
        s_pConfig->EnableRundown();

        // Ask the runtime to emit rundown events.
        if(g_fEEStarted && !g_fEEShutDown)
        {
            ETW::EnumerationLog::EndRundown();
        }

        // Disable the event pipe now that rundown is complete.
        s_pConfig->Disable();

        if(s_pFile != NULL)
        {
            delete(s_pFile);
            s_pFile = NULL;
        }
#ifdef _DEBUG
        if(s_pSyncFile != NULL)
        {
            delete(s_pSyncFile);
            s_pSyncFile = NULL;
        }
        if(s_pJsonFile != NULL)
        {
            delete(s_pJsonFile);
            s_pJsonFile = NULL;
        }
#endif // _DEBUG

        // De-allocate buffers.
        s_pBufferManager->DeAllocateBuffers();

        // Delete deferred providers.
        // Providers can't be deleted during tracing because they may be needed when serializing the file.
        s_pConfig->DeleteDeferredProviders();
    }
}

bool EventPipe::Enabled()
{
    LIMITED_METHOD_CONTRACT;

    bool enabled = false;
    if(s_pConfig != NULL)
    {
        enabled = s_pConfig->Enabled();
    }

    return enabled;
}

EventPipeProvider* EventPipe::CreateProvider(const SString &providerName, EventPipeCallback pCallbackFunction, void *pCallbackData)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    return new EventPipeProvider(providerName, pCallbackFunction, pCallbackData);
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

    if(pProvider != NULL)
    {
        if(Enabled())
        {
            // Save the provider until the end of the tracing session.
            pProvider->SetDeleteDeferred();
        }
        else
        {
            // Delete the provider now.
            // NOTE: This will remove it from all of the EventPipe data structures.
            delete(pProvider);
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

void EventPipe::WriteEvent(EventPipeEvent &event, EventData **pEventData, unsigned int eventDataCount, LPCGUID pActivityId, LPCGUID pRelatedActivityId)
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
        PRECONDITION(s_pBufferManager != NULL);
    }
    CONTRACTL_END;

    // Exit early if the event is not enabled.
    if(!event.IsEnabled())
    {
        return;
    }

    // Get the current thread;
    Thread *pThread = GetThread();
    if(pThread == NULL)
    {
        // We can't write an event without the thread object.
        return;
    }

    if(!s_pConfig->RundownEnabled() && s_pBufferManager != NULL)
    {
        if(!s_pBufferManager->WriteEvent(pThread, event, payload, pActivityId, pRelatedActivityId))
        {
            // This is used in DEBUG to make sure that we don't log an event synchronously that we didn't log to the buffer.
            return;
        }
    }
    else if(s_pConfig->RundownEnabled())
    {
        BYTE *pData = payload.GetFlatData();
        if (pData != NULL)
        {
            // Write synchronously to the file.
            // We're under lock and blocking the disabling thread.
            EventPipeEventInstance instance(
                event,
                pThread->GetOSThreadId(),
                pData,
                payload.GetSize(),
                pActivityId,
                pRelatedActivityId);

            if(s_pFile != NULL)
            {
                s_pFile->WriteEvent(instance);
            }
        }
    }

#ifdef _DEBUG
    {
        GCX_PREEMP();

        BYTE *pData = payload.GetFlatData();
        if (pData != NULL)
        {
            // Create an instance of the event for the synchronous path.
            EventPipeEventInstance instance(
                event,
                pThread->GetOSThreadId(),
                pData,
                payload.GetSize(),
                pActivityId,
                pRelatedActivityId);

            // Write to the EventPipeFile if it exists.
            if(s_pSyncFile != NULL)
            {
                s_pSyncFile->WriteEvent(instance);
            }
 
            // Write to the EventPipeJsonFile if it exists.
            if(s_pJsonFile != NULL)
            {
                s_pJsonFile->WriteEvent(instance);
            }
        }
    }
#endif // _DEBUG
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
    if(s_pBufferManager != NULL)
    {
        // Specify the sampling thread as the "current thread", so that we select the right buffer.
        // Specify the target thread so that the event gets properly attributed.
        if(!s_pBufferManager->WriteEvent(pSamplingThread, *pEvent, payload, NULL /* pActivityId */, NULL /* pRelatedActivityId */, pTargetThread, &stackContents))
        {
            // This is used in DEBUG to make sure that we don't log an event synchronously that we didn't log to the buffer.
            return;
        }
    }

#ifdef _DEBUG
    {
        GCX_PREEMP();

        // Create an instance for the synchronous path.
        SampleProfilerEventInstance instance(*pEvent, pTargetThread, pData, length);
        stackContents.CopyTo(instance.GetStack());

        // Write to the EventPipeFile.
        if(s_pSyncFile != NULL)
        {
            s_pSyncFile->WriteEvent(instance);
        }

        // Write to the EventPipeJsonFile if it exists.
        if(s_pJsonFile != NULL)
        {
            s_pJsonFile->WriteEvent(instance);
        }
    }
#endif // _DEBUG
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
    if(pThread != NULL)
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

    stackContents.Reset();

    StackWalkAction swaRet = pThread->StackWalkFrames(
        (PSTACKWALKFRAMESCALLBACK) &StackWalkCallback,
        &stackContents,
        ALLOW_ASYNC_STACK_WALK | FUNCTIONSONLY | HANDLESKIPPEDFRAMES);

    return ((swaRet == SWA_DONE) || (swaRet == SWA_CONTINUE));
}

StackWalkAction EventPipe::StackWalkCallback(CrawlFrame *pCf, StackContents *pData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(pCf != NULL);
        PRECONDITION(pData != NULL);
    }
    CONTRACTL_END;

    // Get the IP.
    UINT_PTR controlPC = (UINT_PTR)pCf->GetRegisterSet()->ControlPC;
    if(controlPC == 0)
    {
        if(pData->GetLength() == 0)
        {
            // This happens for pinvoke stubs on the top of the stack.
            return SWA_CONTINUE;
        }
    }

    _ASSERTE(controlPC != 0);

    // Add the IP to the captured stack.
    pData->Append(
        controlPC,
        pCf->GetFunction()
        );

    // Continue the stack walk.
    return SWA_CONTINUE;
}

EventPipeConfiguration* EventPipe::GetConfiguration()
{
    LIMITED_METHOD_CONTRACT;

    return s_pConfig;
}

CrstStatic* EventPipe::GetLock()
{
    LIMITED_METHOD_CONTRACT;

    return &s_configCrst;
}

void QCALLTYPE EventPipeInternal::Enable(
        __in_z LPCWSTR outputFile,
        unsigned int circularBufferSizeInMB,
        long profilerSamplingRateInNanoseconds,
        EventPipeProviderConfiguration *pProviders,
        int numProviders)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    SampleProfiler::SetSamplingRate(profilerSamplingRateInNanoseconds);
    EventPipe::Enable(outputFile, circularBufferSizeInMB, pProviders, numProviders);
    END_QCALL;
}

void QCALLTYPE EventPipeInternal::Disable()
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    EventPipe::Disable();
    END_QCALL;
}

INT_PTR QCALLTYPE EventPipeInternal::CreateProvider(
    __in_z LPCWSTR providerName,
    EventPipeCallback pCallbackFunc)
{
    QCALL_CONTRACT;

    EventPipeProvider *pProvider = NULL;

    BEGIN_QCALL;

    pProvider = EventPipe::CreateProvider(providerName, pCallbackFunc, NULL);

    END_QCALL;

    return reinterpret_cast<INT_PTR>(pProvider);
}

INT_PTR QCALLTYPE EventPipeInternal::DefineEvent(
    INT_PTR provHandle,
    unsigned int eventID,
    __int64 keywords,
    unsigned int eventVersion,
    unsigned int level,
    void *pMetadata,
    unsigned int metadataLength)
{
    QCALL_CONTRACT;

    EventPipeEvent *pEvent = NULL;

    BEGIN_QCALL;

    _ASSERTE(provHandle != NULL);
    _ASSERTE(pMetadata != NULL);
    EventPipeProvider *pProvider = reinterpret_cast<EventPipeProvider *>(provHandle);
    pEvent = pProvider->AddEvent(eventID, keywords, eventVersion, (EventPipeEventLevel)level, (BYTE *)pMetadata, metadataLength);
    _ASSERTE(pEvent != NULL);

    END_QCALL;

    return reinterpret_cast<INT_PTR>(pEvent);
}

void QCALLTYPE EventPipeInternal::DeleteProvider(
    INT_PTR provHandle)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    if(provHandle != NULL)
    {
        EventPipeProvider *pProvider = reinterpret_cast<EventPipeProvider*>(provHandle);
        EventPipe::DeleteProvider(pProvider);
    }

    END_QCALL;
}

void QCALLTYPE EventPipeInternal::WriteEvent(
    INT_PTR eventHandle,
    unsigned int eventID,
    void *pData,
    unsigned int length,
    LPCGUID pActivityId,
    LPCGUID pRelatedActivityId)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    _ASSERTE(eventHandle != NULL);
    EventPipeEvent *pEvent = reinterpret_cast<EventPipeEvent *>(eventHandle);
    EventPipe::WriteEvent(*pEvent, (BYTE *)pData, length, pActivityId, pRelatedActivityId);

    END_QCALL;
}

void QCALLTYPE EventPipeInternal::WriteEventData(
    INT_PTR eventHandle,
    unsigned int eventID,
    EventData **pEventData,
    unsigned int eventDataCount,
    LPCGUID pActivityId,
    LPCGUID pRelatedActivityId)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    _ASSERTE(eventHandle != NULL);
    EventPipeEvent *pEvent = reinterpret_cast<EventPipeEvent *>(eventHandle);
    EventPipe::WriteEvent(*pEvent, pEventData, eventDataCount, pActivityId, pRelatedActivityId);

    END_QCALL;
}

#endif // FEATURE_PERFTRACING
