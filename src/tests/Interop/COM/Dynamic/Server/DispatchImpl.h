// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <ComHelpers.h>
#include <Windows.h>

// Implementation of IDispatch operations
class DispatchImpl : public UnknownImpl
{
public:
    DispatchImpl(GUID guid, void *instance, const wchar_t* tlb = nullptr);
    virtual ~DispatchImpl() = default;

    DispatchImpl(const DispatchImpl&) = delete;
    DispatchImpl& operator=(const DispatchImpl&) = delete;

    DispatchImpl(DispatchImpl&&) = default;
    DispatchImpl& operator=(DispatchImpl&&) = default;

protected:
    HRESULT DoGetTypeInfoCount(uint32_t* pctinfo);
    HRESULT DoGetTypeInfo(uint32_t iTInfo, ITypeInfo** ppTInfo);
    HRESULT DoGetIDsOfNames(LPOLESTR* rgszNames, uint32_t cNames, DISPID* rgDispId);
    HRESULT DoInvoke(DISPID dispIdMember, uint16_t wFlags, DISPPARAMS* pDispParams, VARIANT* pVarResult, EXCEPINFO* pExcepInfo, uint32_t* puArgErr);

private:
    ComSmartPtr<ITypeLib> _typeLib;
    ComSmartPtr<ITypeInfo> _typeInfo;
    void *_instance;
};

// Macro to use for defining dispatch impls
#define DEFINE_DISPATCH() \
    STDMETHOD(GetTypeInfoCount)(uint32_t *pctinfo) \
        { return DispatchImpl::DoGetTypeInfoCount(pctinfo); } \
    STDMETHOD(GetTypeInfo)(uint32_t iTInfo, LCID lcid, ITypeInfo **ppTInfo) \
        { return DispatchImpl::DoGetTypeInfo(iTInfo, ppTInfo); } \
    STDMETHOD(GetIDsOfNames)(REFIID riid, LPOLESTR* rgszNames, uint32_t cNames, LCID lcid, DISPID* rgDispId) \
        { return DispatchImpl::DoGetIDsOfNames(rgszNames, cNames, rgDispId); } \
    STDMETHOD(Invoke)(DISPID dispIdMember, REFIID riid, LCID lcid, uint16_t wFlags, DISPPARAMS* pDispParams, VARIANT* pVarResult, EXCEPINFO* pExcepInfo, uint32_t* puArgErr) \
        { return DispatchImpl::DoInvoke(dispIdMember, wFlags, pDispParams, pVarResult, pExcepInfo, puArgErr); }
