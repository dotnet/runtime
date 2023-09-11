// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
//
// Internal data access functionality.
//

//*****************************************************************************

#include "stdafx.h"

#include <winwrap.h>
#include <utilcode.h>
#include <dacprivate.h>

//----------------------------------------------------------------------------
//
// LiveProcDataTarget.
//
//----------------------------------------------------------------------------

LiveProcDataTarget::LiveProcDataTarget(HANDLE process,
                                       DWORD processId,
                                       CLRDATA_ADDRESS baseAddressOfEngine)
{
    m_process = process;
    m_processId = processId;
    m_baseAddressOfEngine = baseAddressOfEngine;
}

STDMETHODIMP
LiveProcDataTarget::QueryInterface(
    THIS_
    IN REFIID InterfaceId,
    OUT PVOID* Interface
    )
{
    if (InterfaceId == IID_IUnknown ||
        InterfaceId == IID_ICLRDataTarget)
    {
        *Interface = (ICLRDataTarget*)this;
        // No need to refcount as this class is contained.
        return S_OK;
    }
    else
    {
        *Interface = NULL;
        return E_NOINTERFACE;
    }
}

STDMETHODIMP_(ULONG)
LiveProcDataTarget::AddRef(
    THIS
    )
{
    // No need to refcount as this class is contained.
    return 1;
}

STDMETHODIMP_(ULONG)
LiveProcDataTarget::Release(
    THIS
    )
{
    SUPPORTS_DAC_HOST_ONLY;
    // No need to refcount as this class is contained.
    return 0;
}

HRESULT STDMETHODCALLTYPE
LiveProcDataTarget::GetMachineType(
    /* [out] */ ULONG32 *machine)
{
    LIMITED_METHOD_CONTRACT;

#if defined(TARGET_X86)
    *machine = IMAGE_FILE_MACHINE_I386;
#elif defined(TARGET_AMD64)
    *machine = IMAGE_FILE_MACHINE_AMD64;
#elif defined(TARGET_ARM)
    *machine = IMAGE_FILE_MACHINE_ARMNT;
#else
    PORTABILITY_ASSERT("Unknown Processor");
#endif
    return S_OK;
}

HRESULT STDMETHODCALLTYPE
LiveProcDataTarget::GetPointerSize(
    /* [out] */ ULONG32 *size)
{
    LIMITED_METHOD_CONTRACT;

    *size = sizeof(void*);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE
LiveProcDataTarget::GetImageBase(
    /* [string][in] */ LPCWSTR name,
    /* [out] */ CLRDATA_ADDRESS *base)
{
    //
    // The only image base that the access code cares
    // about right now is the base of mscorwks.
    //

    if (u16_strcmp(name, MAIN_CLR_DLL_NAME_W))
    {
        return E_NOINTERFACE;
    }

    //
    // If a base address was specified, use that
    //
    if (NULL != m_baseAddressOfEngine)
    {
        *base = m_baseAddressOfEngine;
        return S_OK;
    }

    //
    // Our creator must have told us WHICH clr to work with.
    //
    return E_FAIL;
}

HRESULT STDMETHODCALLTYPE
LiveProcDataTarget::ReadVirtual(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [length_is][size_is][out] */ PBYTE buffer,
    /* [in] */ ULONG32 request,
    /* [optional][out] */ ULONG32 *done)
{
    // ReadProcessMemory will fail if any part of the
    // region to read does not have read access.  This
    // routine attempts to read the largest valid prefix
    // so it has to break up reads on page boundaries.

    HRESULT status = S_OK;
    ULONG32 totalDone = 0;
    SIZE_T read;
    ULONG32 readSize;

    while (request > 0)
    {
        // Calculate bytes to read and don't let read cross
        // a page boundary.
        readSize = GetOsPageSize() - (ULONG32)(address & (GetOsPageSize() - 1));
        readSize = min(request, readSize);

        if (!ReadProcessMemory(m_process, (PVOID)(ULONG_PTR)address,
                               buffer, readSize, &read))
        {
            if (totalDone == 0)
            {
                // If we haven't read anything indicate failure.
                status = E_FAIL;
            }
            break;
        }

        totalDone += (ULONG32)read;
        address += read;
        buffer += read;
        request -= (ULONG32)read;
    }

    *done = totalDone;
    return status;
}

HRESULT STDMETHODCALLTYPE
LiveProcDataTarget::WriteVirtual(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [size_is][in] */ PBYTE buffer,
    /* [in] */ ULONG32 request,
    /* [optional][out] */ ULONG32 *done)
{
    // Not necessary yet.
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
LiveProcDataTarget::GetTLSValue(
    /* [in] */ ULONG32 threadID,
    /* [in] */ ULONG32 index,
    /* [out] */ CLRDATA_ADDRESS* value)
{
    SUPPORTS_DAC;
    // Not necessary yet.
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
LiveProcDataTarget::SetTLSValue(
    /* [in] */ ULONG32 threadID,
    /* [in] */ ULONG32 index,
    /* [in] */ CLRDATA_ADDRESS value)
{
    // Not necessary yet.
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
LiveProcDataTarget::GetCurrentThreadID(
    /* [out] */ ULONG32* threadID)
{
    SUPPORTS_DAC;
    // Not necessary yet.
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
LiveProcDataTarget::GetThreadContext(
    /* [in] */ ULONG32 threadID,
    /* [in] */ ULONG32 contextFlags,
    /* [in] */ ULONG32 contextSize,
    /* [out, size_is(contextSize)] */ PBYTE context)
{
    // Not necessary yet.
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
LiveProcDataTarget::SetThreadContext(
    /* [in] */ ULONG32 threadID,
    /* [in] */ ULONG32 contextSize,
    /* [out, size_is(contextSize)] */ PBYTE context)
{
    // Not necessary yet.
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
LiveProcDataTarget::Request(
    /* [in] */ ULONG32 reqCode,
    /* [in] */ ULONG32 inBufferSize,
    /* [size_is][in] */ BYTE *inBuffer,
    /* [in] */ ULONG32 outBufferSize,
    /* [size_is][out] */ BYTE *outBuffer)
{
    // None supported.
    return E_INVALIDARG;
}
