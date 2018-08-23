// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#pragma once

#include "holder.h"
#include "internalunknownimpl.h"
#include "clrprivbinding.h"
#include "clrprivruntimebinders.h"
#include "clrprivbinderfusion.h"
#include "clrprivbinderutil.h"

class PEAssembly;

//=====================================================================================================================
class CLRPrivBinderLoadFile : 
    public IUnknownCommon<ICLRPrivBinder>
{

public:
    //=============================================================================================
    // ICLRPrivBinder methods

    //---------------------------------------------------------------------------------------------
    STDMETHOD(BindAssemblyByName)(
        IAssemblyName * pAssemblyName,
        ICLRPrivAssembly ** ppAssembly);

    //---------------------------------------------------------------------------------------------
    STDMETHOD(VerifyBind)(
        IAssemblyName *pAssemblyName,
        ICLRPrivAssembly *pAssembly,
        ICLRPrivAssemblyInfo *pAssemblyInfo);

    //---------------------------------------------------------------------------------------------
    STDMETHOD(GetBinderFlags)(
        DWORD *pBinderFlags)
    {
        LIMITED_METHOD_CONTRACT;
        *pBinderFlags = BINDER_NONE;
        return S_OK;
    }

    //---------------------------------------------------------------------------------------------
    STDMETHOD(GetBinderID)(
        UINT_PTR *pBinderId);

    //---------------------------------------------------------------------------------------------
    // FindAssemblyBySpec is not supported by this binder.    
    STDMETHOD(FindAssemblyBySpec)(
        LPVOID pvAppDomain,
        LPVOID pvAssemblySpec,
        HRESULT * pResult,
        ICLRPrivAssembly ** ppAssembly)
    { STATIC_CONTRACT_WRAPPER; return E_FAIL; }

    STDMETHOD(GetLoaderAllocator)(
        /* [retval][out] */ LoaderAllocator** pLoaderAllocator)
    { STATIC_CONTRACT_WRAPPER; return E_FAIL; }

    //=============================================================================================
    // Class methods
    //---------------------------------------------------------------------------------------------
    STDMETHOD(BindAssemblyExplicit)(
        PEImage* pImage,
        IAssemblyName **ppAssemblyName,
        ICLRPrivAssembly ** ppAssembly);

    //---------------------------------------------------------------------------------------------
    static
    CLRPrivBinderLoadFile * GetOrCreateBinder();

    //---------------------------------------------------------------------------------------------
    ~CLRPrivBinderLoadFile();

private:
    //---------------------------------------------------------------------------------------------
    CLRPrivBinderLoadFile();

    //---------------------------------------------------------------------------------------------
    ReleaseHolder<CLRPrivBinderFusion> m_pFrameworkBinder;

    //---------------------------------------------------------------------------------------------
    static CLRPrivBinderLoadFile * s_pSingleton;
};

class CLRPrivAssemblyLoadFile :
    public IUnknownCommon<ICLRPrivAssembly>
{
protected:
    ReleaseHolder<CLRPrivBinderLoadFile> m_pBinder;
    ReleaseHolder<CLRPrivBinderFusion> m_pFrameworkBinder;
    ReleaseHolder<CLRPrivBinderUtil::CLRPrivResourcePathImpl> m_pPathResource;

public:
    //---------------------------------------------------------------------------------------------
    CLRPrivAssemblyLoadFile(
        CLRPrivBinderLoadFile* pBinder,
        CLRPrivBinderFusion* pFrameworkBinder,
        CLRPrivBinderUtil::CLRPrivResourcePathImpl* pPathResource);

    //=============================================================================================
    // ICLRPrivAssembly methods

    //---------------------------------------------------------------------------------------------
    STDMETHOD(BindAssemblyByName)(
        IAssemblyName * pAssemblyName,
        ICLRPrivAssembly ** ppAssembly);

    //---------------------------------------------------------------------------------------------
    STDMETHOD(VerifyBind)(
        IAssemblyName *pAssemblyName,
        ICLRPrivAssembly *pAssembly,
        ICLRPrivAssemblyInfo *pAssemblyInfo);

    //---------------------------------------------------------------------------------------------
    STDMETHOD(GetBinderFlags)(
        DWORD *pBinderFlags)
    {
        LIMITED_METHOD_CONTRACT;
        return m_pBinder->GetBinderFlags(pBinderFlags);
    }

    //---------------------------------------------------------------------------------------------
    STDMETHOD(GetBinderID)(
        UINT_PTR *pBinderId);

    //---------------------------------------------------------------------------------------------
    STDMETHOD(IsShareable)(
        BOOL * pbIsShareable);

    //---------------------------------------------------------------------------------------------
    STDMETHOD(GetAvailableImageTypes)(
        LPDWORD pdwImageTypes);

    //---------------------------------------------------------------------------------------------
    STDMETHOD(GetImageResource)(
        DWORD dwImageType,
        DWORD *pdwImageType,
        ICLRPrivResource ** ppIResource);

    //---------------------------------------------------------------------------------------------
    STDMETHOD(FindAssemblyBySpec)(
        LPVOID pvAppDomain,
        LPVOID pvAssemblySpec,
        HRESULT * pResult,
        ICLRPrivAssembly ** ppAssembly)
    { STATIC_CONTRACT_WRAPPER; return m_pBinder->FindAssemblyBySpec(pvAppDomain, pvAssemblySpec, pResult, ppAssembly); }

    //---------------------------------------------------------------------------------------------
    STDMETHOD(GetLoaderAllocator)(
        LoaderAllocator** pLoaderAllocator)
    {
        WRAPPER_NO_CONTRACT;
        return m_pBinder->GetLoaderAllocator(pLoaderAllocator);
    }
};
