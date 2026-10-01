// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"
#include <corpriv.h>
#include <dnmd_interfaces.hpp>

STDAPI CreateMetaDataDispenser(REFIID riid, void** dispenser)
{
    if (dispenser == nullptr)
        return E_INVALIDARG;
    *dispenser = nullptr;
    return GetDispenser(riid, dispenser);
}

STDAPI GetMDInternalInterface(
    LPVOID data,
    ULONG dataSize,
    DWORD flags,
    REFIID riid,
    void** internalImport)
{
    if (internalImport == nullptr)
        return E_INVALIDARG;
    *internalImport = nullptr;
    if (data == nullptr)
        return E_INVALIDARG;

    IMetaDataDispenser* dispenser = nullptr;
    HRESULT hr = CreateMetaDataDispenser(IID_IMetaDataDispenser, (void**)&dispenser);
    if (FAILED(hr))
        return hr;

    hr = dispenser->OpenScopeOnMemory(data, dataSize, flags, riid, (IUnknown**)internalImport);
    dispenser->Release();
    return hr;
}

STDAPI GetMDInternalInterfaceFromPublic(IUnknown* publicInterface, REFIID riid, void** internalImport)
{
    if (internalImport == nullptr)
        return E_INVALIDARG;
    *internalImport = nullptr;
    if (publicInterface == nullptr || riid != IID_IMDInternalImport)
        return E_INVALIDARG;

    return publicInterface->QueryInterface(riid, internalImport);
}

STDAPI GetMDPublicInterfaceFromInternal(void* internalImport, REFIID riid, void** publicInterface)
{
    if (publicInterface == nullptr)
        return E_INVALIDARG;
    *publicInterface = nullptr;
    if (internalImport == nullptr)
        return E_INVALIDARG;

    return GetDNMDPublicInterfaceFromInternal(
        static_cast<IMDInternalImport*>(internalImport), riid, publicInterface);
}

STDAPI ConvertMDInternalImport(IMDInternalImport* internalImport, IMDInternalImport** converted)
{
    return ConvertDNMDInternalImport(internalImport, converted);
}

STDAPI MDReOpenMetaDataWithMemory(void* publicImport, LPCVOID data, ULONG dataSize, DWORD flags)
{
    return ReOpenDNMDMetaDataWithMemory(static_cast<IUnknown*>(publicImport), data, dataSize, flags);
}
