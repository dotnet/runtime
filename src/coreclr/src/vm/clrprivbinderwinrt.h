// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 


// 
// Contains the types that implement code:ICLRPrivBinder and code:ICLRPrivAssembly for WinRT binding.
// 
//=============================================================================================

#ifdef FEATURE_HOSTED_BINDER

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

#ifndef FEATURE_FUSION
#include "coreclr/corebindresult.h"

// IBindResult maps directly to its one and only implementation on CoreCLR.
typedef CoreBindResult IBindResult;
#endif // FEATURE_FUSION

//=====================================================================================================================
// Forward declarations
class CLRPrivBinderWinRT;
class CLRPrivAssemblyWinRT;
#ifdef FEATURE_CORECLR
class BINDER_SPACE::ApplicationContext;
class BINDER_SPACE::Assembly;
#endif

typedef DPTR(CLRPrivBinderWinRT)     PTR_CLRPrivBinderWinRT;
typedef DPTR(CLRPrivAssemblyWinRT)   PTR_CLRPrivAssemblyWinRT;

BOOL 
IsWindowsNamespace(const char * wszNamespace);

//=====================================================================================================================
//=====================================================================================================================
//=====================================================================================================================
class CLRPrivBinderWinRT : 
    public IUnknownCommon<ICLRPrivBinder
#ifdef FEATURE_FUSION
     , IBindContext
#endif //FEATURE_FUSION
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

    // Implements interface method code:ICLRPrivBinder::VerifyBind.
    STDMETHOD(VerifyBind)(
        IAssemblyName *        pAssemblyName, 
        ICLRPrivAssembly *     pAssembly, 
        ICLRPrivAssemblyInfo * pAssemblyInfo);

    // Implements interface method code:ICLRPrivBinder::GetBinderFlags
    STDMETHOD(GetBinderFlags)(
        DWORD *pBinderFlags)
    {
        STATIC_CONTRACT_WRAPPER;

        if (pBinderFlags == NULL)
            return E_INVALIDARG;

        HRESULT hr = S_OK;

        if (m_pParentBinder != NULL)
            hr = m_pParentBinder->GetBinderFlags(pBinderFlags);
        else
            *pBinderFlags = BINDER_NONE;

        return hr;
    }

    // Implements interface method code:ICLRPrivBinder::GetBinderID.
    STDMETHOD(GetBinderID)(
        UINT_PTR * pBinderId);

    STDMETHOD(FindAssemblyBySpec)(
        LPVOID pvAppDomain,
        LPVOID pvAssemblySpec,
        HRESULT * pResult,
        ICLRPrivAssembly ** ppAssembly)
    {
        LIMITED_METHOD_CONTRACT;

#ifndef DACCESS_COMPILE
        // CLRPrivBinderWinRT instances only have parent binders in Metro processes (not in classic).
        _ASSERTE((AppX::IsAppXProcess()) == (m_pParentBinder != nullptr));
#endif

        if (m_pParentBinder != NULL)
        {
            return m_pParentBinder->FindAssemblyBySpec(pvAppDomain, pvAssemblySpec, pResult, ppAssembly);
        }
        else
        {
            // Note: should never get here if caller is Module::GetAssemblyIfLoaded, but can
            // be called from AssemblySpec::LoadDomainAssembly..
            return FindWinRTAssemblyBySpec(pvAppDomain, pvAssemblySpec, pResult, ppAssembly);
        }
    }

    HRESULT FindWinRTAssemblyBySpec(
        LPVOID pvAppDomain,
        LPVOID pvAssemblySpec,
        HRESULT * pResult,
        ICLRPrivAssembly ** ppAssembly);

#ifdef FEATURE_FUSION	
    //=============================================================================================
    // IBindContext interface methods
    
    // Implements interface method code:IBindContext::PreBind.
    STDMETHOD(PreBind)(
        IAssemblyName * pIAssemblyName, 
        DWORD           dwPreBindFlags, 
        IBindResult **  ppIBindResult);
    
    // Implements interface method code:IBindContext::IsDefaultContext.
    STDMETHOD(IsDefaultContext)();
#endif //FEATURE_FUSION

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
        CLRPrivAssemblyWinRT ** ppAssembly,
        BOOL fPreBind = FALSE);

    // Binds WinRT assemblies only.
    HRESULT BindWinRTAssemblyByName(
        IAssemblyName * pIAssemblyName,
        ICLRPrivAssembly ** ppPrivAssembly,
        BOOL fPreBind = FALSE);

    // Binds WinRT assemblies only.
    HRESULT BindWinRTAssemblyByName(
        IAssemblyName * pIAssemblyName,
        IBindResult ** ppIBindResult,
        BOOL fPreBind = FALSE);

#ifndef FEATURE_FUSION
    HRESULT GetAssemblyAndTryFindNativeImage(SString &sWinmdFilename, LPCWSTR pwzSimpleName, BINDER_SPACE::Assembly ** ppAssembly);
#endif
#ifdef FEATURE_CORECLR
    // On Phone the application's APP_PATH CoreCLR hosting config property is used as the app
    // package graph for RoResolveNamespace to find 3rd party WinMDs.  This method wires up
    // the app paths so the WinRT binder will find 3rd party WinMDs.
    HRESULT SetApplicationContext(BINDER_SPACE::ApplicationContext *pApplicationContext, SString &appLocalWinMD);
#endif
    // Finds assembly with WinRT type if it is already loaded
    // Note: This method could implement interface code:ICLRPrivWinRtTypeBinder if it is ever needed
    PTR_Assembly FindAssemblyForTypeIfLoaded(
        PTR_AppDomain pAppDomain, 
        LPCUTF8       szNamespace, 
        LPCUTF8       szClassName);

#if defined(FEATURE_COMINTEROP_WINRT_DESKTOP_HOST) && !defined(CROSSGEN_COMPILE)
    BOOL SetLocalWinMDPath(HSTRING localWinMDPath);
#endif // FEATURE_COMINTEROP_WINRT_DESKTOP_HOST && !CROSSGEN_COMPILE

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
    
#ifdef FEATURE_FUSION
    HRESULT BindAssemblyToNativeAssembly(CLRPrivAssemblyWinRT *pAssembly);
#endif

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

#ifdef FEATURE_FUSION
    // Native binder assisting logic
    BOOL m_fCanUseNativeImages;

    ReleaseHolder<IILFingerprintFactory> m_pFingerprintFactory;
#endif

#ifdef FEATURE_FUSION
    HRESULT GetParentIBindContext(IBindContext **ppIBindContext);
#endif //FEATURE_FUSION

#ifdef FEATURE_CORECLR
    BINDER_SPACE::ApplicationContext * m_pApplicationContext;
    NewArrayHolder<WCHAR> m_appLocalWinMDPath;
#endif

#ifdef FEATURE_COMINTEROP_WINRT_DESKTOP_HOST
    // App-local location that can be probed for WinMD files
    BOOL m_fCanSetLocalWinMDPath;
    CrstExplicitInit m_localWinMDPathLock;
#ifndef CROSSGEN_COMPILE
    clr::winrt::String m_localWinMDPath;
#endif // !CROSSGEN_COMPILE
#endif // FEATURE_COMINTEROP_WINRT_DESKTOP_HOST

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
    
    // Implements interface method code:ICLRPrivBinder::VerifyBind.
    STDMETHOD(VerifyBind)(
        IAssemblyName *        pAssemblyName, 
        ICLRPrivAssembly *     pAssembly, 
        ICLRPrivAssemblyInfo * pAssemblyInfo)
    {
        STATIC_CONTRACT_WRAPPER;
        return m_pBinder->VerifyBind(pAssemblyName, pAssembly, pAssemblyInfo);
    }

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
    
    // Implements code:ICLRPrivBinder::FindAssemblyBySpec
    STDMETHOD(FindAssemblyBySpec)(
        LPVOID pvAppDomain,
        LPVOID pvAssemblySpec,
        HRESULT * pResult,
        ICLRPrivAssembly ** ppAssembly)
    {
        STATIC_CONTRACT_WRAPPER;
        return m_pBinder->FindAssemblyBySpec(pvAppDomain, pvAssemblySpec, pResult, ppAssembly);
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

#endif //FEATURE_HOSTED_BINDER
