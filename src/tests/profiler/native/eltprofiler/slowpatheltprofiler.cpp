// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define NOMINMAX

#include "slowpatheltprofiler.h"
#include <iostream>
#include <cctype>
#include <iomanip>
#include <algorithm>

using std::shared_ptr;
using std::vector;
using std::wcout;
using std::endl;

shared_ptr<SlowPathELTProfiler> SlowPathELTProfiler::s_profiler;

#ifndef WIN32
#define UINT_PTR_FORMAT "lx"
#define PROFILER_STUB EXTERN_C __attribute__((visibility("hidden"))) void STDMETHODCALLTYPE
#else // WIN32
#define UINT_PTR_FORMAT "llx"
#define PROFILER_STUB EXTERN_C void STDMETHODCALLTYPE
#endif // WIN32

PROFILER_STUB EnterStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    SlowPathELTProfiler::s_profiler->EnterCallback(functionId, eltInfo);
}

PROFILER_STUB LeaveStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    SlowPathELTProfiler::s_profiler->LeaveCallback(functionId, eltInfo);
}

PROFILER_STUB TailcallStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    SlowPathELTProfiler::s_profiler->TailcallCallback(functionId, eltInfo);
}

GUID SlowPathELTProfiler::GetClsid()
{
    // {0B36296B-EC47-44DA-8320-DC5E3071DD06}
    GUID clsid = { 0x0B36296B, 0xEC47, 0x44DA, { 0x83, 0x20, 0xDC, 0x5E, 0x30, 0x71, 0xDD, 0x06 } };
    return clsid;
}

HRESULT SlowPathELTProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    Profiler::Initialize(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;
    constexpr ULONG bufferSize = 1024;
    ULONG envVarLen = 0;
    WCHAR envVar[bufferSize];
    if (FAILED(hr = pCorProfilerInfo->GetEnvironmentVariable(WCHAR("Profiler_Test_Name"),
                                                             bufferSize,
                                                             &envVarLen,
                                                             envVar)))
    {
        wcout << L"Failed to get test name hr=" << std::hex << hr << endl;
        _failures++;
        return hr;
    }

    size_t nullCharPos = std::min(bufferSize - 1, envVarLen);
    envVar[nullCharPos] = 0;
    if (wcscmp(envVar, WCHAR("ELTSlowPathEnter")) == 0)
    {
        wcout << L"Testing enter hooks" << endl;
        _testType = TestType::EnterHooks;
    }
    else if (wcscmp(envVar, WCHAR("ELTSlowPathLeave")) == 0)
    {
        wcout << L"Testing leave hooks" << endl;
        _testType = TestType::LeaveHooks;
    }
    else
    {
        wcout << L"Unknown test type" << endl;
        _failures++;
        return E_FAIL;
    }

    SlowPathELTProfiler::s_profiler = shared_ptr<SlowPathELTProfiler>(this);

    if (FAILED(hr = pCorProfilerInfo->SetEventMask2(COR_PRF_MONITOR_ENTERLEAVE 
                                                    | COR_PRF_ENABLE_FUNCTION_ARGS
                                                    | COR_PRF_ENABLE_FUNCTION_RETVAL
                                                    | COR_PRF_ENABLE_FRAME_INFO, 
                                                    0)))
    {
        wcout << L"FAIL: IpCorProfilerInfo::SetEventMask2() failed hr=0x" << std::hex << hr << endl;
        _failures++;
        return hr;
    }

    hr = this->pCorProfilerInfo->SetEnterLeaveFunctionHooks3WithInfo(EnterStub, LeaveStub, TailcallStub);
    if (hr != S_OK)
    {
        wcout << L"SetEnterLeaveFunctionHooks3WithInfo failed with hr=0x" << std::hex << hr << endl;
        _failures++;
        return hr;
    }

    return S_OK;
}

HRESULT SlowPathELTProfiler::Shutdown()
{
    Profiler::Shutdown();

    if (_testType == TestType::EnterHooks)
    {
        if (_failures == 0 
             && _testType == TestType::EnterHooks
             && _sawSimpleFuncEnter
             && _sawMixedStructFuncEnter
             && _sawLargeStructFuncEnter)
        {
            wcout << L"PROFILER TEST PASSES" << endl;
        }
        else
        {
            wcout << L"TEST FAILED _failures=" << _failures.load() << L", _sawSimpleFuncEnter=" << _sawSimpleFuncEnter 
                    << L", _sawMixedStructFuncEnter=" << _sawMixedStructFuncEnter << L", _sawLargeStructFuncEnter=" 
                    << _sawLargeStructFuncEnter << endl;
        }    
    }
    else if (_testType == TestType::LeaveHooks)
    {
        if (_failures == 0
              && _testType == TestType::LeaveHooks
              && _sawSimpleFuncLeave
              && _sawMixedStructFuncLeave
              && _sawLargeStructFuncLeave
              && _sawIntegerStructFuncLeave
              && _sawFloatingPointStructFuncLeave
              && _sawDoubleRetFuncLeave)
        {
            wcout << L"PROFILER TEST PASSES" << endl;
        }
        else
        {
            wcout << L"TEST FAILED _failures=" << _failures.load() << L", _sawSimpleFuncLeave=" << _sawSimpleFuncLeave 
                    << L", _sawMixedStructFuncLeave=" << _sawMixedStructFuncLeave << L", _sawLargeStructFuncLeave=" 
                    << _sawLargeStructFuncLeave << L"_sawIntegerStructFuncLeave=" << _sawIntegerStructFuncLeave 
                    << L"_sawFloatingPointStructFuncLeave=" << _sawFloatingPointStructFuncLeave 
                    << L"_sawDoubleRetFuncLeave=" << _sawDoubleRetFuncLeave << endl;
        }
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE SlowPathELTProfiler::EnterCallback(FunctionIDOrClientID functionIdOrClientID, COR_PRF_ELT_INFO eltInfo)
{
    if (_testType != TestType::EnterHooks)
    {
        return S_OK;
    }

    COR_PRF_FRAME_INFO frameInfo;
    ULONG pcbArgumentInfo = 0;
    NewArrayHolder<BYTE> pArgumentInfoBytes;
    COR_PRF_FUNCTION_ARGUMENT_INFO *pArgumentInfo = NULL;

    HRESULT hr = pCorProfilerInfo->GetFunctionEnter3Info(functionIdOrClientID.functionID, eltInfo, &frameInfo, &pcbArgumentInfo, NULL);
    if (FAILED(hr) && hr != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
    {
        wcout << L"GetFunctionEnter3Info 1 failed with hr=0x" << std::hex << hr << endl;
        _failures++;
        return hr;
    }
    else if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
    {
        pArgumentInfoBytes = new BYTE[pcbArgumentInfo];
        pArgumentInfo = reinterpret_cast<COR_PRF_FUNCTION_ARGUMENT_INFO *>((BYTE *)pArgumentInfoBytes);
        hr = pCorProfilerInfo->GetFunctionEnter3Info(functionIdOrClientID.functionID, eltInfo, &frameInfo, &pcbArgumentInfo, pArgumentInfo);
        if(FAILED(hr))
        {
            wcout << L"GetFunctionEnter3Info 2 failed with hr=0x" << std::hex << hr << endl;
            _failures++;
            return hr;
        }
    }

    String functionName = GetFunctionIDName(functionIdOrClientID.functionID);
    if (functionName == WCHAR("SimpleArgsFunc"))
    {
        _sawSimpleFuncEnter = true;

        int x = -123;
        float f = -4.3f;
        const WCHAR *str = WCHAR("Hello, test!");

        vector<ExpectedArgValue> expectedValues = { { sizeof(int), (void *)&x, [&](UINT_PTR ptr){ return ValidateInt(ptr, x); } },
                                                    { sizeof(float), (void *)&f, [&](UINT_PTR ptr){ return ValidateFloat(ptr, f); }  },
                                                    { sizeof(UINT_PTR), (void *)str, [&](UINT_PTR ptr){ return ValidateString(ptr, str); } } };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);
    }
    else if (functionName == WCHAR("MixedStructFunc"))
    {
        _sawMixedStructFuncEnter = true;

        // On linux structs can be split with some in int registers and some in float registers
        // so a struct with interleaved ints/doubles is interesting.
        MixedStruct ss = { 1, 1.0 };
        vector<ExpectedArgValue> expectedValues = { { sizeof(MixedStruct), (void *)&ss, [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss); } } };
        
        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);
    }
    else if (functionName == WCHAR("LargeStructFunc"))
    {
        _sawLargeStructFuncEnter = true;

        LargeStruct ls = { 0, 0.0, 1, 1.0, 2, 2.0, 3, 3.0 };
        vector<ExpectedArgValue> expectedValues = { { sizeof(LargeStruct), (void *)&ls, [&](UINT_PTR ptr){ return ValidateLargeStruct(ptr, ls); } } };;

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);
    }

    return hr;
}

HRESULT STDMETHODCALLTYPE SlowPathELTProfiler::LeaveCallback(FunctionIDOrClientID functionIdOrClientID, COR_PRF_ELT_INFO eltInfo)
{
    if (_testType != TestType::LeaveHooks)
    {
        return S_OK;
    }
    
    COR_PRF_FRAME_INFO frameInfo;
    COR_PRF_FUNCTION_ARGUMENT_RANGE * pRetvalRange = new COR_PRF_FUNCTION_ARGUMENT_RANGE;
    HRESULT hr = pCorProfilerInfo->GetFunctionLeave3Info(functionIdOrClientID.functionID, eltInfo, &frameInfo, pRetvalRange);
    if (FAILED(hr))
    {
        wcout << L"GetFunctionLeave3Info failed hr=0x" << std::hex << hr << endl;
        _failures++;
        return hr;
    }

    String functionName = GetFunctionIDName(functionIdOrClientID.functionID);
    if (functionName == WCHAR("SimpleArgsFunc"))
    {
        _sawSimpleFuncLeave = true;

        const WCHAR *str = WCHAR("Hello from SimpleArgsFunc!");

        ExpectedArgValue simpleRetValue = { sizeof(UINT_PTR), (void *)str, [&](UINT_PTR ptr){ return ValidateString(ptr, str); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, simpleRetValue);
    }
    else if (functionName == WCHAR("MixedStructFunc"))
    {
        _sawMixedStructFuncLeave = true;

        MixedStruct ss = { 4, 1.0 };
        ExpectedArgValue MixedStructRetValue = { sizeof(MixedStruct), (void *)&ss, [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, MixedStructRetValue);
    }
    else if (functionName == WCHAR("LargeStructFunc"))
    {
        _sawLargeStructFuncLeave = true;

        int32_t val = 3;
        ExpectedArgValue largeStructRetValue = { sizeof(int32_t), (void *)&val, [&](UINT_PTR ptr){ return ValidateInt(ptr, val); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, largeStructRetValue);
    }
    else if (functionName == WCHAR("IntegerStructFunc"))
    {
        _sawIntegerStructFuncLeave = true;

        IntegerStruct is = { 21, 256 };
        ExpectedArgValue integerStructRetValue = { sizeof(IntegerStruct), (void *)&is, [&](UINT_PTR ptr){ return ValidateIntegerStruct(ptr, is); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, integerStructRetValue);
    }
    else if (functionName == WCHAR("FloatingPointStructFunc"))
    {
        _sawFloatingPointStructFuncLeave = true;

        FloatingPointStruct fps = { 13.0, 256.8 };
        ExpectedArgValue floatingPointStructRetValue = { sizeof(FloatingPointStruct), (void *)&fps, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fps); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, floatingPointStructRetValue);
    }
    else if (functionName == WCHAR("DoubleRetFunc"))
    {
        _sawDoubleRetFuncLeave = true;

        double d = 13.0;
        ExpectedArgValue doubleRetValue = { sizeof(double), (void *)&d, [&](UINT_PTR ptr){ return ValidateDouble(ptr, d); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, doubleRetValue);
    }

    return hr;   
}

HRESULT STDMETHODCALLTYPE SlowPathELTProfiler::TailcallCallback(FunctionIDOrClientID functionIdOrClientID, COR_PRF_ELT_INFO eltInfo)
{
    COR_PRF_FRAME_INFO frameInfo;
    HRESULT hr = pCorProfilerInfo->GetFunctionTailcall3Info(functionIdOrClientID.functionID, eltInfo, &frameInfo);
    if (FAILED(hr))
    {
        wcout << L"GetFunctionTailcall3Info failed hr=0x" << std::hex << hr << endl;
        _failures++;
        return hr;
    }

    // Tailcalls don't happen on debug builds, and there's no arguments to verify from GetFunctionTailcallinfo3

    return hr;    
}

void SlowPathELTProfiler::PrintBytes(const BYTE *bytes, size_t length)
{
    for (size_t i = 0; i < length; ++i)
    {
        wcout << std::setfill(L'0') << std::setw(2) << std::uppercase << std::hex << bytes[i];

        if (i > 1 && (i + 1) % 4 == 0)
        {
            wcout << " ";
        }
    }

    wcout << endl;
}

bool SlowPathELTProfiler::ValidateInt(UINT_PTR ptr, int expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    return *(int *)ptr == expected;
}

bool SlowPathELTProfiler::ValidateFloat(UINT_PTR ptr, float expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    return *(float *)ptr == expected;
}

bool SlowPathELTProfiler::ValidateDouble(UINT_PTR ptr, double expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    return *(double *)ptr == expected;
}

bool SlowPathELTProfiler::ValidateString(UINT_PTR ptr, const WCHAR *expected)
{
    if (ptr == NULL || *(void **)ptr == NULL)
    {
        return false;
    }

    ULONG lengthOffset = 0;
    ULONG bufferOffset = 0;
    HRESULT hr = pCorProfilerInfo->GetStringLayout2(&lengthOffset, &bufferOffset);
    if (FAILED(hr))
    {
        wcout << L"GetStringLayout2 failed hr=0x" << std::hex << hr << endl;
        _failures++;
        return hr;
    }

    UINT_PTR strReference = *((UINT_PTR *)ptr) + bufferOffset;
    WCHAR *strPtr = (WCHAR *)strReference;
    if (wcscmp(strPtr, expected) != 0)
    {
        _failures++;
        return false;
    }

    return true;
}

bool SlowPathELTProfiler::ValidateMixedStruct(UINT_PTR ptr, MixedStruct expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    MixedStruct lhs = *(MixedStruct *)ptr;
    return lhs.x == expected.x && lhs.d == expected.d;
}

bool SlowPathELTProfiler::ValidateLargeStruct(UINT_PTR ptr, LargeStruct expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    LargeStruct lhs = *(LargeStruct *)ptr;
    return lhs.x0 == expected.x0
            && lhs.x1 == expected.x1
            && lhs.x2 == expected.x2 
            && lhs.x3 == expected.x3
            && lhs.d0 == expected.d0
            && lhs.d1 == expected.d1
            && lhs.d2 == expected.d2 
            && lhs.d3 == expected.d3;
}

bool SlowPathELTProfiler::ValidateFloatingPointStruct(UINT_PTR ptr, FloatingPointStruct expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    FloatingPointStruct lhs = *(FloatingPointStruct *)ptr;
    return lhs.d1 == expected.d1 && lhs.d2 == expected.d2;
}

bool SlowPathELTProfiler::ValidateIntegerStruct(UINT_PTR ptr, IntegerStruct expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    IntegerStruct lhs = *(IntegerStruct *)ptr;
    return lhs.x == expected.x && lhs.y == expected.y;
}


HRESULT SlowPathELTProfiler::ValidateOneArgument(COR_PRF_FUNCTION_ARGUMENT_RANGE *pArgRange,
                                        String functionName,
                                        size_t argPos,
                                        ExpectedArgValue expectedValue)
{
    if (pArgRange->length != expectedValue.length)
    {
        wcout << L"Argument " << argPos << L" for function " << functionName << " expected length " << expectedValue.length 
                << L" but got length " << pArgRange->length << endl;
        _failures++;
        return E_FAIL;
    }

    if (!expectedValue.func(pArgRange->startAddress))
    {
        wcout << L"Argument " << argPos << L" for function " << functionName << L" did not match." << endl;
        _failures++;

        // Print out the bytes so you don't have to debug if something mismatches
        BYTE *expectedBytes = (BYTE *)expectedValue.value;
        wcout << L"Expected bytes: "; 
        PrintBytes(expectedBytes, expectedValue.length);

        BYTE *actualBytes = (BYTE *)pArgRange->startAddress;
        wcout << L"Actual bytes  : "; 
        PrintBytes(actualBytes, pArgRange->length);

        return E_FAIL;
    }

    return S_OK;
}

HRESULT SlowPathELTProfiler::ValidateFunctionArgs(COR_PRF_FUNCTION_ARGUMENT_INFO *pArgInfo,
                                          String functionName,
                                          vector<ExpectedArgValue> expectedArgValues)
{
    size_t expectedArgCount = expectedArgValues.size();

    if (pArgInfo->numRanges != expectedArgCount)
    {
        wcout << L"Expected " << expectedArgCount << L" args for " << functionName << L" but got " << pArgInfo->numRanges << endl;
        _failures++;
        return E_FAIL;
    }

    for (size_t i = 0; i < expectedArgCount; ++i)
    {
        ExpectedArgValue expectedValue = expectedArgValues[i];
        COR_PRF_FUNCTION_ARGUMENT_RANGE *pArgRange = &(pArgInfo->ranges[i]);

        HRESULT hr = ValidateOneArgument(pArgRange, functionName, i, expectedValue);
        if (FAILED(hr))
        {
            return hr;
        }
    }

    return S_OK;
}
