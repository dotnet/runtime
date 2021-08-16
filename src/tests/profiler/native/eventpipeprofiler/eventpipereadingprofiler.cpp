// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "eventpipereadingprofiler.h"

using std::mutex;
using std::lock_guard;

GUID EventPipeReadingProfiler::GetClsid()
{
    // 9E7F78E2-B3BE-410B-AA8D-E210E4C757A4
    GUID clsid = { 0x9E7F78E2, 0xB3BE, 0x410B, { 0xAA, 0x8D, 0xE2, 0x10, 0xE4, 0xC7, 0x57, 0xA4 } };
    return clsid;
}

HRESULT EventPipeReadingProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    printf("EventPipeReadingProfiler::Initialize\n");

    HRESULT hr = S_OK;
    if (FAILED(hr = pICorProfilerInfoUnk->QueryInterface(__uuidof(ICorProfilerInfo12), (void **)&_pCorProfilerInfo12)))
    {
        printf("FAIL: failed to QI for ICorProfilerInfo12.\n");
        _failures++;
        return hr;
    }

    if (FAILED(hr = _pCorProfilerInfo12->SetEventMask2(0, COR_PRF_HIGH_MONITOR_EVENT_PIPE)))
    {
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x\n", hr);
        _failures++;
        return hr;
    }

    COR_PRF_EVENTPIPE_PROVIDER_CONFIG providers[] = {
        { WCHAR("EventPipeTestEventSource"), 0xFFFFFFFFFFFFFFFF, 5, NULL }
    };

    hr = _pCorProfilerInfo12->EventPipeStartSession(sizeof(providers) / sizeof(providers[0]),
                                                    providers,
                                                    false,
                                                    &_session);
    if (FAILED(hr))
    {
        printf("Failed to start event pipe session with hr=0x%x\n", hr);
        _failures++;
        return hr;
    }

    printf("Started event pipe session!\n");

    return S_OK;
}

HRESULT EventPipeReadingProfiler::Shutdown()
{
    Profiler::Shutdown();

    if(_failures == 0 && _events.load() == 3)
    {
        printf("PROFILER TEST PASSES\n");
    }
    else
    {
        // failures were printed earlier when _failures was incremented
        printf("EventPipe profiler test failed failures=%d events=%d.\n", _failures.load(), _events.load());
    }
    fflush(stdout);

    return S_OK;
}

HRESULT EventPipeReadingProfiler::EventPipeEventDelivered(
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
    UINT_PTR stackFrames[])
{
    SHUTDOWNGUARD();

    String name = GetOrAddProviderName(provider);
    wprintf(L"EventPipeReadingProfiler saw event %s\n", name.ToCStr());

    EventPipeMetadataInstance metadata = GetOrAddMetadata(metadataBlob, cbMetadataBlob);

    if (metadata.name == WCHAR("MyEvent")
        && ValidateMyEvent(metadata, provider, eventId, eventVersion, cbMetadataBlob, metadataBlob, cbEventData, eventData, pActivityId, pRelatedActivityId, eventThread, numStackFrames, stackFrames))
    {
        _events++;
    }
    else if (metadata.name == WCHAR("MyArrayEvent")
             && ValidateMyArrayEvent(metadata, provider, eventId, eventVersion, cbMetadataBlob, metadataBlob, cbEventData, eventData, pActivityId, pRelatedActivityId, eventThread, numStackFrames, stackFrames))
    {
        _events++;
    }
    else if (metadata.name == WCHAR("KeyValueEvent")
             && ValidateKeyValueEvent(metadata, provider, eventId, eventVersion, cbMetadataBlob, metadataBlob, cbEventData, eventData, pActivityId, pRelatedActivityId, eventThread, numStackFrames, stackFrames))
    {
        _events++;
    }

    return S_OK;
}

HRESULT EventPipeReadingProfiler::EventPipeProviderCreated(EVENTPIPE_PROVIDER provider)
{
    SHUTDOWNGUARD();

    String name = GetOrAddProviderName(provider);
    wprintf(L"CorProfiler::EventPipeProviderCreated provider=%s\n", name.ToCStr());

    return S_OK;
}

String EventPipeReadingProfiler::GetOrAddProviderName(EVENTPIPE_PROVIDER provider)
{
    lock_guard<mutex> guard(_cacheLock);

    auto it = _providerNameCache.find(provider);
    if (it == _providerNameCache.end())
    {
        WCHAR nameBuffer[LONG_LENGTH];
        ULONG nameCount;
        HRESULT hr = _pCorProfilerInfo12->EventPipeGetProviderInfo(provider,
                                                                   LONG_LENGTH,
                                                                   &nameCount,
                                                                   nameBuffer);
        if (FAILED(hr))
        {
            printf("EventPipeGetProviderInfo failed with hr=0x%x\n", hr);
            return WCHAR("GetProviderInfo failed");
        }

        _providerNameCache.insert({provider, String(nameBuffer)});

        it = _providerNameCache.find(provider);
        assert(it != _providerNameCache.end());
    }

    return it->second;
}

EventPipeMetadataInstance EventPipeReadingProfiler::GetOrAddMetadata(LPCBYTE pMetadata, ULONG cbMetadata)
{
    lock_guard<mutex> guard(_cacheLock);

    auto it = _metadataCache.find(pMetadata);
    if (it == _metadataCache.end())
    {
        EventPipeMetadataReader reader;
        EventPipeMetadataInstance parsedMetadata = reader.Parse(pMetadata, cbMetadata);
        _metadataCache.insert({pMetadata, parsedMetadata});

        it = _metadataCache.find(pMetadata);
        assert(it != _metadataCache.end());
    }

    return it->second;
}

bool EventPipeReadingProfiler::ValidateMyEvent(
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
    UINT_PTR stackFrames[])
{
    if (metadata.parameters.size() != 1)
    {
        printf("MyEvent expected param size 1, saw %d\n", (int)metadata.parameters.size());
        _failures++;
        return false;
    }

    EventPipeDataDescriptor param = metadata.parameters[0];
    if (param.name != WCHAR("i")
        || param.type != EventPipeTypeCode::Int32)
    {
        wprintf(L"MyEvent expected param name=i type=Int32, saw name=%s type=%d\n",
            param.name.ToCStr(), param.type);
        _failures++;
        return false;
    }

    ULONG offset = 0;
    INT32 data = ReadFromBuffer<INT32>(eventData, cbEventData, &offset);
    if (data != 12)
    {
        printf("MyEvent expected data=12, saw %d\n", data);
        _failures++;
        return false;
    }

    return true;
}

bool EventPipeReadingProfiler::ValidateMyArrayEvent(
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
    UINT_PTR stackFrames[])
{
    if (metadata.parameters.size() != 3)
    {
        printf("MyArrayEvent expected param size 3, saw %d\n", (int)metadata.parameters.size());
        _failures++;
        return false;
    }

    EventPipeDataDescriptor param0 = metadata.parameters[0];
    if (param0.name != WCHAR("ch")
        || param0.type != EventPipeTypeCode::Char)
    {
        wprintf(L"MyArrayEvent expected param 0 name=ch type=Char, saw name=%s type=%d\n", 
            param0.name.ToCStr(), param0.type);
        _failures++;
        return false;
    }

    ULONG offset = 0;
    WCHAR ch = ReadFromBuffer<WCHAR>(eventData, cbEventData, &offset);
    if (ch != 'd')
    {
        printf("MyArrayEvent expected param 0 value=d, saw %c\n", 
            ch);
        _failures++;
        return false;
    }

    EventPipeDataDescriptor param1 = metadata.parameters[1];
    if (param1.name != WCHAR("intArray")
        || param1.type != EventPipeTypeCode::ArrayType
        || param1.elementType->type != EventPipeTypeCode::Int32)
    {
        wprintf(L"MyArrayEvent expected param 1 name=intArray type=Int32, saw name=%s type=%d\n", 
            param1.name.ToCStr(), param1.elementType->type);
        _failures++;
        return false;
    }

    UINT16 arrayLength = ReadFromBuffer<UINT16>(eventData, cbEventData, &offset);
    if (arrayLength != 120)
    {
        printf("MyArrayEvent expected array length 120, saw %d\n", arrayLength);
        _failures++;
        return false;
    }

    for (int i = 0; i < arrayLength; ++i)
    {
        INT32 data = ReadFromBuffer<INT32>(eventData, cbEventData, &offset);
        if (data != i)
        {
            printf("MyArrayEvent expected array index %d value %d, saw %d\n", i, i, data);
            _failures++;
            return false;
        }
    }

    EventPipeDataDescriptor param2 = metadata.parameters[2];
    if (param2.name != WCHAR("str")
        || param2.type != EventPipeTypeCode::String)
    {
        wprintf(L"MyArrayEvent expected param 2 name=str type=String, saw name=%s type=%d\n", 
            param2.name.ToCStr(), param2.type);
        _failures++;
        return false;
    }

    WCHAR *stringValue = ReadFromBuffer<WCHAR *>(eventData, cbEventData, &offset);
    if (String(WCHAR("Hello from EventPipeTestEventSource!")) != stringValue)
    {
        wprintf(L"MyArrayEvent expected param2 value=\"Hello from EventPipeTestEventSource!\", saw %s\n",
            stringValue);
        _failures++;
        return false;
    }

    return true;
}

bool EventPipeReadingProfiler::ValidateKeyValueEvent(
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
    UINT_PTR stackFrames[])
{
    if (metadata.parameters.size() != 3)
    {
        printf("KeyValueEvent expected param size 3, saw %d\n", (int)metadata.parameters.size());
        _failures++;
        return false;
    }

    EventPipeDataDescriptor param0 = metadata.parameters[0];
    if (param0.name != WCHAR("SourceName")
        || param0.type != EventPipeTypeCode::String)
    {
        wprintf(L"KeyValueEvent expected param 0 name=SourceName type=String, saw name=%s type=%d\n", 
            param0.name.ToCStr(), param0.type);
        _failures++;
        return false;
    }

    ULONG offset = 0;
    WCHAR *str = ReadFromBuffer<WCHAR *>(eventData, cbEventData, &offset);
    if (String(WCHAR("Source")) != str)
    {
        wprintf(L"MyArrayEvent expected param 0 value=\"Source\", saw %s\n", 
            str);
        _failures++;
        return false;
    }

    EventPipeDataDescriptor param1 = metadata.parameters[1];
    if (param1.name != WCHAR("EventName")
        || param1.type != EventPipeTypeCode::String)
    {
        wprintf(L"KeyValueEvent expected param 1 name=EventName type=String, saw name=%s type=%d\n", 
            param1.name.ToCStr(), param1.type);
        _failures++;
        return false;
    }

    WCHAR *event = ReadFromBuffer<WCHAR *>(eventData, cbEventData, &offset);
    if (String(WCHAR("Event")) != event)
    {
        wprintf(L"MyArrayEvent expected param 1 value=\"Event\", saw %s\n", 
            event);
        _failures++;
        return false;
    }

    EventPipeDataDescriptor param2 = metadata.parameters[2];
    if (param2.name != WCHAR("Arguments")
        || param2.type != EventPipeTypeCode::ArrayType
        || param2.elementType->type != EventPipeTypeCode::Object)
    {
        wprintf(L"KeyValueEvent expected param 2 name=Arguments type=String, saw name=%s type=%d\n", 
            param2.name.ToCStr(), param2.elementType->type);
        _failures++;
        return false;
    }

    UINT16 arrayLength = ReadFromBuffer<UINT16>(eventData, cbEventData, &offset);
    if (arrayLength != 1)
    {
        printf("MyArrayEvent expected array length 1, saw %d\n", arrayLength);
        _failures++;
        return false;
    }

    str = ReadFromBuffer<WCHAR *>(eventData, cbEventData, &offset);
    if (String(WCHAR("samplekey")) != str)
    {
        wprintf(L"MyArrayEvent expected param 2 value=\"samplekey\", saw %s\n", 
            str);
        _failures++;
        return false;
    }

    str = ReadFromBuffer<WCHAR *>(eventData, cbEventData, &offset);
    if (String(WCHAR("samplevalue")) != str)
    {
        wprintf(L"MyArrayEvent expected param 2 value=\"samplevalue\", saw %s\n", 
            str);
        _failures++;
        return false;
    }

    return true;
}
