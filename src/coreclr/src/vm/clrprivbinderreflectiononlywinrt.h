// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 
// Contains the types that implement code:ICLRPrivBinder and code:ICLRPrivAssembly for WinRT ReflectionOnly (aka introspection) binding.
// 
//=====================================================================================================================

#ifdef FEATURE_HOSTED_BINDER
#ifdef FEATURE_REFLECTION_ONLY_LOAD

#pragma once

#include "holder.h"
#include "internalunknownimpl.h"
#include "clrprivbinding.h"
#include "clrprivruntimebinders.h"
#include "clrprivbinderutil.h"
#include "clr_std/utility"

//=====================================================================================================================
// Forward declarations
class CLRPrivBinderReflectionOnlyWinRT;
class CLRPrivAssemblyReflectionOnlyWinRT;
class CLRPrivTypeCacheReflectionOnlyWinRT;
class DomainAssembly;

//=====================================================================================================================
//=====================================================================================================================
//=====================================================================================================================
class CLRPrivBinderReflectionOnlyWinRT : 
    public IUnknownCommon<ICLRPrivBinder>
{
    friend class CLRPrivAssemblyReflectionOnlyWinRT;
    
private:
    //=============================================================================================
    // Data structures for Namespace -> FileNameList hash (as returned by RoResolveNamespace API)
    
    // Entry in SHash table that maps namespace to list of files
    struct NamespaceToFileNameListMapEntry
    {
        PWSTR                            m_wszNamespace;
        CLRPrivBinderUtil::WStringList * m_pFileNameList;
    };
    
    // SHash traits for Namespace -> FileNameList hash
    class NamespaceToFileNameListMapTraits : public NoRemoveSHashTraits< DefaultSHashTraits< NamespaceToFileNameListMapEntry > >
    {
    public:
        typedef PCWSTR key_t;
        static const NamespaceToFileNameListMapEntry Null() { NamespaceToFileNameListMapEntry e; e.m_wszNamespace = nullptr; return e; }
        static bool IsNull(const NamespaceToFileNameListMapEntry & e) { return e.m_wszNamespace == nullptr; }
        static PCWSTR GetKey(const NamespaceToFileNameListMapEntry & e) { return e.m_wszNamespace; }
        static count_t Hash(PCWSTR str) { return HashString(str); }
        static BOOL Equals(PCWSTR lhs, PCWSTR rhs) { LIMITED_METHOD_CONTRACT; return (wcscmp(lhs, rhs) == 0); }
        
        void OnDestructPerEntryCleanupAction(const NamespaceToFileNameListMapEntry & e)
        {
            delete [] e.m_wszNamespace;
            CLRPrivBinderUtil::WStringList_Delete(e.m_pFileNameList);
        }
        static const bool s_DestructPerEntryCleanupAction = true;
    };

    typedef SHash<NamespaceToFileNameListMapTraits> NamespaceToFileNameListMap;
    
    //=============================================================================================
    // Data structure for FileName -> CLRPrivAssemblyReflectionOnlyWinRT * map
    
    struct FileNameToAssemblyMapEntry
    {
        PCWSTR                               m_wszFileName;   // File name (owned by m_pAssembly)
        CLRPrivAssemblyReflectionOnlyWinRT * m_pAssembly;
    };
    
    class FileNameToAssemblyMapTraits : public DefaultSHashTraits<FileNameToAssemblyMapEntry>
    {
    public:
        typedef PCWSTR key_t;
        static const FileNameToAssemblyMapEntry Null() { FileNameToAssemblyMapEntry e; e.m_wszFileName = NULL; return e; }
        static bool IsNull(const FileNameToAssemblyMapEntry &e) { return e.m_wszFileName == NULL; }
        static const FileNameToAssemblyMapEntry Deleted() { FileNameToAssemblyMapEntry e; e.m_wszFileName = (PCWSTR)-1; return e; }
        static bool IsDeleted(const FileNameToAssemblyMapEntry & e) { return e.m_wszFileName == (PCWSTR)-1; }
        static PCWSTR GetKey(const FileNameToAssemblyMapEntry & e) { return e.m_wszFileName; }
        static count_t Hash(PCWSTR str) { return HashString(str); }
        static BOOL Equals(PCWSTR lhs, PCWSTR rhs) { LIMITED_METHOD_CONTRACT; return (wcscmp(lhs, rhs) == 0); }
    };
    
    typedef SHash<FileNameToAssemblyMapTraits> FileNameToAssemblyMap;
    
public:
    //=============================================================================================
    // ICLRPrivBinder interface methods

    STDMETHOD(BindAssemblyByName)(
        IAssemblyName * pAssemblyName,
        ICLRPrivAssembly ** ppAssembly);

    // Implements interface method code:ICLRPrivBinder::VerifyBind.
    STDMETHOD(VerifyBind)(
        IAssemblyName *        pAssemblyName, 
        ICLRPrivAssembly *     pAssembly, 
        ICLRPrivAssemblyInfo * pAssemblyInfo);

    // Implements interface method code:ICLRPrivBinder::GetBinderFlags
    STDMETHOD(GetBinderFlags)(
        DWORD *pBinderFlags)
    {
        LIMITED_METHOD_CONTRACT;
        *pBinderFlags = BINDER_NONE;
        return S_OK;
    }

    // Implements interface method code:ICLRPrivBinder::GetBinderID.
    STDMETHOD(GetBinderID)(
        UINT_PTR * pBinderId);

    // FindAssemblyBySpec is not supported by this binder.
    STDMETHOD(FindAssemblyBySpec)(
        LPVOID pvAppDomain,
        LPVOID pvAssemblySpec,
        HRESULT * pResult,
        ICLRPrivAssembly ** ppAssembly)
    { STATIC_CONTRACT_WRAPPER; return E_FAIL; }

    //=============================================================================================
    // Class methods

    CLRPrivBinderReflectionOnlyWinRT(
        CLRPrivTypeCacheReflectionOnlyWinRT * pTypeCache);
    
    ~CLRPrivBinderReflectionOnlyWinRT();

    HRESULT BindAssemblyExplicit(
        const WCHAR *       wszFileName, 
        ICLRPrivAssembly ** ppAssembly);

    HRESULT BindWinRtType(
        LPCSTR              szTypeNamespace, 
        LPCSTR              szTypeClassName, 
        DomainAssembly *    pParentAssembly, 
        ICLRPrivAssembly ** ppPrivAssembly);
    
private:
    //=============================================================================================
    // Accessors for FileName -> CLRPrivAssemblyReflectionOnlyWinRT * map
    
    ReleaseHolder<CLRPrivAssemblyReflectionOnlyWinRT> FindAssemblyByFileName(
        LPCWSTR wzsFileName);
    
    ReleaseHolder<CLRPrivAssemblyReflectionOnlyWinRT> AddFileNameToAssemblyMapping(
        LPCWSTR                              wszFileName,
        CLRPrivAssemblyReflectionOnlyWinRT * pAssembly);
    
    void RemoveFileNameToAssemblyMapping(
        LPCWSTR wszFileName);
    
    ReleaseHolder<CLRPrivAssemblyReflectionOnlyWinRT> FindOrCreateAssemblyByFileName(
        LPCWSTR wzsFileName);
    
    //=============================================================================================
    // Internal methods
    
    // Returns list of file names from code:m_NamespaceToFileNameListMap for the namespace.
    void GetFileNameListForNamespace(
        LPCWSTR                           wszNamespace, 
        DomainAssembly *                  pParentAssembly, 
        CLRPrivBinderUtil::WStringList ** ppFileNameList);
    
    // Adds (thread-safe) list of file names to code:m_NamespaceToFileNameListMap for the namespace - returns the cached value.
    BOOL AddFileNameListForNamespace(
        LPCWSTR                           wszNamespace, 
        CLRPrivBinderUtil::WStringList *  pFileNameList, 
        CLRPrivBinderUtil::WStringList ** ppFileNameList);
    
    HRESULT BindWinRtType_Internal(
        LPCSTR                                szTypeNamespace, 
        LPCSTR                                szTypeClassName, 
        DomainAssembly *                      pParentAssembly, 
        CLRPrivAssemblyReflectionOnlyWinRT ** ppAssembly);
    
private:
    //=============================================================================================
    
    // Namespace -> FileName list map ... items are never removed
    NamespaceToFileNameListMap m_NamespaceToFileNameListMap;
    // FileName -> CLRPrivAssemblyReflectionOnlyWinRT * map ... items can be removed when CLRPrivAssemblyReflectionOnlyWinRT dies
    FileNameToAssemblyMap      m_FileNameToAssemblyMap;
    
    // Lock for the above maps
    Crst m_MapsLock;
    
    //=============================================================================================
    CLRPrivTypeCacheReflectionOnlyWinRT * m_pTypeCache;
    
};  // class CLRPrivBinderReflectionOnlyWinRT


//=====================================================================================================================
//=====================================================================================================================
//=====================================================================================================================
class CLRPrivAssemblyReflectionOnlyWinRT :
    public IUnknownCommon<ICLRPrivAssembly>
{
    friend class CLRPrivBinderReflectionOnlyWinRT;

public:
    //=============================================================================================
    // Class methods
    
    CLRPrivAssemblyReflectionOnlyWinRT(
        LPCWSTR                                      wzFullTypeName, 
        CLRPrivBinderReflectionOnlyWinRT *           pBinder, 
        CLRPrivBinderUtil::CLRPrivResourcePathImpl * pResourceIL);
    
    //=============================================================================================
    // IUnknown interface methods
    
    // Overridden to implement self-removal from assembly map code:CLRPrivBinderReflectionOnlyWinRT::m_FileNameToAssemblyMap.
    STDMETHOD_(ULONG, Release)();
    
    //=============================================================================================
    // ICLRPrivBinder interface methods
    
    STDMETHOD(BindAssemblyByName)(
        IAssemblyName * pAssemblyName,
        ICLRPrivAssembly ** ppAssembly)
    {
        STATIC_CONTRACT_WRAPPER;
        return m_pBinder->BindAssemblyByName(pAssemblyName, ppAssembly);
    }
    
    // Implements interface method code:ICLRPrivBinder::VerifyBind.
    STDMETHOD(VerifyBind)(
        IAssemblyName *        pAssemblyName, 
        ICLRPrivAssembly *     pAssembly, 
        ICLRPrivAssemblyInfo * pAssemblyInfo)
    {
        STATIC_CONTRACT_WRAPPER;
        return m_pBinder->VerifyBind(pAssemblyName, pAssembly, pAssemblyInfo);
    }

    //---------------------------------------------------------------------------------------------
    // Implements interface method code:ICLRPrivBinder::GetBinderFlags
    STDMETHOD(GetBinderFlags)(
        DWORD *pBinderFlags)
    {
        STATIC_CONTRACT_WRAPPER;
        return m_pBinder->GetBinderFlags(pBinderFlags);
    }

    // Implements interface method code:ICLRPrivBinder::GetBinderID.
    STDMETHOD(GetBinderID)(
        UINT_PTR * pBinderId)
    {
        STATIC_CONTRACT_WRAPPER;
        return m_pBinder->GetBinderID(pBinderId);
    }

    //=============================================================================================
    // ICLRPrivAssembly interface methods
    
    // Implements interface method code:ICLRPrivAssembly::IsShareable.
    STDMETHOD(IsShareable)(
        BOOL * pbIsShareable);
    
    // Implements interface method code:ICLRPrivAssembly::GetAvailableImageTypes.
    STDMETHOD(GetAvailableImageTypes)(
        LPDWORD pdwImageTypes);
    
    // Implements interface method code:ICLRPrivAssembly::GetImageResource.
    STDMETHOD(GetImageResource)(
        DWORD               dwImageType, 
        DWORD *             pdwImageType, 
        ICLRPrivResource ** ppIResource);
    
    // Implements code:ICLRPrivBinder::FindAssemblyBySpec
    STDMETHOD(FindAssemblyBySpec)(
        LPVOID pvAppDomain,
        LPVOID pvAssemblySpec,
        HRESULT * pResult,
        ICLRPrivAssembly ** ppAssembly)
    { STATIC_CONTRACT_WRAPPER; return m_pBinder->FindAssemblyBySpec(pvAppDomain, pvAssemblySpec, pResult, ppAssembly); }

private:
    //=============================================================================================
    
    ReleaseHolder<CLRPrivBinderReflectionOnlyWinRT> m_pBinder;
    ReleaseHolder<CLRPrivBinderUtil::CLRPrivResourcePathImpl> m_pResourceIL;
    
};  // class CLRPrivAssemblyReflectionOnlyWinRT

#endif //FEATURE_REFLECTION_ONLY_LOAD
#endif //FEATURE_HOSTED_BINDER
