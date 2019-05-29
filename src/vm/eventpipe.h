// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_H__
#define __EVENTPIPE_H__

#ifdef FEATURE_PERFTRACING
#include "common.h"

class CrstStatic;
class CrawlFrame;
class EventPipeConfiguration;
class EventPipeEvent;
class EventPipeEventInstance;
class EventPipeFile;
class EventPipeBufferManager;
class EventPipeEventSource;
class EventPipeProvider;
class MethodDesc;
struct EventPipeProviderConfiguration;
class EventPipeSession;
class IpcStream;
enum class EventPipeSessionType;

enum class EventPipeEventLevel
{
    LogAlways,
    Critical,
    Error,
    Warning,
    Informational,
    Verbose
};

// EVENT_FILTER_DESCRIPTOR (This type does not exist on non-Windows platforms.)
//  https://docs.microsoft.com/en-us/windows/desktop/api/evntprov/ns-evntprov-_event_filter_descriptor
//  The structure supplements the event provider, level, and keyword data that
//  determines which events are reported and traced. The structure gives the
//  event provider greater control over the selection of events for reporting
//  and tracing.
// TODO: EventFilterDescriptor and EventData (defined below) are the same.
struct EventFilterDescriptor
{
    // A pointer to the filter data.
    ULONGLONG Ptr;

    // The size of the filter data, in bytes. The maximum size is 1024 bytes.
    ULONG Size;

    // The type of filter data. The type is application-defined. An event
    // controller that knows about the provider and knows details about the
    // provider's events can use the Type field to send the provider an
    // arbitrary set of data for use as enhancements to the filtering of events.
    ULONG Type;
};

// Define the event pipe callback to match the ETW callback signature.
typedef void (*EventPipeCallback)(
    LPCGUID SourceID,
    ULONG IsEnabled,
    UCHAR Level,
    ULONGLONG MatchAnyKeywords,
    ULONGLONG MatchAllKeywords,
    EventFilterDescriptor *FilterData,
    void *CallbackContext);

struct EventData
{
    UINT64 Ptr;
    unsigned int Size;
    unsigned int Reserved;
};

class EventPipeEventPayload
{
private:
    BYTE *m_pData;
    EventData *m_pEventData;
    unsigned int m_eventDataCount;
    unsigned int m_size;
    bool m_allocatedData;

    // If the data is stored only as an array of EventData objects, create a flat buffer and copy into it
    void Flatten();

public:
    // Build this payload with a flat buffer inside
    EventPipeEventPayload(BYTE *pData, unsigned int length) :
        m_pData(pData),
        m_pEventData(nullptr),
        m_eventDataCount(0),
        m_size(length),
        m_allocatedData(false)
    {
        LIMITED_METHOD_CONTRACT;
    }

    // Build this payload to contain an array of EventData objects
    EventPipeEventPayload(EventData *pEventData, unsigned int eventDataCount);

    // If a buffer was allocated internally, delete it
    ~EventPipeEventPayload();

    // Copy the data (whether flat or array of objects) into a flat buffer at pDst
    // Assumes that pDst points to an appropriatly sized buffer
    void CopyData(BYTE *pDst);

    // Get the flat formatted data in this payload
    // This method will allocate a buffer if it does not already contain flattened data
    // This method will return NULL on OOM if a buffer needed to be allocated
    BYTE *GetFlatData();

    // Return true is the data is stored in a flat buffer
    bool IsFlattened() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pData != NULL;
    }

    // The the size of buffer needed to contain the stored data
    unsigned int GetSize() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_size;
    }

    EventData *GetEventDataArray() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pEventData;
    }
};

class StackContents
{
private:
    const static unsigned int MAX_STACK_DEPTH = 100;

    // Array of IP values from a stack crawl.
    // Top of stack is at index 0.
    UINT_PTR m_stackFrames[MAX_STACK_DEPTH];

#ifdef _DEBUG
    // Parallel array of MethodDesc pointers.
    // Used for debug-only stack printing.
    MethodDesc *m_methods[MAX_STACK_DEPTH];
#endif // _DEBUG

    // The next available slot in StackFrames.
    unsigned int m_nextAvailableFrame;

public:
    StackContents()
    {
        LIMITED_METHOD_CONTRACT;
        Reset();
    }

    void CopyTo(StackContents *pDest)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(pDest != NULL);

        memcpy_s(pDest->m_stackFrames, MAX_STACK_DEPTH * sizeof(UINT_PTR), m_stackFrames, sizeof(UINT_PTR) * m_nextAvailableFrame);
#ifdef _DEBUG
        memcpy_s(pDest->m_methods, MAX_STACK_DEPTH * sizeof(MethodDesc *), m_methods, sizeof(MethodDesc *) * m_nextAvailableFrame);
#endif
        pDest->m_nextAvailableFrame = m_nextAvailableFrame;
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

#ifdef _DEBUG
    MethodDesc *GetMethod(unsigned int frameIndex)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(frameIndex < MAX_STACK_DEPTH);

        if (frameIndex >= MAX_STACK_DEPTH)
        {
            return NULL;
        }

        return m_methods[frameIndex];
    }
#endif // _DEBUG

    void Append(UINT_PTR controlPC, MethodDesc *pMethod)
    {
        LIMITED_METHOD_CONTRACT;

        if (m_nextAvailableFrame < MAX_STACK_DEPTH)
        {
            m_stackFrames[m_nextAvailableFrame] = controlPC;
#ifdef _DEBUG
            m_methods[m_nextAvailableFrame] = pMethod;
#endif
            m_nextAvailableFrame++;
        }
    }

    BYTE *GetPointer() const
    {
        LIMITED_METHOD_CONTRACT;
        return (BYTE *)m_stackFrames;
    }

    unsigned int GetSize() const
    {
        LIMITED_METHOD_CONTRACT;
        return (m_nextAvailableFrame * sizeof(UINT_PTR));
    }
};

typedef uint64_t EventPipeSessionID;

struct EventPipeProviderCallbackData
{
    LPCWSTR pFilterData;
    EventPipeCallback pCallbackFunction;
    bool enabled;
    INT64 keywords;
    EventPipeEventLevel providerLevel;
    void* pCallbackData;
};

class EventPipeProviderCallbackDataQueue
{
public:
    void Enqueue(EventPipeProviderCallbackData* pEventPipeProviderCallbackData);
    bool TryDequeue(EventPipeProviderCallbackData* pEventPipeProviderCallbackData);

private:
    SList<SListElem<EventPipeProviderCallbackData>> list;
};

// TODO: Maybe this could be an array: EventPipeSession *EventPipeSessions[64];
typedef MapSHashWithRemove<EventPipeSessionID, EventPipeSession *> EventPipeSessions;

class EventPipe
{
    // Declare friends.
    friend class EventPipeConfiguration;
    friend class EventPipeFile;
    friend class EventPipeProvider;

public:
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
        IpcStream *const pStream);

    // Disable tracing via the event pipe.
    static void Disable(EventPipeSessionID id);

    // Get the session for the specified session ID.
    static EventPipeSession *GetSession(EventPipeSessionID id);

    // Specifies whether or not the event pipe is enabled.
    static bool Enabled();

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

    static void InvokeCallback(EventPipeProviderCallbackData eventPipeProviderCallbackData);

    // Get the event used to write metadata to the event stream.
    static EventPipeEventInstance *BuildEventMetadataEvent(EventPipeEventInstance &instance, unsigned int metadataId);

private:
    // The counterpart to WriteEvent which after the payload is constructed
    static void WriteEventInternal(EventPipeEvent &event, EventPipeEventPayload &payload, LPCGUID pActivityId = NULL, LPCGUID pRelatedActivityId = NULL);

    static void DisableInternal(EventPipeSessionID id, EventPipeProviderCallbackDataQueue* pEventPipeProviderCallbackDataQueue);

    // Enable the specified EventPipe session.
    static EventPipeSessionID EnableInternal(
        EventPipeSession *const pSession,
        EventPipeProviderCallbackDataQueue *pEventPipeProviderCallbackDataQueue);

    // Callback function for the stack walker.  For each frame walked, this callback is invoked.
    static StackWalkAction StackWalkCallback(CrawlFrame *pCf, StackContents *pData);

    // Get the configuration object.
    // This is called directly by the EventPipeProvider constructor to register the new provider.
    static EventPipeConfiguration *GetConfiguration()
    {
        LIMITED_METHOD_CONTRACT;
        return s_pConfig;
    }

    // Get the event pipe configuration lock.
    static CrstStatic *GetLock()
    {
        LIMITED_METHOD_CONTRACT;
        return &s_configCrst;
    }

    static CrstStatic s_configCrst;
    static bool s_tracingInitialized;
    static EventPipeConfiguration *s_pConfig;
    static EventPipeSessions *s_pSessions;
    static EventPipeEventSource *s_pEventSource;
    static HANDLE s_fileSwitchTimerHandle;
    static ULONGLONG s_lastFlushTime;
};

struct EventPipeProviderConfiguration
{
private:
    LPCWSTR m_pProviderName = nullptr;
    UINT64 m_keywords = 0;
    UINT32 m_loggingLevel = 0;
    LPCWSTR m_pFilterData = nullptr;

public:
    EventPipeProviderConfiguration() = default;

    EventPipeProviderConfiguration(LPCWSTR pProviderName, UINT64 keywords, UINT32 loggingLevel, LPCWSTR pFilterData) :
        m_pProviderName(pProviderName),
        m_keywords(keywords),
        m_loggingLevel(loggingLevel),
        m_pFilterData(pFilterData)
    {
    }

    LPCWSTR GetProviderName() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pProviderName;
    }

    UINT64 GetKeywords() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_keywords;
    }

    UINT32 GetLevel() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_loggingLevel;
    }

    LPCWSTR GetFilterData() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pFilterData;
    }
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_H__
