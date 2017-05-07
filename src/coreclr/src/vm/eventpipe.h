// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_H__
#define __EVENTPIPE_H__

#ifdef FEATURE_PERFTRACING

#include "crst.h"
#include "stackwalk.h"

class EventPipeConfiguration;
class EventPipeEvent;
class EventPipeFile;
class EventPipeJsonFile;

#ifdef PROFILING_SUPPORTED
class EventPipeProfilerApi;
#endif

class MethodDesc;
class SampleProfilerEventInstance;

class StackContents
{
private:

    const static unsigned int MAX_STACK_DEPTH = 100;

    // Array of IP values from a stack crawl.
    // Top of stack is at index 0.
    UINT_PTR m_stackFrames[MAX_STACK_DEPTH];

    // Parallel array of MethodDesc pointers.
    // Used for debug-only stack printing.
    MethodDesc* m_methods[MAX_STACK_DEPTH];

    // The next available slot in StackFrames.
    unsigned int m_nextAvailableFrame;

public:

    StackContents()
    {
        LIMITED_METHOD_CONTRACT;

        Reset();
    }

    void Reset()
    {
        LIMITED_METHOD_CONTRACT;

        m_nextAvailableFrame = 0;
    }

    bool IsEmpty()
    {
        LIMITED_METHOD_CONTRACT;

        return (m_nextAvailableFrame == 0);
    }

    unsigned int GetLength()
    {
        LIMITED_METHOD_CONTRACT;

        return m_nextAvailableFrame;
    }

    UINT_PTR GetIP(unsigned int frameIndex)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(frameIndex < MAX_STACK_DEPTH);

        if (frameIndex >= MAX_STACK_DEPTH)
        {
            return 0;
        }

        return m_stackFrames[frameIndex];
    }

    MethodDesc* GetMethod(unsigned int frameIndex)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(frameIndex < MAX_STACK_DEPTH);

        if (frameIndex >= MAX_STACK_DEPTH)
        {
            return NULL;
        }

        return m_methods[frameIndex];
    }

    void Append(UINT_PTR controlPC, MethodDesc *pMethod)
    {
        LIMITED_METHOD_CONTRACT;

        if(m_nextAvailableFrame < MAX_STACK_DEPTH)
        {
            m_stackFrames[m_nextAvailableFrame] = controlPC;
            m_methods[m_nextAvailableFrame] = pMethod;
            m_nextAvailableFrame++;
        }
    }

    BYTE* GetPointer() const
    {
        LIMITED_METHOD_CONTRACT;

        return (BYTE*)m_stackFrames;
    }

    unsigned int GetSize() const
    {
        LIMITED_METHOD_CONTRACT;

        return (m_nextAvailableFrame * sizeof(UINT_PTR));
    }
};

class EventPipe
{
    // Declare friends.
    friend class EventPipeConfiguration;
    friend class EventPipeFile;
    friend class EventPipeProvider;
    friend class SampleProfiler;

    public:

        // Initialize the event pipe.
        static void Initialize();

        // Shutdown the event pipe.
        static void Shutdown();

        // Enable tracing from the start-up path based on COMPLUS variable.
        static void EnableOnStartup();

        // Enable tracing via the event pipe.
        static void Enable();

        // Disable tracing via the event pipe.
        static void Disable();

        // Write out an event.
        // Data is written as a serialized blob matching the ETW serialization conventions.
        static void WriteEvent(EventPipeEvent &event, BYTE *pData, unsigned int length);

        // Write out a sample profile event.
        static void WriteSampleProfileEvent(SampleProfilerEventInstance &instance);
        
        // Get the managed call stack for the current thread.
        static bool WalkManagedStackForCurrentThread(StackContents &stackContents);

        // Get the managed call stack for the specified thread.
        static bool WalkManagedStackForThread(Thread *pThread, StackContents &stackContents);

    private:

        // Callback function for the stack walker.  For each frame walked, this callback is invoked.
        static StackWalkAction StackWalkCallback(CrawlFrame *pCf, StackContents *pData);

        // Get the configuration object.
        // This is called directly by the EventPipeProvider constructor to register the new provider.
        static EventPipeConfiguration* GetConfiguration();

        // Get the event pipe configuration lock.
        static CrstStatic* GetLock();

        static CrstStatic s_configCrst;
        static bool s_tracingInitialized;
        static EventPipeConfiguration *s_pConfig;
        static EventPipeFile *s_pFile;
        static EventPipeJsonFile *s_pJsonFile;

#ifdef PROFILING_SUPPORTED
        static EventPipeProfilerApi *s_pProfilerApi;
#endif

};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_H__
