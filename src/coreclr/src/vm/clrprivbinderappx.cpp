// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#include "common.h" // precompiled header
#include "assemblyusagelogmanager.h"

//=====================================================================================================================
#include "clrprivbinderappx.h"
//CLRPrivBinderAppX * CLRPrivBinderAppX::s_pSingleton = nullptr;
SPTR_IMPL_INIT(CLRPrivBinderAppX, CLRPrivBinderAppX, s_pSingleton, nullptr);

#ifndef DACCESS_COMPILE
//=====================================================================================================================
#include "appxutil.h"
#include "clrprivbinderutil.h"
#include "fusionlogging.h"
#include "clrprivtypecachewinrt.h"
#include "fusionp.h"

using namespace CLRPrivBinderUtil;

//=====================================================================================================================
CLRPrivBinderAppX::CLRPrivBinderAppX(LPCWSTR * rgwzAltPath, UINT cAltPaths)
    : m_MapReadLock(CrstCLRPrivBinderMaps,
                static_cast<CrstFlags>(CRST_DEBUG_ONLY_CHECK_FORBID_SUSPEND_THREAD |
                                       CRST_GC_NOTRIGGER_WHEN_TAKEN |
                                       CRST_DEBUGGER_THREAD |
                                       // FindAssemblyBySpec complicates matters, which needs to take the m_MapReadLock.
                                       // But FindAssemblyBySpec cannot switch to preemptive mode, as that would trigger
                                       // a GC. Since this is a leaf lock, and since it does not make any calls out of
                                       // the runtime, this lock can be taken in cooperative mode if the locked scope is
                                       // also marked as ForbidSuspend (since FindAssemblyBySpec can be called by
                                       // the debugger and the profiler). TODO: it would be nice to be able to specify
                                       // this flag for just the specific places where it is necessary rather than for
                                       // the lock as a whole.
                                       CRST_UNSAFE_ANYMODE)),
      m_MapWriteLock(CrstCLRPrivBinderMapsAdd, CRST_DEFAULT),
      m_cAltPaths(cAltPaths),
      m_fCanUseNativeImages(TRUE),
      m_pParentBinder(nullptr),
      m_pFusionBinder(nullptr),
      m_pWinRTBinder(nullptr),

      // Note: the first CLRPrivBinderAppX object is created prior to runtime startup, so this code cannot call
      // AppX::IsAppXDesignMode; however, FindAssemblyBySpec cannot call IsAppXDesignMode either because that would
      // cause a GC_TRIGGERS violation for the GetAssemblyIfLoaded scenario. However this doesn't matter because
      // the assembly map will be empty until at least the first call to BindAssemblyByName is made, at which point
      // a call to IsAppXDesignMode can be made. Thus, we default to the most conversative setting and overwrite this
      // value in BindAssemblyByName.
      m_fusionBindingScope(CLRPrivBinderFusion::kBindingScope_FrameworkSubset)
{
    STANDARD_VM_CONTRACT;

    // Copy altpaths
    if (cAltPaths > 0)
    {
        m_rgAltPathsHolder = new NewArrayHolder<WCHAR>[cAltPaths];
        m_rgAltPaths = new WCHAR *[cAltPaths];

        for (UINT iAltPath = 0; iAltPath < cAltPaths; iAltPath++)
        {
            size_t cchAltPath = wcslen(rgwzAltPath[iAltPath]);
            m_rgAltPathsHolder[iAltPath] = m_rgAltPaths[iAltPath] = new WCHAR[cchAltPath + 1];
            wcscpy_s(m_rgAltPaths[iAltPath], cchAltPath + 1, rgwzAltPath[iAltPath]);
        }
    }

#ifdef FEATURE_FUSION
    IfFailThrow(RuntimeCreateCachingILFingerprintFactory(&m_pFingerprintFactory));
#endif
}

//=====================================================================================================================
CLRPrivBinderFusion::BindingScope CLRPrivBinderAppX::GetFusionBindingScope()
{
    WRAPPER_NO_CONTRACT;

    m_fusionBindingScope = (AppX::IsAppXDesignMode() || (m_pParentBinder != nullptr))
                           ? CLRPrivBinderFusion::kBindingScope_FrameworkAll
                           : CLRPrivBinderFusion::kBindingScope_FrameworkSubset;

    return m_fusionBindingScope;
}

//=====================================================================================================================
CLRPrivBinderAppX::~CLRPrivBinderAppX()
{
    WRAPPER_NO_CONTRACT;
    
    m_NameToAssemblyMap.RemoveAll();
    AssemblyUsageLogManager::UnRegisterBinderFromUsageLog((UINT_PTR)this);
    
    clr::SafeRelease(m_pWinRTBinder);
}

//=====================================================================================================================
CLRPrivBinderAppX * 
CLRPrivBinderAppX::GetOrCreateBinder()
{
    STANDARD_VM_CONTRACT;
    HRESULT hr = S_OK;

    if (s_pSingleton == nullptr)
    {
        ReleaseHolder<IAssemblyUsageLog> pNewUsageLog;
        IfFailThrow(AssemblyUsageLogManager::GetUsageLogForContext(W("App"), AppX::GetHeadPackageMoniker(), &pNewUsageLog));

        ReleaseHolder<CLRPrivBinderAppX> pBinder;
        pBinder = clr::SafeAddRef(new CLRPrivBinderAppX(nullptr, 0));
        
        pBinder->m_pFusionBinder = clr::SafeAddRef(new CLRPrivBinderFusion());
        
        CLRPrivTypeCacheWinRT * pWinRtTypeCache = CLRPrivTypeCacheWinRT::GetOrCreateTypeCache();
        pBinder->m_pWinRTBinder = clr::SafeAddRef(new CLRPrivBinderWinRT(
            pBinder, 
            pWinRtTypeCache, 
            nullptr,    // rgwzAltPath
            0,          // cAltPaths
            CLRPrivBinderWinRT::NamespaceResolutionKind_WindowsAPI, 
            TRUE // fCanUseNativeImages
            ));
        
        if (InterlockedCompareExchangeT<decltype(s_pSingleton)>(&s_pSingleton, pBinder, nullptr) == nullptr)
            pBinder.SuppressRelease();

        // Register binder with usagelog infrastructure.
        UINT_PTR binderId;
        IfFailThrow(pBinder->GetBinderID(&binderId));
        IfFailThrow(AssemblyUsageLogManager::RegisterBinderWithUsageLog(binderId, pNewUsageLog));
        
        // Create and register WinRT usage log
        ReleaseHolder<IAssemblyUsageLog> pNewWinRTUsageLog;
        IfFailThrow(AssemblyUsageLogManager::GetUsageLogForContext(W("WinRT"), AppX::GetHeadPackageMoniker(), &pNewWinRTUsageLog));

        UINT_PTR winRTBinderId;
        IfFailThrow(pBinder->m_pWinRTBinder->GetBinderID(&winRTBinderId));
        IfFailThrow(AssemblyUsageLogManager::RegisterBinderWithUsageLog(winRTBinderId, pNewWinRTUsageLog));
    }

    return s_pSingleton;
}

//=====================================================================================================================
// Used only for designer binding context
CLRPrivBinderAppX * CLRPrivBinderAppX::CreateParentedBinder(
    ICLRPrivBinder *         pParentBinder, 
    CLRPrivTypeCacheWinRT *  pWinRtTypeCache,
    LPCWSTR *                rgwzAltPath, 
    UINT                     cAltPaths,
    BOOL                     fCanUseNativeImages)
{
    STANDARD_VM_CONTRACT;
    HRESULT hr = S_OK;

    ReleaseHolder<CLRPrivBinderAppX> pBinder;
    pBinder = clr::SafeAddRef(new CLRPrivBinderAppX(rgwzAltPath, cAltPaths));

    pBinder->m_pParentBinder = clr::SafeAddRef(pParentBinder);
    pBinder->m_fCanUseNativeImages = fCanUseNativeImages;
    
    // We want to share FusionBinder with pParentBinder (which bubbles up through the chain of binders to the global AppXBinder code:s_pSingleton)
    // Ideally we would get the FusionBinder from pParentBinder (via casting to a new interface). It is much easier just to fetch it from 
    // the global AppX binder directly
    pBinder->m_pFusionBinder = clr::SafeAddRef(s_pSingleton->GetFusionBinder());
    
    if (cAltPaths > 0)
    {
        pBinder->m_pWinRTBinder = clr::SafeAddRef(new CLRPrivBinderWinRT(
            pBinder, 
            pWinRtTypeCache, 
            rgwzAltPath, 
            cAltPaths, 
            CLRPrivBinderWinRT::NamespaceResolutionKind_WindowsAPI,
            fCanUseNativeImages));
    }

    pBinder.SuppressRelease();
    return pBinder;
}

//=====================================================================================================================
HRESULT CLRPrivBinderAppX::BindAppXAssemblyByNameWorker(
    IAssemblyName * pIAssemblyName,
    DWORD dwAppXBindFlags,
    CLRPrivAssemblyAppX ** ppAssembly)
{
    STANDARD_VM_CONTRACT;
    HRESULT hr = S_OK;

    fusion::logging::StatusScope logStatus(0, ID_FUSLOG_BINDING_STATUS_IMMERSIVE, &hr);

    VALIDATE_ARG_RET(pIAssemblyName != nullptr);
    VALIDATE_ARG_RET((dwAppXBindFlags & ABF_BindIL) == ABF_BindIL);
    VALIDATE_ARG_RET(ppAssembly != nullptr);

    DWORD dwContentType = AssemblyContentType_Default;
    IfFailRet(hr = fusion::util::GetProperty(pIAssemblyName, ASM_NAME_CONTENT_TYPE, &dwContentType));
    if ((hr == S_OK) && (dwContentType != AssemblyContentType_Default))
    {
        IfFailRet(CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT);
    }

    ReleaseHolder<CLRPrivAssemblyAppX> pAssembly;

    // Get the simple name.
    WCHAR wzSimpleName[_MAX_PATH];
    DWORD cchSimpleName = _MAX_PATH;
    IfFailRet(pIAssemblyName->GetName(&cchSimpleName, wzSimpleName));

    {   // Look for previous successful bind. Host callouts are now forbidden.
        ForbidSuspendThreadCrstHolder lock(&m_MapReadLock);
        pAssembly = clr::SafeAddRef(m_NameToAssemblyMap.Lookup(wzSimpleName));
    }

    if (pAssembly == nullptr)
    {
        ReleaseHolder<ICLRPrivResource> pResourceIL;
        ReleaseHolder<ICLRPrivResource> pResourceNI;

        // Create assembly identity using the simple name. For successful binds this will be updated
        // with the full assembly identity in the VerifyBind callback.
        NewHolder<AssemblyIdentity> pIdentity = new AssemblyIdentity();
        IfFailRet(pIdentity->Initialize(wzSimpleName));

        //
        // Check the head package first to see if this matches an EXE, then check
        // all packages to see if this matches a DLL.
        //
        WCHAR wzFilePath[_MAX_PATH];
        {
            hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);

            if (FAILED(hr))
            {
                // Create simple name with .EXE extension
                WCHAR wzSimpleFileName[_MAX_PATH];
                wcscpy_s(wzSimpleFileName, NumItems(wzSimpleFileName), wzSimpleName);
                wcscat_s(wzSimpleFileName, NumItems(wzSimpleFileName), W(".EXE"));

                // Search for the file using AppX::FileFileInCurrentPackage helper.
                UINT32 cchFilePath = NumItems(wzFilePath);
                hr = AppX::FindFileInCurrentPackage(
                        wzSimpleFileName,
                        &cchFilePath,
                        wzFilePath,
                        PACKAGE_FILTER_CLR_DEFAULT,
                        (PCWSTR *)(void *)m_rgAltPaths,
                        m_cAltPaths,
                        m_pParentBinder != NULL ? AppX::FindFindInPackageFlags_SkipCurrentPackageGraph : AppX::FindFindInPackageFlags_None);
            }

            if (FAILED(hr))
            {
                // Create simple name with .DLL extension
                WCHAR wzSimpleFileName[_MAX_PATH];
                wcscpy_s(wzSimpleFileName, NumItems(wzSimpleFileName), wzSimpleName);
                wcscat_s(wzSimpleFileName, NumItems(wzSimpleFileName), W(".DLL"));

                // Search for the file using AppX::FileFileInCurrentPackage helper
                UINT32 cchFilePath = NumItems(wzFilePath);
                hr = AppX::FindFileInCurrentPackage(
                        wzSimpleFileName,
                        &cchFilePath,
                        wzFilePath,
                        PACKAGE_FILTER_CLR_DEFAULT,
                        (PCWSTR *)(void *)m_rgAltPaths,
                        m_cAltPaths,
                        m_pParentBinder != NULL ? AppX::FindFindInPackageFlags_SkipCurrentPackageGraph : AppX::FindFindInPackageFlags_None);
            }

            if (SUCCEEDED(hr))
            {
                fusion::logging::LogMessage(0, ID_FUSLOG_BINDING_STATUS_FOUND, wzFilePath);
            }
            else
            {
                // Cache the bind failure result before returning. Careful not to overwrite the bind result with the cache insertion result.
                HRESULT hrResult = hr;
                IfFailRet(CacheBindResult(pIdentity, hr));
                if (hr == S_OK)
                {   // Cache now owns identity object lifetime.
                    pIdentity.SuppressRelease();
                }
                hr = hrResult;
            }
            IfFailRet(hr);
        }

        NewHolder<CLRPrivResourcePathImpl> pResourcePath = new CLRPrivResourcePathImpl(wzFilePath);
        IfFailRet(pResourcePath->QueryInterface(__uuidof(ICLRPrivResource), (LPVOID*)&pResourceIL));
        pResourcePath.SuppressRelease();

        // Create an IBindResult and provide it to the new CLRPrivAssemblyAppX object.
        ReleaseHolder<IBindResult> pIBindResult = ToInterface<IBindResult>(
            new CLRPrivAssemblyBindResultWrapper(pIAssemblyName, wzFilePath, m_pFingerprintFactory));


        // Create the new CLRPrivAssemblyAppX object.
        NewHolder<CLRPrivAssemblyAppX> pAssemblyObj =
            new CLRPrivAssemblyAppX(pIdentity, this, pResourceIL, pIBindResult);

        //
        // Check cache. If someone beat us then use it instead; otherwise add new ICLRPrivAssembly.
        //
        do
        {
            // Because the read lock must be taken within a ForbidSuspend region, use AddInPhases.
            if (m_NameToAssemblyMap.CheckAddInPhases<ForbidSuspendThreadCrstHolder, CrstHolder>(
                    pAssemblyObj, m_MapReadLock, m_MapWriteLock, pAssemblyObj.GetValue()))
            {
                {   // Careful not to allow the cache insertion result to overwrite the bind result.
                    HRESULT hrResult = hr;
                    IfFailRet(CacheBindResult(pIdentity, hr));
                    if (hr == S_OK)
                    {   // Cache now owns identity object lifetime, but ~CLRPrivBinderAssembly
                        // can also remove the identity from the cache prior to cache deletion.
                        pIdentity.SuppressRelease();
                    }
                    hr = hrResult;
                }
                
                pAssembly = pAssemblyObj.Extract();
            }
            else
            {
                ForbidSuspendThreadCrstHolder lock(&m_MapReadLock);
                pAssembly = clr::SafeAddRef(m_NameToAssemblyMap.Lookup(wzSimpleName));
            }
        }
        while (pAssembly == nullptr); // Keep looping until we find the existing one, or add a new one
    }

    _ASSERTE(pAssembly != nullptr);

    if (((dwAppXBindFlags & ABF_BindNI) == ABF_BindNI) && 
        m_fCanUseNativeImages)
    {
        //
        // Look to see if there's a native image available.
        //

        // Fire BindingNgenPhaseStart ETW event if enabled.
        {
            InlineSString<128> ssAssemblyName;
            FireEtwBindingNgenPhaseStart(
                (AppDomain::GetCurrentDomain()->GetId().m_dwId),
                LOADCTX_TYPE_HOSTED,
                ETWFieldUnused,
                ETWLoaderLoadTypeNotAvailable,
                NULL,
                FusionBind::GetAssemblyNameDisplayName(pIAssemblyName, ssAssemblyName, ASM_DISPLAYF_FULL).GetUnicode(),
                GetClrInstanceId());
        }

        ReleaseHolder<IBindResult> pIBindResultIL;
        IfFailRet(pAssembly->GetIBindResult(&pIBindResultIL));
        _ASSERTE(pIBindResultIL != nullptr);

        NewArrayHolder<WCHAR> wzZapSet = DuplicateStringThrowing(g_pConfig->ZapSet());
        NativeConfigData cfgData = {
            wzZapSet,
            PEFile::GetNativeImageConfigFlags()
        };

        IfFailRet(BindToNativeAssembly(
            pIBindResultIL, &cfgData, static_cast<IBindContext*>(this), fusion::logging::GetCurrentFusionBindLog()));

        // Ensure that the native image found above in BindToNativeAssembly is reported as existing in the CLRPrivAssembly object
        if (hr == S_OK)
        {
            ReleaseHolder<ICLRPrivResource> pNIImageResource;
            // This will make GetAvailableImageTypes return that a native image exists.
            IfFailRet(pAssembly->GetImageResource(ASSEMBLY_IMAGE_TYPE_NATIVE, NULL, &pNIImageResource));
#ifdef _DEBUG
            DWORD dwImageTypes;

            _ASSERTE(SUCCEEDED(pAssembly->GetAvailableImageTypes(&dwImageTypes)));
            _ASSERTE((dwImageTypes & ASSEMBLY_IMAGE_TYPE_NATIVE) == ASSEMBLY_IMAGE_TYPE_NATIVE);
#endif
        }

        // Fire BindingNgenPhaseEnd ETW event if enabled.
        {
            InlineSString<128> ssAssemblyName;
            FireEtwBindingNgenPhaseEnd(
                (AppDomain::GetCurrentDomain()->GetId().m_dwId),
                LOADCTX_TYPE_HOSTED,
                ETWFieldUnused,
                ETWLoaderLoadTypeNotAvailable,
                NULL,
                FusionBind::GetAssemblyNameDisplayName(pIAssemblyName, ssAssemblyName, ASM_DISPLAYF_FULL).GetUnicode(),
                GetClrInstanceId());
        }

        // BindToNativeAssembly can return S_FALSE, but this could be misleading.
        if (hr == S_FALSE)
            hr = S_OK;
    }

    if (SUCCEEDED(hr))
    {
        *ppAssembly = pAssembly.Extract();
    }

    return hr;
}

//=====================================================================================================================
HRESULT CLRPrivBinderAppX::BindAppXAssemblyByName(
    IAssemblyName * pIAssemblyName,
    DWORD dwAppXBindFlags,
    ICLRPrivAssembly ** ppPrivAssembly)
{
    STANDARD_VM_CONTRACT;
    HRESULT hr = S_OK;

    ReleaseHolder<CLRPrivAssemblyAppX> pAppXAssembly;
    IfFailRet(BindAppXAssemblyByNameWorker(pIAssemblyName, dwAppXBindFlags, &pAppXAssembly));
    IfFailRet(pAppXAssembly->QueryInterface(__uuidof(ICLRPrivAssembly), (LPVOID*)ppPrivAssembly));

    return hr;
}

//=====================================================================================================================
HRESULT CLRPrivBinderAppX::PreBindAppXAssemblyByName(
    IAssemblyName * pIAssemblyName,
    DWORD           dwAppXBindFlags,
    IBindResult **  ppIBindResult)
{
    STANDARD_VM_CONTRACT;
    HRESULT hr = S_OK;

    VALIDATE_ARG_RET(pIAssemblyName != nullptr);
    VALIDATE_ARG_RET(ppIBindResult != nullptr);


    ReleaseHolder<CLRPrivAssemblyAppX> pAppXAssembly;
    IfFailRet(BindAppXAssemblyByNameWorker(pIAssemblyName, dwAppXBindFlags, &pAppXAssembly));
    IfFailRet(pAppXAssembly->GetIBindResult(ppIBindResult));

    return hr;
}

//=====================================================================================================================
HRESULT CLRPrivBinderAppX::FindAssemblyBySpec(
    LPVOID pvAppDomain,
    LPVOID pvAssemblySpec,
    HRESULT * pResult,
    ICLRPrivAssembly ** ppAssembly)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END

    HRESULT hr = S_OK;

    AppDomain* pAppDomain = reinterpret_cast<AppDomain*>(pvAppDomain);
    AssemblySpec* pAssemblySpec = reinterpret_cast<AssemblySpec*>(pvAssemblySpec);
    VALIDATE_PTR_RET(pAppDomain);
    VALIDATE_PTR_RET(pAssemblySpec);
    VALIDATE_PTR_RET(pResult);
    VALIDATE_PTR_RET(ppAssembly);

    //
    // Follow the same order as a bind.
    //

    hr = CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT;

    if (FAILED(hr))
    {
        _ASSERTE(m_pFusionBinder != nullptr);
        hr = m_pFusionBinder->FindFusionAssemblyBySpec(pAppDomain, pAssemblySpec, m_fusionBindingScope, pResult, ppAssembly);
    }

    // See comment in code:CLRPrivBinderAppX::BindAssemblyByName for explanation of this conditional.
    if (hr == CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT)
    {
        if (FAILED(hr) && (m_pWinRTBinder != nullptr))
        {
            hr = m_pWinRTBinder->FindWinRTAssemblyBySpec(pAppDomain, pAssemblySpec, pResult, ppAssembly);
        }

        if (FAILED(hr))
        {
            AssemblyIdentity refId;
            IfFailRet(refId.Initialize(pAssemblySpec));
            bool fCheckParent = false;

            {   // Check for a previously-recorded bind. Host callouts are now forbidden.
                ForbidSuspendThreadCrstHolder lock(&m_MapReadLock);
                BindingRecordMap::element_t const * pKeyVal = m_BindingRecordMap.LookupPtr(&refId);

                if (pKeyVal != nullptr)
                {
                    //
                    // Previous bind occurred. If a failure result is cached then the binder would
                    // have tried the parent binder (if available) before returning.
                    //

                    AssemblyIdentity const & defId(*pKeyVal->Key());
                    BindingRecord const & record(pKeyVal->Value());

                    *ppAssembly = nullptr;
                    *pResult = record.hr;

                    if (SUCCEEDED(*pResult))
                    {
                        //
                        // Previous bind succeeded. Get the corresponding ICLRPrivAssembly.
                        //

                        // Check this binder for a match. Host callouts are now forbidden.
                        CLRPrivAssemblyAppX* pPrivAssembly = m_NameToAssemblyMap.Lookup(defId.Name);

                        if (pPrivAssembly == nullptr)
                        {
                            _ASSERTE_MSG(false, "Should never see success value and a null CLRPrivAssemblyAppX pointer.");
                            return (*pResult = E_UNEXPECTED);
                        }

                        _ASSERTE(pPrivAssembly->m_pIdentity != nullptr);
                        _ASSERTE(pPrivAssembly->m_pIdentity == &defId);

                        // Now check that the version and PKT values are compatible.
                        *pResult = CLRPrivBinderUtil::VerifyBind(refId, *pPrivAssembly->m_pIdentity);

                        if (SUCCEEDED(*pResult))
                        {
                            VERIFY(SUCCEEDED(pPrivAssembly->QueryInterface(__uuidof(ICLRPrivAssembly), (LPVOID*)ppAssembly)));
                        }

                        return S_OK;
                    }
                    else
                    {
                        //
                        // Previous bind failed. Check the parent binder (if available), but do it outside of this binder's lock.
                        //

                        fCheckParent = true;
                    }
                }
                else
                {
                    //
                    // No previous bind occurred. Do not check the parent binder since this could result
                    // in an incorrect bind (if this binder would have bound to a different assembly).
                    //

                    return E_FAIL;
                }
            }

            if (fCheckParent && m_pParentBinder != nullptr)
            {   // Check the parent (shared designer context) for a match.
                hr = m_pParentBinder->FindAssemblyBySpec(pAppDomain, pAssemblySpec, pResult, ppAssembly);
            }
        }
    }

    // There are three possibilities upon exit:
    //   1. Cache lookup failed, in which case FAILED(hr) == true
    //   2. A binding failure was cached, in which case (1) == false && FAILED(*pResult) == true
    //   3. A binding success was cached, in which case we must find an assembly:
    //      (1) == false && (2) == false && *ppAssembly != nullptr
    _ASSERTE(FAILED(hr) || FAILED(*pResult) || *ppAssembly != nullptr);
    return hr;
}

//=====================================================================================================================
// Record the binding result to support cache-based lookups (using ICLRPrivCachedBinder::FindAssemblyBySpec).

HRESULT CLRPrivBinderAppX::CacheBindResult(
    AssemblyIdentity *      pIdentity,  // On success, will assume object lifetime ownership.
    HRESULT                 hrResult)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;

    VALIDATE_PTR_RET(pIdentity);

    // Initialize the binding record.
    BindingRecord rec = { hrResult };
    BindingRecordMap::element_t newEntry(pIdentity, rec);

    // Because the read lock must be taken within a ForbidSusped region, use CheckAddInPhases.
    if (m_BindingRecordMap.CheckAddInPhases<ForbidSuspendThreadCrstHolder, CrstHolder>(
            newEntry, m_MapReadLock, m_MapWriteLock))
    {
        // Indicates that this identity object was cached.
        // Caller relinquishes object ownership.
        return S_OK;
    }
    else
    {
        // Pre-existing entry was found.

#ifdef _DEBUG
        ForbidSuspendThreadCrstHolder lock(&m_MapReadLock);
        auto pExistingEntry = m_BindingRecordMap.LookupPtr(pIdentity);
        if (pExistingEntry != nullptr)
        {
            // It's possible for racing threads to try to cache their results;
            // just make sure that they got the same HRESULT.
            _ASSERTE(pExistingEntry->Value().hr == rec.hr);
        }
#endif

        // Indicates that previous entry existed, and this identity object was not cached.
        // Caller retains object ownership.
        hr = S_FALSE;
    }

    return hr;
}

//=====================================================================================================================
// Implements code:ICLRPrivBinder::BindAssemblyByName

HRESULT CLRPrivBinderAppX::BindAssemblyByName(
    IAssemblyName * pAssemblyName,
    ICLRPrivAssembly ** ppAssembly)
{
    STANDARD_BIND_CONTRACT;
    BinderHRESULT hr = S_OK;
    ReleaseHolder<ICLRPrivAssembly> pResult;

    VALIDATE_ARG_RET(pAssemblyName != nullptr && ppAssembly != nullptr);

    EX_TRY
    {
        hr = CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT;

        if (FAILED(hr))
        {
            _ASSERTE(m_pFusionBinder != nullptr);
            hr = m_pFusionBinder->BindFusionAssemblyByName(pAssemblyName, GetFusionBindingScope(), &pResult);
        }

        //
        // The fusion binder returns CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT only if it did not
        // recognize pAssemblyName as a FX assembly. Only then should other binders be consulted
        // (otherwise applications would be able to copy arbitrary FX assemblies into their AppX
        // package and use them directly).
        //

        if (hr == CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT)
        {
            if (FAILED(hr) && (m_pWinRTBinder != nullptr))
            {
                hr = m_pWinRTBinder->BindWinRTAssemblyByName(pAssemblyName, &pResult);
            }

            if (FAILED(hr))
            {
                hr = BindAppXAssemblyByName(pAssemblyName, ABF_Default, &pResult);
            }

            if (FAILED(hr) && (m_pParentBinder != nullptr))
            {
                hr = m_pParentBinder->BindAssemblyByName(pAssemblyName, &pResult);
            }

            _ASSERTE(FAILED(hr) || pResult != nullptr);
        }
    }
    EX_CATCH_HRESULT(hr);

    // Return if either the bind or the bind cache fails.
    IfFailRet(hr);

    // Success.
    *ppAssembly = pResult.Extract();
    return hr;
}

//=====================================================================================================================
// Implements code:IBindContext::PreBind
HRESULT CLRPrivBinderAppX::PreBind(
    IAssemblyName * pIAssemblyName, 
    DWORD           dwPreBindFlags, 
    IBindResult **  ppIBindResult)
{
    STANDARD_BIND_CONTRACT;
    BinderHRESULT hr = S_OK;

    VALIDATE_ARG_RET((dwPreBindFlags & ~(PRE_BIND_APPLY_POLICY)) == 0);
    VALIDATE_ARG_RET(pIAssemblyName != nullptr && ppIBindResult != nullptr);

    // Assert that we are only working with binder that supports Native Images context bits.
    _ASSERTE(m_fCanUseNativeImages);

    EX_TRY
    {
        hr = CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT;

        if (FAILED(hr))
        {
            hr = m_pFusionBinder->PreBindFusionAssemblyByName(pIAssemblyName, dwPreBindFlags, ppIBindResult);
        }

        if (FAILED(hr) && (m_pWinRTBinder != nullptr))
        {
            hr = m_pWinRTBinder->BindWinRTAssemblyByName(pIAssemblyName, ppIBindResult, TRUE);
        }

        if (FAILED(hr))
        {
            hr = PreBindAppXAssemblyByName(pIAssemblyName, ABF_BindIL, ppIBindResult);
        }
    }
    EX_CATCH_HRESULT(hr);
    
    if (FAILED(hr) && (m_pParentBinder != nullptr))
    {
        ReleaseHolder<IBindContext> pParentBindContext;
        hr = m_pParentBinder->QueryInterface(__uuidof(IBindContext), (LPVOID *)&pParentBindContext);
        if (SUCCEEDED(hr))
        {
            hr = pParentBindContext->PreBind(pIAssemblyName, dwPreBindFlags, ppIBindResult);
        }
    }
    
    return hr;
}

//=====================================================================================================================
UINT_PTR CLRPrivBinderAppX::GetBinderID()
{
    LIMITED_METHOD_CONTRACT;
    return reinterpret_cast<UINT_PTR>(this);
}

//=====================================================================================================================
// Implements code:ICLRPrivBinder::GetBinderID
HRESULT CLRPrivBinderAppX::GetBinderID(
    UINT_PTR *pBinderId)
{
    LIMITED_METHOD_CONTRACT;

    *pBinderId = GetBinderID();
    return S_OK;
}

//=====================================================================================================================
// Implements code:IBindContext::IsDefaultContext
HRESULT CLRPrivBinderAppX::IsDefaultContext()
{
    LIMITED_METHOD_CONTRACT;
    return S_OK;
}

//=====================================================================================================================
// Implements code:ICLRPrivWinRtTypeBinder::FindAssemblyForWinRtTypeIfLoaded
// Finds Assembly * for type in AppDomain * if it is loaded.
// Returns NULL if assembly is not loaded or type is not found.
// 
void * 
CLRPrivBinderAppX::FindAssemblyForWinRtTypeIfLoaded(
    void *  pAppDomain, 
    LPCUTF8 szNamespace, 
    LPCUTF8 szClassName)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
    }
    CONTRACTL_END
    
    void * pAssembly = nullptr;
    if (m_pWinRTBinder != nullptr)
    {
        pAssembly = (void *)m_pWinRTBinder->FindAssemblyForTypeIfLoaded(
            dac_cast<PTR_AppDomain>((AppDomain *)pAppDomain), 
            szNamespace, 
            szClassName);
    }
    
    if ((pAssembly == nullptr) && (m_pParentBinder != nullptr))
    {
        ReleaseHolder<ICLRPrivWinRtTypeBinder> pParentBinder = 
                ToInterface_NoThrow<ICLRPrivWinRtTypeBinder>(m_pParentBinder.GetValue());
        // Parent binder should be another instance of code:CLRPrivBinderAppX class that implements the interface
        _ASSERTE(pParentBinder != nullptr);
        
        pAssembly = pParentBinder->FindAssemblyForWinRtTypeIfLoaded(
            pAppDomain, 
            szNamespace, 
            szClassName);
    }
    
    return pAssembly;
}

//=====================================================================================================================
CLRPrivAssemblyAppX::CLRPrivAssemblyAppX(
    CLRPrivBinderUtil::AssemblyIdentity * pIdentity,
    CLRPrivBinderAppX *pBinder,
    ICLRPrivResource *pIResourceIL,
    IBindResult * pIBindResult)
    : m_pIdentity(pIdentity),
      m_pBinder(nullptr),
      m_pIResourceIL(nullptr),
      m_pIResourceNI(nullptr),
      m_pIBindResult(nullptr)
{
    STANDARD_VM_CONTRACT;

    VALIDATE_PTR_THROW(pIdentity);
    VALIDATE_PTR_THROW(pBinder);
    VALIDATE_PTR_THROW(pIResourceIL);
    VALIDATE_PTR_THROW(pIBindResult);

    m_pBinder = clr::SafeAddRef(pBinder);
    m_pIResourceIL = clr::SafeAddRef(pIResourceIL);
    m_pIBindResult = clr::SafeAddRef(pIBindResult);
}

//=====================================================================================================================
CLRPrivAssemblyAppX::~CLRPrivAssemblyAppX()
{
    LIMITED_METHOD_CONTRACT;
    clr::SafeRelease(m_pIResourceNI);
}

//=====================================================================================================================
LPCWSTR CLRPrivAssemblyAppX::GetSimpleName() const
{
    LIMITED_METHOD_CONTRACT;
    return m_pIdentity->Name;
}

//=====================================================================================================================
// Implements code:IUnknown::Release
ULONG CLRPrivAssemblyAppX::Release()
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_CAN_TAKE_LOCK;
    _ASSERTE(m_cRef > 0);

    ULONG cRef;
    
    {
        // To achieve proper lifetime semantics, the name to assembly map elements' CLRPrivAssemblyAppX 
        // instances are not ref counted. We cannot allow discovery of the object via m_NameToAssemblyMap 
        // when the ref count is 0 (to prevent another thread to AddRef and Release it back to 0 in parallel).
        // All uses of the map are guarded by the map lock, so we have to decrease the ref count under that 
        // lock (to avoid the chance that 2 threads are running Release to ref count 0 at once).
        // Host callouts are now forbidden.
        ForbidSuspendThreadCrstHolder lock(&m_pBinder->m_MapReadLock);
        
        cRef = InterlockedDecrement(&m_cRef);
        if (cRef == 0)
        {
            m_pBinder->m_NameToAssemblyMap.Remove(GetSimpleName());
            m_pBinder->m_BindingRecordMap.Remove(m_pIdentity);
        }
    }

    if (cRef == 0)
    {
        delete this;
    }
    
    return cRef;
}

//=====================================================================================================================
// Implements code:ICLRPrivBinder::BindAssemblyByName
HRESULT CLRPrivAssemblyAppX::BindAssemblyByName(
    IAssemblyName * pAssemblyName,
    ICLRPrivAssembly ** ppAssembly)
{
    WRAPPER_NO_CONTRACT;

    return m_pBinder->BindAssemblyByName(
        pAssemblyName,
        ppAssembly);
}

//=====================================================================================================================
// Implements code:ICLRPrivBinder::GetBinderID
HRESULT CLRPrivAssemblyAppX::GetBinderID(
    UINT_PTR *pBinderId)
{
    WRAPPER_NO_CONTRACT;
    return m_pBinder->GetBinderID(
        pBinderId);
}

//=====================================================================================================================
// Implements code:ICLRPrivAssembly::IsShareable
HRESULT CLRPrivAssemblyAppX::IsShareable(
    BOOL * pbIsShareable)
{
    LIMITED_METHOD_CONTRACT;

    VALIDATE_ARG_RET(pbIsShareable != nullptr);

    *pbIsShareable = FALSE;
    return S_OK;
}

//=====================================================================================================================
// Implements code:ICLRPrivAssembly::GetAvailableImageTypes
HRESULT CLRPrivAssemblyAppX::GetAvailableImageTypes(
    LPDWORD pdwImageTypes)
{
    LIMITED_METHOD_CONTRACT;

    VALIDATE_ARG_RET(pdwImageTypes != nullptr);

    *pdwImageTypes = 0;

    if (m_pIResourceIL != nullptr)
        *pdwImageTypes |= ASSEMBLY_IMAGE_TYPE_IL;

    if (m_pIResourceNI != nullptr)
        *pdwImageTypes |= ASSEMBLY_IMAGE_TYPE_NATIVE;

    return S_OK;
}

//=====================================================================================================================
static ICLRPrivResource* GetResourceForBindResult(
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

//=====================================================================================================================
// Implements code:ICLRPrivAssembly::GetImageResource
HRESULT CLRPrivAssemblyAppX::GetImageResource(
    DWORD dwImageType,
    DWORD * pdwImageType,
    ICLRPrivResource ** ppIResource)
{
    STANDARD_BIND_CONTRACT;
    HRESULT hr = S_OK;

    VALIDATE_ARG_RET(ppIResource != nullptr && m_pIBindResult != nullptr);

    EX_TRY
    {
        DWORD _dwImageType;
        if (pdwImageType == nullptr)
            pdwImageType = &_dwImageType;

        if ((dwImageType & ASSEMBLY_IMAGE_TYPE_NATIVE) == ASSEMBLY_IMAGE_TYPE_NATIVE)
        {
            ReleaseHolder<IBindResult> pIBindResultNI;
            if (m_pIResourceNI == nullptr)
            {
                if (SUCCEEDED(hr = m_pIBindResult->GetNativeImage(&pIBindResultNI, nullptr)) && pIBindResultNI != nullptr)
                {
                    ReleaseHolder<ICLRPrivResource> pResourceNI = GetResourceForBindResult(pIBindResultNI);
                    if (InterlockedCompareExchangeT<ICLRPrivResource *>(&m_pIResourceNI, pResourceNI, nullptr) == nullptr)
                        pResourceNI.SuppressRelease();
                }
                else
                {
                    IfFailGo(CLR_E_BIND_IMAGE_UNAVAILABLE);
                }
            }

            *ppIResource = clr::SafeAddRef(m_pIResourceNI);
            *pdwImageType = ASSEMBLY_IMAGE_TYPE_NATIVE;
        }
        else if ((dwImageType & ASSEMBLY_IMAGE_TYPE_IL) == ASSEMBLY_IMAGE_TYPE_IL)
        {
            *ppIResource = clr::SafeAddRef(m_pIResourceIL);
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
// Implements code:ICLRPrivBinder::VerifyBind
HRESULT CLRPrivBinderAppX::VerifyBind(
    IAssemblyName *pAssemblyName,
    ICLRPrivAssembly *pAssembly,
    ICLRPrivAssemblyInfo *pAssemblyInfo)
{
    STANDARD_BIND_CONTRACT;

    HRESULT hr = S_OK;

    VALIDATE_ARG_RET(pAssemblyName!= nullptr && pAssemblyInfo != nullptr);

    UINT_PTR binderID;
    IfFailRet(pAssembly->GetBinderID(&binderID));

    if (binderID != GetBinderID())
    {
        return pAssembly->VerifyBind(pAssemblyName, pAssembly, pAssemblyInfo);
    }

    return CLRPrivBinderUtil::VerifyBind(pAssemblyName, pAssemblyInfo);
}

//=====================================================================================================================
/*static*/
LPCWSTR CLRPrivBinderAppX::NameToAssemblyMapTraits::GetKey(CLRPrivAssemblyAppX *pAssemblyAppX)
{
    WRAPPER_NO_CONTRACT;
    _ASSERT(pAssemblyAppX != nullptr);
    return pAssemblyAppX->GetSimpleName();
}

//=====================================================================================================================
HRESULT CLRPrivAssemblyAppX::GetIBindResult(
    IBindResult ** ppIBindResult)
{
    STANDARD_VM_CONTRACT;

    VALIDATE_ARG_RET(ppIBindResult != nullptr);
    VALIDATE_CONDITION(m_pIBindResult != nullptr, return E_UNEXPECTED);

    *ppIBindResult = clr::SafeAddRef(m_pIBindResult);

    return S_OK;
}

#endif // !DACCESS_COMPILE
