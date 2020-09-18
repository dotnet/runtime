// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __EVENTPIPE_PROVIDERCALLBACKDATA_H__
#define __EVENTPIPE_PROVIDERCALLBACKDATA_H__

#ifdef FEATURE_PERFTRACING

#include "common.h"

class EventPipeProvider;

enum class EventPipeEventLevel
{
    LogAlways,
    Critical,
    Error,
    Warning,
    Informational,
    Verbose
};

struct EventPipeProviderConfiguration
{
    friend class ProfToEEInterfaceImpl;

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

class EventPipeProviderCallbackData
{
public:
    EventPipeProviderCallbackData():
        m_pFilterData(nullptr),
        m_pCallbackFunction(nullptr),
        m_enabled(false),
        m_keywords(0),
        m_providerLevel(EventPipeEventLevel::LogAlways),
        m_pCallbackData(nullptr),
        m_pProvider(nullptr)
    {

    }

    EventPipeProviderCallbackData(LPCWSTR pFilterData,
                                  EventPipeCallback pCallbackFunction,
                                  bool enabled,
                                  INT64 keywords,
                                  EventPipeEventLevel providerLevel,
                                  void* pCallbackData,
                                  EventPipeProvider *pProvider) :
        m_pFilterData(nullptr),
        m_pCallbackFunction(pCallbackFunction),
        m_enabled(enabled),
        m_keywords(keywords),
        m_providerLevel(providerLevel),
        m_pCallbackData(pCallbackData),
        m_pProvider(pProvider)
    {
        if (pFilterData != nullptr)
        {
            // This is the only way to create an EventPipeProviderCallbackData that will copy the
            // filter data. The copying is intentional, because sessions die before callbacks happen
            // so we cannot cache a pointer to the session's filter data.
            size_t bufSize = wcslen(pFilterData) + 1;
            m_pFilterData = new WCHAR[bufSize];
            wcscpy_s(m_pFilterData, bufSize, pFilterData);
        }
    }

    EventPipeProviderCallbackData(EventPipeProviderCallbackData &&other)
        : EventPipeProviderCallbackData()
    {
        *this = std::move(other);
    }

    EventPipeProviderCallbackData &operator=(EventPipeProviderCallbackData &&other)
    {
        std::swap(m_pFilterData, other.m_pFilterData);
        m_pCallbackFunction = other.m_pCallbackFunction;
        m_enabled = other.m_enabled;
        m_keywords = other.m_keywords;
        m_providerLevel = other.m_providerLevel;
        m_pCallbackData = other.m_pCallbackData;
        m_pProvider = other.m_pProvider;

        return *this;
    }

    // We don't want to be unintentionally copying and deleting the filter data any more
    // than we have to. Moving (above) is fine, but copying should be avoided.
    EventPipeProviderCallbackData(const EventPipeProviderCallbackData &other) = delete;
    EventPipeProviderCallbackData &operator=(const EventPipeProviderCallbackData &other) = delete;

    ~EventPipeProviderCallbackData()
    {
        if (m_pFilterData != nullptr)
        {
            delete[] m_pFilterData;
            m_pFilterData = nullptr;
        }
    }

    LPCWSTR GetFilterData() const
    {
        return m_pFilterData;
    }

    EventPipeCallback GetCallbackFunction() const
    {
        return m_pCallbackFunction;
    }

    bool GetEnabled() const
    {
        return m_enabled;
    }

    INT64 GetKeywords() const
    {
        return m_keywords;
    }

    EventPipeEventLevel GetProviderLevel() const
    {
        return m_providerLevel;
    }

    void *GetCallbackData() const
    {
        return m_pCallbackData;
    }

    EventPipeProvider *GetProvider() const
    {
        return m_pProvider;
    }

private:
    WCHAR *m_pFilterData;
    EventPipeCallback m_pCallbackFunction;
    bool m_enabled;
    INT64 m_keywords;
    EventPipeEventLevel m_providerLevel;
    void* m_pCallbackData;
    EventPipeProvider *m_pProvider;
};

class EventPipeProviderCallbackDataQueue
{
public:
    void Enqueue(EventPipeProviderCallbackData *pEventPipeProviderCallbackData);
    bool TryDequeue(EventPipeProviderCallbackData* pEventPipeProviderCallbackData);

private:
    SList<SListElem<EventPipeProviderCallbackData>> list;
};

template <class T>
class EventPipeIterator
{
private:
    SList<SListElem<T>> *m_pList;
    typename SList<SListElem<T>>::Iterator m_iterator;

public:
    EventPipeIterator(SList<SListElem<T>> *pList) :
        m_pList(pList),
        m_iterator(pList->begin())
    {
        _ASSERTE(m_pList != nullptr);
    }

    bool Next(T *ppProvider)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(ppProvider != nullptr);
        }
        CONTRACTL_END;

        *ppProvider = *m_iterator;
        ++m_iterator;
        return m_iterator != m_pList->end();
    }   

};

typedef void (*EventPipeSessionSynchronousCallback)(
    EventPipeProvider *provider,
    DWORD eventId,
    DWORD eventVersion,
    ULONG cbMetadataBlob,
    LPCBYTE metadataBlob,
    ULONG cbEventData,
    LPCBYTE eventData,
    LPCGUID pActivityId,
    LPCGUID pRelatedActivityId,
    Thread *pEventThread,
    ULONG numStackFrames,
    UINT_PTR stackFrames[]);

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_PROVIDERCALLBACKDATA_H__
