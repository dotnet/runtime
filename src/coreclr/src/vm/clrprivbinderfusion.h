// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#pragma once

#include "holder.h"
#include "internalunknownimpl.h"
#include "shash.h"
#include "clrprivbinding.h"
#include "clrprivruntimebinders.h"

//=====================================================================================================================
// Forward declarations
class CLRPrivBinderFusion;
class CLRPrivAssemblyFusion;

class PEAssembly;
class DomainAssembly;
struct IMDInternalImport;

//=====================================================================================================================
class CLRPrivBinderFusion :
    public IUnknownCommon<ICLRPrivBinder, IBindContext>
{
    friend class CLRPrivAssemblyFusion;
    
public:
    // Scope for the bind operation
    enum BindingScope
    {
        // Binds only to subset of framework that is not on the black list (non-FX assemblies bindings are rejected)
        kBindingScope_FrameworkSubset, 
        // Binds to all framework assemblies (incl. those on the black list) (non-FX assemblies bindings are rejected)
        // Used by designer binding context and in DevMode
        kBindingScope_FrameworkAll
    };
    
public:
    //=============================================================================================
    // ICLRPrivBinder methods

    //---------------------------------------------------------------------------------------------
    // Implements code:ICLRPrivBinder::BindAssemblyByName
    STDMETHOD(BindAssemblyByName(
        IAssemblyName * pAssemblyName,
        ICLRPrivAssembly ** ppAssembly));

    //---------------------------------------------------------------------------------------------
    // Implements code:ICLRPrivBinder::VerifyBind
    STDMETHOD(VerifyBind)(
        IAssemblyName *pAssemblyName,
        ICLRPrivAssembly *pAssembly,
        ICLRPrivAssemblyInfo *pAssemblyInfo)
    {
        LIMITED_METHOD_CONTRACT;
        if (pAssemblyName == nullptr || pAssembly == nullptr || pAssemblyInfo == nullptr)
            return E_INVALIDARG;
        return S_OK;
    }

    //---------------------------------------------------------------------------------------------
    // Implements code:ICLRPrivBinder::GetBinderFlags
    STDMETHOD(GetBinderFlags)(
        DWORD *pBinderFlags)
    {
        LIMITED_METHOD_CONTRACT;
        *pBinderFlags = BINDER_FINDASSEMBLYBYSPEC_REQUIRES_EXACT_MATCH;
        return S_OK;
    }

    //---------------------------------------------------------------------------------------------
    // Implements code:ICLRPrivBinder::GetBinderID
    STDMETHOD(GetBinderID)(
        UINT_PTR *pBinderId);

    STDMETHOD(FindAssemblyBySpec)(
        LPVOID pvAppDomain,
        LPVOID pvAssemblySpec,
        HRESULT * pResult,
        ICLRPrivAssembly ** ppAssembly);

    //=============================================================================================
    // IBindContext methods

    // Implements code:IBindContext::PreBind
    STDMETHOD(PreBind)(
        IAssemblyName  *pIAssemblyName,
        DWORD           dwPreBindFlags,
        IBindResult   **ppIBindResult);

    // Implements code:IBindContext::IsDefaultContext
    STDMETHOD(IsDefaultContext)();

    //=============================================================================================
    // Class methods

    //---------------------------------------------------------------------------------------------
    CLRPrivBinderFusion()
        : m_SetHostAssemblyLock(CrstLeafLock)
    { STANDARD_VM_CONTRACT; }
    
    //---------------------------------------------------------------------------------------------
    ~CLRPrivBinderFusion();

    //---------------------------------------------------------------------------------------------
    HRESULT FindFusionAssemblyBySpec(
        LPVOID pvAppDomain,
        LPVOID pvAssemblySpec,
        BindingScope kBindingScope,
        HRESULT * pResult,
        ICLRPrivAssembly ** ppAssembly);

    //---------------------------------------------------------------------------------------------
    HRESULT BindFusionAssemblyByName(
        IAssemblyName *     pAssemblyName, 
        BindingScope        kBindingScope, 
        ICLRPrivAssembly ** ppAssembly);

    //---------------------------------------------------------------------------------------------
    HRESULT PreBindFusionAssemblyByName(
        IAssemblyName * pIAssemblyName,
        DWORD           dwPreBindFlags,
        IBindResult **  ppIBindResult);
    
    //---------------------------------------------------------------------------------------------
    // Binds mscorlib.dll
    void BindMscorlib(
        PEAssembly * pPEAssembly);
    
private:
    //---------------------------------------------------------------------------------------------
    HRESULT BindAssemblyByNameWorker(
        IAssemblyName *     pAssemblyName, 
        ICLRPrivAssembly ** ppAssembly);
    
private:
    //---------------------------------------------------------------------------------------------
    // This lock is used to serialize assigning ICLRPrivAssembly instances to PEAssembly objects.
    Crst              m_SetHostAssemblyLock;

};  // class CLRPrivBinderFusion

//=====================================================================================================================
class CLRPrivAssemblyFusion :
    public IUnknownCommon<ICLRPrivAssembly>
{
public:
    //---------------------------------------------------------------------------------------------
    CLRPrivAssemblyFusion(
        LPCWSTR               wszName, 
        CLRPrivBinderFusion * pBinder);
    
    //---------------------------------------------------------------------------------------------
    LPCWSTR GetName() const;
    
    //---------------------------------------------------------------------------------------------
    // Implements code:IUnknown::Release
    STDMETHOD_(ULONG, Release)();
    
    //---------------------------------------------------------------------------------------------
    // Implements code:ICLRPrivBinder::BindAssemblyByName
    STDMETHOD(BindAssemblyByName)(
        IAssemblyName * pAssemblyName,
        ICLRPrivAssembly ** ppAssembly);

    //---------------------------------------------------------------------------------------------
    // Implements code:ICLRPrivAssembly::IsShareable
    STDMETHOD(IsShareable)(
        BOOL * pbIsShareable);

    //---------------------------------------------------------------------------------------------
    // Implements code:ICLRPrivAssembly::GetAvailableImageTypes
    STDMETHOD(GetAvailableImageTypes)(
        LPDWORD pdwImageTypes);

    //---------------------------------------------------------------------------------------------
    // Implements code:ICLRPrivAssembly::GetImageResource
    STDMETHOD(GetImageResource)(
        DWORD dwImageType,
        DWORD *pdwImageType,
        ICLRPrivResource ** ppIResource);

    //---------------------------------------------------------------------------------------------
    // Implements code:ICLRPrivBinder::VerifyBind
    STDMETHOD(VerifyBind)(
        IAssemblyName *pAssemblyName,
        ICLRPrivAssembly *pAssembly,
        ICLRPrivAssemblyInfo *pAssemblyInfo)
    {
        WRAPPER_NO_CONTRACT;
        return m_pBinder->VerifyBind(pAssemblyName, pAssembly, pAssemblyInfo);
    }

    //---------------------------------------------------------------------------------------------
    // Implements code:ICLRPrivBinder::GetBinderFlags
    STDMETHOD(GetBinderFlags)(
        DWORD *pBinderFlags)
    {
        LIMITED_METHOD_CONTRACT;
        return m_pBinder->GetBinderFlags(pBinderFlags);
    }

    //---------------------------------------------------------------------------------------------
    // Implements code:ICLRPrivBinder::GetBinderID
    STDMETHOD(GetBinderID)(
        UINT_PTR *pBinderId);

    //---------------------------------------------------------------------------------------------
    // Implements code:ICLRPrivBinder::FindAssemblyBySpec
    STDMETHOD(FindAssemblyBySpec)(
        LPVOID pvAppDomain,
        LPVOID pvAssemblySpec,
        HRESULT * pResult,
        ICLRPrivAssembly ** ppAssembly)
    { STATIC_CONTRACT_WRAPPER; return m_pBinder->FindAssemblyBySpec(pvAppDomain, pvAssemblySpec, pResult, ppAssembly); }

protected:
    //---------------------------------------------------------------------------------------------
    // The fusion binder. Need to keep it around as long as this object is around.
    ReleaseHolder<CLRPrivBinderFusion> m_pBinder;
    
    // Full display name of the assembly - used to avoid duplicate CLRPrivAssemblyFusion objects
    NewArrayHolder<WCHAR> m_wszName;
    
};  // class CLRPrivAssemblyFusion
