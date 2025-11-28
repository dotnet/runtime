// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//*****************************************************************************

//
// RegMeta.cpp
//
// Implementation for meta data public interface methods for full version.
//
//*****************************************************************************
#include "stdafx.h"
#include "regmeta.h"
#include "metadata.h"
#include "corerror.h"
#include "mdutil.h"
#include "rwutil.h"
#include "mdlog.h"
#include "importhelper.h"
#include "filtermanager.h"
#include "switches.h"
#include "posterror.h"
#include "stgio.h"
#include "sstring.h"

#include <metamodelrw.h>

#if defined(_DEBUG)
#define LOGGING
#endif
#include <log.h>

//*****************************************************************************
// Call this after initialization is complete.
//*****************************************************************************
HRESULT RegMeta::AddToCache()
{
#if defined(FEATURE_METADATA_IN_VM)
    HRESULT hr = S_OK;

    // The ref count must be > 0 before the module is published, else another
    //  thread could find, use, and release the module, causing it to be deleted
    //  before this thread gets a chance to addref.
    _ASSERTE(GetRefCount() > 0);
    // add this RegMeta to the loaded module list.
    m_bCached = true;
    IfFailGo(LOADEDMODULES::AddModuleToLoadedList(this));
ErrExit:
    if (FAILED(hr))
    {
        _ASSERTE(!LOADEDMODULES::IsEntryInList(this));
        m_bCached = false;
    }
    return hr;
#else // FEATURE_METADATA_IN_VM
    return S_OK;
#endif // FEATURE_METADATA_IN_VM
} // RegMeta::AddToCache


//*****************************************************************************
// Implementation of IMetaDataImport::ResolveTypeRef to resolve a typeref across scopes.
//
// Arguments:
//    tr - typeref within this scope to resolve
//    riid - interface on ppIScope to support
//    ppIScope - out-parameter to get metadata scope for typedef (*ptd)
//    ptd - out-parameter to get typedef that the ref resolves to.
//
// Notes:
// TypeDefs define a type within a scope. TypeRefs refer to type-defs in other scopes
// and allow you to import a type from another scope. This function attempts to determine
// which type-def a type-ref points to.
//
// This resolve (type-ref, this cope) --> (type-def=*ptd, other scope=*ppIScope)
//
// However, this resolution requires knowing what modules have been loaded, which is not decided
// until runtime via loader. Thus this interface can't possibly be correct since it doesn't have
// that knowledge. Furthermore, when inspecting metadata from another process (such as a debugger
// inspecting the debuggee's metadata), this API can be truly misleading.
//
// This API usage should be avoided. It is kept to avoid breaking profilers.
//
//*****************************************************************************
STDMETHODIMP
RegMeta::ResolveTypeRef(
    mdTypeRef   tr,
    REFIID      riid,
    IUnknown ** ppIScope,
    mdTypeDef * ptd)
{
#ifdef FEATURE_METADATA_IN_VM
    HRESULT hr;

    TypeRefRec * pTypeRefRec;
    WCHAR        wzNameSpace[MAX_PATH];
    CMiniMdRW *  pMiniMd = NULL;

    LOCKREAD();

    pMiniMd = &(m_pStgdb->m_MiniMd);

    _ASSERTE((ppIScope != NULL) && (ptd != NULL));

    // Init the output values.
    *ppIScope = NULL;
    *ptd = 0;

    if (IsNilToken(tr))
    {
        if (ptd != NULL)
        {
            *ptd = mdTypeDefNil;
        }

        if (ppIScope != NULL)
        {
            *ppIScope = NULL;
        }

        hr = E_INVALIDARG;
        goto ErrExit;
    }

    if (TypeFromToken(tr) == mdtTypeDef)
    {
        // Shortcut when we receive a TypeDef token
        *ptd = tr;
        hr = this->QueryInterface(riid, (void **)ppIScope);
        goto ErrExit;
    }

    // Get the class ref row.
    _ASSERTE(TypeFromToken(tr) == mdtTypeRef);

    IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(tr), &pTypeRefRec));
    IfFailGo(pMiniMd->getNamespaceOfTypeRef(pTypeRefRec, wzNameSpace, ARRAY_SIZE(wzNameSpace), NULL));
    if (hr != NOERROR)
    {
        _ASSERTE(hr == CLDB_S_TRUNCATION);
        // Truncate the namespace string
        wzNameSpace[STRING_LENGTH(wzNameSpace)] = 0;
    }

    if (LOADEDMODULES::ResolveTypeRefWithLoadedModules(
        tr,
        this,
        pMiniMd,
        riid,
        ppIScope,
        ptd) == NOERROR)
    {
        // Done!! We found one match among the loaded modules.
        goto ErrExit;
    }

    IfFailGo(META_E_CANNOTRESOLVETYPEREF);

ErrExit:
    return hr;
#else // FEATURE_METADATA_IN_VM
    return E_NOTIMPL;
#endif // FEATURE_METADATA_IN_VM
} // RegMeta::ResolveTypeRef



// Full version handles metadata caching, which Release() needs to coordinate with.
// Thus Release() is in a satellite lib.
ULONG RegMeta::Release()
{
#if defined(FEATURE_METADATA_IN_VM)
    _ASSERTE(!m_bCached || LOADEDMODULES::IsEntryInList(this));
#else
    _ASSERTE(!m_bCached);
#endif // FEATURE_METADATA_IN_VM
    BOOL  bCached = m_bCached;
    ULONG cRef = InterlockedDecrement(&m_cRef);
    // NOTE: 'this' may be unsafe after this point, if the module is cached, and
    //  another thread finds the module in the cache, releases it, and deletes it
    //  before we get around to deleting it. (That's why we must make a local copy
    //  of m_bCached.)
    // If no references left...
    if (cRef == 0)
    {
        if (!bCached)
        {   // If the module is not (was not) cached, no other thread can have
            //  discovered the module, so this thread can now safely delete it.
            delete this;
        }
#if defined(FEATURE_METADATA_IN_VM)
        else if (LOADEDMODULES::RemoveModuleFromLoadedList(this))
        {   // If the module was cached, RemoveModuleFromLoadedList() will try to
            //  safely un-publish the module, and if it succeeds, no other thread
            //  has (or will) discover the module, so this thread can delete it.
            m_bCached = false;
            delete this;
        }
#endif // FEATURE_METADATA_IN_VM
    }

    return cRef;
} // RegMeta::Release
