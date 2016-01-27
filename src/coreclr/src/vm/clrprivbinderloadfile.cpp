// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#include "common.h" // precompiled header
#include "clrprivbinderutil.h"
#include "clrprivbinderloadfile.h"
#include "clrprivbinderfusion.h"
#include "clrprivbinderappx.h"
#include "fusionlogging.h"

using namespace CLRPrivBinderUtil;

#ifndef DACCESS_COMPILE

using namespace CLRPrivBinderUtil;
using clr::SafeAddRef;
using clr::SafeRelease;

//=====================================================================================================================
CLRPrivBinderLoadFile * CLRPrivBinderLoadFile::s_pSingleton = nullptr;


//=====================================================================================================================
CLRPrivBinderLoadFile * CLRPrivBinderLoadFile::GetOrCreateBinder()
{
    STANDARD_VM_CONTRACT;
    HRESULT hr = S_OK;

    if (s_pSingleton == nullptr)
    {
        ReleaseHolder<CLRPrivBinderLoadFile> pBinder = SafeAddRef(new CLRPrivBinderLoadFile());
        
        CLRPrivBinderAppX *   pAppXBinder = CLRPrivBinderAppX::GetOrCreateBinder();
        CLRPrivBinderFusion * pFusionBinder = pAppXBinder->GetFusionBinder();
        
        pBinder->m_pFrameworkBinder = SafeAddRef(pFusionBinder);
        _ASSERTE(pBinder->m_pFrameworkBinder != nullptr);
        
        if (InterlockedCompareExchangeT<decltype(s_pSingleton)>(&s_pSingleton, pBinder, nullptr) == nullptr)
            pBinder.SuppressRelease();
    }
    
    return s_pSingleton;
}

//=====================================================================================================================
CLRPrivBinderLoadFile::CLRPrivBinderLoadFile()
{
    STANDARD_VM_CONTRACT;
}

//=====================================================================================================================
CLRPrivBinderLoadFile::~CLRPrivBinderLoadFile()
{
    WRAPPER_NO_CONTRACT;
}

//=====================================================================================================================
STDMETHODIMP CLRPrivBinderLoadFile::BindAssemblyExplicit(
        PEImage* pImage,
        IAssemblyName **ppAssemblyName,
        ICLRPrivAssembly ** ppAssembly)
{
    STANDARD_BIND_CONTRACT;
    PRECONDITION(AppDomain::GetCurrentDomain()->IsDefaultDomain());
    VALIDATE_ARG_RET(pImage != nullptr);
    VALIDATE_ARG_RET(ppAssemblyName != nullptr);
    VALIDATE_ARG_RET(ppAssembly != nullptr);

    HRESULT hr = S_OK;

    fusion::logging::StatusScope logStatus(0, ID_FUSLOG_BINDING_STATUS_LOAD_FILE, &hr);

    ReleaseHolder<IAssemblyName> pAssemblyName;
    ReleaseHolder<ICLRPrivAssembly> pAssembly;

    EX_TRY
    {
        // check if a framework assembly
        {
            AssemblySpec spec;
            mdAssembly a;
            IfFailThrow(pImage->GetMDImport()->GetAssemblyFromScope(&a));
            spec.InitializeSpec(a, pImage->GetMDImport(), NULL, false);
            IfFailThrow(spec.CreateFusionName(&pAssemblyName));
        }

        hr = IfTransientFailThrow(m_pFrameworkBinder->BindFusionAssemblyByName(
                pAssemblyName, 
                CLRPrivBinderFusion::kBindingScope_FrameworkSubset, 
                &pAssembly));
        if (FAILED(hr)) // not a Framework assembly
        {
            ReleaseHolder<CLRPrivResourcePathImpl> pPathResource =
                clr::SafeAddRef(new CLRPrivResourcePathImpl(pImage->GetPath().GetUnicode()));
            pAssembly = clr::SafeAddRef(new CLRPrivAssemblyLoadFile(this, m_pFrameworkBinder, pPathResource));

            hr = S_OK;
        }
    }
    EX_CATCH_HRESULT(hr);

    if (SUCCEEDED(hr))
    {
        *ppAssemblyName = pAssemblyName.Extract();
        *ppAssembly = pAssembly.Extract();
    }

    return hr;
};

//=====================================================================================================================
STDMETHODIMP CLRPrivBinderLoadFile::BindAssemblyByName(
    IAssemblyName * pAssemblyName,
    ICLRPrivAssembly ** ppAssembly)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(!"No parentless binds are allowed in NEITHER context");
    return E_NOTIMPL;
};

//=====================================================================================================================
STDMETHODIMP CLRPrivBinderLoadFile::VerifyBind(
    IAssemblyName *pAssemblyName,
    ICLRPrivAssembly *pAssembly,
    ICLRPrivAssemblyInfo *pAssemblyInfo)
{
    WRAPPER_NO_CONTRACT;
    // TODO: move ublic Key verification here, once we are willing to fully convert to hosted model
    _ASSERTE(!"CLRPrivAssemblyLoadFile::VerifyBind");
    return E_NOTIMPL; // we don't care about anything here...
};

//=====================================================================================================================
HRESULT CLRPrivBinderLoadFile::GetBinderID(
    UINT_PTR *pBinderId)
{
    LIMITED_METHOD_CONTRACT;

    *pBinderId = (UINT_PTR)this;
    return S_OK;
}

//==========================================================================
CLRPrivAssemblyLoadFile::CLRPrivAssemblyLoadFile(
    CLRPrivBinderLoadFile* pBinder,
    CLRPrivBinderFusion* pFrameworkBinder,
    CLRPrivBinderUtil::CLRPrivResourcePathImpl* pPathResource)
    : m_pBinder(SafeAddRef(pBinder))
    , m_pFrameworkBinder(SafeAddRef(pFrameworkBinder))
    , m_pPathResource(SafeAddRef(pPathResource))
{
    STANDARD_VM_CONTRACT;
    VALIDATE_ARG_THROW(pBinder != nullptr);
    VALIDATE_ARG_THROW(pFrameworkBinder != nullptr);
    VALIDATE_ARG_THROW(pPathResource != nullptr);
}

//=====================================================================================================================
STDMETHODIMP CLRPrivAssemblyLoadFile::BindAssemblyByName(
    IAssemblyName * pAssemblyName,
    ICLRPrivAssembly ** ppAssembly)
{
    STANDARD_BIND_CONTRACT;
    HRESULT hr = S_OK;

    EX_TRY
    {
        hr = m_pFrameworkBinder->BindFusionAssemblyByName(
                pAssemblyName, 
                CLRPrivBinderFusion::kBindingScope_FrameworkSubset, 
                ppAssembly);
        if (FAILED(hr) && !Exception::IsTransient(hr)) // not a Framework assembly
        {
            *ppAssembly = RaiseAssemblyResolveEvent(pAssemblyName, this);
            if (*ppAssembly == NULL)
            {
                hr = COR_E_FILENOTFOUND;
            }
            else
            {
                (*ppAssembly)->AddRef();
                hr = S_OK;
            }
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
};

//=====================================================================================================================
HRESULT CLRPrivAssemblyLoadFile::GetBinderID(
    UINT_PTR *pBinderId)
{
    LIMITED_METHOD_CONTRACT;
    return m_pBinder->GetBinderID(pBinderId);
}

//=====================================================================================================================
STDMETHODIMP CLRPrivAssemblyLoadFile::VerifyBind(
    IAssemblyName *pAssemblyName,
    ICLRPrivAssembly *pAssembly,
    ICLRPrivAssemblyInfo *pAssemblyInfo)
{
    STANDARD_BIND_CONTRACT;
    return S_OK;
};

//=====================================================================================================================
HRESULT CLRPrivAssemblyLoadFile::IsShareable(
    BOOL * pbIsShareable)
{
    LIMITED_METHOD_CONTRACT;

    VALIDATE_ARG_RET(pbIsShareable != nullptr);

    *pbIsShareable = FALSE;   // no sharing for loadfile
    return S_OK;
}

//=====================================================================================================================
HRESULT CLRPrivAssemblyLoadFile::GetAvailableImageTypes(
    LPDWORD pdwImageTypes)
{
    LIMITED_METHOD_CONTRACT;
    VALIDATE_ARG_RET(pdwImageTypes != nullptr);
    PRECONDITION(m_pPathResource != nullptr);

    *pdwImageTypes = ASSEMBLY_IMAGE_TYPE_IL;

    return S_OK;
}


//=====================================================================================================================
HRESULT CLRPrivAssemblyLoadFile::GetImageResource(
    DWORD dwImageType,
    DWORD *pdwImageType,
    ICLRPrivResource ** ppIResource)
{
    LIMITED_METHOD_CONTRACT;
    VALIDATE_ARG_RET(pdwImageType != nullptr);
    VALIDATE_ARG_RET(ppIResource != nullptr);

    *ppIResource = nullptr;

    HRESULT hr = S_OK;
    if ((dwImageType & ASSEMBLY_IMAGE_TYPE_NATIVE) == ASSEMBLY_IMAGE_TYPE_NATIVE)
    {
        return CLR_E_BIND_IMAGE_UNAVAILABLE;
    }

    *pdwImageType = ASSEMBLY_IMAGE_TYPE_IL;
    *ppIResource = clr::SafeAddRef(m_pPathResource);

    return S_OK;
}

#endif // !DACCESS_COMPILE

