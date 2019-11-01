// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 
// Contains VM implementation of WinRT type cache for code:CLRPrivBinderWinRT binder.
// 
//=====================================================================================================================

#include "common.h" // precompiled header
#include "clrprivtypecachewinrt.h"

#ifndef DACCESS_COMPILE

//=====================================================================================================================
// S_OK - pAssembly contains type wszTypeName
// S_FALSE - pAssembly does not contain type wszTypeName
// 
HRESULT 
CLRPrivTypeCacheWinRT::ContainsType(
    ICLRPrivAssembly * pPrivAssembly, 
    LPCWSTR            wszTypeName)
{
    STANDARD_VM_CONTRACT;
    
    HRESULT hr = S_OK;
    
    AppDomain * pAppDomain = AppDomain::GetCurrentDomain();
    
    ReleaseHolder<PEAssembly> pPEAssembly;
    IfFailGo(pAppDomain->BindHostedPrivAssembly(nullptr, pPrivAssembly, nullptr, &pPEAssembly));
    _ASSERTE(pPEAssembly != nullptr);
    
    {
        // Find DomainAssembly * (can be cached if this is too slow to call always)
        DomainAssembly * pDomainAssembly = pAppDomain->LoadDomainAssembly(
            nullptr,    // pIdentity
            pPEAssembly,
            FILE_LOAD_DELIVER_EVENTS);
        
        // Convert the type name into namespace and class name in UTF8
        StackSString ssTypeNameWCHAR(wszTypeName);
        
        StackSString ssTypeName;
        ssTypeNameWCHAR.ConvertToUTF8(ssTypeName);
        LPUTF8 szTypeName = (LPUTF8)ssTypeName.GetUTF8NoConvert();
    
        LPCUTF8 szNamespace;
        LPCUTF8 szClassName;
        ns::SplitInline(szTypeName, szNamespace, szClassName);
        
        hr = ContainsTypeHelper(pDomainAssembly->GetAssembly(), szNamespace, szClassName);
        _ASSERTE((hr == S_OK) || (hr == S_FALSE));
        return hr;
    }
    
ErrExit:
    return hr;
} // CLRPrivTypeCacheWinRT::ContainsType

#endif //!DACCESS_COMPILE

//=====================================================================================================================
// 
// Checks if the type (szNamespace/szClassName) is present in the assembly pAssembly.
// 
// Return value:
//  S_OK -    Type is present in the assembly.
//  S_FALSE - Type is not present.
//  No other error codes or success codes
// 
HRESULT 
CLRPrivTypeCacheWinRT::ContainsTypeHelper(
    PTR_Assembly pAssembly, 
    LPCUTF8      szNamespace, 
    LPCUTF8      szClassName)
{
    CONTRACTL
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END
    
    NameHandle typeName(szNamespace, szClassName);
    
    // Find the type in the assembly (use existing hash of all type names defined in the assembly)
    TypeHandle thType;
    mdToken    tkType;
    Module *   pTypeModule;
    mdToken    tkExportedType;
    
    if (pAssembly->GetLoader()->FindClassModuleThrowing(
            &typeName, 
            &thType, 
            &tkType, 
            &pTypeModule, 
            &tkExportedType, 
            nullptr,    // pFoundEntry
            nullptr,    // pLookInThisModuleOnly
            Loader::DontLoad))
    {
        return S_OK;
    }
    else
    {
        return S_FALSE;
    }
} // CLRPrivTypeCacheWinRT::ContainsTypeHelper

//=====================================================================================================================
// 
// Checks if the assembly pPrivAssembly (referenced from assembly in pAppDomain) contains type (szNamespace/szClassName).
// Fills *ppAssembly if it contains the type.
// 
// Return value:
//  S_OK -    Contains type (fills *ppAssembly).
//  S_FALSE - Does not contain the type (*ppAssembly is not filled).
//  E_FAIL -  Assembly is not loaded.
// 
HRESULT 
CLRPrivTypeCacheWinRT::ContainsTypeIfLoaded(
    PTR_AppDomain        pAppDomain, 
    PTR_ICLRPrivAssembly pPrivAssembly, 
    LPCUTF8              szNamespace, 
    LPCUTF8              szClassName, 
    PTR_Assembly *       ppAssembly)
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
    
    HRESULT hr;
    
    PTR_DomainAssembly pDomainAssembly = pAppDomain->FindAssembly(pPrivAssembly);
    if (pDomainAssembly == nullptr || !pDomainAssembly->IsLoaded())
    {   // The assembly is not loaded into the AppDomain
        return E_FAIL;
    }
    PTR_Assembly pAssembly = dac_cast<PTR_Assembly>(pDomainAssembly->GetLoadedAssembly());
    if (pAssembly == nullptr)
    {   // The assembly failed to load earlier (exception is cached on pDomainAssembly)
        return E_FAIL;
    }
    
    hr = ContainsTypeHelper(pAssembly, szNamespace, szClassName);
    _ASSERTE((hr == S_OK) || (hr == S_FALSE));
    if (hr == S_OK)
    {   // The type is present in the assembly
        *ppAssembly = pAssembly;
    }
    return hr;
} // CLRPrivTypeCacheWinRT::ContainsTypeIfLoaded

#ifndef DACCESS_COMPILE

#ifndef CROSSGEN_COMPILE
//=====================================================================================================================
// Raises user event DesignerNamespaceResolveEvent to get a list of files for this namespace.
// 
void 
CLRPrivTypeCacheWinRT::RaiseDesignerNamespaceResolveEvent(
    LPCWSTR                                wszNamespace, 
    CLRPrivBinderUtil::WStringListHolder * pFileNameList)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pFileNameList != nullptr);

    GCX_COOP();
    
    struct _gc {
        STRINGREF str;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    
    GCPROTECT_BEGIN(gc);
    MethodDescCallSite onNamespaceResolve(METHOD__WINDOWSRUNTIMEMETATADA__ON_DESIGNER_NAMESPACE_RESOLVE);
    gc.str = StringObject::NewString(wszNamespace);
    ARG_SLOT args[1] =
    {
        ObjToArgSlot(gc.str)
    };
    PTRARRAYREF ResultingFileNameArrayRef = (PTRARRAYREF) onNamespaceResolve.Call_RetOBJECTREF(args);
    if (ResultingFileNameArrayRef != NULL)
    {
        for (DWORD i = 0; i < ResultingFileNameArrayRef->GetNumComponents(); i++)
        {
            STRINGREF ResultingFileNameRef = (STRINGREF) ResultingFileNameArrayRef->GetAt(i);
            _ASSERTE(ResultingFileNameRef != NULL); // Verified in the managed code OnDesignerNamespaceResolveEvent
                
            SString sFileName;
            ResultingFileNameRef->GetSString(sFileName);
            _ASSERTE(!sFileName.IsEmpty()); // Verified in the managed code OnDesignerNamespaceResolveEvent
                
            pFileNameList->InsertTail(sFileName.GetUnicode());
        }
    }
    GCPROTECT_END();
} // CLRPrivTypeCacheWinRT::RaiseDesignerNamespaceResolveEvent

//=====================================================================================================================
#endif // CROSSGEN_COMPILE

CLRPrivTypeCacheWinRT * CLRPrivTypeCacheWinRT::s_pSingleton = nullptr;

//=====================================================================================================================
CLRPrivTypeCacheWinRT * 
CLRPrivTypeCacheWinRT::GetOrCreateTypeCache()
{
    STANDARD_VM_CONTRACT;
    
    if (s_pSingleton == nullptr)
    {
        ReleaseHolder<CLRPrivTypeCacheWinRT> pTypeCache;
        pTypeCache = clr::SafeAddRef(new CLRPrivTypeCacheWinRT());

        if (InterlockedCompareExchangeT<decltype(s_pSingleton)>(&s_pSingleton, pTypeCache, nullptr) == nullptr)
        {
            pTypeCache.SuppressRelease();
        }
    }
    
    return s_pSingleton;
}

//=====================================================================================================================

#endif //!DACCESS_COMPILE
