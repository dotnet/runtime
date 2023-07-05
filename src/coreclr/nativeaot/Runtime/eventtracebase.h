// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// File: eventtracebase.h
// Abstract: This module implements base Event Tracing support (excluding some of the
// CLR VM-specific ETW helpers).
//
// #EventTracing
// Windows
// ETW (Event Tracing for Windows) is a high-performance, low overhead and highly scalable
// tracing facility provided by the Windows Operating System. ETW is available on Win2K and above. There are
// four main types of components in ETW: event providers, controllers, consumers, and event trace sessions.
// An event provider is a logical entity that writes events to ETW sessions. The event provider must register
// a provider ID with ETW through the registration API. A provider first registers with ETW and writes events
// from various points in the code by invoking the ETW logging API. When a provider is enabled dynamically by
// the ETW controller application, calls to the logging API sends events to a specific trace session
// designated by the controller. Each event sent by the event provider to the trace session consists of a
// fixed header that includes event metadata and additional variable user-context data. CLR is an event
// provider.

// Mac
// DTrace is similar to ETW and has been made to look like ETW at most of the places.
// For convenience, it is called ETM (Event Tracing for Mac) and exists only on the Mac Leopard OS
// ============================================================================

#ifndef _ETWTRACER_HXX_
#define _ETWTRACER_HXX_

struct EventStructTypeData;
void InitializeEventTracing();

#ifdef FEATURE_EVENT_TRACE

// !!!!!!! NOTE !!!!!!!!
// The flags must match those in the ETW manifest exactly
// !!!!!!! NOTE !!!!!!!!

enum EtwTypeFlags
{
    kEtwTypeFlagsDelegate =                         0x1,
    kEtwTypeFlagsFinalizable =                      0x2,
    kEtwTypeFlagsExternallyImplementedCOMObject =   0x4,
    kEtwTypeFlagsArray =                            0x8,
    kEtwTypeFlagsModuleBaseAddress =                0x10,
};

enum EtwThreadFlags
{
    kEtwThreadFlagGCSpecial =         0x00000001,
    kEtwThreadFlagFinalizer =         0x00000002,
    kEtwThreadFlagThreadPoolWorker =  0x00000004,
};


// During a heap walk, this is the storage for keeping track of all the nodes and edges
// being batched up by ETW, and for remembering whether we're also supposed to call into
// a profapi profiler.  This is allocated toward the end of a GC and passed to us by the
// GC heap walker.
struct ProfilerWalkHeapContext
{
public:
    ProfilerWalkHeapContext(BOOL fProfilerPinnedParam, LPVOID pvEtwContextParam)
    {
        fProfilerPinned = fProfilerPinnedParam;
        pvEtwContext = pvEtwContextParam;
    }

    BOOL fProfilerPinned;
    LPVOID pvEtwContext;
};

class Object;

/******************************/
/* CLR ETW supported versions */
/******************************/
#define ETW_SUPPORTED_MAJORVER 5    // ETW is supported on win2k and above
#define ETW_ENABLED_MAJORVER 6      // OS versions >= to this we enable ETW registration by default, since on XP and Windows 2003, registration is too slow.

/***************************************/
/* Tracing levels supported by CLR ETW */
/***************************************/
#define ETWMAX_TRACE_LEVEL 6        // Maximum Number of Trace Levels supported
#define TRACE_LEVEL_NONE        0   // Tracing is not on
#define TRACE_LEVEL_FATAL       1   // Abnormal exit or termination
#define TRACE_LEVEL_ERROR       2   // Severe errors that need logging
#define TRACE_LEVEL_WARNING     3   // Warnings such as allocation failure
#define TRACE_LEVEL_INFORMATION 4   // Includes non-error cases such as Entry-Exit
#define TRACE_LEVEL_VERBOSE     5   // Detailed traces from intermediate steps

struct ProfilingScanContext;

//
// Use this macro to check if ETW is initialized and the event is enabled
//
#define ETW_TRACING_ENABLED(Context, EventDescriptor) \
    (Context.IsEnabled && ETW_TRACING_INITIALIZED(Context.RegistrationHandle) && ETW_EVENT_ENABLED(Context, EventDescriptor))

//
// Using KEYWORDZERO means when checking the events category ignore the keyword
//
#define KEYWORDZERO 0x0

//
// Use this macro to check if ETW is initialized and the category is enabled
//
#define ETW_TRACING_CATEGORY_ENABLED(Context, Level, Keyword) \
    (ETW_TRACING_INITIALIZED(Context.RegistrationHandle) && ETW_CATEGORY_ENABLED(Context, Level, Keyword))

#else // FEATURE_EVENT_TRACE

#include "etmdummy.h"
#endif // FEATURE_EVENT_TRACE

// These parts of the ETW namespace are common for both FEATURE_NATIVEAOT and
// !FEATURE_NATIVEAOT builds.


struct ProfilingScanContext;
struct ProfilerWalkHeapContext;
class Object;

namespace ETW
{
    // Class to wrap the logging of threads (runtime and rundown providers)
    class ThreadLog
    {
    private:
        static DWORD GetEtwThreadFlags(Thread * pThread);

    public:
        static void FireThreadCreated(Thread * pThread);
        static void FireThreadDC(Thread * pThread);
    };
};

#endif //_ETWTRACER_HXX_
