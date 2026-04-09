// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "eventpipewritingprofiler.h"
#include <inttypes.h>

EventPipeWritingProfiler *EventPipeWritingProfiler::s_singleton = nullptr;

GUID EventPipeWritingProfiler::GetClsid()
{
    // {2726B5B4-3F88-462D-AEC0-4EFDC8D7B921}
    GUID clsid = { 0x2726B5B4, 0x3F88, 0x462D,{ 0xAE, 0xC0, 0x4E, 0xFD, 0xC8, 0xD7, 0xB9, 0x21 } };
    return clsid;
}

static void Callback(
    const UINT8 *source_id,
    UINT32 is_enabled,
    UINT8 level,
    UINT64 match_any_keywords,
    UINT64 match_all_keywords,
    COR_PRF_FILTER_DATA *filter_data,
    void *callback_data)
{
    EventPipeWritingProfiler::GetSingleton()->ProviderCallback(source_id,
                                                               is_enabled,
                                                               level,
                                                               match_any_keywords,
                                                               match_all_keywords,
                                                               filter_data,
                                                               callback_data);
}

HRESULT EventPipeWritingProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    s_singleton = this;

    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    if (FAILED(hr = pCorProfilerInfo->QueryInterface(__uuidof(ICorProfilerInfo14), (void **)&_pCorProfilerInfo)))
    {
        printf("FAIL: failed to QI for ICorProfilerInfo14.\n");
        _failures++;
        return hr;
    }

    // No event mask, just calling the EventPipe APIs.
    if (FAILED(hr = _pCorProfilerInfo->SetEventMask2(COR_PRF_MONITOR_JIT_COMPILATION | COR_PRF_MONITOR_CACHE_SEARCHES, 0)))
    {
        _failures++;
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x\n", hr);
        return hr;
    }

    if (FAILED(hr = _pCorProfilerInfo->EventPipeCreateProvider2(WCHAR("MySuperAwesomeEventPipeProvider"), Callback, &_provider)))
    {
        _failures++;
        printf("FAIL: could not create EventPipe provider hr=0x%x\n", hr);
        return hr;
    }

    // Create a param descriptor for every type
    COR_PRF_EVENTPIPE_PARAM_DESC allTypesParams[] = {
        { COR_PRF_EVENTPIPE_BOOLEAN,  0, WCHAR("Boolean") },
        { COR_PRF_EVENTPIPE_CHAR,     0, WCHAR("Char") },
        { COR_PRF_EVENTPIPE_SBYTE,    0, WCHAR("SByte") },
        { COR_PRF_EVENTPIPE_BYTE,     0, WCHAR("Byte") },
        { COR_PRF_EVENTPIPE_INT16,    0, WCHAR("Int16") },
        { COR_PRF_EVENTPIPE_UINT16,   0, WCHAR("UInt16") },
        { COR_PRF_EVENTPIPE_INT32,    0, WCHAR("Int32") },
        { COR_PRF_EVENTPIPE_UINT32,   0, WCHAR("UInt32") },
        { COR_PRF_EVENTPIPE_INT64,    0, WCHAR("Int64") },
        { COR_PRF_EVENTPIPE_UINT64,   0, WCHAR("UInt64") },
        { COR_PRF_EVENTPIPE_SINGLE,   0, WCHAR("Single") },
        { COR_PRF_EVENTPIPE_DOUBLE,   0, WCHAR("Double") },
        { COR_PRF_EVENTPIPE_GUID,     0, WCHAR("Guid") },
        { COR_PRF_EVENTPIPE_STRING,   0, WCHAR("String") },
        { COR_PRF_EVENTPIPE_DATETIME, 0, WCHAR("DateTime") }
    };

    const size_t allTypesParamsCount = sizeof(allTypesParams) / sizeof(allTypesParams[0]);
    hr = _pCorProfilerInfo->EventPipeDefineEvent(
            _provider,                      // Provider
            WCHAR("AllTypesEvent"),         // Name
            1,                              // ID
            0,                              // Keywords
            1,                              // Version
            COR_PRF_EVENTPIPE_LOGALWAYS,    // Level
            0,                              // opcode
            true,                           // Needs stack
            allTypesParamsCount,            // size of params
            allTypesParams,                 // Param descriptors
            &_allTypesEvent                 // [OUT] event ID
        );
    if (FAILED(hr))
    {
        _failures++;
        printf("FAIL: could not create EventPipe event with all types hr=0x%x\n", hr);
        return hr;
    }

    COR_PRF_EVENTPIPE_PARAM_DESC arrayTypeParams[] = {
        { COR_PRF_EVENTPIPE_ARRAY, COR_PRF_EVENTPIPE_INT32,    WCHAR("IntArray")},
    };
    const size_t arrayTypeParamsCount = sizeof(arrayTypeParams) / sizeof(arrayTypeParams[0]);
    hr = _pCorProfilerInfo->EventPipeDefineEvent(
            _provider,                       // Provider
            WCHAR("ArrayTypeEvent"),         // Name
            3,                               // ID
            0,                               // Keywords
            1,                               // Version
            COR_PRF_EVENTPIPE_LOGALWAYS,     // Level
            0,                               // opcode
            true,                            // Needs stack
            arrayTypeParamsCount,            // size of params
            arrayTypeParams,                 // Param descriptors
            &_arrayTypeEvent                 // [OUT] event ID
        );
    if (FAILED(hr))
    {
        _failures++;
        printf("FAIL: could not create array type EventPipe event with hr=0x%x\n", hr);
        return hr;
    }

    // EVENTPIPE_EVENT _emptyEvent;
    hr = _pCorProfilerInfo->EventPipeDefineEvent(
            _provider,                      // Provider
            WCHAR("EmptyEvent"),            // Name
            2032,                           // ID
            0,                              // Keywords
            1,                              // Version
            COR_PRF_EVENTPIPE_INFORMATIONAL,// Level
            0,                              // opcode
            false,                          // Needs stack
            0,                              // size of params
            NULL,                           // Param descriptors
            &_emptyEvent                    // [OUT] event ID
    );
    if (FAILED(hr))
    {
        _failures++;
        printf("FAIL: could not create EventPipe event with no types hr=0x%x\n", hr);
        return hr;
    }

    COR_PRF_EVENTPIPE_PARAM_DESC simpleParams[] = {
        { COR_PRF_EVENTPIPE_INT32, 0, WCHAR("Int32") }
    };

    const size_t simpleParamsCount = sizeof(simpleParams) / sizeof(simpleParams[0]);
    // EVENTPIPE_EVENT _simpleEvent;
    hr = _pCorProfilerInfo->EventPipeDefineEvent(
            _provider,                      // Provider
            WCHAR("SimpleEvent"),           // Name
            2,                              // ID
            0,                              // Keywords
            1,                              // Version
            COR_PRF_EVENTPIPE_VERBOSE,      // Level
            0,                              // opcode
            true,                           // Needs stack
            simpleParamsCount,              // size of params
            simpleParams,                   // Param descriptors
            &_simpleEvent                   // [OUT] event ID
    );
    if (FAILED(hr))
    {
        _failures++;
        printf("FAIL: could not create EventPipe event with simple types hr=0x%x\n", hr);
        return hr;
    }

    return S_OK;
}

HRESULT EventPipeWritingProfiler::Shutdown()
{
    Profiler::Shutdown();

    if(_failures == 0 && _enables == 2 && _disables == 2)
    {
        printf("PROFILER TEST PASSES\n");
    }
    else
    {
        // failures were printed earlier when _failures was incremented
        printf("EventPipe profiler test failed, check log for more info. _enables=%d _disables=%d\n", _enables.load(), _disables.load());
    }
    fflush(stdout);

    return S_OK;
}

HRESULT EventPipeWritingProfiler::JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock)
{
    SHUTDOWNGUARD();

    return FunctionSeen(functionId);
}

HRESULT STDMETHODCALLTYPE EventPipeWritingProfiler::JITCachedFunctionSearchFinished(FunctionID functionId, COR_PRF_JIT_CACHE result)
{
    SHUTDOWNGUARD();

    if (result == COR_PRF_CACHED_FUNCTION_FOUND)
    {
        return FunctionSeen(functionId);
    }

    return S_OK;
}

void EventPipeWritingProfiler::ProviderCallback(
    const UINT8 *source_id,
    UINT32 is_enabled,
    UINT8 level,
    UINT64 match_any_keywords,
    UINT64 match_all_keywords,
    COR_PRF_FILTER_DATA *filter_data,
    void *callback_data)
{
    // The callback contract is is_enabled will be true if any session has this provider active
    // so if is_enabled == true the way you can tell if a session is closing is by checking
    // if source_id == null
    if (is_enabled && source_id != nullptr)
    {
        ++_enables;
    }
    else
    {
        ++_disables;
    }

    // The diagnostics client library sets keywords to 0xF00000000000 by default
    if (match_any_keywords != 0xF00000000000
        || match_all_keywords != 0
        || level != 5)
    {
        _failures++;
        printf("Saw incorrect data in ProviderCallback any=%" PRIu64 ", all=%" PRIu64 ", level=%d\n",
            match_any_keywords, match_all_keywords, level);
    }
}

HRESULT EventPipeWritingProfiler::FunctionSeen(FunctionID functionID)
{
    SHUTDOWNGUARD();

    String functionName = GetFunctionIDName(functionID);
    if (functionName == WCHAR("TriggerMethod"))
    {
        printf("TriggerMethod found! Sending event...\n");

        COR_PRF_EVENT_DATA eventData[15];

        // { COR_PRF_EVENTPIPE_BOOLEAN, WCHAR("Boolean") }
        BOOL b = TRUE;
        eventData[0].ptr = reinterpret_cast<UINT64>(&b);
        eventData[0].size = sizeof(BOOL);
        // { COR_PRF_EVENTPIPE_CHAR, WCHAR("Char") }
        WCHAR ch = 'A';
        eventData[1].ptr = reinterpret_cast<UINT64>(&ch);
        eventData[1].size = sizeof(WCHAR);
        // { COR_PRF_EVENTPIPE_SBYTE, WCHAR("SByte") }
        int8_t i8t = -124;
        eventData[2].ptr = reinterpret_cast<UINT64>(&i8t);
        eventData[2].size = sizeof(int8_t);
        // { COR_PRF_EVENTPIPE_BYTE, WCHAR("Byte") }
        uint8_t ui8t = 125;
        eventData[3].ptr = reinterpret_cast<UINT64>(&ui8t);
        eventData[3].size = sizeof(uint8_t);
        // { COR_PRF_EVENTPIPE_INT16, WCHAR("Int16") }
        int16_t i16t = -35;
        eventData[4].ptr = reinterpret_cast<UINT64>(&i16t);
        eventData[4].size = sizeof(int16_t);
        // { COR_PRF_EVENTPIPE_UINT16, WCHAR("UInt16") }
        uint16_t u16t = 98;
        eventData[5].ptr = reinterpret_cast<UINT64>(&u16t);
        eventData[5].size = sizeof(uint16_t);
        // { COR_PRF_EVENTPIPE_INT32, WCHAR("Int32") }
        int32_t i32t = -560;
        eventData[6].ptr = reinterpret_cast<UINT64>(&i32t);
        eventData[6].size = sizeof(int32_t);
        // { COR_PRF_EVENTPIPE_UINT32, WCHAR("UInt32") }
        uint32_t ui32t = 561;
        eventData[7].ptr = reinterpret_cast<UINT64>(&ui32t);
        eventData[7].size = sizeof(uint32_t);
        // { COR_PRF_EVENTPIPE_INT64, WCHAR("Int64") }
        int64_t i64t = 2147483648LL;
        eventData[8].ptr = reinterpret_cast<UINT64>(&i64t);
        eventData[8].size = sizeof(int64_t);
        // { COR_PRF_EVENTPIPE_UINT64, WCHAR("UInt64") }
        uint64_t ui64t = 2147483649ULL;
        eventData[9].ptr = reinterpret_cast<UINT64>(&ui64t);
        eventData[9].size = sizeof(uint64_t);
        // { COR_PRF_EVENTPIPE_SINGLE, WCHAR("Single") }
        float f = 3.0f;
        eventData[10].ptr = reinterpret_cast<UINT64>(&f);
        eventData[10].size = sizeof(float);
        // { COR_PRF_EVENTPIPE_DOUBLE, WCHAR("Double") }
        double d = 3.023;
        eventData[11].ptr = reinterpret_cast<UINT64>(&d);
        eventData[11].size = sizeof(double);
        // { COR_PRF_EVENTPIPE_GUID, WCHAR("Guid") }
        GUID guid = { 0x176FBED1,0xA55C,0x4796, { 0x98,0xCA,0xA9,0xDA,0x0E,0xF8,0x83,0xE7 }};
        eventData[12].ptr = reinterpret_cast<UINT64>(&guid);
        eventData[12].size = sizeof(GUID);
        // { COR_PRF_EVENTPIPE_STRING, WCHAR("String") }
        LPCWCH str = WCHAR("Hello, this is a string!");
        eventData[13].ptr = reinterpret_cast<UINT64>(str);
        eventData[13].size = static_cast<UINT32>(wcslen(str) + 1 /*include null char*/) * sizeof(WCHAR);
        // { COR_PRF_EVENTPIPE_DATETIME, WCHAR("DateTime") }
        // TraceEvent uses DateTime.FromFileTime() to parse
        uint64_t dateTime = 132243707160000000ULL;
        eventData[14].ptr = reinterpret_cast<UINT64>(&dateTime);
        eventData[14].size = sizeof(uint64_t);

        HRESULT hr = _pCorProfilerInfo->EventPipeWriteEvent(
                        _allTypesEvent,
                        sizeof(eventData)/sizeof(COR_PRF_EVENT_DATA),
                        eventData,
                        NULL,
                        NULL);
        if (FAILED(hr))
        {
            printf("FAIL: EventPipeWriteEvent failed for AllTypesEvent with hr=0x%x\n", hr);
            _failures++;
            return hr;
        }

        // Array types are not supported in the version of TraceEvent we consume in the
        // tests, but we can at least make sure nothing asserts in the runtime.
        // Once TraceEvent has a new version out with the array type support
        // we can update and it will work.
        // { COR_PRF_EVENTPIPE_FLAG_ARRAY_TYPE, COR_PRF_EVENTPIPE_INT32, WCHAR("IntArray")}
        COR_PRF_EVENT_DATA arrayTypeEventData[1];
        constexpr INT32 arraySize = 2 + (100 * sizeof(INT32));
        BYTE dataSource[arraySize];
        size_t offset = 0;
        WriteToBuffer<UINT16>(dataSource, arraySize, &offset, 100);

        for (int i = 0; i < 100; ++i)
        {
            WriteToBuffer<INT32>(dataSource, arraySize, &offset, 100 - i);
        }

        arrayTypeEventData[0].ptr = reinterpret_cast<UINT64>(&dataSource[0]);
        arrayTypeEventData[0].size = arraySize;
        hr = _pCorProfilerInfo->EventPipeWriteEvent(
                        _arrayTypeEvent,
                        sizeof(arrayTypeEventData) / sizeof(arrayTypeEventData[0]),
                        arrayTypeEventData,
                        NULL,
                        NULL);
        if (FAILED(hr))
        {
            printf("FAIL: EventPipeWriteEvent failed for ArrayTypeEvent with hr=0x%x\n", hr);
            _failures++;
            return hr;
        }

        for (int i= 0; i < 10; ++i)
        {
            hr = _pCorProfilerInfo->EventPipeWriteEvent(
                        _emptyEvent,
                        0,
                        NULL,
                        NULL,
                        NULL);
            if (FAILED(hr))
            {
                printf("FAIL: EventPipeWriteEvent failed for EmptyEvent with hr=0x%x\n", hr);
                _failures++;
                return hr;
            }
        }

        for (int32_t i32 = 0; i32 < 10000; ++i32)
        {
            COR_PRF_EVENT_DATA simpleEventData[1];
            simpleEventData[0].ptr = reinterpret_cast<UINT64>(&i32);
            simpleEventData[0].size = sizeof(int32_t);

            hr = _pCorProfilerInfo->EventPipeWriteEvent(
                        _simpleEvent,
                        sizeof(simpleEventData) / sizeof(simpleEventData[0]),
                        simpleEventData,
                        NULL,
                        NULL);
            if (FAILED(hr))
            {
                printf("FAIL: EventPipeWriteEvent failed for SimpleEvent with hr=0x%x\n", hr);
                _failures++;
                return hr;
            }
        }
    }

    return S_OK;
}
