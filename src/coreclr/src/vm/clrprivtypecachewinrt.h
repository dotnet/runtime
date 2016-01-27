// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 

// 
// Contains VM implementation of WinRT type cache for code:CLRPrivBinderWinRT binder.
// 
//=====================================================================================================================

#ifdef FEATURE_HOSTED_BINDER

#pragma once

#include "internalunknownimpl.h"
#include "clrprivbinding.h"

#ifdef CLR_STANDALONE_BINDER
typedef HRESULT (*ContainsTypeFnPtr)(
	IUnknown         * object,
    ICLRPrivAssembly * pAssembly, 
    LPCWSTR            wszTypeName);

// CLRPrivTypeCacheWinRT proxy object for use by the mdilbind assembly binder.
class CLRPrivTypeCacheWinRT : public IUnknownCommon<IUnknown>
{
	ReleaseHolder<IUnknown> m_actualCacheObject;
	ContainsTypeFnPtr m_containsTypeFunction;
public:
	CLRPrivTypeCacheWinRT(IUnknown *object, ContainsTypeFnPtr containsTypeFunction)
	{
		m_actualCacheObject =  clr::SafeAddRef(object);
		m_containsTypeFunction = containsTypeFunction;
	}
    //=============================================================================================
    // Class methods
    
    // S_OK - pAssembly contains type wszTypeName
    // S_FALSE - pAssembly does not contain type wszTypeName
    HRESULT ContainsType(
        ICLRPrivAssembly * pAssembly, 
        LPCWSTR            wszTypeName)
	{
		return m_containsTypeFunction(m_actualCacheObject, pAssembly, wszTypeName);
	}
};
#else
//=====================================================================================================================
class CLRPrivTypeCacheWinRT : 
    public IUnknownCommon<IUnknown>
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
#endif
typedef DPTR(CLRPrivTypeCacheWinRT) PTR_CLRPrivTypeCacheWinRT;

#endif // FEATURE_HOSTED_BINDER
