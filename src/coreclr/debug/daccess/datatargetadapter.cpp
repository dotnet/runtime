// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// DataTargetAdapter.cpp
//

//
// implementation of compatibility adapter for ICLRDataTarget
//*****************************************************************************

#include "stdafx.h"
#include "datatargetadapter.h"
#include <clrdata.h>
#include "dacimpl.h"

//
// DataTargetAdaptor ctor
//
// Instantiate a DataTargetAdapter over the supplied legacy DataTarget interface.
// Takes a ref on the supplied interface and releases it in our dtor.
//
DataTargetAdapter::DataTargetAdapter(ICLRDataTarget * pLegacyTarget) :
    m_ref(0),
    m_pLegacyTarget(pLegacyTarget)
{
    m_pLegacyTarget->AddRef();
}

//
// DataTargetAdapter dtor
//
// Releases the underlying DataTarget interface
//
DataTargetAdapter::~DataTargetAdapter()
{
    m_pLegacyTarget->Release();
}

// Standard impl of IUnknown::QueryInterface
HRESULT STDMETHODCALLTYPE
DataTargetAdapter::QueryInterface(
    REFIID interfaceId,
    PVOID* pInterface)
{
    if (interfaceId == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown *>(static_cast<ICorDebugDataTarget *>(this));
    }
    else if (interfaceId == IID_ICorDebugDataTarget)
    {
        *pInterface = static_cast<ICorDebugDataTarget *>(this);
    }
    else if (interfaceId == IID_ICorDebugMutableDataTarget)
    {
        // Note that we always implement the mutable interface, even though our underlying target
        // may return E_NOTIMPL for all the functions on this interface.  There is no reliable way
        // to tell apriori whether an ICLRDataTarget instance supports writing or not.
        *pInterface = static_cast<ICorDebugMutableDataTarget *>(this);
    }
    else
    {
        // For ICorDebugDataTarget4 and other interfaces directly implemented by the legacy data target.
        return m_pLegacyTarget->QueryInterface(interfaceId, pInterface);
    }

    AddRef();
    return S_OK;
}

// Standard impl of IUnknown::AddRef
ULONG STDMETHODCALLTYPE
DataTargetAdapter::AddRef()
{
    LONG ref = InterlockedIncrement(&m_ref);
    return ref;
}

// Standard impl of IUnknown::Release
ULONG STDMETHODCALLTYPE
DataTargetAdapter::Release()
{
    SUPPORTS_DAC_HOST_ONLY;
    LONG ref = InterlockedDecrement(&m_ref);
    if (ref == 0)
    {
        delete this;
    }
    return ref;
}

// impl of interface method ICorDebugDataTarget::GetPlatform
HRESULT STDMETHODCALLTYPE
DataTargetAdapter::GetPlatform(
    CorDebugPlatform * pPlatform)
{
    SUPPORTS_DAC_HOST_ONLY;

    // Get the target machine type, and assume it's Windows
    HRESULT hr;

    ULONG32 ulMachineType;
    IfFailRet(m_pLegacyTarget->GetMachineType(&ulMachineType));

    ULONG32 ulExpectedPointerSize;
    CorDebugPlatform platform;

    switch(ulMachineType)
    {
#ifdef TARGET_UNIX
    case IMAGE_FILE_MACHINE_I386:
        ulExpectedPointerSize = 4;
        platform = CORDB_PLATFORM_POSIX_X86;
        break;

    case IMAGE_FILE_MACHINE_AMD64:
        ulExpectedPointerSize = 8;
        platform = CORDB_PLATFORM_POSIX_AMD64;
        break;

    case IMAGE_FILE_MACHINE_ARMNT:
        ulExpectedPointerSize = 4;
        platform = CORDB_PLATFORM_POSIX_ARM;
        break;

    case IMAGE_FILE_MACHINE_ARM64:
        ulExpectedPointerSize = 8;
        platform = CORDB_PLATFORM_POSIX_ARM64;
	break;

    case IMAGE_FILE_MACHINE_IA64:
        _ASSERTE_MSG(false, "Not supported platform.");
        return E_NOTIMPL;

    case IMAGE_FILE_MACHINE_RISCV64:
        ulExpectedPointerSize = 8;
        platform = CORDB_PLATFORM_POSIX_RISCV64;
    break;
#else   // TARGET_UNIX
    case IMAGE_FILE_MACHINE_I386:
        ulExpectedPointerSize = 4;
        platform = CORDB_PLATFORM_WINDOWS_X86;
        break;

    case IMAGE_FILE_MACHINE_AMD64:
        ulExpectedPointerSize = 8;
        platform = CORDB_PLATFORM_WINDOWS_AMD64;
        break;

    case IMAGE_FILE_MACHINE_IA64:
        ulExpectedPointerSize = 8;
        platform = CORDB_PLATFORM_WINDOWS_IA64;
        break;

    case IMAGE_FILE_MACHINE_ARMNT:
        ulExpectedPointerSize = 4;
        platform = CORDB_PLATFORM_WINDOWS_ARM;
        break;

    case IMAGE_FILE_MACHINE_ARM64:
        ulExpectedPointerSize = 8;
        platform = CORDB_PLATFORM_WINDOWS_ARM64;
        break;
#endif  // TARGET_UNIX

    default:
        // No other platforms are current supported
        return E_NOTIMPL;
    }

    // Validate that the target pointer size matches
    ULONG32 ulPointerSize;
    IfFailRet(m_pLegacyTarget->GetPointerSize(&ulPointerSize));

    if (ulPointerSize != ulExpectedPointerSize)
    {
        return E_UNEXPECTED;
    }

    // Found a match
    *pPlatform = platform;
    return S_OK;
}

// impl of interface method ICorDebugDataTarget::ReadVirtual
HRESULT STDMETHODCALLTYPE
DataTargetAdapter::ReadVirtual(
    CORDB_ADDRESS address,
    PBYTE pBuffer,
    ULONG32 cbRequestSize,
    ULONG32 * pcbRead)
{
    SUPPORTS_DAC_HOST_ONLY;
    CLRDATA_ADDRESS cdAddr = TO_CDADDR(address);
    return m_pLegacyTarget->ReadVirtual(cdAddr, pBuffer, cbRequestSize, pcbRead);
}

// impl of interface method ICorDebugMutableDataTarget::WriteVirtual
HRESULT STDMETHODCALLTYPE
DataTargetAdapter::WriteVirtual(
    CORDB_ADDRESS address,
    const BYTE * pBuffer,
    ULONG32 cbRequestSize)
{
    SUPPORTS_DAC_HOST_ONLY;
    CLRDATA_ADDRESS cdAddr = TO_CDADDR(address);
    ULONG32 cbWritten = 0;
    HRESULT hr = S_OK;

    hr = m_pLegacyTarget->WriteVirtual(cdAddr, const_cast<BYTE *>(pBuffer), cbRequestSize, &cbWritten);

    if (SUCCEEDED(hr) && cbWritten != cbRequestSize)
    {
        // This shouldn't happen - existing data target implementations make writes atomic (eg.
        // WriteProcessMemory), even though that isn't strictly required by the old interface.
        // If this does happen, we technically leave the process in an inconsistent state, and we make no
        // attempt to recover from that here.
        _ASSERTE_MSG(false, "Legacy data target WriteVirtual partial write - target left in inconsistent state");
        return HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY);
    }
    return hr;
}


// impl of interface method ICorDebugDataTarget::GetThreadContext
HRESULT STDMETHODCALLTYPE
DataTargetAdapter::GetThreadContext(
    DWORD dwThreadID,
    ULONG32 contextFlags,
    ULONG32 contextSize,
    PBYTE   pContext)
{
    SUPPORTS_DAC_HOST_ONLY;
    return m_pLegacyTarget->GetThreadContext(dwThreadID, contextFlags, contextSize, pContext);
}

// impl of interface method ICorDebugMutableDataTarget::SetThreadContext
HRESULT STDMETHODCALLTYPE
DataTargetAdapter::SetThreadContext(
    DWORD dwThreadID,
    ULONG32 contextSize,
    const BYTE * pContext)
{
    SUPPORTS_DAC_HOST_ONLY;
    return m_pLegacyTarget->SetThreadContext(dwThreadID, contextSize, const_cast<BYTE *>(pContext));
}

// implementation of ICorDebugMutableDataTarget::ContinueStatusChanged
HRESULT STDMETHODCALLTYPE
DataTargetAdapter::ContinueStatusChanged(
    DWORD dwThreadId,
    CORDB_CONTINUE_STATUS continueStatus)
{
    SUPPORTS_DAC_HOST_ONLY;
    // No corresponding API in pre-arrowhead ICLRDataTarget* interfaces.
    // Note that we briefly had a ICLRDataTarget4 with this API, but this was never released outside the CLR so
    // all existing implementations should now be gone.
    return E_NOTIMPL;
}
