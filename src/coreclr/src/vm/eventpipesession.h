// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __EVENTPIPE_SESSION_H__
#define __EVENTPIPE_SESSION_H__

#ifdef FEATURE_PERFTRACING

#include "common.h"
#include "threadsuspend.h"
#include "eventpipesessionprovider.h"

class EventPipeBufferManager;
class EventPipeEventInstance;
class EventPipeFile;
class EventPipeThread;

// TODO: Revisit the need of this enum and its usage.
enum class EventPipeSessionType
{
    File,
    Listener,
    IpcStream,
    Synchronous
};

enum class EventPipeSerializationFormat
{
    // Default format used in .Net Core 2.0-3.0 Preview 6
    // TBD - it may remain the default format .Net Core 3.0 when
    // used with private EventPipe managed API via reflection.
    // This format had limited official exposure in documented
    // end-user RTM scenarios, but it is supported by PerfView,
    // TraceEvent, and was used by AI profiler
    NetPerfV3,

    // Default format we plan to use in .Net Core 3 Preview7+
    // for most if not all scenarios
    NetTraceV4,

    Count
};

//! Encapsulates an EventPipe session information and memory management.
class EventPipeSession
{
private:
    const uint32_t m_index;

    // The set of configurations for each provider in the session.
    EventPipeSessionProviderList *const m_pProviderList;

    // Session buffer manager.
    EventPipeBufferManager * m_pBufferManager;

    // True if rundown is enabled.
    Volatile<bool> m_rundownEnabled;

    // The type of the session.
    // This determines behavior within the system (e.g. policies around which events to drop, etc.)
    const EventPipeSessionType m_SessionType;

    // For file/IPC sessions this controls the format emitted. For in-proc EventListener it is
    // irrelevant.
    EventPipeSerializationFormat m_format;

    // For determininig if a particular session needs rundown events
    const bool m_rundownRequested;

    // For synchronous sessions
    EventPipeSessionSynchronousCallback m_synchronousCallback;

    // Start date and time in UTC.
    FILETIME m_sessionStartTime;

    // Start timestamp.
    LARGE_INTEGER m_sessionStartTimeStamp;

    // Object used to flush event data (File, IPC stream, etc.).
    EventPipeFile *m_pFile;

    // Data members used when an IPC streaming thread is used.
    Volatile<BOOL> m_ipcStreamingEnabled = false;

    // When the session is of IPC type, this becomes a handle to the streaming thread.
    Thread *m_pIpcStreamingThread = nullptr;

    // Event object used to signal Disable that the IPC streaming thread is done.
    CLREvent m_threadShutdownEvent;

    void CreateIpcStreamingThread();

    static DWORD WINAPI ThreadProc(void *args);

    void SetThreadShutdownEvent();

    void DisableIpcStreamingThread();

    // Note - access to this field is NOT synchronized
    //
    // This field is currently modified in EventPipe::EnableViaEnvironmentVariables() during process startup
    // and GCToEEInterface::AnalyzeSurvivorsFinished() while the GC has already synchronized all the threads.
    // 
    // It is read in EventPipeSession::WriteEvent(). While it is possible for other preemptive threads to read
    // the field while GC is happening, it should not happen because the only gcGenAwareSession only subscribe
    // to GC events.
    // 
    // This functionality is a workaround because we couldn't safely Enable()/Disable() the session where we wanted to due to lock-leveling.
    // we expect to remove it in the future once that limitation is resolved
    // other scenarios are discouraged from using this given that we plan to make it go away
    bool m_paused;

public:
    EventPipeSession(
        uint32_t index,
        LPCWSTR strOutputPath,
        IpcStream *const pStream,
        EventPipeSessionType sessionType,
        EventPipeSerializationFormat format,
        bool rundownRequested,
        uint32_t circularBufferSizeInMB,
        const EventPipeProviderConfiguration *pProviders,
        uint32_t numProviders,
        EventPipeSessionSynchronousCallback callback = nullptr);

    ~EventPipeSession();

    /**
     * Please do not use this function, see EventPipeSession::m_paused for more information
     */
    void Pause();

    /**
     * Please do not use this function, see EventPipeSession::m_paused for more information
     */
    void Resume();

    /**
     * Please do not use this function, see EventPipeSession::m_paused for more information
     */
    bool Paused()
    {
        return this->m_paused;
    }

    uint64_t GetMask() const
    {
        LIMITED_METHOD_CONTRACT;
        return ((uint64_t)1 << m_index);
    }

    uint32_t GetIndex() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_index;
    }

    // Get the session type.
    EventPipeSessionType GetSessionType() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_SessionType;
    }

    // Get the format version used by the file/IPC serializer
    EventPipeSerializationFormat GetSerializationFormat() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_format;
    }

    // Get whether rundown was requested by the client.
    bool RundownRequested() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_rundownRequested;
    }

    // Determine if rundown is enabled.
    bool RundownEnabled() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_rundownEnabled;
    }

    // Get the session start time in UTC.
    FILETIME GetStartTime() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_sessionStartTime;
    }

    // Get the session start timestamp.
    LARGE_INTEGER GetStartTimeStamp() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_sessionStartTimeStamp;
    }

    bool IsIpcStreamingEnabled() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_ipcStreamingEnabled;
    }

#ifdef DEBUG
    EventPipeBufferManager* GetBufferManager() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pBufferManager;
    }
#endif

    Thread *GetIpcStreamingThread() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pIpcStreamingThread;
    }

    EventPipeSessionProviderIterator GetProviders()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pProviderList->GetProviders();
    }

    // Add a new provider to the session.
    void AddSessionProvider(EventPipeSessionProvider *pProvider);

    // Get the session provider for the specified provider if present.
    EventPipeSessionProvider* GetSessionProvider(const EventPipeProvider *pProvider) const;

    bool WriteAllBuffersToFile(bool *pEventsWritten);

    // If a session is non-synchronous (i.e. a file, pipe, etc) WriteEvent will
    // put the event in a buffer and return as quick as possible. If a session is
    // synchronous (callback to the profiler) then this method will block until the
    // profiler is done parsing and reacting to it.
    bool WriteEvent(
        Thread *pThread,
        EventPipeEvent &event,
        EventPipeEventPayload &payload,
        LPCGUID pActivityId,
        LPCGUID pRelatedActivityId,
        Thread *pEventThread = nullptr,
        StackContents *pStack = nullptr);

    // Write a sequence point into the output stream synchronously
    void WriteSequencePointUnbuffered();

    EventPipeEventInstance *GetNextEvent();

    CLREvent *GetWaitEvent();

    // Enable a session in the event pipe.
    // MUST be called AFTER sending the IPC response
    // Side effects:
    // - sends file header information for nettrace format
    // - turns on IpcStreaming thread which flushes events to stream
    void StartStreaming();

    // Disable a session in the event pipe.
    // side-effects: writes all buffers to stream/file
    void Disable();

    // Force all in-progress writes to either finish or cancel
    // This is required to ensure we can safely flush and delete the buffers
    void SuspendWriteEvent();

    void EnableRundown();
    void ExecuteRundown();

    // Determine if the session is valid or not.  Invalid sessions can be detected before they are enabled.
    bool IsValid() const;

    bool HasIpcStreamingStarted() /* This is not const because CrtsHolder does not take a const* */;
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_SESSION_H__
