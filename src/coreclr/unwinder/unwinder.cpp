// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

#include "stdafx.h"
#include "baseunwinder.h"

EXTERN_C void GetRuntimeStackWalkInfo(IN  ULONG64   ControlPc,
                                      OUT UINT_PTR* pModuleBase,
                                      OUT UINT_PTR* pFuncEntry);

//---------------------------------------------------------------------------------------
//
// Given a control PC, return the base of the module it is in.  For jitted managed code, this is the
// start of the code heap.
//
// Arguments:
//    address - the specified address
//    pdwBase - out parameter; returns the module base
//
// Return Value:
//    S_OK if we retrieve the module base successfully;
//    E_FAIL otherwise
//

HRESULT OOPStackUnwinder::GetModuleBase(      DWORD64  address,
                                          _Out_ PDWORD64 pdwBase)
{
    GetRuntimeStackWalkInfo(address, reinterpret_cast<UINT_PTR *>(pdwBase), NULL);
    return ((*pdwBase == NULL) ? E_FAIL : S_OK);
}

//---------------------------------------------------------------------------------------
//
// Given a control PC, return the function entry of the function it is in.
//
// Arguments:
//    address  - the specified IP
//    pBuffer  - the buffer to store the retrieved function entry
//    cbBuffer - the size of the buffer
//
// Return Value:
//    S_OK          if we retrieve the function entry successfully;
//    E_INVALIDARG  if the buffer is too small;
//    E_FAIL        otherwise
//

HRESULT OOPStackUnwinder::GetFunctionEntry(                       DWORD64 address,
                                           _Out_writes_(cbBuffer) PVOID   pBuffer,
                                                                  DWORD   cbBuffer)
{
    if (cbBuffer < sizeof(T_RUNTIME_FUNCTION))
    {
        return E_INVALIDARG;
    }

    PVOID pFuncEntry = NULL;
    GetRuntimeStackWalkInfo(address, NULL, reinterpret_cast<UINT_PTR *>(&pFuncEntry));
    if (pFuncEntry == NULL)
    {
        return E_FAIL;
    }

    memcpy(pBuffer, pFuncEntry, cbBuffer);
    return S_OK;
}
