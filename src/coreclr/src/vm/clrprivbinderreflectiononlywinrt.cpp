// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 
// Contains the types that implement code:ICLRPrivBinder and code:ICLRPrivAssembly for WinRT ReflectionOnly (aka introspection) binding.
// 
//=====================================================================================================================

#include "common.h" // precompiled header

#ifndef DACCESS_COMPILE
#ifdef FEATURE_REFLECTION_ONLY_LOAD

//=====================================================================================================================
#include "sstring.h"
#include "policy.h"
#include "clrprivbinderreflectiononlywinrt.h"
#include "appxutil.h"
#include "clrprivbinderutil.h"
#include "imprthelpers.h" // in fusion/inc

#include <winstring.h>
#include <typeresolution.h>

using namespace CLRPrivBinderUtil;

//=====================================================================================================================

//=====================================================================================================================
CLRPrivBinderReflectionOnlyWinRT::CLRPrivBinderReflectionOnlyWinRT(
    CLRPrivTypeCacheReflectionOnlyWinRT * pTypeCache)
    : m_MapsLock(CrstLeafLock, CRST_REENTRANCY) // Reentracy is needed for code:CLRPrivAssemblyReflectionOnlyWinRT::Release
{
    STANDARD_VM_CONTRACT;
    
    // This binder is not supported in AppX scenario.
    _ASSERTE(!AppX::IsAppXProcess());
    
    _ASSERTE(pTypeCache != nullptr);
    m_pTypeCache = clr::SafeAddRef(pTypeCache);
}

//=====================================================================================================================
CLRPrivBinderReflectionOnlyWinRT::~CLRPrivBinderReflectionOnlyWinRT()
{
    WRAPPER_NO_CONTRACT;
    
    if (m_pTypeCache != nullptr)
    {
        m_pTypeCache->Release();
    }
}

//=====================================================================================================================
HRESULT 
CLRPrivBinderReflectionOnlyWinRT::BindWinRtType_Internal(
    LPCSTR                                szTypeNamespace, 
    LPCSTR                                szTypeClassName, 
    DomainAssembly *                      pParentAssembly, 
    CLRPrivAssemblyReflectionOnlyWinRT ** ppAssembly)
{
    STANDARD_VM_CONTRACT;
    
    HRESULT hr = S_OK;
    
    VALIDATE_ARG_RET(ppAssembly != nullptr);
    
    CLRPrivBinderUtil::WStringList * pFileNameList = nullptr;
    
    StackSString ssTypeNamespace(SString::Utf8, szTypeNamespace);
    
    GetFileNameListForNamespace(ssTypeNamespace.GetUnicode(), pParentAssembly, &pFileNameList);
    
    if (pFileNameList == nullptr)
    {   // There are no files associated with the namespace
        return CLR_E_BIND_TYPE_NOT_FOUND;
    }
    
    StackSString ssTypeName(ssTypeNamespace);
    ssTypeName.Append(W('.'));
    ssTypeName.AppendUTF8(szTypeClassName);
    
    CLRPrivBinderUtil::WStringListElem * pFileNameElem = pFileNameList->GetHead();
    while (pFileNameElem != nullptr)
    {
        const WCHAR * wszFileName = pFileNameElem->GetValue();
        ReleaseHolder<CLRPrivAssemblyReflectionOnlyWinRT> pAssembly = FindOrCreateAssemblyByFileName(wszFileName);
        _ASSERTE(pAssembly != NULL);
        
        IfFailRet(hr = m_pTypeCache->ContainsType(pAssembly, ssTypeName.GetUnicode()));
        if (hr == S_OK)
        {   // The type we are looking for has been found in this assembly
            *ppAssembly = pAssembly.Extract();
            return S_OK;
        }
        _ASSERTE(hr == S_FALSE);
        
        // Try next file name for this namespace
        pFileNameElem = CLRPrivBinderUtil::WStringList::GetNext(pFileNameElem);
    }
    
    // The type has not been found in any of the files from the type's namespace
    return CLR_E_BIND_TYPE_NOT_FOUND;
} // CLRPrivBinderReflectionOnlyWinRT::BindWinRtType_Internal

//=====================================================================================================================
HRESULT 
CLRPrivBinderReflectionOnlyWinRT::BindWinRtType(
    LPCSTR              szTypeNamespace, 
    LPCSTR              szTypeClassName, 
    DomainAssembly *    pParentAssembly, 
    ICLRPrivAssembly ** ppPrivAssembly)
{
    STANDARD_VM_CONTRACT;
    
    HRESULT hr = S_OK;
    
    ReleaseHolder<CLRPrivAssemblyReflectionOnlyWinRT> pWinRTAssembly;
    IfFailRet(BindWinRtType_Internal(szTypeNamespace, szTypeClassName, pParentAssembly, &pWinRTAssembly));
    IfFailRet(pWinRTAssembly->QueryInterface(__uuidof(ICLRPrivAssembly), (LPVOID *)ppPrivAssembly));
    
    return hr;
}

//=====================================================================================================================
// Implements interface method code:ICLRPrivBinder::BindAssemblyByName.
// 
HRESULT CLRPrivBinderReflectionOnlyWinRT::BindAssemblyByName(
    IAssemblyName     * pAssemblyName, 
    ICLRPrivAssembly ** ppAssembly)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE_MSG(false, "Unexpected call to CLRPrivBinderReflectionOnlyWinRT::BindAssemblyByName");
    return E_UNEXPECTED;
}

//=====================================================================================================================
ReleaseHolder<CLRPrivAssemblyReflectionOnlyWinRT> 
CLRPrivBinderReflectionOnlyWinRT::FindAssemblyByFileName(
    LPCWSTR wszFileName)
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_CAN_TAKE_LOCK;
    
    CrstHolder lock(&m_MapsLock);
    const FileNameToAssemblyMapEntry * pEntry = m_FileNameToAssemblyMap.LookupPtr(wszFileName);
    return (pEntry == nullptr) ? nullptr : clr::SafeAddRef(pEntry->m_pAssembly);
}

//=====================================================================================================================
// Add FileName -> CLRPrivAssemblyReflectionOnlyWinRT * mapping to the map (multi-thread safe).
ReleaseHolder<CLRPrivAssemblyReflectionOnlyWinRT> 
CLRPrivBinderReflectionOnlyWinRT::AddFileNameToAssemblyMapping(
    LPCWSTR                              wszFileName, 
    CLRPrivAssemblyReflectionOnlyWinRT * pAssembly)
{
    STANDARD_VM_CONTRACT;
    
    _ASSERTE(pAssembly != nullptr);
    
    CrstHolder lock(&m_MapsLock);
    
    const FileNameToAssemblyMapEntry * pEntry = m_FileNameToAssemblyMap.LookupPtr(wszFileName);
    CLRPrivAssemblyReflectionOnlyWinRT * pResultAssembly = nullptr;
    if (pEntry != nullptr)
    {
        pResultAssembly = pEntry->m_pAssembly;
    }
    else
    {
        FileNameToAssemblyMapEntry e;
        e.m_wszFileName = wszFileName;
        e.m_pAssembly = pAssembly;
        m_FileNameToAssemblyMap.Add(e);
        
        pResultAssembly = pAssembly;
    }
    return clr::SafeAddRef(pResultAssembly);
}

//=====================================================================================================================
void 
CLRPrivBinderReflectionOnlyWinRT::RemoveFileNameToAssemblyMapping(
    LPCWSTR wszFileName)
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_CAN_TAKE_LOCK;
    
    CrstHolder lock(&m_MapsLock);
    m_FileNameToAssemblyMap.Remove(wszFileName);
}

//=====================================================================================================================
ReleaseHolder<CLRPrivAssemblyReflectionOnlyWinRT> 
CLRPrivBinderReflectionOnlyWinRT::FindOrCreateAssemblyByFileName(
    LPCWSTR wszFileName)
{
    STANDARD_VM_CONTRACT;
    
    ReleaseHolder<CLRPrivAssemblyReflectionOnlyWinRT> pAssembly = FindAssemblyByFileName(wszFileName);
    
    if (pAssembly == nullptr)
    {
        NewHolder<CLRPrivResourcePathImpl> pResource(
            new CLRPrivResourcePathImpl(wszFileName));
        
        NewHolder<CLRPrivAssemblyReflectionOnlyWinRT> pNewAssembly(
            new CLRPrivAssemblyReflectionOnlyWinRT(wszFileName, this, pResource));
        
        // pNewAssembly holds reference to this now
        pResource.SuppressRelease();
        
        // Add the assembly into cache (multi-thread aware)
        pAssembly = AddFileNameToAssemblyMapping(pResource->GetPath(), pNewAssembly);
        
        if (pAssembly == pNewAssembly)
        {   // We did not find an existing assembly in the cache and are using the newly created pNewAssembly.
            // Stop it from being deleted when we go out of scope.
            pNewAssembly.SuppressRelease();
        }
    }
    return pAssembly.Extract();
}

//=====================================================================================================================
// Returns list of file names from code:m_NamespaceToFileNameListMap for the namespace.
// 
void 
CLRPrivBinderReflectionOnlyWinRT::GetFileNameListForNamespace(
    LPCWSTR                           wszNamespace, 
    DomainAssembly *                  pParentAssembly, 
    CLRPrivBinderUtil::WStringList ** ppFileNameList)
{
    STANDARD_VM_CONTRACT;
    
    CLRPrivBinderUtil::WStringList * pFileNameList = nullptr;
    {
        CrstHolder lock(&m_MapsLock);
        
        const NamespaceToFileNameListMapEntry * pEntry = m_NamespaceToFileNameListMap.LookupPtr(wszNamespace);
        if (pEntry != nullptr)
        {
            // Entries from the map are never removed, so we do not have to protect the file name list with a lock
            pFileNameList = pEntry->m_pFileNameList;
        }
    }
    
    if (pFileNameList != nullptr)
    {
        *ppFileNameList = pFileNameList;
    }
    else
    {
        CLRPrivBinderUtil::WStringListHolder hFileNameList;
        
        EX_TRY
        {
            m_pTypeCache->RaiseNamespaceResolveEvent(wszNamespace, pParentAssembly, &hFileNameList);
        }
        EX_CATCH
        {
            Exception * ex = GET_EXCEPTION();
            if (!ex->IsTransient())
            {   // Exception was caused by user code
                // Cache empty file name list for this namespace
                (void)AddFileNameListForNamespace(wszNamespace, nullptr, ppFileNameList);
            }
            EX_RETHROW;
        }
        EX_END_CATCH_UNREACHABLE
        
        if (AddFileNameListForNamespace(wszNamespace, hFileNameList.GetValue(), ppFileNameList))
        {   // The file name list was added to the cache - do not delete it
            _ASSERTE(*ppFileNameList == hFileNameList.GetValue());
            (void)hFileNameList.Extract();
        }
    }
} // CLRPrivBinderReflectionOnlyWinRT::GetFileNameListForNamespace

//=====================================================================================================================
// Adds (thread-safe) list of file names to code:m_NamespaceToFileNameListMap for the namespace - returns the cached value.
// Returns TRUE, if pFileNameList was added to the cache and caller should NOT delete it.
// Returns FALSE, if pFileNameList was not added to the cache and caller should delete it.
// 
BOOL 
CLRPrivBinderReflectionOnlyWinRT::AddFileNameListForNamespace(
    LPCWSTR                           wszNamespace, 
    CLRPrivBinderUtil::WStringList *  pFileNameList, 
    CLRPrivBinderUtil::WStringList ** ppFileNameList)
{
    STANDARD_VM_CONTRACT;
    
    NewArrayHolder<WCHAR> wszEntryNamespace = DuplicateStringThrowing(wszNamespace);
    
    NamespaceToFileNameListMapEntry entry;
    entry.m_wszNamespace = wszEntryNamespace;
    entry.m_pFileNameList = pFileNameList;
    
    {
        CrstHolder lock(&m_MapsLock);
        
        const NamespaceToFileNameListMapEntry * pEntry = m_NamespaceToFileNameListMap.LookupPtr(wszEntryNamespace);
        if (pEntry == nullptr)
        {
            m_NamespaceToFileNameListMap.Add(entry);
            
            // These values are now owned by the hash table element
            wszEntryNamespace.SuppressRelease();
            *ppFileNameList = pFileNameList;
            return TRUE;
        }
        else
        {   // Another thread beat us adding this entry to the hash table
            *ppFileNameList = pEntry->m_pFileNameList;
            return FALSE;
        }
    }
} // CLRPrivBinderReflectionOnlyWinRT::AddFileNameListForNamespace

//=====================================================================================================================
HRESULT 
CLRPrivBinderReflectionOnlyWinRT::BindAssemblyExplicit(
    const WCHAR *       wszFileName, 
    ICLRPrivAssembly ** ppAssembly)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    HRESULT hr;
    
    GCX_PREEMP();
    
    ReleaseHolder<CLRPrivAssemblyReflectionOnlyWinRT> pAssembly = FindOrCreateAssemblyByFileName(wszFileName);
    _ASSERTE(pAssembly != NULL);
    
    IfFailRet(pAssembly->QueryInterface(__uuidof(ICLRPrivAssembly), (LPVOID *)ppAssembly));
    
    return S_OK;
}

//=====================================================================================================================
CLRPrivAssemblyReflectionOnlyWinRT::CLRPrivAssemblyReflectionOnlyWinRT(
    LPCWSTR                            wzSimpleName, 
    CLRPrivBinderReflectionOnlyWinRT * pBinder, 
    CLRPrivResourcePathImpl *          pResourceIL)
{
    STANDARD_VM_CONTRACT;
    VALIDATE_ARG_THROW((wzSimpleName != nullptr) && (pBinder != nullptr) && (pResourceIL != nullptr));

    m_pBinder = clr::SafeAddRef(pBinder);
    m_pResourceIL = clr::SafeAddRef(pResourceIL);
}

//=====================================================================================================================
ULONG CLRPrivAssemblyReflectionOnlyWinRT::Release()
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_CAN_TAKE_LOCK;
    _ASSERTE(m_cRef > 0);
    
    ULONG cRef;
    
    {
        // To achieve proper lifetime semantics, the name to assembly map elements' CLRPrivAssemblyReflectionOnlyWinRT 
        // instances are not ref counted. We cannot allow discovery of the object via m_FileNameToAssemblyMap 
        // when the ref count is 0 (to prevent another thread to AddRef and Release it back to 0 in parallel).
        // All uses of the map are guarded by the map lock, so we have to decrease the ref count under that 
        // lock (to avoid the chance that 2 threads are running Release to ref count 0 at once).
        CrstHolder lock(&m_pBinder->m_MapsLock);
        
        cRef = InterlockedDecrement(&m_cRef);
        if (cRef == 0)
        {
            m_pBinder->RemoveFileNameToAssemblyMapping(m_pResourceIL->GetPath());
        }
    }
    
    if (cRef == 0)
    {
        delete this;
    }
    return cRef;
}

//=====================================================================================================================
// Implements interface method code:ICLRPrivAssembly::IsShareable.
// 
HRESULT CLRPrivAssemblyReflectionOnlyWinRT::IsShareable(
    BOOL * pbIsShareable)
{
    LIMITED_METHOD_CONTRACT;

    VALIDATE_ARG_RET(pbIsShareable != nullptr);

    *pbIsShareable = FALSE;
    return S_OK;
}

//=====================================================================================================================
// Implements interface method code:ICLRPrivAssembly::GetAvailableImageTypes.
// 
HRESULT CLRPrivAssemblyReflectionOnlyWinRT::GetAvailableImageTypes(
    LPDWORD pdwImageTypes)
{
    LIMITED_METHOD_CONTRACT;

    VALIDATE_ARG_RET(pdwImageTypes != nullptr);

    *pdwImageTypes = 0;

    if (m_pResourceIL != nullptr)
        *pdwImageTypes |= ASSEMBLY_IMAGE_TYPE_IL;

    return S_OK;
}

//=====================================================================================================================
// Implements interface method code:ICLRPrivAssembly::GetImageResource.
// 
HRESULT CLRPrivAssemblyReflectionOnlyWinRT::GetImageResource(
    DWORD               dwImageType, 
    DWORD *             pdwImageType, 
    ICLRPrivResource ** ppIResource)
{
    STANDARD_BIND_CONTRACT;
    HRESULT hr = S_OK;
    
    VALIDATE_ARG_RET(ppIResource != nullptr);
    
    EX_TRY
    {
        DWORD _dwImageType;
        if (pdwImageType == nullptr)
        {
            pdwImageType = &_dwImageType;
        }
        
        if ((dwImageType & ASSEMBLY_IMAGE_TYPE_IL) == ASSEMBLY_IMAGE_TYPE_IL)
        {
            *ppIResource = clr::SafeAddRef(m_pResourceIL);
            *pdwImageType = ASSEMBLY_IMAGE_TYPE_IL;
        }
        else
        {   // Native image is not supported by this binder
            hr = CLR_E_BIND_IMAGE_UNAVAILABLE;
        }
    }
    EX_CATCH_HRESULT(hr);
    
    return hr;
}

//=====================================================================================================================
// Implements interface method code:ICLRPrivBinder::VerifyBind.
// 
HRESULT CLRPrivBinderReflectionOnlyWinRT::VerifyBind(
    IAssemblyName *        pAssemblyName, 
    ICLRPrivAssembly *     pAssembly, 
    ICLRPrivAssemblyInfo * pAssemblyInfo)
{
    STANDARD_BIND_CONTRACT;
    HRESULT hr = S_OK;
    
    UINT_PTR binderID;
    IfFailRet(pAssembly->GetBinderID(&binderID));
    if (binderID != reinterpret_cast<UINT_PTR>(this))
    {
        return pAssembly->VerifyBind(pAssemblyName, pAssembly, pAssemblyInfo);
    }
    
    // Since WinRT types are bound by type name and not assembly name, assembly-level version validation 
    // does not make sense here. Just return S_OK.
    return S_OK;
}

//=====================================================================================================================
// Implements interface method code:ICLRPrivBinder::GetBinderID.
// 
HRESULT CLRPrivBinderReflectionOnlyWinRT::GetBinderID(
    UINT_PTR * pBinderId)
{
    LIMITED_METHOD_CONTRACT;
    
    *pBinderId = reinterpret_cast<UINT_PTR>(this);
    return S_OK;
}

#endif //FEATURE_REFLECTION_ONLY_LOAD
#endif //!DACCESS_COMPILE
