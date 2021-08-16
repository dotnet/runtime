// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: ReadOnlyDataTargetFacade.inl
//

//
//*****************************************************************************


//---------------------------------------------------------------------------------------
//
// Ctor for ReadOnlyDataTargetFacade. Just initializes ref count to 0.
//
//---------------------------------------------------------------------------------------
ReadOnlyDataTargetFacade::ReadOnlyDataTargetFacade()
    : m_ref(0)
{
}

// Standard impl of IUnknown::QueryInterface
HRESULT STDMETHODCALLTYPE
ReadOnlyDataTargetFacade::QueryInterface(
    REFIID InterfaceId,
    PVOID* pInterface
    )
{
    if (InterfaceId == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown *>(static_cast<ICorDebugDataTarget *>(this));
    }
    else if (InterfaceId == IID_ICorDebugDataTarget)
    {
        *pInterface = static_cast<ICorDebugDataTarget *>(this);
    }
    else if (InterfaceId == IID_ICorDebugMutableDataTarget)
    {
        *pInterface = static_cast<ICorDebugMutableDataTarget *>(this);
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

// Standard impl of IUnknown::AddRef
ULONG STDMETHODCALLTYPE
ReadOnlyDataTargetFacade::AddRef()
{
    SUPPORTS_DAC;
    LONG ref = InterlockedIncrement(&m_ref);
    return ref;
}

// Standard impl of IUnknown::Release
ULONG STDMETHODCALLTYPE
ReadOnlyDataTargetFacade::Release()
{
    SUPPORTS_DAC;
    LONG ref = InterlockedDecrement(&m_ref);
    if (ref == 0)
    {
        delete this;
    }
    return ref;
}

// impl of interface method ICorDebugDataTarget::GetPlatform
HRESULT STDMETHODCALLTYPE
ReadOnlyDataTargetFacade::GetPlatform(
        CorDebugPlatform *pPlatform)
{
    SUPPORTS_DAC;
    _ASSERTE_MSG(false, "Unexpected call to read-API on read-only DataTarget facade");
    return E_UNEXPECTED;
}

// impl of interface method ICorDebugDataTarget::ReadVirtual
HRESULT STDMETHODCALLTYPE
ReadOnlyDataTargetFacade::ReadVirtual(
    CORDB_ADDRESS address,
    PBYTE pBuffer,
    ULONG32 cbRequestSize,
    ULONG32 *pcbRead)
{
    SUPPORTS_DAC;
    _ASSERTE_MSG(false, "Unexpected call to read-API on read-only DataTarget facade");
    return E_UNEXPECTED;
}

// impl of interface method ICorDebugDataTarget::GetThreadContext
HRESULT STDMETHODCALLTYPE
ReadOnlyDataTargetFacade::GetThreadContext(
    DWORD dwThreadID,
    ULONG32 contextFlags,
    ULONG32 contextSize,
    BYTE *  pContext)
{
    SUPPORTS_DAC;
    _ASSERTE_MSG(false, "Unexpected call to read-API on read-only DataTarget facade");
    return E_UNEXPECTED;
}

// impl of interface method ICorDebugMutableDataTarget::WriteVirtual
HRESULT STDMETHODCALLTYPE
ReadOnlyDataTargetFacade::WriteVirtual(
    CORDB_ADDRESS pAddress,
    const BYTE * pBuffer,
    ULONG32 cbRequestSize)
{
    SUPPORTS_DAC;
    return CORDBG_E_TARGET_READONLY;
}

// impl of interface method ICorDebugMutableDataTarget::SetThreadContext
HRESULT STDMETHODCALLTYPE
ReadOnlyDataTargetFacade::SetThreadContext(
    DWORD dwThreadID,
    ULONG32 contextSize,
    const BYTE * pContext)
{
    SUPPORTS_DAC;
    return CORDBG_E_TARGET_READONLY;
}

// Public implementation of ICorDebugMutableDataTarget::ContinueStatusChanged
HRESULT STDMETHODCALLTYPE
ReadOnlyDataTargetFacade::ContinueStatusChanged(
    DWORD dwThreadId,
    CORDB_CONTINUE_STATUS dwContinueStatus)
{
    SUPPORTS_DAC;
    return CORDBG_E_TARGET_READONLY;
}
