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
class EventPipeJsonFile;
class EventPipeBuffer;
class EventPipeBufferManager;
class EventPipeEventSource;
class EventPipeProvider;
class MethodDesc;
class SampleProfilerEventInstance;
struct EventPipeProviderConfiguration;
class EventPipeSession;

// EVENT_FILTER_DESCRIPTOR (This type does not exist on non-Windows platforms.)
//  https://docs.microsoft.com/en-us/windows/desktop/api/evntprov/ns-evntprov-_event_filter_descriptor
//  The structure supplements the event provider, level, and keyword data that
//  determines which events are reported and traced. The structure gives the
//  event provider greater control over the selection of events for reporting
//  and tracing.
struct EventFilterDescriptor
{
    // A pointer to the filter data.
    ULONGLONG Ptr;

    // The size of the filter data, in bytes. The maximum size is 1024 bytes.
    ULONG     Size;

    // The type of filter data. The type is application-defined. An event
    // controller that knows about the provider and knows details about the
    // provider's events can use the Type field to send the provider an
    // arbitrary set of data for use as enhancements to the filtering of events.
    ULONG     Type;
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
public:
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
    EventPipeEventPayload(BYTE *pData, unsigned int length);

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
    BYTE* GetFlatData();

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

    EventData* GetEventDataArray() const
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
    MethodDesc* m_methods[MAX_STACK_DEPTH];
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
        memcpy_s(pDest->m_methods, MAX_STACK_DEPTH * sizeof(MethodDesc*), m_methods, sizeof(MethodDesc*) * m_nextAvailableFrame);
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
#endif // _DEBUG

    void Append(UINT_PTR controlPC, MethodDesc *pMethod)
    {
        LIMITED_METHOD_CONTRACT;

        if(m_nextAvailableFrame < MAX_STACK_DEPTH)
        {
            m_stackFrames[m_nextAvailableFrame] = controlPC;
#ifdef _DEBUG
            m_methods[m_nextAvailableFrame] = pMethod;
#endif
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

typedef UINT64 EventPipeSessionID;

class EventPipe
{
    // Declare friends.
    friend class EventPipeConfiguration;
    friend class EventPipeFile;
    friend class EventPipeProvider;
    friend class EventPipeBufferManager;
    friend class SampleProfiler;

    public:

        // Initialize the event pipe.
        static void Initialize();

        // Shutdown the event pipe.
        static void Shutdown();

        // Enable tracing via the event pipe.
        static EventPipeSessionID Enable(
            LPCWSTR strOutputPath,
            unsigned int circularBufferSizeInMB,
            EventPipeProviderConfiguration *pProviders,
            int numProviders,
            UINT64 multiFileTraceLengthInSeconds);

        // Disable tracing via the event pipe.
        static void Disable(EventPipeSessionID id);

        // Get the session for the specified session ID.
        static EventPipeSession* GetSession(EventPipeSessionID id);

        // Specifies whether or not the event pipe is enabled.
        static bool Enabled();

        // Create a provider.
        static EventPipeProvider* CreateProvider(const SString &providerName, EventPipeCallback pCallbackFunction = NULL, void *pCallbackData = NULL);

        // Get a provider.
        static EventPipeProvider* GetProvider(const SString &providerName);

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

        // Save the command line for the current process.
        static void SaveCommandLine(LPCWSTR pwzAssemblyPath, int argc, LPCWSTR *argv);

        // Get next event.
        static EventPipeEventInstance* GetNextEvent();

    protected:

        // The counterpart to WriteEvent which after the payload is constructed
        static void WriteEventInternal(EventPipeEvent &event, EventPipeEventPayload &payload, LPCGUID pActivityId = NULL, LPCGUID pRelatedActivityId = NULL);

    private:

        // Enable the specified EventPipe session.
        static EventPipeSessionID Enable(LPCWSTR strOutputPath, EventPipeSession *pSession);

        static void CreateFileSwitchTimer();

        static void DeleteFileSwitchTimer();

        // Performs one polling operation to determine if it is necessary to switch to a new file.
        // If the polling operation decides it is time, it will perform the switch.
        // Called directly from the timer when the timer is triggered.
        static void WINAPI SwitchToNextFileTimerCallback(PVOID parameter, BOOLEAN timerFired);

        // If event pipe has been configured to write multiple files, switch to the next file.
        static void SwitchToNextFile();

        // Generate the file path for the next trace file.
        // This is used when event pipe has been configured to create multiple trace files with a specified maximum length of time.
        static void GetNextFilePath(EventPipeSession *pSession, SString &nextTraceFilePath);

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
        static EventPipeSession *s_pSession;
        static EventPipeBufferManager *s_pBufferManager;
        static LPCWSTR s_pOutputPath;
        static unsigned long s_nextFileIndex;
        static EventPipeFile *s_pFile;
        static EventPipeEventSource *s_pEventSource;
        static LPCWSTR s_pCommandLine;
        const static DWORD FileSwitchTimerPeriodMS = 1000;
        static HANDLE s_fileSwitchTimerHandle;
        static ULONGLONG s_lastFileSwitchTime;
};

struct EventPipeProviderConfiguration
{

private:

    LPCWSTR m_pProviderName;
    UINT64 m_keywords;
    UINT32 m_loggingLevel;
    LPCWSTR m_pFilterData;

public:

    EventPipeProviderConfiguration()
    {
        LIMITED_METHOD_CONTRACT;
        m_pProviderName = NULL;
        m_keywords = NULL;
        m_loggingLevel = 0;
        m_pFilterData = NULL;
    }

    EventPipeProviderConfiguration(
        LPCWSTR pProviderName,
        UINT64 keywords,
        UINT32 loggingLevel,
        LPCWSTR pFilterData)
    {
        LIMITED_METHOD_CONTRACT;
        m_pProviderName = pProviderName;
        m_keywords = keywords;
        m_loggingLevel = loggingLevel;
        m_pFilterData = pFilterData;
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

class EventPipeInternal
{
private:

    enum class ActivityControlCode
    {
        EVENT_ACTIVITY_CONTROL_GET_ID = 1,
        EVENT_ACTIVITY_CONTROL_SET_ID = 2,
        EVENT_ACTIVITY_CONTROL_CREATE_ID = 3,
        EVENT_ACTIVITY_CONTROL_GET_SET_ID = 4,
        EVENT_ACTIVITY_CONTROL_CREATE_SET_ID = 5
    };

    struct EventPipeEventInstanceData
    {
    public:
        void *ProviderID;
        unsigned int EventID;
        unsigned int ThreadID;
        LARGE_INTEGER TimeStamp;
        GUID ActivityId;
        GUID RelatedActivityId;
        const BYTE *Payload;
        unsigned int PayloadLength;
    };

    struct EventPipeSessionInfo
    {
    public:
        FILETIME StartTimeAsUTCFileTime;
        LARGE_INTEGER StartTimeStamp;
        LARGE_INTEGER TimeStampFrequency;
    };

public:

    static UINT64 QCALLTYPE Enable(
        __in_z LPCWSTR outputFile,
        UINT32 circularBufferSizeInMB,
        INT64 profilerSamplingRateInNanoseconds,
        EventPipeProviderConfiguration *pProviders,
        INT32 numProviders,
        UINT64 multiFileTraceLengthInSeconds);

    static void QCALLTYPE Disable(UINT64 sessionID);

    static bool QCALLTYPE GetSessionInfo(UINT64 sessionID, EventPipeSessionInfo *pSessionInfo);

    static INT_PTR QCALLTYPE CreateProvider(
        __in_z LPCWSTR providerName,
        EventPipeCallback pCallbackFunc);

    static INT_PTR QCALLTYPE DefineEvent(
        INT_PTR provHandle,
        UINT32 eventID,
        __int64 keywords,
        UINT32 eventVersion,
        UINT32 level,
        void *pMetadata,
        UINT32 metadataLength);

    static INT_PTR QCALLTYPE GetProvider(
        __in_z LPCWSTR providerName);

    static void QCALLTYPE DeleteProvider(
        INT_PTR provHandle);

    static int QCALLTYPE EventActivityIdControl(
        uint controlCode,
        GUID *pActivityId);

    static void QCALLTYPE WriteEvent(
        INT_PTR eventHandle,
        UINT32 eventID,
        void *pData,
        UINT32 length,
        LPCGUID pActivityId, LPCGUID pRelatedActivityId);

    static void QCALLTYPE WriteEventData(
        INT_PTR eventHandle,
        UINT32 eventID,
        EventData *pEventData,
        UINT32 eventDataCount,
        LPCGUID pActivityId, LPCGUID pRelatedActivityId);

    static bool QCALLTYPE GetNextEvent(
        EventPipeEventInstanceData *pInstance);
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_H__
