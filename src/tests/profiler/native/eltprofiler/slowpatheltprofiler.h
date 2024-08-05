// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <functional>
#include <memory>
#include <unordered_map>
#include <vector>
#include "../profiler.h"

typedef bool (*validateFunc)(void *pMem);

typedef struct
{
    size_t length;
    void *value;
    std::function<bool(UINT_PTR)> func;
} ExpectedArgValue;

typedef struct
{
    int x;
    double d;
} MixedStruct;

typedef struct
{
    int x0;
    double d0;
    int x1;
    double d1;
    int x2;
    double d2;
    int x3;
    double d3;
} LargeStruct;

typedef struct
{
    int x;
    int y;
} IntegerStruct;

typedef struct
{
    float x;
    float y;
} Fp32x2Struct;

typedef struct
{
    float x;
    float y;
    float z;
} Fp32x3Struct;

typedef struct
{
    float x;
    float y;
    float z;
    float w;
} Fp32x4Struct;

typedef struct
{
    double x;
    double y;
} Fp64x2Struct;

typedef struct
{
    double x;
    double y;
    double z;
} Fp64x3Struct;

typedef struct
{
    double x;
    double y;
    double z;
    double w;
} Fp64x4Struct;

typedef struct
{
    int x;
    int y;
    double z;
} IntegerSseStruct;

typedef struct
{
    float x;
    float y;
    int z;
} SseIntegerStruct;

typedef struct
{
    float x;
    int y;
    float z;
    float w;
} MixedSseStruct;

typedef struct
{
    float x;
    float y;
    int z;
    float w;
} SseMixedStruct;

typedef struct
{
    float x;
    int y;
    int z;
    float w;
} MixedMixedStruct;

class SlowPathELTProfiler : public Profiler
{
public:
    static std::shared_ptr<SlowPathELTProfiler> s_profiler;

    SlowPathELTProfiler() : Profiler(), _failures(0), _testType(TestType::Unknown)
    {
        _sawFuncEnter[L"SimpleArgsFunc"] = false;
        _sawFuncEnter[L"MixedStructFunc"] = false;
        _sawFuncEnter[L"LargeStructFunc"] = false;
        _sawFuncEnter[L"Fp32x2StructFunc"] = false;
        _sawFuncEnter[L"Fp32x2StructFp32x3StructFunc"] = false;
        _sawFuncEnter[L"Fp32x3StructFunc"] = false;
        _sawFuncEnter[L"Fp32x3StructFp32x2StructFunc"] = false;
        _sawFuncEnter[L"Fp32x3StructSingleFp32x3StructSingleFunc"] = false;
        _sawFuncEnter[L"Fp32x4StructFunc"] = false;
        _sawFuncEnter[L"Fp32x4StructFp32x4StructFunc"] = false;
        _sawFuncEnter[L"Fp64x2StructFunc"] = false;
        _sawFuncEnter[L"Fp64x2StructFp64x3StructFunc"] = false;
        _sawFuncEnter[L"Fp64x3StructFunc"] = false;
        _sawFuncEnter[L"Fp64x3StructFp64x2StructFunc"] = false;
        _sawFuncEnter[L"Fp64x3StructDoubleFp64x3StructDoubleFunc"] = false;
        _sawFuncEnter[L"Fp64x4StructFunc"] = false;
        _sawFuncEnter[L"Fp64x4StructFp64x4StructFunc"] = false;
        _sawFuncEnter[L"IntManyMixedStructFunc"] = false;
        _sawFuncEnter[L"DoubleManyMixedStructFunc"] = false;

        _sawFuncLeave[L"SimpleArgsFunc"] = false;
        _sawFuncLeave[L"MixedStructFunc"] = false;
        _sawFuncLeave[L"LargeStructFunc"] = false;
        _sawFuncLeave[L"IntegerStructFunc"] = false;
        _sawFuncLeave[L"Fp32x2StructFunc"] = false;
        _sawFuncLeave[L"Fp32x3StructFunc"] = false;
        _sawFuncLeave[L"Fp32x4StructFunc"] = false;
        _sawFuncLeave[L"Fp64x2StructFunc"] = false;
        _sawFuncLeave[L"Fp64x3StructFunc"] = false;
        _sawFuncLeave[L"Fp64x4StructFunc"] = false;
        _sawFuncLeave[L"DoubleRetFunc"] = false;
        _sawFuncLeave[L"FloatRetFunc"] = false;
        _sawFuncLeave[L"IntegerSseStructFunc"] = false;
        _sawFuncLeave[L"SseIntegerStructFunc"] = false;
        _sawFuncLeave[L"MixedSseStructFunc"] = false;
        _sawFuncLeave[L"SseMixedStructFunc"] = false;
        _sawFuncLeave[L"MixedMixedStructFunc"] = false;
    }

    static GUID GetClsid();
    virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk);
    virtual HRESULT STDMETHODCALLTYPE Shutdown();

    HRESULT STDMETHODCALLTYPE EnterCallback(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
    HRESULT STDMETHODCALLTYPE LeaveCallback(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
    HRESULT STDMETHODCALLTYPE TailcallCallback(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);

private:
    enum class TestType
    {
        EnterHooks,
        LeaveHooks,
        Unknown
    };

    std::atomic<int> _failures;
    std::unordered_map<std::wstring, bool> _sawFuncEnter;
    std::unordered_map<std::wstring, bool> _sawFuncLeave;

    TestType _testType;

    void PrintBytes(const BYTE *bytes, size_t length);

    bool ValidateInt(UINT_PTR ptr, int expected);
    bool ValidateFloat(UINT_PTR ptr, float expected);
    bool ValidateDouble(UINT_PTR ptr, double expected);
    bool ValidateString(UINT_PTR ptr, const WCHAR *expected);
    bool ValidateMixedStruct(UINT_PTR ptr, MixedStruct expected);
    bool ValidateLargeStruct(UINT_PTR ptr, LargeStruct expected);
    bool ValidateFloatingPointStruct(UINT_PTR ptr, Fp32x2Struct expected);
    bool ValidateFloatingPointStruct(UINT_PTR ptr, Fp32x3Struct expected);
    bool ValidateFloatingPointStruct(UINT_PTR ptr, Fp32x4Struct expected);
    bool ValidateFloatingPointStruct(UINT_PTR ptr, Fp64x2Struct expected);
    bool ValidateFloatingPointStruct(UINT_PTR ptr, Fp64x3Struct expected);
    bool ValidateFloatingPointStruct(UINT_PTR ptr, Fp64x4Struct expected);
    bool ValidateIntegerStruct(UINT_PTR ptr, IntegerStruct expected);
    bool ValidateIntegerSseStruct(UINT_PTR ptr, IntegerSseStruct expected);
    bool ValidateSseIntegerStruct(UINT_PTR ptr, SseIntegerStruct expected);
    bool ValidateMixedSseStruct(UINT_PTR ptr, MixedSseStruct expected);
    bool ValidateSseMixedStruct(UINT_PTR ptr, SseMixedStruct expected);
    bool ValidateMixedMixedStruct(UINT_PTR ptr, MixedMixedStruct expected);

    HRESULT ValidateOneArgument(COR_PRF_FUNCTION_ARGUMENT_RANGE *pArgRange,
                                String functionName,
                                size_t argPos,
                                ExpectedArgValue expectedValue);

    HRESULT ValidateFunctionArgs(COR_PRF_FUNCTION_ARGUMENT_INFO *pArgInfo,
                                 String name,
                                 std::vector<ExpectedArgValue> expectedArgValues);
};
