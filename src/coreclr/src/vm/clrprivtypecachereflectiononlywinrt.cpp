// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 
// Contains VM implementation of WinRT type cache for code:CLRPrivBinderReflectionOnlyWinRT binder.
// 
//=====================================================================================================================

#include "common.h" // precompiled header

#ifndef DACCESS_COMPILE
#ifdef FEATURE_REFLECTION_ONLY_LOAD

#include "clrprivtypecachereflectiononlywinrt.h"
#include <typeresolution.h>

//=====================================================================================================================
// S_OK - pAssembly contains type wszTypeName
// S_FALSE - pAssembly does not contain type wszTypeName
// 
HRESULT 
CLRPrivTypeCacheReflectionOnlyWinRT::ContainsType(
    ICLRPrivAssembly * pPrivAssembly, 
    LPCWSTR            wszTypeName)
{
    STANDARD_VM_CONTRACT;
    
    HRESULT hr = S_OK;
    
    AppDomain * pAppDomain = AppDomain::GetCurrentDomain();

    ReleaseHolder<PEAssembly> pPEAssembly;
    IfFailGo(pAppDomain->BindHostedPrivAssembly(nullptr, pPrivAssembly, nullptr, &pPEAssembly, TRUE));
    _ASSERTE(pPEAssembly != nullptr);
    
    {
        // Find DomainAssembly * (can be cached if this is too slow to call always)
        DomainAssembly * pDomainAssembly = pAppDomain->LoadDomainAssembly(
            nullptr,    // pIdentity
            pPEAssembly, 
            FILE_LOADED, 
            nullptr);   // pLoadSecurity
        
        // Convert the type name into namespace and type names in UTF8
        StackSString ssTypeNameWCHAR(wszTypeName);
        
        StackSString ssTypeName;
        ssTypeNameWCHAR.ConvertToUTF8(ssTypeName);
        LPUTF8 szTypeName = (LPUTF8)ssTypeName.GetUTF8NoConvert();
        
        LPCUTF8 szNamespace;
        LPCUTF8 szClassName;
        ns::SplitInline(szTypeName, szNamespace, szClassName);
        
        NameHandle typeName(szNamespace, szClassName);
        
        // Find the type in the assembly (use existing hash of all type names defined in the assembly)
        TypeHandle thType;
        mdToken    tkType;
        Module *   pTypeModule;
        mdToken    tkExportedType;
        if (pDomainAssembly->GetAssembly()->GetLoader()->FindClassModuleThrowing(
            &typeName, 
            &thType, 
            &tkType, 
            &pTypeModule, 
            &tkExportedType, 
            nullptr,    // ppClassHashEntry
            nullptr,    // pLookInThisModuleOnly
            Loader::DontLoad))
        {   // The type is present in the assembly
            hr = S_OK;
        }
        else
        {   // The type is not present in the assembly
            hr = S_FALSE;
        }
    }
    
ErrExit:
    return hr;
} // CLRPrivTypeCacheReflectionOnlyWinRT::ContainsType

//=====================================================================================================================
// Raises user event NamespaceResolveEvent to get a list of files for this namespace.
// 
void 
CLRPrivTypeCacheReflectionOnlyWinRT::RaiseNamespaceResolveEvent(
    LPCWSTR                                wszNamespace, 
    DomainAssembly *                       pParentAssembly, 
    CLRPrivBinderUtil::WStringListHolder * pFileNameList)
{
    STANDARD_VM_CONTRACT;
    
    _ASSERTE(pFileNameList != nullptr);
    
    AppDomain * pAppDomain = AppDomain::GetCurrentDomain();
    
    GCX_COOP();
    
    struct _gc {
        OBJECTREF AppDomainRef;
        OBJECTREF AssemblyRef;
        STRINGREF str;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    
    GCPROTECT_BEGIN(gc);
    if ((gc.AppDomainRef = pAppDomain->GetRawExposedObject()) != NULL)
    {
        if (pParentAssembly != nullptr)
        {
            gc.AssemblyRef = pParentAssembly->GetExposedAssemblyObject();
        }
        
        MethodDescCallSite onNamespaceResolve(METHOD__APP_DOMAIN__ON_REFLECTION_ONLY_NAMESPACE_RESOLVE, &gc.AppDomainRef);
        gc.str = StringObject::NewString(wszNamespace);
        ARG_SLOT args[3] =
        {
            ObjToArgSlot(gc.AppDomainRef),
            ObjToArgSlot(gc.AssemblyRef),
            ObjToArgSlot(gc.str)
        };
        PTRARRAYREF ResultingAssemblyArrayRef = (PTRARRAYREF) onNamespaceResolve.Call_RetOBJECTREF(args);
        if (ResultingAssemblyArrayRef != NULL)
        {
            for (DWORD i = 0; i < ResultingAssemblyArrayRef->GetNumComponents(); i++)
            {
                ASSEMBLYREF ResultingAssemblyRef = (ASSEMBLYREF) ResultingAssemblyArrayRef->GetAt(i);
                Assembly * pAssembly = ResultingAssemblyRef->GetAssembly();
                
                if (pAssembly->IsCollectible())
                {
                    COMPlusThrow(kNotSupportedException, W("NotSupported_CollectibleAssemblyResolve"));
                }
                
                PEAssembly * pPEAssembly = pAssembly->GetManifestFile();
                
                ICLRPrivAssembly * pPrivAssembly = pPEAssembly->GetHostAssembly();
                if ((pPrivAssembly == NULL) || !IsAfContentType_WindowsRuntime(pPEAssembly->GetFlags()))
                {
                    COMPlusThrow(kNotSupportedException, IDS_EE_REFLECTIONONLY_WINRT_INVALIDASSEMBLY);
                }
                
                pFileNameList->InsertTail(pPEAssembly->GetILimage()->GetPath());
            }
        }
    }
    GCPROTECT_END();
} // CLRPrivTypeCacheReflectionOnlyWinRT::RaiseNamespaceResolveEvent

//=====================================================================================================================
// Implementation of QCall System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMetadata.nResolveNamespace
// It's basically a PInvoke wrapper into Win8 API RoResolveNamespace
// 
void 
QCALLTYPE 
CLRPrivTypeCacheReflectionOnlyWinRT::ResolveNamespace(
    LPCWSTR                    wszNamespace, 
    LPCWSTR                    wszWindowsSdkPath, 
    LPCWSTR *                  rgPackageGraphPaths, 
    INT32                      cPackageGraphPaths, 
    QCall::ObjectHandleOnStack retFileNames)
{
    QCALL_CONTRACT;
    
    _ASSERTE(wszNamespace != nullptr);
    
    BEGIN_QCALL;
    
    CoTaskMemHSTRINGArrayHolder hFileNames;
    
    if (!WinRTSupported())
    {
        IfFailThrow(COR_E_PLATFORMNOTSUPPORTED);
    }
    
    {
        CLRPrivBinderUtil::HSTRINGArrayHolder rgPackageGraph;
        rgPackageGraph.Allocate(cPackageGraphPaths);
        
        LPCWSTR wszNamespaceRoResolve = wszNamespace;

        for (INT32 i = 0; i < cPackageGraphPaths; i++)
        {
            _ASSERTE(rgPackageGraph.GetRawArray()[i] == nullptr);
            WinRtString hsPackageGraphPath;
            IfFailThrow(hsPackageGraphPath.Initialize(rgPackageGraphPaths[i]));
            hsPackageGraphPath.Detach(&rgPackageGraph.GetRawArray()[i]);
        }

        UINT32 cchNamespace, cchWindowsSdkPath;
        IfFailThrow(StringCchLength(wszNamespace, &cchNamespace));
        IfFailThrow(StringCchLength(wszWindowsSdkPath, &cchWindowsSdkPath));
        
        DWORD     cFileNames = 0;
        HSTRING * rgFileNames = nullptr;
        HRESULT hr = RoResolveNamespace(
            WinRtStringRef(wszNamespace, cchNamespace),
            WinRtStringRef(wszWindowsSdkPath, cchWindowsSdkPath),
            rgPackageGraph.GetCount(), 
            rgPackageGraph.GetRawArray(), 
            &cFileNames, 
            &rgFileNames, 
            nullptr,    // pcDirectNamespaceChildren
            nullptr);   // rgDirectNamespaceChildren
        hFileNames.Init(rgFileNames, cFileNames);
        
        if (hr == HRESULT_FROM_WIN32(APPMODEL_ERROR_NO_PACKAGE))
        {   // User tried to resolve 3rd party namespace without passing package graph - throw InvalidOperationException with custom message
            _ASSERTE(cPackageGraphPaths == 0);
            COMPlusThrow(kInvalidOperationException, IDS_EE_REFLECTIONONLY_WINRT_LOADFAILURE_THIRDPARTY);
        }
        IfFailThrow(hr);
        if (hr != S_OK)
        {
            IfFailThrow(E_UNEXPECTED);
        }
    }
    
    {
        GCX_COOP();
        
        PTRARRAYREF orFileNames = NULL;
        GCPROTECT_BEGIN(orFileNames);
        
        orFileNames = (PTRARRAYREF) AllocateObjectArray(hFileNames.GetCount(), g_pStringClass);
        
        for (DWORD i = 0; i < hFileNames.GetCount(); i++)
        {
            UINT32  cchFileName = 0;

            HSTRING hsFileName = hFileNames.GetAt(i);
            LPCWSTR wszFileName;
            
            if (hsFileName != nullptr)
            {
                wszFileName = WindowsGetStringRawBuffer(
                    hsFileName, 
                    &cchFileName);
            
                STRINGREF str = StringObject::NewString(wszFileName);
                orFileNames->SetAt(i, str);
            }
        }
        
        retFileNames.Set(orFileNames);
        
        GCPROTECT_END();
    }
    
    END_QCALL;
} // CLRPrivTypeCacheReflectionOnlyWinRT::ResolveNamespace

//=====================================================================================================================

#endif //FEATURE_REFLECTION_ONLY_LOAD
#endif //!DACCESS_COMPILE
