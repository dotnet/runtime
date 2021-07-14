// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <memory>
#include <vector>
#include <functional>
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

class SlowPathELTProfiler : public Profiler
{
public:
    static std::shared_ptr<SlowPathELTProfiler> s_profiler;

    SlowPathELTProfiler() : Profiler(),
        _failures(0),
        _sawSimpleFuncEnter(false),
        _sawMixedStructFuncEnter(false),
        _sawLargeStructFuncEnter(false),
        _sawSimpleFuncLeave(false),
        _sawMixedStructFuncLeave(false),
        _sawLargeStructFuncLeave(false),
        _sawIntegerStructFuncLeave(false),
        _sawFp32x2StructFuncLeave(false),
        _sawFp32x3StructFuncLeave(false),
        _sawFp32x4StructFuncLeave(false),
        _sawFp64x2StructFuncLeave(false),
        _sawFp64x3StructFuncLeave(false),
        _sawFp64x4StructFuncLeave(false),
        _sawDoubleRetFuncLeave(false),
        _testType(TestType::Unknown)
    {}

    virtual GUID GetClsid();
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
    bool _sawSimpleFuncEnter;
    bool _sawMixedStructFuncEnter;
    bool _sawLargeStructFuncEnter;
    bool _sawSimpleFuncLeave;
    bool _sawMixedStructFuncLeave;
    bool _sawLargeStructFuncLeave;
    bool _sawIntegerStructFuncLeave;
    bool _sawFp32x2StructFuncLeave;
    bool _sawFp32x3StructFuncLeave;
    bool _sawFp32x4StructFuncLeave;
    bool _sawFp64x2StructFuncLeave;
    bool _sawFp64x3StructFuncLeave;
    bool _sawFp64x4StructFuncLeave;
    bool _sawDoubleRetFuncLeave;

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

    HRESULT ValidateOneArgument(COR_PRF_FUNCTION_ARGUMENT_RANGE *pArgRange,
                                String functionName,
                                size_t argPos,
                                ExpectedArgValue expectedValue);

    HRESULT ValidateFunctionArgs(COR_PRF_FUNCTION_ARGUMENT_INFO *pArgInfo,
                                 String name,
                                 std::vector<ExpectedArgValue> expectedArgValues);
};
