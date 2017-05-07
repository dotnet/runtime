// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipe.h"
#include "eventpipeconfiguration.h"
#include "eventpipeevent.h"
#include "eventpipefile.h"
#include "eventpipeprovider.h"
#include "eventpipejsonfile.h"

#ifdef PROFILING_SUPPORTED
#include "eventpipeprofilerapi.h"
#endif

#include "sampleprofiler.h"

#ifdef FEATURE_PAL
#include "pal.h"
#endif // FEATURE_PAL

#ifdef FEATURE_PERFTRACING

CrstStatic EventPipe::s_configCrst;
bool EventPipe::s_tracingInitialized = false;
EventPipeConfiguration* EventPipe::s_pConfig = NULL;
EventPipeFile* EventPipe::s_pFile = NULL;
EventPipeJsonFile* EventPipe::s_pJsonFile = NULL;

#ifdef PROFILING_SUPPORTED
EventPipeProfilerApi* EventPipe::s_pProfilerApi = NULL;
#endif

#ifdef FEATURE_PAL
// This function is auto-generated from /src/scripts/genEventPipe.py
extern "C" void InitProvidersAndEvents();
#endif

void EventPipe::Initialize()
{
    STANDARD_VM_CONTRACT;

    s_tracingInitialized = s_configCrst.InitNoThrow(
        CrstEventPipe,
        (CrstFlags)(CRST_REENTRANCY | CRST_TAKEN_DURING_SHUTDOWN));

    s_pConfig = new EventPipeConfiguration();
    s_pConfig->Initialize();

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
    if(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PerformanceTracing) != 0)
    {
        Enable();
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
}

void EventPipe::Enable()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if(!s_tracingInitialized)
    {
        return;
    }

    // Take the lock before enabling tracing.
    CrstHolder _crst(GetLock());

    // Create the event pipe file.
    SString eventPipeFileOutputPath;
    eventPipeFileOutputPath.Printf("Process-%d.netperf", GetCurrentProcessId());
    s_pFile = new EventPipeFile(eventPipeFileOutputPath);

#ifdef PROFILING_SUPPORTED
    s_pProfilerApi = new EventPipeProfilerApi();
#endif

    if(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PerformanceTracing) == 2)
    {
        // File placed in current working directory.
        SString outputFilePath;
        outputFilePath.Printf("Process-%d.PerfView.json", GetCurrentProcessId());
        s_pJsonFile = new EventPipeJsonFile(outputFilePath);
    }

    // Enable tracing.
    s_pConfig->Enable();

    // Enable the sample profiler
    SampleProfiler::Enable();

    // TODO: Iterate through the set of providers, enable them as appropriate.
    // This in-turn will iterate through all of the events and set their isEnabled bits.
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

    // Disable the profiler.
    SampleProfiler::Disable();

    // Disable tracing.
    s_pConfig->Disable();

    if(s_pJsonFile != NULL)
    {
        delete(s_pJsonFile);
        s_pJsonFile = NULL;
    }

    if(s_pFile != NULL)
    {
        delete(s_pFile);
        s_pFile = NULL;
    }

    if(s_pConfig != NULL)
    {
        delete(s_pConfig);
        s_pConfig = NULL;
    }
}

void EventPipe::WriteEvent(EventPipeEvent &event, BYTE *pData, unsigned int length)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Exit early if the event is not enabled.
    if(!event.IsEnabled())
    {
        return;
    }

    DWORD threadID = GetCurrentThreadId();

    // Create an instance of the event.
    EventPipeEventInstance instance(
        event,
        threadID,
        pData,
        length);

    // Write to the EventPipeFile.
    _ASSERTE(s_pFile != NULL);
    s_pFile->WriteEvent(instance);

#ifdef PROFILING_SUPPORTED
    // Write to the EventPipeProfilerApi.
    _ASSERTE(s_pProfilerApi != NULL);
    s_pProfilerApi->WriteEvent(instance);
#endif

    // Write to the EventPipeJsonFile if it exists.
    if(s_pJsonFile != NULL)
    {
        s_pJsonFile->WriteEvent(instance);
    }
}

void EventPipe::WriteSampleProfileEvent(SampleProfilerEventInstance &instance)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // Write to the EventPipeFile.
    if(s_pFile != NULL)
    {
        s_pFile->WriteEvent(instance);
    }

#ifdef PROFILING_SUPPORTED
    // Write to the EventPipeProfilerApi.
    if(s_pProfilerApi != NULL)
    {
        s_pProfilerApi->WriteEvent(instance);
    }
#endif

    // Write to the EventPipeJsonFile if it exists.
    if(s_pJsonFile != NULL)
    {
        s_pJsonFile->WriteEvent(instance);
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

#endif // FEATURE_PERFTRACING
