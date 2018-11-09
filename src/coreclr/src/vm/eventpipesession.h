// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __EVENTPIPE_SESSION_H__
#define __EVENTPIPE_SESSION_H__

#ifdef FEATURE_PERFTRACING

enum class EventPipeEventLevel;
struct EventPipeProviderConfiguration;
class EventPipeSessionProviderList;
class EventPipeSessionProvider;

enum class EventPipeSessionType
{
    File,
    Streaming
};

class EventPipeSession
{
private:
    // The set of configurations for each provider in the session.
    EventPipeSessionProviderList *m_pProviderList;

    // The configured size of the circular buffer.
    size_t m_circularBufferSizeInBytes;

    // True if rundown is enabled.
    Volatile<bool> m_rundownEnabled;

    // The type of the session.
    // This determines behavior within the system (e.g. policies around which events to drop, etc.)
    EventPipeSessionType m_sessionType;

    // Start date and time in UTC.
    FILETIME m_sessionStartTime;

    // Start timestamp.
    LARGE_INTEGER m_sessionStartTimeStamp;

    // The maximum trace length in seconds.  Used to determine when to flush the current file and start a new one.
    UINT64 m_multiFileTraceLengthInSeconds;

public:

    // TODO: This needs to be exposed via EventPipe::CreateSession() and EventPipe::DeleteSession() to avoid memory ownership issues.
    EventPipeSession(
        EventPipeSessionType sessionType,
        unsigned int circularBufferSizeInMB,
        EventPipeProviderConfiguration *pProviders,
        unsigned int numProviders,
        UINT64 multiFileTraceLengthInSeconds);

    ~EventPipeSession();

    // Determine if the session is valid or not.  Invalid sessions can be detected before they are enabled.
    bool IsValid() const;

    // Get the session type.
    EventPipeSessionType GetSessionType() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_sessionType;
    }

    // Get the configured size of the circular buffer.
    size_t GetCircularBufferSize() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_circularBufferSizeInBytes;
    }

    // Determine if rundown is enabled.
    bool RundownEnabled() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_rundownEnabled;
    }

    // Set the rundown enabled flag.
    void SetRundownEnabled(bool value)
    {
        LIMITED_METHOD_CONTRACT;
        m_rundownEnabled = value;
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

    UINT64 GetMultiFileTraceLengthInSeconds() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_multiFileTraceLengthInSeconds;
    }

    // Add a new provider to the session.
    void AddSessionProvider(EventPipeSessionProvider *pProvider);

    // Get the session provider for the specified provider if present.
    EventPipeSessionProvider* GetSessionProvider(EventPipeProvider *pProvider);
};

class EventPipeSessionProviderList
{

private:

    // The list of providers.
    SList<SListElem<EventPipeSessionProvider*>> *m_pProviders;

    // A catch-all provider used when tracing is enabled for all events.
    EventPipeSessionProvider *m_pCatchAllProvider;

public:

    // Create a new list based on the input.
    EventPipeSessionProviderList(EventPipeProviderConfiguration *pConfigs, unsigned int numConfigs);
    ~EventPipeSessionProviderList();

    // Add a new session provider to the list.
    void AddSessionProvider(EventPipeSessionProvider *pProvider);

    // Get the session provider for the specified provider.
    // Return NULL if one doesn't exist.
    EventPipeSessionProvider* GetSessionProvider(EventPipeProvider *pProvider);

    // Returns true if the list is empty.
    bool IsEmpty() const;
};

class EventPipeSessionProvider
{
private:

    // The provider name.
    WCHAR *m_pProviderName;

    // The enabled keywords.
    UINT64 m_keywords;

    // The loging level.
    EventPipeEventLevel m_loggingLevel;

    // The filter data.
    WCHAR *m_pFilterData;

public:

    EventPipeSessionProvider(
        LPCWSTR providerName,
        UINT64 keywords,
        EventPipeEventLevel loggingLevel,
        LPCWSTR filterData);
    ~EventPipeSessionProvider();

    LPCWSTR GetProviderName() const;

    UINT64 GetKeywords() const;

    EventPipeEventLevel GetLevel() const;

    LPCWSTR GetFilterData() const;
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_SESSION_H__
