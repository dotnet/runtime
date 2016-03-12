// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// Contains helper types for assembly binding host infrastructure.

#include "common.h"

#include "utilcode.h"
#include "strsafe.h"

#include "clrprivbinderutil.h"

inline
LPWSTR CopyStringThrowing(
    LPCWSTR wszString)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    NewArrayHolder<WCHAR> wszDup = NULL;
    if (wszString != NULL)
    {
        size_t wszLen = wcslen(wszString);
        wszDup = new WCHAR[wszLen + 1];
        IfFailThrow(StringCchCopy(wszDup, wszLen + 1, wszString));
    }
    wszDup.SuppressRelease();

    return wszDup;
}


namespace CLRPrivBinderUtil
{
#ifdef FEATURE_FUSION
    //-----------------------------------------------------------------------------------------------------------------
    CLRPrivAssemblyBindResultWrapper::CLRPrivAssemblyBindResultWrapper(
        IAssemblyName *pIAssemblyName,
        PCWSTR wzAssemblyPath,
        IILFingerprintFactory *pILFingerprintFactory
        ) :
        m_wzAssemblyPath(DuplicateStringThrowing(wzAssemblyPath)),
        m_pIAssemblyName(clr::SafeAddRef(pIAssemblyName)),
        m_bIBindResultNISet(false),
        m_pIBindResultNI(nullptr),
        m_pIILFingerprint(nullptr),
        m_pILFingerprintFactory(clr::SafeAddRef(pILFingerprintFactory)),
        m_lock(CrstLeafLock)
    {
        STANDARD_VM_CONTRACT;
        VALIDATE_ARG_THROW(pIAssemblyName != nullptr && wzAssemblyPath != nullptr);
    }

    //-----------------------------------------------------------------------------------------------------------------
    CLRPrivAssemblyBindResultWrapper::~CLRPrivAssemblyBindResultWrapper()
    {
        clr::SafeRelease(m_pIAssemblyName);
        clr::SafeRelease(m_pIILFingerprint);
        clr::SafeRelease(m_pIBindResultNI);
    }

    //=================================================================================================================
    // IBindResult methods

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::GetAssemblyNameDef(
        /*out*/ IAssemblyName **ppIAssemblyNameDef)
    {
        LIMITED_METHOD_CONTRACT;
        
        VALIDATE_ARG_RET(ppIAssemblyNameDef != nullptr);
        *ppIAssemblyNameDef = clr::SafeAddRef(m_pIAssemblyName);
        return S_OK;
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::GetNextAssemblyModuleName(
        /*in*/      DWORD   dwNIndex,
        __inout_ecount(*pdwCCModuleName)    LPWSTR  pwzModuleName,
        /*in, out, annotation("__inout")*/  LPDWORD pdwCCModuleName)
    {
        STANDARD_BIND_CONTRACT;
        _ASSERTE(!("E_NOTIMPL: " __FUNCTION__));
        return E_NOTIMPL;
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::GetAssemblyLocation(
        /*out*/ IAssemblyLocation **ppIAssemblyLocation)
    {
        STANDARD_BIND_CONTRACT;
        VALIDATE_ARG_RET(ppIAssemblyLocation != nullptr);
        return this->QueryInterface(ppIAssemblyLocation);
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::GetNativeImage(
        /*out*/ IBindResult **ppIBindResultNI,
        /*out*/ BOOL         *pfIBindResultNIProbed)
    {
        LIMITED_METHOD_CONTRACT;

        // m_bIBindResultNISet must always be read *before* m_pIBindResultNI
        bool bIBindResultNISet = m_bIBindResultNISet;

        if (pfIBindResultNIProbed != nullptr)
            *pfIBindResultNIProbed = bIBindResultNISet;

        if (bIBindResultNISet && ppIBindResultNI != nullptr)
            *ppIBindResultNI = clr::SafeAddRef(m_pIBindResultNI);

        return S_OK;
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::SetNativeImage(
        /*in*/  IBindResult  *pIBindResultNI,
        /*out*/ IBindResult **ppIBindResultNIFinal)
    {
        STANDARD_BIND_CONTRACT;
        HRESULT hr = S_OK;

        EX_TRY
        {
            // Native Binder needs S_FALSE returned if it loses the race.
            hr = S_FALSE;

            if (!m_bIBindResultNISet)
            {
                CrstHolder lock(&m_lock);
                if (!m_bIBindResultNISet)
                {
                    m_pIBindResultNI = clr::SafeAddRef(pIBindResultNI);
                    m_bIBindResultNISet = true;

                    // Won the race!
                    hr = S_OK;
                }
            }
        }
        EX_CATCH_HRESULT(hr);

        if (ppIBindResultNIFinal != nullptr)
            *ppIBindResultNIFinal = clr::SafeAddRef(m_pIBindResultNI);

        return hr;
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::IsEqual(
        /*in*/ IUnknown *pIUnk)
    {
        STANDARD_BIND_CONTRACT;
        HRESULT hr = S_OK;

        VALIDATE_ARG_RET(pIUnk != nullptr);

        ReleaseHolder<IBindResult> pIBindResult;

        hr = pIUnk->QueryInterface(__uuidof(IBindResult), (void **)&pIBindResult);
        if (SUCCEEDED(hr))
        {
            hr = pIBindResult == static_cast<IBindResult*>(this) ? S_OK : S_FALSE;
        }
        else if (hr == E_NOINTERFACE)
        {
            hr = S_FALSE;
        }

        return hr;
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::GetNextAssemblyNameRef(
        /*in*/  DWORD           dwNIndex,
        /*out*/ IAssemblyName **ppIAssemblyNameRef)
    {
        STANDARD_BIND_CONTRACT;
        _ASSERTE(!("E_UNEXPECTED: " __FUNCTION__));
        return E_UNEXPECTED;
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::GetNextDependentAssembly(
        /*in*/  DWORD      dwNIndex,
        /*out*/ IUnknown **ppIUnknownAssembly)
    {
        STANDARD_BIND_CONTRACT;
        _ASSERTE(!("E_UNEXPECTED: " __FUNCTION__));
        return E_UNEXPECTED;
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::GetAssemblyLocationOfILImage(
        /*out*/ IAssemblyLocation **ppAssemblyLocation)
    {
        LIMITED_METHOD_CONTRACT;
        return this->QueryInterface(ppAssemblyLocation);
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::GetILFingerprint(
        /*out*/ IILFingerprint **ppFingerprint)
    {
        STANDARD_BIND_CONTRACT;
        HRESULT hr = S_OK;

        VALIDATE_ARG_RET(ppFingerprint != nullptr);

        EX_TRY
        {
            *ppFingerprint = m_pIILFingerprint;
            if (*ppFingerprint == nullptr)
            {
                ReleaseHolder<IILFingerprint> pFingerprint;
                if (SUCCEEDED(hr = m_pILFingerprintFactory->GetILFingerprintForPath(GetILAssemblyPath(), &pFingerprint)))
                {
                    if (InterlockedCompareExchangeT<IILFingerprint>(&m_pIILFingerprint, pFingerprint, nullptr) == nullptr)
                    {
                        pFingerprint.SuppressRelease();
                    }
                }
            }
            *ppFingerprint = clr::SafeAddRef(m_pIILFingerprint);
        }
        EX_CATCH_HRESULT(hr);

        return hr;
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::GetSourceILTimestamp(
        /*out*/ FILETIME* pFileTime)
    {
        STANDARD_BIND_CONTRACT;
        HRESULT hr = S_OK;

        VALIDATE_ARG_RET(pFileTime != nullptr);

        EX_TRY
        {
            WIN32_FILE_ATTRIBUTE_DATA wfd;
            if (!WszGetFileAttributesEx(GetILAssemblyPath(), GetFileExInfoStandard, &wfd))
                ThrowLastError();
            *pFileTime = wfd.ftLastWriteTime;
        }
        EX_CATCH_HRESULT(hr);

        return hr;
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::GetSourceILSize(
        /*out*/ DWORD* pSize)
    {
        STANDARD_BIND_CONTRACT;
        HRESULT hr = S_OK;

        VALIDATE_ARG_RET(pSize != nullptr);

        EX_TRY
        {
            WIN32_FILE_ATTRIBUTE_DATA wfd;
            if (!WszGetFileAttributesEx(GetILAssemblyPath(), GetFileExInfoStandard, &wfd))
                ThrowLastError();
            if(wfd.nFileSizeHigh != 0)
                ThrowHR(COR_E_OVERFLOW);
            *pSize = wfd.nFileSizeLow;
        }
        EX_CATCH_HRESULT(hr);

        return hr;
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::GetNIInfo(
        /*out*/ INativeImageInstallInfo** pInfo)
    {
        STANDARD_BIND_CONTRACT;
        _ASSERTE(!("E_UNEXPECTED: " __FUNCTION__));
        return E_UNEXPECTED;
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::GetFlags(
        /*out*/ DWORD * pdwFlags)
    {
        STANDARD_BIND_CONTRACT;
        PRECONDITION(CheckPointer(pdwFlags));
        if (pdwFlags == nullptr)
        {
            return E_POINTER;
        }

        // Currently, no effort is made to open assemblies and build a full IAssemblyName - this currently
        // only contains the simple name. Since AppX packages cannot be in-place updated we can be confident
        // that the binding environment will remain unchanged between NGEN and runtime. As such, return the
        // flag to indicate that the native image binder should skip self assembly definition validation.
        *pdwFlags = IBindResultFlag_AssemblyNameDefIncomplete;

        return S_OK;
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::GetLocationType(
        /*out*/DWORD *pdwLocationType)
    {
        LIMITED_METHOD_CONTRACT;
        VALIDATE_ARG_RET(pdwLocationType != nullptr);
        
        if (pdwLocationType == nullptr)
            return E_INVALIDARG;
        *pdwLocationType = ASSEMBLY_LOCATION_PATH;
        return S_OK;
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::GetPath(
        __inout_ecount(*pdwccAssemblyPath) LPWSTR  pwzAssemblyPath,
        /*in, annotation("__inout")*/      LPDWORD pdwccAssemblyPath)
    {
        STANDARD_BIND_CONTRACT;
        HRESULT hr = S_OK;

        VALIDATE_ARG_RET(pdwccAssemblyPath != nullptr);

        EX_TRY
        {
            DWORD cchILAssemblyPath = static_cast<DWORD>(wcslen(GetILAssemblyPath())) + 1;
            if (pwzAssemblyPath != nullptr && cchILAssemblyPath <= *pdwccAssemblyPath)
            {
                IfFailThrow(StringCchCopy(pwzAssemblyPath, *pdwccAssemblyPath, GetILAssemblyPath()));
                *pdwccAssemblyPath = cchILAssemblyPath;
                hr = S_OK;
            }
            else
            {
                *pdwccAssemblyPath = cchILAssemblyPath;
                hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
            }
        }
        EX_CATCH_HRESULT(hr);

        return hr;
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT CLRPrivAssemblyBindResultWrapper::GetHostID(
        /*out*/ UINT64 *puiHostID)
    {
        STANDARD_BIND_CONTRACT;
        _ASSERTE(!("E_UNEXPECTED: " __FUNCTION__));
        return E_UNEXPECTED;
    }
#endif //FEATURE_FUSION

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT VerifyBind(
        IAssemblyName *pRefAssemblyName,
        ICLRPrivAssemblyInfo *pDefAssemblyInfo)
    {
        STANDARD_BIND_CONTRACT;

        HRESULT hr = S_OK;
        VALIDATE_PTR_RET(pRefAssemblyName);
        VALIDATE_PTR_RET(pDefAssemblyInfo);

        AssemblyIdentity refIdentity;
        IfFailRet(refIdentity.Initialize(pRefAssemblyName));

        AssemblyIdentity defIdentity;
        IfFailRet(defIdentity.Initialize(pDefAssemblyInfo));

        return VerifyBind(refIdentity, defIdentity);
    }

    //-----------------------------------------------------------------------------------------------------------------
    HRESULT VerifyBind(
        CLRPrivBinderUtil::AssemblyIdentity const & refIdentity,
        CLRPrivBinderUtil::AssemblyIdentity const & defIdentity)
    {
        LIMITED_METHOD_CONTRACT;

        //
        // Compare versions. Success conditions are the same as those in Silverlight:
        //  1. Reference identity has no version.
        //  2. Both identities have versions, and ref.version <= def.version.
        //
        // Since the default value of AssemblyVersion is 0.0.0.0, then if the
        // ref has no value set then the comparison will use 0.0.0.0, which will
        // always compare as true to the version contained in the def.
        //

        if (defIdentity.Version < refIdentity.Version)
        {   // Bound assembly has a lower version number than the reference.
            return CLR_E_BIND_ASSEMBLY_VERSION_TOO_LOW;
        }

        //
        // Compare public key tokens. Success conditions are:
        //  1. Reference identity has no PKT.
        //  2. Both identities have identical PKT values.
        //

        if (refIdentity.KeyToken.GetSize() != 0 &&          // Ref without PKT always passes.
            refIdentity.KeyToken != defIdentity.KeyToken)   // Otherwise Def must have matching PKT.
        {
            return CLR_E_BIND_ASSEMBLY_PUBLIC_KEY_MISMATCH;
        }

        return S_OK;
    }

    //---------------------------------------------------------------------------------------------
    CLRPrivResourcePathImpl::CLRPrivResourcePathImpl(LPCWSTR wzPath)
        : m_wzPath(CopyStringThrowing(wzPath))
    { STANDARD_VM_CONTRACT; }

    //---------------------------------------------------------------------------------------------
    HRESULT CLRPrivResourcePathImpl::GetPath(
        DWORD cchBuffer,
        LPDWORD pcchBuffer,
        __inout_ecount_part(cchBuffer, *pcchBuffer) LPWSTR wzBuffer)
    {
        LIMITED_METHOD_CONTRACT;
        HRESULT hr = S_OK;

        if (pcchBuffer == nullptr)
            IfFailRet(E_INVALIDARG);

        *pcchBuffer = (DWORD)wcslen(m_wzPath);

        if (wzBuffer != nullptr)
        {
            if (FAILED(StringCchCopy(wzBuffer, cchBuffer, m_wzPath)))
                IfFailRet(HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER));
        }
            
        return hr;
    }

    //---------------------------------------------------------------------------------------------
    CLRPrivResourceStreamImpl::CLRPrivResourceStreamImpl(IStream * pStream)
        : m_pStream(pStream)
    {
        LIMITED_METHOD_CONTRACT;
        pStream->AddRef();
    }

    //---------------------------------------------------------------------------------------------
    HRESULT CLRPrivResourceStreamImpl::GetStream(
        REFIID riid,
        LPVOID * ppvStream)
    {
        LIMITED_METHOD_CONTRACT;
        return m_pStream->QueryInterface(riid, ppvStream);
    }

    //---------------------------------------------------------------------------------------------
    HRESULT AssemblyVersion::Initialize(
        IAssemblyName * pAssemblyName)
    {
        WRAPPER_NO_CONTRACT;
        HRESULT hr = pAssemblyName->GetVersion(&dwMajorMinor, &dwBuildRevision);
        if (hr == FUSION_E_INVALID_NAME)
        {
            hr = S_FALSE;
        }
        return hr;
    }

    //---------------------------------------------------------------------------------------------
    HRESULT AssemblyVersion::Initialize(
        ICLRPrivAssemblyInfo * pAssemblyInfo)
    {
        WRAPPER_NO_CONTRACT;
        return pAssemblyInfo->GetAssemblyVersion(&wMajor, &wMinor, &wBuild, &wRevision);
    }

    //---------------------------------------------------------------------------------------------
    HRESULT PublicKey::Initialize(
        ICLRPrivAssemblyInfo * pAssemblyInfo)
    {
        LIMITED_METHOD_CONTRACT;
        HRESULT hr = S_OK;
        
        VALIDATE_PTR_RET(pAssemblyInfo);

        Uninitialize();

        DWORD cbKeyDef = 0;
        hr = pAssemblyInfo->GetAssemblyPublicKey(cbKeyDef, &cbKeyDef, nullptr);

        if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
        {
            if (cbKeyDef != 0)
            {
                NewArrayHolder<BYTE> pbKeyDef = new (nothrow) BYTE[cbKeyDef];
                IfNullRet(pbKeyDef);

                if (SUCCEEDED(hr = pAssemblyInfo->GetAssemblyPublicKey(cbKeyDef, &cbKeyDef, pbKeyDef)))
                {
                    m_key = pbKeyDef.Extract();
                    m_key_owned = true;
                    m_size = cbKeyDef;
                }
            }
        }

        return hr;
    }

    //---------------------------------------------------------------------------------------------
    HRESULT PublicKeyToken::Initialize(
        BYTE * pbKeyToken,
        DWORD cbKeyToken)
    {
        LIMITED_METHOD_CONTRACT;

        VALIDATE_CONDITION((pbKeyToken == nullptr) == (cbKeyToken == 0), return E_INVALIDARG);
        VALIDATE_ARG_RET(cbKeyToken == 0 || cbKeyToken == PUBLIC_KEY_TOKEN_LEN1);

        m_cbKeyToken = cbKeyToken;

        if (pbKeyToken != nullptr)
        {
            memcpy(m_rbKeyToken, pbKeyToken, PUBLIC_KEY_TOKEN_LEN1);
        }
        else
        {
            memset(m_rbKeyToken, 0, PUBLIC_KEY_TOKEN_LEN1);
        }

        return S_OK;
    }

    //---------------------------------------------------------------------------------------------
    HRESULT PublicKeyToken::Initialize(
        PublicKey const & pk)
    {
        LIMITED_METHOD_CONTRACT;

        StrongNameBufferHolder<BYTE>            pbKeyToken;
        DWORD                                   cbKeyToken;

        if (!StrongNameTokenFromPublicKey(const_cast<BYTE*>(pk.GetKey()), pk.GetSize(), &pbKeyToken, &cbKeyToken))
        {
            return static_cast<HRESULT>(StrongNameErrorInfo());
        }

        return Initialize(pbKeyToken, cbKeyToken);
    }

    //=====================================================================================================================
    HRESULT PublicKeyToken::Initialize(
        IAssemblyName * pName)
    {
        LIMITED_METHOD_CONTRACT;

        HRESULT hr = S_OK;

        DWORD cbKeyToken = sizeof(m_rbKeyToken);
        hr = pName->GetProperty(ASM_NAME_PUBLIC_KEY_TOKEN, m_rbKeyToken, &cbKeyToken);
        if (SUCCEEDED(hr))
        {
            m_cbKeyToken = cbKeyToken;
        }

        if (hr == FUSION_E_INVALID_NAME)
        {
            hr = S_FALSE;
        }

        return hr;
    }

    //=====================================================================================================================
    HRESULT PublicKeyToken::Initialize(
        ICLRPrivAssemblyInfo * pName)
    {
        LIMITED_METHOD_CONTRACT;

        HRESULT hr = S_OK;

        PublicKey pk;
        IfFailRet(pk.Initialize(pName));

        if (hr == S_OK) // Can return S_FALSE if no public key/token defined.
        {
            hr = Initialize(pk);
        }

        return hr;
    }

    //=====================================================================================================================
    bool operator==(
        PublicKeyToken const & lhs,
        PublicKeyToken const & rhs)
    {
        LIMITED_METHOD_CONTRACT;

        // Sizes must match
        if (lhs.GetSize() != rhs.GetSize())
        {
            return false;
        }

        // Empty PKT values are considered to be equal.
        if (lhs.GetSize() == 0)
        {
            return true;
        }

        // Compare values.
        return memcmp(lhs.GetToken(), rhs.GetToken(), lhs.GetSize()) == 0;
    }

    //=====================================================================================================================
    HRESULT AssemblyIdentity::Initialize(
        LPCWSTR wzName)
    {
        LIMITED_METHOD_CONTRACT;
        return StringCchCopy(Name, sizeof(Name) / sizeof(Name[0]), wzName);
    }

    //=====================================================================================================================
    HRESULT AssemblyIdentity::Initialize(
        ICLRPrivAssemblyInfo * pAssemblyInfo)
    {
        STANDARD_BIND_CONTRACT;
        HRESULT hr = S_OK;

        DWORD cchName = sizeof(Name) / sizeof(Name[0]);
        IfFailRet(pAssemblyInfo->GetAssemblyName(cchName, &cchName, Name));
        IfFailRet(Version.Initialize(pAssemblyInfo));
        IfFailRet(KeyToken.Initialize(pAssemblyInfo));

        return hr;
    }

    //=====================================================================================================================
    HRESULT AssemblyIdentity::Initialize(
        IAssemblyName * pAssemblyName)
    {
        STANDARD_BIND_CONTRACT;
        HRESULT hr = S_OK;

        DWORD cchName = sizeof(Name) / sizeof(Name[0]);
        IfFailRet(pAssemblyName->GetName(&cchName, Name));
        IfFailRet(Version.Initialize(pAssemblyName));
        IfFailRet(KeyToken.Initialize(pAssemblyName));

        return hr;
    }

#ifdef FEATURE_FUSION
    //=====================================================================================================================
    HRESULT AssemblyIdentity::Initialize(
        AssemblySpec * pSpec)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            FORBID_FAULT;
            MODE_ANY;
            CAN_TAKE_LOCK;
        }
        CONTRACTL_END

        HRESULT hr = S_OK;

        if (0 == WszMultiByteToWideChar(
            CP_UTF8, 0 /*flags*/, pSpec->GetName(), -1, Name, (int) (sizeof(Name) / sizeof(Name[0]))))
        {
            return HRESULT_FROM_GetLastError();
        }

        AssemblyMetaDataInternal * pAMDI = pSpec->GetContext();
        if (pAMDI != nullptr)
        {
            Version.wMajor = pAMDI->usMajorVersion;
            Version.wMinor = pAMDI->usMinorVersion;
            Version.wBuild = pAMDI->usBuildNumber;
            Version.wRevision = pAMDI->usRevisionNumber;
        }

        if (pSpec->HasPublicKeyToken())
        {
            PBYTE pbKey;
            DWORD cbKey;
            pSpec->GetPublicKeyToken(&pbKey, &cbKey);
            IfFailRet(KeyToken.Initialize(pbKey, cbKey));
        }

        return hr;
    }
#endif

    
    //=====================================================================================================================
    // Destroys list of strings (code:WStringList).
    void 
    WStringList_Delete(
        WStringList * pList)
    {
        LIMITED_METHOD_CONTRACT;
        
        if (pList != nullptr)
        {
            for (WStringListElem * pElem = pList->RemoveHead(); pElem != nullptr; pElem = pList->RemoveHead())
            {
                // Delete the string
                delete [] pElem->GetValue();
                delete pElem;
            }
            
            delete pList;
        }
    }


////////////////////////////////////////////////////////////////////////////////////////////////////
///// ----------------------------- Direct calls to VM  -------------------------------------------
////////////////////////////////////////////////////////////////////////////////////////////////////
#if defined(FEATURE_APPX_BINDER)
    ICLRPrivAssembly* RaiseAssemblyResolveEvent(IAssemblyName *pAssemblyName, ICLRPrivAssembly* pRequestingAssembly)
    {
        CONTRACT(ICLRPrivAssembly*)
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
            PRECONDITION(AppX::IsAppXProcess());
            PRECONDITION(AppDomain::GetCurrentDomain()->IsDefaultDomain());
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
            INJECT_FAULT(COMPlusThrowOM(););
        }
        CONTRACT_END;

        BinderMethodID methodId;

        methodId = METHOD__APP_DOMAIN__ON_ASSEMBLY_RESOLVE;  // post-bind execution event (the classic V1.0 event)

        // Elevate threads allowed loading level.  This allows the host to load an assembly even in a restricted
        // condition.  Note, however, that this exposes us to possible recursion failures, if the host tries to
        // load the assemblies currently being loaded.  (Such cases would then throw an exception.)

        OVERRIDE_LOAD_LEVEL_LIMIT(FILE_ACTIVE);
        OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

        DomainAssembly* pDomainAssembly = AppDomain::GetCurrentDomain()->FindAssembly(pRequestingAssembly);

        GCX_COOP();

        Assembly* pAssembly = NULL;

        struct _gc {
            OBJECTREF AppDomainRef;
            OBJECTREF AssemblyRef;
            STRINGREF str;
        } gc;
        ZeroMemory(&gc, sizeof(gc));

        SString ssAssemblyName;
        FusionBind::GetAssemblyNameDisplayName(pAssemblyName, ssAssemblyName);

        GCPROTECT_BEGIN(gc);
        if ((gc.AppDomainRef = GetAppDomain()->GetRawExposedObject()) != NULL)
        {
            gc.AssemblyRef = pDomainAssembly->GetExposedAssemblyObject();

            MethodDescCallSite onAssemblyResolve(methodId, &gc.AppDomainRef);

            gc.str = StringObject::NewString(ssAssemblyName.GetUnicode());
            ARG_SLOT args[3] = {
                ObjToArgSlot(gc.AppDomainRef),
                ObjToArgSlot(gc.AssemblyRef),
                ObjToArgSlot(gc.str)
            };
            ASSEMBLYREF ResultingAssemblyRef = (ASSEMBLYREF) onAssemblyResolve.Call_RetOBJECTREF(args);
            if (ResultingAssemblyRef != NULL)
            {
                pAssembly = ResultingAssemblyRef->GetAssembly();
            }
        }
        GCPROTECT_END();

        if (pAssembly != NULL)
        {
            if (pAssembly->IsIntrospectionOnly())
            {
                // Cannot return an introspection assembly from an execution callback or vice-versa
                COMPlusThrow(kFileLoadException, IDS_CLASSLOAD_ASSEMBLY_RESOLVE_RETURNED_INTROSPECTION );
            }
            if (pAssembly->IsCollectible())
            {
                COMPlusThrow(kNotSupportedException, W("NotSupported_CollectibleAssemblyResolve"));
            }

            // Check that the public key token matches the one specified in the spec
            // MatchPublicKeys throws as appropriate.
            
            StackScratchBuffer ssBuffer;
            AssemblySpec spec;
            IfFailThrow(spec.Init(ssAssemblyName.GetUTF8(ssBuffer)));
            spec.MatchPublicKeys(pAssembly);

        }

        if (pAssembly == nullptr)
            ThrowHR(COR_E_FILENOTFOUND);

        RETURN  pAssembly->GetManifestFile()->GetHostAssembly();
    }

    BOOL CompareHostBinderSpecs(AssemblySpec * a1, AssemblySpec * a2)
    {
        WRAPPER_NO_CONTRACT;
        return a1->CompareEx(a2, AssemblySpec::ASC_Default);
    }
#endif // FEATURE_APPX
} // namespace CLRPrivBinderUtil
