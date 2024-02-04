// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>

namespace
{
    HRESULT InvokeDelegate(IDispatch* dele, VARIANT* pResult)
    {
        HRESULT hr;
        BSTR bstrName = SysAllocString(L"DynamicInvoke");
        DISPID dispid = 0;
        hr = dele->GetIDsOfNames(
            IID_NULL,
            &bstrName,
            1,
            GetUserDefaultLCID(),
            &dispid);

        SysFreeString(bstrName);

        if (FAILED(hr))
        {
            printf("\nERROR: Invoke failed: 0x%x\n", (uint32_t)hr);
            return hr;
        }

        DISPPARAMS params = { NULL, NULL, 0, 0 };
        VariantInit(pResult);
        hr = dele->Invoke(
            dispid,
            IID_NULL,
            GetUserDefaultLCID(),
            DISPATCH_METHOD,
            &params,
            pResult,
            NULL,
            NULL);

        return hr;
    }
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ValidateDelegateValueMatchesExpected(int i, IDispatch* delegate)
{
    VARIANT pRetVal;
    HRESULT hr = InvokeDelegate(delegate, &pRetVal);
    return SUCCEEDED(hr) && V_VT(&pRetVal) == VT_I4 && V_I4(&pRetVal) == i ? TRUE : FALSE;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ValidateDelegateValueMatchesExpectedAndClear(int i, IDispatch** delegate)
{
    VARIANT pRetVal;
    HRESULT hr = InvokeDelegate(*delegate, &pRetVal);
    BOOL result = SUCCEEDED(hr) && V_VT(&pRetVal) == VT_I4 && V_I4(&pRetVal) == i ? TRUE : FALSE;
    (*delegate)->Release();
    *delegate = nullptr;
    return result;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE DuplicateDelegate(int i, IDispatch* delegateIn, IDispatch** delegateOut)
{
    VARIANT pRetVal;
    HRESULT hr = InvokeDelegate(delegateIn, &pRetVal);
    BOOL result = SUCCEEDED(hr) && V_VT(&pRetVal) == VT_I4 && V_I4(&pRetVal) == i ? TRUE : FALSE;
    delegateIn->AddRef();
    *delegateOut = delegateIn;
    return result;
}

extern "C" DLL_EXPORT IDispatch* STDMETHODCALLTYPE DuplicateDelegateReturned(IDispatch* delegateIn)
{
    delegateIn->AddRef();
    return delegateIn;
}

struct DispatchDelegateWithExpectedValue
{
    int expected;
    IDispatch* delegate;
};


extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ValidateStructDelegateValueMatchesExpected(DispatchDelegateWithExpectedValue dispatch)
{
    VARIANT pRetVal;
    HRESULT hr = InvokeDelegate(dispatch.delegate, &pRetVal);
    return SUCCEEDED(hr) && V_VT(&pRetVal) == VT_I4 && V_I4(&pRetVal) == dispatch.expected ? TRUE : FALSE;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ValidateDelegateValueMatchesExpectedAndClearStruct(DispatchDelegateWithExpectedValue* dispatch)
{
    VARIANT pRetVal;
    HRESULT hr = InvokeDelegate(dispatch->delegate, &pRetVal);
    BOOL result = SUCCEEDED(hr) && V_VT(&pRetVal) == VT_I4 && V_I4(&pRetVal) == dispatch->expected ? TRUE : FALSE;
    dispatch->delegate->Release();
    dispatch->delegate = nullptr;
    return result;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE DuplicateStruct(DispatchDelegateWithExpectedValue dispatchIn, DispatchDelegateWithExpectedValue* dispatchOut)
{
    VARIANT pRetVal;
    HRESULT hr = InvokeDelegate(dispatchIn.delegate, &pRetVal);
    BOOL result = SUCCEEDED(hr) && V_VT(&pRetVal) == VT_I4 && V_I4(&pRetVal) == dispatchIn.expected ? TRUE : FALSE;
    dispatchIn.delegate->AddRef();
    dispatchOut->delegate = dispatchIn.delegate;
    dispatchOut->expected = dispatchIn.expected;
    return result;
}

extern "C" DLL_EXPORT void* Invalid(...)
{
    return nullptr;
}
