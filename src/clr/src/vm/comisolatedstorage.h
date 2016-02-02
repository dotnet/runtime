// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//============================================================
//
// Class: COMIsolatedStorage
//
//
// Purpose: Native Implementation of IsolatedStorage
//

//

//============================================================


#ifndef __COMISOLATEDSTORAGE_h__
#define __COMISOLATEDSTORAGE_h__
#ifdef FEATURE_ISOSTORE

// Dependency in managed : System.IO.IsolatedStorage.IsolatedStorage.cs
#ifndef FEATURE_ISOSTORE_LIGHT
#define ISS_ROAMING_STORE   0x08
#define ISS_MACHINE_STORE   0x10
#endif // !FEATURE_ISOSTORE_LIGHT

class COMIsolatedStorage
{
public:
#ifndef FEATURE_ISOSTORE_LIGHT    
    static
    void QCALLTYPE GetCaller(QCall::ObjectHandleOnStack retAssembly);
#endif // !FEATURE_ISOSTORE_LIGHT

    static void DECLSPEC_NORETURN ThrowISS(HRESULT hr);

private:
#ifndef FEATURE_ISOSTORE_LIGHT  
    static StackWalkAction StackWalkCallBack(CrawlFrame* pCf, PVOID ppv);
#endif // !FEATURE_ISOSTORE_LIGHT
};

// --- [ Structure of data that gets persisted on disk ] -------------(Begin)

// non-standard extension: 0-length arrays in struct
#ifdef _MSC_VER
#pragma warning(disable:4200)
#endif
#include <pshpack1.h>

typedef unsigned __int64 QWORD;

typedef QWORD ISS_USAGE;

// Accounting Information
typedef struct
{
    ISS_USAGE   cUsage;           // The amount of resource used

	DWORD       dwVersion;        // Version of bookkeeping file on disk (so we know the layout)
	QWORD       qwQuota;          // Quota stored on disk (persisted if increased by the host)
    QWORD       qwReserved[5];    // For future use, set to 0
	DWORD       dwReserved;       // For future use, set to 0

} ISS_RECORD;

#include <poppack.h>
#ifdef _MSC_VER
#pragma warning(default:4200)
#endif

// --- [ Structure of data that gets persisted on disk ] ---------------(End)

class AccountingInfo
{
public:

    // The file name is used to open / create the file.
    // A synchronization object will also be created using the sync name

    AccountingInfo(const WCHAR *wszFileName, const WCHAR *wszSyncName);

    // Init should be called before Reserve / GetUsage is called.

    HRESULT Init();             // Creates the file if necessary

    // Reserves space (Increments qwQuota)
    // This method is synchrinized. If quota + request > limit, method fails

    HRESULT Reserve(
        ISS_USAGE   cLimit,     // The max allowed
        ISS_USAGE   cRequest,   // amount of space (request / free)
        BOOL        fFree);     // TRUE will free, FALSE will reserve

    // Method is not synchronized. So the information may not be current.
    // This implies "Pass if (Request + GetUsage() < Limit)" is an Error!
    // Use Reserve() method instead.

    HRESULT GetUsage(
        ISS_USAGE   *pcUsage);  // [out] The amount of space / resource used

	BOOL GetQuota(
        INT64 *qwQuota); // [out] The Quota stored on disk (if there is one)

	void SetQuota(
        INT64 qwQuota); // [out] The Quota stored on disk (if there is one)

    // Frees cached pointers, Closes handles

    ~AccountingInfo();

    static void AcquireLock(AccountingInfo *pAI) {
        STANDARD_VM_CONTRACT;
        HRESULT hr = pAI->Lock();
        if (FAILED(hr)) COMIsolatedStorage::ThrowISS(hr);
    }
    static void ReleaseLock(AccountingInfo *pAI) { 
        WRAPPER_NO_CONTRACT;
        pAI->Unlock(); 
    }
    typedef Holder<AccountingInfo *, AccountingInfo::AcquireLock, AccountingInfo::ReleaseLock> AccountingInfoLockHolder;

private:
    HRESULT Lock();     // Machine wide Lock
    void    Unlock();   // Unlock the store

    HRESULT Map();      // Maps the store file into memory
    void    Unmap();    // Unmaps the store file from memory
    void    Close();    // Close the store file, and file mapping

    WCHAR          *m_wszFileName;  // The file name
    HANDLE          m_hFile;        // File handle for the file
    HANDLE          m_hMapping;     // File mapping for the memory mapped file

    // members used for synchronization
    WCHAR          *m_wszName;      // The name of the mutex object
    HANDLE          m_hLock;        // Handle to the Mutex object

#ifdef _DEBUG
    ULONG           m_dwNumLocks;   // The number of locks owned by this object
#endif

    union {
        PBYTE       m_pData;        // The start of file stream
        ISS_RECORD *m_pISSRecord;
    };
};
class COMIsolatedStorageFile
{
public:
    static
    void QCALLTYPE GetRootDir(DWORD                      dwFlags,
                              QCall::StringHandleOnStack retRootDir);

    static
    UINT64 QCALLTYPE GetUsage(__in_opt AccountingInfo * pAI);

    static
    void QCALLTYPE Reserve(__in_opt AccountingInfo * pAI,
                           UINT64                    qwQuota,
                           UINT64                    qwReserve,
                           BOOL                      fFree);

	static 
    BOOL QCALLTYPE GetQuota(__in_opt AccountingInfo * pAI,
	                        __out INT64 * qwQuota);

    static 
    void QCALLTYPE SetQuota(__in_opt AccountingInfo * pAI,
	                                 INT64 qwQuota);

    static
    AccountingInfo * QCALLTYPE Open(LPCWSTR wszFileName,
                                    LPCWSTR wszSyncName);

    static
    void QCALLTYPE Close(__in_opt AccountingInfo * pAI);

    static
    BOOL QCALLTYPE Lock(__in AccountingInfo * handle,
                             BOOL             fLock);

#ifndef FEATURE_ISOSTORE_LIGHT      
    // create the machine store root directory and apply the correct DACL
    static
    void QCALLTYPE CreateDirectoryWithDacl(LPCWSTR wszPath);
#endif // !FEATURE_ISOSTORE_LIGHT

private:

    static void GetRootDirInternal(DWORD dwFlags, __in_ecount(cPath) WCHAR *path, DWORD cPath);
    static void CreateDirectoryIfNotPresent(__in_z const WCHAR *path, LPSECURITY_ATTRIBUTES lpSecurityAttributes = NULL);
#ifndef FEATURE_ISOSTORE_LIGHT      
    static BOOL GetMachineStoreDirectory(__out_ecount(cchMachineStorageRoot) WCHAR *wszMachineStorageRoot, DWORD cchMachineStorageRoot);
    static HRESULT GetMachineStoreDacl(PACL *ppAcl);
#endif // !FEATURE_ISOSTORE_LIGHT
};
#endif // FEATURE_ISOSTORE

#endif  // __COMISOLATEDSTORAGE_h__

