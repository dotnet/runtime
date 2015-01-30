//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// File: InProcDac.cpp
// 

//
// 
//
//*****************************************************************************

#include "stdafx.h"

#if defined(FEATURE_DBGIPC_TRANSPORT_VM)

#include "inprocdac.h"
#include "dacdbiinterface.h"
#include "cordebug.h"
#include "metadata.h"

InProcDac::InProcDac() :
    m_pDacDbi(NULL),
    m_pUnpacker(NULL)
{
}

InProcDac::~InProcDac()
{
    Cleanup();
}

//
// Debugger::InitializeDAC
// 
// DAC is used in-process on the Mac and ARM devices.
// This is similar to CordbProcess::CreateDacDbiInterface on Windows.
// @dbgtodo : try and share some of this code with the RS equivalent?
// 
void InProcDac::Initialize()
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

	 // don't double-init
    _ASSERTE(m_pDataTarget == NULL);
    _ASSERTE(m_pDacDbi == NULL);
    _ASSERTE(m_pUnpacker == NULL);

    HRESULT hrStatus = S_OK;
    HModuleHolder hDacDll;

    //
    // Load the access DLL from the same directory as the the current CLR DLL.
    //
    WCHAR wszRuntimePath[MAX_PATH];         // base directory of the runtime (including trailing /)
    WCHAR wszAccessDllPath[MAX_PATH];       // full path to the DAC Dll

    if (!WszGetModuleFileName(GetCLRModule(), wszRuntimePath, NumItems(wszRuntimePath)))
    {
        ThrowLastError();
    }

    const char pathSep = '\\';
    
    // remove CLR filename
    PWSTR pPathTail = wcsrchr(wszRuntimePath, pathSep);
    if (!pPathTail)
    {
        ThrowHR(E_INVALIDARG);
    }
    pPathTail[1] = '\0';

    // In the case where this function is called multiple times, save the module handle to the DAC shared
    // library so that we won't try to free and load it multiple times.
    if (m_hDacModule == NULL)
    {
        if (wcscpy_s(wszAccessDllPath, _countof(wszAccessDllPath), wszRuntimePath) ||
            wcscat_s(wszAccessDllPath, _countof(wszAccessDllPath), MAKEDLLNAME_W(MAIN_DAC_MODULE_NAME_W)))
        {
            ThrowHR(E_INVALIDARG);
        }

        hDacDll.Assign(WszLoadLibrary(wszAccessDllPath));
        if (!hDacDll)
        {
            CONSISTENCY_CHECK_MSGF(false,("Unable to find DAC dll: %s", wszAccessDllPath));

            DWORD dwLastError = GetLastError();
            if (dwLastError == ERROR_MOD_NOT_FOUND)
            {
                // Give a more specific error in the case where we can't find the DAC dll.
                ThrowHR(CORDBG_E_DEBUG_COMPONENT_MISSING);
            }
            else
            {
                ThrowWin32(dwLastError);
            }
        }

        // Succeeded. Now copy out.
        m_hDacModule.Assign(hDacDll);
        hDacDll.SuppressRelease();
    }

    // Create the data target
    ReleaseHolder<InProcDataTarget> pDataTarget = new InProcDataTarget();

    //
    // Get the access interface, passing our callback interfaces (data target, and metadata lookup) 
    //

    IDacDbiInterface::IMetaDataLookup * pMetaDataLookup = this;
    IDacDbiInterface::IAllocator * pAllocator = this;

    // Get the CLR instance ID - the base address of the CLR module
    CORDB_ADDRESS clrInstanceId = reinterpret_cast<CORDB_ADDRESS>(GetCLRModule());

    typedef HRESULT (STDAPICALLTYPE * PFN_DacDbiInterfaceInstance)(
        ICorDebugDataTarget *, 
        CORDB_ADDRESS,
        IDacDbiInterface::IAllocator *, 
        IDacDbiInterface::IMetaDataLookup *, 
        IDacDbiInterface **);

    IDacDbiInterface* pInterfacePtr = NULL;
    PFN_DacDbiInterfaceInstance pfnEntry = (PFN_DacDbiInterfaceInstance)
    GetProcAddress(m_hDacModule, "DacDbiInterfaceInstance");

    if (!pfnEntry)
    {
        ThrowLastError();
    }

    hrStatus = pfnEntry(pDataTarget, clrInstanceId,
        				pAllocator, pMetaDataLookup, &pInterfacePtr);
    IfFailThrow(hrStatus);

    // We now have a resource, pInterfacePtr, that needs to be freed.

    m_pDacDbi = pInterfacePtr;   
	m_pDataTarget = pDataTarget.Extract();

    // Enable DAC target consistency checking - we're in-proc and so better always be consistent
    m_pDacDbi->DacSetTargetConsistencyChecks( true );
    m_pUnpacker = new DDUnpack(pInterfacePtr, pAllocator); // throws
}

void InProcDac::Cleanup()
{
    CONTRACTL
    {
        NOTHROW; // backout code.
    }
    CONTRACTL_END;

    if (m_pDacDbi != NULL)
    {
        m_pDacDbi->Destroy();
        m_pDacDbi = NULL;
    }

    if(m_pUnpacker != NULL)
    {
        delete m_pUnpacker;
        m_pUnpacker = NULL;
    }

    if (m_pDataTarget != NULL)
    {
        m_pDataTarget.Clear();
    }

    // Note that once we release this handle, the DAC module can be unloaded and all calls
    // into DAC could be invalid.
    if (m_hDacModule != NULL)
    {
        m_hDacModule.Clear();
    }
}

HRESULT InProcDac::DoRequest(ReadBuffer * pSend, WriteBuffer * pResult)
{
    HRESULT hr = S_OK;

    // Lazily initialize the DacDbiMarshalStub.
    if (m_pDacDbi == NULL)
    {
        EX_TRY
        {
            Initialize();
        }
        EX_CATCH_HRESULT(hr);
        IfFailRet(hr);
    }

    _ASSERTE(m_pDacDbi != NULL);

     /*
     * @dbgtodo : We have to make sure to call Flush whenever runtime data structures may have changed.  
     * Eg:
     *    - after every IPC event
     *    - whenever we suspend the process
     * For now we rely on the RS to tell us when to flush, just like the Windows runtime.  It's a little riskier
     * in this case because the target is actually running code.  Since the cost of copying locally is fairly 
     * low, it is probably best to just flush at the beginning and/or end of all DD requests (i.e. here).
     * Flushing more that necessary may be best for performance.
     * Note however that this could in theory expose lateng bugs where we've been getting away with bleeding
     * DAC state across DD calls on Windows.     
     */   
    EX_TRY
    {
        m_pUnpacker->HandleDDMessage(pSend, pResult);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

#ifndef DACCESS_COMPILE
IMDInternalImport * InProcDac::LookupMetaData(VMPTR_PEFile addressPEFile, bool &isILMetaDataForNGENImage)
{
    isILMetaDataForNGENImage = false;
    PEFile* peFile = addressPEFile.GetRawPtr();
    return peFile->GetPersistentMDImport();
}
#endif
//***************************************************************
// InProcDataTarget implementation
//***************************************************************

//
// InProcDataTarget ctor
// 
// Instantiate an InProcDataTarget 
// 
InProcDac::InProcDataTarget::InProcDataTarget() :
    m_ref(0)
{
}

//
// InProcDataTarget dtor
// 
// 
InProcDac::InProcDataTarget::~InProcDataTarget()
{
}

// Standard impl of IUnknown::QueryInterface
HRESULT STDMETHODCALLTYPE
InProcDac::InProcDataTarget::QueryInterface(
    REFIID InterfaceId,
    PVOID* pInterface)
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
InProcDac::InProcDataTarget::AddRef()
{
    LONG ref = InterlockedIncrement(&m_ref);    
    return ref;
}

// Standard impl of IUnknown::Release
ULONG STDMETHODCALLTYPE
InProcDac::InProcDataTarget::Release()
{    
    LONG ref = InterlockedDecrement(&m_ref);
    if (ref == 0)
    {
        delete this;
    }
    return ref;
}

// impl of interface method ICorDebugDataTarget::GetPlatform
HRESULT STDMETHODCALLTYPE
InProcDac::InProcDataTarget::GetPlatform( 
    CorDebugPlatform * pPlatform)
{
#if defined(_TARGET_X86_)
    *pPlatform = CORDB_PLATFORM_WINDOWS_X86;
#elif defined(_TARGET_AMD64_)
    *pPlatform = CORDB_PLATFORM_WINDOWS_AMD64;
#elif defined(_TARGET_ARM_)
    *pPlatform = CORDB_PLATFORM_WINDOWS_ARM;
#else
#error Unknown Processor.
#endif // platform

    return S_OK;
}

// impl of interface method ICorDebugDataTarget::ReadVirtual
HRESULT STDMETHODCALLTYPE
InProcDac::InProcDataTarget::ReadVirtual( 
    CORDB_ADDRESS address,
    PBYTE pBuffer,
    ULONG32 cbRequestSize,
    ULONG32 * pcbRead)
{
    void * pSrc = reinterpret_cast<void*>(address);
    memcpy(pBuffer, pSrc, cbRequestSize);
    if (pcbRead != NULL)
    {
        *pcbRead = cbRequestSize;
    }
    return S_OK;
}

// impl of interface method ICorDebugMutableDataTarget::WriteVirtual
HRESULT STDMETHODCALLTYPE
InProcDac::InProcDataTarget::WriteVirtual( 
    CORDB_ADDRESS address,
    const BYTE * pBuffer,
    ULONG32 cbRequestSize)
{
    void * pDst = reinterpret_cast<void*>(address);
    memcpy(pDst, pBuffer, cbRequestSize);
    return S_OK;
}


// impl of interface method ICorDebugDataTarget::GetThreadContext
HRESULT STDMETHODCALLTYPE
InProcDac::InProcDataTarget::GetThreadContext(
    DWORD dwThreadID,
    ULONG32 contextFlags,
    ULONG32 contextSize,
    PBYTE   pContext)
{
    if (contextSize < sizeof(CONTEXT))
    {
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

    HandleHolder hThread = ::OpenThread(THREAD_GET_CONTEXT, FALSE, dwThreadID);
    if (hThread == NULL)
    {
        return HRESULT_FROM_GetLastError();
    }

    // This assumes pContext is appropriately aligned.
    CONTEXT * pCtx = reinterpret_cast<CONTEXT*>(pContext);
    pCtx->ContextFlags = contextFlags;
    if (!::GetThreadContext(hThread, pCtx))
    {
        return HRESULT_FROM_GetLastError();
    }

    return S_OK;
}

// impl of interface method ICorDebugMutableDataTarget::SetThreadContext
HRESULT STDMETHODCALLTYPE
InProcDac::InProcDataTarget::SetThreadContext(
    DWORD dwThreadID,
    ULONG32 contextSize,
    const BYTE * pContext)
{
    if (contextSize < sizeof(CONTEXT))
    {
        return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
    }

    HandleHolder hThread = ::OpenThread(THREAD_SET_CONTEXT, FALSE, dwThreadID);
    if (hThread == NULL)
    {
        return HRESULT_FROM_GetLastError();
    }

    // This assumes pContext is appropriately aligned.
    const CONTEXT * pCtx = reinterpret_cast<const CONTEXT*>(pContext);
    if (!::SetThreadContext(hThread,pCtx))
    {
        return HRESULT_FROM_GetLastError();
    }

    return S_OK;
}

// implementation of ICorDebugMutableDataTarget::ContinueStatusChanged
HRESULT STDMETHODCALLTYPE
InProcDac::InProcDataTarget::ContinueStatusChanged(
    DWORD dwThreadId,
    CORDB_CONTINUE_STATUS continueStatus)
{
    return E_NOTIMPL;
}

#ifndef DACCESS_COMPILE

// Trivial implementation for IDacDbiInterface::IAllocator methods
void * InProcDac::Alloc(SIZE_T lenBytes)
{
    return new BYTE[lenBytes];
}

void InProcDac::Free(void * p)
{
    BYTE* pB = static_cast<BYTE*>(p);
    delete[] pB;
}

#endif //!DACCESS_COMPILE

#endif //FEATURE_DBGIPC_TRANSPORT_VM
