// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "DispatchImpl.h"

namespace
{
    const wchar_t* s_tlbDefault = L"DynamicTestServer.tlb";
}

DispatchImpl::DispatchImpl(GUID guid, void *instance, const wchar_t* tlb)
    : _typeLib{ nullptr }
    , _typeInfo{ nullptr }
    , _instance{ instance }
{
    const wchar_t* tlbToLoad = tlb == nullptr ? s_tlbDefault : tlb;
    HRESULT hr = ::LoadTypeLibEx(tlbToLoad, REGKIND::REGKIND_NONE, &_typeLib);
    if (FAILED(hr))
        throw hr;

    hr = _typeLib->GetTypeInfoOfGuid(guid, &_typeInfo);
    if (FAILED(hr))
        throw hr;
}

HRESULT DispatchImpl::DoGetTypeInfoCount(UINT* pctinfo)
{
    *pctinfo = 1;
    return S_OK;
}

HRESULT DispatchImpl::DoGetTypeInfo(UINT iTInfo, ITypeInfo** ppTInfo)
{
    if (iTInfo != 0)
        return DISP_E_BADINDEX;

    assert(_typeInfo != nullptr);
    return _typeInfo->QueryInterface(__uuidof(*ppTInfo), (void**)ppTInfo);
}

HRESULT DispatchImpl::DoGetIDsOfNames(LPOLESTR* rgszNames, UINT cNames, DISPID* rgDispId)
{
    return _typeInfo->GetIDsOfNames(rgszNames, cNames, rgDispId);
}

HRESULT DispatchImpl::DoInvoke(DISPID dispIdMember, WORD wFlags, DISPPARAMS* pDispParams, VARIANT* pVarResult, EXCEPINFO* pExcepInfo, UINT* puArgErr)
{
    return _typeInfo->Invoke(_instance, dispIdMember, wFlags, pDispParams, pVarResult, pExcepInfo, puArgErr);
}
