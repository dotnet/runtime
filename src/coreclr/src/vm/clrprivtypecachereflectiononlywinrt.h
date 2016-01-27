// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 
// Contains VM implementation of code:ICLRPrivTypeCacheReflectionOnlyWinRT for code:CLRPrivBinderReflectionOnlyWinRT binder.
// 
//=====================================================================================================================

#ifdef FEATURE_HOSTED_BINDER
#ifdef FEATURE_REFLECTION_ONLY_LOAD

#pragma once

#include "internalunknownimpl.h"
#include "clrprivbinding.h"

//=====================================================================================================================
// Forward declarations
class DomainAssembly;

//=====================================================================================================================
class CLRPrivTypeCacheReflectionOnlyWinRT : 
    public IUnknownCommon<IUnknown>
{
public:
    //=============================================================================================
    // Class methods
    
    // S_OK - pAssembly contains type wszTypeName
    // S_FALSE - pAssembly does not contain type wszTypeName
    STDMETHOD(ContainsType)(
        ICLRPrivAssembly * pAssembly, 
        LPCWSTR            wszTypeName);
    
#ifndef DACCESS_COMPILE
    
    // Raises user event NamespaceResolveEvent to get a list of files for this namespace.
    void RaiseNamespaceResolveEvent(
        LPCWSTR                                wszNamespace, 
        DomainAssembly *                       pParentAssembly, 
        CLRPrivBinderUtil::WStringListHolder * pFileNameList);

#endif //!DACCESS_COMPILE
    
    // Implementation of QCall System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMetadata.nResolveNamespace
    // It's basically a PInvoke wrapper into Win8 API RoResolveNamespace
    static 
    void QCALLTYPE ResolveNamespace(
        LPCWSTR                    wszNamespace, 
        LPCWSTR                    wszWindowsSdkPath, 
        LPCWSTR *                  rgPackageGraphPaths, 
        INT32                      cPackageGraphPaths, 
        QCall::ObjectHandleOnStack retFileNames);
    
};  // class CLRPrivTypeCaheReflectionOnlyWinRT

#endif //FEATURE_REFLECTION_ONLY_LOAD
#endif // FEATURE_HOSTED_BINDER
