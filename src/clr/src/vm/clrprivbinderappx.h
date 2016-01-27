// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#pragma once

#include "holder.h"
#include "internalunknownimpl.h"
#include "shash.h"
#include "fusion.h"
#include "clrprivbinding.h"
#include "clrprivruntimebinders.h"
#include "clrprivbinderfusion.h"
#include "clrprivbinderwinrt.h"

//=====================================================================================================================
// Forward declarations
class CLRPrivBinderAppX;
class CLRPrivAssemblyAppX;

class DomainAssembly;

// Forward declaration of helper class used in native image binding.
class CLRPrivAssemblyAppX_NIWrapper;

typedef DPTR(CLRPrivBinderAppX) PTR_CLRPrivBinderAppX;

//=====================================================================================================================
class CLRPrivBinderAppX : 
    public IUnknownCommon<ICLRPrivBinder, IBindContext, ICLRPrivWinRtTypeBinder>
{
    friend class CLRPrivAssemblyAppX;

public:
    //=============================================================================================
    // ICLRPrivBinder methods

    // Implements code:ICLRPrivBinder::BindAssemblyByName
    STDMETHOD(BindAssemblyByName)(
        IAssemblyName * pAssemblyName,
        ICLRPrivAssembly ** ppAssembly);

    // Implements code:ICLRPrivBinder::VerifyBind
    STDMETHOD(VerifyBind)(
        IAssemblyName *pAssemblyName,
        ICLRPrivAssembly *pAssembly,
        ICLRPrivAssemblyInfo *pAssemblyInfo);

    // Implements code:ICLRPrivBinder::GetBinderFlags
    STDMETHOD(GetBinderFlags)(
        DWORD *pBinderFlags)
    {
        LIMITED_METHOD_CONTRACT;

        if (pBinderFlags == NULL)
            return E_INVALIDARG;

        *pBinderFlags = m_pParentBinder != NULL ? BINDER_DESIGNER_BINDING_CONTEXT : BINDER_NONE;
        return S_OK;
    }

    // Implements code:ICLRPrivBinder::GetBinderID
    STDMETHOD(GetBinderID)(
        UINT_PTR *pBinderId);
    
    STDMETHOD(FindAssemblyBySpec)(
        LPVOID pvAppDomain,
        LPVOID pvAssemblySpec,
        HRESULT * pResult,
        ICLRPrivAssembly ** ppAssembly);

    //=============================================================================================
    // ICLRPrivWinRtTypeBinder methods
    
    // Implements code:ICLRPrivWinRtTypeBinder::FindAssemblyForWinRtTypeIfLoaded
    STDMETHOD_(void *, FindAssemblyForWinRtTypeIfLoaded)(
        void *  pAppDomain, 
        LPCUTF8 szNamespace, 
        LPCUTF8 szClassName);
    
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
    static
    CLRPrivBinderAppX * GetOrCreateBinder();
    
    static 
    PTR_CLRPrivBinderAppX GetBinderOrNull()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return s_pSingleton;
    }
    
    static 
    CLRPrivBinderAppX * CreateParentedBinder(
        ICLRPrivBinder *        pParentBinder, 
        CLRPrivTypeCacheWinRT * pWinRtTypeCache, 
        LPCWSTR *               rgwzAltPath, 
        UINT                    cAltPaths,
        BOOL                    fCanUseNativeImages);
    
    //---------------------------------------------------------------------------------------------
    ~CLRPrivBinderAppX();

    //---------------------------------------------------------------------------------------------
    enum AppXBindFlags
    {
        ABF_BindIL      = 1,
        ABF_BindNI      = 2,
        ABF_Default     = ABF_BindIL | ABF_BindNI,
    };

    //---------------------------------------------------------------------------------------------
    CLRPrivBinderFusion * GetFusionBinder()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pFusionBinder;
    }
    
    PTR_CLRPrivBinderWinRT GetWinRtBinder()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pWinRTBinder;
    }
    
private:
    //---------------------------------------------------------------------------------------------
    // Binds within AppX packages only. BindAssemblyByName takes care of delegating to Fusion
    // when needed.
    HRESULT BindAppXAssemblyByNameWorker(
        IAssemblyName * pIAssemblyName,
        DWORD dwAppXBindFlags,
        CLRPrivAssemblyAppX ** ppAssembly);

    //---------------------------------------------------------------------------------------------
    // Binds within AppX packages only. BindAssemblyByName takes care of delegating to Fusion
    // when needed.
    HRESULT BindAppXAssemblyByName(
        IAssemblyName * pIAssemblyName,
        DWORD dwAppXBindFlags,
        ICLRPrivAssembly ** ppPrivAssembly);

    //---------------------------------------------------------------------------------------------
    // Binds within AppX packages only. PreBindAssemblyByName takes care of delegating to Fusion
    // when needed.
    HRESULT PreBindAppXAssemblyByName(
        IAssemblyName * pIAssemblyName,
        DWORD dwAppXBindFlags,
        IBindResult ** ppIBindResult);

    //---------------------------------------------------------------------------------------------
    UINT_PTR GetBinderID();

    //---------------------------------------------------------------------------------------------
    CLRPrivBinderAppX(LPCWSTR *rgwzAltPath, UINT cAltPaths);

    //---------------------------------------------------------------------------------------------
    HRESULT CheckGetAppXRT();

    //---------------------------------------------------------------------------------------------
    HRESULT CacheBindResult(
        CLRPrivBinderUtil::AssemblyIdentity *   pIdentity,
        HRESULT                                 hrResult);
    
    //---------------------------------------------------------------------------------------------
    CLRPrivBinderFusion::BindingScope GetFusionBindingScope();

    //---------------------------------------------------------------------------------------------
    Crst              m_MapReadLock;
    Crst              m_MapWriteLock;

    //---------------------------------------------------------------------------------------------
    // Identity to CLRPrivBinderAppX map
    struct NameToAssemblyMapTraits : public StringSHashTraits<CLRPrivAssemblyAppX, WCHAR, CaseInsensitiveStringCompareHash<WCHAR> >
    {
        static LPCWSTR GetKey(CLRPrivAssemblyAppX *pAssemblyAppX);
    };
    typedef SHash<NameToAssemblyMapTraits> NameToAssemblyMap;

    NameToAssemblyMap m_NameToAssemblyMap;

    //---------------------------------------------------------------------------------------------
    // Binding record map, used by cache lookup requests.
    struct BindingRecord
    {
        // This stores the result of the original bind request.
        HRESULT hr;
    };

    struct BindingRecordMapTraits : public MapSHashTraits<CLRPrivBinderUtil::AssemblyIdentity*, BindingRecord>
    {
        typedef MapSHashTraits<CLRPrivBinderUtil::AssemblyIdentity*, BindingRecord> base_t;
        typedef base_t::element_t element_t;
        typedef base_t::count_t count_t;
        typedef base_t::key_t;

        static count_t Hash(key_t k) 
        {
            return HashiString(k->Name);
        }

        static BOOL Equals(key_t k1, key_t k2)
        {
            return SString::_wcsicmp(k1->Name, k2->Name) == 0;
        }

        static const bool s_DestructPerEntryCleanupAction = true;
        static inline void OnDestructPerEntryCleanupAction(element_t const & e)
        {
            delete [] e.Key();
        }
    };
    typedef SHash<BindingRecordMapTraits> BindingRecordMap;

    BindingRecordMap m_BindingRecordMap;

    //---------------------------------------------------------------------------------------------
    NewArrayHolder< NewArrayHolder<WCHAR> > m_rgAltPathsHolder;
    NewArrayHolder< WCHAR* >                m_rgAltPaths;
    UINT                                    m_cAltPaths;

#ifdef FEATURE_FUSION
    BOOL                                    m_fCanUseNativeImages;
    ReleaseHolder<IILFingerprintFactory>    m_pFingerprintFactory;
#endif

    //---------------------------------------------------------------------------------------------
    // ParentBinder is set only in designer binding context (forms a chain of binders)
    ReleaseHolder<ICLRPrivBinder>      m_pParentBinder;
    
    ReleaseHolder<CLRPrivBinderFusion> m_pFusionBinder;
    PTR_CLRPrivBinderWinRT             m_pWinRTBinder;
    
    //---------------------------------------------------------------------------------------------
    //static CLRPrivBinderAppX * s_pSingleton;
    SPTR_DECL(CLRPrivBinderAppX, s_pSingleton);

    //---------------------------------------------------------------------------------------------
    // Cache the binding scope in the constructor so that there is no need to call into a WinRT
    // API in a GC_NOTRIGGER scope later on.
    CLRPrivBinderFusion::BindingScope   m_fusionBindingScope;
};  // class CLRPrivBinderAppX


//=====================================================================================================================
class CLRPrivAssemblyAppX :
    public IUnknownCommon<ICLRPrivAssembly>
{
    friend class CLRPrivBinderAppX;

public:
    //---------------------------------------------------------------------------------------------
    CLRPrivAssemblyAppX(
        CLRPrivBinderUtil::AssemblyIdentity * pIdentity,
        CLRPrivBinderAppX *pBinder,
        ICLRPrivResource *pIResourceIL,
        IBindResult * pIBindResult);

    //---------------------------------------------------------------------------------------------
    ~CLRPrivAssemblyAppX();

    //---------------------------------------------------------------------------------------------
    // Implements code:IUnknown::Release
    STDMETHOD_(ULONG, Release)();

    //---------------------------------------------------------------------------------------------
    LPCWSTR GetSimpleName() const;

    //=============================================================================================
    // ICLRPrivBinder interface methods
    
    // Implements code:ICLRPrivBinder::BindAssemblyByName
    STDMETHOD(BindAssemblyByName)(
        IAssemblyName * pAssemblyName,
        ICLRPrivAssembly ** ppAssembly);

    // Implements code:ICLRPrivBinder::VerifyBind
    STDMETHOD(VerifyBind)(
        IAssemblyName *pAssemblyName,
        ICLRPrivAssembly *pAssembly,
        ICLRPrivAssemblyInfo *pAssemblyInfo)
    {
        STANDARD_BIND_CONTRACT;

        HRESULT hr = S_OK;

        VALIDATE_PTR_RET(pAssemblyName);
        VALIDATE_PTR_RET(pAssembly);
        VALIDATE_PTR_RET(pAssemblyInfo);

        // Re-initialize the assembly identity with full identity contained in metadata.
        IfFailRet(m_pIdentity->Initialize(pAssemblyInfo));

        return m_pBinder->VerifyBind(pAssemblyName, pAssembly, pAssemblyInfo);
    }

    // Implements code:ICLRPrivBinder::GetBinderFlags
    STDMETHOD(GetBinderFlags)(
        DWORD *pBinderFlags)
    {
        LIMITED_METHOD_CONTRACT;
        return m_pBinder->GetBinderFlags(pBinderFlags);
    }

    // Implements code:ICLRPrivBinder::GetBinderID
    STDMETHOD(GetBinderID)(
        UINT_PTR *pBinderId);

    // Implements code:ICLRPrivBinder::FindAssemblyBySpec
    STDMETHOD(FindAssemblyBySpec)(
        LPVOID pvAppDomain,
        LPVOID pvAssemblySpec,
        HRESULT * pResult,
        ICLRPrivAssembly ** ppAssembly)
    { STATIC_CONTRACT_WRAPPER; return m_pBinder->FindAssemblyBySpec(pvAppDomain, pvAssemblySpec, pResult, ppAssembly); }

    //=============================================================================================
    // ICLRPrivAssembly interface methods
    
    // Implements code:ICLRPrivAssembly::IsShareable
    STDMETHOD(IsShareable)(
        BOOL * pbIsShareable);

    // Implements code:ICLRPrivAssembly::GetAvailableImageTypes
    STDMETHOD(GetAvailableImageTypes)(
        LPDWORD pdwImageTypes);

    // Implements code:ICLRPrivAssembly::GetImageResource
    STDMETHOD(GetImageResource)(
        DWORD dwImageType,
        DWORD *pdwImageType,
        ICLRPrivResource ** ppIResource);

    //---------------------------------------------------------------------------------------------
    HRESULT GetIBindResult(
        IBindResult ** ppIBindResult);

private:
    CLRPrivBinderUtil::AssemblyIdentity * m_pIdentity;
    
    ReleaseHolder<CLRPrivBinderAppX>      m_pBinder;

    ReleaseHolder<ICLRPrivResource>       m_pIResourceIL;
    // This cannot be a holder as there can be a race to assign to it.
    ICLRPrivResource *                    m_pIResourceNI;

    ReleaseHolder<IBindResult>            m_pIBindResult;
};

