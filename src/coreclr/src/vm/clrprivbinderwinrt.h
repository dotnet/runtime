// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 


// 
// Contains the types that implement code:ICLRPrivBinder and code:ICLRPrivAssembly for WinRT binding.
// 
//=============================================================================================

#pragma once

#include "holder.h"
#include "internalunknownimpl.h"
#include "clrprivbinding.h"
#include "clrprivruntimebinders.h"
#include "clrprivbinderutil.h"
#include "clrprivtypecachewinrt.h"
#include "clr_std/utility"
#include "winrt/windowsstring.h"
#include "appxutil.h"

#include "coreclr/corebindresult.h"

// IBindResult maps directly to its one and only implementation on CoreCLR.
typedef CoreBindResult IBindResult;

//=====================================================================================================================
// Forward declarations
class CLRPrivBinderWinRT;
class CLRPrivAssemblyWinRT;
class BINDER_SPACE::ApplicationContext;
class BINDER_SPACE::Assembly;

typedef DPTR(CLRPrivBinderWinRT)     PTR_CLRPrivBinderWinRT;
typedef DPTR(CLRPrivAssemblyWinRT)   PTR_CLRPrivAssemblyWinRT;

BOOL 
IsWindowsNamespace(const char * wszNamespace);

//=====================================================================================================================
//=====================================================================================================================
//=====================================================================================================================
class CLRPrivBinderWinRT : 
    public IUnknownCommon<ICLRPrivBinder
    >
{
    friend class CLRPrivAssemblyWinRT;
    
public:
    //=============================================================================================
    // Options of namespace resolution
    enum NamespaceResolutionKind
    {
        NamespaceResolutionKind_WindowsAPI,             // Using RoResolveNamespace Win8 API
        NamespaceResolutionKind_DesignerResolveEvent    // Using DesignerNamespaceResolve event
    };
    
private:
    //=============================================================================================
    // Data structures for Namespace -> FileNameList map (as returned by RoResolveNamespace API)
    
    // Entry in SHash table that maps namespace to list of files
    struct NamespaceToFileNameListMapEntry
    {
        PTR_WSTR                           m_wszNamespace;
        CLRPrivBinderUtil::PTR_WStringList m_pFileNameList;
    };
    
    // SHash traits for Namespace -> FileNameList hash
    class NamespaceToFileNameListMapTraits : public NoRemoveSHashTraits< DefaultSHashTraits< NamespaceToFileNameListMapEntry > >
    {
    public:
        typedef PCWSTR key_t;
        static const NamespaceToFileNameListMapEntry Null() { NamespaceToFileNameListMapEntry e; e.m_wszNamespace = PTR_WSTR(nullptr); return e; }
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
    // Data structure for FileName -> CLRPrivAssemblyWinRT * map
    
    struct FileNameToAssemblyWinRTMapEntry
    {
        PTR_CWSTR                m_wszFileName;   // File name (owned by m_pAssembly)
        PTR_CLRPrivAssemblyWinRT m_pAssembly;
    };
    
    class FileNameToAssemblyWinRTMapTraits : public DefaultSHashTraits<FileNameToAssemblyWinRTMapEntry>
    {
    public:
        typedef PCWSTR key_t;
        static const FileNameToAssemblyWinRTMapEntry Null() { FileNameToAssemblyWinRTMapEntry e; e.m_wszFileName = PTR_CWSTR(nullptr); return e; }
        static bool IsNull(const FileNameToAssemblyWinRTMapEntry &e) { return e.m_wszFileName == PTR_CWSTR(nullptr); }
        static const FileNameToAssemblyWinRTMapEntry Deleted() { FileNameToAssemblyWinRTMapEntry e; e.m_wszFileName = (PTR_CWSTR)-1; return e; }
        static bool IsDeleted(const FileNameToAssemblyWinRTMapEntry & e) { return dac_cast<TADDR>(e.m_wszFileName) == (TADDR)-1; }
        static PCWSTR GetKey(const FileNameToAssemblyWinRTMapEntry & e) { return e.m_wszFileName; }
        static count_t Hash(PCWSTR str) { return HashString(str); }
        static BOOL Equals(PCWSTR lhs, PCWSTR rhs) { LIMITED_METHOD_CONTRACT; return (wcscmp(lhs, rhs) == 0); }
    };
    
    typedef SHash<FileNameToAssemblyWinRTMapTraits> FileNameToAssemblyWinRTMap;
    
public:
    //=============================================================================================
    // ICLRPrivBinder interface methods

    // Implements interface method code:ICLRPrivBinder::BindAssemblyByName.
    STDMETHOD(BindAssemblyByName)(
        IAssemblyName * pAssemblyName,
        ICLRPrivAssembly ** ppAssembly);

    // Implements interface method code:ICLRPrivBinder::GetBinderID.
    STDMETHOD(GetBinderID)(
        UINT_PTR * pBinderId);

    STDMETHOD(GetLoaderAllocator)(
        LPVOID * pLoaderAllocator)
    {
        return E_FAIL;
    }


    //=============================================================================================
    // Class methods

    CLRPrivBinderWinRT(
        ICLRPrivBinder *        pParentBinder, 
        CLRPrivTypeCacheWinRT * pWinRtTypeCache,
        LPCWSTR *               rgwzAltPath, 
        UINT                    cAltPaths, 
        NamespaceResolutionKind fNamespaceResolutionKind,
        BOOL                    fCanUseNativeImages);
    
    static 
    CLRPrivBinderWinRT * GetOrCreateBinder(
        CLRPrivTypeCacheWinRT * pWinRtTypeCache, 
        NamespaceResolutionKind fNamespaceResolutionKind);
    
    ~CLRPrivBinderWinRT();

    // Binds WinRT assemblies only.
    HRESULT BindWinRTAssemblyByName(
        IAssemblyName * pIAssemblyName,
        CLRPrivAssemblyWinRT ** ppAssembly);

    // Binds WinRT assemblies only.
    HRESULT BindWinRTAssemblyByName(
        IAssemblyName * pIAssemblyName,
        ICLRPrivAssembly ** ppPrivAssembly);

    // Binds WinRT assemblies only.
    HRESULT BindWinRTAssemblyByName(
        IAssemblyName * pIAssemblyName,
        IBindResult ** ppIBindResult);

    HRESULT GetAssemblyAndTryFindNativeImage(SString &sWinmdFilename, LPCWSTR pwzSimpleName, BINDER_SPACE::Assembly ** ppAssembly);
    // On Phone the application's APP_PATH CoreCLR hosting config property is used as the app
    // package graph for RoResolveNamespace to find 3rd party WinMDs.  This method wires up
    // the app paths so the WinRT binder will find 3rd party WinMDs.
    HRESULT SetApplicationContext(BINDER_SPACE::ApplicationContext *pApplicationContext, LPCWSTR pwzAppLocalWinMD);
    // Finds assembly with WinRT type if it is already loaded
    // Note: This method could implement interface code:ICLRPrivWinRtTypeBinder if it is ever needed
    PTR_Assembly FindAssemblyForTypeIfLoaded(
        PTR_AppDomain pAppDomain, 
        LPCUTF8       szNamespace, 
        LPCUTF8       szClassName);


private:
    //=============================================================================================
    // Accessors for FileName -> CLRPrivAssemblyWinRT * map
    
    ReleaseHolder<CLRPrivAssemblyWinRT> FindAssemblyByFileName(
        PCWSTR wzsFileName);
    
    ReleaseHolder<CLRPrivAssemblyWinRT> AddFileNameToAssemblyMapping(
        PCWSTR                 wszFileName,
        CLRPrivAssemblyWinRT * pAssembly);
    
    void RemoveFileNameToAssemblyMapping(
        PCWSTR wszFileName);
    
    //=============================================================================================
    // Internal methods
    
    // Returns list of file names from code:m_NamespaceToFileNameListMap for the namespace
    HRESULT GetFileNameListForNamespace(LPCWSTR wszNamespace, CLRPrivBinderUtil::WStringList ** ppFileNameList);
    
    // Adds (thread-safe) list of file names to code:m_NamespaceToFileNameListMap for the namespace.
    // Returns TRUE if the list was added to the cache.
    BOOL AddFileNameListForNamespace(
        LPCWSTR                           wszNamespace, 
        CLRPrivBinderUtil::WStringList *  pFileNameList, 
        CLRPrivBinderUtil::WStringList ** ppFileNameList);
    

private:
    //=============================================================================================
    
    // Namespace -> FileName list map ... items are never removed
    NamespaceToFileNameListMap m_NamespaceToFileNameListMap;
    // FileName -> CLRPrivAssemblyWinRT * map ... items can be removed when CLRPrivAssemblyWinRT dies
    FileNameToAssemblyWinRTMap m_FileNameToAssemblyMap;

    // Lock for the above maps
    CrstExplicitInit m_MapsLock;
    // Lock for adding into the above maps, in addition to the read-lock above
    CrstExplicitInit m_MapsAddLock;

    //=============================================================================================
    
    PTR_CLRPrivTypeCacheWinRT m_pTypeCache;
    
    // The kind of namespace resolution (RoResolveNamespace Win8 API or DesignerNamespaceResolve event)
    NamespaceResolutionKind m_fNamespaceResolutionKind;
    
    static CLRPrivBinderWinRT * s_pSingleton;
    
    // Parent binder used to delegate bind requests up the binder hierarchy.
    ICLRPrivBinder * m_pParentBinder;
    
#ifndef CROSSGEN_COMPILE
    // Alternative paths for use with RoGetNamespace api
    CLRPrivBinderUtil::HSTRINGArrayHolder m_rgAltPaths;
#endif



    BINDER_SPACE::ApplicationContext * m_pApplicationContext;
    NewArrayHolder<WCHAR> m_appLocalWinMDPath;


};  // class CLRPrivBinderWinRT


//=====================================================================================================================
//=====================================================================================================================
//=====================================================================================================================
class CLRPrivAssemblyWinRT :
    public IUnknownCommon<ICLRPrivAssembly, ICLRPrivAssemblyID_WinRT>
{
    friend class CLRPrivBinderWinRT;
    
public:
    //=============================================================================================
    // Class methods
    
    CLRPrivAssemblyWinRT(
        CLRPrivBinderWinRT *                         pBinder, 
        CLRPrivBinderUtil::CLRPrivResourcePathImpl * pResourceIL,
        IBindResult *                                pIBindResult,
        BOOL                                         fShareable);
    
    ~CLRPrivAssemblyWinRT();

    HRESULT GetIBindResult(
        IBindResult ** ppIBindResult);
    
    static HRESULT GetIBindResult(
        ICLRPrivAssembly * pPrivAssembly, 
        IBindResult **     ppIBindResult);
    
    //=============================================================================================
    // IUnknown interface methods
    
    // Implements interface method code:IUnknown::Release.
    // Overridden to implement self-removal from assembly map code:CLRPrivBinderWinRT::m_FileNameToAssemblyMap.
    STDMETHOD_(ULONG, Release)();
    
    //=============================================================================================
    // ICLRPrivBinder interface methods
    
    // Implements interface method code:ICLRPrivBinder::BindAssemblyByName.
    STDMETHOD(BindAssemblyByName)(
        IAssemblyName * pAssemblyName,
        ICLRPrivAssembly ** ppAssembly)
    {
        STATIC_CONTRACT_WRAPPER;
        return m_pBinder->BindAssemblyByName(pAssemblyName, ppAssembly);
    }
    
    // Implements interface method code:ICLRPrivBinder::GetBinderID.
    STDMETHOD(GetBinderID)(
        UINT_PTR * pBinderId)
    {
        STATIC_CONTRACT_WRAPPER;
        return m_pBinder->GetBinderID(pBinderId);
    }
    
    STDMETHOD(GetLoaderAllocator)(
        LPVOID * pLoaderAllocator)
    {
        WRAPPER_NO_CONTRACT;
        return m_pBinder->GetLoaderAllocator(pLoaderAllocator);
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
    
private:
    //=============================================================================================
    
    HRESULT EnsureAvailableImageTypes();

    ReleaseHolder<CLRPrivBinderWinRT> m_pBinder;
    ReleaseHolder<CLRPrivBinderUtil::CLRPrivResourcePathImpl> m_pResourceIL;
    // This cannot be a holder as there can be a race to assign to it.
    ICLRPrivResource * m_pIResourceNI;
    ReleaseHolder<IBindResult> m_pIBindResult;
    BOOL m_fShareable;
    Volatile<DWORD> m_dwImageTypes;
};  // class CLRPrivAssemblyWinRT
