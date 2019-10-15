// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "createdump.h"

#define IMAGE_FILE_MACHINE_AMD64             0x8664  // AMD64 (K8)

DumpDataTarget::DumpDataTarget(pid_t pid) :
    m_ref(1),
    m_pid(pid),
    m_fd(-1),
    m_crashInfo(nullptr)
{
}

DumpDataTarget::~DumpDataTarget()
{
    if (m_fd != -1)
    {
        close(m_fd);
        m_fd = -1;
    }
}

bool
DumpDataTarget::Initialize(CrashInfo * crashInfo)
{
    char memPath[128];
    _snprintf_s(memPath, sizeof(memPath), sizeof(memPath), "/proc/%lu/mem", m_pid);

    m_fd = open(memPath, O_RDONLY);
    if (m_fd == -1)
    {
        fprintf(stderr, "open(%s) FAILED %d (%s)\n", memPath, errno, strerror(errno));
        return false;
    }
    m_crashInfo = crashInfo;
    return true;
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
#ifdef _AMD64_
    *machine = IMAGE_FILE_MACHINE_AMD64;
#elif _ARM_
    *machine = IMAGE_FILE_MACHINE_ARMNT;
#elif _ARM64_
    *machine = IMAGE_FILE_MACHINE_ARM64;
#elif _X86_
    *machine = IMAGE_FILE_MACHINE_I386;
#else
#error Unsupported architecture
#endif
    return S_OK;
}

HRESULT STDMETHODCALLTYPE
DumpDataTarget::GetPointerSize(
    /* [out] */ ULONG32 *size)
{
#if defined(_AMD64_) || defined(_ARM64_)
    *size = 8;
#elif defined(_ARM_) || defined(_X86_)
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
    assert(m_crashInfo != nullptr);
    *baseAddress = 0;

    char tempModuleName[MAX_PATH];
    int length = WideCharToMultiByte(CP_ACP, 0, moduleName, -1, tempModuleName, sizeof(tempModuleName), NULL, NULL);
    if (length > 0)
    {
        for (const MemoryRegion& image : m_crashInfo->ModuleMappings()) 
        {
            const char *name = strrchr(image.FileName(), '/');
            if (name != nullptr)
            {
                name++;
            }
            else
            {
                name = image.FileName();
            }
            if (strcmp(name, tempModuleName) == 0)
            {
                *baseAddress = image.StartAddress();
                return S_OK;
            }
        }
    }
    return E_FAIL;
}

HRESULT STDMETHODCALLTYPE
DumpDataTarget::ReadVirtual(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [length_is][size_is][out] */ PBYTE buffer,
    /* [in] */ ULONG32 size,
    /* [optional][out] */ ULONG32 *done)
{
    assert(m_fd != -1);
    ssize_t read = pread64(m_fd, buffer, size, (off64_t)(ULONG_PTR)address);
    if (read == -1)
    {
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
    assert(m_crashInfo != nullptr);
    if (contextSize < sizeof(CONTEXT)) 
    {
        assert(false);
        return E_INVALIDARG;
    }
    memset(context, 0, contextSize);
    for (const ThreadInfo* thread : m_crashInfo->Threads())
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
