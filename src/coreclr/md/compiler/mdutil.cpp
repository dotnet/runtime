// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
        }
    }

ErrExit:
    return bRemoved;
}  // LOADEDMODULES::RemoveModuleFromLoadedList

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

//*******************************************************************************
//
// Determine the blob size base of the ELEMENT_TYPE_* associated with the blob.
// This cannot be a table lookup because ELEMENT_TYPE_STRING is an unicode string.
// The size of the blob is determined by calling u16_strstr of the string + 1.
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
            ulSize = (ULONG)(sizeof(WCHAR) * u16_strlen((LPWSTR)pValue));
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
