// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//============================================================
//
// Class: COMIsolatedStorage
//
// Purpose: Native Implementation of IsolatedStorage
//

//

//============================================================


#include "common.h"
#include "excep.h"
#include "eeconfig.h"
#include "qcall.h"
#include "comisolatedstorage.h"

#ifdef FEATURE_ISOSTORE

#include <shlobj.h>

#ifndef FEATURE_ISOSTORE_LIGHT
#define IS_ROAMING(x)   ((x) & ISS_ROAMING_STORE)
#endif // !FEATURE_ISOSTORE_LIGHT

void DECLSPEC_NORETURN COMIsolatedStorage::ThrowISS(HRESULT hr)
{
    STANDARD_VM_CONTRACT;

    if ((hr >= ISS_E_ISOSTORE_START) && (hr <= ISS_E_ISOSTORE_END))
    {
        switch (hr)
        {
        case ISS_E_ISOSTORE :
        case ISS_E_OPEN_STORE_FILE :
        case ISS_E_OPEN_FILE_MAPPING :
        case ISS_E_MAP_VIEW_OF_FILE :
        case ISS_E_GET_FILE_SIZE :
        case ISS_E_CREATE_MUTEX :
        case ISS_E_LOCK_FAILED :
        case ISS_E_FILE_WRITE :
        case ISS_E_SET_FILE_POINTER :
        case ISS_E_CREATE_DIR :
        case ISS_E_CORRUPTED_STORE_FILE :
        case ISS_E_STORE_VERSION :
        case ISS_E_FILE_NOT_MAPPED :
        case ISS_E_BLOCK_SIZE_TOO_SMALL :
        case ISS_E_ALLOC_TOO_LARGE :
        case ISS_E_USAGE_WILL_EXCEED_QUOTA :
        case ISS_E_TABLE_ROW_NOT_FOUND :
        case ISS_E_DEPRECATE :
        case ISS_E_CALLER :
        case ISS_E_PATH_LENGTH :
        case ISS_E_MACHINE :
        case ISS_E_STORE_NOT_OPEN :
        case ISS_E_MACHINE_DACL :
            COMPlusThrowHR(hr);
            break;

        default :
            _ASSERTE(!"Unknown hr");
        }
    }

    COMPlusThrowHR(hr);
}

#ifndef FEATURE_ISOSTORE_LIGHT
StackWalkAction COMIsolatedStorage::StackWalkCallBack(
        CrawlFrame* pCf, PVOID ppv)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    // Get the function descriptor for this frame...
    MethodDesc *pMeth = pCf->GetFunction();
    MethodTable *pMT = pMeth->GetMethodTable();

    // Skip the Isolated Store and all it's sub classes..
    // <TODO>@Todo : This will work for now, but need to walk up to the base class
    // @Todo : Work out the JIT inlining issiues</TODO>

    if ((MscorlibBinder::IsClass(pMT, CLASS__ISS_STORE)) ||
        (MscorlibBinder::IsClass(pMT, CLASS__ISS_STORE_FILE)) ||
        (MscorlibBinder::IsClass(pMT, CLASS__ISS_STORE_FILE_STREAM)))
    {
        LOG((LF_STORE, LL_INFO10000, "StackWalk Continue %s\n",
            pMeth->m_pszDebugMethodName));
        return SWA_CONTINUE;
    }

    *(PVOID *)ppv = pMeth->GetModule()->GetAssembly();

    return SWA_ABORT;
}

void QCALLTYPE COMIsolatedStorage::GetCaller(QCall::ObjectHandleOnStack retAssembly)
{
    QCALL_CONTRACT;

    DomainAssembly * pDomainAssembly = NULL;

    BEGIN_QCALL;

    Assembly * pAssembly = NULL;
    StackWalkAction result;

    {
        GCX_COOP();
        result = StackWalkFunctions(GetThread(), StackWalkCallBack, (VOID*)&pAssembly);
    }

    if (result == SWA_FAILED)
        ThrowISS(ISS_E_CALLER);

    if (pAssembly == NULL)
        ThrowISS(ISS_E_CALLER);

#ifdef _DEBUG
    LOG((LF_STORE, LL_INFO10000, "StackWalk Found %s\n", pAssembly->GetSimpleName()));
#endif

    pDomainAssembly = pAssembly->GetDomainAssembly();

    GCX_COOP();
    retAssembly.Set(pDomainAssembly->GetExposedAssemblyObject());
    END_QCALL;

    return;
}
#endif // !FEATURE_ISOSTORE_LIGHT

// static
UINT64 QCALLTYPE COMIsolatedStorageFile::GetUsage(__in_opt AccountingInfo * pAI)
{
    QCALL_CONTRACT;

    UINT64 retVal = 0;
    BEGIN_QCALL;

    if (pAI == NULL)
        COMIsolatedStorage::ThrowISS(ISS_E_STORE_NOT_OPEN);

    PREFIX_ASSUME(pAI != NULL);

    HRESULT hr = pAI->GetUsage(&retVal);

    if (FAILED(hr))
        COMIsolatedStorage::ThrowISS(hr);

    END_QCALL;
    return retVal;
}

// static
void QCALLTYPE COMIsolatedStorageFile::Close(__in_opt AccountingInfo * pAI)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    if (pAI != NULL)
        delete pAI;

    END_QCALL;
}

//static 
BOOL QCALLTYPE COMIsolatedStorageFile::Lock(__in AccountingInfo * pAI,
                                            BOOL             fLock)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(pAI != NULL);
    } CONTRACTL_END;

    BEGIN_QCALL;

    if (fLock)
        AccountingInfo::AcquireLock(pAI);
    else
        AccountingInfo::ReleaseLock(pAI);

    END_QCALL;

    // AcquireLock will throw if it fails, ReleaseLock will ASSERT
    return TRUE;
}

// static
AccountingInfo * QCALLTYPE COMIsolatedStorageFile::Open(LPCWSTR wszFileName,
                                                        LPCWSTR wszSyncName)
{
    CONTRACT(AccountingInfo *)
    {
        QCALL_CHECK;
        PRECONDITION(wszFileName != NULL);
        PRECONDITION(wszSyncName != NULL);
        POSTCONDITION(RETVAL != NULL);
    } CONTRACT_END;

    AccountingInfo * pReturn = NULL;
    BEGIN_QCALL;

    AccountingInfo *pAI = new AccountingInfo(wszFileName, wszSyncName);

    HRESULT hr = pAI->Init();

    if (FAILED(hr))
        COMIsolatedStorage::ThrowISS(hr);

    pReturn = pAI;

    END_QCALL;
    RETURN(pReturn);
}

// static
void QCALLTYPE COMIsolatedStorageFile::Reserve(__in_opt AccountingInfo * pAI,
                                               UINT64                    qwQuota,
                                               UINT64                    qwReserve,
                                               BOOL                      fFree)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    if (pAI == NULL)
        COMIsolatedStorage::ThrowISS(ISS_E_STORE_NOT_OPEN);

    PREFIX_ASSUME(pAI != NULL);
    HRESULT hr = pAI->Reserve(qwQuota, qwReserve, fFree);

    if (FAILED(hr))
    {
#ifdef _DEBUG
        if (fFree) {
            LOG((LF_STORE, LL_INFO10000, "free 0x%x failed\n",
                (int)(qwReserve)));
        }
        else {
            LOG((LF_STORE, LL_INFO10000, "reserve 0x%x failed\n",
                (int)(qwReserve)));
        }
#endif
        COMIsolatedStorage::ThrowISS(hr);
    }

#ifdef _DEBUG
    if (fFree) {
        LOG((LF_STORE, LL_INFO10000, "free 0x%x\n",
            (int)(qwReserve)));
    } else {
        LOG((LF_STORE, LL_INFO10000, "reserve 0x%x\n",
            (int)(qwReserve)));
    }
#endif

    END_QCALL;
}

// static
BOOL QCALLTYPE COMIsolatedStorageFile::GetQuota(__in_opt AccountingInfo * pAI,
	                                            __out INT64 * qwQuota)
{
    QCALL_CONTRACT;
	BOOL retVal = false;
	BEGIN_QCALL;

    if (pAI == NULL)
        COMIsolatedStorage::ThrowISS(ISS_E_STORE_NOT_OPEN);

    PREFIX_ASSUME(pAI != NULL);

	retVal = pAI->GetQuota(qwQuota);

	END_QCALL;

	return retVal;
}

// static
void QCALLTYPE COMIsolatedStorageFile::SetQuota(__in_opt AccountingInfo * pAI,
	                                                     INT64 qwQuota)
{
    QCALL_CONTRACT;
	BEGIN_QCALL;

    if (pAI == NULL)
        COMIsolatedStorage::ThrowISS(ISS_E_STORE_NOT_OPEN);

    PREFIX_ASSUME(pAI != NULL);

	pAI->SetQuota(qwQuota);

	END_QCALL;
}

// static
void QCALLTYPE COMIsolatedStorageFile::GetRootDir(DWORD                      dwFlags,
                                                  QCall::StringHandleOnStack retRootDir)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    WCHAR wszPath[MAX_LONGPATH + 1] = {0};
    GetRootDirInternal(dwFlags, wszPath, COUNTOF(wszPath));
    retRootDir.Set(wszPath);

    END_QCALL;
}

#ifndef FEATURE_ISOSTORE_LIGHT   
// static
void QCALLTYPE COMIsolatedStorageFile::CreateDirectoryWithDacl(LPCWSTR wszPath)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(wszPath != NULL);
    } CONTRACTL_END;

    BEGIN_QCALL;

    SECURITY_ATTRIBUTES *pSecAttrib = NULL;

    SECURITY_ATTRIBUTES SecAttrib;
    SECURITY_DESCRIPTOR sd;
    NewArrayHolder<ACL> pDacl = NULL;

    memset(&SecAttrib, 0, sizeof(SecAttrib));

    BOOL ret = InitializeSecurityDescriptor(&sd, SECURITY_DESCRIPTOR_REVISION);
    if (!ret)
        COMIsolatedStorage::ThrowISS(ISS_E_MACHINE_DACL);

    HRESULT hr = GetMachineStoreDacl(&pDacl);
    if (FAILED(hr))
        COMIsolatedStorage::ThrowISS(ISS_E_MACHINE_DACL);

    ret = SetSecurityDescriptorDacl(&sd, TRUE, pDacl, FALSE);
    if (!ret)
        COMIsolatedStorage::ThrowISS(ISS_E_MACHINE_DACL);

    SecAttrib.nLength = sizeof(SECURITY_ATTRIBUTES);
    SecAttrib.bInheritHandle = FALSE;
    SecAttrib.lpSecurityDescriptor = &sd;
    pSecAttrib = &SecAttrib;

    CreateDirectoryIfNotPresent(wszPath, pSecAttrib);

    END_QCALL;
}

// Get the machine location for Isolated Storage
BOOL COMIsolatedStorageFile::GetMachineStoreDirectory (__out_ecount(cchMachineStorageRoot) WCHAR * wszMachineStorageRoot,
                                                       DWORD cchMachineStorageRoot)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    HRESULT hr = WszSHGetFolderPath(NULL,
                    CSIDL_COMMON_APPDATA | CSIDL_FLAG_CREATE,
                    NULL,
                    SHGFP_TYPE_CURRENT,
                    cchMachineStorageRoot,
                    wszMachineStorageRoot);
    LOG((LF_STORE, LL_INFO10000, "GetMachineStoreDirectory returned [%#x].\n", hr));
    return SUCCEEDED(hr);
}

// Creates a DACL for the machine store directory so that
// everyone may create directories beneath it.
// This method should only be called on NT platforms.

HRESULT COMIsolatedStorageFile::GetMachineStoreDacl(PACL * ppAcl)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;
    SID_IDENTIFIER_AUTHORITY siaWorld = SECURITY_WORLD_SID_AUTHORITY;
    SID_IDENTIFIER_AUTHORITY siaNTAuth = SECURITY_NT_AUTHORITY;
    SID_IDENTIFIER_AUTHORITY siaCreatorOwnerAuthority = SECURITY_CREATOR_SID_AUTHORITY;
    PSID pEveryoneSid = NULL;
    PSID pAdminsSid = NULL;
    PSID pCreatorOwnerSid = NULL;

    //
    // prepare Sids representing the world and admins
    //

    if (!AllocateAndInitializeSid(&siaWorld,
                                  1,
                                  SECURITY_WORLD_RID,
                                  0, 0, 0, 0, 0, 0, 0,
                                  &pEveryoneSid)) {
        hr = HRESULT_FROM_GetLastError();
        goto ErrorExit;
    }

    if (!AllocateAndInitializeSid(&siaNTAuth,
                                  2,
                                  SECURITY_BUILTIN_DOMAIN_RID,
                                  DOMAIN_ALIAS_RID_ADMINS,
                                  0, 0, 0, 0, 0, 0,
                                  &pAdminsSid)) {
        hr = HRESULT_FROM_GetLastError();
        goto ErrorExit;
    }

    if (!AllocateAndInitializeSid(&siaCreatorOwnerAuthority,
                                  1,
                                  SECURITY_CREATOR_OWNER_RID,
                                  0, 0, 0, 0, 0, 0, 0,
                                  &pCreatorOwnerSid)) {
        hr = HRESULT_FROM_GetLastError();
        goto ErrorExit;
    }

    //
    // compute size of new Acl
    //

    DWORD dwAclSize = sizeof(ACL)
                + 3 * (sizeof(ACCESS_ALLOWED_ACE) - sizeof(DWORD))
                + GetLengthSid(pEveryoneSid)
                + GetLengthSid(pAdminsSid)
                + GetLengthSid(pCreatorOwnerSid);

    *ppAcl = new ACL[dwAclSize / sizeof(ACL) + 1];
    if (!InitializeAcl(*ppAcl, dwAclSize, ACL_REVISION)) {
        hr = HRESULT_FROM_GetLastError();
        goto ErrorExit;
    }

    if (!AddAccessAllowedAce(*ppAcl,
                             ACL_REVISION,
                             (FILE_GENERIC_WRITE | FILE_GENERIC_READ) & (~WRITE_DAC),
                             pEveryoneSid)) {
        hr = HRESULT_FROM_GetLastError();
        goto ErrorExit;
    }

    if (!AddAccessAllowedAce(*ppAcl,
                             ACL_REVISION,
                             FILE_ALL_ACCESS,
                             pAdminsSid)) {
        hr = HRESULT_FROM_GetLastError();
        goto ErrorExit;
    }

    if (!AddAccessAllowedAce(*ppAcl,
                             ACL_REVISION,
                             FILE_ALL_ACCESS,
                             pCreatorOwnerSid)) {
        hr = HRESULT_FROM_GetLastError();
        goto ErrorExit;
    }

    //
    // make ACL inheritable
    //

    PACCESS_ALLOWED_ACE pAce;
    for (DWORD index = 0; index < 3; index++) {
        if(!GetAce(*ppAcl, index, (void **) &pAce)) {
            hr = HRESULT_FROM_GetLastError();
            goto ErrorExit;
        }
        pAce->Header.AceFlags = CONTAINER_INHERIT_ACE | OBJECT_INHERIT_ACE;
    }

ErrorExit:
    if (NULL != pEveryoneSid)
        FreeSid(pEveryoneSid);
    if (NULL != pAdminsSid)
        FreeSid(pAdminsSid);
    if (NULL != pCreatorOwnerSid)
        FreeSid(pCreatorOwnerSid);

    LOG((LF_STORE, LL_INFO10000, "GetMachineStoreDacl returned error code [%#x].\n", hr));

    return hr;
}
#endif // !FEATURE_ISOSTORE_LIGHT

// Throws on error
void COMIsolatedStorageFile::CreateDirectoryIfNotPresent(__in_z const WCHAR *path, LPSECURITY_ATTRIBUTES lpSecurityAttributes)
{
    STANDARD_VM_CONTRACT;

    LONG  lresult;

    // Check if the directory is already present
    lresult = WszGetFileAttributes(path);

    if (lresult == -1)
    {
        if (!WszCreateDirectory(path, lpSecurityAttributes))
        {
            COMIsolatedStorage::ThrowISS(ISS_E_CREATE_DIR);
        }
    }
    else if ((lresult & FILE_ATTRIBUTE_DIRECTORY) == 0)
    {
        COMIsolatedStorage::ThrowISS(ISS_E_CREATE_DIR);
    }
}

// Synchronized by the managed caller

#ifdef FEATURE_ISOSTORE_LIGHT

const WCHAR* const g_relativePath[] = {
    W("\\CoreIsolatedStorage")
};

#define nRelativePathLen       (  \
    sizeof("\\CoreIsolatedStorage") + 1)

#else // FEATURE_ISOSTORE_LIGHT

const WCHAR* const g_relativePath[] = {
    W("\\IsolatedStorage")
};

#define nRelativePathLen       (  \
    sizeof("\\IsolatedStorage") + 1)

#endif // FEATURE_ISOSTORE_LIGHT

#define nSubDirs (sizeof(g_relativePath)/sizeof(g_relativePath[0]))

void COMIsolatedStorageFile::GetRootDirInternal(
        DWORD dwFlags, __in_ecount(cPath) WCHAR *path, DWORD cPath)
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(cPath > 1);
        PRECONDITION(cPath <= MAX_LONGPATH + 1);
    } CONTRACTL_END;

    ULONG len;

    --cPath;    // To be safe.
    path[cPath] = 0;

    // Get roaming or local App Data locations
    BOOL res;

#ifdef FEATURE_ISOSTORE_LIGHT
    res = GetUserDir(path, cPath, FALSE);
#else 
    if ((dwFlags & ISS_MACHINE_STORE) == 0)
        res = GetUserDir(path, cPath, IS_ROAMING(dwFlags));
    else
        res = GetMachineStoreDirectory(path, cPath);
#endif // !FEATURE_ISOSTORE_LIGHT            
    LOG((LF_STORE, LL_INFO10000, "The isolated storage root directory location is [%S].\n", path));

    if (!res)
    {
        COMIsolatedStorage::ThrowISS(ISS_E_CREATE_DIR);
    }

    len = (ULONG)wcslen(path);

    if ((len + nRelativePathLen + 1) > cPath)
        COMIsolatedStorage::ThrowISS(ISS_E_PATH_LENGTH);

    CreateDirectoryIfNotPresent(path);

    // Create the store directory if necessary
    for (unsigned int i=0; i<nSubDirs; ++i)
    {
        wcscat_s(path, cPath, g_relativePath[i]);
        CreateDirectoryIfNotPresent(path);
    }

    wcscat_s(path, cPath, W("\\"));
}

#define WSZ_GLOBAL W("Global\\")

//--------------------------------------------------------------------------
// The file name is used to open / create the file.
// A synchronization object will also be created using this name
// with '\' replaced by '-'
//--------------------------------------------------------------------------
AccountingInfo::AccountingInfo(const WCHAR *wszFileName, const WCHAR *wszSyncName) :
        m_hFile(INVALID_HANDLE_VALUE),
        m_hMapping(NULL),
        m_hLock(NULL),
        m_pData(NULL)
{
    STANDARD_VM_CONTRACT;

#ifdef _DEBUG
    m_dwNumLocks = 0;
#endif

    int buffLen;
    buffLen = (int)wcslen(wszFileName) + 1;

    NewArrayHolder<WCHAR> pwszFileName(new WCHAR[buffLen]);

    // String length is known, using a memcpy here is faster, however, this
    // makes the code here and below less readable, this is not a very frequent
    // operation. No real perf gain here. Same comment applies to the strcpy
    // following this.

    wcscpy_s(pwszFileName, buffLen, wszFileName);

    _ASSERTE(((int)wcslen(pwszFileName) + 1) <= buffLen);

    // Allocate the Mutex name
    buffLen = (int)wcslen(wszSyncName) + (sizeof(WSZ_GLOBAL) / sizeof(WCHAR)) + 1;

    NewArrayHolder<WCHAR> pwszName(new WCHAR[buffLen]);

    wcscpy_s(pwszName, buffLen, WSZ_GLOBAL);
    wcscat_s(pwszName, buffLen, wszSyncName);

    _ASSERTE(((int)wcslen(pwszName) + 1) <= buffLen);

    pwszFileName.SuppressRelease();
    pwszName.SuppressRelease();

    // Now publish the strings
    m_wszFileName = pwszFileName;
    m_wszName = pwszName;
}

//--------------------------------------------------------------------------
// Frees memory, and open handles
//--------------------------------------------------------------------------
AccountingInfo::~AccountingInfo()
{
    CONTRACTL 
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }CONTRACTL_END;

    if (m_pData)
        CLRUnmapViewOfFile(m_pData);

    if (m_hMapping != NULL)
        CloseHandle(m_hMapping);

    if (m_hFile != INVALID_HANDLE_VALUE)
        CloseHandle(m_hFile);

    if (m_hLock != NULL)
        CloseHandle(m_hLock);

    if (m_wszFileName)
        delete [] m_wszFileName;

    if (m_wszName)
        delete [] m_wszName;

    _ASSERTE(m_dwNumLocks == 0);
}

//--------------------------------------------------------------------------
// Init should be called before Reserve / GetUsage is called.
// Creates the file if necessary
//--------------------------------------------------------------------------
HRESULT AccountingInfo::Init()
{
    CONTRACTL {
        STANDARD_VM_CHECK;
        PRECONDITION(m_hLock == NULL); // Init was called multiple times on this object without calling Close
    } CONTRACTL_END;

    // Create the synchronization object

    HRESULT hr = S_OK;
    m_hLock = WszCreateMutex(NULL, FALSE /* Initially not owned */, m_wszName);

    if (m_hLock == NULL)
        IfFailGo(ISS_E_CREATE_MUTEX);

    // Init was called multiple times on this object without calling Close

    _ASSERTE(m_hFile == INVALID_HANDLE_VALUE);

    {
        // The default DACL is fine here since we've already set the DACL on the root
        m_hFile = WszCreateFile(m_wszFileName,
                                GENERIC_READ | GENERIC_WRITE,
                                FILE_SHARE_READ | FILE_SHARE_WRITE,
                                NULL,
                                OPEN_ALWAYS,
                                FILE_FLAG_RANDOM_ACCESS,
                                NULL);
        
        if (m_hFile == INVALID_HANDLE_VALUE)
            IfFailGo(ISS_E_OPEN_STORE_FILE);
    }

    // If this file was created for the first time, then create the accounting
    // record and set to zero
    {
        AccountingInfoLockHolder pAI(this);

        DWORD   dwLow = 0, dwHigh = 0;    // For checking file size
        QWORD   qwSize;

        dwLow = ::GetFileSize(m_hFile, &dwHigh);

        if ((dwLow == 0xFFFFFFFF) && (GetLastError() != NO_ERROR))
        {
            IfFailGo(ISS_E_GET_FILE_SIZE);
        }

        qwSize = ((QWORD)dwHigh << 32) | dwLow;

        if (qwSize < sizeof(ISS_RECORD))
        {
            DWORD dwWrite;

            // Need to create the initial file
            NewArrayHolder<BYTE> pb(new BYTE[sizeof(ISS_RECORD)]);

            memset(pb, 0, sizeof(ISS_RECORD));

            dwWrite = 0;

            if ((WriteFile(m_hFile, pb, sizeof(ISS_RECORD), &dwWrite, NULL)
                == 0) || (dwWrite != sizeof(ISS_RECORD)))
            {
                IfFailGo(ISS_E_FILE_WRITE);
            }
        }

        // Lock out of scope here will be released
    }
ErrExit:
    ;
    return hr;
}

//--------------------------------------------------------------------------
// Get the amount of quota saved on disk.  Some hosts may allow users of 
// IsolatedStorage to increase the quota.  If so, we persist this data.
// If there is no saved quota, this method returns FALSE.
//--------------------------------------------------------------------------
BOOL AccountingInfo::GetQuota(
         INT64 *qwQuota)
{
    STANDARD_VM_CONTRACT;

	BOOL retVal = FALSE;
    HRESULT hr = S_OK;
    {
        AccountingInfoLockHolder pAI(this);

        hr = Map();

        if (SUCCEEDED(hr))
        {
			if(m_pISSRecord->dwVersion >= 1) {
                *qwQuota = m_pISSRecord->qwQuota;
				retVal = TRUE;
			} else {
       	        *qwQuota = 0;
	     	    retVal = FALSE;
			}
			Unmap();
		}
		else
		{
			*qwQuota = 0;
			retVal = FALSE;
		}
	}
	return retVal;
}

//--------------------------------------------------------------------------
// Sets the amount of quota saved on disk.  Some hosts may allow users of 
// IsolatedStorage to increase the quota.  If so, we persist this data.
//--------------------------------------------------------------------------
void AccountingInfo::SetQuota(
         INT64 qwQuota)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;
    {
        AccountingInfoLockHolder pAI(this);

        hr = Map();

        if (SUCCEEDED(hr))
        {
			m_pISSRecord->dwVersion = (m_pISSRecord->dwVersion >= 1) ? m_pISSRecord->dwVersion : 1;
		    m_pISSRecord->qwQuota = qwQuota;
			Unmap();
		}
	}
}

//--------------------------------------------------------------------------
// Reserves space (Increments qwQuota)
// This method is synchronized. If quota + request > limit, method fails
//--------------------------------------------------------------------------
HRESULT AccountingInfo::Reserve(
            ISS_USAGE   cLimit,     // The max allowed
            ISS_USAGE   cRequest,   // amount of space (request / free)
            BOOL        fFree)      // TRUE will free, FALSE will reserve
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;
    {
        AccountingInfoLockHolder pAI(this);

        hr = Map();

        if (SUCCEEDED(hr))
        {
            if (fFree)
            {
                if (m_pISSRecord->cUsage > cRequest)
                    m_pISSRecord->cUsage -= cRequest;
                else
                    m_pISSRecord->cUsage = 0;
            }
            else
            {
                if ((m_pISSRecord->cUsage + cRequest) > cLimit)
                    hr = ISS_E_USAGE_WILL_EXCEED_QUOTA;
                else
                    // Safe to increment quota.
                    m_pISSRecord->cUsage += cRequest;
            }

            Unmap();
        }
        // Lock out of scope here will be released
    }

    return hr;
}

//--------------------------------------------------------------------------
// Method is not synchronized. So the information may not be current.
// This implies "Pass if (Request + GetUsage() < Limit)" is an Error!
// Use Reserve() method instead.
//--------------------------------------------------------------------------
HRESULT AccountingInfo::GetUsage(ISS_USAGE *pcUsage)  // pcUsage - [out]
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;
    {
        AccountingInfoLockHolder pAI(this);

        hr = Map();

        if (! FAILED(hr))
        {
            *pcUsage = m_pISSRecord->cUsage;

            Unmap();
        }
        // Lock out of scope here will be released
    }
    return hr;
}

//--------------------------------------------------------------------------
// Maps the store file into memory
//--------------------------------------------------------------------------
HRESULT AccountingInfo::Map()
{
    STANDARD_VM_CONTRACT;

    // Mapping will fail if filesize is 0
    if (m_hMapping == NULL)
    {
        m_hMapping = WszCreateFileMapping(
            m_hFile,
            NULL,
            PAGE_READWRITE,
            0,
            0,
            NULL);

        if (m_hMapping == NULL)
            return ISS_E_OPEN_FILE_MAPPING;
    }

    _ASSERTE(m_pData == NULL);

    m_pData = (PBYTE) CLRMapViewOfFile(
        m_hMapping,
        FILE_MAP_WRITE,
        0,
        0,
        0);

    if (m_pData == NULL)
        return ISS_E_MAP_VIEW_OF_FILE;

    return S_OK;
}

//--------------------------------------------------------------------------
// Unmaps the store file from memory
//--------------------------------------------------------------------------
void AccountingInfo::Unmap()
{
    CONTRACTL 
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }CONTRACTL_END;


    if (m_pData)
    {
        CLRUnmapViewOfFile(m_pData);
        m_pData = NULL;
    }
}

//--------------------------------------------------------------------------
// Close the store file, and file mapping
//--------------------------------------------------------------------------
void AccountingInfo::Close()
{
    WRAPPER_NO_CONTRACT;
    Unmap();

    if (m_hMapping != NULL)
    {
        CloseHandle(m_hMapping);
        m_hMapping = NULL;
    }

    if (m_hFile != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_hFile);
        m_hFile = INVALID_HANDLE_VALUE;
    }

    if (m_hLock != NULL)
    {
        CloseHandle(m_hLock);
        m_hLock = NULL;
    }

#ifdef _DEBUG
    _ASSERTE(m_dwNumLocks == 0);
#endif
}

//--------------------------------------------------------------------------
// Machine wide Lock
//--------------------------------------------------------------------------
HRESULT AccountingInfo::Lock()
{
    STANDARD_VM_CONTRACT;

    // Lock is intented to be used for inter process/thread synchronization.

#ifdef _DEBUG
    _ASSERTE(m_hLock);

    LOG((LF_STORE, LL_INFO10000, "Lock %S, thread 0x%x start..\n",
            m_wszName, GetCurrentThreadId()));
#endif

    DWORD dwRet;
    {
        // m_hLock is a mutex
#ifndef FEATURE_CORECLR        
        Thread::BeginThreadAffinityAndCriticalRegion();
#endif
        dwRet = WaitForSingleObject(m_hLock, INFINITE);
    }

#ifdef _DEBUG
    if (dwRet == WAIT_OBJECT_0)
        InterlockedIncrement((LPLONG)&m_dwNumLocks);

    switch (dwRet)
    {
    case WAIT_OBJECT_0:
        LOG((LF_STORE, LL_INFO10000, "Loc %S, thread 0x%x - WAIT_OBJECT_0\n",
            m_wszName, GetCurrentThreadId()));
        break;

    case WAIT_ABANDONED:
        LOG((LF_STORE, LL_INFO10000, "Loc %S, thread 0x%x - WAIT_ABANDONED\n",
            m_wszName, GetCurrentThreadId()));
        break;

    case WAIT_FAILED:
        LOG((LF_STORE, LL_INFO10000, "Loc %S, thread 0x%x - WAIT_FAILED\n",
            m_wszName, GetCurrentThreadId()));
        break;

    case WAIT_TIMEOUT:
        LOG((LF_STORE, LL_INFO10000, "Loc %S, thread 0x%x - WAIT_TIMEOUT\n",
            m_wszName, GetCurrentThreadId()));
        break;

    default:
        LOG((LF_STORE, LL_INFO10000, "Loc %S, thread 0x%x - 0x%x\n",
            m_wszName, GetCurrentThreadId(), dwRet));
        break;
    }

#endif

    if ((dwRet == WAIT_OBJECT_0) || (dwRet == WAIT_ABANDONED))
        return S_OK;

    return ISS_E_LOCK_FAILED;
}

//--------------------------------------------------------------------------
// Unlock the store
//--------------------------------------------------------------------------
void AccountingInfo::Unlock()
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

#ifdef _DEBUG
    _ASSERTE(m_hLock);
    _ASSERTE(m_dwNumLocks >= 1);

    LOG((LF_STORE, LL_INFO10000, "UnLoc %S, thread 0x%x\n",
        m_wszName, GetCurrentThreadId()));
#endif

    BOOL released;
    released = ReleaseMutex(m_hLock);
    _ASSERTE(released);

#ifdef _DEBUG
    InterlockedDecrement((LPLONG)&m_dwNumLocks);
#endif

#ifndef FEATURE_CORECLR        
    Thread::EndThreadAffinityAndCriticalRegion();
#endif
}

#endif
