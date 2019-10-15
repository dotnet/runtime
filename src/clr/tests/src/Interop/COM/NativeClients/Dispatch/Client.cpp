// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ClientTests.h"
#include <memory>


void Validate_Numeric_In_ReturnByRef();
void Validate_Float_In_ReturnAndUpdateByRef();
void Validate_Double_In_ReturnAndUpdateByRef();
void Validate_LCID_Marshaled();

template<COINIT TM>
struct ComInit
{
    const HRESULT Result;

    ComInit()
        : Result{ ::CoInitializeEx(nullptr, TM) }
    { }

    ~ComInit()
    {
        if (SUCCEEDED(Result))
            ::CoUninitialize();
    }
};

using ComMTA = ComInit<COINIT_MULTITHREADED>;

int __cdecl main()
{
    ComMTA init;
    if (FAILED(init.Result))
        return -1;

    try
    {
        Validate_Numeric_In_ReturnByRef();
        Validate_Float_In_ReturnAndUpdateByRef();
        Validate_Double_In_ReturnAndUpdateByRef();
        Validate_LCID_Marshaled();
    }
    catch (HRESULT hr)
    {
        ::printf("Test Failure: 0x%08x\n", hr);
        return 101;
    }

    return 100;
}

void Validate_Numeric_In_ReturnByRef()
{
    HRESULT hr;

    CoreShimComActivation csact{ W("NETServer"), W("DispatchTesting") };

    ComSmartPtr<IDispatchTesting> dispatchTesting;
    THROW_IF_FAILED(::CoCreateInstance(CLSID_DispatchTesting, nullptr, CLSCTX_INPROC, IID_IDispatchTesting, (void**)&dispatchTesting));

    LPOLESTR numericMethodName = (LPOLESTR)W("DoubleNumeric_ReturnByRef");
    LCID lcid = MAKELCID(LANG_USER_DEFAULT, SORT_DEFAULT);
    DISPID methodId;

    THROW_IF_FAILED(dispatchTesting->GetIDsOfNames(
        IID_NULL,
        &numericMethodName,
        1,
        lcid,
        &methodId));
    
    BYTE b1 = 24;
    BYTE b2;
    SHORT s1 = 53;
    SHORT s2;
    USHORT us1 = 74;
    USHORT us2;
    LONG i1 = 34;
    LONG i2;
    ULONG ui1 = 854;
    ULONG ui2;
    LONGLONG l1 = 894;
    LONGLONG l2;
    ULONGLONG ul1 = 4168;
    ULONGLONG ul2;

    DISPPARAMS params;
    params.cArgs = 14;
    params.rgvarg = new VARIANTARG[params.cArgs];
    params.cNamedArgs = 0;
    params.rgdispidNamedArgs = nullptr;
    
    V_VT(&params.rgvarg[13]) = VT_UI1;
    V_UI1(&params.rgvarg[13]) = b1;
    V_VT(&params.rgvarg[12]) = VT_BYREF | VT_UI1;
    V_UI1REF(&params.rgvarg[12]) = &b2;
    V_VT(&params.rgvarg[11]) = VT_I2;
    V_I2(&params.rgvarg[11]) = s1;
    V_VT(&params.rgvarg[10]) = VT_BYREF | VT_I2;
    V_I2REF(&params.rgvarg[10]) = &s2;
    V_VT(&params.rgvarg[9]) = VT_UI2;
    V_UI2(&params.rgvarg[9]) = us1;
    V_VT(&params.rgvarg[8]) = VT_BYREF | VT_UI2;
    V_UI2REF(&params.rgvarg[8]) = &us2;
    V_VT(&params.rgvarg[7]) = VT_I4;
    V_I4(&params.rgvarg[7]) = i1;
    V_VT(&params.rgvarg[6]) = VT_BYREF | VT_I4;
    V_I4REF(&params.rgvarg[6]) = &i2;
    V_VT(&params.rgvarg[5]) = VT_UI4;
    V_UI4(&params.rgvarg[5]) = ui1;
    V_VT(&params.rgvarg[4]) = VT_BYREF | VT_UI4;
    V_UI4REF(&params.rgvarg[4]) = &ui2;
    V_VT(&params.rgvarg[3]) = VT_I8;
    V_I8(&params.rgvarg[3]) = l1;
    V_VT(&params.rgvarg[2]) = VT_BYREF | VT_I8;
    V_I8REF(&params.rgvarg[2]) = &l2;
    V_VT(&params.rgvarg[1]) = VT_UI8;
    V_UI8(&params.rgvarg[1]) = ul1;
    V_VT(&params.rgvarg[0]) = VT_BYREF | VT_UI8;
    V_UI8REF(&params.rgvarg[0]) = &ul2;

    THROW_IF_FAILED(dispatchTesting->Invoke(
        methodId,
        IID_NULL,
        lcid,
        DISPATCH_METHOD,
        &params,
        nullptr,
        nullptr,
        nullptr
    ));

    THROW_FAIL_IF_FALSE(b2 == b1 * 2);
    THROW_FAIL_IF_FALSE(s2 == s1 * 2);
    THROW_FAIL_IF_FALSE(us2 == us1 * 2);
    THROW_FAIL_IF_FALSE(i2 == i1 * 2);
    THROW_FAIL_IF_FALSE(ui2 == ui1 * 2);
    THROW_FAIL_IF_FALSE(l2 == l1 * 2);
    THROW_FAIL_IF_FALSE(ul2 == ul1 * 2);
}

namespace
{
    bool EqualByBound(float expected, float actual)
    {
        float low = expected - 0.0001f;
        float high = expected + 0.0001f;
        float eps = abs(expected - actual);
        return eps < std::numeric_limits<float>::epsilon() || (low < actual && actual < high);
    }

    bool EqualByBound(double expected, double actual)
    {
        double low = expected - 0.00001;
        double high = expected + 0.00001;
        double eps = abs(expected - actual);
        return eps < std::numeric_limits<double>::epsilon() || (low < actual && actual < high);
    }
}

void Validate_Float_In_ReturnAndUpdateByRef()
{
    HRESULT hr;

    CoreShimComActivation csact{ W("NETServer"), W("DispatchTesting") };

    ComSmartPtr<IDispatchTesting> dispatchTesting;
    THROW_IF_FAILED(::CoCreateInstance(CLSID_DispatchTesting, nullptr, CLSCTX_INPROC, IID_IDispatchTesting, (void**)&dispatchTesting));

    LPOLESTR numericMethodName = (LPOLESTR)W("Add_Float_ReturnAndUpdateByRef");
    LCID lcid = MAKELCID(LANG_USER_DEFAULT, SORT_DEFAULT);
    DISPID methodId;

    THROW_IF_FAILED(dispatchTesting->GetIDsOfNames(
        IID_NULL,
        &numericMethodName,
        1,
        lcid,
        &methodId));
    
    float a = 12.34f;
    float b = 1.234f;
    float expected = b + a;

    DISPPARAMS params;
    params.cArgs = 2;
    params.rgvarg = new VARIANTARG[params.cArgs];
    params.cNamedArgs = 0;
    params.rgdispidNamedArgs = nullptr;

    VARIANT result;

    V_VT(&params.rgvarg[1]) = VT_R4;
    V_R4(&params.rgvarg[1]) = a;
    V_VT(&params.rgvarg[0]) = VT_BYREF | VT_R4;
    V_R4REF(&params.rgvarg[0]) = &b;


    THROW_IF_FAILED(dispatchTesting->Invoke(
        methodId,
        IID_NULL,
        lcid,
        DISPATCH_METHOD,
        &params,
        &result,
        nullptr,
        nullptr
    ));

    THROW_FAIL_IF_FALSE(EqualByBound(expected, V_R4(&result)));
    THROW_FAIL_IF_FALSE(EqualByBound(expected, b));
}

void Validate_Double_In_ReturnAndUpdateByRef()
{
    HRESULT hr;

    CoreShimComActivation csact{ W("NETServer"), W("DispatchTesting") };

    ComSmartPtr<IDispatchTesting> dispatchTesting;
    THROW_IF_FAILED(::CoCreateInstance(CLSID_DispatchTesting, nullptr, CLSCTX_INPROC, IID_IDispatchTesting, (void**)&dispatchTesting));

    LPOLESTR numericMethodName = (LPOLESTR)W("Add_Double_ReturnAndUpdateByRef");
    LCID lcid = MAKELCID(LANG_USER_DEFAULT, SORT_DEFAULT);
    DISPID methodId;

    THROW_IF_FAILED(dispatchTesting->GetIDsOfNames(
        IID_NULL,
        &numericMethodName,
        1,
        lcid,
        &methodId));
    
    double a = 1856.5634;
    double b = 587867.757;
    double expected = a + b;

    DISPPARAMS params;
    params.cArgs = 2;
    params.rgvarg = new VARIANTARG[params.cArgs];
    params.cNamedArgs = 0;
    params.rgdispidNamedArgs = nullptr;

    VARIANT result;

    V_VT(&params.rgvarg[1]) = VT_R8;
    V_R8(&params.rgvarg[1]) = a;
    V_VT(&params.rgvarg[0]) = VT_BYREF | VT_R8;
    V_R8REF(&params.rgvarg[0]) = &b;


    THROW_IF_FAILED(dispatchTesting->Invoke(
        methodId,
        IID_NULL,
        lcid,
        DISPATCH_METHOD,
        &params,
        &result,
        nullptr,
        nullptr
    ));

    THROW_FAIL_IF_FALSE(EqualByBound(expected, V_R8(&result)));
    THROW_FAIL_IF_FALSE(EqualByBound(expected, b));
}

void Validate_LCID_Marshaled()
{
    HRESULT hr;

    CoreShimComActivation csact{ W("NETServer"), W("DispatchTesting") };

    ComSmartPtr<IDispatchTesting> dispatchTesting;
    THROW_IF_FAILED(::CoCreateInstance(CLSID_DispatchTesting, nullptr, CLSCTX_INPROC, IID_IDispatchTesting, (void**)&dispatchTesting));

    LPOLESTR numericMethodName = (LPOLESTR)W("PassThroughLCID");
    LCID lcid = MAKELCID(MAKELANGID(LANG_SPANISH, SUBLANG_SPANISH_CHILE), SORT_DEFAULT);
    DISPID methodId;

    THROW_IF_FAILED(dispatchTesting->GetIDsOfNames(
        IID_NULL,
        &numericMethodName,
        1,
        lcid,
        &methodId));

    DISPPARAMS params;
    params.cArgs = 0;
    params.rgvarg = nullptr;
    params.cNamedArgs = 0;
    params.rgdispidNamedArgs = nullptr;

    VARIANT result;

    THROW_IF_FAILED(dispatchTesting->Invoke(
        methodId,
        IID_NULL,
        lcid,
        DISPATCH_METHOD,
        &params,
        &result,
        nullptr,
        nullptr
    ));

    THROW_FAIL_IF_FALSE(lcid == V_I4(&result));
}
