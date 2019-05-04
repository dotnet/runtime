// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// MDUtil.cpp
// 

//
// contains utility code to MD directory. This is only used for the full version.
//
//*****************************************************************************
#include "stdafx.h"
#include "metadata.h"
#include "mdutil.h"
#include "regmeta.h"
#include "disp.h"
#include "mdcommon.h"
#include "importhelper.h"
#include "sstring.h"

#include <rwutil.h>

#if defined(FEATURE_METADATA_IN_VM)

LOADEDMODULES * LOADEDMODULES::s_pLoadedModules = NULL;
UTSemReadWrite * LOADEDMODULES::m_pSemReadWrite = NULL;
RegMeta * LOADEDMODULES::m_HashedModules[LOADEDMODULES_HASH_SIZE] = { NULL };

//*****************************************************************************
// Hash a file name.
//*****************************************************************************
ULONG LOADEDMODULES::HashFileName(
    LPCWSTR szName)
{
    return HashString(szName) % LOADEDMODULES_HASH_SIZE;
} // LOADEDMODULES::HashFileName

//---------------------------------------------------------------------------------------
// 
// Initialize the static instance and lock.
// 
HRESULT 
LOADEDMODULES::InitializeStatics()
{
    HRESULT hr = S_OK;
    
    if (VolatileLoad(&s_pLoadedModules) == NULL)
    {
        // Initialize global read-write lock
        {
            NewHolder<UTSemReadWrite> pSemReadWrite = new (nothrow) UTSemReadWrite();
            IfNullGo(pSemReadWrite);
            IfFailGo(pSemReadWrite->Init());
            
            if (InterlockedCompareExchangeT<UTSemReadWrite *>(&m_pSemReadWrite, pSemReadWrite, NULL) == NULL)
            {   // We won the initialization race
                pSemReadWrite.SuppressRelease();
            }
        }
        
        // Initialize the global instance
        {
            NewHolder<LOADEDMODULES> pLoadedModules = new (nothrow) LOADEDMODULES();
            IfNullGo(pLoadedModules);
            
            {
                LOCKWRITE();
                
                if (VolatileLoad(&s_pLoadedModules) == NULL)
                {
                    VolatileStore(&s_pLoadedModules, pLoadedModules.Extract());
                }
            }
        }
    }
    
ErrExit:
    return hr;
} // LOADEDMODULES::InitializeStatics

//---------------------------------------------------------------------------------------
// 
// Destroy the static instance and lock.
// 
void 
LOADEDMODULES::DeleteStatics()
{
    HRESULT hr = S_OK;
    
    if (s_pLoadedModules != NULL)
    {
        delete s_pLoadedModules;
        s_pLoadedModules = NULL;
    }
    if (m_pSemReadWrite != NULL)
    {
        delete m_pSemReadWrite;
        m_pSemReadWrite = NULL;
    }
} // LOADEDMODULES::DeleteStatics

//*****************************************************************************
// Add a RegMeta pointer to the loaded module list
//*****************************************************************************
HRESULT LOADEDMODULES::AddModuleToLoadedList(RegMeta * pRegMeta)
{
    HRESULT    hr = NOERROR;
    RegMeta ** ppRegMeta;
    
    IfFailGo(InitializeStatics());
    
    {
        LOCKWRITE();
    
        ppRegMeta = s_pLoadedModules->Append();
        IfNullGo(ppRegMeta);
    
        // The cache holds a copy of the pointer, but no ref-count.  There is no
        //  point to the ref-count, because it just changes comparisons against 0
        //  to comparisons against 1.
        *ppRegMeta = pRegMeta;
    
        // If the module is read-only, hash it.
        if (pRegMeta->IsReadOnly())
        {
            ULONG ixHash = HashFileName(pRegMeta->GetNameOfDBFile());
            m_HashedModules[ixHash] = pRegMeta;
        }
    }
    
ErrExit:    
    return hr;
} // LOADEDMODULES::AddModuleToLoadedList

//*****************************************************************************
// Remove a RegMeta pointer from the loaded module list
//*****************************************************************************
BOOL LOADEDMODULES::RemoveModuleFromLoadedList(RegMeta * pRegMeta)
{
    BOOL  bRemoved = FALSE;     // Was this module removed from the cache?
    int   iFound = -1;          // Index at which it was found.
    ULONG cRef;                 // Ref count of the module.
    
    // Lock the cache for write, so that no other thread will find what this 
    //  thread is about to delete, and so that no other thread will delete
    //  what this thread is about to try to find.
    HRESULT hr = S_OK;
    
    IfFailGo(InitializeStatics());
    
    {
        LOCKWRITE();
    
        // Search for this module in list of loaded modules.
        int count = s_pLoadedModules->Count();
        for (int index = 0; index < count; index++)
        {
            if ((*s_pLoadedModules)[index] == pRegMeta)
            {   // found a match to remove
                iFound = index;
                break;
            }
        }
    
        // If the module is still in the cache, it hasn't been deleted yet.
        if (iFound >= 0)
        {
            // See if there are any external references left.
            cRef = pRegMeta->GetRefCount();

            // If the cRef that we got from the module is zero, it will stay that way,
            //  because no other thread can discover the module while this thread holds
            //  the lock.

            // OTOH, if the cRef is not zero, this thread can just return, because the
            //  other thread will eventually take the ref count to zero, and will then 
            //  come through here to clean up the module. And this thread must not 
            //  delete the module out from under other threads.

            // It is possible that the cRef is zero, yet another thread has a pointer that
            //  it discovered before this thread took the lock.  (And that thread has
            //  released the ref-counts.)  In such a case, this thread can still remove the 
            //  module from the cache, and tell the caller to delete it, because the
            //  other thread will wait on the lock, then discover that the module
            //  is not in the cache, and it won't try to delete the module.

            if (cRef != 0)
            {   // Some other thread snuck in and found the entry in the cache.
                return FALSE;
            }

            // No other thread owns the object.  Remove from cache, and tell caller
            //  that we're done with it.  (Caller will delete.)
            s_pLoadedModules->Delete(iFound);
            bRemoved = TRUE;

            // If the module is read-only, remove from hash.
            if (pRegMeta->IsReadOnly())
            {
                // There may have been multiple capitalizations pointing to the same entry.
                //  Find and remove all of them.
                for (ULONG ixHash = 0; ixHash < LOADEDMODULES_HASH_SIZE; ++ixHash)
                {
                    if (m_HashedModules[ixHash] == pRegMeta)
                        m_HashedModules[ixHash] = NULL;
                }
            }
        }
    }
    
ErrExit:
    return bRemoved;
}  // LOADEDMODULES::RemoveModuleFromLoadedList


//*****************************************************************************
// Search the cached RegMetas for a given scope.
//*****************************************************************************
HRESULT LOADEDMODULES::FindCachedReadOnlyEntry(
    LPCWSTR    szName,      // Name of the desired file.
    DWORD      dwOpenFlags, // Flags the new file is opened with.
    RegMeta ** ppMeta)      // Put found RegMeta here.
{
    RegMeta * pRegMeta = 0;
    BOOL      bWillBeCopyMemory;    // Will the opened file be copied to memory?
    DWORD     dwLowFileSize;        // Low bytes of this file's size
    DWORD     dwLowFileTime;        // Low butes of this file's last write time
    HRESULT   hr;
    ULONG     ixHash = 0;
    
    IfFailGo(InitializeStatics());
    
    {
        LOCKREAD();

        hr = S_FALSE; // We haven't found a match yet.

        // Avoid confusion.
        *ppMeta = NULL;
    
        bWillBeCopyMemory = IsOfCopyMemory(dwOpenFlags);

        // The cache is locked for read, so the list will not change.
    
        // Figure out the size and timestamp of this file
        WIN32_FILE_ATTRIBUTE_DATA faData;
        if (!WszGetFileAttributesEx(szName, GetFileExInfoStandard, &faData))
            return E_FAIL;
        dwLowFileSize = faData.nFileSizeLow;
        dwLowFileTime = faData.ftLastWriteTime.dwLowDateTime;

        // Check the hash first.
        ixHash = HashFileName(szName);
        if ((pRegMeta = m_HashedModules[ixHash]) != NULL)
        {
            _ASSERTE(pRegMeta->IsReadOnly());

            // Only match if the IsOfCopyMemory() bit is the same in both.  This is because
            //  when ofCopyMemory is set, the file is not locked on disk, and may become stale
            //  in memory.
            //
            // Also, only match if the date and size are the same
            if (pRegMeta->IsCopyMemory() == bWillBeCopyMemory &&
                pRegMeta->GetLowFileTimeOfDBFile() == dwLowFileTime &&
                pRegMeta->GetLowFileSizeOfDBFile() == dwLowFileSize)
            {
                // If the name matches...
                LPCWSTR pszName = pRegMeta->GetNameOfDBFile();
    #ifdef FEATURE_CASE_SENSITIVE_FILESYSTEM
                if (wcscmp(szName, pszName) == 0)
    #else
                if (SString::_wcsicmp(szName, pszName) == 0)
    #endif
                {
                    ULONG cRefs;
                
                    // Found it.  Add a reference, and return it.
                    *ppMeta = pRegMeta;
                    cRefs = pRegMeta->AddRef();
                
                    LOG((LF_METADATA, LL_INFO10, "Disp::OpenScope found cached RegMeta in hash: %#8x, crefs: %d\n", pRegMeta, cRefs));
                
                    return S_OK;
                }
            }
        }

        // Not found in hash; loop through each loaded modules
        int count = s_pLoadedModules->Count();
        for (int index = 0; index < count; index++)
        {
            pRegMeta = (*s_pLoadedModules)[index];

            // If the module is read-only, and the CopyMemory bit matches, and the date
            // and size are the same....
            if (pRegMeta->IsReadOnly() && 
                pRegMeta->IsCopyMemory() == bWillBeCopyMemory &&
                pRegMeta->GetLowFileTimeOfDBFile() == dwLowFileTime &&
                pRegMeta->GetLowFileSizeOfDBFile() == dwLowFileSize)
            {
                // If the name matches...
                LPCWSTR pszName = pRegMeta->GetNameOfDBFile();
    #ifdef FEATURE_CASE_SENSITIVE_FILESYSTEM
                if (wcscmp(szName, pszName) == 0)
    #else
                if (SString::_wcsicmp(szName, pszName) == 0)
    #endif
                {
                    ULONG cRefs;

                    // Found it.  Add a reference, and return it.
                    *ppMeta = pRegMeta;
                    cRefs = pRegMeta->AddRef();

                    // Update the hash.
                    m_HashedModules[ixHash] = pRegMeta;

                    LOG((LF_METADATA, LL_INFO10, "Disp::OpenScope found cached RegMeta by search: %#8x, crefs: %d\n", pRegMeta, cRefs));
                
                    return S_OK;
                }
            }
        }
    }

ErrExit:
    // Didn't find it.
    LOG((LF_METADATA, LL_INFO10, "Disp::OpenScope did not find cached RegMeta\n"));

    _ASSERTE(hr != S_OK);
    return hr;
} // LOADEDMODULES::FindCachedReadOnlyEntry

#ifdef _DEBUG

//*****************************************************************************
// Search the cached RegMetas for a given scope.
//*****************************************************************************
BOOL LOADEDMODULES::IsEntryInList(
    RegMeta * pRegMeta)
{
    HRESULT hr = S_OK;
    
    IfFailGo(InitializeStatics());
    
    {
        LOCKREAD();
    
        // Loop through each loaded modules
        int count = s_pLoadedModules->Count();
        for (int index = 0; index < count; index++)
        {
            if ((*s_pLoadedModules)[index] == pRegMeta)
            {
                return TRUE;
            }
        }
    }

ErrExit:
    return FALSE;
} // LOADEDMODULES::IsEntryInList

#endif //_DEBUG

#endif //FEATURE_METADATA_IN_VM 

#ifdef FEATURE_METADATA_IN_VM

//*****************************************************************************
// Remove a RegMeta pointer from the loaded module list
//*****************************************************************************
// static
HRESULT 
LOADEDMODULES::ResolveTypeRefWithLoadedModules(
    mdTypeRef          tkTypeRef,       // [IN] TypeRef to be resolved.
    RegMeta *          pTypeRefRegMeta, // [IN] Scope in which the TypeRef is defined.
    IMetaModelCommon * pTypeRefScope,   // [IN] Scope in which the TypeRef is defined.
    REFIID             riid,            // [IN] iid for the return interface.
    IUnknown **        ppIScope,        // [OUT] Return interface.
    mdTypeDef *        ptd)             // [OUT] TypeDef corresponding the TypeRef.
{
    HRESULT   hr = NOERROR;
    RegMeta * pRegMeta;
    CQuickArray<mdTypeRef> cqaNesters;
    CQuickArray<LPCUTF8>   cqaNesterNamespaces;
    CQuickArray<LPCUTF8>   cqaNesterNames;
    
    IfFailGo(InitializeStatics());
    
    {
        LOCKREAD();
    
        // Get the Nesting hierarchy.
        IfFailGo(ImportHelper::GetNesterHierarchy(
            pTypeRefScope, 
            tkTypeRef, 
            cqaNesters, 
            cqaNesterNamespaces, 
            cqaNesterNames));

        int count = s_pLoadedModules->Count();
        for (int index = 0; index < count; index++)
        {
            pRegMeta = (*s_pLoadedModules)[index];
        
            {
                // Do not lock the TypeRef RegMeta (again), as it is already locked for read by the caller.
                // The code:UTSemReadWrite will block ReadLock even for thread holding already the read lock if 
                // some other thread is waiting for WriteLock on the same lock. That would cause dead-lock if we 
                // try to lock for read again here.
                CMDSemReadWrite cSemRegMeta((pRegMeta == pTypeRefRegMeta) ? NULL : pRegMeta->GetReaderWriterLock());
                IfFailGo(cSemRegMeta.LockRead());
            
                hr = ImportHelper::FindNestedTypeDef(
                    pRegMeta->GetMiniMd(), 
                    cqaNesterNamespaces, 
                    cqaNesterNames, 
                    mdTokenNil, 
                    ptd);
            }
            if (hr == CLDB_E_RECORD_NOTFOUND)
            {   // Process next MetaData module
                continue;
            }
            IfFailGo(hr);
        
            // Found a loaded module containing the TypeDef.
            IfFailGo(pRegMeta->QueryInterface(riid, (void **)ppIScope));
            break;
        }
    }
    if (FAILED(hr))
    {
        // cannot find the match!
        hr = E_FAIL;
    }
ErrExit:
    return hr;
}    // LOADEDMODULES::ResolveTypeRefWithLoadedModules

#endif //FEATURE_METADATA_IN_VM

#if defined(FEATURE_METADATA_IN_VM)

//*****************************************************************************
// This is a routine to try to find a class implementation given its fully
// qualified name by using the CORPATH environment variable.  CORPATH is a list
// of directories (like PATH).  Before checking CORPATH, this checks the current
// directory, then the directory that the exe lives in.  The search is
// performed by parsing off one element at a time from the class name,
// appending it to the directory and looking for a subdirectory or image with
// that name.  If the subdirectory exists, it drills down into that subdirectory
// and tries the next element of the class name.  When it finally bottoms out
// but can't find the image it takes the rest of the fully qualified class name
// and appends them with intervening '.'s trying to find a matching DLL.
// Example:
//
// CORPATH=c:\bin;c:\prog
// classname = namespace.class
//
// checks the following things in order:
// c:\bin\namespace, (if <-exists) c:\bin\namespace\class.dll,
//        c:\bin\namespace.dll, c:\bin\namespace.class.dll
// c:\prog\namespace, (if <-exists) c:\prog\namespace\class.dll,
//        c:\prog\namespace.dll, c:\prog\namespace.class.dll
//*****************************************************************************
HRESULT CORPATHService::GetClassFromCORPath(
    __in __in_z LPWSTR        wzClassname,            // [IN] fully qualified class name
    mdTypeRef   tr,                     // [IN] TypeRef to be resolved.
    IMetaModelCommon *pCommon,          // [IN] Scope in which the TypeRef is defined.
    REFIID        riid,                   // [IN] Interface type to be returned.
    IUnknown    **ppIScope,             // [OUT] Scope in which the TypeRef resolves.
    mdTypeDef    *ptd)                    // [OUT] typedef corresponding the typeref
{
    PathString    rcCorPath;  // The CORPATH environment variable.
    LPWSTR        szCorPath;  // Used to parse CORPATH.
    int            iLen;                   // Length of the directory.
    PathString     rcCorDir;    // Buffer for the directory.
    WCHAR        *temp;                  // Used as a parsing temp.
    WCHAR        *szSemiCol;

    // Get the CORPATH environment variable.
    if (WszGetEnvironmentVariable(W("CORPATH"), rcCorPath))
    {
        NewArrayHolder<WCHAR> szCorPathHolder = rcCorPath.GetCopyOfUnicodeString();
        szCorPath = szCorPathHolder.GetValue();
        // Try each directory in the path.
        for(;*szCorPath != W('\0');)
        {
            // Get the next directory off the path.
            if ((szSemiCol = wcschr(szCorPath, W(';'))))
            {
                temp = szCorPath;
                *szSemiCol = W('\0');
                szCorPath = szSemiCol + 1;
            }
            else 
            {
                temp = szCorPath;
                szCorPath += wcslen(temp);
            }

            rcCorDir.Set(temp);

            // Check if we can find the class in the directory.
            if (CORPATHService::GetClassFromDir(wzClassname, rcCorDir, tr, pCommon, riid, ppIScope, ptd) == S_OK)
                return S_OK;
        }
    }

    //<TODO>These should go before the path search, but it will cause test
    // some headaches right now, so we'll give them a little time to transition.</TODO>

    // Try the current directory first.
    if ((iLen = WszGetCurrentDirectory( rcCorDir)) > 0 &&
        CORPATHService::GetClassFromDir(wzClassname, rcCorDir, tr, pCommon, riid, ppIScope, ptd) == S_OK)
    {
        return S_OK;
    }
    
    // Try the app directory next.
    if ((iLen = WszGetModuleFileName(NULL, rcCorDir)) > 0)
    {
        
        if(SUCCEEDED(CopySystemDirectory(rcCorDir, rcCorDir)) && 
           CORPATHService::GetClassFromDir(
                    wzClassname, 
                    rcCorDir, 
                    tr, 
                    pCommon, 
                    riid, 
                    ppIScope, 
                    ptd) == S_OK)
        {
            return (S_OK);
        }
    }

    // Couldn't find the class.
    return S_FALSE;
} // CORPATHService::GetClassFromCORPath

//*****************************************************************************
// This is used in conjunction with GetClassFromCORPath.  See it for details
// of the algorithm.
//*****************************************************************************
HRESULT CORPATHService::GetClassFromDir(
    __in __in_z LPWSTR        wzClassname,            // Fully qualified class name.
    __in SString&             directory,             // Directory to try. at most appended with a '\\'
    mdTypeRef   tr,                     // TypeRef to resolve.
    IMetaModelCommon *pCommon,          // Scope in which the TypeRef is defined.
    REFIID        riid, 
    IUnknown    **ppIScope,
    mdTypeDef    *ptd)                    // [OUT] typedef
{
    WCHAR    *temp;                        // Used as a parsing temp.
    int        iTmp;
    bool    bContinue;                    // Flag to check if the for loop should end.
    LPWSTR    wzSaveClassname = NULL;     // Saved offset into the class name string.
    
    // Process the class name appending each segment of the name to the
    // directory until we find a DLL.
    PathString dir;
    if (!directory.EndsWith(DIRECTORY_SEPARATOR_CHAR_W))
    {
        directory.Append(DIRECTORY_SEPARATOR_CHAR_W);
    }

    for(;;)
    {
        bContinue = false;
        dir.Set(directory);

        if ((temp = wcschr(wzClassname, NAMESPACE_SEPARATOR_WCHAR)) != NULL)
        {
            *temp = W('\0');  //terminate with null so that it can be appended
            dir.Append(wzClassname);
            *temp = NAMESPACE_SEPARATOR_WCHAR;   //recover the '.'

            wzClassname = temp+1;
            // Check if a directory by this name exists.
            DWORD iAttrs = WszGetFileAttributes(dir);
            if (iAttrs != 0xffffffff && (iAttrs & FILE_ATTRIBUTE_DIRECTORY))
            {
                // Next element in the class spec.
                bContinue = true;
                wzSaveClassname = wzClassname;
            }
        }
        else
        {
            dir.Append(wzClassname);

            // Advance past the class name.
            iTmp = (int)wcslen(wzClassname);
            wzClassname += iTmp;
        }

        // Try to load the image.
        dir.Append(W(".dll"));
       
        // OpenScope given the dll name and make sure that the class is defined in the module.
        if ( SUCCEEDED( CORPATHService::FindTypeDef(dir, tr, pCommon, riid, ppIScope, ptd) ) )
        {
            return (S_OK);
        }

        // If we didn't find the dll, try some more.
        while (*wzClassname != W('\0'))
        {
            // Find the length of the next class name element.
            if ((temp = wcschr(wzClassname, NAMESPACE_SEPARATOR_WCHAR)) == NULL)
            {
                temp = wzClassname + wcslen(wzClassname);
            }

            // Tack on ".element.dll"
            SString::Iterator iter = dir.End();
            BOOL findperiod = dir.FindBack(iter, NAMESPACE_SEPARATOR_WCHAR);
            _ASSERTE(findperiod);
            iter++;
            dir.Truncate(iter);
            
            WCHAR save = *temp;
            *temp      = W('\0');
            dir.Append(wzClassname);  //element
            *temp = save;
            
            // Try to load the image.
            dir.Append(W(".dll"));
           
            // OpenScope given the dll name and make sure that the class is defined in the module.
            if ( SUCCEEDED( CORPATHService::FindTypeDef(dir, tr, pCommon, riid, ppIScope, ptd) ) )
            {
                return (S_OK);
            }

            // Advance to the next class name element.
            wzClassname = temp;
            if (*wzClassname != '\0')
                ++wzClassname;
        }
        if (bContinue)
        {
            
            wzClassname = wzSaveClassname;
        }
        else
        {
            break;
        }
    }
    return S_FALSE;
} // CORPATHService::GetClassFromDir

//*************************************************************
//
// Open the file with name wzModule and check to see if there is a type 
// with namespace/class of wzNamespace/wzType. If so, return the RegMeta
// corresponding to the file and the mdTypeDef of the typedef
//
//*************************************************************
HRESULT CORPATHService::FindTypeDef(
    __in __in_z LPCWSTR wzModule,    // name of the module that we are going to open
    mdTypeRef          tr,          // TypeRef to resolve.
    IMetaModelCommon * pCommon,     // Scope in which the TypeRef is defined.
    REFIID             riid, 
    IUnknown **        ppIScope,
    mdTypeDef *        ptd)         // [OUT] the type that we resolve to
{
    HRESULT                         hr = NOERROR;
    NewHolder<Disp>                 pDisp;
    ReleaseHolder<IMetaDataImport2> pImport = NULL;
    CQuickArray<mdTypeRef>          cqaNesters;
    CQuickArray<LPCUTF8>            cqaNesterNamespaces;
    CQuickArray<LPCUTF8>            cqaNesterNames;
    RegMeta *                       pRegMeta;
    
    _ASSERTE((ppIScope != NULL) && (ptd != NULL));
    
    *ppIScope = NULL;
    
    pDisp = new (nothrow) Disp;
    IfNullGo(pDisp);
    
    IfFailGo(pDisp->OpenScope(wzModule, 0, IID_IMetaDataImport2, (IUnknown **)&pImport));
    pRegMeta = static_cast<RegMeta *>(pImport.GetValue());
    
    // Get the Nesting hierarchy.
    IfFailGo(ImportHelper::GetNesterHierarchy(pCommon, tr, cqaNesters,
                                cqaNesterNamespaces, cqaNesterNames));

    hr = ImportHelper::FindNestedTypeDef(
                                pRegMeta->GetMiniMd(),
                                cqaNesterNamespaces,
                                cqaNesterNames,
                                mdTokenNil,
                                ptd);
    if (SUCCEEDED(hr))
    {
        *ppIScope = pImport.Extract();
    }
    
ErrExit:
    return hr;
} // CORPATHService::FindTypeDef

#endif //FEATURE_METADATA_IN_VM

//*******************************************************************************
//
// Determine the blob size base of the ELEMENT_TYPE_* associated with the blob.
// This cannot be a table lookup because ELEMENT_TYPE_STRING is an unicode string.
// The size of the blob is determined by calling wcsstr of the string + 1.
//
//*******************************************************************************
ULONG _GetSizeOfConstantBlob(
    DWORD  dwCPlusTypeFlag, // ELEMENT_TYPE_*
    void * pValue,          // BLOB value
    ULONG  cchString)       // String length in wide chars, or -1 for auto.
{
    ULONG ulSize = 0;

    switch (dwCPlusTypeFlag)
    {
    case ELEMENT_TYPE_BOOLEAN:
        ulSize = sizeof(BYTE);
        break;
    case ELEMENT_TYPE_I1:
    case ELEMENT_TYPE_U1:
        ulSize = sizeof(BYTE);
        break;
    case ELEMENT_TYPE_CHAR:
    case ELEMENT_TYPE_I2:
    case ELEMENT_TYPE_U2:
        ulSize = sizeof(SHORT);
        break;
    case ELEMENT_TYPE_I4:
    case ELEMENT_TYPE_U4:
    case ELEMENT_TYPE_R4:
        ulSize = sizeof(LONG);
 
        break;
        
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
    case ELEMENT_TYPE_R8:
        ulSize = sizeof(DOUBLE);
        break;

    case ELEMENT_TYPE_STRING:
        if (pValue == 0)
            ulSize = 0;
        else
        if (cchString != (ULONG) -1)
            ulSize = cchString * sizeof(WCHAR);
        else
            ulSize = (ULONG)(sizeof(WCHAR) * wcslen((LPWSTR)pValue));
        break;

    case ELEMENT_TYPE_CLASS:
        // This was originally 'sizeof(IUnknown *)', but that varies across platforms.
        //  The only legal value is a null pointer, and on 32 bit platforms we've already
        //  stored 32 bits, so we will use just 32 bits of null.  If the type is
        //  E_T_CLASS, the caller should know that the value is always NULL anyway.
        ulSize = sizeof(ULONG);
        break;
    default:
        _ASSERTE(!"Not a valid type to specify default value!");
        break;
    }
    return ulSize;
} // _GetSizeOfConstantBlob
