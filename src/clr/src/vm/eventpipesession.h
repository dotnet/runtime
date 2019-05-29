// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_SESSION_H__
#define __EVENTPIPE_SESSION_H__

#ifdef FEATURE_PERFTRACING

#include "common.h"
#include "hosting.h"
#include "threadsuspend.h"

class EventPipeBufferManager;
class EventPipeEventInstance;
class EventPipeFile;
class EventPipeSessionProvider;
class EventPipeSessionProviderList;

// TODO: Revisit the need of this enum and its usage.
enum class EventPipeSessionType
{
    File,
    Listener,
    IpcStream
};

class EventPipeSession
{
private:

    const EventPipeSessionID m_Id;

    // The set of configurations for each provider in the session.
    EventPipeSessionProviderList *m_pProviderList;

    // The configured size of the circular buffer.
    const size_t m_CircularBufferSizeInBytes;

    // Session buffer manager.
    EventPipeBufferManager *const m_pBufferManager;

    // True if rundown is enabled.
    Volatile<bool> m_rundownEnabled;

    // The type of the session.
    // This determines behavior within the system (e.g. policies around which events to drop, etc.)
    const EventPipeSessionType m_SessionType;

    // Start date and time in UTC.
    FILETIME m_sessionStartTime;

    // Start timestamp.
    LARGE_INTEGER m_sessionStartTimeStamp;

    // Object used to flush event data (File, IPC stream, etc.).
    EventPipeFile *m_pFile;

    // Data members used when an IPC streaming thread is used.
    Volatile<BOOL> m_ipcStreamingEnabled = false;

    //
    Thread *m_pIpcStreamingThread = nullptr;

    //
    CLREvent m_threadShutdownEvent;

    //
    Thread *m_pRundownThread = nullptr;

    void CreateIpcStreamingThread();

    static DWORD WINAPI ThreadProc(void *args);

    void DestroyIpcStreamingThread();

    void SetThreadShutdownEvent();

    void DisableIpcStreamingThread();

public:
    EventPipeSession(
        EventPipeSessionID id,
        LPCWSTR strOutputPath,
        IpcStream *const pStream,
        EventPipeSessionType sessionType,
        unsigned int circularBufferSizeInMB,
        const EventPipeProviderConfiguration *pProviders,
        uint32_t numProviders,
        bool rundownEnabled = false);
    ~EventPipeSession();

    EventPipeSessionID GetId() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_Id;
    }

    // Get the session type.
    EventPipeSessionType GetSessionType() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_SessionType;
    }

    // Get the configured size of the circular buffer.
    size_t GetCircularBufferSize() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_CircularBufferSizeInBytes;
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

    bool IsRundownThread() const
    {
        LIMITED_METHOD_CONTRACT;
        return (m_pRundownThread == GetThread());
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

    // Add a new provider to the session.
    void AddSessionProvider(EventPipeSessionProvider *pProvider);

    // Get the session provider for the specified provider if present.
    EventPipeSessionProvider* GetSessionProvider(EventPipeProvider *pProvider);

    bool WriteAllBuffersToFile();

    bool WriteEvent(
        Thread *pThread,
        EventPipeEvent &event,
        EventPipeEventPayload &payload,
        LPCGUID pActivityId,
        LPCGUID pRelatedActivityId,
        Thread *pEventThread = nullptr,
        StackContents *pStack = nullptr);

    void WriteEvent(EventPipeEventInstance &instance);

    EventPipeEventInstance *GetNextEvent();

    // Enable a session in the event pipe.
    void Enable();

    // Disable a session in the event pipe.
    void Disable();

    void EnableRundown();
    void ExecuteRundown();

    // Determine if the session is valid or not.  Invalid sessions can be detected before they are enabled.
    bool IsValid() /* This is not const because CrtsHolder does not take a const* */;

    bool HasIpcStreamingStarted() /* This is not const because CrtsHolder does not take a const* */;
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_SESSION_H__
