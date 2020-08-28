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
    double d1;
    double d2;
} FloatingPointStruct;

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
    bool _sawFloatingPointStructFuncLeave;
    bool _sawDoubleRetFuncLeave;

    TestType _testType;
    
    void PrintBytes(const BYTE *bytes, size_t length);
    
    bool ValidateInt(UINT_PTR ptr, int expected);
    bool ValidateFloat(UINT_PTR ptr, float expected);
    bool ValidateDouble(UINT_PTR ptr, double expected);
    bool ValidateString(UINT_PTR ptr, const WCHAR *expected);
    bool ValidateMixedStruct(UINT_PTR ptr, MixedStruct expected);
    bool ValidateLargeStruct(UINT_PTR ptr, LargeStruct expected);
    bool ValidateFloatingPointStruct(UINT_PTR ptr, FloatingPointStruct expected);
    bool ValidateIntegerStruct(UINT_PTR ptr, IntegerStruct expected);

    HRESULT ValidateOneArgument(COR_PRF_FUNCTION_ARGUMENT_RANGE *pArgRange,
                                String functionName,
                                size_t argPos,
                                ExpectedArgValue expectedValue);

    HRESULT ValidateFunctionArgs(COR_PRF_FUNCTION_ARGUMENT_INFO *pArgInfo,
                                 String name,
                                 std::vector<ExpectedArgValue> expectedArgValues);
};
