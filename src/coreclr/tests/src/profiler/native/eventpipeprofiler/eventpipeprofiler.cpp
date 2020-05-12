// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "eventpipeprofiler.h"

GUID EventPipeProfiler::GetClsid()
{
    // {2726B5B4-3F88-462D-AEC0-4EFDC8D7B921}
    GUID clsid = { 0x2726B5B4, 0x3F88, 0x462D,{ 0xAE, 0xC0, 0x4E, 0xFD, 0xC8, 0xD7, 0xB9, 0x21 } };
    return clsid;
}

HRESULT EventPipeProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    if (FAILED(hr = pCorProfilerInfo->QueryInterface(__uuidof(ICorProfilerInfo12), (void **)&_pCorProfilerInfo12)))
    {
        printf("FAIL: failed to QI for ICorProfilerInfo12.\n");
        _failures++;
        return hr;
    }

    // No event mask, just calling the EventPipe APIs.
    if (FAILED(hr = _pCorProfilerInfo12->SetEventMask2(COR_PRF_MONITOR_JIT_COMPILATION | COR_PRF_MONITOR_CACHE_SEARCHES, 0)))
    {
        _failures++;
        printf("FAIL: ICorProfilerInfo::SetEventMask2() failed hr=0x%x\n", hr);
        return hr;
    }

    if (FAILED(hr = _pCorProfilerInfo12->EventPipeCreateProvider(WCHAR("MySuperAwesomeEventPipeProvider"), &_provider)))
    {
        _failures++;
        printf("FAIL: could not create EventPipe provider hr=0x%x\n", hr);
        return hr;
    }

    // Create a param descriptor for every type
    COR_PRF_EVENTPIPE_PARAM_DESC allTypesParams[] = {
        { COR_PRF_EVENTPIPE_BOOLEAN, WCHAR("Boolean") },
        { COR_PRF_EVENTPIPE_CHAR, WCHAR("Char") },
        { COR_PRF_EVENTPIPE_SBYTE, WCHAR("SByte") },
        { COR_PRF_EVENTPIPE_BYTE, WCHAR("Byte") },
        { COR_PRF_EVENTPIPE_INT16, WCHAR("Int16") },
        { COR_PRF_EVENTPIPE_UINT16, WCHAR("UInt16") },
        { COR_PRF_EVENTPIPE_INT32, WCHAR("Int32") },
        { COR_PRF_EVENTPIPE_UINT32, WCHAR("UInt32") },
        { COR_PRF_EVENTPIPE_INT64, WCHAR("Int64") },
        { COR_PRF_EVENTPIPE_UINT64, WCHAR("UInt64") },
        { COR_PRF_EVENTPIPE_SINGLE, WCHAR("Single") },
        { COR_PRF_EVENTPIPE_DOUBLE, WCHAR("Double") },
        { COR_PRF_EVENTPIPE_GUID, WCHAR("Guid") },
        { COR_PRF_EVENTPIPE_STRING, WCHAR("String") },
        { COR_PRF_EVENTPIPE_DATETIME, WCHAR("DateTime") }
    };

    const size_t allTypesParamsCount = sizeof(allTypesParams) / sizeof(allTypesParams[0]);
    hr = _pCorProfilerInfo12->EventPipeDefineEvent(
            _provider,                      // Provider
            WCHAR("AllTypesEvent"),         // Name
            1,                              // ID
            0,                              // Keywords
            1,                              // Version
            COR_PRF_EVENTPIPE_LOGALWAYS,    // Level
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

    // EVENTPIPE_EVENT _emptyEvent;
    hr = _pCorProfilerInfo12->EventPipeDefineEvent(
        _provider,                      // Provider
        WCHAR("EmptyEvent"),            // Name
        2032,                           // ID
        0,                              // Keywords
        1,                              // Version
        COR_PRF_EVENTPIPE_INFORMATIONAL,// Level
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
        { COR_PRF_EVENTPIPE_INT32, WCHAR("Int32") }
    };

    const size_t simpleParamsCount = sizeof(simpleParams) / sizeof(simpleParams[0]);
    // EVENTPIPE_EVENT _simpleEvent;
    hr = _pCorProfilerInfo12->EventPipeDefineEvent(
            _provider,                      // Provider
            WCHAR("SimpleEvent"),           // Name
            2,                              // ID
            0,                              // Keywords
            1,                              // Version
            COR_PRF_EVENTPIPE_VERBOSE,      // Level
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

HRESULT EventPipeProfiler::Shutdown()
{
    Profiler::Shutdown();

    if(_failures == 0)
    {
        printf("PROFILER TEST PASSES\n");
    }
    else
    {
        // failures were printed earlier when _failures was incremented
        printf("EventPipe profiler test failed, check log for more info.\n");
    }
    fflush(stdout);

    return S_OK;
}

HRESULT EventPipeProfiler::JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock)
{
    return FunctionSeen(functionId);
}

HRESULT STDMETHODCALLTYPE EventPipeProfiler::JITCachedFunctionSearchFinished(FunctionID functionId, COR_PRF_JIT_CACHE result)
{
    if (result == COR_PRF_CACHED_FUNCTION_FOUND)
    {
        return FunctionSeen(functionId);
    }

    return S_OK;
}


HRESULT EventPipeProfiler::FunctionSeen(FunctionID functionId)
{
    String functionName = GetFunctionIDName(functionId);
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

        HRESULT hr = _pCorProfilerInfo12->EventPipeWriteEvent(
                        _allTypesEvent,
                        eventData,
                        sizeof(eventData)/sizeof(COR_PRF_EVENT_DATA),
                        NULL,
                        NULL);
        if (FAILED(hr))
        {
            printf("FAIL: EventPipeWriteEvent failed for AllTypesEvent with hr=0x%x\n", hr);
            _failures++;
            return hr;
        }

        for (int i= 0; i < 10; ++i)
        {
            hr = _pCorProfilerInfo12->EventPipeWriteEvent(
                        _emptyEvent,
                        NULL,
                        0,
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

            hr = _pCorProfilerInfo12->EventPipeWriteEvent(
                        _simpleEvent,
                        simpleEventData,
                        sizeof(simpleEventData) / sizeof(simpleEventData[0]),
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