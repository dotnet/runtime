// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

DumpDataTarget::DumpDataTarget(CrashInfo& crashInfo) :
    m_ref(1),
    m_crashInfo(crashInfo)
{
}

DumpDataTarget::~DumpDataTarget()
{
}

STDMETHODIMP
DumpDataTarget::QueryInterface(
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
    else if (InterfaceId == IID_ICLRRuntimeLocator)
    {
        *Interface = (ICLRRuntimeLocator*)this;
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
DumpDataTarget::AddRef()
{
    LONG ref = InterlockedIncrement(&m_ref);
    return ref;
}

STDMETHODIMP_(ULONG)
DumpDataTarget::Release()
{
    LONG ref = InterlockedDecrement(&m_ref);
    if (ref == 0)
    {
        delete this;
    }
    return ref;
}

HRESULT STDMETHODCALLTYPE
DumpDataTarget::GetMachineType(
    /* [out] */ ULONG32 *machine)
{
#ifdef HOST_AMD64
    *machine = IMAGE_FILE_MACHINE_AMD64;
#elif HOST_ARM
    *machine = IMAGE_FILE_MACHINE_ARMNT;
#elif HOST_ARM64
    *machine = IMAGE_FILE_MACHINE_ARM64;
#elif HOST_X86
    *machine = IMAGE_FILE_MACHINE_I386;
#elif HOST_LOONGARCH64
    *machine = IMAGE_FILE_MACHINE_LOONGARCH64;
#else
#error Unsupported architecture
#endif
    return S_OK;
}

HRESULT STDMETHODCALLTYPE
DumpDataTarget::GetPointerSize(
    /* [out] */ ULONG32 *size)
{
#if defined(HOST_AMD64) || defined(HOST_ARM64) || defined(HOST_LOONGARCH64)
    *size = 8;
#elif defined(HOST_ARM) || defined(HOST_X86)
    *size = 4;
#else
#error Unsupported architecture
#endif
    return S_OK;
}

HRESULT STDMETHODCALLTYPE
DumpDataTarget::GetImageBase(
    /* [string][in] */ LPCWSTR moduleName,
    /* [out] */ CLRDATA_ADDRESS *baseAddress)
{
    *baseAddress = 0;

    char tempModuleName[MAX_PATH];
    int length = WideCharToMultiByte(CP_ACP, 0, moduleName, -1, tempModuleName, sizeof(tempModuleName), NULL, NULL);
    if (length > 0)
    {
        *baseAddress = m_crashInfo.GetBaseAddressFromName(tempModuleName);
    }

    return *baseAddress != 0 ? S_OK : E_FAIL;
}

HRESULT STDMETHODCALLTYPE
DumpDataTarget::ReadVirtual(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [length_is][size_is][out] */ PBYTE buffer,
    /* [in] */ ULONG32 size,
    /* [optional][out] */ ULONG32 *done)
{
    size_t read = 0;
    if (!m_crashInfo.ReadProcessMemory((void*)(ULONG_PTR)address, buffer, size, &read))
    {
        TRACE("DumpDataTarget::ReadVirtual %p %d FAILED\n", (void*)address, size);
        *done = 0;
        return E_FAIL;
    }
    *done = read;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE
DumpDataTarget::WriteVirtual(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [size_is][in] */ PBYTE buffer,
    /* [in] */ ULONG32 size,
    /* [optional][out] */ ULONG32 *done)
{
    assert(false);
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
DumpDataTarget::GetTLSValue(
    /* [in] */ ULONG32 threadID,
    /* [in] */ ULONG32 index,
    /* [out] */ CLRDATA_ADDRESS* value)
{
    assert(false);
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
DumpDataTarget::SetTLSValue(
    /* [in] */ ULONG32 threadID,
    /* [in] */ ULONG32 index,
    /* [in] */ CLRDATA_ADDRESS value)
{
    assert(false);
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
DumpDataTarget::GetCurrentThreadID(
    /* [out] */ ULONG32* threadID)
{
    assert(false);
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
DumpDataTarget::GetThreadContext(
    /* [in] */ ULONG32 threadID,
    /* [in] */ ULONG32 contextFlags,
    /* [in] */ ULONG32 contextSize,
    /* [out, size_is(contextSize)] */ PBYTE context)
{
    if (contextSize < sizeof(CONTEXT))
    {
        assert(false);
        return E_INVALIDARG;
    }
    memset(context, 0, contextSize);
    for (const ThreadInfo* thread : m_crashInfo.Threads())
    {
        if (thread->Tid() == (pid_t)threadID)
        {
            thread->GetThreadContext(contextFlags, reinterpret_cast<CONTEXT*>(context));
            return S_OK;
        }
    }
    return E_FAIL;
}

HRESULT STDMETHODCALLTYPE
DumpDataTarget::SetThreadContext(
    /* [in] */ ULONG32 threadID,
    /* [in] */ ULONG32 contextSize,
    /* [out, size_is(contextSize)] */ PBYTE context)
{
    assert(false);
    return E_NOTIMPL;
}

HRESULT STDMETHODCALLTYPE
DumpDataTarget::Request(
    /* [in] */ ULONG32 reqCode,
    /* [in] */ ULONG32 inBufferSize,
    /* [size_is][in] */ BYTE *inBuffer,
    /* [in] */ ULONG32 outBufferSize,
    /* [size_is][out] */ BYTE *outBuffer)
{
    assert(false);
    return E_NOTIMPL;
}

// ICLRRuntimeLocator

HRESULT STDMETHODCALLTYPE 
DumpDataTarget::GetRuntimeBase(
    /* [out] */ CLRDATA_ADDRESS* baseAddress)
{
    *baseAddress = m_crashInfo.RuntimeBaseAddress();
    return S_OK;
}
