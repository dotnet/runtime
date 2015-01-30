//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// 

#include "stdafx.h"
#include "unwinder.h"

EXTERN_C void GetRuntimeStackWalkInfo(IN  ULONG64   ControlPc,
                                      OUT UINT_PTR* pModuleBase,
                                      OUT UINT_PTR* pFuncEntry);

#if !defined(_TARGET_ARM_) && !defined(_TARGET_ARM64_)
//---------------------------------------------------------------------------------------
//
// Read an UNWIND_INFO structure given its address.  The UNWIND_INFO structure is variable sized.
// This is just a simple wrapper over the platform-specific DAC function which does the real work.
//
// Arguments:
//    taUnwindInfo - target address of the beginning of the UNWIND_INFO structure
//
// Return Value:
//   Return the specified UNWIND_INFO.  It lives in the DAC cache, so the caller doesn't need to explicitly
//   free the memory.  It'll get flushed when the DAC cache is flushed (i.e. when we continue).
//

UNWIND_INFO * OOPStackUnwinder::GetUnwindInfo(TADDR taUnwindInfo)
{
    return DacGetUnwindInfo(taUnwindInfo);
}
#endif // !_TARGET_ARM_ && !_TARGET_ARM64_

//---------------------------------------------------------------------------------------
//
// This is a simple wrapper over code:OOPStackUnwinder::ReadMemory().  Unlike ReadMemory(), 
// it fails if we don't successfully read all the specified bytes.
//
// Arguments:
//    address   - the address to be read
//    pbBuffer  - the buffer to store the read memory
//    cbRequest - the number of bytes requested
//
// Return Value:
//   S_OK if all the memory is read successfully.
//   HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY) if only part of the memory is read.
//   Failure HRs otherwise.
//

HRESULT OOPStackUnwinder::ReadAllMemory(                       DWORD64 address,
                                        __in_ecount(cbRequest) PVOID   pbBuffer,
                                                               DWORD   cbRequest)
{
    DWORD cbDone = 0;
    HRESULT hr = ReadMemory(address, pbBuffer, cbRequest, &cbDone);
    if (SUCCEEDED(hr) && (cbDone != cbRequest))
    {
        return HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY);
    }
    else
    {
        return hr;
    }
}

//---------------------------------------------------------------------------------------
//
// Read the specified memory from out-of-process.  This function uses DacReadAll() to do the trick.
//
// Arguments:
//    address   - the address to be read
//    pbBuffer  - the buffer to store the read memory
//    cbRequest - the number of bytes requested
//    pcbDone   - out parameter; returns the number of bytes read
//
// Return Value:
//   S_OK if all the memory is read successfully
//

HRESULT OOPStackUnwinder::ReadMemory(                       DWORD64 address,
                                       __in_ecount(cbRequest) PVOID   pbBuffer,
                                                              DWORD   cbRequest,
                                       __out_opt              PDWORD  pcbDone)
{
    _ASSERTE(pcbDone != NULL);

    HRESULT hr = DacReadAll(TO_TADDR(address), pbBuffer, cbRequest, false);
    if (SUCCEEDED(hr))
    {
        *pcbDone = cbRequest;

        // On X64, we need to replace any patches which are within the requested memory range.
        // This is because the X64 unwinder needs to disassemble the native instructions in order to determine
        // whether the IP is in an epilog.
#if defined(_TARGET_AMD64_)
        MemoryRange range(dac_cast<PTR_VOID>((TADDR)address), cbRequest);
        hr = DacReplacePatchesInHostMemory(range, pbBuffer);
#endif // _TARGET_AMD64_
    }
    else
    {
        *pcbDone = 0;
    }

    return hr;
}

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
                                          __out PDWORD64 pdwBase)
{
    GetRuntimeStackWalkInfo(address, reinterpret_cast<UINT_PTR *>(pdwBase), NULL);
    return ((*pdwBase == NULL) ? E_FAIL : S_OK);
}

//---------------------------------------------------------------------------------------
//
// Given a control PC, return the function entry of the functoin it is in.
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
                                           __out_ecount(cbBuffer) PVOID   pBuffer,
                                                                  DWORD   cbBuffer)
{
    if (cbBuffer < sizeof(RUNTIME_FUNCTION))
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
