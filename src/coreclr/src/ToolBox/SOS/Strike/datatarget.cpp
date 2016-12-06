// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "sos.h"
#include "datatarget.h"
#include "corhdr.h"
#include "cor.h"
#include "dacprivate.h"
#include "sospriv.h"
#include "corerror.h"

#define IMAGE_FILE_MACHINE_AMD64             0x8664  // AMD64 (K8)

DataTarget::DataTarget(void) :
    m_ref(0)
{
}

STDMETHODIMP
DataTarget::QueryInterface(
    THIS_
    ___in REFIID InterfaceId,
    ___out PVOID* Interface
    )
{
    if (InterfaceId == IID_IUnknown ||
        InterfaceId == IID_ICLRDataTarget)
    {
        *Interface = (ICLRDataTarget*)this;
        AddRef();
        return S_OK;
    }
    else if (InterfaceId == IID_ICorDebugDataTarget4)
    {
        *Interface = (ICorDebugDataTarget4*)this;
        AddRef();
        return S_OK;
    }
    else
    {
        *Interface = NULL;
        return E_NOINTERFACE;
    }
}

STDMETHODIMP_(ULONG)
DataTarget::AddRef(
    THIS
    )
{
    LONG ref = InterlockedIncrement(&m_ref);    
    return ref;
}

STDMETHODIMP_(ULONG)
DataTarget::Release(
    THIS
    )
{
    LONG ref = InterlockedDecrement(&m_ref);
    if (ref == 0)
    {
        delete this;
    }
    return ref;
}

HRESULT STDMETHODCALLTYPE
DataTarget::GetMachineType(
    /* [out] */ ULONG32 *machine)
{
    if (g_ExtControl == NULL)
    {
        return E_UNEXPECTED;
    }
    return g_ExtControl->GetExecutingProcessorType(machine);
}

HRESULT STDMETHODCALLTYPE
DataTarget::GetPointerSize(
    /* [out] */ ULONG32 *size)
{
#if defined(SOS_TARGET_AMD64) || defined(SOS_TARGET_ARM64)
    *size = 8;
#elif defined(SOS_TARGET_ARM) || defined(SOS_TARGET_X86)
    *size = 4;
#else
  #error Unsupported architecture
#endif

    return S_OK;
}

HRESULT STDMETHODCALLTYPE
DataTarget::GetImageBase(
    /* [string][in] */ LPCWSTR name,
    /* [out] */ CLRDATA_ADDRESS *base)
{
    if (g_ExtSymbols == NULL)
    {
        return E_UNEXPECTED;
    }
    CHAR lpstr[MAX_LONGPATH];
    int name_length = WideCharToMultiByte(CP_ACP, 0, name, -1, lpstr, MAX_LONGPATH, NULL, NULL);
    if (name_length == 0)
    {
        return E_FAIL;
    }
    return g_ExtSymbols->GetModuleByModuleName(lpstr, 0, NULL, base);
}

HRESULT STDMETHODCALLTYPE
DataTarget::ReadVirtual(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [length_is][size_is][out] */ PBYTE buffer,
    /* [in] */ ULONG32 request,
    /* [optional][out] */ ULONG32 *done)
{
    if (g_ExtData == NULL)
    {
        return E_UNEXPECTED;
    }
    return g_ExtData->ReadVirtual(address, (PVOID)buffer, request, done);
}

HRESULT STDMETHODCALLTYPE
DataTarget::WriteVirtual(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [size_is][in] */ PBYTE buffer,
    /* [in] */ ULONG32 request,
    /* [optional][out] */ ULONG32 *done)
{
    if (g_ExtData == NULL)
    {
        return E_UNEXPECTED;
    }
    return g_ExtData->WriteVirtual(address, (PVOID)buffer, request, done);
}

HRESULT STDMETHODCALLTYPE
DataTarget::GetTLSValue(
    /* [in] */ ULONG32 threadID,
    /* [in] */ ULONG32 index,
    /* [out] */ CLRDATA_ADDRESS* value)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
DataTarget::SetTLSValue(
    /* [in] */ ULONG32 threadID,
    /* [in] */ ULONG32 index,
    /* [in] */ CLRDATA_ADDRESS value)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
DataTarget::GetCurrentThreadID(
    /* [out] */ ULONG32* threadID)
{
    if (g_ExtSystem == NULL)
    {
        return E_UNEXPECTED;
    }
    return g_ExtSystem->GetCurrentThreadSystemId(threadID);
}

HRESULT STDMETHODCALLTYPE
DataTarget::GetThreadContext(
    /* [in] */ ULONG32 threadID,
    /* [in] */ ULONG32 contextFlags,
    /* [in] */ ULONG32 contextSize,
    /* [out, size_is(contextSize)] */ PBYTE context)
{
    if (g_ExtSystem == NULL)
    {
        return E_UNEXPECTED;
    }
    return g_ExtSystem->GetThreadContextById(threadID, contextFlags, contextSize, context);
}

HRESULT STDMETHODCALLTYPE
DataTarget::SetThreadContext(
    /* [in] */ ULONG32 threadID,
    /* [in] */ ULONG32 contextSize,
    /* [out, size_is(contextSize)] */ PBYTE context)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
DataTarget::Request(
    /* [in] */ ULONG32 reqCode,
    /* [in] */ ULONG32 inBufferSize,
    /* [size_is][in] */ BYTE *inBuffer,
    /* [in] */ ULONG32 outBufferSize,
    /* [size_is][out] */ BYTE *outBuffer)
{
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE 
DataTarget::VirtualUnwind(
    /* [in] */ DWORD threadId,
    /* [in] */ ULONG32 contextSize,
    /* [in, out, size_is(contextSize)] */ PBYTE context)
{
    if (g_ExtServices == NULL)
    {
        return E_UNEXPECTED;
    }
    return g_ExtServices->VirtualUnwind(threadId, contextSize, context);
}
