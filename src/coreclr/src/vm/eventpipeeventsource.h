// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __EVENTPIPE_EVENTSOURCE_H__
#define __EVENTPIPE_EVENTSOURCE_H__

#ifdef FEATURE_PERFTRACING

class EventPipeProvider;
class EventPipeEvent;
class EventPipeSession;

class EventPipeEventSource
{
private:
    const static WCHAR* s_pProviderName;
    EventPipeProvider *m_pProvider;

    const static WCHAR* s_pProcessInfoEventName;
    EventPipeEvent *m_pProcessInfoEvent;

public:
    EventPipeEventSource();
    ~EventPipeEventSource();

    void Enable(EventPipeSession *pSession);
    void SendProcessInfo(LPCWSTR pCommandLine);

    const static WCHAR* s_pOSInformation;
    const static WCHAR* s_pArchInformation;
};

#endif // FEATURE_PERFTRACING

#endif // __EVENTPIPE_EVENTSOURCE_H__
