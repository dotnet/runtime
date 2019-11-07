// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// Contains VM implementation of WinRT type cache for code:CLRPrivBinderWinRT binder.
//
//=====================================================================================================================

#pragma once

#include "internalunknownimpl.h"
#include "clrprivbinding.h"

//=====================================================================================================================
class CLRPrivTypeCacheWinRT :
    public IUnknownCommon<IUnknown, IID_IUnknown>
{
public:
    //=============================================================================================
    // Class methods

    // S_OK - pAssembly contains type wszTypeName
    // S_FALSE - pAssembly does not contain type wszTypeName
    HRESULT ContainsType(
        ICLRPrivAssembly * pAssembly,
        LPCWSTR            wszTypeName);

    // S_OK - pAssembly contains type wszTypeName
    // S_FALSE - pAssembly does not contain type wszTypeName
    // E_FAIL - assembly is not loaded
    HRESULT ContainsTypeIfLoaded(
        PTR_AppDomain        pAppDomain,
        PTR_ICLRPrivAssembly pPrivAssembly,
        LPCUTF8              szNamespace,
        LPCUTF8              szClassName,
        PTR_Assembly *       ppAssembly);

    static CLRPrivTypeCacheWinRT * GetOrCreateTypeCache();

#ifndef DACCESS_COMPILE

#ifndef CROSSGEN_COMPILE
    // Raises user event DesignerNamespaceResolveEvent to get a list of files for this namespace.
    void RaiseDesignerNamespaceResolveEvent(
        LPCWSTR                                wszNamespace,
        CLRPrivBinderUtil::WStringListHolder * pFileNameList);
#endif // CROSSGEN_COMPILE

#endif //!DACCESS_COMPILE

private:
    //=============================================================================================
    // Private methods

    // Checks if the type (szNamespace/szClassName) is present in the assembly pAssembly.
    HRESULT ContainsTypeHelper(
        PTR_Assembly pAssembly,
        LPCUTF8      szNamespace,
        LPCUTF8      szClassName);

    //=============================================================================================
    // Class fields

    static CLRPrivTypeCacheWinRT * s_pSingleton;

};  // class CLRPrivTypeCaheWinRT

typedef DPTR(CLRPrivTypeCacheWinRT) PTR_CLRPrivTypeCacheWinRT;
