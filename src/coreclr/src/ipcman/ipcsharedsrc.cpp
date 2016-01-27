// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
//
 
//
// ==--==
//*****************************************************************************
// File: IPCSharedSrc.cpp
//
// Shared source for COM+ IPC Reader & Writer classes
//
//*****************************************************************************

#include "stdafx.h"
#include "ipcshared.h"
#include "ipcmanagerinterface.h"

#ifndef FEATURE_CORECLR
#include "AppXUtil.h"
#endif

#if defined(FEATURE_IPCMAN)

//-----------------------------------------------------------------------------
// Close a handle and pointer to any memory mapped file
//-----------------------------------------------------------------------------
void IPCShared::CloseMemoryMappedFile(HANDLE & hMemFile, void * & pBlock)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_CORDB, LL_INFO10, "IPCS::CloseMemoryMappedFile: closing 0x%08x\n", hMemFile));

    if (pBlock != NULL) {
        if (!UnmapViewOfFile(pBlock))
            _ASSERTE(!"UnmapViewOfFile failed");
        pBlock = NULL;
    }

    if (hMemFile != NULL) {
        CloseHandle(hMemFile);
        hMemFile = NULL;
    }
}

//-----------------------------------------------------------------------------
// Based on the pid, write a unique name for a memory mapped file
//-----------------------------------------------------------------------------
void IPCShared::GenerateName(DWORD pid, SString & sName)
{
    WRAPPER_NO_CONTRACT;

    const WCHAR * szFormat = CorLegacyPrivateIPCBlock;
    szFormat = L"Global\\" CorLegacyPrivateIPCBlock;

    sName.Printf(szFormat, pid);
}

//-----------------------------------------------------------------------------
// Based on the pid, write a unique name for a memory mapped file
//-----------------------------------------------------------------------------
void IPCShared::GenerateNameLegacyTempV4(DWORD pid, SString & sName)
{
    WRAPPER_NO_CONTRACT;

    const WCHAR * szFormat = CorLegacyPrivateIPCBlockTempV4;
    szFormat = L"Global\\" CorLegacyPrivateIPCBlockTempV4;

    sName.Printf(szFormat, pid);
}

//-----------------------------------------------------------------------------
// Based on the pid, write a unique name for a memory mapped file
//-----------------------------------------------------------------------------
void IPCShared::GenerateLegacyPublicName(DWORD pid, SString & sName)
{
    WRAPPER_NO_CONTRACT;

    const WCHAR * szFormat = CorLegacyPublicIPCBlock;
    szFormat = L"Global\\" CorLegacyPublicIPCBlock;

    sName.Printf(szFormat, pid);
}

#ifndef DACCESS_COMPILE

//-----------------------------------------------------------------------------
// Based on the pid, write a unique name for the IPCBlockTable on Vista and Higher
//-----------------------------------------------------------------------------
HRESULT IPCShared::GenerateBlockTableName(DWORD pid, SString & sName, HANDLE & pBoundaryDesc, HANDLE & pPrivateNamespace, PSID* pSID, BOOL bCreate)
{
    WRAPPER_NO_CONTRACT;
    HRESULT hr = E_FAIL;

#define SIZE 100
    const WCHAR * szFormat = CorSxSPublicIPCBlock;
    static HMODULE hKernel32 = NULL;
    if(hKernel32 == NULL)
        hKernel32 = WszGetModuleHandle(L"kernel32.dll");
    if(hKernel32 == NULL)
    {
        hr = HRESULT_FROM_GetLastError();
        return hr;
    }
    //We are using static function pointers so that we dont call GetProcAddress every time
    //We know that the Writer will call this function only once and the reader (perfmon) is a single
    //threaded App. Therefore its safe to assign static local variables in this case. 
    typedef WINBASEAPI BOOL (WINAPI ADD_SID_TO_BOUNDARY_DESCRIPTOR)(HANDLE*, PSID);
    static ADD_SID_TO_BOUNDARY_DESCRIPTOR * pAddSIDToBoundaryDescriptor = NULL;

    typedef WINBASEAPI HANDLE (WINAPI CREATE_BOUNDARY_DESCRIPTOR)(LPCWSTR,ULONG);
    static CREATE_BOUNDARY_DESCRIPTOR * pCreateBoundaryDescriptor = NULL;
    
    typedef WINBASEAPI HANDLE (WINAPI CREATE_PRIVATE_NAMESPACE )(LPSECURITY_ATTRIBUTES, LPVOID, LPCWSTR);
    static CREATE_PRIVATE_NAMESPACE * pCreatePrivateNamespace = NULL;

    typedef WINBASEAPI HANDLE (WINAPI OPEN_PRIVATE_NAMESPACE)(LPVOID,LPCWSTR);
    static OPEN_PRIVATE_NAMESPACE * pOpenPrivateNamespace = NULL;

    if(pAddSIDToBoundaryDescriptor == NULL)
        pAddSIDToBoundaryDescriptor = (ADD_SID_TO_BOUNDARY_DESCRIPTOR *)GetProcAddress(hKernel32, "AddSIDToBoundaryDescriptor"); 
    if(pCreateBoundaryDescriptor == NULL)
        pCreateBoundaryDescriptor = (CREATE_BOUNDARY_DESCRIPTOR *)GetProcAddress(hKernel32, "CreateBoundaryDescriptorW"); 
    if(pCreatePrivateNamespace == NULL)
        pCreatePrivateNamespace = (CREATE_PRIVATE_NAMESPACE *)GetProcAddress(hKernel32, "CreatePrivateNamespaceW"); 
    if(pOpenPrivateNamespace==NULL)
        pOpenPrivateNamespace = (OPEN_PRIVATE_NAMESPACE *)GetProcAddress(hKernel32, "OpenPrivateNamespaceW");
    _ASSERTE((pAddSIDToBoundaryDescriptor != NULL) && 
            (pCreateBoundaryDescriptor != NULL) && 
            (pCreatePrivateNamespace != NULL) && 
            (pOpenPrivateNamespace != NULL));

    if ((pAddSIDToBoundaryDescriptor == NULL) || 
            (pCreateBoundaryDescriptor == NULL) || 
            (pCreatePrivateNamespace == NULL) || 
            (pOpenPrivateNamespace == NULL))
    {
        return ERROR_PROC_NOT_FOUND;
    }

    WCHAR wsz[SIZE];
    swprintf_s(wsz,SIZE, CorSxSBoundaryDescriptor, pid);

    ULONG flags = 0;
    if (RunningOnWin8())
    {
        // on win8 we specify this flag regardless if the process is inside an appcontainer, the kernel will do the right thing.
        // note that for appcontainers this flag is necessary regardless of producer or consumer, ie you can't create a boundary
        // descriptor in an appcontainer process without adding the appcontainer SID (the API call will fail).
        flags |= CREATE_BOUNDARY_DESCRIPTOR_ADD_APPCONTAINER_SID;
    }

    pBoundaryDesc = (*pCreateBoundaryDescriptor)((LPCWSTR)&wsz, flags);
    if(!pBoundaryDesc)
    {
        hr = HRESULT_FROM_GetLastError();
        return hr;
    }        
    SID_IDENTIFIER_AUTHORITY SIDWorldAuth = SECURITY_WORLD_SID_AUTHORITY;
    if(!AllocateAndInitializeSid( &SIDWorldAuth, 1,SECURITY_WORLD_RID, 0, 0, 0, 0, 0, 0, 0, pSID)) 
    {
         hr = HRESULT_FROM_GetLastError();
         return hr;
    }
    if(!(*pAddSIDToBoundaryDescriptor) (&pBoundaryDesc,*pSID))
    {
        hr = HRESULT_FROM_GetLastError();
        return hr;
    }

#ifndef FEATURE_CORECLR
    // when pid != GetCurrentProcessId() it means we're the consumer opening other process perf counter data
    if (pid != GetCurrentProcessId())
    {
        // if the target process is inside an appcontainer we need to add the appcontainer SID to the boundary descriptor.
        NewArrayHolder<BYTE> pbTokenMem;
        hr = AppX::GetAppContainerTokenInfoForProcess(pid, pbTokenMem);

        if (FAILED(hr))
        {
            // failed to open the target's process, continue on
            // assuming that the process isn't in an AppContainer.
            _ASSERTE(pbTokenMem == NULL);
        }
        else
        {
            if (hr == S_FALSE)
            {
                // not an appcontainer
                _ASSERTE(pbTokenMem == NULL);
            }
            else
            {
                // process is an appcontainer so add the SID
                PTOKEN_APPCONTAINER_INFORMATION pAppContainerTokenInfo =
                    reinterpret_cast<PTOKEN_APPCONTAINER_INFORMATION>(pbTokenMem.GetValue());
                _ASSERTE(pAppContainerTokenInfo);
                _ASSERTE(pAppContainerTokenInfo->TokenAppContainer);

                if (!(*pAddSIDToBoundaryDescriptor)(&pBoundaryDesc, pAppContainerTokenInfo->TokenAppContainer))
                    return HRESULT_FROM_WIN32(GetLastError());
            }
        }
    }
#endif // FEATURE_CORECLR
    
    if(bCreate)
    {
        SECURITY_ATTRIBUTES *pSA = NULL;
        IPCShared::CreateWinNTDescriptor(pid, FALSE, &pSA, PrivateNamespace, eDescriptor_Public);
        pPrivateNamespace = (*pCreatePrivateNamespace)(pSA, (VOID *)(pBoundaryDesc), 
                                                        (LPCWSTR)CorSxSWriterPrivateNamespacePrefix);
        if(!pPrivateNamespace)
        { 
            hr = HRESULT_FROM_GetLastError();
        }
        IPCShared::DestroySecurityAttributes(pSA);

        if(!pPrivateNamespace)
        { 
            //if already created by a different version of the runtime we return OK.
            if(hr ==HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS))
            {
                hr = S_OK;
            }
            else
            {
                return hr;
            }
        }
     }
     else
     {
        pPrivateNamespace = (*pOpenPrivateNamespace)((VOID *)(pBoundaryDesc), (LPCWSTR)CorSxSReaderPrivateNamespacePrefix);
        if(!pPrivateNamespace)
        { 
            hr = HRESULT_FROM_GetLastError();
            return hr;
        }
     }
    szFormat = (bCreate ? CorSxSWriterPrivateNamespacePrefix L"\\"  CorSxSVistaPublicIPCBlock : CorSxSReaderPrivateNamespacePrefix L"\\"  CorSxSVistaPublicIPCBlock);
    sName.Printf(szFormat);
    hr=S_OK;

    return hr;
}

#endif

//-----------------------------------------------------------------------------
// Free's the handle to a boundary descriptor and a SID
//-----------------------------------------------------------------------------
HRESULT IPCShared::FreeHandles(HANDLE & hBoundaryDescriptor, PSID & pSID)
{
    WRAPPER_NO_CONTRACT;
    HRESULT hr = S_OK;
    if(hBoundaryDescriptor != NULL) 
    {
        static HMODULE hKernel32 = NULL;
        if(hKernel32 == NULL)
            hKernel32 = WszGetModuleHandle(L"kernel32.dll");
        if(hKernel32 == NULL)
        {
            hr = HRESULT_FROM_GetLastError();
            return hr;
        }
        typedef WINBASEAPI VOID (WINAPI DELETE_BOUNDARY_DESCRIPTOR)(HANDLE);
        static DELETE_BOUNDARY_DESCRIPTOR * pDeleteBoundaryDescriptor = NULL;
        if(pDeleteBoundaryDescriptor == NULL) 
            pDeleteBoundaryDescriptor = (DELETE_BOUNDARY_DESCRIPTOR *)GetProcAddress(hKernel32, "DeleteBoundaryDescriptor");
        _ASSERTE(pDeleteBoundaryDescriptor != NULL);
        if (pDeleteBoundaryDescriptor == NULL)
        {
            hr = ERROR_PROC_NOT_FOUND;
        }
        else 
        {
            (*pDeleteBoundaryDescriptor)(hBoundaryDescriptor);
            hBoundaryDescriptor = NULL;
    
        }
    }
    if(pSID != NULL)
    {
        FreeSid(pSID);
        pSID = NULL;
    }

    return hr;
}

//--------------------------------------------------------------------------------------
// Free's the handle to a boundary descriptor, a SID and a handle to a privatenamespace
//--------------------------------------------------------------------------------------
HRESULT IPCShared::FreeHandles(HANDLE & hBoundaryDescriptor, PSID & pSID, HANDLE & hPrivateNamespace)
{
    WRAPPER_NO_CONTRACT;
    HRESULT hr = S_OK;

    hr = IPCShared::FreeHandles(hBoundaryDescriptor,pSID);
    if(!SUCCEEDED(hr))
        return hr;
    if(hPrivateNamespace != NULL) 
    {
        static HMODULE hKernel32 = NULL;
        if(hKernel32 == NULL)
            hKernel32 = WszGetModuleHandle(L"kernel32.dll");
        if(hKernel32 == NULL)
        {
            hr = HRESULT_FROM_GetLastError();
            return hr;
        }
        typedef WINBASEAPI BOOL (WINAPI CLOSE_PRIVATE_NAMESPACE)(HANDLE, ULONG);
        static CLOSE_PRIVATE_NAMESPACE * pClosePrivateNamespace;
        if(pClosePrivateNamespace == NULL)
            pClosePrivateNamespace = (CLOSE_PRIVATE_NAMESPACE *)GetProcAddress(hKernel32, "ClosePrivateNamespace");
        _ASSERTE(pClosePrivateNamespace != NULL);
        if (pClosePrivateNamespace == NULL)
        {
            hr = ERROR_PROC_NOT_FOUND;
        }
        else
        {
            BOOL isClosed = (*pClosePrivateNamespace)(hPrivateNamespace,0);
            hPrivateNamespace = NULL;
            if(!isClosed)
            {
                hr = HRESULT_FROM_GetLastError();
            }
            
        }
    }

    return hr;
}

HRESULT IPCShared::CreateWinNTDescriptor(DWORD pid, BOOL bRestrictiveACL, SECURITY_ATTRIBUTES **ppSA, KernelObject whatObject)
{
    WRAPPER_NO_CONTRACT;

    return IPCShared::CreateWinNTDescriptor(pid, bRestrictiveACL, ppSA, whatObject, eDescriptor_Private);
}

//-----------------------------------------------------------------------------
// Setup a security descriptor for the named kernel objects if we're on NT.
//-----------------------------------------------------------------------------

HRESULT IPCShared::CreateWinNTDescriptor(DWORD pid, BOOL bRestrictiveACL, SECURITY_ATTRIBUTES **ppSA, KernelObject whatObject, EDescriptorType descType)
{
    WRAPPER_NO_CONTRACT;

    HRESULT hr = NO_ERROR;

    // Gotta have a place to stick the new SA...
    if (ppSA == NULL)
    {
        _ASSERTE(!"Caller must supply ppSA");
        return E_INVALIDARG;
    }

    *ppSA = NULL;

    ACL *pACL = NULL;
    SECURITY_DESCRIPTOR *pSD = NULL;
    SECURITY_ATTRIBUTES *pSA = NULL;

    // Allocate a SD.
    _ASSERTE (SECURITY_DESCRIPTOR_MIN_LENGTH == sizeof(SECURITY_DESCRIPTOR));
    pSD = new (nothrow) SECURITY_DESCRIPTOR;

    if (pSD == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto errExit;
    }

    // Do basic SD initialization
    if (!InitializeSecurityDescriptor(pSD, SECURITY_DESCRIPTOR_REVISION))
    {
        hr = HRESULT_FROM_GetLastError();
        goto errExit;
    }

    // Grab the ACL for the IPC block for the given process
    if (!InitializeGenericIPCAcl(pid, bRestrictiveACL, &pACL, whatObject, descType))
    {
        hr = E_FAIL;
        goto errExit;
    }

    // Add the ACL as the DACL for the SD.
    if (!SetSecurityDescriptorDacl(pSD, TRUE, pACL, FALSE))
    {
        hr = HRESULT_FROM_GetLastError();
        goto errExit;
    }

    // Allocate a SA.
    pSA = new (nothrow) SECURITY_ATTRIBUTES;

    if (pSA == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto errExit;
    }

    // Pass out the new SA.
    *ppSA = pSA;

    pSA->nLength = sizeof(SECURITY_ATTRIBUTES);
    pSA->lpSecurityDescriptor = pSD;
    pSA->bInheritHandle = FALSE;

    // uncomment this line if you want to see the DACL being generated.
    //DumpSD(pSD);

errExit:
    if (FAILED(hr))
    {
        if (pACL != NULL)
        {
            for(int i = 0; i < pACL->AceCount; i++)
                DeleteAce(pACL, i);

            delete [] pACL;
        }

        if (pSD != NULL)
            delete pSD;
    }

    return hr;
}

//-----------------------------------------------------------------------------
// Helper to destroy the security attributes for the shared memory for a given
// process.
//-----------------------------------------------------------------------------
void IPCShared::DestroySecurityAttributes(SECURITY_ATTRIBUTES *pSA)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // We'll take a NULL param just to be nice.
    if (pSA == NULL)
        return;

    // Cleanup the DACL in the SD.
    SECURITY_DESCRIPTOR *pSD = (SECURITY_DESCRIPTOR*) pSA->lpSecurityDescriptor;

    if (pSD != NULL)
    {
        // Grab the DACL
        BOOL isDACLPresent = FALSE;
        BOOL isDefaultDACL = FALSE;
        ACL *pACL = NULL;

        BOOL res = GetSecurityDescriptorDacl(pSD, &isDACLPresent, &pACL, &isDefaultDACL);

        // If we got the DACL, then free the stuff inside of it.
        if (res && isDACLPresent && (pACL != NULL) && !isDefaultDACL)
        {
            for(int i = 0; i < pACL->AceCount; i++)
                DeleteAce(pACL, i);

            delete [] pACL;
        }

        // Free the SD from within the SA.
        delete pSD;
    }

    // Finally, free the SA.
    delete pSA;
}

//-----------------------------------------------------------------------------
// Given a PID, grab the SID for the owner of the process.
//
// NOTE:: Caller has to free *ppBufferToFreeByCaller.
// This buffer is allocated to hold the PSID return by GetPrcoessTokenInformation.
// The tkOwner field may contain a poniter into this allocated buffer. So we cannot free
// the buffer in GetSidForProcess.
//
//-----------------------------------------------------------------------------
HRESULT IPCShared::GetSidForProcess(HINSTANCE hDll,
                                    DWORD pid,
                                    PSID *ppSID,
                                    __deref_out_opt char **ppBufferToFreeByCaller)
{
    WRAPPER_NO_CONTRACT;

    HRESULT hr = S_OK;
    HANDLE hProc = NULL;
    HANDLE hToken = NULL;
    PSID_IDENTIFIER_AUTHORITY pSID = NULL;
    TOKEN_OWNER *ptkOwner = NULL;
    DWORD dwRetLength;

    LOG((LF_CORDB, LL_INFO10, "IPCWI::GSFP: GetSidForProcess 0x%x (%d)", pid, pid));

    // Grab a handle to the target process.
    hProc = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, pid);

    *ppBufferToFreeByCaller = NULL;

    if (hProc == NULL)
    {
        hr = HRESULT_FROM_GetLastError();

        LOG((LF_CORDB, LL_INFO10,
             "IPCWI::GSFP: Unable to get SID for process. "
             "OpenProcess(%d) failed: 0x%08x\n", pid, hr));

        goto ErrorExit;
    }

    // Get the pointer to the requested function
    FARPROC pProcAddr = GetProcAddress(hDll, "OpenProcessToken");

    // If the proc address was not found, return error
    if (pProcAddr == NULL)
    {
        hr = HRESULT_FROM_GetLastError();

        LOG((LF_CORDB, LL_INFO10,
             "IPCWI::GSFP: Unable to get SID for process. "
             "GetProcAddr (OpenProcessToken) failed: 0x%08x\n", hr));

        goto ErrorExit;
    }

    typedef BOOL WINAPI OPENPROCESSTOKEN(HANDLE, DWORD, PHANDLE);

    // Retrieve a handle of the access token
    if (!((OPENPROCESSTOKEN *)pProcAddr)(hProc, TOKEN_QUERY, &hToken))
    {
        hr = HRESULT_FROM_GetLastError();

        LOG((LF_CORDB, LL_INFO100,
             "IPCWI::GSFP: OpenProcessToken() failed: 0x%08x\n", hr));

        goto ErrorExit;
    }

    // Get the pointer to the requested function
    pProcAddr = GetProcAddress(hDll, "GetTokenInformation");

    // If the proc address was not found, return error
    if (pProcAddr == NULL)
    {
        hr = HRESULT_FROM_GetLastError();

        LOG((LF_CORDB, LL_INFO10,
             "IPCWI::GSFP: Unable to get SID for process. "
             "GetProcAddr (GetTokenInformation) failed: 0x%08x\n", hr));

        goto ErrorExit;
    }

    typedef BOOL GETTOKENINFORMATION(HANDLE, TOKEN_INFORMATION_CLASS, LPVOID,
                                     DWORD, PDWORD);

    // get the required size of buffer
    ((GETTOKENINFORMATION *)pProcAddr) (hToken, TokenOwner, NULL,
                                        0, &dwRetLength);
    _ASSERTE (dwRetLength);

    *ppBufferToFreeByCaller = new (nothrow) char [dwRetLength];
    if ((ptkOwner = (TOKEN_OWNER *) *ppBufferToFreeByCaller) == NULL)
    {
        hr = E_OUTOFMEMORY;

        LOG((LF_CORDB, LL_INFO10,
             "IPCWI::GSFP: OutOfMemory... "
             "GetTokenInformation() failed.\n"));

        goto ErrorExit;
    }

    if (!((GETTOKENINFORMATION *)pProcAddr) (hToken, TokenOwner, (LPVOID)ptkOwner,
                                            dwRetLength, &dwRetLength))
    {
        hr = HRESULT_FROM_GetLastError();

        LOG((LF_CORDB, LL_INFO10,
             "IPCWI::GSFP: Unable to get SID for process. "
             "GetTokenInformation() failed: 0x%08x\n", hr));

        goto ErrorExit;
    }

    *ppSID = ptkOwner->Owner;

ErrorExit:
    if (hProc != NULL)
        CloseHandle(hProc);

    if (hToken != NULL)
        CloseHandle(hToken);

    return hr;
}

/* static  */
DWORD IPCShared::GetAccessFlagsForObject(KernelObject whatObject, BOOL bFullControlACL)
{
    _ASSERTE(whatObject >= 0 && whatObject < TotalKernelObjects);
    
    DWORD dwAccessFlags = 0;
    
    if (!bFullControlACL)
    {
        if (whatObject == Section)
            dwAccessFlags = (STANDARD_RIGHTS_ALL | SECTION_MAP_READ) & ~WRITE_DAC & ~WRITE_OWNER & ~DELETE;
        else if (whatObject == Event)
            dwAccessFlags = (EVENT_ALL_ACCESS) & ~WRITE_DAC & ~WRITE_OWNER & ~DELETE;
        else if (whatObject == PrivateNamespace)
            dwAccessFlags = FILE_MAP_READ;
    }
    else
    {
        _ASSERTE(whatObject != PrivateNamespace);
        if (whatObject == Section)
            dwAccessFlags = CLR_IPC_GENERIC_RIGHT;
        else if (whatObject == Event)
            dwAccessFlags = EVENT_ALL_ACCESS;
    }

    _ASSERTE(dwAccessFlags != 0);
    return dwAccessFlags;
}


//-----------------------------------------------------------------------------
// This function will initialize the Access Control List with three
// Access Control Entries:
// The first ACE entry grants all permissions to "Administrators".
// The second ACE grants all permissions to the monitoring users (for perfcounters).
// The third ACE grants all permissions to "Owner" of the target process.
//-----------------------------------------------------------------------------
BOOL IPCShared::InitializeGenericIPCAcl(DWORD pid, BOOL bRestrictiveACL, PACL *ppACL, KernelObject whatObject, EDescriptorType descType)
{
    WRAPPER_NO_CONTRACT;

    struct PermissionStruct
    {
        PSID    rgPSID;
        DWORD   rgAccessFlags;
    } PermStruct[MaxNumberACEs];

    SID_IDENTIFIER_AUTHORITY SIDAuthNT = SECURITY_NT_AUTHORITY;
    HRESULT hr = S_OK;
    DWORD dwAclSize;
    BOOL returnCode = false;
    *ppACL = NULL;
    DWORD i;
    DWORD cActualACECount = 0;
    char *pBufferToFreeByCaller = NULL;
    int iSIDforAdmin = -1;
    int iSIDforUsers = -1;
    int iSIDforLoggingUsers = -1;
#if !defined (DACCESS_COMPILE) && !defined(FEATURE_CORECLR)
    NewArrayHolder<BYTE> pbTokenMem;
    PTOKEN_APPCONTAINER_INFORMATION pAppContainerTokenInfo = NULL;
#endif

    PermStruct[0].rgPSID = NULL;

    HINSTANCE hDll = WszGetModuleHandle(L"advapi32");

    if (hDll == NULL)
    {
        LOG((LF_CORDB, LL_INFO10, "IPCWI::IGIPCA: Unable to generate ACL for IPC. LoadLibrary (advapi32) failed.\n"));
        return false;
    }
    _ASSERTE(hDll != NULL);

    // Get the pointer to the requested function
    FARPROC pProcAddr = GetProcAddress(hDll, "AllocateAndInitializeSid");

    // If the proc address was not found, return error
    if (pProcAddr == NULL)
    {
        LOG((LF_CORDB, LL_INFO10,
             "IPCWI::IGIPCA: Unable to generate ACL for IPC. "
             "GetProcAddr (AllocateAndInitializeSid) failed.\n"));
        goto ErrorExit;
    }

    typedef BOOL ALLOCATEANDINITIALIZESID(PSID_IDENTIFIER_AUTHORITY,
                            BYTE, DWORD, DWORD, DWORD, DWORD,
                            DWORD, DWORD, DWORD, DWORD, PSID *);


    BOOL bGrantAllAccess = ((descType == eDescriptor_Private) ? TRUE : FALSE);
    // Create a SID for the BUILTIN\Administrators group.
    // SECURITY_BUILTIN_DOMAIN_RID + DOMAIN_ALIAS_RID_ADMINS = all Administrators. This translates to (A;;GA;;;BA).
    if (!((ALLOCATEANDINITIALIZESID *) pProcAddr)(&SIDAuthNT,
                                                  2,
                                                  SECURITY_BUILTIN_DOMAIN_RID,
                                                  DOMAIN_ALIAS_RID_ADMINS,
                                                  0, 0, 0, 0, 0, 0,
                                                  &PermStruct[0].rgPSID))
    {
        hr = HRESULT_FROM_GetLastError();
        _ASSERTE(SUCCEEDED(hr));

        LOG((LF_CORDB, LL_INFO10,
             "IPCWI::IGIPCA: failed to allocate AdminSid: 0x%08x\n", hr));

        goto ErrorExit;
    }
    // GENERIC_ALL access for Administrators
    PermStruct[cActualACECount].rgAccessFlags = GetAccessFlagsForObject(whatObject, bGrantAllAccess);

    iSIDforAdmin = cActualACECount;
    cActualACECount++;

    // Next, we get the SID for the owner of the current process.
    hr = GetSidForProcess(hDll, GetCurrentProcessId(), &(PermStruct[cActualACECount].rgPSID), &pBufferToFreeByCaller);
    DWORD accessFlags = 0;
    if (whatObject == Section) {
        //special case, grant SECTION_MAP_WRITE for current owner just to support inProc SxS.
        accessFlags = GetAccessFlagsForObject(whatObject, bGrantAllAccess) | SECTION_MAP_WRITE;
    }
    else {
        accessFlags = GetAccessFlagsForObject(whatObject, bGrantAllAccess);
    }
    PermStruct[cActualACECount].rgAccessFlags = accessFlags;

    // Don't fail out if we cannot get the SID for the owner of the current process. In this case, the
    // share memory block will be created with only Admin (and optionall "Users") permissions.
    // Currently we discovered the anonymous user doesn't have privilege to call OpenProcess. Without OpenProcess,
    // we cannot get the SID...
    //
    if (SUCCEEDED(hr))
    {
        cActualACECount++;
    }
#if _DEBUG
    else
        LOG((LF_CORDB, LL_INFO100, "IPCWI::IGIPCA: GetSidForProcess() failed: 0x%08x\n", hr));
#endif // _DEBUG


    if (descType == eDescriptor_Public)
    {
        DWORD dwRet = ((ALLOCATEANDINITIALIZESID *) pProcAddr)(&SIDAuthNT,
                                                    2,
                                                    SECURITY_BUILTIN_DOMAIN_RID,
                                                    DOMAIN_ALIAS_RID_MONITORING_USERS,
                                                    0, 0, 0, 0, 0, 0,
                                                    &PermStruct[cActualACECount].rgPSID);

        if (dwRet)
        {
            // "Users" shouldn't be able to write to block, delete object, change DACLs, or change ownership
            PermStruct[cActualACECount].rgAccessFlags = GetAccessFlagsForObject(whatObject, FALSE);

            iSIDforUsers = cActualACECount;
            cActualACECount++;
        }
        else
        {
            hr = HRESULT_FROM_GetLastError();
            _ASSERTE(SUCCEEDED(hr));

            LOG((LF_CORDB, LL_INFO10,
                 "IPCWI::IGIPCA: failed to allocate Users Sid: 0x%08x\n", hr));

            // non-fatal error, so don't goto errorexit
        }
        
        dwRet = ((ALLOCATEANDINITIALIZESID *) pProcAddr)(&SIDAuthNT,
                                                    2,
                                                    SECURITY_BUILTIN_DOMAIN_RID,
                                                    DOMAIN_ALIAS_RID_LOGGING_USERS,
                                                    0, 0, 0, 0, 0, 0,
                                                    &PermStruct[cActualACECount].rgPSID);
        if (dwRet)
        {
            PermStruct[cActualACECount].rgAccessFlags = GetAccessFlagsForObject(whatObject, FALSE);

            iSIDforLoggingUsers = cActualACECount;
            cActualACECount++;
        }
        else
        {
            hr = HRESULT_FROM_GetLastError();
            _ASSERTE(SUCCEEDED(hr));

            LOG((LF_CORDB, LL_INFO10,
                    "IPCWI::IGIPCA: failed to allocate Domain Logging Users Sid: 0x%08x\n", hr));

            // non-fatal error, so don't goto errorexit
        }

#if !defined(DACCESS_COMPILE) && !defined(FEATURE_CORECLR)
        // when running on win8 if the process is an appcontainer then add the appcontainer SID to the ACL
        // going down this code path means we're creating the descriptor for our current PID.
        _ASSERTE(pid == GetCurrentProcessId());
        hr = AppX::GetAppContainerTokenInfoForProcess(pid, pbTokenMem);

        if (FAILED(hr))
        {
            // failed to open the target's process, continue on
            // assuming that the process isn't in an AppContainer.
            _ASSERTE(pbTokenMem == NULL);
        }
        else
        {
            if (hr == S_FALSE)
            {
                // not an appcontainer
                _ASSERTE(pbTokenMem == NULL);
            }
            else
            {
                // process is an appcontainer so add the SID
                pAppContainerTokenInfo =
                    reinterpret_cast<PTOKEN_APPCONTAINER_INFORMATION>(pbTokenMem.GetValue());
                _ASSERTE(pAppContainerTokenInfo);
                _ASSERTE(pAppContainerTokenInfo->TokenAppContainer);

                PermStruct[cActualACECount].rgPSID = pAppContainerTokenInfo->TokenAppContainer;
                PermStruct[cActualACECount].rgAccessFlags = GetAccessFlagsForObject(whatObject, FALSE);
                ++cActualACECount;
            }
        }
#endif // !defined(DACCESS_COMPILE) && !defined(FEATURE_CORECLR)        
    }

    _ASSERTE(cActualACECount <= MaxNumberACEs);

    // Now, create an Initialize an ACL and add the ACE entries to it.  NOTE: We're not using "SetEntriesInAcl" because
    // it loads a bunch of other dlls which can be avoided by using this roundabout way!!

    // Get the pointer to the requested function
    pProcAddr = GetProcAddress(hDll, "InitializeAcl");

    // If the proc address was not found, return error
    if (pProcAddr == NULL)
    {
        LOG((LF_CORDB, LL_INFO10,
             "IPCWI::IGIPCA: Unable to generate ACL for IPC. "
             "GetProcAddr (InitializeAcl) failed.\n"));
        goto ErrorExit;
    }

    // Also calculate the memory required for ACE entries in the ACL using the
    // following method:
    // "sizeof (ACCESS_ALLOWED_ACE) - sizeof (ACCESS_ALLOWED_ACE.SidStart) + GetLengthSid (pAceSid);"

    dwAclSize = sizeof (ACL) + (sizeof (ACCESS_ALLOWED_ACE) - sizeof (DWORD)) * cActualACECount;

    for (i = 0; i < cActualACECount; i++)
    {
        dwAclSize += GetLengthSid(PermStruct[i].rgPSID);
    }

    // now allocate memory
    if ((*ppACL = (PACL) new (nothrow) char[dwAclSize]) == NULL)
    {
        LOG((LF_CORDB, LL_INFO10, "IPCWI::IGIPCA: OutOfMemory... 'new Acl' failed.\n"));

        goto ErrorExit;
    }

    typedef BOOL INITIALIZEACL(PACL, DWORD, DWORD);

    if (!((INITIALIZEACL *)pProcAddr)(*ppACL, dwAclSize, ACL_REVISION))
    {
        hr = HRESULT_FROM_GetLastError();

        LOG((LF_CORDB, LL_INFO100,
             "IPCWI::IGIPCA: InitializeACL() failed: 0x%08x\n", hr));

        goto ErrorExit;
    }

    // Get the pointer to the requested function
    pProcAddr = GetProcAddress(hDll, "AddAccessAllowedAce");

    // If the proc address was not found, return error
    if (pProcAddr == NULL)
    {
        LOG((LF_CORDB, LL_INFO10,
             "IPCWI::IGIPCA: Unable to generate ACL for IPC. "
             "GetProcAddr (AddAccessAllowedAce) failed.\n"));
        goto ErrorExit;
    }

    typedef BOOL ADDACCESSALLOWEDACE(PACL, DWORD, DWORD, PSID);

    for (i=0; i < cActualACECount; i++)
    {
        if (!((ADDACCESSALLOWEDACE *)pProcAddr)(*ppACL,
                                                ACL_REVISION,
                                                PermStruct[i].rgAccessFlags,
                                                PermStruct[i].rgPSID))

        {
            hr = HRESULT_FROM_GetLastError();

            LOG((LF_CORDB, LL_INFO100,
                 "IPCWI::IGIPCA: AddAccessAllowedAce() failed: 0x%08x\n", hr));
            goto ErrorExit;
        }
    }

    returnCode = true;
    goto NormalExit;


ErrorExit:
    returnCode = FALSE;

    if (*ppACL)
    {
        delete [] (*ppACL);
        *ppACL = NULL;
    }

NormalExit:

    if (pBufferToFreeByCaller != NULL)
        delete [] pBufferToFreeByCaller;

    // Get the pointer to the requested function
    pProcAddr = GetProcAddress(hDll, "FreeSid");

    // If the proc address was not found, return error
    if (pProcAddr == NULL)
    {
        LOG((LF_CORDB, LL_INFO10,
             "IPCWI::IGIPCA: Unable to generate ACL for IPC. "
             "GetProcAddr (FreeSid) failed.\n"));
        return false;
    }

    typedef BOOL FREESID(PSID);

    // Free the SID created earlier. Function does not return a value.
    if( iSIDforAdmin != -1 )
        ((FREESID *) pProcAddr)(PermStruct[iSIDforAdmin].rgPSID);

    // free the SID for "Users"
    if (iSIDforUsers != -1)
        ((FREESID *) pProcAddr)(PermStruct[iSIDforUsers].rgPSID);

    // free the SID for "Performance Logging Users"
    if (iSIDforLoggingUsers != -1)
        ((FREESID *) pProcAddr)(PermStruct[iSIDforLoggingUsers].rgPSID);

    return returnCode;
}

#endif // FEATURE_IPCMAN
