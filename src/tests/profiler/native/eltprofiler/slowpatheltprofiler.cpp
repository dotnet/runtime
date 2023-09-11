// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define NOMINMAX

#include "slowpatheltprofiler.h"
#include <iostream>
#include <cctype>
#include <iomanip>
#include <algorithm>
#include <thread>
#include <chrono>

using std::shared_ptr;
using std::vector;
using std::wcout;
using std::endl;
using std::atomic;

shared_ptr<SlowPathELTProfiler> SlowPathELTProfiler::s_profiler;

#define PROFILER_STUB static void STDMETHODCALLTYPE

#ifndef WIN32
#define UINT_PTR_FORMAT "lx"
#else // WIN32
#define UINT_PTR_FORMAT "llx"
#endif // WIN32

PROFILER_STUB EnterStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    SHUTDOWNGUARD_RETVOID();

    SlowPathELTProfiler::s_profiler->EnterCallback(functionId, eltInfo);
}

PROFILER_STUB LeaveStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    SHUTDOWNGUARD_RETVOID();

    SlowPathELTProfiler::s_profiler->LeaveCallback(functionId, eltInfo);
}

PROFILER_STUB TailcallStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    SHUTDOWNGUARD_RETVOID();

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

    if (_testType != TestType::EnterHooks && _testType != TestType::LeaveHooks)
    {
        return S_OK;
    }
    
    bool allPass = true;
    bool isEnter = (_testType == TestType::EnterHooks);
    auto& sawFunc = isEnter ? _sawFuncEnter : _sawFuncLeave;

    for (auto p: sawFunc)
    {
        allPass = allPass && p.second;
    }

    int failures =_failures.load();
    if (failures == 0 && allPass)
    {
        wcout << L"PROFILER TEST PASSES" << endl;
    }
    else
    {
        wcout << L"TEST FAILED _failures=" << failures << endl;

        if (!allPass)
        {
            const wchar_t* label = isEnter ? L"Enter" : L"Leave";
            for (auto p: sawFunc)
            {
                if (!p.second)
                    wcout << L"_sawFunc" << label << L"[" << p.first << L"]=" << p.second << endl;
            }
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

    Fp32x2Struct fp32x2 = {1.2f, 3.5f};
    Fp32x3Struct fp32x3 = {6.7f, 10.11f, 13.14f};
    Fp32x4Struct fp32x4 = {15.17f, 19.21f, 22.23f, 26.29f};
    Fp64x2Struct fp64x2 = {1.2, 3.5};
    Fp64x3Struct fp64x3 = {6.7, 10.11, 13.14};
    Fp64x4Struct fp64x4 = {15.17, 19.21, 22.23, 26.29};

    String functionName = GetFunctionIDName(functionIdOrClientID.functionID);
    if (functionName == WCHAR("SimpleArgsFunc"))
    {
        int x = -123;
        float f = -4.3f;
        const WCHAR *str = WCHAR("Hello, test!");

        vector<ExpectedArgValue> expectedValues = { { sizeof(int), (void *)&x, [&](UINT_PTR ptr){ return ValidateInt(ptr, x); } },
                                                    { sizeof(float), (void *)&f, [&](UINT_PTR ptr){ return ValidateFloat(ptr, f); }  },
                                                    { sizeof(UINT_PTR), (void *)str, [&](UINT_PTR ptr){ return ValidateString(ptr, str); } } };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("MixedStructFunc"))
    {
        // On linux structs can be split with some in int registers and some in float registers
        // so a struct with interleaved ints/doubles is interesting.
        MixedStruct ss = { 1, 1.0 };
        vector<ExpectedArgValue> expectedValues = { { sizeof(MixedStruct), (void *)&ss, [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss); } } };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("LargeStructFunc"))
    {
        LargeStruct ls = { 0, 0.0, 1, 1.0, 2, 2.0, 3, 3.0 };
        vector<ExpectedArgValue> expectedValues = { { sizeof(LargeStruct), (void *)&ls, [&](UINT_PTR ptr){ return ValidateLargeStruct(ptr, ls); } } };;

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp32x2StructFunc"))
    {
        vector<ExpectedArgValue> expectedValues = { { sizeof(Fp32x2Struct), (void *)&fp32x2, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp32x2); } } };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp32x2StructFp32x3StructFunc"))
    {
        vector<ExpectedArgValue> expectedValues = {
            { sizeof(Fp32x2Struct), (void *)&fp32x2, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp32x2); } },
            { sizeof(Fp32x3Struct), (void *)&fp32x3, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp32x3); } }
        };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
     else if (functionName == WCHAR("Fp32x3StructFunc"))
    {
        vector<ExpectedArgValue> expectedValues = { { sizeof(Fp32x3Struct), (void *)&fp32x3, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp32x3); } } };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp32x3StructFp32x2StructFunc"))
    {
        vector<ExpectedArgValue> expectedValues = {
            { sizeof(Fp32x3Struct), (void *)&fp32x3, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp32x3); } },
            { sizeof(Fp32x2Struct), (void *)&fp32x2, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp32x2); } }
        };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp32x3StructSingleFp32x3StructSingleFunc"))
    {
        float flt1 = 1.2f;
        float flt2 = 3.5f;

        vector<ExpectedArgValue> expectedValues = {
            { sizeof(Fp32x3Struct), (void *)&fp32x3, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp32x3); } },
            { sizeof(float), (void *)&flt1, [&](UINT_PTR ptr){ return ValidateFloat(ptr, flt1); } },
            { sizeof(Fp32x3Struct), (void *)&fp32x3, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp32x3); } },
            { sizeof(float), (void *)&flt2, [&](UINT_PTR ptr){ return ValidateFloat(ptr, flt2); } }
        };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp32x4StructFunc"))
    {
        vector<ExpectedArgValue> expectedValues ={ { sizeof(Fp32x4Struct), (void *)&fp32x4, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp32x4); } } };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp32x4StructFp32x4StructFunc"))
    {
        vector<ExpectedArgValue> expectedValues = {
            { sizeof(Fp32x4Struct), (void *)&fp32x4, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp32x4); } },
            { sizeof(Fp32x4Struct), (void *)&fp32x4, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp32x4); } }
        };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp64x2StructFunc"))
    {
        vector<ExpectedArgValue> expectedValues = { { sizeof(Fp64x2Struct), (void *)&fp64x2, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp64x2); } } };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp64x2StructFp64x3StructFunc"))
    {
        vector<ExpectedArgValue> expectedValues = {
            { sizeof(Fp64x2Struct), (void *)&fp64x2, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp64x2); } },
            { sizeof(Fp64x3Struct), (void *)&fp64x3, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp64x3); } }
        };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
     else if (functionName == WCHAR("Fp64x3StructFunc"))
    {
        vector<ExpectedArgValue> expectedValues = { { sizeof(Fp64x3Struct), (void *)&fp64x3, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp64x3); } } };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp64x3StructFp64x2StructFunc"))
    {
        vector<ExpectedArgValue> expectedValues = {
            { sizeof(Fp64x3Struct), (void *)&fp64x3, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp64x3); } },
            { sizeof(Fp64x2Struct), (void *)&fp64x2, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp64x2); } }
        };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp64x3StructDoubleFp64x3StructDoubleFunc"))
    {
        double dbl1 = 1.2;
        double dbl2 = 3.5;

        vector<ExpectedArgValue> expectedValues = {
            { sizeof(Fp64x3Struct), (void *)&fp64x3, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp64x3); } },
            { sizeof(double), (void *)&dbl1, [&](UINT_PTR ptr){ return ValidateDouble(ptr, dbl1); } },
            { sizeof(Fp64x3Struct), (void *)&fp64x3, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp64x3); } },
            { sizeof(double), (void *)&dbl2, [&](UINT_PTR ptr){ return ValidateDouble(ptr, dbl2); } }
        };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp64x4StructFunc"))
    {
        vector<ExpectedArgValue> expectedValues ={ { sizeof(Fp64x4Struct), (void *)&fp64x4, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp64x4); } } };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp64x4StructFp64x4StructFunc"))
    {
        vector<ExpectedArgValue> expectedValues = {
            { sizeof(Fp64x4Struct), (void *)&fp64x4, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp64x4); } },
            { sizeof(Fp64x4Struct), (void *)&fp64x4, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp64x4); } }
        };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("IntManyMixedStructFunc"))
    {
        int i = 11;
        MixedStruct ss[] = {{1, 1.0}, {2, 2.0}, {3, 3.0}, {4, 4.0}, {5, 5.0}, {6, 6.0}, {7, 7.0}, {8, 8.0}, {9, 9.0}};
        vector<ExpectedArgValue> expectedValues = {
            { sizeof(int), (void *)&i, [&](UINT_PTR ptr){ return ValidateInt(ptr, i); } },
            { sizeof(MixedStruct), (void *)&ss[0], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[0]); } },
            { sizeof(MixedStruct), (void *)&ss[1], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[1]); } },
            { sizeof(MixedStruct), (void *)&ss[2], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[2]); } },
            { sizeof(MixedStruct), (void *)&ss[3], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[3]); } },
            { sizeof(MixedStruct), (void *)&ss[4], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[4]); } },
            { sizeof(MixedStruct), (void *)&ss[5], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[5]); } },
            { sizeof(MixedStruct), (void *)&ss[6], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[6]); } },
            { sizeof(MixedStruct), (void *)&ss[7], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[7]); } },
            { sizeof(MixedStruct), (void *)&ss[8], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[8]); } },
        };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("DoubleManyMixedStructFunc"))
    {
        double d = 11.0;
        MixedStruct ss[] = {{1, 1.0}, {2, 2.0}, {3, 3.0}, {4, 4.0}, {5, 5.0}, {6, 6.0}, {7, 7.0}, {8, 8.0}, {9, 9.0}};
        vector<ExpectedArgValue> expectedValues = {
            { sizeof(double), (void *)&d, [&](UINT_PTR ptr){ return ValidateDouble(ptr, d); } },
            { sizeof(MixedStruct), (void *)&ss[0], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[0]); } },
            { sizeof(MixedStruct), (void *)&ss[1], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[1]); } },
            { sizeof(MixedStruct), (void *)&ss[2], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[2]); } },
            { sizeof(MixedStruct), (void *)&ss[3], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[3]); } },
            { sizeof(MixedStruct), (void *)&ss[4], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[4]); } },
            { sizeof(MixedStruct), (void *)&ss[5], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[5]); } },
            { sizeof(MixedStruct), (void *)&ss[6], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[6]); } },
            { sizeof(MixedStruct), (void *)&ss[7], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[7]); } },
            { sizeof(MixedStruct), (void *)&ss[8], [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss[8]); } },
        };

        hr = ValidateFunctionArgs(pArgumentInfo, functionName, expectedValues);

        _sawFuncEnter[functionName.ToWString()] = true;
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

    Fp32x2Struct fp32x2 = { 1.2f, 2 };
    Fp32x3Struct fp32x3 = { 6.7f, 10.11f, 3 };
    Fp32x4Struct fp32x4 = { 15.17f, 19.21f, 22.23f, 4 };
    Fp64x2Struct fp64x2 = { 1.2, 2 };
    Fp64x3Struct fp64x3 = { 6.7, 10.11, 3 };
    Fp64x4Struct fp64x4 = { 15.17, 19.21, 22.23, 4 };

    String functionName = GetFunctionIDName(functionIdOrClientID.functionID);
    if (functionName == WCHAR("SimpleArgsFunc"))
    {
        const WCHAR *str = WCHAR("Hello from SimpleArgsFunc!");

        ExpectedArgValue simpleRetValue = { sizeof(UINT_PTR), (void *)str, [&](UINT_PTR ptr){ return ValidateString(ptr, str); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, simpleRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("MixedStructFunc"))
    {
        MixedStruct ss = { 4, 1.0 };
        ExpectedArgValue MixedStructRetValue = { sizeof(MixedStruct), (void *)&ss, [&](UINT_PTR ptr){ return ValidateMixedStruct(ptr, ss); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, MixedStructRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("LargeStructFunc"))
    {
        int32_t val = 3;
        ExpectedArgValue largeStructRetValue = { sizeof(int32_t), (void *)&val, [&](UINT_PTR ptr){ return ValidateInt(ptr, val); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, largeStructRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("IntegerStructFunc"))
    {
        IntegerStruct is = { 21, 256 };
        ExpectedArgValue integerStructRetValue = { sizeof(IntegerStruct), (void *)&is, [&](UINT_PTR ptr){ return ValidateIntegerStruct(ptr, is); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, integerStructRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp32x2StructFunc"))
    {
        ExpectedArgValue floatingPointStructRetValue = { sizeof(Fp32x2Struct), (void *)&fp32x2, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp32x2); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, floatingPointStructRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp32x3StructFunc"))
    {
        ExpectedArgValue floatingPointStructRetValue = { sizeof(Fp32x3Struct), (void *)&fp32x3, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp32x3); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, floatingPointStructRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp32x4StructFunc"))
    {
        ExpectedArgValue floatingPointStructRetValue = { sizeof(Fp32x4Struct), (void *)&fp32x4, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp32x4); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, floatingPointStructRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp64x2StructFunc"))
    {
        ExpectedArgValue floatingPointStructRetValue = { sizeof(Fp64x2Struct), (void *)&fp64x2, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp64x2); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, floatingPointStructRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp64x3StructFunc"))
    {
        ExpectedArgValue floatingPointStructRetValue = { sizeof(Fp64x3Struct), (void *)&fp64x3, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp64x3); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, floatingPointStructRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("Fp64x4StructFunc"))
    {
        ExpectedArgValue floatingPointStructRetValue = { sizeof(Fp64x4Struct), (void *)&fp64x4, [&](UINT_PTR ptr){ return ValidateFloatingPointStruct(ptr, fp64x4); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, floatingPointStructRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("DoubleRetFunc"))
    {
        double d = 13.0;
        ExpectedArgValue doubleRetValue = { sizeof(double), (void *)&d, [&](UINT_PTR ptr){ return ValidateDouble(ptr, d); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, doubleRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("FloatRetFunc"))
    {
        float f = 13.0f;
        ExpectedArgValue floatRetValue = { sizeof(float), (void *)&f, [&](UINT_PTR ptr){ return ValidateFloat(ptr, f); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, floatRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("IntegerSseStructFunc"))
    {
        IntegerSseStruct val = { 1, 2, 3.5 };
        ExpectedArgValue expectedRetValue = { sizeof(IntegerSseStruct), (void*)&val, [&](UINT_PTR ptr) { return ValidateIntegerSseStruct(ptr, val); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, expectedRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("SseIntegerStructFunc"))
    {
        SseIntegerStruct val = { 1.2f, 3.5f, 6 };
        ExpectedArgValue expectedRetValue = { sizeof(SseIntegerStruct), (void*)&val, [&](UINT_PTR ptr) { return ValidateSseIntegerStruct(ptr, val); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, expectedRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("MixedSseStructFunc"))
    {
        MixedSseStruct val = { 1.2f, 3, 5.6f, 7.10f };
        ExpectedArgValue expectedRetValue = { sizeof(MixedSseStruct), (void*)&val, [&](UINT_PTR ptr) { return ValidateMixedSseStruct(ptr, val); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, expectedRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("SseMixedStructFunc"))
    {
        SseMixedStruct val = { 1.2f, 3.5f, 6, 7.10f };
        ExpectedArgValue expectedRetValue = { sizeof(SseMixedStruct), (void*)&val, [&](UINT_PTR ptr) { return ValidateSseMixedStruct(ptr, val); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, expectedRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
    }
    else if (functionName == WCHAR("MixedMixedStructFunc"))
    {
        MixedMixedStruct val = { 1.2f, 3, 5, 6.7f };
        ExpectedArgValue expectedRetValue = { sizeof(MixedMixedStruct), (void*)&val, [&](UINT_PTR ptr) { return ValidateMixedMixedStruct(ptr, val); } };
        hr = ValidateOneArgument(pRetvalRange, functionName, 0, expectedRetValue);

        _sawFuncLeave[functionName.ToWString()] = true;
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

bool SlowPathELTProfiler::ValidateFloatingPointStruct(UINT_PTR ptr, Fp32x2Struct expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    Fp32x2Struct lhs = *(Fp32x2Struct *)ptr;
    return lhs.x == expected.x && lhs.y == expected.y;
}

bool SlowPathELTProfiler::ValidateFloatingPointStruct(UINT_PTR ptr, Fp32x3Struct expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    Fp32x3Struct lhs = *(Fp32x3Struct *)ptr;
    return lhs.x == expected.x && lhs.y == expected.y && lhs.z == expected.z;
}

bool SlowPathELTProfiler::ValidateFloatingPointStruct(UINT_PTR ptr, Fp32x4Struct expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    Fp32x4Struct lhs = *(Fp32x4Struct *)ptr;
    return lhs.x == expected.x
        && lhs.y == expected.y
        && lhs.z == expected.z
        && lhs.w == expected.w;
}

bool SlowPathELTProfiler::ValidateFloatingPointStruct(UINT_PTR ptr, Fp64x2Struct expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    Fp64x2Struct lhs = *(Fp64x2Struct *)ptr;
    return lhs.x == expected.x && lhs.y == expected.y;
}

bool SlowPathELTProfiler::ValidateFloatingPointStruct(UINT_PTR ptr, Fp64x3Struct expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    Fp64x3Struct lhs = *(Fp64x3Struct *)ptr;
    return lhs.x == expected.x && lhs.y == expected.y && lhs.z == expected.z;
}

bool SlowPathELTProfiler::ValidateFloatingPointStruct(UINT_PTR ptr, Fp64x4Struct expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    Fp64x4Struct lhs = *(Fp64x4Struct *)ptr;
    return lhs.x == expected.x
        && lhs.y == expected.y
        && lhs.z == expected.z
        && lhs.w == expected.w;
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

bool SlowPathELTProfiler::ValidateIntegerSseStruct(UINT_PTR ptr, IntegerSseStruct expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    IntegerSseStruct lhs = *(IntegerSseStruct*)ptr;
    return lhs.x == expected.x
        && lhs.y == expected.y
        && lhs.z == expected.z;
}

bool SlowPathELTProfiler::ValidateSseIntegerStruct(UINT_PTR ptr, SseIntegerStruct expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    SseIntegerStruct lhs = *(SseIntegerStruct*)ptr;
    return lhs.x == expected.x
        && lhs.y == expected.y
        && lhs.z == expected.z;
}

bool SlowPathELTProfiler::ValidateMixedSseStruct(UINT_PTR ptr, MixedSseStruct expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    MixedSseStruct lhs = *(MixedSseStruct*)ptr;
    return lhs.x == expected.x
        && lhs.y == expected.y
        && lhs.z == expected.z
        && lhs.w == expected.w;
}

bool SlowPathELTProfiler::ValidateSseMixedStruct(UINT_PTR ptr, SseMixedStruct expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    SseMixedStruct lhs = *(SseMixedStruct*)ptr;
    return lhs.x == expected.x
        && lhs.y == expected.y
        && lhs.z == expected.z
        && lhs.w == expected.w;
}

bool SlowPathELTProfiler::ValidateMixedMixedStruct(UINT_PTR ptr, MixedMixedStruct expected)
{
    if (ptr == NULL)
    {
        return false;
    }

    MixedMixedStruct lhs = *(MixedMixedStruct*)ptr;
    return lhs.x == expected.x
        && lhs.y == expected.y
        && lhs.z == expected.z
        && lhs.w == expected.w;
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
