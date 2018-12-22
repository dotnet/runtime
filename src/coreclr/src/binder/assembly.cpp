// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// Assembly.cpp
//


//
// Implements the Assembly class
//
// ============================================================
#include "common.h"
#include "clrprivbinderutil.h"
#include "assembly.hpp"
#include "utils.hpp"

namespace BINDER_SPACE
{
    namespace
    {
        BOOL IsPlatformArchitecture(PEKIND kArchitecture)
        {
            return ((kArchitecture != peMSIL) && (kArchitecture != peNone));
        }

        HRESULT GetAssemblyRefTokens(IMDInternalImport        *pMDImport,
                                     mdAssembly              **ppAssemblyRefTokens,
                                     DWORD                    *pdwCAssemblyRefTokens)
        {
            HRESULT hr = S_OK;

            _ASSERTE(pMDImport != NULL);
            _ASSERTE(ppAssemblyRefTokens != NULL);
            _ASSERTE(pdwCAssemblyRefTokens != NULL);

            mdAssembly *pAssemblyRefTokens = NULL;
            COUNT_T assemblyRefCount;
            
            HENUMInternalHolder hEnumAssemblyRef(pMDImport);
            IF_FAIL_GO(hEnumAssemblyRef.EnumInitNoThrow(mdtAssemblyRef, mdTokenNil));
            
            assemblyRefCount = hEnumAssemblyRef.EnumGetCount();

            pAssemblyRefTokens = new (nothrow) mdAssemblyRef[assemblyRefCount];
            if (pAssemblyRefTokens == NULL)
            {
                IF_FAIL_GO(E_OUTOFMEMORY);
            }
            ZeroMemory(pAssemblyRefTokens, assemblyRefCount * sizeof(mdAssemblyRef));

            for (COUNT_T i = 0; i < assemblyRefCount; i++)
            {
                bool ret = hEnumAssemblyRef.EnumNext(&(pAssemblyRefTokens[i]));
                _ASSERTE(ret);
            }

            *ppAssemblyRefTokens = pAssemblyRefTokens;
            pAssemblyRefTokens = NULL;

            *pdwCAssemblyRefTokens= assemblyRefCount;
            hr = S_OK;
    
Exit:
            SAFE_DELETE_ARRAY(pAssemblyRefTokens);    

            return hr;
        }
    };

    STDMETHODIMP Assembly::QueryInterface(REFIID   riid,
                                          void   **ppv)
    {
        HRESULT hr = S_OK;

        if (ppv == NULL)
        {
            hr = E_POINTER;
        }    
        else
        {
            if (IsEqualIID(riid, IID_IUnknown))
            {
                AddRef();
                *ppv = static_cast<IUnknown *>(this);
            }
            else
            {
                *ppv = NULL;
                hr = E_NOINTERFACE;
            }
        }

        return hr;
    }

    STDMETHODIMP_(ULONG) Assembly::AddRef()
    {
        return InterlockedIncrement(&m_cRef);
    }

    STDMETHODIMP_(ULONG) Assembly::Release()
    {
        ULONG ulRef = InterlockedDecrement(&m_cRef);

        if (ulRef == 0) 
        {
            delete this;
        }

        return ulRef;
    }

    Assembly::Assembly()
    {
        m_cRef = 1;
        m_pPEImage = NULL;
        m_pNativePEImage = NULL;
        m_pAssemblyName = NULL;
        m_pMDImport = NULL;
        m_pAssemblyRefTokens = NULL;
        m_dwCAssemblyRefTokens = static_cast<DWORD>(-1);
        m_dwAssemblyFlags = FLAG_NONE;
        m_pBinder = NULL;
    }

    Assembly::~Assembly()
    {
        BINDER_LOG_ASSEMBLY_NAME(L"destructing assembly", m_pAssemblyName);

        if (m_pPEImage != NULL)
        {
            BinderReleasePEImage(m_pPEImage);
            m_pPEImage = NULL;
        }

#ifdef  FEATURE_PREJIT
        if (m_pNativePEImage != NULL)
        {
            BinderReleasePEImage(m_pNativePEImage);
            m_pNativePEImage = NULL;
        }
#endif

        SAFE_RELEASE(m_pAssemblyName);
        SAFE_RELEASE(m_pMDImport);
        SAFE_DELETE_ARRAY(m_pAssemblyRefTokens);
    }

    HRESULT Assembly::Init(
                           IMDInternalImport       *pIMetaDataAssemblyImport,
                           PEKIND                   PeKind,
                           PEImage                 *pPEImage,
                           PEImage                 *pNativePEImage,
                           SString                 &assemblyPath,
                           BOOL                     fInspectionOnly,
                           BOOL                     fIsInGAC)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(L"Assembly::Init");

        ReleaseHolder<AssemblyName> pAssemblyName;
        SAFE_NEW(pAssemblyName, AssemblyName);

        // Get assembly name def from meta data import and store it for later refs access
        IF_FAIL_GO(pAssemblyName->Init(pIMetaDataAssemblyImport, PeKind));
        SetMDImport(pIMetaDataAssemblyImport);
        if (!fIsInGAC)
        {
            GetPath().Set(assemblyPath);
        }

        BINDER_LOG_ASSEMBLY_NAME(L"AssemblyNameDef", pAssemblyName);
        BINDER_LOG_STRING(L"System Architecture",
                          AssemblyName::ArchitectureToString(GetSystemArchitecture()));

        // Safe architecture for validation
        PEKIND kAssemblyArchitecture;
        kAssemblyArchitecture = pAssemblyName->GetArchitecture();
        SetInspectionOnly(fInspectionOnly);
        SetIsInGAC(fIsInGAC);
        SetPEImage(pPEImage);
        SetNativePEImage(pNativePEImage);
        pAssemblyName->SetIsDefinition(TRUE);

        // Now take ownership of assembly names
        SetAssemblyName(pAssemblyName.Extract(), FALSE /* fAddRef */);

        // Finally validate architecture
        if (!fInspectionOnly && !IsValidArchitecture(kAssemblyArchitecture))
        {
            // Assembly image can't be executed on this platform
            IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_BAD_FORMAT));
        }

    Exit:
        BINDER_LOG_LEAVE_HR(L"Assembly::Init", hr);
        return hr;
    }

    HRESULT Assembly::GetMVID(GUID *pMVID)
    {
        // Zero init the GUID incase we fail
        ZeroMemory(pMVID, sizeof(GUID));
        
        return m_pMDImport->GetScopeProps(NULL, pMVID);
    }
    
    HRESULT Assembly::GetNextAssemblyNameRef(DWORD          nIndex,
                                             AssemblyName **ppAssemblyName)
    {
        HRESULT hr = S_OK;
        BINDER_LOG_ENTER(L"Assembly::GetNextAssemblyNameRef");

        if (ppAssemblyName == NULL)
        {
            IF_FAIL_GO(E_INVALIDARG);
        }
        else if (GetNbAssemblyRefTokens() == static_cast<DWORD>(-1))
        {
            mdAssembly *pAssemblyRefTokens = NULL;
            DWORD dwCAssemblyRefTokens = 0;

            IF_FAIL_GO(BINDER_SPACE::GetAssemblyRefTokens(GetMDImport(),
                                                          &pAssemblyRefTokens,
                                                          &dwCAssemblyRefTokens));

            if (InterlockedCompareExchangeT(&m_pAssemblyRefTokens,
                                            pAssemblyRefTokens,
                                            NULL))
            {
                SAFE_DELETE_ARRAY(pAssemblyRefTokens);
            }
            SetNbAsssemblyRefTokens(dwCAssemblyRefTokens);
        }

        _ASSERTE(GetNbAssemblyRefTokens() != static_cast<DWORD>(-1));

        // Verify input index
        if (nIndex >= GetNbAssemblyRefTokens())
        {
            IF_FAIL_GO(HRESULT_FROM_WIN32(ERROR_NO_MORE_ITEMS));
        }
        else
        {
            ReleaseHolder<AssemblyName> pAssemblyName;

            SAFE_NEW(pAssemblyName, AssemblyName);
            IF_FAIL_GO(pAssemblyName->Init(GetMDImport(),
                                           peNone,
                                           GetAssemblyRefTokens()[nIndex],
                                           FALSE /* fIsDefinition */));

            *ppAssemblyName = pAssemblyName.Extract();
        }

    Exit:
        BINDER_LOG_LEAVE_HR(L"Assembly::GetNextAssemblyNameRef", hr);
        return hr;
    }

    /* static */
    PEKIND Assembly::GetSystemArchitecture()
    {
#if defined(_TARGET_X86_)
        return peI386;
#elif defined(_TARGET_AMD64_) 
        return peAMD64;
#elif defined(_TARGET_ARM_) 
        return peARM;
#elif defined(_TARGET_ARM64_) 
        return peARM64;
#else
        PORTABILITY_ASSERT("Assembly::GetSystemArchitecture");
#endif
    }
    
    /* static */
    BOOL Assembly::IsValidArchitecture(PEKIND kArchitecture)
    {
        if (!IsPlatformArchitecture(kArchitecture))
            return TRUE;

        return (kArchitecture == GetSystemArchitecture());
    }
    
    // --------------------------------------------------------------------
    // ICLRPrivAssembly methods
    // --------------------------------------------------------------------
    LPCWSTR Assembly::GetSimpleName() 
    {
        AssemblyName *pAsmName = GetAssemblyName();
        return (pAsmName == nullptr ? nullptr : pAsmName->GetSimpleName());
    }

    HRESULT Assembly::BindAssemblyByName(IAssemblyName * pIAssemblyName, ICLRPrivAssembly ** ppAssembly)
    {
        return (m_pBinder == NULL) ? E_FAIL : m_pBinder->BindAssemblyByName(pIAssemblyName, ppAssembly);
    }

    HRESULT Assembly::GetBinderID(UINT_PTR *pBinderId)
    {
        return (m_pBinder == NULL) ? E_FAIL : m_pBinder->GetBinderID(pBinderId);
    }
 
    HRESULT Assembly::GetLoaderAllocator(LPVOID* pLoaderAllocator)
    {
        return (m_pBinder == NULL) ? E_FAIL : m_pBinder->GetLoaderAllocator(pLoaderAllocator);
    }

    HRESULT Assembly::IsShareable(
        BOOL * pbIsShareable)
    {
        if(pbIsShareable == nullptr)
            return E_INVALIDARG;

        *pbIsShareable = GetIsSharable();
        return S_OK;
    }

    HRESULT Assembly::GetAvailableImageTypes(
        LPDWORD pdwImageTypes)
    {
        HRESULT hr = E_FAIL;

        if(pdwImageTypes == nullptr)
            return E_INVALIDARG;

        *pdwImageTypes = ASSEMBLY_IMAGE_TYPE_ASSEMBLY;

        return S_OK;
    }

    HRESULT Assembly::GetImageResource(
        DWORD dwImageType,
        DWORD * pdwImageType,
        ICLRPrivResource ** ppIResource)
    {
        HRESULT hr = S_OK;
        if(ppIResource == nullptr)
            return E_INVALIDARG;

        if ((dwImageType & ASSEMBLY_IMAGE_TYPE_ASSEMBLY) == ASSEMBLY_IMAGE_TYPE_ASSEMBLY)
        {
            *ppIResource = clr::SafeAddRef(&m_clrPrivRes);
            if (pdwImageType != nullptr)
                *pdwImageType = ASSEMBLY_IMAGE_TYPE_ASSEMBLY;
        }
        else
        {
            hr = CLR_E_BIND_IMAGE_UNAVAILABLE;
        }
        
        return hr;
    }

    // get parent pointer from nested type
    #define GetPThis() ((BINDER_SPACE::Assembly*)(((PBYTE)this) - offsetof(BINDER_SPACE::Assembly, m_clrPrivRes)))
    
    HRESULT Assembly::CLRPrivResourceAssembly::QueryInterface(REFIID riid, void ** ppv)
    {
        HRESULT hr = S_OK;
        VALIDATE_ARG_RET(ppv != NULL);
        
        if (IsEqualIID(riid, IID_IUnknown))
        {
            AddRef();
            *ppv = this;
        }
		else if (IsEqualIID(riid, __uuidof(ICLRPrivResource)))
		{
			AddRef();
			// upcasting is safe
			*ppv = static_cast<ICLRPrivResource *>(this);
		}
        else if (IsEqualIID(riid, __uuidof(ICLRPrivResourceAssembly)))
        {
            AddRef();
            *ppv = static_cast<ICLRPrivResourceAssembly *>(this);
        }
        else
        {
            *ppv = NULL;
            hr = E_NOINTERFACE;
        }
        
        return hr;
    }
 
    ULONG Assembly::CLRPrivResourceAssembly::AddRef()
    {
        return GetPThis()->AddRef();
    }

    ULONG Assembly::CLRPrivResourceAssembly::Release()
    {
        return GetPThis()->Release();
    }

    HRESULT Assembly::CLRPrivResourceAssembly::GetResourceType(IID *pIID)
    {
        VALIDATE_ARG_RET(pIID != nullptr);
        *pIID = __uuidof(ICLRPrivResourceAssembly);
        return S_OK;
    }
    
    HRESULT Assembly::CLRPrivResourceAssembly::GetAssembly(LPVOID *ppAssembly)
    {
        VALIDATE_ARG_RET(ppAssembly != nullptr);
        AddRef();
        *ppAssembly = GetPThis();
        return S_OK; 
    }
    
}

