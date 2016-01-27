// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#include "common.h" // precompiled header

#ifndef DACCESS_COMPILE

//=====================================================================================================================
#include "assemblyspec.hpp"
#include "corhdr.h"
#include "domainfile.h"
#include "fusion.h"
#include "policy.h"
#include "sstring.h"
#include "stackingallocator.h"
#include "threads.h"
#include "clrprivbinderfusion.h"
#include "clrprivbinderutil.h"
#include "fusionlogging.h"

using namespace CLRPrivBinderUtil;

//=================================================================================================
#define STDMETHOD_NOTIMPL(...) \
    STDMETHOD(__VA_ARGS__) \
    { \
        WRAPPER_NO_CONTRACT; \
        _ASSERTE_MSG(false, "Method not implemented."); \
        return E_NOTIMPL; \
    }

//=================================================================================================
static HRESULT PropagateOutStringArgument(
    __in  LPCSTR pszValue,
    __out_ecount_opt(*pcchArg) LPWSTR pwzArg,
    __in  DWORD cchArg,
    __out DWORD * pcchArg)
{
    LIMITED_METHOD_CONTRACT;

    VALIDATE_PTR_RET(pszValue);
    VALIDATE_CONDITION((pwzArg == nullptr || cchArg > 0), return E_INVALIDARG);

    HRESULT hr = S_OK;

    if (pwzArg != nullptr)
    {
        DWORD cchWritten = WszMultiByteToWideChar(
            CP_UTF8, 0 /*flags*/, pszValue, -1, pwzArg, cchArg);

        if (cchWritten == 0)
        {
            hr = HRESULT_FROM_GetLastError();
        }
        else if (pcchArg != nullptr)
        {
            *pcchArg = cchWritten;
        }
    }

    if (pcchArg != nullptr && (pwzArg == nullptr || hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER)))
    {
        *pcchArg = WszMultiByteToWideChar(
            CP_UTF8, 0 /*flags*/, pszValue, -1, nullptr, 0);

        if (*pcchArg == 0)
        {
            hr = HRESULT_FROM_GetLastError();
        }
    }

    return hr;
}

//=================================================================================================
// This is needed to allow calls to IsAnyFrameworkAssembly in GC_NOTRIGGER/NO_FAULT regions (i.e.,
// GC stack walking). CAssemblyName (which implements IAssemblyName in most other uses) allocates
// during construction and so cannot be used in this scenario.

class AssemblySpecAsIAssemblyName
    : public IAssemblyName
{
public:
    AssemblySpecAsIAssemblyName(
        AssemblySpec * pSpec)
        : m_pSpec(pSpec)
    { LIMITED_METHOD_CONTRACT; }

    //=============================================================================================
    // IUnknown methods

    // Not used by IsAnyFrameworkAssembly
    STDMETHOD_(ULONG, AddRef())
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE_MSG(false, "Method not implemented.");
        return E_NOTIMPL;
    }

    // Not used by IsAnyFrameworkAssembly
    STDMETHOD_(ULONG, Release())
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE_MSG(false, "Method not implemented.");
        return E_NOTIMPL;
    }

    // Not used by IsAnyFrameworkAssembly
    STDMETHOD_NOTIMPL(QueryInterface(
        REFIID riid,
        void **ppvObject));

    //=============================================================================================
    // IAssemblyName methods

    STDMETHOD_NOTIMPL(SetProperty(
        DWORD        PropertyId, 
        void const * pvProperty,
        DWORD        cbProperty));

#define ASSURE_SUFFICIENT_BUFFER(SRCSIZE) \
    do { \
        if ((pvProperty == nullptr) || (*pcbProperty < SRCSIZE)) { \
            *pcbProperty = SRCSIZE; \
            return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER); \
        } \
    } while (false)

    STDMETHOD(GetProperty)(
        DWORD    PropertyId, 
        LPVOID   pvProperty,
        LPDWORD  pcbProperty)
    {
        LIMITED_METHOD_CONTRACT;

        VALIDATE_PTR_RET(pcbProperty);
        VALIDATE_CONDITION((pvProperty == nullptr) == (*pcbProperty == 0), return E_INVALIDARG);

        HRESULT hr = S_OK;

        switch (PropertyId)
        {
        case ASM_NAME_NAME:
            return PropagateOutStringArgument(m_pSpec->GetName(), (LPWSTR) pvProperty,
                                              *pcbProperty / sizeof(WCHAR), pcbProperty);

        case ASM_NAME_MAJOR_VERSION:
            ASSURE_SUFFICIENT_BUFFER(sizeof(USHORT));
            *reinterpret_cast<USHORT*>(pvProperty) = m_pSpec->GetContext()->usMajorVersion;
            *pcbProperty = sizeof(USHORT);
            return S_OK;

        case ASM_NAME_MINOR_VERSION:
            ASSURE_SUFFICIENT_BUFFER(sizeof(USHORT));
            *reinterpret_cast<USHORT*>(pvProperty) = m_pSpec->GetContext()->usMinorVersion;
            *pcbProperty = sizeof(USHORT);
            return S_OK;

        case ASM_NAME_BUILD_NUMBER:
            ASSURE_SUFFICIENT_BUFFER(sizeof(USHORT));
            *reinterpret_cast<USHORT*>(pvProperty) = m_pSpec->GetContext()->usBuildNumber;
            *pcbProperty = sizeof(USHORT);
            return S_OK;

        case ASM_NAME_REVISION_NUMBER:
            ASSURE_SUFFICIENT_BUFFER(sizeof(USHORT));
            *reinterpret_cast<USHORT*>(pvProperty) = m_pSpec->GetContext()->usRevisionNumber;
            *pcbProperty = sizeof(USHORT);
            return S_OK;

        case ASM_NAME_CULTURE:
            if (m_pSpec->GetContext()->szLocale == nullptr)
            {
                return FUSION_E_INVALID_NAME;
            }
            return PropagateOutStringArgument(m_pSpec->GetContext()->szLocale, (LPWSTR) pvProperty,
                                              *pcbProperty / sizeof(WCHAR), pcbProperty);

        case ASM_NAME_PUBLIC_KEY_TOKEN:
            {
                if (!m_pSpec->HasPublicKeyToken())
                {
                    return FUSION_E_INVALID_NAME;
                }

                PBYTE pbSN;
                DWORD cbSN;
                m_pSpec->GetPublicKeyToken(&pbSN, &cbSN);
                ASSURE_SUFFICIENT_BUFFER(cbSN);
                memcpy_s(pvProperty, *pcbProperty, pbSN, cbSN);
                *pcbProperty = cbSN;
            }
            return S_OK;

        case ASM_NAME_RETARGET:
            ASSURE_SUFFICIENT_BUFFER(sizeof(BOOL));
            *reinterpret_cast<BOOL*>(pvProperty) = m_pSpec->IsRetargetable();
            *pcbProperty = sizeof(BOOL);
            return S_OK;

        default:
            _ASSERTE_MSG(false, "Unexpected property requested.");
            return E_INVALIDARG;
        }
    }

#undef ASSURE_SUFFICIENT_BUFFER

    // Not used by IsAnyFrameworkAssembly
    STDMETHOD_NOTIMPL(Finalize());

    // Not used by IsAnyFrameworkAssembly
    STDMETHOD_NOTIMPL(GetDisplayName(
        __out_ecount_opt(*pccDisplayName) LPOLESTR szDisplayName,
        __inout LPDWORD pccDisplayName,
        DWORD dwDisplayFlags));

    // Not used by IsAnyFrameworkAssembly
    STDMETHOD_NOTIMPL(Reserved(
        REFIID               refIID,
        IUnknown            *pUnkReserved1, 
        IUnknown            *pUnkReserved2,
        LPCOLESTR            szReserved,
        LONGLONG             llReserved,
        LPVOID               pvReserved,
        DWORD                cbReserved,
        LPVOID               *ppReserved));


    STDMETHOD(GetName)(
        __inout LPDWORD lpcwBuffer,
        __out_ecount_opt(*lpcwBuffer) WCHAR *pwzName)
    {
        LIMITED_METHOD_CONTRACT;

        VALIDATE_PTR_RET(lpcwBuffer);
        return PropagateOutStringArgument(
            m_pSpec->GetName(), pwzName, *lpcwBuffer, lpcwBuffer);
    }
        
    STDMETHOD(GetVersion)(
        LPDWORD pdwVersionHi,
        LPDWORD pdwVersionLow)
    {
        LIMITED_METHOD_CONTRACT;

        HRESULT     hr = S_OK;

        VALIDATE_PTR_RET(pdwVersionHi);
        VALIDATE_PTR_RET(pdwVersionLow);

        AssemblyMetaDataInternal * pAMDI = m_pSpec->GetContext();
        
        *pdwVersionHi  = MAKELONG(pAMDI->usMinorVersion, pAMDI->usMajorVersion);
        *pdwVersionLow = MAKELONG(pAMDI->usRevisionNumber, pAMDI->usBuildNumber);

        return S_OK;
    }


    // Exists exclusively to support fusion's IsSystem helper, which compares against 'mscorlib'.
    STDMETHOD(IsEqual)(
        IAssemblyName *pName,
        DWORD dwCmpFlags)
    {
        LIMITED_METHOD_CONTRACT;

        HRESULT     hr = S_OK;

        VALIDATE_PTR_RET(pName);

        // This function is here just to support checks against the name 'mscorlib'.
        if ((dwCmpFlags & ASM_CMPF_NAME) != ASM_CMPF_NAME)
        {
            return E_NOTIMPL;
        }

        DWORD cchName1 = 0;
        WCHAR wzName1[_MAX_PATH];
        IfFailRet(pName->GetName(&cchName1, wzName1));
        _ASSERTE(SString::_wcsicmp(wzName1, W("mscorlib")) == 0);

        WCHAR wzName2[_MAX_PATH];
        DWORD cchName2 = WszMultiByteToWideChar(
            CP_UTF8, 0 /*flags*/, m_pSpec->GetName(), -1, wzName2, (int) (sizeof(wzName2) / sizeof(wzName2[0])));

        if (0 == cchName2)
        {
            _ASSERTE(HRESULT_FROM_GetLastError() != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER));
            return HRESULT_FROM_GetLastError();
        }

        if (cchName1 != cchName2)
        {
            return S_FALSE;
        }

        return SString::_wcsnicmp(wzName1, wzName2, cchName1) == 0
             ? S_OK
             : S_FALSE;
    }
        
    STDMETHOD_NOTIMPL(Clone(
        IAssemblyName **pName));

private:
    AssemblySpec * m_pSpec;
};

//=====================================================================================================================
HRESULT CLRPrivBinderFusion::FindFusionAssemblyBySpec(
    LPVOID pvAppDomain,
    LPVOID pvAssemblySpec,
    BindingScope kBindingScope,
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
        return CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT;
    }

    BOOL fIsSupportedInAppX;
    {    
        AssemblySpecAsIAssemblyName asName(pAssemblySpec);

        if (Fusion::Util::IsAnyFrameworkAssembly(&asName, &fIsSupportedInAppX) != S_OK)
        {   // Not a framework assembly identity.
            IfFailRet(CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT);
        }
    }

    if (kBindingScope == kBindingScope_FrameworkSubset)
    {   // We should allow only some framework assemblies to load
        
        // DevMode has to allow all FX assemblies, not just a subset - see code:PreBind for more info
        {
            // Disabling for now, as it causes too many violations.
            //CONTRACT_VIOLATION(GCViolation | FaultViolation | ModeViolation);
            //_ASSERTE(!AppX::IsAppXDesignMode());
        }
        
        if (!fIsSupportedInAppX)
        {   // Assembly is blocked for AppX, fail the load
            *pResult = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
            *ppAssembly = nullptr;
            return S_OK;
        }
    }

    return FindAssemblyBySpec(pvAppDomain, pvAssemblySpec, pResult, ppAssembly);
}

//=====================================================================================================================
static
PEAssembly * FindCachedFile(AppDomain * pDomain, AssemblySpec * pSpec)
{
    // Look for cached bind result. Prefer a cached DomainAssembly, as it takes priority over a
    // cached PEAssembly (which can be different from the one associated with the DomainAssembly).
    DomainAssembly * pDomainAssembly = pDomain->FindCachedAssembly(pSpec, FALSE);
    return (pDomainAssembly != nullptr)
         ? (pDomainAssembly->GetFile())
         : (pDomain->FindCachedFile(pSpec, FALSE));
}

//=====================================================================================================================
// There is no need to create a separate binding record, since we can always just look in the AppDomain's
// AssemblySpecBindingCache for an answer (which is precisely what this function does).

HRESULT CLRPrivBinderFusion::FindAssemblyBySpec(
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

    // For the Architecture property, canonicalize peMSIL to peNone (which are considered equivalent),
    // to ensure consistent lookups in the AssemblySpecBindingCache for the CLRPrivBinderFusion binder.
    if (pAssemblySpec->GetPEKIND() == peMSIL)
    {
        pAssemblySpec->SetPEKIND(peNone);
    }

    PEAssembly * pPEAssembly = FindCachedFile(pAppDomain, pAssemblySpec);
    if (pPEAssembly == nullptr)
    {
        return E_FAIL;
    }

    // Could be racing with another thread that has just added the PEAssembly to the binding cache
    // but not yet allocated and assigned a host assembly.
    if (!pPEAssembly->HasHostAssembly())
    {
        return E_FAIL;
    }

    *pResult = S_OK;
    *ppAssembly = clr::SafeAddRef(pPEAssembly->GetHostAssembly());

    return S_OK;
}

//=====================================================================================================================
HRESULT CLRPrivBinderFusion::BindAssemblyByNameWorker(
    IAssemblyName *     pAssemblyName, 
    ICLRPrivAssembly ** ppAssembly)
{
    STANDARD_VM_CONTRACT;
    PRECONDITION(CheckPointer(pAssemblyName));
    PRECONDITION(CheckPointer(ppAssembly));

    HRESULT hr = S_OK;

    AppDomain * pCurDomain = AppDomain::GetCurrentDomain();
    if (pCurDomain == nullptr)
        ThrowHR(E_UNEXPECTED);

    AssemblySpec prePolicySpec;
    AssemblySpec postPolicySpec;

    prePolicySpec.InitializeSpec(pAssemblyName);

    // For the Architecture property, canonicalize peMSIL to peNone (which are considered equivalent),
    // to ensure consistent lookups in the AssemblySpecBindingCache for the CLRPrivBinderFusion binder.
    if (prePolicySpec.GetPEKIND() == peMSIL)
    {
        prePolicySpec.SetPEKIND(peNone);
    }

    AssemblySpec * pBindSpec = &prePolicySpec;
    PEAssemblyHolder pPEAssembly = clr::SafeAddRef(FindCachedFile(pCurDomain, pBindSpec));

    if (pPEAssembly == nullptr)
    {
        // Early on in domain setup there may not be a fusion context, so skip ApplyPolicy then.
        _ASSERTE(pCurDomain->GetFusionContext() != nullptr || prePolicySpec.IsMscorlib());
        if (pCurDomain->GetFusionContext() != nullptr)
        {
            ReleaseHolder<IAssemblyName> pPolicyAssemblyName;
            DWORD dwPolicyApplied = 0;
            ApplyPolicy(pAssemblyName, pCurDomain->GetFusionContext(), nullptr, &pPolicyAssemblyName, nullptr, nullptr, &dwPolicyApplied);

            if (dwPolicyApplied != 0)
            {
                postPolicySpec.InitializeSpec(pPolicyAssemblyName);
                pBindSpec = &postPolicySpec;
                pPEAssembly = clr::SafeAddRef(FindCachedFile(pCurDomain, pBindSpec));
            }
        }

        if (pPEAssembly == nullptr)
        {
            // Trigger a load.
            pPEAssembly = pCurDomain->BindAssemblySpec(
                pBindSpec,  // AssemblySpec
                TRUE,       // ThrowOnFileNotFound
                FALSE,      // RaisePrebindEvents
                nullptr,    // CallerStackMark
                nullptr,    // AssemblyLoadSecurity
                FALSE);     // fUseHostBinderIfAvailable - to avoid infinite recursion
                _ASSERTE(FindCachedFile(pCurDomain, pBindSpec) == pPEAssembly || pBindSpec->IsMscorlib());
        }

        // If a post-policy spec was used, add the pre-policy spec to the binding cache
        // so that it can be found by FindAssemblyBySpec.
        if (&prePolicySpec != pBindSpec)
        {
            // Failure to add simply means someone else beat us to it. In that case
            // the FindCachedFile call below (after catch block) will update result
            // to the cached value.
            INDEBUG(BOOL fRes =) pCurDomain->AddFileToCache(&prePolicySpec, pPEAssembly, TRUE /* fAllowFailure */);
            _ASSERTE(!fRes || prePolicySpec.IsMscorlib() || FindCachedFile(pCurDomain, &prePolicySpec) == pPEAssembly);
        }

        // Ensure that the assembly is discoverable through a consistent assembly name (the assembly def name of the assembly)
        AssemblySpec specAssemblyDef;
        specAssemblyDef.InitializeSpec(pPEAssembly);

        // It is expected that all assemlbies found here will be unified assemblies, and therefore have a public key.
        _ASSERTE(specAssemblyDef.IsStrongNamed()); 

        // Convert public key into the format that matches the garaunteed cache in the AssemblySpecBindingCache  ... see the extended logic
        // in Module::GetAssemblyIfLoaded.
        if (specAssemblyDef.IsStrongNamed() && specAssemblyDef.HasPublicKey())
        {
            specAssemblyDef.ConvertPublicKeyToToken();
        }
        pCurDomain->AddFileToCache(&specAssemblyDef, pPEAssembly, TRUE);
    }

    if (!pPEAssembly->HasHostAssembly())
    {
        // This can happen if we just loaded the PEAssembly with BindAssemblySpec above, or if the PEAssembly
        // Was not loaded through this binder. (NGEN Case)
        
        // Note: There can be multiple PEAssembly objects for the same file, however we have to create unique 
        // CLRPrivAssemblyFusion object, otherwise code:AppDomain::FindAssembly will not recognize the duplicates which 
        // will lead to creation of multiple code:DomainAssembly objects for the same file in the same AppDomain.
        
        InlineSString<128> ssPEAssemblyName;
        FusionBind::GetAssemblyNameDisplayName(pPEAssembly->GetFusionAssemblyName(), ssPEAssemblyName, ASM_DISPLAYF_FULL);
        NewHolder<CLRPrivAssemblyFusion> pAssemblyObj = new CLRPrivAssemblyFusion(ssPEAssemblyName.GetUnicode(), this);
        
        {
            CrstHolder lock(&m_SetHostAssemblyLock);
            if (!pPEAssembly->HasHostAssembly())
            {
                // Add the host assembly to the PEAssembly.
                pPEAssembly->SetHostAssembly(pAssemblyObj.Extract());
            }
        }
    }

    // Trigger a load so that a DomainAssembly is associated with the ICLRPrivAssembly created above.
    pPEAssembly = clr::SafeAddRef(pCurDomain->LoadDomainAssembly(pBindSpec, pPEAssembly, FILE_LOADED)->GetFile());

    _ASSERTE(pPEAssembly != nullptr);
    _ASSERTE(pPEAssembly->HasHostAssembly());
    _ASSERTE(pCurDomain->FindAssembly(pPEAssembly->GetHostAssembly()) != nullptr);

    fusion::logging::LogMessage(0, ID_FUSLOG_BINDING_STATUS_FOUND, pPEAssembly->GetPath().GetUnicode());

    *ppAssembly = clr::SafeAddRef(pPEAssembly->GetHostAssembly());

    return hr;
}

//=====================================================================================================================
void CLRPrivBinderFusion::BindMscorlib(
    PEAssembly * pPEAssembly)
{
    STANDARD_VM_CONTRACT;
    
#ifdef _DEBUG
    NewArrayHolder<WCHAR> dbg_wszAssemblySimpleName;
    _ASSERTE(SUCCEEDED(fusion::util::GetProperty(pPEAssembly->GetFusionAssemblyName(), ASM_NAME_NAME, &dbg_wszAssemblySimpleName)));
    
    _ASSERTE(wcscmp(dbg_wszAssemblySimpleName, W("mscorlib")) == 0);
#endif //_DEBUG
    
    NewHolder<CLRPrivAssemblyFusion> pPrivAssembly = new CLRPrivAssemblyFusion(W("mscorlib"), this);
   
    pPEAssembly->SetHostAssembly(pPrivAssembly.Extract());
}

//=====================================================================================================================
HRESULT CLRPrivBinderFusion::BindFusionAssemblyByName(
    IAssemblyName *     pAssemblyName, 
    BindingScope        kBindingScope, 
    ICLRPrivAssembly ** ppAssembly)
{
    STANDARD_VM_CONTRACT;
    HRESULT hr = S_OK;
    
    fusion::logging::StatusScope logStatus(0, ID_FUSLOG_BINDING_STATUS_FRAMEWORK, &hr);
    
    DWORD dwContentType = AssemblyContentType_Default;
    IfFailRet(fusion::util::GetProperty(pAssemblyName, ASM_NAME_CONTENT_TYPE, &dwContentType));
    if ((hr == S_OK) && (dwContentType != AssemblyContentType_Default))
    {   // Not a NetFX content type.
        IfFailRet(CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT);
    }
    
    BOOL fIsSupportedInAppX;
    if (Fusion::Util::IsAnyFrameworkAssembly(pAssemblyName, &fIsSupportedInAppX) != S_OK)
    {   // Not a framework assembly identity.
        IfFailRet(CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT);
    }
    if (kBindingScope == kBindingScope_FrameworkSubset)
    {   // We should allow only some framework assemblies to load
        
        // DevMode has to allow all FX assemblies, not just a subset - see code:PreBind for more info
        _ASSERTE(!AppX::IsAppXDesignMode());
        
        if (!fIsSupportedInAppX)
        {   // Assembly is blocked for AppX, fail the load
            fusion::logging::LogMessage(0, ID_FUSLOG_BINDING_STATUS_FX_ASSEMBLY_BLOCKED);
            
            IfFailRet(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND));
        }
    }
    
    return (hr = BindAssemblyByNameWorker(pAssemblyName, ppAssembly));
}

//=====================================================================================================================
// Implements code:ICLRPrivBinder::BindAssemblyByName
HRESULT CLRPrivBinderFusion::BindAssemblyByName(
    IAssemblyName * pAssemblyName,
    ICLRPrivAssembly ** ppAssembly)
{
    WRAPPER_NO_CONTRACT;
    return BindAssemblyByNameWorker(
        pAssemblyName, 
        ppAssembly);
}

//=====================================================================================================================
// Implements code:ICLRPrivBinder::GetBinderID
HRESULT CLRPrivBinderFusion::GetBinderID(
    UINT_PTR *pBinderId)
{
    LIMITED_METHOD_CONTRACT;

    *pBinderId = (UINT_PTR)this;
    return S_OK;
}

//=====================================================================================================================
// Implements code:IBindContext::PreBind
HRESULT CLRPrivBinderFusion::PreBind(
    IAssemblyName * pIAssemblyName,
    DWORD           dwPreBindFlags,
    IBindResult **  ppIBindResult)
{
    STANDARD_BIND_CONTRACT;
    PRECONDITION(CheckPointer(pIAssemblyName));
    PRECONDITION(CheckPointer(ppIBindResult));
    
    HRESULT hr = S_OK;
    
    BOOL fIsSupportedInAppX;
    if (Fusion::Util::IsAnyFrameworkAssembly(pIAssemblyName, &fIsSupportedInAppX) != S_OK)
    {   // Not a framework assembly identity.
        return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
    }
    
    EX_TRY
    {
        // Create new IL binding scope.
        fusion::logging::BindingScope defaultScope(pIAssemblyName, FUSION_BIND_LOG_CATEGORY_DEFAULT);
        
        // Ideally the caller would give us arg kBindingContext like in code:BindFusionAssemblyByName, so we can give the same answer.
        // That is not easy, so we will make the decision here:
        //   - DevMode will allow all FX assemblies (that covers designer binding context scenario for designers that need to 
        //      load WPF with ngen images for perf reasons).
        //      We know that the real bind via code:BindFusionAssemblyByName will succeed for the assemblies (because we are in DevMode).
        //   - Normal mode (non-DevMode) we will allow only subset of FX assemblies.
        //      It implies that designer binding context (used by debuggers) will not use ngen images for blocked FX assemblies 
        //      (transitively). That is acceptable performance trade-off.
        if (!AppX::IsAppXDesignMode())
        {   // We should allow only some framework assemblies to load
            if (!fIsSupportedInAppX)
            {   // Assembly is blocked for AppX, fail the load
                fusion::logging::LogMessage(0, ID_FUSLOG_BINDING_STATUS_FX_ASSEMBLY_BLOCKED);
                
                hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
            }
        }
        
        if (SUCCEEDED(hr))
        {
            AppDomain * pDomain = AppDomain::GetCurrentDomain();
            ReleaseHolder<IBindContext> pIBindContext;
            if (SUCCEEDED(hr = GetBindContextFromApplicationContext(pDomain->CreateFusionContext(), &pIBindContext)))
            {
                hr = pIBindContext->PreBind(pIAssemblyName, dwPreBindFlags, ppIBindResult);
            }
        }
    }
    EX_CATCH_HRESULT(hr);
    
    return hr;
}

//=====================================================================================================================
HRESULT CLRPrivBinderFusion::PreBindFusionAssemblyByName(
    IAssemblyName  *pIAssemblyName,
    DWORD           dwPreBindFlags,
    IBindResult   **ppIBindResult)
{
    STANDARD_VM_CONTRACT;
    HRESULT hr = S_OK;

    DWORD dwContentType = AssemblyContentType_Default;
    IfFailRet(fusion::util::GetProperty(pIAssemblyName, ASM_NAME_CONTENT_TYPE, &dwContentType));
    if ((hr == S_OK) && (dwContentType != AssemblyContentType_Default))
    {   // Not a NetFX content type.
        IfFailRet(CLR_E_BIND_UNRECOGNIZED_IDENTITY_FORMAT);
    }

    IfFailRet(PreBind(pIAssemblyName, dwPreBindFlags, ppIBindResult));
    _ASSERTE(*ppIBindResult != nullptr);

    if (*ppIBindResult == nullptr)
        IfFailRet(E_UNEXPECTED);

    return S_OK;
}

//=====================================================================================================================
// Implements code:IBindContext::IsDefaultContext
HRESULT CLRPrivBinderFusion::IsDefaultContext()
{
    STANDARD_BIND_CONTRACT;
    return S_OK;
}

//=====================================================================================================================
CLRPrivBinderFusion::~CLRPrivBinderFusion()
{
    WRAPPER_NO_CONTRACT;
}

//=====================================================================================================================
CLRPrivAssemblyFusion::CLRPrivAssemblyFusion(
    LPCWSTR               wszName, 
    CLRPrivBinderFusion * pBinder)
    : m_pBinder(clr::SafeAddRef(pBinder)), 
    m_wszName(DuplicateStringThrowing(wszName))
{
    STANDARD_VM_CONTRACT;
}

//=====================================================================================================================
LPCWSTR CLRPrivAssemblyFusion::GetName() const
{
    LIMITED_METHOD_CONTRACT;
    
    return m_wszName;
}

//=====================================================================================================================
// Implements code:IUnknown::Release
ULONG CLRPrivAssemblyFusion::Release()
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_CAN_TAKE_LOCK;
    _ASSERTE(m_cRef > 0);
    
    ULONG cRef = InterlockedDecrement(&m_cRef);

    if (cRef == 0)
    {
        delete this;
    }

    return cRef;
}

//=====================================================================================================================
// Implements code:ICLRPrivBinder::BindAssemblyByName
HRESULT CLRPrivAssemblyFusion::BindAssemblyByName(
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
HRESULT CLRPrivAssemblyFusion::GetBinderID(
    UINT_PTR *pBinderId)
{
    LIMITED_METHOD_CONTRACT;

    *pBinderId = reinterpret_cast<UINT_PTR>(m_pBinder.GetValue());
    return S_OK;
}

//=====================================================================================================================
// Implements code:ICLRPrivAssembly::IsShareable
HRESULT CLRPrivAssemblyFusion::IsShareable(
    BOOL * pbIsShareable)
{
    LIMITED_METHOD_CONTRACT;
    *pbIsShareable = TRUE; // These things are only used in the AppX scenario, where all fusion assemblies are unified, shareable assemblies.
    return S_OK;
}

//=====================================================================================================================
// Implements code:ICLRPrivAssembly::GetAvailableImageTypes
HRESULT CLRPrivAssemblyFusion::GetAvailableImageTypes(
    LPDWORD pdwImageTypes)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"CLRPrivAssemblyFusion::GetAvailableImageTypes");
    return E_NOTIMPL;
}    

//=====================================================================================================================
// Implements code:ICLRPrivAssembly::GetImageResource
HRESULT CLRPrivAssemblyFusion::GetImageResource(
    DWORD dwImageType,
    DWORD *pdwImageType,
    ICLRPrivResource ** ppIResource)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"CLRPrivAssemblyFusion::GetImageResource");
    return E_NOTIMPL;
}

#endif // !DACCESS_COMPILE

