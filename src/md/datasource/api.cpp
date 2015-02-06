//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
