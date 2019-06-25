#ifndef __EVENTPIPE_SESSION_PROVIDER_SESSION_H__
#define __EVENTPIPE_SESSION_PROVIDER_SESSION_H__

#ifdef FEATURE_PERFTRACING

enum class EventPipeEventLevel;
class EventPipeProvider;

class EventPipeSessionProvider
{
public:
    EventPipeSessionProvider(
        LPCWSTR providerName,
        UINT64 keywords,
        EventPipeEventLevel loggingLevel,
        LPCWSTR filterData);
    ~EventPipeSessionProvider();

    LPCWSTR GetProviderName() const
    {
        return m_pProviderName;
    }

    UINT64 GetKeywords() const
    {
        return m_keywords;
    }

    EventPipeEventLevel GetLevel() const
    {
        return m_loggingLevel;
    }

    LPCWSTR GetFilterData() const
    {
        return m_pFilterData;
    }

private:
    WCHAR *m_pProviderName;
    UINT64 m_keywords;
    EventPipeEventLevel m_loggingLevel;
    WCHAR *m_pFilterData;
};

class EventPipeSessionProviderList
{
public:
    // Create a new list based on the input.
    EventPipeSessionProviderList(
        const EventPipeProviderConfiguration *pConfigs,
        uint32_t numConfigs);
    ~EventPipeSessionProviderList();

    // Add a new session provider to the list.
    void AddSessionProvider(EventPipeSessionProvider *pProvider);

    // Get the session provider for the specified provider.
    // Return NULL if one doesn't exist.
    EventPipeSessionProvider* GetSessionProvider(const EventPipeProvider *pProvider) const;

    // Returns true if the list is empty.
    bool IsEmpty() const;

    // Clear the list of providers.
    void Clear();

    EventPipeSessionProviderList() = delete;
    EventPipeSessionProviderList(const EventPipeSessionProviderList &other) = delete;
    EventPipeSessionProviderList(EventPipeSessionProviderList &&other) = delete;
    EventPipeSessionProviderList &operator=(const EventPipeSessionProviderList &rhs) = delete;
    EventPipeSessionProviderList &&operator=(EventPipeSessionProviderList &&rhs) = delete;

private:
    SList<SListElem<EventPipeSessionProvider*>> *m_pProviders = nullptr;

    // A catch-all provider used when tracing is enabled for all events.
    EventPipeSessionProvider *m_pCatchAllProvider = nullptr;
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_SESSION_PROVIDER_SESSION_H__
