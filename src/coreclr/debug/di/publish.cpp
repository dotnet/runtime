// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: publish.cpp
//

//
//*****************************************************************************


#include "stdafx.h"
#ifdef FEATURE_DBG_PUBLISH

#include "check.h"

#include <tlhelp32.h>
#include "wtsapi32.h"

#ifndef SM_REMOTESESSION
#define SM_REMOTESESSION 0x1000
#endif

#include "corpriv.h"
#include "../../dlls/mscorrc/resource.h"
#include <limits.h>

// Publish shares header files with the rest of ICorDebug.
// ICorDebug should not call ReadProcessMemory & other APIs directly, it should instead go through
// the Data-target. ICD headers #define these APIs to help enforce this.
// Since Publish is separate and doesn't use data-targets, it can access the APIs directly.
// see code:RSDebuggingInfo#UseDataTarget
#undef ReadProcessMemory

//****************************************************************************
//************ App Domain Publishing Service API Implementation **************
//****************************************************************************

// This function enumerates all the process in the system and returns
// their PIDs
BOOL GetAllProcessesInSystem(DWORD *ProcessId,
                             DWORD dwArraySize,
                             DWORD *pdwNumEntries)
{
    HandleHolder hSnapshotHolder;

#if !defined(FEATURE_CORESYSTEM)
    // Load the dll "kernel32.dll".
    HModuleHolder hDll = WszLoadLibrary(W("kernel32"));
    _ASSERTE(hDll != NULL);

    if (hDll == NULL)
    {
        LOG((LF_CORDB, LL_INFO1000,
                "Unable to load the dll for enumerating processes. "
                "LoadLibrary (kernel32.dll) failed.\n"));
        return FALSE;
    }
#else
	// Load the dll "api-ms-win-obsolete-kernel32-l1-1-0.dll".
    HModuleHolder hDll = WszLoadLibrary(W("api-ms-win-obsolete-kernel32-l1-1-0.dll"));
    _ASSERTE(hDll != NULL);

    if (hDll == NULL)
    {
        LOG((LF_CORDB, LL_INFO1000,
                "Unable to load the dll for enumerating processes. "
                "LoadLibrary (api-ms-win-obsolete-kernel32-l1-1-0.dll) failed.\n"));
        return FALSE;
    }
#endif


    // Create the Process' Snapshot
    // Get the pointer to the requested function
    FARPROC pProcAddr = GetProcAddress(hDll, "CreateToolhelp32Snapshot");

    // If the proc address was not found, return error
    if (pProcAddr == NULL)
    {
        LOG((LF_CORDB, LL_INFO1000,
                "Unable to enumerate processes in the system. "
                "GetProcAddr (CreateToolhelp32Snapshot) failed.\n"));
        return FALSE;
    }



    // Handle from CreateToolHelp32Snapshot must be freed via CloseHandle().
    typedef HANDLE CREATETOOLHELP32SNAPSHOT(DWORD, DWORD);

    HANDLE hSnapshot =
            ((CREATETOOLHELP32SNAPSHOT *)pProcAddr)(TH32CS_SNAPPROCESS, NULL);

    if (hSnapshot == INVALID_HANDLE_VALUE)
    {
        LOG((LF_CORDB, LL_INFO1000,
                "Unable to create snapshot of processes in the system. "
                "CreateToolhelp32Snapshot() failed.\n"));
        return FALSE;
    }
    // HandleHolder doesn't deal with INVALID_HANDLE_VALUE, so we only assign if we have a legal value.
    hSnapshotHolder.Assign(hSnapshot);

    // Get the first process in the process list
    // Get the pointer to the requested function
    pProcAddr = GetProcAddress(hDll, "Process32First");

    // If the proc address was not found, return error
    if (pProcAddr == NULL)
    {
        LOG((LF_CORDB, LL_INFO1000,
                "Unable to enumerate processes in the system. "
                "GetProcAddr (Process32First) failed.\n"));
        return FALSE;
    }

    PROCESSENTRY32  PE32;

    // need to initialize the dwSize field before calling Process32First
    PE32.dwSize = sizeof (PROCESSENTRY32);

    typedef BOOL PROCESS32FIRST(HANDLE, LPPROCESSENTRY32);

    BOOL succ =
            ((PROCESS32FIRST *)pProcAddr)(hSnapshot, &PE32);

    if (succ != TRUE)
    {
        LOG((LF_CORDB, LL_INFO1000,
                "Unable to create snapshot of processes in the system. "
                "Process32First() returned FALSE.\n"));
        return FALSE;
    }


    // Loop over and get all the remaining processes
    // Get the pointer to the requested function
    pProcAddr = GetProcAddress(hDll, "Process32Next");

    // If the proc address was not found, return error
    if (pProcAddr == NULL)
    {
        LOG((LF_CORDB, LL_INFO1000,
                "Unable to enumerate processes in the system. "
                "GetProcAddr (Process32Next) failed.\n"));
        return FALSE;
    }

    typedef BOOL PROCESS32NEXT(HANDLE, LPPROCESSENTRY32);

    int iIndex = 0;

    do
    {
        ProcessId [iIndex++] = PE32.th32ProcessID;

        succ = ((PROCESS32NEXT *)pProcAddr)(hSnapshot, &PE32);

    } while ((succ == TRUE) && (iIndex < (int)dwArraySize));

    // I would like to know if we're running more than 512 processes on Win95!!
    _ASSERTE (iIndex < (int)dwArraySize);

    *pdwNumEntries = iIndex;

    // If we made it this far, we succeeded
    return TRUE;
}


// We never want to wait infinite on an object that we can't verify.
// Wait with a timeout.
const DWORD SAFETY_TIMEOUT = 2000;

// ******************************************
// CorpubPublish
// ******************************************

CorpubPublish::CorpubPublish()
    : CordbCommonBase(0),
    m_fpGetModuleFileNameEx(NULL)
{
    // Try to get psapi!GetModuleFileNameExW once, and then every process object can use it.
    // If we can't get it, then we'll fallback to getting information from the IPC block.
#if !defined(FEATURE_CORESYSTEM)
    m_hPSAPIdll = WszLoadLibrary(W("psapi.dll"));
#else
	m_hPSAPIdll = WszLoadLibrary(W("api-ms-win-obsolete-psapi-l1-1-0.dll"));
#endif

    if (m_hPSAPIdll != NULL)
    {
        m_fpGetModuleFileNameEx = (FPGetModuleFileNameEx*) GetProcAddress(m_hPSAPIdll, "GetModuleFileNameExW");
    }

    CordbCommonBase::InitializeCommon();
}

CorpubPublish::~CorpubPublish()
{
    // m_hPSAPIdll is a module holder, so the dtor will free it automatically for us.
}


COM_METHOD CorpubPublish::QueryInterface(REFIID id, void **ppInterface)
{
    if (id == IID_ICorPublish)
        *ppInterface = (ICorPublish*)this;
    else if (id == IID_IUnknown)
        *ppInterface = (IUnknown*)(ICorPublish*)this;
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}


COM_METHOD CorpubPublish::EnumProcesses(COR_PUB_ENUMPROCESS Type,
                                        ICorPublishProcessEnum **ppIEnum)
{
    HRESULT hr = E_FAIL;
    CorpubProcess* pProcessList = NULL  ;
    CorpubProcessEnum* pProcEnum = NULL;
    *ppIEnum = NULL;

    if( Type != COR_PUB_MANAGEDONLY )
    {
        hr = E_INVALIDARG;
        goto exit;
    }

    // call function to get PIDs for all processes in the system
#define MAX_PROCESSES  512

    DWORD ProcessId[MAX_PROCESSES];
    DWORD dwNumProcesses = 0;
    if( !GetAllProcessesInSystem(ProcessId, MAX_PROCESSES, &dwNumProcesses) )
    {
        hr = E_FAIL;
        goto exit;
    }

    // iterate over all the processes to fetch all the managed processes
    for (int i = 0; i < (int)dwNumProcesses; i++)
    {
        CorpubProcess *pProcess = NULL;
        hr = GetProcessInternal( ProcessId[i], &pProcess );
        if( FAILED(hr) )
        {
            _ASSERTE( pProcess == NULL );
            goto exit;      // a serious error has occurred, abort
        }

        if( hr == S_OK )
        {
            // Success, Add the process to the list.
            _ASSERTE( pProcess != NULL );
            pProcess->SetNext( pProcessList );
            pProcessList = pProcess;
        }
        else
        {
            // Ignore this process (isn't managed, or shut down, etc.)
            _ASSERTE( pProcess == NULL );
        }
    }

    // create and return the ICorPublishProcessEnum
    pProcEnum = new (nothrow) CorpubProcessEnum(pProcessList);
    if (pProcEnum == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto exit;
    }
    pProcEnum->AddRef();

    hr = pProcEnum->QueryInterface(IID_ICorPublishProcessEnum, (void**)ppIEnum);
    if( FAILED(hr) )
    {
        goto exit;
    }

    hr = S_OK;

exit:
    // release our handle on the process objects
    while (pProcessList != NULL)
    {
        CorpubProcess *pTmp = pProcessList;
        pProcessList = pProcessList->GetNextProcess();
        pTmp->Release();
    }
    if( pProcEnum != NULL )
    {
        pProcEnum->Release();
        pProcEnum = NULL;
    }

    return hr;
}


HRESULT CorpubPublish::GetProcess(unsigned pid,
                                  ICorPublishProcess **ppProcess)
{
    *ppProcess = NULL;

    // Query for this specific process (even if we've already handed out a
    // now-stale process object for this pid)
    CorpubProcess * pProcess = NULL;
    HRESULT hr = GetProcessInternal( pid, &pProcess );
    if( hr != S_OK )
    {
        // Couldn't get this process (doesn't exist, or isn't managed)
        _ASSERTE( pProcess == NULL );
        if( FAILED(hr) )
        {
            return hr;      // there was a serious error trying to get this process info
        }
        return E_INVALIDARG;  // this process doesn't exist, isn't managed or is shutting down
    }

    // QI to ICorPublishProcess and return it
    _ASSERTE( pProcess != NULL );
    hr = pProcess->QueryInterface(IID_ICorPublishProcess, (void**)ppProcess);
    pProcess->Release();
    return hr;
}


// Attempts to create a CorpubProcess object for a specific managed process
// On success returns S_OK and sets ppProcess to a new AddRef'd CorpubProcess
// object.  Otherwise, returns S_FALSE if the process isn't managed or if it has
// terminated (i.e. it should be ignored), or and error code on a serious failure.
HRESULT CorpubPublish::GetProcessInternal(
                                    unsigned pid,
                                    CorpubProcess **ppProcess )
{
#if defined(FEATURE_DBGIPC_TRANSPORT_DI)
    return E_NOTIMPL;

#else // !FEATURE_DBGIPC_TRANSPORT_DI
    HRESULT hr = S_OK;
    *ppProcess = NULL;

    NewHolder<IPCReaderInterface> pIPCReader( new (nothrow) IPCReaderInterface() );
    if (pIPCReader == NULL)
    {
        LOG((LF_CORDB, LL_INFO100, "CP::EP: Failed to allocate memory for IPCReaderInterface.\n"));
        return E_OUTOFMEMORY;
    }

    // See if it is a managed process by trying to open the shared
    // memory block.
    hr = pIPCReader->OpenLegacyPrivateBlockTempV4OnPid(pid);
    if (FAILED(hr))
    {
        return S_FALSE;     // Not a managed process
    }

    // Get the AppDomainIPCBlock
    AppDomainEnumerationIPCBlock *pAppDomainCB = pIPCReader->GetAppDomainBlock();
    if (pAppDomainCB == NULL)
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::EP: Failed to obtain AppDomainIPCBlock.\n"));
        return S_FALSE;
    }

    // Get the process handle.
    HANDLE hProcess = OpenProcess((PROCESS_VM_READ |
                                   PROCESS_QUERY_INFORMATION |
                                   PROCESS_DUP_HANDLE |
                                   SYNCHRONIZE),
                                  FALSE, pid);
    if (hProcess == NULL)
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::EP: OpenProcess() returned NULL handle.\n"));
        return S_FALSE;
    }

    // If the mutex isn't filled in, the CLR is either starting up or shutting down
    if (pAppDomainCB->m_hMutex == NULL)
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::EP: IPC block isn't properly filled in.\n"));
        return S_FALSE;
    }

    // Dup the valid mutex handle into this process.
    HANDLE hMutex;
    if( !pAppDomainCB->m_hMutex.DuplicateToLocalProcess(hProcess, &hMutex) )
    {
        return S_FALSE;
    }

    // Acquire the mutex, only waiting two seconds.
    // We can't actually gaurantee that the target put a mutex object in here.
    DWORD dwRetVal = WaitForSingleObject(hMutex, SAFETY_TIMEOUT);

    if (dwRetVal == WAIT_OBJECT_0)
    {
        // Make sure the mutex handle is still valid. If
        // its not, then we lost a shutdown race.
        if (pAppDomainCB->m_hMutex == NULL)
        {
            LOG((LF_CORDB, LL_INFO1000, "CP::EP: lost shutdown race, skipping...\n"));

            ReleaseMutex(hMutex);
            CloseHandle(hMutex);
            return S_FALSE;
        }
    }
    else
    {
        // Again, landing here is most probably a shutdown race. Its okay, though...
        LOG((LF_CORDB, LL_INFO1000, "CP::EP: failed to get IPC mutex.\n"));

        if (dwRetVal == WAIT_ABANDONED)
        {
            ReleaseMutex(hMutex);
        }
        CloseHandle(hMutex);
        return S_FALSE;
    }
    // Beware: if the target pid is not properly honoring the mutex, the data in the
    // IPC block may still shift underneath us.

    // If we get here, then hMutex is held by this process.

    // Now create the CorpubProcess object for the ProcessID
    CorpubProcess *pProc = new (nothrow) CorpubProcess(pid,
                                           true,
                                           hProcess,
                                           hMutex,
                                           pAppDomainCB,
                                           pIPCReader,
                                           m_fpGetModuleFileNameEx);

    // Release our lock on the IPC block.
    ReleaseMutex(hMutex);

    if (pProc == NULL)
    {
        return E_OUTOFMEMORY;
    }
    pIPCReader.SuppressRelease();

    // Success, return the Process object
    pProc->AddRef();
    *ppProcess = pProc;
    return S_OK;

#endif // FEATURE_DBGIPC_TRANSPORT_DI
}



// ******************************************
// CorpubProcess
// ******************************************

// Constructor
CorpubProcess::CorpubProcess(DWORD dwProcessId,
                             bool fManaged,
                             HANDLE hProcess,
                             HANDLE hMutex,
                             AppDomainEnumerationIPCBlock *pAD,
#if !defined(FEATURE_DBGIPC_TRANSPORT_DI)
                             IPCReaderInterface *pIPCReader,
#endif // !FEATURE_DBGIPC_TRANSPORT_DI
                             FPGetModuleFileNameEx * fpGetModuleFileNameEx)
    : CordbCommonBase(0, enumCorpubProcess),
      m_dwProcessId(dwProcessId),
      m_fIsManaged(fManaged),
      m_hProcess(hProcess),
      m_hMutex(hMutex),
      m_AppDomainCB(pAD),
#if !defined(FEATURE_DBGIPC_TRANSPORT_DI)
      m_pIPCReader(pIPCReader),
#endif // !FEATURE_DBGIPC_TRANSPORT_DI
      m_pNext(NULL)
{
    {
        // First try to get the process name from the OS. That can't be spoofed by badly formed IPC block.
        // psapi!GetModuleFileNameExW can get that, but it's not available on all platforms so we
        // need to load it dynamically.
        if (fpGetModuleFileNameEx != NULL)
        {
            // MSDN is very confused about whether the lenght is in bytes (MSDN 2002) or chars (MSDN 2004).
            // We err on the safe side by having buffer that's twice as large, and ignoring
            // the units on the return value.
            WCHAR szName[MAX_LONGPATH * sizeof(WCHAR)];

            DWORD lenInCharsOrBytes = MAX_LONGPATH*sizeof(WCHAR);

            // Pass NULL module handle to get "Main Module", which will give us the process name.
            DWORD ret = (*fpGetModuleFileNameEx) (hProcess, NULL, szName, lenInCharsOrBytes);
            if (ret > 0)
            {
                // Recompute string length because we don't know if 'ret' is in bytes or char.
                SIZE_T len = wcslen(szName) + 1;
                m_szProcessName = new (nothrow) WCHAR[len];
                if (m_szProcessName != NULL)
                {
                    wcscpy_s(m_szProcessName, len, szName);
                    goto exit;
                }
            }
        }

        // This is a security feature on WinXp + above, so make sure it worked there.
        CONSISTENCY_CHECK_MSGF(FALSE, ("On XP/2k03 OSes + above, we should have been able to get\n"
            "the module name from psapi!GetModuleFileNameEx. fp=0x%p\n.", fpGetModuleFileNameEx));
    }
    // We couldn't get it from the OS, so fallthrough to getting it from the IPC block.

    // Fetch the process name from the AppDomainIPCBlock
    _ASSERTE (pAD->m_szProcessName != NULL);

    if (pAD->m_szProcessName == NULL)
        m_szProcessName = NULL;
    else
    {
        SIZE_T nBytesRead;

        _ASSERTE(pAD->m_iProcessNameLengthInBytes > 0);

        // Note: this assumes we're reading the null terminator from
        // the IPC block.
        m_szProcessName = (WCHAR*) new (nothrow) char[pAD->m_iProcessNameLengthInBytes];

        if (m_szProcessName == NULL)
        {
            LOG((LF_CORDB, LL_INFO1000,
             "CP::CP: Failed to allocate memory for ProcessName.\n"));

            goto exit;
        }

        BOOL bSucc = ReadProcessMemory(hProcess,
                                        pAD->m_szProcessName,
                                        m_szProcessName,
                                        pAD->m_iProcessNameLengthInBytes,
                                        &nBytesRead);

        if ((bSucc == 0) ||
            (nBytesRead != (SIZE_T)pAD->m_iProcessNameLengthInBytes))
        {
            // The EE may have done a rude exit
            LOG((LF_CORDB, LL_INFO1000,
             "CP::EAD: ReadProcessMemory (ProcessName) failed.\n"));
        }
    }

exit:
    ;
}

CorpubProcess::~CorpubProcess()
{
    delete [] m_szProcessName;
#if !defined(FEATURE_DBGIPC_TRANSPORT_DI)
    delete m_pIPCReader;
#endif // !FEATURE_DBGIPC_TRANSPORT_DI
    CloseHandle(m_hProcess);
    CloseHandle(m_hMutex);
}


HRESULT CorpubProcess::QueryInterface(REFIID id, void **ppInterface)
{
    if (id == IID_ICorPublishProcess)
        *ppInterface = (ICorPublishProcess*)this;
    else if (id == IID_IUnknown)
        *ppInterface = (IUnknown*)(ICorPublishProcess*)this;
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}


// Helper to tell if this process has exited.
bool CorpubProcess::IsExited()
{
    DWORD res = WaitForSingleObject(this->m_hProcess, 0);
    return (res == WAIT_OBJECT_0);
}


HRESULT CorpubProcess::IsManaged(BOOL *pbManaged)
{
    *pbManaged = (m_fIsManaged == true) ? TRUE : FALSE;

    return S_OK;
}

// Helper.
// Allocates a local buffer (using 'new') and fills it by copying it from remote memory.
// Returns:
// - on success, S_OK, *ppNewLocalBuffer points to a newly allocated buffer containing
//               the full copy from remote memoy. Caller must use 'delete []' to free this.
// - on failure, a failing HR. No memory is allocated.
HRESULT AllocateAndReadRemoteBuffer(
    HANDLE hProcess,
    void * pRemotePtr,
    SIZE_T cbSize, // size of buffer to allocate + copy.
    BYTE * * ppNewLocalBuffer
)
{
    _ASSERTE(ppNewLocalBuffer != NULL);
    *ppNewLocalBuffer = NULL;


    if (pRemotePtr == NULL)
    {
        return E_INVALIDARG;
    }

    BYTE *pLocalBuffer = new (nothrow) BYTE[cbSize];

    if (pLocalBuffer == NULL)
    {
        _ASSERTE(!"Failed to alloc memory. Likely size is bogusly large, perhaps from an attacker.");
        return E_OUTOFMEMORY;
    }

    SIZE_T nBytesRead;

    // Need to read in the remote process' memory
    BOOL bSucc = ReadProcessMemory(hProcess,
                                    pRemotePtr,
                                    pLocalBuffer, cbSize,
                                    &nBytesRead);

    if ((bSucc == 0) || (nBytesRead != cbSize))
    {
        // The EE may have done a rude exit
        delete [] pLocalBuffer;
        return E_FAIL;
    }

    *ppNewLocalBuffer = pLocalBuffer;
    return S_OK;
}

// Wrapper around AllocateAndReadRemoteBuffer,
// to ensure that we're reading an remote-null terminated string.
// Ensures that string is null-terminated.
HRESULT AllocateAndReadRemoteString(
    HANDLE hProcess,
    void * pRemotePtr,
    SIZE_T cbSize, // size of buffer to allocate + copy.
    _Outptr_result_bytebuffer_(cbSize) WCHAR * * ppNewLocalBuffer
    )
{
    // Make sure buffer has right geometry.
    if (cbSize < 0)
    {
        return E_INVALIDARG;
    }

    // If it's not on a WCHAR boundary, then we may have a 1-byte buffer-overflow.
    SIZE_T ceSize = cbSize / sizeof(WCHAR);
    if ((ceSize * sizeof(WCHAR)) != cbSize)
    {
        return E_INVALIDARG;
    }

    // It should at least have 1 char for the null terminator.
    if (ceSize < 1)
    {
        return E_INVALIDARG;
    }


    HRESULT hr = AllocateAndReadRemoteBuffer(hProcess, pRemotePtr, cbSize, (BYTE**) ppNewLocalBuffer);
    if (SUCCEEDED(hr))
    {
        // Ensure that the string we just read is actually null terminated.
        // We can't call wcslen() on it yet, since that may AV on a non-null terminated string.
        WCHAR * pString = *ppNewLocalBuffer;

        if (pString[ceSize - 1] == W('\0'))
        {
            // String is null terminated.
            return S_OK;
        }
        pString[ceSize - 1] = W('\0');

        SIZE_T ceTestLen = wcslen(pString);
        if (ceTestLen == ceSize - 1)
        {
            // String was not previously null-terminated.
            delete [] ppNewLocalBuffer;
            return E_INVALIDARG;
        }
    }
    return S_OK;
}

//
// Enumerate the list of known application domains in the target process.
//
HRESULT CorpubProcess::EnumAppDomains(ICorPublishAppDomainEnum **ppIEnum)
{
    VALIDATE_POINTER_TO_OBJECT(ppIEnum, ICorPublishAppDomainEnum **);
    *ppIEnum = NULL;

    int i;

    HRESULT hr = S_OK;
    WCHAR *pAppDomainName = NULL;
    CorpubAppDomain *pAppDomainHead = NULL;

    // Lock the IPC block:
    // We can't trust any of the data in the IPC block (including our own mutex handle),
    // because we don't want bugs in the debuggee escalating into bugs in the debugger.
    DWORD res = WaitForSingleObject(m_hMutex, SAFETY_TIMEOUT);

    if (res == WAIT_TIMEOUT)
    {
        // This should only happen if the target process is illbehaved.
        return CORDBG_E_TIMEOUT;
    }

    // If the process has gone away, or if it has cleared out its control block, then
    // we've lost the race to access this process before it is terminated.
    // Note that if the EE does a rude process exit, it won't have cleared the control block so there
    // will be a small race window.
    if (this->IsExited() || this->m_AppDomainCB->m_hMutex == NULL )
    {
        // This is the common case. A process holding the mutex shouldn't normally exit,
        // but once it releases the mutex, it may exit asynchronously.
        return CORDBG_E_PROCESS_TERMINATED;
    }

    if (res == WAIT_FAILED)
    {
        // This should be the next most common failure case
        return HRESULT_FROM_GetLastError();
    }

    if (res != WAIT_OBJECT_0)
    {
        // Catch all other possible failures
        return E_FAIL;
    }

    int iAppDomainCount = 0;
    AppDomainInfo *pADI = NULL;

    // Make a copy of the IPC block so that we can gaurantee that it's not changing on us.
    AppDomainEnumerationIPCBlock tempBlock;
    memcpy(&tempBlock, m_AppDomainCB, sizeof(tempBlock));

    // Allocate memory to read the remote process' memory into
    const SIZE_T cbADI = tempBlock.m_iSizeInBytes;

    // It's possible the process will not have any appdomains.
    if ((tempBlock.m_rgListOfAppDomains == NULL) != (tempBlock.m_iSizeInBytes == 0))
    {
        _ASSERTE(!"Inconsistent IPC block in publish.");
        hr = E_FAIL;
        goto exit;
    }

    // All the data in the IPC block is signed integers. They should never be negative,
    // so check that now.
    if ((tempBlock.m_iTotalSlots < 0) ||
        (tempBlock.m_iNumOfUsedSlots < 0) ||
        (tempBlock.m_iLastFreedSlot < 0) ||
        (tempBlock.m_iSizeInBytes < 0) ||
        (tempBlock.m_iProcessNameLengthInBytes < 0))
    {
        hr = E_FAIL;
        goto exit;
    }

    // Check other invariants.
    if (cbADI != tempBlock.m_iTotalSlots * sizeof(AppDomainInfo))
    {
        _ASSERTE(!"Size mismatch");
        hr = E_FAIL;
        goto exit;
    }

    hr = AllocateAndReadRemoteBuffer(m_hProcess, tempBlock.m_rgListOfAppDomains, cbADI, (BYTE**) &pADI);
    if (FAILED(hr))
    {
        goto exit;
    }
    _ASSERTE(pADI != NULL);

    // Collect all the AppDomain info info a list of CorpubAppDomains
    for (i = 0; i < tempBlock.m_iTotalSlots; i++)
    {
        if (!pADI[i].IsEmpty())
        {
            // Should be positive, and at least have a null-terminator character.
            if (pADI[i].m_iNameLengthInBytes <= 1)
            {
                hr = E_INVALIDARG;
                goto exit;
            }
            hr = AllocateAndReadRemoteString(m_hProcess,
                (void*) pADI[i].m_szAppDomainName, pADI[i].m_iNameLengthInBytes, // remote string + size in bytes
                &pAppDomainName);
            if (FAILED(hr))
            {
                goto exit;
            }

            // create a new AppDomainObject. This will take ownership of pAppDomainName.
            // We know the string is a well-formed null-terminated string,
            // but beyond that, we can't verify that the data is actually truthful.
            CorpubAppDomain *pCurrentAppDomain = new (nothrow) CorpubAppDomain(pAppDomainName,
                                                            pADI[i].m_id);

            if (pCurrentAppDomain == NULL)
            {
                LOG((LF_CORDB, LL_INFO1000,
                 "CP::EAD: Failed to allocate memory for CorpubAppDomain.\n"));

                hr = E_OUTOFMEMORY;
                goto exit;
            }

            // Since CorpubAppDomain now owns pAppDomain's memory, we don't worry about freeing it.
            pAppDomainName = NULL;

            // Add the appdomain to the list.
            pCurrentAppDomain->SetNext(pAppDomainHead);
            pAppDomainHead = pCurrentAppDomain;

            // Shortcut to opt out of reading the rest of the array if it's empty.
            if (++iAppDomainCount >= tempBlock.m_iNumOfUsedSlots)
                break;
        }
    }

    {
        _ASSERTE ((iAppDomainCount >= tempBlock.m_iNumOfUsedSlots)
                  && (i <= tempBlock.m_iTotalSlots));

        // create and return the ICorPublishAppDomainEnum object, handing off the AppDomain list to it
        CorpubAppDomainEnum *pTemp = new (nothrow) CorpubAppDomainEnum(pAppDomainHead);

        if (pTemp == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto exit;
        }

        pAppDomainHead = NULL;      // handed off AppDomain list to enum, don't delete below

        hr = pTemp->QueryInterface(IID_ICorPublishAppDomainEnum,
                                   (void **)ppIEnum);
    }

exit:
    ReleaseMutex(m_hMutex);

    // If we didn't hand off the AppDomain objects, delete them
    while( pAppDomainHead != NULL )
    {
        CorpubAppDomain *pTemp = pAppDomainHead;
        pAppDomainHead = pAppDomainHead->GetNextAppDomain();
        delete pTemp;
    }

    if (pADI != NULL)
        delete[] pADI;

    if (pAppDomainName != NULL)
        delete [] pAppDomainName;

    // Either we succeeded && provided an enumerator; or we failed and didn't provide an enum.
    _ASSERTE(SUCCEEDED(hr) == (*ppIEnum != NULL));
    return hr;
}

/*
 * Returns the OS ID for the process in question.
 */
HRESULT CorpubProcess::GetProcessID(unsigned *pid)
{
    *pid = m_dwProcessId;

    return S_OK;
}

/*
 * Get the display name for a process.
 */
HRESULT CorpubProcess::GetDisplayName(ULONG32 cchName,
                                      ULONG32 *pcchName,
                                      _Out_writes_to_opt_(cchName, *pcchName) WCHAR szName[])
{
    VALIDATE_POINTER_TO_OBJECT_ARRAY_OR_NULL(szName, WCHAR, cchName, true, true);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pcchName, ULONG32 *);

    // Reasonable defaults
    if (szName)
        *szName = 0;

    if (pcchName)
        *pcchName = 0;

    const WCHAR *szTempName = m_szProcessName;

    // In case we didn't get the name (most likely out of memory on ctor).
    if (!szTempName)
        szTempName = W("<unknown>");

    return CopyOutString(szTempName, cchName, pcchName, szName);
}


// ******************************************
// CorpubAppDomain
// ******************************************

CorpubAppDomain::CorpubAppDomain (_In_ LPWSTR szAppDomainName, ULONG Id)
    : CordbCommonBase (0, enumCorpubAppDomain),
    m_pNext (NULL),
    m_szAppDomainName (szAppDomainName),
    m_id (Id)
{
    _ASSERTE(m_szAppDomainName != NULL);
}

CorpubAppDomain::~CorpubAppDomain()
{
    delete [] m_szAppDomainName;
}

HRESULT CorpubAppDomain::QueryInterface (REFIID id, void **ppInterface)
{
    if (id == IID_ICorPublishAppDomain)
        *ppInterface = (ICorPublishAppDomain*)this;
    else if (id == IID_IUnknown)
        *ppInterface = (IUnknown*)(ICorPublishAppDomain*)this;
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}


/*
 * Get the name and ID for an application domain.
 */
HRESULT CorpubAppDomain::GetID (ULONG32 *pId)
{
    VALIDATE_POINTER_TO_OBJECT(pId, ULONG32 *);

    *pId = m_id;

    return S_OK;
}

/*
 * Get the name for an application domain.
 */
HRESULT CorpubAppDomain::GetName(ULONG32 cchName,
                                ULONG32 *pcchName,
                                _Out_writes_to_opt_(cchName, *pcchName) WCHAR szName[])
{
    VALIDATE_POINTER_TO_OBJECT_ARRAY_OR_NULL(szName, WCHAR, cchName, true, true);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pcchName, ULONG32 *);

    const WCHAR *szTempName = m_szAppDomainName;

    // In case we didn't get the name (most likely out of memory on ctor).
    if (!szTempName)
        szTempName = W("<unknown>");

    return CopyOutString(szTempName, cchName, pcchName, szName);
}



// ******************************************
// CorpubProcessEnum
// ******************************************

CorpubProcessEnum::CorpubProcessEnum (CorpubProcess *pFirst)
    : CordbCommonBase (0, enumCorpubProcessEnum),
    m_pFirst (pFirst),
    m_pCurrent (pFirst)
{
    // Increment the ref count on each process, we own the list
    CorpubProcess * cur = pFirst;
    while( cur != NULL )
    {
        cur->AddRef();
        cur = cur->GetNextProcess();
    }
}

CorpubProcessEnum::~CorpubProcessEnum()
{
    // Release each process in the list (our client may still have a reference
    // to some of them)
    while (m_pFirst != NULL)
    {
        CorpubProcess *pTmp = m_pFirst;
        m_pFirst = m_pFirst->GetNextProcess();
        pTmp->Release();
    }
}

HRESULT CorpubProcessEnum::QueryInterface (REFIID id, void **ppInterface)
{
    if (id == IID_ICorPublishProcessEnum)
        *ppInterface = (ICorPublishProcessEnum*)this;
    else if (id == IID_IUnknown)
        *ppInterface = (IUnknown*)(ICorPublishProcessEnum*)this;
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}


HRESULT CorpubProcessEnum::Skip(ULONG celt)
{
    while ((m_pCurrent != NULL) && (celt-- > 0))
    {
        m_pCurrent = m_pCurrent->GetNextProcess();
    }

    return S_OK;
}

HRESULT CorpubProcessEnum::Reset()
{
    m_pCurrent = m_pFirst;

    return S_OK;
}

HRESULT CorpubProcessEnum::Clone(ICorPublishEnum **ppEnum)
{
    VALIDATE_POINTER_TO_OBJECT(ppEnum, ICorPublishEnum **);
    return E_NOTIMPL;
}

HRESULT CorpubProcessEnum::GetCount(ULONG *pcelt)
{
    VALIDATE_POINTER_TO_OBJECT(pcelt, ULONG *);

    CorpubProcess *pTemp = m_pFirst;

    *pcelt = 0;

    while (pTemp != NULL)
    {
        (*pcelt)++;
        pTemp = pTemp->GetNextProcess();
    }

    return S_OK;
}

HRESULT CorpubProcessEnum::Next(ULONG celt,
                ICorPublishProcess *objects[],
                ULONG *pceltFetched)
{
    VALIDATE_POINTER_TO_OBJECT_ARRAY(objects, ICorPublishProcess *,
        celt, true, true);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pceltFetched, ULONG *);

    if ((pceltFetched == NULL) && (celt != 1))
    {
        return E_INVALIDARG;
    }

    if (celt == 0)
    {
        if (pceltFetched != NULL)
        {
            *pceltFetched = 0;
        }
        return S_OK;
    }

    HRESULT hr = S_OK;

    ULONG count = 0;

    while ((m_pCurrent != NULL) && (count < celt))
    {
        hr = m_pCurrent->QueryInterface (IID_ICorPublishProcess,
                                        (void**)&objects[count]);

        if (hr != S_OK)
        {
            break;
        }

        count++;
        m_pCurrent = m_pCurrent->GetNextProcess();
    }

    if (pceltFetched != NULL)
    {
        *pceltFetched = count;
    }

    //
    // If we reached the end of the enumeration, but not the end
    // of the number of requested items, we return S_FALSE.
    //
    if (count < celt)
    {
        return S_FALSE;
    }

    return hr;
}

// ******************************************
// CorpubAppDomainEnum
// ******************************************
CorpubAppDomainEnum::CorpubAppDomainEnum (CorpubAppDomain *pFirst)
    : CordbCommonBase (0, enumCorpubAppDomainEnum),
    m_pFirst (pFirst),
    m_pCurrent (pFirst)
{
    CorpubAppDomain *pCur = pFirst;
    while( pCur != NULL )
    {
        pCur->AddRef();
        pCur = pCur->GetNextAppDomain();
    }
}

CorpubAppDomainEnum::~CorpubAppDomainEnum()
{
    // Delete all the app domains
    while (m_pFirst != NULL )
    {
        CorpubAppDomain *pTemp = m_pFirst;
        m_pFirst = m_pFirst->GetNextAppDomain();
        pTemp->Release();
    }
}

HRESULT CorpubAppDomainEnum::QueryInterface (REFIID id, void **ppInterface)
{
    if (id == IID_ICorPublishAppDomainEnum)
        *ppInterface = (ICorPublishAppDomainEnum*)this;
    else if (id == IID_IUnknown)
        *ppInterface = (IUnknown*)(ICorPublishAppDomainEnum*)this;
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}


HRESULT CorpubAppDomainEnum::Skip(ULONG celt)
{
    while ((m_pCurrent != NULL) && (celt-- > 0))
    {
        m_pCurrent = m_pCurrent->GetNextAppDomain();
    }

    return S_OK;
}

HRESULT CorpubAppDomainEnum::Reset()
{
    m_pCurrent = m_pFirst;

    return S_OK;
}

HRESULT CorpubAppDomainEnum::Clone(ICorPublishEnum **ppEnum)
{
    VALIDATE_POINTER_TO_OBJECT(ppEnum, ICorPublishEnum **);
    return E_NOTIMPL;
}

HRESULT CorpubAppDomainEnum::GetCount(ULONG *pcelt)
{
    VALIDATE_POINTER_TO_OBJECT(pcelt, ULONG *);

    CorpubAppDomain *pTemp = m_pFirst;

    *pcelt = 0;

    while (pTemp != NULL)
    {
        (*pcelt)++;
        pTemp = pTemp->GetNextAppDomain();
    }

    return S_OK;
}

HRESULT CorpubAppDomainEnum::Next(ULONG celt,
                ICorPublishAppDomain *objects[],
                ULONG *pceltFetched)
{
    VALIDATE_POINTER_TO_OBJECT_ARRAY(objects, ICorPublishProcess *,
        celt, true, true);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pceltFetched, ULONG *);

    if ((pceltFetched == NULL) && (celt != 1))
    {
        return E_INVALIDARG;
    }

    if (celt == 0)
    {
        if (pceltFetched != NULL)
        {
    *pceltFetched = 0;
        }
        return S_OK;
    }

    HRESULT hr = S_OK;

    ULONG count = 0;

    while ((m_pCurrent != NULL) && (count < celt))
    {
        hr = m_pCurrent->QueryInterface (IID_ICorPublishAppDomain,
                                        (void **)&objects[count]);

        if (hr != S_OK)
        {
            break;
        }

        count++;
        m_pCurrent = m_pCurrent->GetNextAppDomain();
    }


    if (pceltFetched != NULL)
    {
        *pceltFetched = count;
    }

    //
    // If we reached the end of the enumeration, but not the end
    // of the number of requested items, we return S_FALSE.
    //
    if (count < celt)
    {
        return S_FALSE;
    }

    return hr;
}

#endif // defined(FEATURE_DBG_PUBLISH)
