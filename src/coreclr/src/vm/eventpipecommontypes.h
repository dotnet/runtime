// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_PROVIDERCALLBACKDATA_H__
#define __EVENTPIPE_PROVIDERCALLBACKDATA_H__

#ifdef FEATURE_PERFTRACING

#include "common.h"

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

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_PROVIDERCALLBACKDATA_H__
