// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipe.h"
#include "eventpipejsonfile.h"
#include "sampleprofiler.h"

CrstStatic EventPipe::s_initCrst;
bool EventPipe::s_tracingInitialized = false;
bool EventPipe::s_tracingEnabled = false;
EventPipeJsonFile* EventPipe::s_pJsonFile = NULL;

void EventPipe::Initialize()
{
    STANDARD_VM_CONTRACT;

    s_tracingInitialized = s_initCrst.InitNoThrow(
        CrstEventPipe,
        (CrstFlags)(CRST_TAKEN_DURING_SHUTDOWN));
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

    // Take the lock and enable tracing.
    CrstHolder _crst(&s_initCrst);
    s_tracingEnabled = true;
    if(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PerformanceTracing) == 2)
    {
        // File placed in current working directory.
        SString outputFilePath;
        outputFilePath.Printf("Process-%d.PerfView.json", GetCurrentProcessId());
        s_pJsonFile = new EventPipeJsonFile(outputFilePath);
    }

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

    CrstHolder _crst(&s_initCrst);
    s_tracingEnabled = false;
    SampleProfiler::Disable();

    if(s_pJsonFile != NULL)
    {
        delete(s_pJsonFile);
        s_pJsonFile = NULL;
    }
}

bool EventPipe::EventEnabled(GUID& providerID, INT64 keyword)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // TODO: Implement filtering.
    return false;
}

void EventPipe::WriteEvent(GUID& providerID, INT64 eventID, BYTE *pData, size_t length, bool sampleStack)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackContents stackContents;
    bool stackWalkSucceeded;

    if(sampleStack)
    {
        stackWalkSucceeded = WalkManagedStackForCurrentThread(stackContents);
    }

    // TODO: Write the event.
}

void EventPipe::WriteSampleProfileEvent(Thread *pThread, StackContents &stackContents)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(pThread != NULL);
    }
    CONTRACTL_END;

    EX_TRY
    {
        if(s_pJsonFile != NULL)
        {
            CommonEventFields eventFields;
            QueryPerformanceCounter(&eventFields.TimeStamp);
            eventFields.ThreadID = pThread->GetOSThreadId();

            static SString message(W("THREAD_TIME"));
            s_pJsonFile->WriteEvent(eventFields, message, stackContents);
        }
    }
    EX_CATCH{} EX_END_CATCH(SwallowAllExceptions);
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
    _ASSERTE(pThread != NULL);
    return WalkManagedStackForThread(pThread, stackContents);
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
