// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "../profiler.h"
#include <mutex>
#include <vector>
#include <condition_variable>
#include <map>
#include "eventpipemetadatareader.h"

class EventPipeReadingProfiler : public Profiler
{
public:
    EventPipeReadingProfiler() : Profiler(),
        _failures(0),
        _events(0),
        _session(),
        _cacheLock(),
        _providerNameCache(),
        _metadataCache()
    {}

    static GUID GetClsid();

    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);

    virtual HRESULT STDMETHODCALLTYPE Shutdown();

    virtual HRESULT STDMETHODCALLTYPE EventPipeEventDelivered(EVENTPIPE_PROVIDER provider,
                                                              DWORD eventId,
                                                              DWORD eventVersion,
                                                              ULONG cbMetadataBlob,
                                                              LPCBYTE metadataBlob,
                                                              ULONG cbEventData,
                                                              LPCBYTE eventData,
                                                              LPCGUID pActivityId,
                                                              LPCGUID pRelatedActivityId,
                                                              ThreadID eventThread,
                                                              ULONG numStackFrames,
                                                              UINT_PTR stackFrames[]);

    virtual HRESULT STDMETHODCALLTYPE EventPipeProviderCreated(EVENTPIPE_PROVIDER provider);

private:
    std::atomic<int> _failures;
    std::atomic<int> _events;
    ICorProfilerInfo12 *_pCorProfilerInfo12;
    EVENTPIPE_SESSION _session;
    std::mutex _cacheLock;
    std::map<EVENTPIPE_PROVIDER, String> _providerNameCache;
    std::map<LPCBYTE, EventPipeMetadataInstance> _metadataCache;

    String GetOrAddProviderName(EVENTPIPE_PROVIDER provider);

    EventPipeMetadataInstance GetOrAddMetadata(LPCBYTE pMetadata, ULONG cbMetadata);

    bool ValidateMyEvent(
        EventPipeMetadataInstance metadata,
        EVENTPIPE_PROVIDER provider,
        DWORD eventId,
        DWORD eventVersion,
        ULONG cbMetadataBlob,
        LPCBYTE metadataBlob,
        ULONG cbEventData,
        LPCBYTE eventData,
        LPCGUID pActivityId,
        LPCGUID pRelatedActivityId,
        ThreadID eventThread,
        ULONG numStackFrames,
        UINT_PTR stackFrames[]);

    bool ValidateMyArrayEvent(
        EventPipeMetadataInstance metadata,
        EVENTPIPE_PROVIDER provider,
        DWORD eventId,
        DWORD eventVersion,
        ULONG cbMetadataBlob,
        LPCBYTE metadataBlob,
        ULONG cbEventData,
        LPCBYTE eventData,
        LPCGUID pActivityId,
        LPCGUID pRelatedActivityId,
        ThreadID eventThread,
        ULONG numStackFrames,
        UINT_PTR stackFrames[]);

    bool ValidateKeyValueEvent(
        EventPipeMetadataInstance metadata,
        EVENTPIPE_PROVIDER provider,
        DWORD eventId,
        DWORD eventVersion,
        ULONG cbMetadataBlob,
        LPCBYTE metadataBlob,
        ULONG cbEventData,
        LPCBYTE eventData,
        LPCGUID pActivityId,
        LPCGUID pRelatedActivityId,
        ThreadID eventThread,
        ULONG numStackFrames,
        UINT_PTR stackFrames[]);
};
