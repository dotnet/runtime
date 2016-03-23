// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 


// 
// Contains the types that implement code:ICLRPrivBinder and code:ICLRPrivAssembly for WinRT binding.
// 
//=============================================================================================

#include "common.h" // precompiled header

#ifndef FEATURE_CORECLR
#include "assemblyusagelogmanager.h"
#endif
#include "clr/fs/file.h"
#include "clrprivbinderwinrt.h"
#include "clrprivbinderutil.h"

#ifndef DACCESS_COMPILE

//=====================================================================================================================
#include "sstring.h"
#ifdef FEATURE_FUSION
#include "fusionlogging.h"
#include "policy.h"
#include "imprthelpers.h" // in fusion/inc
#include "asmimprt.h"
#endif
#ifdef FEATURE_APPX
#include "appxutil.h"
#endif
#include <TypeResolution.h>
#include "delayloadhelpers.h"
#ifdef FEATURE_CORECLR
#include "../binder/inc/applicationcontext.hpp"
#include "../binder/inc/assemblybinder.hpp"
#include "../binder/inc/assembly.hpp"
#include "../binder/inc/debuglog.hpp"
#include "../binder/inc/utils.hpp"
#include "../binder/inc/fusionassemblyname.hpp"
#endif

#ifdef CROSSGEN_COMPILE
#include "crossgenroresolvenamespace.h"
#include "../binder/inc/fusionassemblyname.hpp"
#endif


using namespace CLRPrivBinderUtil;


//=====================================================================================================================
#define WINDOWS_NAMESPACE W("Windows")  
#define WINDOWS_NAMESPACE_PREFIX WINDOWS_NAMESPACE W(".")  

#define WINDOWS_NAMESPACEA "Windows"  
#define WINDOWS_NAMESPACE_PREFIXA WINDOWS_NAMESPACEA "."  

//=====================================================================================================================
static BOOL 
IsWindowsNamespace(const WCHAR * wszNamespace)
{
    LIMITED_METHOD_CONTRACT;
    
    if (wcsncmp(wszNamespace, WINDOWS_NAMESPACE_PREFIX, (_countof(WINDOWS_NAMESPACE_PREFIX) - 1)) == 0)
    {  
        return TRUE;  
    }  
    else if (wcscmp(wszNamespace, WINDOWS_NAMESPACE) == 0)  
    {  
        return TRUE;
    }  

    return FALSE;
}


//=====================================================================================================================
BOOL 
IsWindowsNamespace(const char * wszNamespace)
{
    LIMITED_METHOD_CONTRACT;
    
    if (strncmp(wszNamespace, WINDOWS_NAMESPACE_PREFIXA, (_countof(WINDOWS_NAMESPACE_PREFIXA) - 1)) == 0)
    {  
        return TRUE;  
    }  
    else if (strcmp(wszNamespace, WINDOWS_NAMESPACEA) == 0)
    {  
        return TRUE;
    }  

    return FALSE;
}


//=====================================================================================================================
DELAY_LOADED_FUNCTION(WinTypes, RoResolveNamespace);

//=====================================================================================================================
HRESULT RoResolveNamespace(
    _In_opt_ const HSTRING name,
    _In_opt_ const HSTRING windowsMetaDataDir,
    _In_ const DWORD packageGraphDirsCount,
    _In_reads_opt_(packageGraphDirsCount) const HSTRING *packageGraphDirs,
    _Out_opt_ DWORD *metaDataFilePathsCount,
    _Outptr_opt_result_buffer_(*metaDataFilePathsCount) HSTRING **metaDataFilePaths,
    _Out_opt_ DWORD *subNamespacesCount,
    _Outptr_opt_result_buffer_(*subNamespacesCount) HSTRING **subNamespaces)
{
    LIMITED_METHOD_CONTRACT;
    HRESULT hr = S_OK;

    decltype(RoResolveNamespace) * pFunc = nullptr;
    IfFailRet(DelayLoad::WinTypes::RoResolveNamespace.GetValue(&pFunc));

    return (*pFunc)(
        name, windowsMetaDataDir, packageGraphDirsCount, packageGraphDirs, metaDataFilePathsCount,
        metaDataFilePaths, subNamespacesCount, subNamespaces);
}

//=====================================================================================================================
CLRPrivBinderWinRT * CLRPrivBinderWinRT::s_pSingleton = nullptr;

//=====================================================================================================================
CLRPrivBinderWinRT::CLRPrivBinderWinRT(
    ICLRPrivBinder *        pParentBinder, 
    CLRPrivTypeCacheWinRT * pWinRtTypeCache, 
    LPCWSTR *               rgwzAltPath, 
    UINT                    cAltPaths, 
    NamespaceResolutionKind fNamespaceResolutionKind,
    BOOL                    fCanUseNativeImages)
    : m_pTypeCache(clr::SafeAddRef(pWinRtTypeCache))
    , m_pParentBinder(pParentBinder)                        // Do not addref, lifetime directly tied to parent.
#ifdef FEATURE_FUSION
    , m_fCanUseNativeImages(fCanUseNativeImages)
#endif
    , m_fNamespaceResolutionKind(fNamespaceResolutionKind)
#ifdef FEATURE_CORECLR
    , m_pApplicationContext(nullptr)
    , m_appLocalWinMDPath(nullptr)
#endif
#ifdef FEATURE_COMINTEROP_WINRT_DESKTOP_HOST
    , m_fCanSetLocalWinMDPath(TRUE)
#endif // FEATURE_COMINTEROP_WINRT_DESKTOP_HOST
{
    STANDARD_VM_CONTRACT;
    PRECONDITION(CheckPointer(pWinRtTypeCache));
    
#ifndef CROSSGEN_COMPILE
    //  - To prevent deadlock with GC thread, we cannot trigger GC while holding the lock
    //  - To prevent deadlock with profiler thread, we cannot allow thread suspension
    m_MapsLock.Init(
        CrstCLRPrivBinderMaps, 
        (CrstFlags)(CRST_REENTRANCY // Reentracy is needed for code:CLRPrivAssemblyWinRT::Release
                    | CRST_GC_NOTRIGGER_WHEN_TAKEN 
                    | CRST_DEBUGGER_THREAD 
                    INDEBUG(| CRST_DEBUG_ONLY_CHECK_FORBID_SUSPEND_THREAD)));
    m_MapsAddLock.Init(CrstCLRPrivBinderMapsAdd);

#ifdef FEATURE_COMINTEROP_WINRT_DESKTOP_HOST
    m_localWinMDPathLock.Init(CrstCrstCLRPrivBinderLocalWinMDPath);
#endif // FEATURE_COMINTEROP_WINRT_DESKTOP_HOST

    // Copy altpaths
    if (cAltPaths > 0)
    {
        m_rgAltPaths.Allocate(cAltPaths);

        for (UINT iAltPath = 0; iAltPath < cAltPaths; iAltPath++)
        {
            IfFailThrow(WindowsCreateString(
                rgwzAltPath[iAltPath], 
                (UINT32)wcslen(rgwzAltPath[iAltPath]), 
                m_rgAltPaths.GetRawArray() + iAltPath));
        }
    }
#if defined(FEATURE_APPX) && !defined(FEATURE_CORECLR)
    else if (AppX::IsAppXNGen())
    {
        // If this is an NGen worker process for AppX, then the process doesn't actually run in the package,
        // and RoResolveNamespace won't work without some help.  AppX::GetCurrentPackageInfo can give us the
        // package graph, which we can pass to RoResolveNamespace to make it work properly.
        UINT32 cbBuffer = 0;
        UINT32 nCount = 0;
        HRESULT hr = AppX::GetCurrentPackageInfo(PACKAGE_FILTER_CLR_DEFAULT, &cbBuffer, nullptr, nullptr);
        if (hr != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
            ThrowHR(hr);

        NewArrayHolder<BYTE> pbBuffer(new (nothrow) BYTE[cbBuffer]);
        IfNullThrow(pbBuffer);
        IfFailThrow(AppX::GetCurrentPackageInfo(PACKAGE_FILTER_CLR_DEFAULT, &cbBuffer, pbBuffer, &nCount));

        m_rgAltPaths.Allocate(nCount);

        PCPACKAGE_INFO pPackageInfo = reinterpret_cast<PCPACKAGE_INFO>(static_cast<PBYTE>(pbBuffer));
        for (UINT32 iAltPath = 0; iAltPath < nCount; iAltPath++)
        {
            IfFailThrow(WindowsCreateString(
                pPackageInfo[iAltPath].path,
                (UINT32)wcslen(pPackageInfo[iAltPath].path),
                m_rgAltPaths.GetRawArray() + iAltPath));
        }
    }
#endif //FEATURE_APPX && !FEATURE_CORECLR
#endif //CROSSGEN_COMPILE

#ifdef FEATURE_FUSION
    IfFailThrow(RuntimeCreateCachingILFingerprintFactory(&m_pFingerprintFactory));
#endif
}

//=====================================================================================================================
CLRPrivBinderWinRT::~CLRPrivBinderWinRT()
{
    WRAPPER_NO_CONTRACT;

#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)
    AssemblyUsageLogManager::UnRegisterBinderFromUsageLog((UINT_PTR)this);
#endif
    if (m_pTypeCache != nullptr)
    {
        m_pTypeCache->Release();
    }
}

//=====================================================================================================================
CLRPrivBinderWinRT * 
CLRPrivBinderWinRT::GetOrCreateBinder(
    CLRPrivTypeCacheWinRT * pWinRtTypeCache, 
    NamespaceResolutionKind fNamespaceResolutionKind)
{
    STANDARD_VM_CONTRACT;
    HRESULT hr = S_OK;

    // This should be allocated directly by CLRPrivBinderAppX in the AppX scenario.
#ifdef FEATURE_APPX_BINDER
	_ASSERTE(!AppX::IsAppXProcess());
#endif

    if (s_pSingleton == nullptr)
    {
        ReleaseHolder<CLRPrivBinderWinRT> pBinder;
        pBinder = clr::SafeAddRef(new CLRPrivBinderWinRT(
            nullptr,    // pParentBinder
            pWinRtTypeCache, 
            nullptr,    // rgwzAltPath
            0,          // cAltPaths
            fNamespaceResolutionKind,
            TRUE // fCanUseNativeImages
            ));
        
        if (InterlockedCompareExchangeT<decltype(s_pSingleton)>(&s_pSingleton, pBinder, nullptr) == nullptr)
        {
            pBinder.SuppressRelease();
        }
#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)
        // Create and register WinRT usage log
        ReleaseHolder<IAssemblyUsageLog> pNewWinRTUsageLog;
        IfFailThrow(AssemblyUsageLogManager::GetUsageLogForContext(W("WinRT"), W("NotApp"), &pNewWinRTUsageLog));

        UINT_PTR winRTBinderId;
        IfFailThrow(pBinder->GetBinderID(&winRTBinderId));
        IfFailThrow(AssemblyUsageLogManager::RegisterBinderWithUsageLog(winRTBinderId, pNewWinRTUsageLog));
#endif
    }
    _ASSERTE(s_pSingleton->m_fNamespaceResolutionKind == fNamespaceResolutionKind);
    
    return clr::SafeAddRef(s_pSingleton);
}

//=====================================================================================================================
STDAPI
CreateAssemblyNameObjectFromMetaData(
    LPASSEMBLYNAME    *ppAssemblyName,
    LPCOLESTR          szAssemblyName,
    ASSEMBLYMETADATA  *pamd,
    LPVOID             pvReserved);

//=====================================================================================================================
HRESULT CLRPrivBinderWinRT::BindWinRTAssemblyByName(
    IAssemblyName *         pAssemblyName,
    CLRPrivAssemblyWinRT ** ppAssembly,
    BOOL fPreBind)
{
    STANDARD_VM_CONTRACT;
    HRESULT hr = S_OK;
    ReleaseHolder<CLRPrivAssemblyWinRT> pAssembly;    
#ifndef FEATURE_CORECLR
    NewArrayHolder<WCHAR> wszAssemblySimpleName;
#endif

#ifndef CROSSGEN_COMPILE
#ifndef FEATURE_CORECLR
    fusion::logging::StatusScope logStatus(0, ID_FUSLOG_BINDING_STATUS_WINRT, &hr);
#else
    BINDER_SPACE::BINDER_LOG_ENTER(W("CLRPrivBinderWinRT_CoreCLR::BindWinRTAssemblyByName"));
#endif
#endif
    VALIDATE_ARG_RET(pAssemblyName != nullptr);
    VALIDATE_ARG_RET(ppAssembly != nullptr);
    
    DWORD dwContentType = AssemblyContentType_Default;
    IfFailGo(hr = fusion::util::GetProperty(pAssemblyName, ASM_NAME_CONTENT_TYPE, &dwContentType));
    if ((hr != S_OK) || (dwContentType != AssemblyContentType_WindowsRuntime))
    {
        IfFailGo(CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT);
    }
    
    // Note: WinRT type resolution is supported also on pre-Win8 with DesignerResolveEvent
    if (!WinRTSupported() && (m_fNamespaceResolutionKind != NamespaceResolutionKind_DesignerResolveEvent))
    {
        IfFailGo(COR_E_PLATFORMNOTSUPPORTED);
    }
    
#ifndef FEATURE_CORECLR
    IfFailGo(fusion::util::GetProperty(pAssemblyName, ASM_NAME_NAME, &wszAssemblySimpleName));
#else
    WCHAR wszAssemblySimpleName[_MAX_PATH];
    DWORD cchAssemblySimpleName = _MAX_PATH;
    IfFailGo(pAssemblyName->GetName(&cchAssemblySimpleName, wszAssemblySimpleName));
#endif
    
    LPWSTR wszFullTypeName = wcschr(wszAssemblySimpleName, W('!'));
    
    if (wszFullTypeName != nullptr)
    {
        _ASSERTE(wszAssemblySimpleName < wszFullTypeName);
        if (!(wszAssemblySimpleName < wszFullTypeName))
        {
            IfFailGo(E_UNEXPECTED);
        }

        // Turns wszAssemblySimpleName into simple name, wszFullTypeName into type name.
        *wszFullTypeName++ = W('\0');
        
        CLRPrivBinderUtil::WStringList * pFileNameList = nullptr;
        BOOL fIsWindowsNamespace = FALSE;

        {
            // don't look past the first generics backtick (if any)
            WCHAR *pGenericBegin = (WCHAR*)wcschr(wszFullTypeName, W('`'));
            if (pGenericBegin != nullptr)
                *pGenericBegin = W('\0');

            LPWSTR wszSimpleTypeName = wcsrchr(wszFullTypeName, W('.'));

            // restore the generics backtick
            if (pGenericBegin != nullptr)
                *pGenericBegin = W('`');

            if (wszSimpleTypeName == nullptr)
            {
                IfFailGo(CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT);
            }
            
            // Turns wszFullTypeName into namespace name (without simple type name)
            *wszSimpleTypeName = W('\0');
            
            IfFailGo(GetFileNameListForNamespace(wszFullTypeName, &pFileNameList));
            
            fIsWindowsNamespace = IsWindowsNamespace(wszFullTypeName);

            // Turns wszFullTypeName back into full type name (was namespace name)
            *wszSimpleTypeName = W('.');
        }
        
        if (pFileNameList == nullptr)
        {   // There are no file associated with the namespace
            IfFailGo(CLR_E_BIND_TYPE_NOT_FOUND);
        }
        
        CLRPrivBinderUtil::WStringListElem * pFileNameElem = pFileNameList->GetHead();
        for (; pFileNameElem != nullptr; pFileNameElem = CLRPrivBinderUtil::WStringList::GetNext(pFileNameElem))
        {
            const WCHAR * wszFileName = pFileNameElem->GetValue();
            pAssembly = FindAssemblyByFileName(wszFileName);
            
            WCHAR wszFileNameStripped[_MAX_PATH] = {0};
            SplitPath(wszFileName, NULL, NULL, NULL, NULL, wszFileNameStripped, _MAX_PATH, NULL, NULL);

            if (pAssembly == nullptr)
            {
                NewHolder<CLRPrivResourcePathImpl> pResource(
                    new CLRPrivResourcePathImpl(wszFileName));
                
                ReleaseHolder<IAssemblyName> pAssemblyDefName;

                // Instead of using the metadata of the assembly to get the AssemblyDef name, fake one up from the filename.
                // This ties in with the PreBind binding behavior and ngen. This particular logic was implemented in order
                // to provide best performance as actually reading the metadata was prohibitively slow. (Due to the cost of opening
                // the assembly file.) We use a zeroed out ASSEMBLYMETADATA structure to create the assembly name object
                // in order to ensure that every field of the assembly name is filled out as if this was created from  a normal
                // assembly def row.
                // See comment on CLRPrivBinderWinRT::PreBind for further details about NGEN binding and WinMDs.
                ASSEMBLYMETADATA asmd = { 0 };
                IfFailGo(CreateAssemblyNameObjectFromMetaData(&pAssemblyDefName, wszFileNameStripped, &asmd, NULL));
                DWORD dwAsmContentType = AssemblyContentType_WindowsRuntime;
                IfFailGo(pAssemblyDefName->SetProperty(ASM_NAME_CONTENT_TYPE, (LPBYTE)&dwAsmContentType, sizeof(dwAsmContentType)));

                // 
                // Creating the BindResult we will pass to the native binder to find native images.
                // We strip off the type from the assembly name, leaving the simple assembly name.
                // The native binder stores native images under subdirectories named after their
                // simple name so we only want to pass the simple name portion of the name to it,
                // which it uses along with the fingerprint matching in BindResult to find the 
                // native image for this WinRT assembly.
                // The WinRT team has said that WinMDs will have the same simple name as the filename.
                // 
                IfFailGo(pAssemblyDefName->SetProperty(ASM_NAME_NAME, wszFileNameStripped, (lstrlenW(wszFileNameStripped) + 1) * sizeof(WCHAR)));

#ifdef FEATURE_FUSION
                NewHolder<CLRPrivAssemblyBindResultWrapper> pBindResult(
                    new CLRPrivAssemblyBindResultWrapper(pAssemblyDefName, wszFileName, m_pFingerprintFactory));
#else                
                NewHolder<CoreBindResult> pBindResult(new CoreBindResult());
                StackSString sAssemblyPath(pResource->GetPath());
                ReleaseHolder<BINDER_SPACE::Assembly> pBinderAssembly;

                IfFailGo(GetAssemblyAndTryFindNativeImage(sAssemblyPath, wszFileNameStripped, &pBinderAssembly));

                // We have set bInGac to TRUE here because the plan is full trust for WinRT.  If this changes, we may need to check with
                // AppDomain to verify trust based on the WinMD's path
                pBindResult->Init(pBinderAssembly, TRUE);
#endif
                NewHolder<CLRPrivAssemblyWinRT> pNewAssembly(
                    new CLRPrivAssemblyWinRT(this, pResource, pBindResult, fIsWindowsNamespace));
                
                // pNewAssembly holds references to these now
                pResource.SuppressRelease();
                pBindResult.SuppressRelease();
                
                // Add the assembly into cache (multi-thread aware)
                pAssembly = AddFileNameToAssemblyMapping(pResource->GetPath(), pNewAssembly);
                
                // We did not find an existing assembly in the cache and are using the newly created pNewAssembly.
                // Stop it from being deleted when we go out of scope.
                if (pAssembly == pNewAssembly)
                {
                    pNewAssembly.SuppressRelease();
                }
                
#ifndef FEATURE_CORECLR                
                if (fPreBind)
                {
                    // We are pre-binding to this WinMD and do not want to open it
                    // Compare the filename to the assembly simple name.  This is legal to do with WinRT because at NGen time
                    // we embed a WinRT dependency as assembly def name component plus a namespace and type from it.
                    // At bind time, this type should still exist in the same assembly.  If it doesn't, and has been moved,
                    // the native image validation will fail anyway and we'll fall back to IL.  This is because if the type has 
                    // been moved to another WinMD, it must have been removed from the first one because WinRT allows no duplicates.
                    // See comment on CLRPrivBinderWinRT::PreBind for further details.                    
                    if (!_wcsicmp(wszAssemblySimpleName, wszFileNameStripped))
                    {
                        *ppAssembly = pAssembly.Extract();
                        return (hr = S_OK);
                    }
                    else
                    {
                        continue;
                    }
                }
#endif
            }
#ifndef FEATURE_CORECLR
            else if (fPreBind)
            {
                // We are pre-binding to this WinMD and do not want to force it to be loaded into the runtime yet.
                // Compare the filename to the assembly simple name.  This is legal to do with WinRT because at NGen time
                // we embed a WinRT dependency as assembly def name component plus a namespace and type from it.
                // At bind time, this type should still exist in the same assembly.  If it doesn't, and has been moved,
                // the native image validation will fail anyway and we'll fall back to IL.  This is because if the type has 
                // been moved to another WinMD, it must have been removed from the first one because WinRT allows no duplicates.
                // See comment on CLRPrivBinderWinRT::PreBind for further details.
                if (!_wcsicmp(wszAssemblySimpleName, wszFileNameStripped))
                {
                    *ppAssembly = pAssembly.Extract();
                    return (hr = S_OK);
                } 
                else
                {
                    continue;
                }
            }
#endif

            //
            // Look to see if there's a native image available.
            //
            hr = pAssembly->EnsureAvailableImageTypes();

            // Determine if this is the assembly we really want to find.
            IfFailGo(hr = m_pTypeCache->ContainsType(pAssembly, wszFullTypeName));
            if (hr == S_OK)
            {   // The type we are looking for has been found in this assembly
#ifndef CROSSGEN_COMPILE
#ifndef FEATURE_CORECLR
                fusion::logging::LogMessage(0, ID_FUSLOG_BINDING_STATUS_FOUND, wszFileName);
#else
                BINDER_SPACE::BINDER_LOG_LEAVE_HR(W("CLRPrivBinderWinRT_CoreCLR::BindWinRTAssemblyByName"), hr);
#endif
#endif
                *ppAssembly = pAssembly.Extract();
                return (hr = S_OK);
            }
            _ASSERTE(hr == S_FALSE);
        }
    }

    // The type has not been found in any of the files from the type's namespace
    hr = CLR_E_BIND_TYPE_NOT_FOUND;
 ErrExit:

#ifdef FEATURE_CORECLR
    BINDER_SPACE::BINDER_LOG_LEAVE_HR(W("CLRPrivBinderWinRT_CoreCLR::BindWinRTAssemblyByName"), hr);
#endif
    return hr;
} // CLRPrivBinderWinRT::BindWinRTAssemblyByName

#ifdef FEATURE_FUSION
//=====================================================================================================================
HRESULT CLRPrivBinderWinRT::BindAssemblyToNativeAssembly(CLRPrivAssemblyWinRT *pAssembly)
{
    HRESULT hr = S_OK;

    if (!m_fCanUseNativeImages)
        return hr;

    ReleaseHolder<IBindResult> pIBindResultIL;
    IfFailRet(pAssembly->GetIBindResult(&pIBindResultIL));
    _ASSERTE(pIBindResultIL != nullptr);

    NewArrayHolder<WCHAR> wzZapSet = DuplicateStringThrowing(g_pConfig->ZapSet());
    NativeConfigData cfgData = {
        wzZapSet,
        PEFile::GetNativeImageConfigFlags()
    };

    ReleaseHolder<IBindContext> pIBindContext;
    IfFailRet(GetParentIBindContext(&pIBindContext));
                    
    // Fire BindingNgenPhaseStart ETW event if enabled.
    {
        InlineSString<_MAX_PATH> ssAssemblyName;
        FireEtwBindingNgenPhaseStart(
            (AppDomain::GetCurrentDomain()->GetId().m_dwId),
            LOADCTX_TYPE_HOSTED,
            ETWFieldUnused,
            ETWLoaderLoadTypeNotAvailable,
            NULL,
            pAssembly->m_pResourceIL->GetPath(),
            GetClrInstanceId());
    }
                    
    IfFailRet(BindToNativeAssembly(pIBindResultIL, &cfgData, pIBindContext, fusion::logging::GetCurrentFusionBindLog()));

    // Fire BindingNgenPhaseEnd ETW event if enabled.
    {
        InlineSString<_MAX_PATH> ssAssemblyName;
        FireEtwBindingNgenPhaseEnd(
            (AppDomain::GetCurrentDomain()->GetId().m_dwId),
            LOADCTX_TYPE_HOSTED,
            ETWFieldUnused,
            ETWLoaderLoadTypeNotAvailable,
            NULL,
            pAssembly->m_pResourceIL->GetPath(),
            GetClrInstanceId());
    }
                    
    // BindToNativeAssembly can return S_FALSE, but this could be misleading.
    if (hr == S_FALSE)
        hr = S_OK;

    return hr;
}
#endif

#if defined(FEATURE_COMINTEROP_WINRT_DESKTOP_HOST) && !defined(CROSSGEN_COMPILE)
BOOL CLRPrivBinderWinRT::SetLocalWinMDPath(HSTRING localWinMDPath)
{
    STANDARD_VM_CONTRACT;
    STATIC_CONTRACT_CAN_TAKE_LOCK;

    CrstHolder lock(&m_localWinMDPathLock);

    // We use the empty string as a sential, so don't allow explicitly setting the binding base to empty.
    if (localWinMDPath == nullptr)
    {
        return FALSE;
    }

    // If we've already set a binding base, then the current base must match the exisitng one exactly
    if (!m_localWinMDPath.IsEmpty())
    {
        return m_localWinMDPath.CompareOrdinal(clr::winrt::StringReference(localWinMDPath)) == 0;
    }

    // If we've already done WinRT binding, we can't set the binding base because that could lead to inconsistent results when binding
    // the same name after the base is set.
    if (!m_fCanSetLocalWinMDPath)
    {
        return FALSE;
    }

    m_localWinMDPath.Initialize(localWinMDPath);

    return TRUE;
}
#endif // FEATURE_COMINTEROP_WINRT_DESKTOP_HOST && !CROSSGEN_COMPILE

//=====================================================================================================================
HRESULT CLRPrivBinderWinRT::BindWinRTAssemblyByName(
    IAssemblyName *     pAssemblyName,
    ICLRPrivAssembly ** ppPrivAssembly,
    BOOL fPreBind)
{
    STANDARD_VM_CONTRACT;
    HRESULT hr = S_OK;

    ReleaseHolder<CLRPrivAssemblyWinRT> pWinRTAssembly;
    IfFailRet(BindWinRTAssemblyByName(pAssemblyName, &pWinRTAssembly, fPreBind));
    IfFailRet(pWinRTAssembly->QueryInterface(__uuidof(ICLRPrivAssembly), (LPVOID *)ppPrivAssembly));

    return hr;
}

//=====================================================================================================================
HRESULT CLRPrivBinderWinRT::BindWinRTAssemblyByName(
    IAssemblyName * pAssemblyName,
    IBindResult ** ppIBindResult,
    BOOL fPreBind)
{
    STANDARD_VM_CONTRACT;
    HRESULT hr = S_OK;

    VALIDATE_ARG_RET(pAssemblyName != nullptr);
    VALIDATE_ARG_RET(ppIBindResult != nullptr);

    ReleaseHolder<CLRPrivAssemblyWinRT> pWinRTAssembly;
    IfFailRet(BindWinRTAssemblyByName(pAssemblyName, &pWinRTAssembly, fPreBind));
    IfFailRet(pWinRTAssembly->GetIBindResult(ppIBindResult));

    return hr;
}

#ifndef FEATURE_FUSION
//
// This method opens the assembly using the CoreCLR Binder, which has logic supporting opening either the IL or 
// even just the native image without IL present.
// RoResolveNamespace has already told us the IL file to open.  We try and find a native image to open instead
// by looking in the TPA list and the App_Ni_Paths.
//
HRESULT CLRPrivBinderWinRT::GetAssemblyAndTryFindNativeImage(SString &sWinmdFilename, LPCWSTR pwzSimpleName, BINDER_SPACE::Assembly ** ppAssembly)
{
    HRESULT hr = S_OK;

#ifdef FEATURE_CORECLR
    if (!m_pApplicationContext->IsTpaListProvided())
        return COR_E_FILENOTFOUND;

    BINDER_SPACE::SimpleNameToFileNameMap * tpaMap = m_pApplicationContext->GetTpaList();
    const BINDER_SPACE::SimpleNameToFileNameMapEntry *pTpaEntry = tpaMap->LookupPtr(pwzSimpleName);
    if (pTpaEntry != nullptr)
    {
        ReleaseHolder<BINDER_SPACE::Assembly> pAssembly;

        if (pTpaEntry->m_wszNIFileName != nullptr)
        {
            SString fileName(pTpaEntry->m_wszNIFileName);

            // A GetAssembly overload perhaps, or just another parameter to the existing method
            hr = BINDER_SPACE::AssemblyBinder::GetAssembly(fileName,
                                FALSE, /* fInspectionOnly */
                                TRUE, /* fIsInGAC */
                                TRUE /* fExplicitBindToNativeImage */,
                                &pAssembly,
                                sWinmdFilename.GetUnicode()
                                );

            // On file not found, simply fall back to app ni path probing
            if (hr != HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
            {
                // Any other error is fatal
                IfFailRet(hr);

                *ppAssembly = pAssembly.Extract();
                return (hr = S_OK);
            }
        }
    }

    StringArrayList *pBindingPaths = m_pApplicationContext->GetAppNiPaths();
    
    // Loop through the binding paths looking for a matching assembly
    for (DWORD i = 0; i < pBindingPaths->GetCount(); i++)
    {
        ReleaseHolder<BINDER_SPACE::Assembly> pAssembly;
        LPCWSTR wszBindingPath = (*pBindingPaths)[i];
        
        SString simpleName(pwzSimpleName);
        SString fileName(wszBindingPath);
        BINDER_SPACE::CombinePath(fileName, simpleName, fileName);
        fileName.Append(W(".ni.DLL"));
        
        hr = BINDER_SPACE::AssemblyBinder::GetAssembly(fileName,
                        FALSE, /* fInspectionOnly */
                        FALSE, /* fIsInGAC */
                        TRUE /* fExplicitBindToNativeImage */,
                        &pAssembly);
        
        // Since we're probing, file not founds are ok and we should just try another
        // probing path
        if (hr == HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND))
        {
            continue;
        }
        
        IfFailRet(hr);

        *ppAssembly = pAssembly.Extract();
        return (hr = S_OK);
    }
    
    // We did not find a native image for this WinMD; open the WinMD file itself as the assembly to return.
    hr = BINDER_SPACE::AssemblyBinder::GetAssembly(sWinmdFilename,
                            FALSE, /* fInspectionOnly */
                            FALSE, /* fIsInGAC */
                            FALSE /* fExplicitBindToNativeImage */,
                            ppAssembly);
#else
    ReleaseHolder<BINDER_SPACE::Assembly> pAssembly;

    // This codepath is used for desktop crossgen
    pAssembly = new BINDER_SPACE::Assembly();

    pAssembly->SetPEImage(PEImage::OpenImage(sWinmdFilename, MDInternalImport_Default));

    pAssembly->m_assemblyPath.Set(sWinmdFilename);

    *ppAssembly = pAssembly.Extract();
#endif

    return hr;
}
#endif // !FEATURE_FUSION

#ifdef FEATURE_CORECLR
//=====================================================================================================================
HRESULT CLRPrivBinderWinRT::SetApplicationContext(BINDER_SPACE::ApplicationContext *pApplicationContext, SString &appLocalWinMD)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;
    
    _ASSERTE(pApplicationContext != nullptr);
    m_pApplicationContext = pApplicationContext;

    StringArrayList * pAppPaths = m_pApplicationContext->GetAppPaths();
    
#ifndef CROSSGEN_COMPILE
    DWORD cAppPaths = pAppPaths->GetCount();
    m_rgAltPaths.Allocate(cAppPaths);
    
    for (DWORD i = 0; i < cAppPaths; i++)
    {
        IfFailRet(WindowsCreateString(
                pAppPaths->Get(i).GetUnicode(),
                (UINT32)(pAppPaths->Get(i).GetCount()),
                m_rgAltPaths.GetRawArray() + i));
    }

    if (!appLocalWinMD.IsEmpty())
    {
        m_appLocalWinMDPath = DuplicateStringThrowing(appLocalWinMD.GetUnicode());
    }
#else
    Crossgen::SetAppPaths(pAppPaths);
#endif
    
    return hr;
}
#endif //FEATURE_CORECLR

//=====================================================================================================================
// Implements interface method code:ICLRPrivBinder::BindAssemblyByName.
// 
HRESULT CLRPrivBinderWinRT::BindAssemblyByName(
    IAssemblyName     * pAssemblyName,
    ICLRPrivAssembly ** ppAssembly)
{
    STANDARD_BIND_CONTRACT;
    HRESULT hr = S_OK;

    VALIDATE_ARG_RET((pAssemblyName != nullptr) && (ppAssembly != nullptr));
    
    EX_TRY
    {
        if (m_pParentBinder != nullptr)
        {
            // Delegate to parent binder.
            hr = m_pParentBinder->BindAssemblyByName(pAssemblyName, ppAssembly);
        }
        else
        {
            hr = BindWinRTAssemblyByName(pAssemblyName, ppAssembly);
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

//=====================================================================================================================
ReleaseHolder<CLRPrivAssemblyWinRT> 
CLRPrivBinderWinRT::FindAssemblyByFileName(
    PCWSTR wszFileName)
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_CAN_TAKE_LOCK;

    ForbidSuspendThreadHolder suspend;
    {
        CrstHolder lock(&m_MapsLock);

        const FileNameToAssemblyWinRTMapEntry * pEntry = m_FileNameToAssemblyMap.LookupPtr(wszFileName);
        return (pEntry == nullptr) ? nullptr : clr::SafeAddRef(pEntry->m_pAssembly);
    }
}

//=====================================================================================================================
// Add FileName -> CLRPrivAssemblyWinRT * mapping to the map (multi-thread safe).
// 
ReleaseHolder<CLRPrivAssemblyWinRT> 
CLRPrivBinderWinRT::AddFileNameToAssemblyMapping(
    PCWSTR                 wszFileName, 
    CLRPrivAssemblyWinRT * pAssembly)
{
    STANDARD_VM_CONTRACT;
    
    _ASSERTE(pAssembly != nullptr);
    
    // We have to serialize all Add operations
    CrstHolder lock(&m_MapsAddLock);
    
    // Wrapper for m_FileNameToAssemblyMap.Add that avoids call out into host
    FileNameToAssemblyWinRTMap::AddPhases addCall;
    
    // 1. Preallocate one element
    addCall.PreallocateForAdd(&m_FileNameToAssemblyMap);
    {
        // 2. Take the reader lock which can be taken during stack walking
        // We cannot call out into host from ForbidSuspend region (i.e. no allocations/deallocations)
        ForbidSuspendThreadHolder suspend;
        {
            CrstHolder lock(&m_MapsLock);

            const FileNameToAssemblyWinRTMapEntry * pEntry = m_FileNameToAssemblyMap.LookupPtr(wszFileName);
            CLRPrivAssemblyWinRT * pResultAssembly = nullptr;
            if (pEntry != nullptr)
            {
                pResultAssembly = pEntry->m_pAssembly;
                
                // 3a. Use the newly allocated table (if any) to avoid allocation in the next call (no call out into host)
                addCall.AddNothing_PublishPreallocatedTable();
            }
            else
            {
                // 3b. Add the element to the hash table (no call out into host)
                FileNameToAssemblyWinRTMapEntry e;
                e.m_wszFileName = wszFileName;
                e.m_pAssembly = pAssembly;
                addCall.Add(e);
                
                pResultAssembly = pAssembly;
            }
            return clr::SafeAddRef(pResultAssembly);
        }
    }
    // 4. Cleanup the old memory (if any) - will be called by destructor of addCall
    //addCall.DeleteOldTable();
}

//=====================================================================================================================
void 
CLRPrivBinderWinRT::RemoveFileNameToAssemblyMapping(
    PCWSTR wszFileName)
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_CAN_TAKE_LOCK;

    ForbidSuspendThreadHolder suspend;
    {
        CrstHolder lock(&m_MapsLock);

        m_FileNameToAssemblyMap.Remove(wszFileName);
    }
}

//=====================================================================================================================
// Returns list of file names from code:m_NamespaceToFileNameListMap for the namespace
// 
HRESULT 
CLRPrivBinderWinRT::GetFileNameListForNamespace(
    LPCWSTR                           wszNamespace, 
    CLRPrivBinderUtil::WStringList ** ppFileNameList)
{
    STANDARD_VM_CONTRACT;
    STATIC_CONTRACT_CAN_TAKE_LOCK;

    HRESULT hr = S_OK;
    
    CLRPrivBinderUtil::WStringList * pFileNameList = nullptr;
    {
        ForbidSuspendThreadHolder suspend;
        {
            CrstHolder lock(&m_MapsLock);

            const NamespaceToFileNameListMapEntry * pEntry = m_NamespaceToFileNameListMap.LookupPtr(wszNamespace);
            if (pEntry != nullptr)
            {
                // Entries from the map are never removed, so we do not have to protect the file name list with a lock
                pFileNameList = pEntry->m_pFileNameList;
            }
        }
    }
    
    if (pFileNameList != nullptr)
    {
        *ppFileNameList = pFileNameList;
    }
    else
    {
        CLRPrivBinderUtil::WStringListHolder hFileNameList;
        LPCWSTR wszNamespaceRoResolve = wszNamespace;
        
#ifndef CROSSGEN_COMPILE
        if (m_fNamespaceResolutionKind == NamespaceResolutionKind_WindowsAPI)
        {
            CoTaskMemHSTRINGArrayHolder hFileNames;
            
            UINT32 cchNamespaceRoResolve;
            IfFailRet(StringCchLength(wszNamespaceRoResolve, &cchNamespaceRoResolve));

            CLRConfigStringHolder wszWinMDPathConfig;
            LPWSTR wszWinMDPath = nullptr;
            UINT32 cchWinMDPath = 0;

#ifdef FEATURE_CORECLR
            wszWinMDPath = m_appLocalWinMDPath;
#else
            if (AppX::IsAdaptiveApp())
            {
                IfFailRet(AppX::GetWinMetadataDirForAdaptiveApps(&wszWinMDPath));
            }

            else if (AppX::IsAppXDesignMode() || IsNgenOffline())
            {
                wszWinMDPathConfig = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_WinMDPath);
                wszWinMDPath = wszWinMDPathConfig;
            }
#endif // FEATURE_CORECLR
                
            if (wszWinMDPath != nullptr)
            {
                IfFailRet(StringCchLength(wszWinMDPath, &cchWinMDPath));
            }
   
            DWORD     cFileNames = 0;
            HSTRING * rgFileNames = nullptr;
            hr = RoResolveNamespace(
                WinRtStringRef(wszNamespaceRoResolve, cchNamespaceRoResolve),
                wszWinMDPath != nullptr ? (HSTRING)WinRtStringRef(wszWinMDPath, cchWinMDPath) : nullptr, // hsWindowsSdkPath
                m_rgAltPaths.GetCount(),    // cPackageGraph
                m_rgAltPaths.GetRawArray(), // rgPackageGraph
                &cFileNames, 
                &rgFileNames, 
                nullptr,    // pcDirectNamespaceChildren
                nullptr);  // rgDirectNamespaceChildren
#ifdef FEATURE_CORECLR
            // For CoreCLR, if the process is not AppX, deliver more appropriate error message
            // when trying to bind to 3rd party WinMDs that is not confusing.
            if (HRESULT_FROM_WIN32(APPMODEL_ERROR_NO_PACKAGE) == hr)
            {
                if (!AppX::IsAppXProcess())
                {
                    IfFailRet(HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED));
                }
            }
#endif

#ifdef FEATURE_COMINTEROP_WINRT_DESKTOP_HOST
            // If we failed to find the requested name, but we have an application local probing path setup, then
            // we can use that to try to find the name now.
            if (hr == RO_E_METADATA_NAME_NOT_FOUND || hr == HRESULT_FROM_WIN32(APPMODEL_ERROR_NO_PACKAGE))
            {
                // We only want to probe the application local path for 3rd party WinMDs as these are the only ones
                // which do not have code sharing enabled.  Although we currently only allow a single alternate probing
                // path per process, shutting this off now will give us easier behavior to support in the future if we
                // do need to enable per-domain local paths.
                if (!IsWindowsNamespace(wszNamespaceRoResolve))
                {
                    HSTRING localWinMDPath = nullptr;
                    {
                        CrstHolder lock(&m_localWinMDPathLock);

                        localWinMDPath = m_localWinMDPath.Get();

                        // If the host has not configured the local winmd path, and we have not yet done any winmd probing
                        // then see if we have config to setup a local winmd path.
                        if (localWinMDPath == nullptr && m_fCanSetLocalWinMDPath)
                        {
                            NewArrayHolder<WCHAR> configWinMDPath(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_LocalWinMDPath));
                            if (!configWinMDPath.IsNull())
                            {
                                m_localWinMDPath.Initialize(configWinMDPath);
                                localWinMDPath = m_localWinMDPath.Get();
                            }
                        }

                        // Do not allow any further setting of the application binding base at this point, since if it
                        // is not currently set, setting it in the future could result in different binding results.
                        m_fCanSetLocalWinMDPath = FALSE;
                    }

                    if (localWinMDPath != nullptr)
                    {
                        hr = RoResolveNamespace(
                            WinRtStringRef(wszNamespaceRoResolve, cchNamespaceRoResolve),
                            wszWinMDPath != nullptr ? (HSTRING)WinRtStringRef(wszWinMDPath, cchWinMDPath) : nullptr, // hsWindowsSdkPath
                            1,        // cPackageGraph
                            &localWinMDPath, // rgPackageGraph
                            &cFileNames,
                            &rgFileNames,
                            nullptr,    // pcDirectNamespaceChildren
                            nullptr);  // rgDirectNamespaceChildren
                    }
                }
            }
#endif // FEATURE_COMINTEROP_WINRT_DESKTOP_HOST

            IfFailRet(hr);
            if (hr != S_OK)
            {   // Not expecting success codes other than S_OK.
                IfFailRet(E_UNEXPECTED);
            }

            hFileNames.Init(rgFileNames, cFileNames);
        
            for (DWORD i = 0; i < hFileNames.GetCount(); i++)
            {
                UINT32  cchFileName = 0;
                LPCWSTR wszFileName = WindowsGetStringRawBuffer(
                    hFileNames.GetAt(i), 
                    &cchFileName);
                
                BOOL fSkipFilename = FALSE;
#ifndef FEATURE_CORECLR            
                // If we have a specified path list. Be certain to only find filenames in that list.
                // NGen for AppX is an exception, where the path list contains the package graph, and we can
                // accept files found elsewhere (e.g., in the Windows WinMD directory).
                // On CoreCLR, we have no concept of an AppX package, so we want the passed in app
                // paths to additively contribute to the set of WinMDs the API can find.
                if (m_rgAltPaths.GetCount() > 0 && !AppX::IsAppXNGen())
                {
                    fSkipFilename = TRUE;
                    for (DWORD iAltPath = 0; iAltPath < m_rgAltPaths.GetCount(); iAltPath++)
                    {
                        UINT32  cchAltPath = 0;
                        LPCWSTR wszAltPath = WindowsGetStringRawBuffer(
                            m_rgAltPaths.GetAt(iAltPath), 
                            &cchAltPath);
                    
                        if (cchAltPath >= cchFileName)
                            continue;
                    
                        if (wcsncmp(wszAltPath, wszFileName, cchAltPath) == 0)
                        {
                            fSkipFilename = FALSE;
                            break;
                        }
                    }
                }
#endif            
                if (!fSkipFilename)
                    hFileNameList.InsertTail(wszFileName);
            }
        }
        else
        {
            // This code is desktop specific. 
            _ASSERTE(m_fNamespaceResolutionKind == NamespaceResolutionKind_DesignerResolveEvent);
            
            EX_TRY
            {
                m_pTypeCache->RaiseDesignerNamespaceResolveEvent(wszNamespace, &hFileNameList);
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
        }
        
#else //CROSSGEN_COMPILE

        DWORD     cFileNames = 0;
        SString * rgFileNames = nullptr;

        hr = Crossgen::CrossgenRoResolveNamespace(
            wszNamespaceRoResolve,
            &cFileNames, 
            &rgFileNames); 

        IfFailRet(hr);

        if (cFileNames > 0)
        {
            _ASSERTE(cFileNames == 1); //only support mapping to one file in coregen
            hFileNameList.InsertTail(rgFileNames->GetUnicode());
            delete rgFileNames;
        }
        
#endif //CROSSGEN_COMPILE
        
        // Add the Namespace -> File name list entry into cache (even if the file name list is empty)
        if (AddFileNameListForNamespace(wszNamespace, hFileNameList.GetValue(), ppFileNameList))
        {   // The file name list was added to the cache - do not delete it
            _ASSERTE(*ppFileNameList == hFileNameList.GetValue());
            (void)hFileNameList.Extract();
        }
    }
    
    return hr;
} // CLRPrivBinderWinRT::GetFileNameListForNamespace

//=====================================================================================================================
// Adds (thread-safe) list of file names to code:m_NamespaceToFileNameListMap for the namespace - returns the cached value.
// Returns TRUE, if pFileNameList was added to the cache and caller should NOT delete it.
// Returns FALSE, if pFileNameList was not added to the cache and caller should delete it.
// 
BOOL 
CLRPrivBinderWinRT::AddFileNameListForNamespace(
    LPCWSTR                           wszNamespace, 
    CLRPrivBinderUtil::WStringList *  pFileNameList, 
    CLRPrivBinderUtil::WStringList ** ppFileNameList)
{
    STANDARD_VM_CONTRACT;
    
    NewArrayHolder<WCHAR> wszEntryNamespace = DuplicateStringThrowing(wszNamespace);
    
    NamespaceToFileNameListMapEntry entry;
    entry.m_wszNamespace = wszEntryNamespace.GetValue();
    entry.m_pFileNameList = pFileNameList;
    
    // We have to serialize all Add operations
    CrstHolder lock(&m_MapsAddLock);

    // Wrapper for m_NamespaceToFileNameListMap.Add that avoids call out into host
    NamespaceToFileNameListMap::AddPhases addCall;
    
    // Status if the element was added to the hash table or not
    BOOL fAddedToCache = FALSE;
    
    // 1. Preallocate one element
    addCall.PreallocateForAdd(&m_NamespaceToFileNameListMap);
    {
        // 2. Take the reader lock which can be taken during stack walking
        // We cannot call out into host from ForbidSuspend region (i.e. no allocations/deallocations)
        ForbidSuspendThreadHolder suspend;
        {
            CrstHolder lock(&m_MapsLock);

            const NamespaceToFileNameListMapEntry * pEntry = m_NamespaceToFileNameListMap.LookupPtr(wszNamespace);
            if (pEntry == nullptr)
            {
                // 3a. Add the element to the hash table (no call out into host)
                addCall.Add(entry);
            
                // These values are now owned by the hash table element
                wszEntryNamespace.SuppressRelease();
                *ppFileNameList = pFileNameList;
                fAddedToCache = TRUE;
            }
            else
            {   // Another thread beat us adding this entry to the hash table
                *ppFileNameList = pEntry->m_pFileNameList;
                
                // 3b. Use the newly allocated table (if any) to avoid allocation in the next call (no call out into host)
                addCall.AddNothing_PublishPreallocatedTable();
                _ASSERTE(fAddedToCache == FALSE);
            }
        }
    }
    // 4. Cleanup the old memory (if any), also called from the destructor of addCall
    addCall.DeleteOldTable();
    
    return fAddedToCache;
} // CLRPrivBinderWinRT::AddFileNameListForNamespace

#endif //!DACCESS_COMPILE

//=====================================================================================================================
// Finds assembly with WinRT type if it is already loaded.
// 
PTR_Assembly 
CLRPrivBinderWinRT::FindAssemblyForTypeIfLoaded(
    PTR_AppDomain pAppDomain, 
    LPCUTF8       szNamespace, 
    LPCUTF8       szClassName)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END
    
    WCHAR wszNamespace[MAX_CLASSNAME_LENGTH];
    int cchNamespace = WszMultiByteToWideChar(CP_UTF8, 0, szNamespace, -1, wszNamespace, _countof(wszNamespace));
    if (cchNamespace == 0)
    {
        return NULL;
    }
    
    CLRPrivBinderUtil::WStringListElem * pFileNameElem= nullptr; 
    const NamespaceToFileNameListMapEntry * pNamespaceEntry;
    {
        ForbidSuspendThreadHolder suspend;
        {
            CrstHolder lock(&m_MapsLock);

            pNamespaceEntry = m_NamespaceToFileNameListMap.LookupPtr(wszNamespace);
            if ((pNamespaceEntry == nullptr) || (pNamespaceEntry->m_pFileNameList == nullptr))
            {
               return NULL;
            }

            pFileNameElem = pNamespaceEntry->m_pFileNameList->GetHead();
        }
    }
    
    while (pFileNameElem != nullptr)
    {
        const WCHAR * wszFileName = pFileNameElem->GetValue();
        PTR_CLRPrivAssemblyWinRT pPrivAssembly=NULL;
        const FileNameToAssemblyWinRTMapEntry * pFileNameEntry;
        {
            ForbidSuspendThreadHolder suspend;
            {
                CrstHolder lock(&m_MapsLock);
                
                pFileNameEntry = m_FileNameToAssemblyMap.LookupPtr(wszFileName);
                if (pFileNameEntry == nullptr || pFileNameEntry->m_pAssembly == nullptr)
                {
                    return NULL;
                }

                pPrivAssembly = pFileNameEntry->m_pAssembly;
            }
        }

        if (pPrivAssembly == NULL)
        {
            return NULL;
        }
        
        _ASSERT(((void *)(CLRPrivAssemblyWinRT *)0x100) == 
                ((void *)(ICLRPrivAssembly *)(CLRPrivAssemblyWinRT *)0x100));
        
        PTR_Assembly pAssembly = NULL;
        HRESULT hr = m_pTypeCache->ContainsTypeIfLoaded(
            pAppDomain, 
            dac_cast<PTR_ICLRPrivAssembly>(pPrivAssembly), 
            szNamespace, 
            szClassName, 
            &pAssembly);
        if (hr == S_OK)
        {   // The type we are looking for has been found in this assembly
            _ASSERTE(pAssembly != nullptr);
            return pAssembly;
        }
        if (FAILED(hr))
        {   // Assembly was not loaded
            return NULL;
        }
        // Type was not found in the assembly
        _ASSERTE(hr == S_FALSE);
        
        // Try next file name for this namespace
        pFileNameElem = CLRPrivBinderUtil::WStringList::GetNext(pFileNameElem);
    }
    
    return NULL;
} // CLRPrivBinderWinRT::FindAssemblyForTypeIfLoaded

#ifndef DACCESS_COMPILE

#ifdef FEATURE_FUSION
//=====================================================================================================================
// Implements interface method code:IBindContext::PreBind.
// 
// Prebinding to WinMD files follows a special contract. We want to avoid loading the actual target assembly
// and we need to validate that all dependencies of the file remain equivalent to that which was available at ngen time
// We do this by comparing the filename to the assembly simple name.  This is legal to do with WinRT because at NGen time
// we embed a WinRT dependency as assembly def name component plus a namespace and type from it.
// At bind time, this type should still exist in the same assembly.  If it doesn't, and has been moved,
// the native image validation will fail anyway and we'll fall back to IL.  This is because if the type has 
// been moved to another WinMD, it must have been removed from the first one because WinRT allows no duplicates.
// This no duplicate rule is obviously not actually gauranteed by the WinRT runtime for 3rd party assemblies,
// but violating the rule is known to cause a number of binding behavior errors that we do not attempt to protect against.
HRESULT 
CLRPrivBinderWinRT::PreBind(
    IAssemblyName * pAssemblyName, 
    DWORD           dwPreBindFlags, 
    IBindResult **  ppIBindResult)
{
    STANDARD_VM_CONTRACT;
    HRESULT hr  = S_OK;
    
    // Assert that we are only working with a binder that supports native images
    _ASSERTE(m_fCanUseNativeImages);
    
    ReleaseHolder<IBindContext> pIBindContext;
    IfFailRet(GetParentIBindContext(&pIBindContext));
    
    DWORD dwContentType = AssemblyContentType_Default;
    DWORD cbContentTypeSize = sizeof(dwContentType);
    IfFailRet(pAssemblyName->GetProperty(ASM_NAME_CONTENT_TYPE, &dwContentType, &cbContentTypeSize));

    if (dwContentType == AssemblyContentType_Default)
    {
        hr = pIBindContext->PreBind(pAssemblyName, dwPreBindFlags, ppIBindResult);
    }
    else
    {
        hr = BindWinRTAssemblyByName(pAssemblyName, ppIBindResult, TRUE);
    }
    
    return hr;
}

//=====================================================================================================================
// Implements interface method code:IBindContext::IsDefaultContext.
// 
HRESULT 
CLRPrivBinderWinRT::IsDefaultContext()
{
    LIMITED_METHOD_CONTRACT;
    return S_OK;
}
#endif

//=====================================================================================================================
CLRPrivAssemblyWinRT::CLRPrivAssemblyWinRT(
    CLRPrivBinderWinRT *      pBinder, 
    CLRPrivResourcePathImpl * pResourceIL, 
    IBindResult *             pIBindResult,
    BOOL                      fShareable)
    : m_pBinder(nullptr),
      m_pResourceIL(nullptr),
      m_pIResourceNI(nullptr),
      m_pIBindResult(nullptr),
      m_fShareable(fShareable),
      m_dwImageTypes(0)
{
    STANDARD_VM_CONTRACT;
    VALIDATE_ARG_THROW((pBinder != nullptr) && (pResourceIL != nullptr) && (pIBindResult != nullptr));

    m_pBinder = clr::SafeAddRef(pBinder);
    m_pResourceIL = clr::SafeAddRef(pResourceIL);
    m_pIBindResult = clr::SafeAddRef(pIBindResult);
}

//=====================================================================================================================
CLRPrivAssemblyWinRT::~CLRPrivAssemblyWinRT()
{
    LIMITED_METHOD_CONTRACT;
    clr::SafeRelease(m_pIResourceNI);
}

//=====================================================================================================================
// Implements interface method code:IUnknown::Release.
// Overridden to implement self-removal from assembly map code:CLRPrivBinderWinRT::m_FileNameToAssemblyMap.
// 
ULONG CLRPrivAssemblyWinRT::Release()
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_CAN_TAKE_LOCK;
    _ASSERTE(m_cRef > 0);
    
    ULONG cRef;
    
    {
        // To achieve proper lifetime semantics, the name to assembly map elements' CLRPrivAssemblyWinRT 
        // instances are not ref counted. We cannot allow discovery of the object via m_FileNameToAssemblyMap 
        // when the ref count is 0 (to prevent another thread to AddRef and Release it back to 0 in parallel).
        // All uses of the map are guarded by the map lock, so we have to decrease the ref count under that 
        // lock (to avoid the chance that 2 threads are running Release to ref count 0 at once).
        ForbidSuspendThreadHolder suspend;
        {
            CrstHolder lock(&m_pBinder->m_MapsLock);
            cRef = InterlockedDecrement(&m_cRef);
            if (cRef == 0)
            {
                m_pBinder->RemoveFileNameToAssemblyMapping(m_pResourceIL->GetPath());
            }
        }
    }
    
    // Note: We cannot deallocate memory in the ForbidSuspendThread region
    if (cRef == 0)
    {
        delete this;
    }
    
    return cRef;
} // CLRPrivAssemblyWinRT::Release

//=====================================================================================================================
// Implements interface method code:ICLRPrivAssembly::IsShareable.
// 
HRESULT CLRPrivAssemblyWinRT::IsShareable(
    BOOL * pbIsShareable)
{
    LIMITED_METHOD_CONTRACT;

    VALIDATE_ARG_RET(pbIsShareable != nullptr);

    *pbIsShareable = m_fShareable;
    return S_OK;
}

//=====================================================================================================================
// Implements interface method code:ICLRPrivAssembly::GetAvailableImageTypes.
// 
HRESULT CLRPrivAssemblyWinRT::GetAvailableImageTypes(
    LPDWORD pdwImageTypes)
{
    STANDARD_BIND_CONTRACT;

    HRESULT hr = S_OK;

    VALIDATE_ARG_RET(pdwImageTypes != nullptr);

    EX_TRY
    {
        IfFailGo(EnsureAvailableImageTypes());
    
        *pdwImageTypes = m_dwImageTypes;
        hr = S_OK;
    ErrExit:
        ;
    }
    EX_CATCH_HRESULT(hr);
    
    return hr;
}

#ifdef FEATURE_FUSION
static ICLRPrivResource * GetResourceForBindResult(
    IBindResult * pIBindResult)
{
    STANDARD_VM_CONTRACT;
    VALIDATE_ARG_THROW(pIBindResult != nullptr);

    WCHAR wzPath[_MAX_PATH];
    DWORD cchPath = NumItems(wzPath);
    ReleaseHolder<IAssemblyLocation> pIAssemLoc;
    IfFailThrow(pIBindResult->GetAssemblyLocation(&pIAssemLoc));
    IfFailThrow(pIAssemLoc->GetPath(wzPath, &cchPath));
    return ToInterface<ICLRPrivResource>(new CLRPrivResourcePathImpl(wzPath));
}
#endif

//=====================================================================================================================
// Implements interface method code:ICLRPrivAssembly::GetImageResource.
// 
HRESULT CLRPrivAssemblyWinRT::GetImageResource(
    DWORD               dwImageType, 
    DWORD *             pdwImageType, 
    ICLRPrivResource ** ppIResource)
{
    STANDARD_BIND_CONTRACT;
    HRESULT hr = S_OK;
    
    VALIDATE_ARG_RET((ppIResource != nullptr) && (m_pIBindResult != nullptr));
    
    EX_TRY
    {
        IfFailGo(EnsureAvailableImageTypes());

        DWORD _dwImageType;
        if (pdwImageType == nullptr)
        {
            pdwImageType = &_dwImageType;
        }
        
        if ((dwImageType & ASSEMBLY_IMAGE_TYPE_NATIVE) == ASSEMBLY_IMAGE_TYPE_NATIVE)
        {
            if (m_pIResourceNI == nullptr)
            {
                IfFailGo(CLR_E_BIND_IMAGE_UNAVAILABLE);
            }

            *ppIResource = clr::SafeAddRef(m_pIResourceNI);
            *pdwImageType = ASSEMBLY_IMAGE_TYPE_NATIVE;
        }
        else if ((dwImageType & ASSEMBLY_IMAGE_TYPE_IL) == ASSEMBLY_IMAGE_TYPE_IL)
        {
            *ppIResource = clr::SafeAddRef(m_pResourceIL);
            *pdwImageType = ASSEMBLY_IMAGE_TYPE_IL;
        }
        else
        {
            hr = CLR_E_BIND_IMAGE_UNAVAILABLE;
        }
    ErrExit:
        ;
    }
    EX_CATCH_HRESULT(hr);
    
    return hr;
}

//=====================================================================================================================
// Implements interface method code:ICLRPrivBinder::VerifyBind.
// 
HRESULT CLRPrivBinderWinRT::VerifyBind(
    IAssemblyName *        pAssemblyName, 
    ICLRPrivAssembly *     pAssembly, 
    ICLRPrivAssemblyInfo * pAssemblyInfo)
{
    STANDARD_BIND_CONTRACT;
    HRESULT hr = S_OK;

    VALIDATE_ARG_RET(pAssemblyInfo != nullptr);
    
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
HRESULT CLRPrivBinderWinRT::GetBinderID(
    UINT_PTR * pBinderId)
{
    LIMITED_METHOD_CONTRACT;

    *pBinderId = reinterpret_cast<UINT_PTR>(this);
    return S_OK;
}

#if defined(FEATURE_CORECLR) || defined(CROSSGEN_COMPILE)
//=====================================================================================================================
HRESULT CLRPrivBinderWinRT::FindWinRTAssemblyBySpec(
    LPVOID pvAppDomain,
    LPVOID pvAssemblySpec,
    HRESULT * pResult,
    ICLRPrivAssembly ** ppAssembly)
{
    STATIC_CONTRACT_WRAPPER;
    return E_FAIL; 
}
#endif

#ifdef FEATURE_FUSION
//=====================================================================================================================
HRESULT CLRPrivBinderWinRT::FindWinRTAssemblyBySpec(
    LPVOID pvAppDomain,
    LPVOID pvAssemblySpec,
    HRESULT * pResult,
    ICLRPrivAssembly ** ppAssembly)
{
    LIMITED_METHOD_CONTRACT;;
    HRESULT hr = S_OK;

    AppDomain* pAppDomain = reinterpret_cast<AppDomain*>(pvAppDomain);
    AssemblySpec* pAssemblySpec = reinterpret_cast<AssemblySpec*>(pvAssemblySpec);
    VALIDATE_PTR_RET(pAppDomain);
    VALIDATE_PTR_RET(pAssemblySpec);
    VALIDATE_PTR_RET(pResult);
    VALIDATE_PTR_RET(ppAssembly);

    if (pAssemblySpec->IsContentType_WindowsRuntime())
    {
        // FindAssemblyBySpec is not supported by this binder.
        *pResult = CLR_E_BIND_TYPE_NOT_FOUND;
        *ppAssembly = nullptr;
        return S_OK;
    }
    else
    {
        return CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT;
    }
}

//=====================================================================================================================
HRESULT CLRPrivBinderWinRT::GetParentIBindContext(
    IBindContext **ppIBindContext)
{
    STANDARD_BIND_CONTRACT;
    VALIDATE_ARG_RET(ppIBindContext != nullptr);

    HRESULT hr = S_OK;

    if (m_pParentBinder != nullptr)
    {
        _ASSERTE(AppX::IsAppXProcess());
        IfFailRet(m_pParentBinder->QueryInterface(__uuidof(IBindContext), (void**)ppIBindContext));
    }
    else
    {
        _ASSERTE(!AppX::IsAppXProcess());
        EX_TRY
        {
            AppDomain* pDomain = AppDomain::GetCurrentDomain();
            hr = GetBindContextFromApplicationContext(pDomain->CreateFusionContext(), ppIBindContext);
        }
        EX_CATCH_HRESULT(hr);
    }

    _ASSERTE(FAILED(hr) || *ppIBindContext != nullptr);
    return hr;
}
#endif

//=====================================================================================================================
HRESULT CLRPrivAssemblyWinRT::GetIBindResult(
    IBindResult ** ppIBindResult)
{
    LIMITED_METHOD_CONTRACT;
    
    VALIDATE_ARG_RET(ppIBindResult != nullptr);
    VALIDATE_CONDITION((m_pIBindResult != nullptr), return E_UNEXPECTED);
    
    *ppIBindResult = clr::SafeAddRef(m_pIBindResult);
    
    return S_OK;
}

//=====================================================================================================================
HRESULT CLRPrivAssemblyWinRT::EnsureAvailableImageTypes()
{
    STANDARD_VM_CONTRACT;
    HRESULT hr = S_OK;

    DWORD dwImageTypesLocal = m_dwImageTypes;

    // If image types has not yet been set, attempt to bind to native assembly
    if (dwImageTypesLocal == 0)
    {
#ifdef FEATURE_FUSION
        CLRPrivBinderWinRT *pBinder = m_pBinder;
        IfFailGo(pBinder->BindAssemblyToNativeAssembly(this));
#endif
        if (m_pIResourceNI == nullptr)
        {
#ifdef FEATURE_FUSION
            ReleaseHolder<IBindResult> pIBindResultNI;

            if (SUCCEEDED(hr = m_pIBindResult->GetNativeImage(&pIBindResultNI, nullptr)) && pIBindResultNI != nullptr)
            {
                ReleaseHolder<ICLRPrivResource> pResourceNI = GetResourceForBindResult(pIBindResultNI);
                if (InterlockedCompareExchangeT<ICLRPrivResource *>(&m_pIResourceNI, pResourceNI, nullptr) == nullptr)
                    pResourceNI.SuppressRelease();
            }
#else
            if (m_pIBindResult->HasNativeImage())
            {
                SString sPath = m_pIBindResult->GetNativeImage()->GetPath();
                m_pIResourceNI = new CLRPrivResourcePathImpl(sPath.GetUnicode());
                m_pIResourceNI->AddRef();
            }
#endif
            IfFailGo(hr);
        }

        DWORD dwImageTypes = 0;

        if (m_pResourceIL != nullptr)
            dwImageTypes |= ASSEMBLY_IMAGE_TYPE_IL;

        if (m_pIResourceNI != nullptr)
            dwImageTypes |= ASSEMBLY_IMAGE_TYPE_NATIVE;

        m_dwImageTypes = dwImageTypes;
    }
ErrExit:

    return hr;
}

//=====================================================================================================================
//static
HRESULT CLRPrivAssemblyWinRT::GetIBindResult(
    ICLRPrivAssembly * pPrivAssembly, 
    IBindResult **     ppIBindResult)
{
    LIMITED_METHOD_CONTRACT;
    
    HRESULT hr;
    
    VALIDATE_ARG_RET(pPrivAssembly != nullptr);
    
    ReleaseHolder<ICLRPrivAssemblyID_WinRT> pAssemblyID;
    IfFailRet(pPrivAssembly->QueryInterface(__uuidof(ICLRPrivAssemblyID_WinRT), (LPVOID *)&pAssemblyID));
    // QI succeeded, we can cast up:
    CLRPrivAssemblyWinRT * pPrivAssemblyWinRT = static_cast<CLRPrivAssemblyWinRT *>(pPrivAssembly);
    
    return pPrivAssemblyWinRT->GetIBindResult(ppIBindResult);
}

#endif //!DACCESS_COMPILE
