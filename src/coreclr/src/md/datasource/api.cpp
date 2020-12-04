// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// api.cpp
//

//
//*****************************************************************************

#include "stdafx.h"
#include "remotemdinternalrwsource.h"

HRESULT CreateRemoteMDInternalRWSource(TADDR mdInternalRWRemoteAddress, ICorDebugDataTarget* pDataTarget, DWORD defines, DWORD dataStructureVersion, IMDCustomDataSource** ppDataSource)
{
    HRESULT hr = S_OK;
    RemoteMDInternalRWSource* pSource = new (nothrow) RemoteMDInternalRWSource();
    if (pSource == NULL)
        return E_OUTOFMEMORY;

    hr = pSource->InitFromTarget(mdInternalRWRemoteAddress, pDataTarget, defines, dataStructureVersion);
    if (SUCCEEDED(hr))
    {
        hr = pSource->QueryInterface(IID_IMDCustomDataSource, (void**)ppDataSource);
    }
    if (FAILED(hr))
    {
        delete pSource;
    }
    return hr;
}
