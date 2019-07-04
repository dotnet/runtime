// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_H__
#define __EVENTPIPE_H__

#ifdef FEATURE_PERFTRACING
#include "common.h"
#include "eventpipecommontypes.h"
#include "stackcontents.h"

class CrstStatic;
class CrawlFrame;
class EventPipeConfiguration;
class EventPipeEvent;
class EventPipeEventInstance;
class EventPipeFile;
class EventPipeEventSource;
class EventPipeProvider;
class EventPipeSession;
class IpcStream;
enum class EventPipeSessionType;
enum class EventPipeSerializationFormat;
class EventPipeEventPayload;
struct EventData;

typedef uint64_t EventPipeSessionID;

enum class EventPipeState : uint32_t
{
    NotInitialized,
    Initialized,
    ShuttingDown,
};

class EventPipe
{
    // Declare friends.
    friend class EventPipeConfiguration;
    friend class EventPipeFile;
    friend class EventPipeProvider;

public:
    static const uint32_t MaxNumberOfSessions = 64;

    // Initialize the event pipe.
    static void Initialize();

    // Shutdown the event pipe.
    static void Shutdown();

    // Enable tracing via the event pipe.
    static EventPipeSessionID Enable(
        LPCWSTR strOutputPath,
        uint32_t circularBufferSizeInMB,
        const EventPipeProviderConfiguration *pProviders,
        uint32_t numProviders,
        EventPipeSessionType sessionType,
        EventPipeSerializationFormat format,
        const bool rundownRequested,
        IpcStream *const pStream);

    // Disable tracing via the event pipe.
    static void Disable(EventPipeSessionID id);

    // Get the session for the specified session ID.
    static EventPipeSession *GetSession(EventPipeSessionID id);

    // Specifies whether or not the event pipe is enabled.
    static bool Enabled()
    {
        LIMITED_METHOD_CONTRACT;
        return (s_state.LoadWithoutBarrier() >= EventPipeState::Initialized) && (s_numberOfSessions.LoadWithoutBarrier() > 0);
    }

    // Create a provider.
    static EventPipeProvider *CreateProvider(
        const SString &providerName,
        EventPipeCallback pCallbackFunction = nullptr,
        void *pCallbackData = nullptr);

    static EventPipeProvider *CreateProvider(const SString &providerName, EventPipeCallback pCallbackFunction, void *pCallbackData, EventPipeProviderCallbackDataQueue* pEventPipeProviderCallbackDataQueue);

    // Get a provider.
    static EventPipeProvider *GetProvider(const SString &providerName);

    // Delete a provider.
    static void DeleteProvider(EventPipeProvider *pProvider);

    // Write out an event from a flat buffer.
    // Data is written as a serialized blob matching the ETW serialization conventions.
    static void WriteEvent(EventPipeEvent &event, BYTE *pData, unsigned int length, LPCGUID pActivityId = NULL, LPCGUID pRelatedActivityId = NULL);

    // Write out an event from an EventData array.
    // Data is written as a serialized blob matching the ETW serialization conventions.
    static void WriteEvent(EventPipeEvent &event, EventData *pEventData, unsigned int eventDataCount, LPCGUID pActivityId = NULL, LPCGUID pRelatedActivityId = NULL);

    // Write out a sample profile event.
    static void WriteSampleProfileEvent(Thread *pSamplingThread, EventPipeEvent *pEvent, Thread *pTargetThread, StackContents &stackContents, BYTE *pData = NULL, unsigned int length = 0);

    // Get the managed call stack for the current thread.
    static bool WalkManagedStackForCurrentThread(StackContents &stackContents);

    // Get the managed call stack for the specified thread.
    static bool WalkManagedStackForThread(Thread *pThread, StackContents &stackContents);

    // Get next event.
    static EventPipeEventInstance *GetNextEvent(EventPipeSessionID sessionID);

#ifdef DEBUG
    static bool IsLockOwnedByCurrentThread();
#endif

    template <class T>
    static void RunWithCallbackPostponed(T f)
    {
        EventPipeProviderCallbackDataQueue eventPipeProviderCallbackDataQueue;
        EventPipeProviderCallbackData eventPipeProviderCallbackData;
        {
            CrstHolder _crst(GetLock());
            f(&eventPipeProviderCallbackDataQueue);
        }

        while (eventPipeProviderCallbackDataQueue.TryDequeue(&eventPipeProviderCallbackData))
            InvokeCallback(eventPipeProviderCallbackData);
    }

    // Returns the a number 0...N representing the processor number this thread is currently
    // running on. If for any reason we can't tell then return 0xFFFFFFFF.
    static unsigned int GetCurrentProcessorNumber()
    {
#ifndef FEATURE_PAL
        if (s_pProcGroupOffsets)
        {
            PROCESSOR_NUMBER procNum;
            GetCurrentProcessorNumberEx(&procNum);
            return s_pProcGroupOffsets[procNum.Group] + procNum.Number;
        }
#endif
        return 0xFFFFFFFF;
    }

private:
    static void InvokeCallback(EventPipeProviderCallbackData eventPipeProviderCallbackData);

    // Get the event used to write metadata to the event stream.
    static EventPipeEventInstance *BuildEventMetadataEvent(EventPipeEventInstance &instance, unsigned int metadataId);

    // The counterpart to WriteEvent which after the payload is constructed
    static void WriteEventInternal(
        EventPipeEvent &event,
        EventPipeEventPayload &payload,
        LPCGUID pActivityId = nullptr,
        LPCGUID pRelatedActivityId = nullptr);

    static void WriteEventInternal(
        Thread *pThread,
        EventPipeEvent &event,
        EventPipeEventPayload &payload,
        LPCGUID pActivityId,
        LPCGUID pRelatedActivityId,
        Thread *pEventThread = nullptr,
        StackContents *pStack = nullptr);

    static void DisableInternal(EventPipeSessionID id, EventPipeProviderCallbackDataQueue* pEventPipeProviderCallbackDataQueue);

    // Enable the specified EventPipe session.
    static bool EnableInternal(
        EventPipeSession *const pSession,
        EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue);

    // Callback function for the stack walker.  For each frame walked, this callback is invoked.
    static StackWalkAction StackWalkCallback(CrawlFrame *pCf, StackContents *pData);

    //! Helper function used to locate a free index in the range 0 - EventPipe::MaxNumberOfSessions
    //! Returns EventPipe::MaxNumberOfSessions if there are no free indexes
    static uint32_t GenerateSessionIndex();

    static bool IsSessionIdInCollection(EventPipeSessionID id);

    template <typename EventPipeSessionHandlerCallback>
    static void ForEachSession(EventPipeSessionHandlerCallback callback)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(IsLockOwnedByCurrentThread());

        for (VolatilePtr<EventPipeSession> &session : s_pSessions)
        {
            // Entering EventPipe lock gave us a barrier, we don't need
            // more of them
            EventPipeSession *const pSession = session.LoadWithoutBarrier();
            if (pSession)
                callback(*pSession);
        }
    }

    // Get the event pipe configuration lock.
    static CrstStatic *GetLock()
    {
        LIMITED_METHOD_CONTRACT;
        return &s_configCrst;
    }

    static CrstStatic s_configCrst;
    static Volatile<EventPipeState> s_state;
    static EventPipeConfiguration s_config;
    static VolatilePtr<EventPipeSession> s_pSessions[MaxNumberOfSessions];
    static Volatile<uint64_t> s_allowWrite;
    static EventPipeEventSource *s_pEventSource;

    //! Bitmask tracking EventPipe active sessions.
    // in all groups preceding it. For example if there are three groups with sizes:
    // 1, 7, 6 the table would be 0, 1, 8
#ifndef FEATURE_PAL
    static unsigned int * s_pProcGroupOffsets;
#endif
    static Volatile<uint32_t> s_numberOfSessions;
};

static_assert(EventPipe::MaxNumberOfSessions == 64, "Maximum number of EventPipe sessions is not 64.");

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_H__
