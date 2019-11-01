// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

 

#include "stdafx.h"
#include "winmdinterfaces.h"
#include "metadataexports.h"
#include "inc/adapter.h"
#include "strsafe.h"

//========================================================================================
// This metadata importer is exposed publically via CoCreateInstance(CLSID_CorMetaDataDispenser...).
// when the target is a .winmd file. It applies a small number of on-the-fly
// conversions to make the .winmd file look like a regular .NET assembly.
//
// Despite what the MSDN docs say, this importer only supports the reader interfaces and
// a subset of them at that. Supporting the whole set would not be practical with an
// on-the-fly translation strategy.
//========================================================================================
class WinMDImport : public IMetaDataImport2
                         , IMetaDataAssemblyImport
                         , IMetaDataWinMDImport
                         , IMetaDataValidate
                         , IMDCommon
                         , IMetaModelCommon
                         , IWinMDImport
#ifdef FEATURE_METADATA_INTERNAL_APIS
                         , IGetIMDInternalImport
#endif //FEATURE_METADATA_INTERNAL_APIS 
{
  public:
    //=========================================================
    // Factory
    //=========================================================
    static HRESULT Create(IMDCommon *pRawMDCommon, WinMDImport **ppWinMDImport)
    {
        HRESULT hr;
        *ppWinMDImport = NULL;

        WinMDImport *pNewWinMDImport = new (nothrow) WinMDImport(pRawMDCommon);

        IfFailGo(pNewWinMDImport->m_pRawMDCommon->QueryInterface(IID_IMetaDataImport2, (void**)&pNewWinMDImport->m_pRawImport));
        IfFailGo(pNewWinMDImport->m_pRawImport->QueryInterface(IID_IMetaDataAssemblyImport, (void**)&pNewWinMDImport->m_pRawAssemblyImport));
        
        if (FAILED(pNewWinMDImport->m_pRawImport->QueryInterface(IID_IMetaDataValidate, (void**)&pNewWinMDImport->m_pRawValidate)))
        {
            pNewWinMDImport->m_pRawValidate = nullptr;
        }
  
        pNewWinMDImport->m_pRawMetaModelCommonRO = pRawMDCommon->GetMetaModelCommonRO();
  
        IfFailGo(WinMDAdapter::Create(pRawMDCommon, &pNewWinMDImport->m_pWinMDAdapter));

        (*ppWinMDImport = pNewWinMDImport)->AddRef();
        hr = S_OK;

      ErrExit:
        if (pNewWinMDImport)
            pNewWinMDImport->Release();
        return hr;
    }
    

  private:
    //=========================================================
    // Ctors, Dtors
    //=========================================================
    WinMDImport(IMDCommon * pRawMDCommon)
    {
        m_cRef = 1;
        m_pRawImport = NULL;
        m_pWinMDAdapter = NULL;
        m_pRawAssemblyImport = NULL;
        m_pFreeThreadedMarshaler = NULL;
        (m_pRawMDCommon = pRawMDCommon)->AddRef();
    }

    //---------------------------------------------------------
    ~WinMDImport()
    {
        if (m_pRawMDCommon)
            m_pRawMDCommon->Release();
        if (m_pRawImport)
            m_pRawImport->Release();
        if (m_pRawAssemblyImport)
            m_pRawAssemblyImport->Release();
        if (m_pRawValidate)
            m_pRawValidate->Release();
        if (m_pFreeThreadedMarshaler)
            m_pFreeThreadedMarshaler->Release();
        delete m_pWinMDAdapter;
    }

    //---------------------------------------------------------
    BOOL IsValidNonNilToken(mdToken token, DWORD tkKind)
    {
        DWORD tkKindActual = TypeFromToken(token);
        DWORD rid          = RidFromToken(token);

        if (tkKindActual != tkKind)
            return FALSE;

        if (rid == 0)
            return FALSE;

        ULONG ulRowCount = m_pRawMetaModelCommonRO->CommonGetRowCount(tkKind);
        if (tkKind == mdtAssemblyRef)
            return rid <= ulRowCount + m_pWinMDAdapter->GetExtraAssemblyRefCount();
        else
            return rid <= ulRowCount;
    }

  public:
    //=========================================================
    // IUnknown methods
    //=========================================================
    STDMETHODIMP QueryInterface(REFIID riid, void** ppUnk)
    {
        HRESULT hr;
        
        *ppUnk = 0;
        if (riid == IID_IUnknown || riid == IID_IWinMDImport)
            *ppUnk = (IWinMDImport*)this;
        else if (riid == IID_IMetaDataImport || riid == IID_IMetaDataImport2)
            *ppUnk = (IMetaDataImport*)this;
        else if (riid == IID_IMetaDataWinMDImport)
            *ppUnk = (IMetaDataWinMDImport*)this;
        else if (riid == IID_IMetaDataAssemblyImport)
            *ppUnk = (IMetaDataAssemblyImport*)this;
        else if (riid == IID_IMDCommon)
            *ppUnk = (IMDCommon*)this;
        else if (riid == IID_IMetaDataValidate)
        {
            if (m_pRawValidate == NULL)
            {
                return E_NOINTERFACE;
            }

            *ppUnk = (IMetaDataValidate*)this;
        }
#ifdef FEATURE_METADATA_INTERNAL_APIS
        else if (riid == IID_IGetIMDInternalImport)
            *ppUnk = (IGetIMDInternalImport*)this;
#endif // FEATURE_METADATA_INTERNAL_APIS
        else if (riid == IID_IMarshal)
        {
            if (m_pFreeThreadedMarshaler == NULL)
            {
                ReleaseHolder<IUnknown> pFreeThreadedMarshaler = NULL;
                IfFailRet(CoCreateFreeThreadedMarshaler((IUnknown *)(IMetaDataImport *)this, &pFreeThreadedMarshaler));
                if (InterlockedCompareExchangeT<IUnknown *>(&m_pFreeThreadedMarshaler, pFreeThreadedMarshaler, NULL) == NULL)
                {   // We won the initialization race
                    pFreeThreadedMarshaler.SuppressRelease();
                }
            }
            return m_pFreeThreadedMarshaler->QueryInterface(riid, ppUnk); 
        }
        else
        {
#ifndef DACCESS_COMPILE
#ifdef _DEBUG
            if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MD_WinMD_AssertOnIllegalUsage))
            {
                if (riid == IID_IMetaDataTables)
                    _ASSERTE(!"WinMDImport::QueryInterface(IID_IMetaDataTables) returning E_NOINTERFACE");
                else if (riid == IID_IMetaDataTables2)
                    _ASSERTE(!"WinMDImport::QueryInterface(IID_IMetaDataTables2) returning E_NOINTERFACE");
                else if (riid == IID_IMetaDataInfo)
                    _ASSERTE(!"WinMDImport::QueryInterface(IID_IMetaDataInfo) returning E_NOINTERFACE");
                else if (riid == IID_IMetaDataEmit)
                    _ASSERTE(!"WinMDImport::QueryInterface(IID_IMetaDataEmit) returning E_NOINTERFACE");
                else if (riid == IID_IMetaDataEmit2)
                    _ASSERTE(!"WinMDImport::QueryInterface(IID_IMetaDataEmit2) returning E_NOINTERFACE");
                else if (riid == IID_IMetaDataAssemblyEmit)
                    _ASSERTE(!"WinMDImport::QueryInterface(IID_IMetaDataAssemblyEmit) returning E_NOINTERFACE");
                else if (riid == IID_IMetaDataValidate)
                    _ASSERTE(!"WinMDImport::QueryInterface(IID_IMetaDataValidate) returning E_NOINTERFACE");
                else if (riid == IID_IMetaDataFilter)
                    _ASSERTE(!"WinMDImport::QueryInterface(IID_IMetaDataFilter) returning E_NOINTERFACE");
                else if (riid == IID_IMetaDataHelper)
                    _ASSERTE(!"WinMDImport::QueryInterface(IID_IMetaDataHelper) returning E_NOINTERFACE");
                else if (riid == IID_IMDInternalEmit)
                    _ASSERTE(!"WinMDImport::QueryInterface(IID_IMDInternalEmit) returning E_NOINTERFACE");
                else if (riid == IID_IMetaDataEmitHelper)
                    _ASSERTE(!"WinMDImport::QueryInterface(IID_IMetaDataEmitHelper) returning E_NOINTERFACE");
#ifdef FEATURE_PREJIT
                else if (riid == IID_IMetaDataCorProfileData)
                    _ASSERTE(!"WinMDImport::QueryInterface(IID_IMetaDataCorProfileData) returning E_NOINTERFACE");
                else if (riid == IID_IMDInternalMetadataReorderingOptions)
                    _ASSERTE(!"WinMDImport::QueryInterface(IID_IMDInternalMetadataReorderingOptions) returning E_NOINTERFACE");
#endif
                else
                    _ASSERTE(!"WinMDImport::QueryInterface() returning E_NOINTERFACE");
            }
#endif //_DEBUG
#endif
            return E_NOINTERFACE;
        }
        AddRef();
        return S_OK;
    }
    //---------------------------------------------------------
    STDMETHODIMP_(ULONG) AddRef(void)
    {
        return InterlockedIncrement(&m_cRef);
    }
    //---------------------------------------------------------
    STDMETHODIMP_(ULONG) Release(void)
    {
        ULONG   cRef = InterlockedDecrement(&m_cRef);
        if (!cRef)
            delete this;
        return cRef;
    }

  public:
    //=========================================================
    // IWinMDImport methods
    //=========================================================
    STDMETHODIMP IsScenarioWinMDExp(BOOL *pbResult)
    {
        if (pbResult == NULL)
            return E_POINTER;

        *pbResult = m_pWinMDAdapter->IsScenarioWinMDExp();
        return S_OK;
    }

    STDMETHODIMP IsRuntimeClassImplementation(mdTypeDef tkTypeDef, BOOL *pbResult)
    {
        if (pbResult == NULL)
            return E_POINTER;

        return m_pWinMDAdapter->IsRuntimeClassImplementation(tkTypeDef, pbResult);
    }

    //=========================================================
    // IMetaDataImport methods
    //=========================================================
    STDMETHODIMP_(void) CloseEnum(HCORENUM hEnum)
    {
        m_pRawImport->CloseEnum(hEnum);
    }

    STDMETHODIMP CountEnum(HCORENUM hEnum, ULONG *pulCount)      
    {
        _ASSERTE(pulCount != NULL); // Crash on NULL pulCount (just like real RegMeta)
        if (hEnum != NULL)
        {
            HENUMInternal *henuminternal = static_cast<HENUMInternal*>(hEnum);
            // Special case: We hijack MethodImpl enum's 
            if (henuminternal->m_tkKind == (TBL_MethodImpl << 24))
            {                                              
                *pulCount = henuminternal->m_ulCount / 2;  // MethodImpl enum's are weird - their entries are body/decl pairs so the internal count is twice the number the caller wants to see.
                return S_OK;
            }
        }

        return m_pRawImport->CountEnum(hEnum, pulCount);
    }

    STDMETHODIMP ResetEnum(HCORENUM hEnum, ULONG ulPos)      
    {
        return m_pRawImport->ResetEnum(hEnum, ulPos);
    }

    STDMETHODIMP EnumTypeDefs(HCORENUM *phEnum, mdTypeDef rTypeDefs[],
                            ULONG cMax, ULONG *pcTypeDefs)      
    {
        return m_pRawImport->EnumTypeDefs(phEnum, rTypeDefs, cMax, pcTypeDefs);
    }

    STDMETHODIMP EnumInterfaceImpls(HCORENUM *phEnum, mdTypeDef td,
                            mdInterfaceImpl rImpls[], ULONG cMax,
                            ULONG* pcImpls)      
    {
        return m_pRawImport->EnumInterfaceImpls(phEnum, td, rImpls, cMax, pcImpls);
    }

    STDMETHODIMP EnumTypeRefs(HCORENUM *phEnum, mdTypeRef rTypeRefs[],
                            ULONG cMax, ULONG* pcTypeRefs)      
    {
            // No trick needed: a previous call to EnumTypeRefs must have already taken care of the
            // extra type refs.
            return m_pRawImport->EnumTypeRefs(phEnum, rTypeRefs, cMax, pcTypeRefs);
    }

    STDMETHODIMP FindTypeDefByName(         // S_OK or error.
        LPCWSTR     wszTypeDef,             // [IN] Name of the Type.
        mdToken     tkEnclosingClass,       // [IN] TypeDef/TypeRef for Enclosing class.
        mdTypeDef   *ptd)                   // [OUT] Put the TypeDef token here.
    {
        if (wszTypeDef == NULL)          // E_INVALIDARG on NULL szTypeDef (just like real RegMeta)
            return E_INVALIDARG;
        _ASSERTE(ptd != NULL);          // AV on NULL ptd (just like real RegMeta)
        *ptd = mdTypeDefNil; 

        LPUTF8      szFullName;
        LPCUTF8     szNamespace;
        LPCUTF8     szName;
        // Convert the  name to UTF8.
        UTF8STR(wszTypeDef, szFullName);
        ns::SplitInline(szFullName, szNamespace, szName);

        return m_pWinMDAdapter->FindTypeDef(szNamespace, szName, tkEnclosingClass, ptd);
    }


    STDMETHODIMP GetScopeProps(               // S_OK or error.
      __out_ecount_part_opt(cchName, *pchName)
        LPWSTR      szName,                 // [OUT] Put the name here.
        ULONG       cchName,                // [IN] Size of name buffer in wide chars.
        ULONG       *pchName,               // [OUT] Put size of name (wide chars) here.
        GUID        *pmvid)                 // [OUT, OPTIONAL] Put MVID here.
    {
        // Returns error code from name filling (may be CLDB_S_TRUNCTATION)
        return m_pRawImport->GetScopeProps(szName, cchName, pchName, pmvid);
    }


    STDMETHODIMP GetModuleFromScope(          // S_OK.
        mdModule    *pmd)                   // [OUT] Put mdModule token here.
    {
        return m_pRawImport->GetModuleFromScope(pmd);
    }


    STDMETHODIMP GetTypeDefProps(             // S_OK or error.
        mdTypeDef   td,                     // [IN] TypeDef token for inquiry.
      __out_ecount_part_opt(cchTypeDef, *pchTypeDef)
        LPWSTR      szTypeDef,              // [OUT] Put name here.
        ULONG       cchTypeDef,             // [IN] size of name buffer in wide chars.
        ULONG       *pchTypeDef,            // [OUT] put size of name (wide chars) here.
        DWORD       *pdwTypeDefFlags,       // [OUT] Put flags here.
        mdToken     *ptkExtends)            // [OUT] Put base class TypeDef/TypeRef here.
    {
        HRESULT hr;
        LPCSTR szNamespace;
        LPCSTR szName;
        if (!IsValidNonNilToken(td, mdtTypeDef))
        {
            // Idiosyncractic edge cases - delegate straight through to inherit the correct idiosyncractic edge results 
            return m_pRawImport->GetTypeDefProps(td, szTypeDef, cchTypeDef, pchTypeDef, pdwTypeDefFlags, ptkExtends);
        }
        IfFailRet(m_pWinMDAdapter->GetTypeDefProps(td, &szNamespace, &szName, pdwTypeDefFlags, ptkExtends));
        // Returns error code from name filling (may be CLDB_S_TRUNCTATION)
        return DeliverUtf8NamespaceAndName(szNamespace, szName, szTypeDef, cchTypeDef, pchTypeDef);
    }


    STDMETHODIMP GetInterfaceImplProps(       // S_OK or error.
        mdInterfaceImpl iiImpl,             // [IN] InterfaceImpl token.
        mdTypeDef   *pClass,                // [OUT] Put implementing class token here.
        mdToken     *ptkIface)              // [OUT] Put implemented interface token here.
    {
        return m_pRawImport->GetInterfaceImplProps(iiImpl, pClass, ptkIface);
    }

            
    STDMETHODIMP GetTypeRefProps(             // S_OK or error.
        mdTypeRef   tr,                     // [IN] TypeRef token.
        mdToken     *ptkResolutionScope,    // [OUT] Resolution scope, ModuleRef or AssemblyRef.
      __out_ecount_part_opt(cchName, *pchName)
        LPWSTR      szName,                 // [OUT] Name of the TypeRef.
        ULONG       cchName,                // [IN] Size of buffer.
        ULONG       *pchName)               // [OUT] Size of Name.
    {
        HRESULT hr;
        if (!IsValidNonNilToken(tr, mdtTypeRef))
        {
            // Idiosyncratic edge cases - delegate straight through to inherit the correct idiosyncratic edge result
            return m_pRawImport->GetTypeRefProps(tr, ptkResolutionScope, szName, cchName, pchName);
        }

        LPCUTF8 szUtf8Namespace;
        LPCUTF8 szUtf8Name;
        mdToken tkResolutionScope;
        IfFailRet(CommonGetTypeRefProps(tr, &szUtf8Namespace, &szUtf8Name, &tkResolutionScope));
        if (ptkResolutionScope != NULL)
        {
            *ptkResolutionScope = tkResolutionScope;
        }
        // Returns error code from name filling (may be CLDB_S_TRUNCTATION)
        return DeliverUtf8NamespaceAndName(szUtf8Namespace, szUtf8Name, szName, cchName, pchName);
    }


    STDMETHODIMP ResolveTypeRef(mdTypeRef tr, REFIID riid, IUnknown **ppIScope, mdTypeDef *ptd)      
    {
        WINMD_COMPAT_ASSERT("IMetaDataImport::ResolveTypeRef() not supported on .winmd files.");
        return E_NOTIMPL;
    }


    STDMETHODIMP EnumMembers(                 // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        mdToken     rMembers[],             // [OUT] Put MemberDefs here.
        ULONG       cMax,                   // [IN] Max MemberDefs to put.
        ULONG       *pcTokens)              // [OUT] Put # put here.
    {
        return m_pRawImport->EnumMembers(phEnum, cl, rMembers, cMax, pcTokens);
    }


    STDMETHODIMP EnumMembersWithName(         // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        LPCWSTR     szName,                 // [IN] Limit results to those with this name.
        mdToken     rMembers[],             // [OUT] Put MemberDefs here.
        ULONG       cMax,                   // [IN] Max MemberDefs to put.
        ULONG       *pcTokens)              // [OUT] Put # put here.
    {
        return m_pRawImport->EnumMembersWithName(phEnum, cl, szName, rMembers, cMax, pcTokens);
    }


    STDMETHODIMP EnumMethods(                 // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        mdMethodDef rMethods[],             // [OUT] Put MethodDefs here.
        ULONG       cMax,                   // [IN] Max MethodDefs to put.
        ULONG       *pcTokens)              // [OUT] Put # put here.
    {
        return m_pRawImport->EnumMethods(phEnum, cl, rMethods, cMax, pcTokens);
    }


    STDMETHODIMP EnumMethodsWithName(         // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        LPCWSTR     szName,                 // [IN] Limit results to those with this name.
        mdMethodDef rMethods[],             // [OU] Put MethodDefs here.
        ULONG       cMax,                   // [IN] Max MethodDefs to put.
        ULONG       *pcTokens)              // [OUT] Put # put here.
    {
        return m_pRawImport->EnumMethodsWithName(phEnum, cl, szName, rMethods, cMax, pcTokens);
    }


    STDMETHODIMP EnumFields(                  // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        mdFieldDef  rFields[],              // [OUT] Put FieldDefs here.
        ULONG       cMax,                   // [IN] Max FieldDefs to put.
        ULONG       *pcTokens)              // [OUT] Put # put here.
    {
        return m_pRawImport->EnumFields(phEnum, cl, rFields, cMax, pcTokens);
    }


    STDMETHODIMP EnumFieldsWithName(          // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        LPCWSTR     szName,                 // [IN] Limit results to those with this name.
        mdFieldDef  rFields[],              // [OUT] Put MemberDefs here.
        ULONG       cMax,                   // [IN] Max MemberDefs to put.
        ULONG       *pcTokens)              // [OUT] Put # put here.
    {
        return m_pRawImport->EnumFieldsWithName(phEnum, cl, szName, rFields, cMax, pcTokens);
    }



    STDMETHODIMP EnumParams(                  // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdMethodDef mb,                     // [IN] MethodDef to scope the enumeration. 
        mdParamDef  rParams[],              // [OUT] Put ParamDefs here.
        ULONG       cMax,                   // [IN] Max ParamDefs to put.
        ULONG       *pcTokens)              // [OUT] Put # put here.
    {
        return m_pRawImport->EnumParams(phEnum, mb, rParams, cMax, pcTokens);
    }


    STDMETHODIMP EnumMemberRefs(              // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken     tkParent,               // [IN] Parent token to scope the enumeration.
        mdMemberRef rMemberRefs[],          // [OUT] Put MemberRefs here.
        ULONG       cMax,                   // [IN] Max MemberRefs to put.
        ULONG       *pcTokens)              // [OUT] Put # put here.
    {
        return m_pRawImport->EnumMemberRefs(phEnum, tkParent, rMemberRefs, cMax, pcTokens);
    }


    STDMETHODIMP EnumMethodImpls(             // S_OK, S_FALSE, or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
        mdToken     rMethodBody[],          // [OUT] Put Method Body tokens here.
        mdToken     rMethodDecl[],          // [OUT] Put Method Declaration tokens here.
        ULONG       cMax,                   // [IN] Max tokens to put.
        ULONG       *pcTokens)              // [OUT] Put # put here.
    {
        // Note: This wrapper emulates the underlying RegMeta's parameter validation semantics:
        //   Pass a bad phEnum: Instant AV
        //   Pass out of range typedef: returns S_FALSE (i.e. generates empty list)
        //   Pass non-typedef: returns S_FALSE with assert
        //   Pass bad output ptr for bodies or decls, AV 
        //   Pass bad output ptr for pcTokens, AV (but NULL is allowed.)

        HRESULT         hr;
        HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
        HENUMInternal  *pEnum = *ppmdEnum;
        
        if ( pEnum == 0 )
        {
            // Create the enumerator, DynamicArrayEnum does not use the token type.
            IfFailGo( HENUMInternal::CreateDynamicArrayEnum( (TBL_MethodImpl << 24), &pEnum) );
            IfFailGo( m_pWinMDAdapter->AddMethodImplsToEnum(td, pEnum));
            // set the output parameter
            *ppmdEnum = pEnum;
        }
    
        // fill the output token buffer
        hr = HENUMInternal::EnumWithCount(pEnum, cMax, rMethodBody, rMethodDecl, pcTokens);
    
    ErrExit:
        HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
        return hr;
    }


    STDMETHODIMP EnumPermissionSets(          // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken     tk,                     // [IN] if !NIL, token to scope the enumeration.
        DWORD       dwActions,              // [IN] if !0, return only these actions.
        mdPermission rPermission[],         // [OUT] Put Permissions here.
        ULONG       cMax,                   // [IN] Max Permissions to put. 
        ULONG       *pcTokens)              // [OUT] Put # put here.
    {
        return m_pRawImport->EnumPermissionSets(phEnum, tk, dwActions, rPermission, cMax, pcTokens);
    }


    STDMETHODIMP FindMember(
        mdTypeDef   td,                     // [IN] given typedef
        LPCWSTR     szName,                 // [IN] member name 
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature 
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdToken     *pmb)                   // [OUT] matching memberdef 
    {
        HRESULT hr = FindMethod(
            td,
            szName,
            pvSigBlob,
            cbSigBlob,
            pmb);

        if (hr == CLDB_E_RECORD_NOTFOUND)
        {
            // now try field table
            hr = FindField(
                td,
                szName,
                pvSigBlob,
                cbSigBlob,
                pmb);
        }

        return hr;
    }


    STDMETHODIMP FindMethod(
        mdTypeDef   td,                     // [IN] given typedef
        LPCWSTR     szName,                 // [IN] member name 
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature 
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdMethodDef *pmb)                   // [OUT] matching memberdef 
    {
        if (pvSigBlob == NULL || cbSigBlob == 0)
        {
            // if signature matching is not needed, we can delegate to the underlying implementation
            return m_pRawImport->FindMethod(td, szName, pvSigBlob, cbSigBlob, pmb);
        }

        // The following code emulates RegMeta::FindMethod. We cannot call the underlying
        // implementation because we need to compare pvSigBlob to reinterpreted signatures.
        HRESULT hr = S_OK;
        HCORENUM hEnum = NULL;

        CQuickBytes qbSig; // holds non-varargs signature
        CQuickArray<WCHAR> rName;

        if (szName == NULL || pmb == NULL)
            IfFailGo(E_INVALIDARG);

        *pmb = mdMethodDefNil;

        {
            // check to see if this is a vararg signature
            PCCOR_SIGNATURE pvSigTemp = pvSigBlob;
            if (isCallConv(CorSigUncompressCallingConv(pvSigTemp), IMAGE_CEE_CS_CALLCONV_VARARG))
            {
                // Get the fixed part of VARARG signature
                IfFailGo(_GetFixedSigOfVarArg(pvSigBlob, cbSigBlob, &qbSig, &cbSigBlob));
                pvSigBlob = (PCCOR_SIGNATURE) qbSig.Ptr();
            }
        }

        // now iterate all methods in td and compare name and signature
        IfNullGo(rName.AllocNoThrow(wcslen(szName) + 1));

        mdMethodDef md;
        ULONG count;
        while ((hr = EnumMethods(&hEnum, td, &md, 1, &count)) == S_OK)
        {
            PCCOR_SIGNATURE pvMethodSigBlob;
            ULONG cbMethodSigBlob;
            ULONG chMethodName;
            DWORD dwMethodAttr;

            IfFailGo(GetMethodProps(md, NULL, rName.Ptr(), (ULONG)rName.Size(), &chMethodName, &dwMethodAttr, &pvMethodSigBlob, &cbMethodSigBlob, NULL, NULL));

            if (chMethodName == rName.Size() && wcscmp(szName, rName.Ptr()) == 0)
            {
                // we have a name match, check signature
                if (cbSigBlob != cbMethodSigBlob || memcmp(pvSigBlob, pvMethodSigBlob, cbSigBlob) != 0)
                    continue;

                // ignore PrivateScope methods
                if (IsMdPrivateScope(dwMethodAttr))
                    continue;

                // we found the method
                *pmb = md;
                goto ErrExit;
            }
        }
        IfFailGo(hr);
        hr = CLDB_E_RECORD_NOTFOUND;

    ErrExit:
        if (hEnum != NULL)
            CloseEnum(hEnum);

        return hr;
    }


    STDMETHODIMP FindField(
        mdTypeDef   td,                     // [IN] given typedef
        LPCWSTR     szName,                 // [IN] member name 
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature 
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdFieldDef  *pmb)                   // [OUT] matching memberdef 
    {
        if (pvSigBlob == NULL || cbSigBlob == 0)
        {
            // if signature matching is not needed, we can delegate to the underlying implementation
            return m_pRawImport->FindField(td, szName, pvSigBlob, cbSigBlob, pmb);
        }

        // The following code emulates RegMeta::FindField. We cannot call the underlying
        // implementation because we need to compare pvSigBlob to reinterpreted signatures.
        HRESULT hr = S_OK;
        HCORENUM hEnum = NULL;

        CQuickArray<WCHAR> rName;

        if (szName == NULL || pmb == NULL)
            IfFailGo(E_INVALIDARG);

        *pmb = mdFieldDefNil;

        // now iterate all fields in td and compare name and signature
        IfNullGo(rName.AllocNoThrow(wcslen(szName) + 1));

        mdFieldDef fd;
        ULONG count;
        while ((hr = EnumFields(&hEnum, td, &fd, 1, &count)) == S_OK)
        {
            PCCOR_SIGNATURE pvFieldSigBlob;
            ULONG cbFieldSigBlob;
            ULONG chFieldName;
            DWORD dwFieldAttr;

            IfFailGo(GetFieldProps(fd, NULL, rName.Ptr(), (ULONG)rName.Size(), &chFieldName, &dwFieldAttr, &pvFieldSigBlob, &cbFieldSigBlob, NULL, NULL, NULL));

            if (chFieldName == rName.Size() && wcscmp(szName, rName.Ptr()) == 0)
            {
                // we have a name match, check signature
                if (cbSigBlob != cbFieldSigBlob || memcmp(pvSigBlob, pvFieldSigBlob, cbSigBlob) != 0)
                    continue;

                // ignore PrivateScope fields
                if (IsFdPrivateScope(dwFieldAttr))
                    continue;

                // we found the field
                *pmb = fd;
                goto ErrExit;
            }
        }
        IfFailGo(hr);
        hr = CLDB_E_RECORD_NOTFOUND;

    ErrExit:
        if (hEnum != NULL)
            CloseEnum(hEnum);

        return hr;
    }

   
   STDMETHODIMP FindMemberRef(
		   mdTypeRef   td,					   // [IN] given typeRef
		   LPCWSTR	   szName,				   // [IN] member name 
		   PCCOR_SIGNATURE pvSigBlob,		   // [IN] point to a blob value of CLR signature 
		   ULONG	   cbSigBlob,			   // [IN] count of bytes in the signature blob
		   mdMemberRef *pmr)				   // [OUT] matching memberref 
	   {
		   if (pvSigBlob == NULL || cbSigBlob == 0)
		   {
			   // if signature matching is not needed, we can delegate to the underlying implementation
			   return m_pRawImport->FindMemberRef(td, szName, pvSigBlob, cbSigBlob, pmr);
		   }
   
		   // The following code emulates RegMeta::FindMemberRef. We cannot call the underlying
		   // implementation because we need to compare pvSigBlob to reinterpreted signatures.
		   HRESULT hr = S_OK;
		   HCORENUM hEnum = NULL;
   
		   CQuickArray<WCHAR> rName;
   
		   if (szName == NULL || pmr == NULL)
			   IfFailGo(CLDB_E_RECORD_NOTFOUND);

		   *pmr = mdMemberRefNil;
   
		   // now iterate all members in td and compare name and signature
           IfNullGo(rName.AllocNoThrow(wcslen(szName) + 1));
   
		   mdMemberRef rd;
		   ULONG count;
		   while ((hr = EnumMemberRefs(&hEnum, td, &rd, 1, &count)) == S_OK)
		   {
			   PCCOR_SIGNATURE pvMemberSigBlob;
			   ULONG cbMemberSigBlob;
			   ULONG chMemberName;
   
			   IfFailGo(GetMemberRefProps(rd, NULL, rName.Ptr(), (ULONG)rName.Size(), &chMemberName, &pvMemberSigBlob, &cbMemberSigBlob));
   
			   if (chMemberName == rName.Size() && wcscmp(szName, rName.Ptr()) == 0)
			   {
				   // we have a name match, check signature
				   if (cbSigBlob != cbMemberSigBlob || memcmp(pvSigBlob, pvMemberSigBlob, cbSigBlob) != 0)
					   continue;
   
				   // we found the member
				   *pmr = rd;
				   goto ErrExit;
			   }
		   }
		   IfFailGo(hr);
		   hr = CLDB_E_RECORD_NOTFOUND;
   
	   ErrExit:
		   if (hEnum != NULL)
			   CloseEnum(hEnum);
   
		   return hr;
	   }   


    STDMETHODIMP GetMethodProps( 
        mdMethodDef mb,                     // The method for which to get props.
        mdTypeDef   *pClass,                // Put method's class here. 
      __out_ecount_part_opt(cchMethod, *pchMethod)
        LPWSTR      szMethod,               // Put method's name here.
        ULONG       cchMethod,              // Size of szMethod buffer in wide chars.
        ULONG       *pchMethod,             // Put actual size here 
        DWORD       *pdwAttr,               // Put flags here.
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
        ULONG       *pulCodeRVA,            // [OUT] codeRVA
        DWORD       *pdwImplFlags)          // [OUT] Impl. Flags
    {
        HRESULT hr;
        HRESULT hrNameTruncation;
        if (!IsValidNonNilToken(mb, mdtMethodDef))
        {
            // Idiosyncratic edge cases - delegate straight through to inherit the correct idiosyncratic edge result
            return m_pRawImport->GetMethodProps(mb, pClass, szMethod, cchMethod, pchMethod, pdwAttr, ppvSigBlob, pcbSigBlob, pulCodeRVA, pdwImplFlags);
        }

        ULONG cbOrigSigBlob = (ULONG)(-1);
        PCCOR_SIGNATURE pOrigSig = NULL;
        IfFailRet(hrNameTruncation = m_pRawImport->GetMethodProps(mb, pClass, szMethod, cchMethod, pchMethod, pdwAttr, &pOrigSig, &cbOrigSigBlob, pulCodeRVA, pdwImplFlags));

        IfFailRet((m_pWinMDAdapter->GetSignatureForToken<IMetaDataImport2, mdtMethodDef>(
            mb, 
            &pOrigSig,          // ppOrigSig
            &cbOrigSigBlob,     // pcbOrigSig
            ppvSigBlob, 
            pcbSigBlob, 
            m_pRawImport)));

        LPCSTR szNewName = NULL;
        IfFailRet(m_pWinMDAdapter->ModifyMethodProps(mb, pdwAttr, pdwImplFlags, pulCodeRVA, &szNewName));
        if (szNewName != NULL)
        {
            // We want to return name truncation status from the method that really fills the output buffer, rewrite the previous value
            IfFailRet(hrNameTruncation = DeliverUtf8String(szNewName, szMethod, cchMethod, pchMethod));
        }
        
        // Return the success code from name filling (S_OK or CLDB_S_TRUNCATION)
        return hrNameTruncation;
    }


    STDMETHODIMP GetMemberRefProps(           // S_OK or error.
        mdMemberRef mr,                     // [IN] given memberref 
        mdToken     *ptk,                   // [OUT] Put classref or classdef here. 
      __out_ecount_part_opt(cchMember, *pchMember)
        LPWSTR      szMember,               // [OUT] buffer to fill for member's name
        ULONG       cchMember,              // [IN] the count of char of szMember
        ULONG       *pchMember,             // [OUT] actual count of char in member name
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to meta data blob value
        ULONG       *pbSig)                 // [OUT] actual size of signature blob
    {
        HRESULT hr = S_OK;
        HRESULT hrNameTruncation;

        ULONG cbOrigSigBlob = (ULONG)(-1);
        PCCOR_SIGNATURE pOrigSig = NULL;
        IfFailRet(hrNameTruncation = m_pRawImport->GetMemberRefProps(mr, ptk, szMember, cchMember, pchMember, &pOrigSig, &cbOrigSigBlob));
        LPCSTR szNewName = NULL;
        IfFailRet(m_pWinMDAdapter->ModifyMemberProps(mr, NULL, NULL, NULL, &szNewName));
        IfFailRet((m_pWinMDAdapter->GetSignatureForToken<IMetaDataImport2, mdtMemberRef>(
            mr, 
            &pOrigSig,          // ppOrigSig
            &cbOrigSigBlob,     // pcbOrigSig
            ppvSigBlob, 
            pbSig, 
            m_pRawImport)));
        if (szNewName != NULL)
        {
            // We want to return name truncation status from the method that really fills the output buffer, rewrite the previous value
            IfFailRet(hrNameTruncation = DeliverUtf8String(szNewName, szMember, cchMember, pchMember));
        }
        // Return the success code from name filling (S_OK or CLDB_S_TRUNCATION)
        return hrNameTruncation;
    }


    STDMETHODIMP EnumProperties(              // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
        mdProperty  rProperties[],          // [OUT] Put Properties here.
        ULONG       cMax,                   // [IN] Max properties to put.
        ULONG       *pcProperties)          // [OUT] Put # put here.
    {
        return m_pRawImport->EnumProperties(phEnum, td, rProperties, cMax, pcProperties);
    }


    STDMETHODIMP EnumEvents(                  // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
        mdEvent     rEvents[],              // [OUT] Put events here.
        ULONG       cMax,                   // [IN] Max events to put.
        ULONG       *pcEvents)              // [OUT] Put # put here.
    {
        return m_pRawImport->EnumEvents(phEnum, td, rEvents, cMax, pcEvents);
    }


    STDMETHODIMP GetEventProps(               // S_OK, S_FALSE, or error. 
        mdEvent     ev,                     // [IN] event token 
        mdTypeDef   *pClass,                // [OUT] typedef containing the event declarion.
        LPCWSTR     szEvent,                // [OUT] Event name 
        ULONG       cchEvent,               // [IN] the count of wchar of szEvent
        ULONG       *pchEvent,              // [OUT] actual count of wchar for event's name 
        DWORD       *pdwEventFlags,         // [OUT] Event flags.
        mdToken     *ptkEventType,          // [OUT] EventType class
        mdMethodDef *pmdAddOn,              // [OUT] AddOn method of the event
        mdMethodDef *pmdRemoveOn,           // [OUT] RemoveOn method of the event
        mdMethodDef *pmdFire,               // [OUT] Fire method of the event
        mdMethodDef rmdOtherMethod[],       // [OUT] other method of the event
        ULONG       cMax,                   // [IN] size of rmdOtherMethod
        ULONG       *pcOtherMethod)         // [OUT] total number of other method of this event 
    {
        // Returns error code from name filling (may be CLDB_S_TRUNCTATION)
        return m_pRawImport->GetEventProps(ev, pClass, szEvent, cchEvent, pchEvent, pdwEventFlags, ptkEventType, pmdAddOn, pmdRemoveOn, pmdFire, rmdOtherMethod, cMax, pcOtherMethod);
    }


    STDMETHODIMP EnumMethodSemantics(         // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdMethodDef mb,                     // [IN] MethodDef to scope the enumeration. 
        mdToken     rEventProp[],           // [OUT] Put Event/Property here.
        ULONG       cMax,                   // [IN] Max properties to put.
        ULONG       *pcEventProp)           // [OUT] Put # put here.
    {
        return m_pRawImport->EnumMethodSemantics(phEnum, mb, rEventProp, cMax, pcEventProp);
    }

    STDMETHODIMP GetMethodSemantics(          // S_OK, S_FALSE, or error. 
        mdMethodDef mb,                     // [IN] method token
        mdToken     tkEventProp,            // [IN] event/property token.
        DWORD       *pdwSemanticsFlags)       // [OUT] the role flags for the method/propevent pair 
    {
        return m_pRawImport->GetMethodSemantics(mb, tkEventProp, pdwSemanticsFlags);
    }


    STDMETHODIMP GetClassLayout( 
        mdTypeDef   td,                     // [IN] give typedef
        DWORD       *pdwPackSize,           // [OUT] 1, 2, 4, 8, or 16
        COR_FIELD_OFFSET rFieldOffset[],    // [OUT] field offset array 
        ULONG       cMax,                   // [IN] size of the array
        ULONG       *pcFieldOffset,         // [OUT] needed array size
        ULONG       *pulClassSize)              // [OUT] the size of the class
    {
        return m_pRawImport->GetClassLayout(td, pdwPackSize, rFieldOffset, cMax, pcFieldOffset, pulClassSize);
    }


    STDMETHODIMP GetFieldMarshal(
        mdToken     tk,                     // [IN] given a field's memberdef
        PCCOR_SIGNATURE *ppvNativeType,     // [OUT] native type of this field
        ULONG       *pcbNativeType)         // [OUT] the count of bytes of *ppvNativeType
    {
        return m_pRawImport->GetFieldMarshal(tk, ppvNativeType, pcbNativeType);
    }


    STDMETHODIMP GetRVA(                      // S_OK or error.
        mdToken     tk,                     // Member for which to set offset
        ULONG       *pulCodeRVA,            // The offset
        DWORD       *pdwImplFlags)          // the implementation flags 
    {
        HRESULT hr;
        if (!IsValidNonNilToken(tk, mdtMethodDef))
        {
            // Idiosyncratic edge cases - delegate straight through to inherit the correct idiosyncratic edge result
            return m_pRawImport->GetRVA(tk, pulCodeRVA, pdwImplFlags);
        }

        IfFailRet(m_pRawImport->GetRVA(tk, pulCodeRVA, pdwImplFlags));
        IfFailRet(m_pWinMDAdapter->ModifyMethodProps(tk, NULL, pdwImplFlags, pulCodeRVA, NULL));
        return hr;
    }


    STDMETHODIMP GetPermissionSetProps(
        mdPermission pm,                    // [IN] the permission token.
        DWORD       *pdwAction,             // [OUT] CorDeclSecurity.
        void const  **ppvPermission,        // [OUT] permission blob.
        ULONG       *pcbPermission)         // [OUT] count of bytes of pvPermission.
    {
        return m_pRawImport->GetPermissionSetProps(pm, pdwAction, ppvPermission, pcbPermission);
    }


    STDMETHODIMP GetSigFromToken(           // S_OK or error.
        mdSignature mdSig,                  // [IN] Signature token.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to token.
        ULONG       *pcbSig)                // [OUT] return size of signature.
    {
        // mdSignature is not part of public WinMD surface, so it does not need signature rewriting
        return m_pRawImport->GetSigFromToken(mdSig, ppvSig, pcbSig);
    }

    STDMETHODIMP GetModuleRefProps(           // S_OK or error.
        mdModuleRef mur,                    // [IN] moduleref token.
      __out_ecount_part_opt(cchName, *pchName)
        LPWSTR      szName,                 // [OUT] buffer to fill with the moduleref name.
        ULONG       cchName,                // [IN] size of szName in wide characters.
        ULONG       *pchName)               // [OUT] actual count of characters in the name.
    {
        // Returns error code from name filling (may be CLDB_S_TRUNCTATION)
        return m_pRawImport->GetModuleRefProps(mur, szName, cchName, pchName);
    }


    STDMETHODIMP EnumModuleRefs(              // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
        mdModuleRef rModuleRefs[],          // [OUT] put modulerefs here.
        ULONG       cmax,                   // [IN] max memberrefs to put.
        ULONG       *pcModuleRefs)          // [OUT] put # put here.
    {
        return m_pRawImport->EnumModuleRefs(phEnum, rModuleRefs, cmax, pcModuleRefs);
    }


    STDMETHODIMP GetTypeSpecFromToken(        // S_OK or error.
        mdTypeSpec typespec,                // [IN] TypeSpec token.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to TypeSpec signature
        ULONG       *pcbSig)                // [OUT] return size of signature.
    {
        return m_pWinMDAdapter->GetSignatureForToken<IMetaDataImport2, mdtTypeSpec>(
            typespec, 
            NULL,     // ppOrigSig
            NULL,     // pcbOrigSig
            ppvSig, 
            pcbSig, 
            m_pRawImport);
    }


    STDMETHODIMP GetNameFromToken(            // Not Recommended! May be removed!
        mdToken     tk,                     // [IN] Token to get name from.  Must have a name.
        MDUTF8CSTR  *pszUtf8NamePtr)        // [OUT] Return pointer to UTF8 name in heap.
    {
        HRESULT hr;
        
        if (!IsValidNonNilToken(tk, mdtTypeRef))
        {
            // Handle corner error case
            IfFailGo(m_pRawImport->GetNameFromToken(tk, pszUtf8NamePtr));        
        }
        else
        {
            IfFailGo(m_pWinMDAdapter->GetTypeRefProps(tk, NULL, pszUtf8NamePtr, NULL));
        }
        
      ErrExit:
        return hr;
    }


    STDMETHODIMP EnumUnresolvedMethods(       // S_OK, S_FALSE, or error. 
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken     rMethods[],             // [OUT] Put MemberDefs here.
        ULONG       cMax,                   // [IN] Max MemberDefs to put.
        ULONG       *pcTokens)              // [OUT] Put # put here.
    {
        return m_pRawImport->EnumUnresolvedMethods(phEnum, rMethods, cMax, pcTokens);
    }


    STDMETHODIMP GetUserString(               // S_OK or error.
        mdString    stk,                    // [IN] String token.
      __out_ecount_part_opt(cchString, *pchString)
        LPWSTR      szString,               // [OUT] Copy of string.
        ULONG       cchString,              // [IN] Max chars of room in szString.
        ULONG       *pchString)             // [OUT] How many chars in actual string.
    {
        // Returns error code from name filling (may be CLDB_S_TRUNCTATION)
        return m_pRawImport->GetUserString(stk, szString, cchString, pchString);
    }


    STDMETHODIMP GetPinvokeMap(               // S_OK or error.
        mdToken     tk,                     // [IN] FieldDef or MethodDef.
        DWORD       *pdwMappingFlags,       // [OUT] Flags used for mapping.
      __out_ecount_part_opt(cchImportName, *pchImportName)
        LPWSTR      szImportName,           // [OUT] Import name.
        ULONG       cchImportName,          // [IN] Size of the name buffer.
        ULONG       *pchImportName,         // [OUT] Actual number of characters stored.
        mdModuleRef *pmrImportDLL)          // [OUT] ModuleRef token for the target DLL.
    {
        // Returns error code from name filling (may be CLDB_S_TRUNCTATION)
        return m_pRawImport->GetPinvokeMap(tk, pdwMappingFlags, szImportName, cchImportName, pchImportName, pmrImportDLL);
    }


    STDMETHODIMP EnumSignatures(              // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
        mdSignature rSignatures[],          // [OUT] put signatures here.
        ULONG       cmax,                   // [IN] max signatures to put.
        ULONG       *pcSignatures)          // [OUT] put # put here.
    {
        return m_pRawImport->EnumSignatures(phEnum, rSignatures, cmax, pcSignatures);
    }


    STDMETHODIMP EnumTypeSpecs(               // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
        mdTypeSpec  rTypeSpecs[],           // [OUT] put TypeSpecs here.
        ULONG       cmax,                   // [IN] max TypeSpecs to put.
        ULONG       *pcTypeSpecs)           // [OUT] put # put here.
    {
        return m_pRawImport->EnumTypeSpecs(phEnum, rTypeSpecs, cmax, pcTypeSpecs);
    }


    STDMETHODIMP EnumUserStrings(             // S_OK or error.
        HCORENUM    *phEnum,                // [IN/OUT] pointer to the enum.
        mdString    rStrings[],             // [OUT] put Strings here.
        ULONG       cmax,                   // [IN] max Strings to put.
        ULONG       *pcStrings)             // [OUT] put # put here.
    {
        return m_pRawImport->EnumUserStrings(phEnum, rStrings, cmax, pcStrings);
    }


    STDMETHODIMP GetParamForMethodIndex(      // S_OK or error.
        mdMethodDef md,                     // [IN] Method token.
        ULONG       ulParamSeq,             // [IN] Parameter sequence.
        mdParamDef  *ppd)                   // [IN] Put Param token here.
    {
        return m_pRawImport->GetParamForMethodIndex(md, ulParamSeq, ppd);
    }


    STDMETHODIMP EnumCustomAttributes(        // S_OK or error.
        HCORENUM    *phEnum,                // [IN, OUT] COR enumerator.
        mdToken     tk,                     // [IN] Token to scope the enumeration, 0 for all.
        mdToken     tkType,                 // [IN] Type of interest, 0 for all.
        mdCustomAttribute rCustomAttributes[], // [OUT] Put custom attribute tokens here.
        ULONG       cMax,                   // [IN] Size of rCustomAttributes.
        ULONG       *pcCustomAttributes)        // [OUT, OPTIONAL] Put count of token values here.
    {
        return m_pRawImport->EnumCustomAttributes(phEnum, tk, tkType, rCustomAttributes, cMax, pcCustomAttributes);
    }


    STDMETHODIMP GetCustomAttributeProps(     // S_OK or error.
        mdCustomAttribute cv,               // [IN] CustomAttribute token.
        mdToken     *ptkObj,                // [OUT, OPTIONAL] Put object token here.
        mdToken     *ptkType,               // [OUT, OPTIONAL] Put AttrType token here.
        void const  **ppBlob,               // [OUT, OPTIONAL] Put pointer to data here.
        ULONG       *pcbSize)               // [OUT, OPTIONAL] Put size of date here.
    {
        HRESULT hr;
        if (!IsValidNonNilToken(cv, mdtCustomAttribute))
        {
            // Idiosyncratic edge cases - delegate straight through to inherit the correct idiosyncratic edge result
            return m_pRawImport->GetCustomAttributeProps(cv, ptkObj, ptkType, ppBlob, pcbSize);
        }
        IfFailRet(m_pRawImport->GetCustomAttributeProps(cv, ptkObj, ptkType, NULL, NULL));
        IfFailRet(m_pWinMDAdapter->GetCustomAttributeBlob(cv, ppBlob, pcbSize));
        return hr;
    }


    STDMETHODIMP FindTypeRef(
        mdToken     tkResolutionScope,      // [IN] ModuleRef, AssemblyRef or TypeRef.
        LPCWSTR     wzTypeName,             // [IN] TypeRef Name.
        mdTypeRef   *ptr)                   // [OUT] matching TypeRef.
    {

        HRESULT hr;
        LPUTF8      szFullName;
        LPCUTF8     szNamespace;
        LPCUTF8     szName;

        _ASSERTE(wzTypeName && ptr);
        *ptr = mdTypeRefNil;   // AV if caller passes NULL: just like the real RegMeta.

        // Convert the  name to UTF8.
        PREFIX_ASSUME(wzTypeName != NULL); // caller might pass NULL, but they'll AV (just like the real RegMeta)
        UTF8STR(wzTypeName, szFullName);
        ns::SplitInline(szFullName, szNamespace, szName);
        hr = m_pWinMDAdapter->FindTypeRef(szNamespace, szName, tkResolutionScope, ptr);

        return hr;
    }


    STDMETHODIMP GetMemberProps(
        mdToken     mb,                     // The member for which to get props.
        mdTypeDef   *pClass,                // Put member's class here. 
      __out_ecount_part_opt(cchMember, *pchMember)
        LPWSTR      szMember,               // Put member's name here.
        ULONG       cchMember,              // Size of szMember buffer in wide chars.
        ULONG       *pchMember,             // Put actual size here 
        DWORD       *pdwAttr,               // Put flags here.
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
        ULONG       *pulCodeRVA,            // [OUT] codeRVA
        DWORD       *pdwImplFlags,          // [OUT] Impl. Flags
        DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*
        UVCP_CONSTANT *ppValue,             // [OUT] constant value 
        ULONG       *pcchValue)             // [OUT] size of constant string in chars, 0 for non-strings.
    {
        HRESULT hr;
        HRESULT hrNameTruncation;
        if (!IsValidNonNilToken(mb, mdtMethodDef) && !IsValidNonNilToken(mb, mdtFieldDef))
        {
            // Idiosyncratic edge cases - delegate straight through to inherit the correct idiosyncratic edge result
            return m_pRawImport->GetMemberProps(mb, pClass, szMember, cchMember, pchMember, pdwAttr, ppvSigBlob, pcbSigBlob, pulCodeRVA, pdwImplFlags, pdwCPlusTypeFlag, ppValue, pcchValue);
        }

        PCCOR_SIGNATURE pOrigSig;
        ULONG cbOrigSig;
        
        IfFailRet(hrNameTruncation = m_pRawImport->GetMemberProps(mb, pClass, szMember, cchMember, pchMember, pdwAttr, &pOrigSig, &cbOrigSig, pulCodeRVA, pdwImplFlags, pdwCPlusTypeFlag, ppValue, pcchValue));

        LPCSTR szNewName = NULL;
        IfFailRet(m_pWinMDAdapter->ModifyMemberProps(mb, pdwAttr, pdwImplFlags, pulCodeRVA, &szNewName));
        
        if (IsValidNonNilToken(mb, mdtMethodDef))
        {        
            IfFailRet((m_pWinMDAdapter->GetSignatureForToken<IMetaDataImport2, mdtMethodDef>(
                mb, 
                &pOrigSig,          // ppOrigSig
                &cbOrigSig,         // pcbOrigSig
                ppvSigBlob, 
                pcbSigBlob, 
                m_pRawImport)));
        }
        else if (IsValidNonNilToken(mb, mdtFieldDef))
        {
            IfFailRet((m_pWinMDAdapter->GetSignatureForToken<IMetaDataImport2, mdtFieldDef>(
                mb, 
                &pOrigSig,          // ppOrigSig
                &cbOrigSig,         // pcbOrigSig
                ppvSigBlob, 
                pcbSigBlob, 
                m_pRawImport)));
        }
        else
        {
            if (ppvSigBlob != NULL)
                *ppvSigBlob = pOrigSig;

            if (pcbSigBlob != NULL)
                *pcbSigBlob = cbOrigSig;
        } 

        if (szNewName != NULL)
        {
            // We want to return name truncation status from the method that really fills the output buffer, rewrite the previous value
            IfFailRet(hrNameTruncation = DeliverUtf8String(szNewName, szMember, cchMember, pchMember));
        }
        
        // Return the success code from name filling (S_OK or CLDB_S_TRUNCATION)
        return hrNameTruncation;
    }


    STDMETHODIMP GetFieldProps(
        mdFieldDef  mb,                     // The field for which to get props.
        mdTypeDef   *pClass,                // Put field's class here.
      __out_ecount_part_opt(cchField, *pchField)
        LPWSTR      szField,                // Put field's name here.
        ULONG       cchField,               // Size of szField buffer in wide chars.
        ULONG       *pchField,              // Put actual size here 
        DWORD       *pdwAttr,               // Put flags here.
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
        DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*
        UVCP_CONSTANT *ppValue,             // [OUT] constant value 
        ULONG       *pcchValue)             // [OUT] size of constant string in chars, 0 for non-strings.
    {
        HRESULT hr;
        HRESULT hrNameTruncation;

        PCCOR_SIGNATURE pOrigSig;
        ULONG cbOrigSig;
        IfFailRet(hrNameTruncation = m_pRawImport->GetFieldProps(mb, pClass, szField, cchField, pchField, pdwAttr, &pOrigSig, &cbOrigSig, pdwCPlusTypeFlag, ppValue, pcchValue));

        IfFailRet(m_pWinMDAdapter->ModifyFieldDefProps(mb, pdwAttr));
        IfFailRet((m_pWinMDAdapter->GetSignatureForToken<IMetaDataImport2, mdtFieldDef>(
            mb,
            &pOrigSig, 
            &cbOrigSig, 
            ppvSigBlob, 
            pcbSigBlob, 
            m_pRawImport)));
        
        // Return the success code from name filling (S_OK or CLDB_S_TRUNCATION)
        return hrNameTruncation;
    }


    STDMETHODIMP GetPropertyProps(            // S_OK, S_FALSE, or error. 
        mdProperty  prop,                   // [IN] property token
        mdTypeDef   *pClass,                // [OUT] typedef containing the property declarion. 
        LPCWSTR     szProperty,             // [OUT] Property name
        ULONG       cchProperty,            // [IN] the count of wchar of szProperty
        ULONG       *pchProperty,           // [OUT] actual count of wchar for property name
        DWORD       *pdwPropFlags,          // [OUT] property flags.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] property type. pointing to meta data internal blob 
        ULONG       *pbSig,                 // [OUT] count of bytes in *ppvSig
        DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*
        UVCP_CONSTANT *ppDefaultValue,      // [OUT] constant value 
        ULONG       *pcchDefaultValue,      // [OUT] size of constant string in chars, 0 for non-strings.
        mdMethodDef *pmdSetter,             // [OUT] setter method of the property
        mdMethodDef *pmdGetter,             // [OUT] getter method of the property
        mdMethodDef rmdOtherMethod[],       // [OUT] other method of the property
        ULONG       cMax,                   // [IN] size of rmdOtherMethod
        ULONG       *pcOtherMethod)         // [OUT] total number of other method of this property
    {
        HRESULT hr = S_OK;
        HRESULT hrNameTruncation;

        ULONG cbOrigSigBlob = (ULONG)(-1);
        PCCOR_SIGNATURE pOrigSig = NULL;
        IfFailRet(hrNameTruncation = m_pRawImport->GetPropertyProps(prop, pClass, szProperty, cchProperty, pchProperty, pdwPropFlags, &pOrigSig, &cbOrigSigBlob, pdwCPlusTypeFlag, ppDefaultValue, pcchDefaultValue, pmdSetter, pmdGetter, rmdOtherMethod, cMax, pcOtherMethod));

        IfFailRet((m_pWinMDAdapter->GetSignatureForToken<IMetaDataImport2, mdtProperty>(
            prop, 
            &pOrigSig,          // ppOrigSig
            &cbOrigSigBlob,     // pcbOrigSig
            ppvSig, 
            pbSig, 
            m_pRawImport)));
        // Return the success code from name filling (S_OK or CLDB_S_TRUNCATION)
        return hrNameTruncation;
    }


    STDMETHODIMP GetParamProps(               // S_OK or error.
        mdParamDef  tk,                     // [IN]The Parameter.
        mdMethodDef *pmd,                   // [OUT] Parent Method token.
        ULONG       *pulSequence,           // [OUT] Parameter sequence.
      __out_ecount_part_opt(cchName, *pchName)
        LPWSTR      szName,                 // [OUT] Put name here.
        ULONG       cchName,                // [OUT] Size of name buffer.
        ULONG       *pchName,               // [OUT] Put actual size of name here.
        DWORD       *pdwAttr,               // [OUT] Put flags here.
        DWORD       *pdwCPlusTypeFlag,      // [OUT] Flag for value type. selected ELEMENT_TYPE_*.
        UVCP_CONSTANT *ppValue,             // [OUT] Constant value.
        ULONG       *pcchValue)             // [OUT] size of constant string in chars, 0 for non-strings.
    {
        // Returns error code from name filling (may be CLDB_S_TRUNCTATION)
        return m_pRawImport->GetParamProps(tk, pmd, pulSequence, szName, cchName, pchName, pdwAttr, pdwCPlusTypeFlag, ppValue, pcchValue);
    }


    STDMETHODIMP GetCustomAttributeByName(    // S_OK or error.
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCWSTR     wszName,                // [IN] Name of desired Custom Attribute.
        const void  **ppData,               // [OUT] Put pointer to data here.
        ULONG       *pcbData)               // [OUT] Put size of data here.
    {
        HRESULT hr;
        if (wszName == NULL)
        {
            hr = S_FALSE;   // Questionable response but maintains compatibility with RegMeta
        }
        else
        {
            MAKE_UTF8PTR_FROMWIDE_NOTHROW(szName, wszName);
            IfNullGo(szName);
            IfFailGo(CommonGetCustomAttributeByName(tkObj, szName, ppData, pcbData));
        }
      ErrExit:
        return hr;
    }


    STDMETHODIMP_(BOOL) IsValidToken(         // True or False.
        mdToken     tk)                     // [IN] Given token.
    {
        mdToken tokenType = TypeFromToken(tk);
        if (tokenType == mdtAssemblyRef)
            return m_pWinMDAdapter->IsValidAssemblyRefToken(tk);
        
        return m_pRawImport->IsValidToken(tk);
    }


    STDMETHODIMP GetNestedClassProps(         // S_OK or error.
        mdTypeDef   tdNestedClass,          // [IN] NestedClass token.
        mdTypeDef   *ptdEnclosingClass)       // [OUT] EnclosingClass token.
    {
        return m_pRawImport->GetNestedClassProps(tdNestedClass, ptdEnclosingClass);
    }


    STDMETHODIMP GetNativeCallConvFromSig(    // S_OK or error.
        void const  *pvSig,                 // [IN] Pointer to signature.
        ULONG       cbSig,                  // [IN] Count of signature bytes.
        ULONG       *pCallConv)             // [OUT] Put calling conv here (see CorPinvokemap).
    {
        return m_pRawImport->GetNativeCallConvFromSig(pvSig, cbSig, pCallConv);
    }


    STDMETHODIMP IsGlobal(                    // S_OK or error.
        mdToken     pd,                     // [IN] Type, Field, or Method token.
        int         *pbGlobal)              // [OUT] Put 1 if global, 0 otherwise.
    {
        return m_pRawImport->IsGlobal(pd, pbGlobal);
    }

  public:

      // ========================================================
      // IMetaDataWinMDImport methods
      // ========================================================

      // This method returns the RAW view of the metadata. Essentially removing the Adapter's projection support for the typeRef.
      STDMETHODIMP GetUntransformedTypeRefProps(
          mdTypeRef   tr,                     // [IN] TypeRef token.
          mdToken     *ptkResolutionScope,    // [OUT] Resolution scope, ModuleRef or AssemblyRef.
          __out_ecount_part_opt(cchName, *pchName)
          LPWSTR      szName,                 // [OUT] Unprojected name of the TypeRef.
          ULONG       cchName,                // [IN] Size of buffer.
          ULONG       *pchName)               // [OUT] Size of Name.
      {
          // By-pass the call to the raw importer, removing the adapter layer.
          return m_pRawImport->GetTypeRefProps(tr, ptkResolutionScope, szName, cchName, pchName);
      }

    //=========================================================
    // IMetaDataImport2 methods
    //=========================================================
    STDMETHODIMP EnumGenericParams(
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken      tk,                    // [IN] TypeDef or MethodDef whose generic parameters are requested
        mdGenericParam rGenericParams[],    // [OUT] Put GenericParams here.
        ULONG       cMax,                   // [IN] Max GenericParams to put.
        ULONG       *pcGenericParams)       // [OUT] Put # put here.
    {
        return m_pRawImport->EnumGenericParams(phEnum, tk, rGenericParams, cMax, pcGenericParams);
    }

    
    STDMETHODIMP GetGenericParamProps(        // S_OK or error.
        mdGenericParam gp,                  // [IN] GenericParam
        ULONG        *pulParamSeq,          // [OUT] Index of the type parameter
        DWORD        *pdwParamFlags,        // [OUT] Flags, for future use (e.g. variance)
        mdToken      *ptOwner,              // [OUT] Owner (TypeDef or MethodDef)
        DWORD       *reserved,              // [OUT] For future use (e.g. non-type parameters)
      __out_ecount_part_opt(cchName, *pchName)
        LPWSTR       wzname,                // [OUT] Put name here
        ULONG        cchName,               // [IN] Size of buffer
        ULONG        *pchName)              // [OUT] Put size of name (wide chars) here.
    {
        // Returns error code from name filling (may be CLDB_S_TRUNCTATION)
        return m_pRawImport->GetGenericParamProps(gp, pulParamSeq, pdwParamFlags, ptOwner, reserved, wzname, cchName, pchName);
    }

    
    STDMETHODIMP GetMethodSpecProps(
        mdMethodSpec mi,                    // [IN] The method instantiation
        mdToken *tkParent,                  // [OUT] MethodDef or MemberRef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob)            // [OUT] actual size of signature blob
    {
        HRESULT hr = S_OK;

        ULONG cbOrigSigBlob = (ULONG)(-1);
        PCCOR_SIGNATURE pOrigSig = NULL;
        IfFailRet(m_pRawImport->GetMethodSpecProps(mi, tkParent, &pOrigSig, &cbOrigSigBlob));

        return m_pWinMDAdapter->GetSignatureForToken<IMetaDataImport2, mdtMethodSpec>(
            mi, 
            &pOrigSig,          // ppOrigSig
            &cbOrigSigBlob,     // pcbOrigSig
            ppvSigBlob, 
            pcbSigBlob, 
            m_pRawImport);
    }

    
    STDMETHODIMP EnumGenericParamConstraints(
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdGenericParam tk,                  // [IN] GenericParam whose constraints are requested
        mdGenericParamConstraint rGenericParamConstraints[],    // [OUT] Put GenericParamConstraints here.
        ULONG       cMax,                   // [IN] Max GenericParamConstraints to put.
        ULONG       *pcGenericParamConstraints)       // [OUT] Put # put here.
    {
        return m_pRawImport->EnumGenericParamConstraints(phEnum, tk, rGenericParamConstraints, cMax, pcGenericParamConstraints);
    }

    
    STDMETHODIMP GetGenericParamConstraintProps( // S_OK or error.
        mdGenericParamConstraint gpc,       // [IN] GenericParamConstraint
        mdGenericParam *ptGenericParam,     // [OUT] GenericParam that is constrained
        mdToken      *ptkConstraintType)       // [OUT] TypeDef/Ref/Spec constraint
    {
        return m_pRawImport->GetGenericParamConstraintProps(gpc, ptGenericParam, ptkConstraintType);
    }

    
    STDMETHODIMP GetPEKind(                   // S_OK or error.
        DWORD* pdwPEKind,                   // [OUT] The kind of PE (0 - not a PE)
        DWORD* pdwMachine)                  // [OUT] Machine as defined in NT header
    {
        return m_pRawImport->GetPEKind(pdwPEKind, pdwMachine);
    }

    
    STDMETHODIMP GetVersionString(            // S_OK or error.
      __out_ecount_part_opt(ccBufSize, *pccBufSize)
        LPWSTR      pwzBuf,                 // [OUT] Put version string here.
        DWORD       ccBufSize,              // [IN] size of the buffer, in wide chars
        DWORD       *pccBufSize)            // [OUT] Size of the version string, wide chars, including terminating nul.
    {
        HRESULT hr;
        LPCSTR szVersion;
        IfFailRet(GetVersionString(&szVersion));
        // Returns error code from name filling (may be CLDB_S_TRUNCTATION)
        return DeliverUtf8String(szVersion, pwzBuf, ccBufSize, pccBufSize);
    }

    
    STDMETHODIMP EnumMethodSpecs(
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken      tk,                    // [IN] MethodDef or MemberRef whose MethodSpecs are requested
        mdMethodSpec rMethodSpecs[],        // [OUT] Put MethodSpecs here.
        ULONG       cMax,                   // [IN] Max tokens to put.
        ULONG       *pcMethodSpecs)         // [OUT] Put actual count here.
    {
        return m_pRawImport->EnumMethodSpecs(phEnum, tk, rMethodSpecs, cMax, pcMethodSpecs);
    }


  public:
    //=========================================================
    // IMetaDataAssemblyImport methods
    //=========================================================

    STDMETHODIMP GetAssemblyProps(            // S_OK or error.
        mdAssembly  mda,                    // [IN] The Assembly for which to get the properties.
        const void  **ppbPublicKey,         // [OUT] Pointer to the public key.
        ULONG       *pcbPublicKey,          // [OUT] Count of bytes in the public key.
        ULONG       *pulHashAlgId,          // [OUT] Hash Algorithm.
        __out_ecount_part_opt(cchName, *pchName) LPWSTR  szName, // [OUT] Buffer to fill with assembly's simply name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        ASSEMBLYMETADATA *pMetaData,        // [OUT] Assembly MetaData.
        DWORD       *pdwAssemblyFlags)          // [OUT] Flags.
    {
        return m_pRawAssemblyImport->GetAssemblyProps(mda, ppbPublicKey, pcbPublicKey, pulHashAlgId, szName, cchName, pchName, pMetaData, pdwAssemblyFlags);
    }


    STDMETHODIMP GetAssemblyRefProps(         // S_OK or error.
        mdAssemblyRef mdar,                 // [IN] The AssemblyRef for which to get the properties.
        const void  **ppbPublicKeyOrToken,  // [OUT] Pointer to the public key or token.
        ULONG       *pcbPublicKeyOrToken,   // [OUT] Count of bytes in the public key or token.
        __out_ecount_part_opt(cchName, *pchName)LPWSTR szName, // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        ASSEMBLYMETADATA *pMetaData,        // [OUT] Assembly MetaData.
        const void  **ppbHashValue,         // [OUT] Hash blob.
        ULONG       *pcbHashValue,          // [OUT] Count of bytes in the hash blob.
        DWORD       *pdwAssemblyRefFlags)       // [OUT] Flags.
    {
        HRESULT hr;
        HRESULT hrNameTruncation;

        if (!IsValidNonNilToken(mdar, mdtAssemblyRef))
        {
            // Idiosyncratic edge cases - delegate straight through to inherit the correct idiosyncratic edge result
            return m_pRawAssemblyImport->GetAssemblyRefProps(mdar, ppbPublicKeyOrToken, pcbPublicKeyOrToken, szName, cchName, pchName, pMetaData, ppbHashValue, pcbHashValue, pdwAssemblyRefFlags);
        }

        mdAssemblyRef md = mdar;
        if (RidFromToken(md) > m_pWinMDAdapter->GetRawAssemblyRefCount())
        {
            // The extra framework assemblies we add references to should all have the
            // same verion, key, culture, etc as those of mscorlib.
            // So we retrieve the mscorlib properties and change the name.
            md = m_pWinMDAdapter->GetAssemblyRefMscorlib();
        }

        IfFailRet(hrNameTruncation = m_pRawAssemblyImport->GetAssemblyRefProps(md, ppbPublicKeyOrToken, pcbPublicKeyOrToken, szName, cchName, pchName, pMetaData, ppbHashValue, pcbHashValue, pdwAssemblyRefFlags));

        LPCSTR szNewName = nullptr;
        USHORT *pusMajorVersion = nullptr;
        USHORT *pusMinorVersion = nullptr;
        USHORT *pusBuildNumber = nullptr;
        USHORT *pusRevisionNumber = nullptr;

        if (pMetaData != nullptr)
        {
            pusMajorVersion = &pMetaData->usMajorVersion;
            pusMinorVersion = &pMetaData->usMinorVersion;
            pusBuildNumber = &pMetaData->usBuildNumber;
            pusRevisionNumber = &pMetaData->usRevisionNumber;
        }

        m_pWinMDAdapter->ModifyAssemblyRefProps(
            mdar,
            ppbPublicKeyOrToken,
            pcbPublicKeyOrToken,
            &szNewName,
            pusMajorVersion,
            pusMinorVersion,
            pusBuildNumber,
            pusRevisionNumber,
            ppbHashValue,
            pcbHashValue);
        
        if (szNewName != nullptr)
        {
            IfFailRet(hrNameTruncation = DeliverUtf8String(szNewName, szName, cchName, pchName));
        }

        // Return the success code from name filling (S_OK or CLDB_S_TRUNCATION)
        return hrNameTruncation;
    }


    STDMETHODIMP GetFileProps(                // S_OK or error.
        mdFile      mdf,                    // [IN] The File for which to get the properties.
        __out_ecount_part_opt(cchName, *pchName) LPWSTR      szName, // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        const void  **ppbHashValue,         // [OUT] Pointer to the Hash Value Blob.
        ULONG       *pcbHashValue,          // [OUT] Count of bytes in the Hash Value Blob.
        DWORD       *pdwFileFlags)          // [OUT] Flags.
    {
        // Returns error code from name filling (may be CLDB_S_TRUNCTATION)
        return m_pRawAssemblyImport->GetFileProps(mdf, szName, cchName, pchName, ppbHashValue, pcbHashValue, pdwFileFlags);
    }


    STDMETHODIMP GetExportedTypeProps(        // S_OK or error.
        mdExportedType   mdct,              // [IN] The ExportedType for which to get the properties.
        __out_ecount_part_opt(cchName, *pchName) LPWSTR      szName, // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef or mdExportedType.
        mdTypeDef   *ptkTypeDef,            // [OUT] TypeDef token within the file.
        DWORD       *pdwExportedTypeFlags)       // [OUT] Flags.
    {
        HRESULT hr;
        LPCSTR szUtf8Namespace;
        LPCSTR szUtf8Name;
        if (!IsValidNonNilToken(mdct, mdtExportedType))
        {
            // Idiosyncractic edge cases - delegate straight through to inherit the correct idiosyncractic edge results 
            return m_pRawAssemblyImport->GetExportedTypeProps(mdct, szName, cchName, pchName, ptkImplementation, ptkTypeDef, pdwExportedTypeFlags);
        }
        IfFailRet(m_pRawAssemblyImport->GetExportedTypeProps(mdct, NULL, 0, NULL, NULL, ptkTypeDef, pdwExportedTypeFlags));
        IfFailRet(this->CommonGetExportedTypeProps(mdct, &szUtf8Namespace, &szUtf8Name, ptkImplementation));
        // Returns error code from name filling (may be CLDB_S_TRUNCTATION)
        return DeliverUtf8NamespaceAndName(szUtf8Namespace, szUtf8Name, szName, cchName, pchName);
    }


    STDMETHODIMP GetManifestResourceProps(    // S_OK or error.
        mdManifestResource  mdmr,           // [IN] The ManifestResource for which to get the properties.
        __out_ecount_part_opt(cchName, *pchName)LPWSTR      szName,  // [OUT] Buffer to fill with name.
        ULONG       cchName,                // [IN] Size of buffer in wide chars.
        ULONG       *pchName,               // [OUT] Actual # of wide chars in name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ManifestResource.
        DWORD       *pdwOffset,             // [OUT] Offset to the beginning of the resource within the file.
        DWORD       *pdwResourceFlags)      // [OUT] Flags.
    {
        // Returns error code from name filling (may be CLDB_S_TRUNCTATION)
        return m_pRawAssemblyImport->GetManifestResourceProps(mdmr, szName, cchName, pchName, ptkImplementation, pdwOffset, pdwResourceFlags);
    }


    STDMETHODIMP EnumAssemblyRefs(            // S_OK or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdAssemblyRef rAssemblyRefs[],      // [OUT] Put AssemblyRefs here.
        ULONG       cMax,                   // [IN] Max AssemblyRefs to put.
        ULONG       *pcTokens)              // [OUT] Put # put here.
    {
        if (*phEnum != NULL)
        {
            // No trick needed: a previous call to EnumAssemblyRefs must have already taken care of the
            // extra assembly refs.
            return m_pRawAssemblyImport->EnumAssemblyRefs(phEnum, rAssemblyRefs, cMax, pcTokens);
        }
        else
        {
            // if *phEnum is NULL, we need create the HENUMInternal, adjust the assembly ref count, 
            // and enumerate the number of assembly refs requested. This is done in three steps:
            HRESULT hr;

            // Step 1: Call EnumAssemblyRefs with an empty buffer to create the HENUMInternal
            IfFailGo(m_pRawAssemblyImport->EnumAssemblyRefs(phEnum, NULL, 0, NULL));

            {
                // Step 2: Increment the count to include the extra assembly refs
                HENUMInternal *phInternalEnum = static_cast<HENUMInternal*>(*phEnum);

                _ASSERTE(phInternalEnum->m_EnumType == MDSimpleEnum);

                _ASSERTE( phInternalEnum->m_ulCount == m_pWinMDAdapter->GetRawAssemblyRefCount());
                int n = m_pWinMDAdapter->GetExtraAssemblyRefCount();
                phInternalEnum->m_ulCount += n;
                phInternalEnum->u.m_ulEnd += n;
            }

            // Step 3: Call EnumAssemblyRefs again and pass in the modifed HENUMInternal and the real buffer
            IfFailGo(m_pRawAssemblyImport->EnumAssemblyRefs(phEnum, rAssemblyRefs, cMax, pcTokens));

ErrExit:
            return hr;
        }
    }


    STDMETHODIMP EnumFiles(                   // S_OK or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdFile      rFiles[],               // [OUT] Put Files here.
        ULONG       cMax,                   // [IN] Max Files to put.
        ULONG       *pcTokens)              // [OUT] Put # put here.
    {
        return m_pRawAssemblyImport->EnumFiles(phEnum, rFiles, cMax, pcTokens);
    }


    STDMETHODIMP EnumExportedTypes(           // S_OK or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdExportedType   rExportedTypes[],  // [OUT] Put ExportedTypes here.
        ULONG       cMax,                   // [IN] Max ExportedTypes to put.
        ULONG       *pcTokens)              // [OUT] Put # put here.
    {
        return m_pRawAssemblyImport->EnumExportedTypes(phEnum, rExportedTypes, cMax, pcTokens);
    }


    STDMETHODIMP EnumManifestResources(       // S_OK or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdManifestResource  rManifestResources[],   // [OUT] Put ManifestResources here.
        ULONG       cMax,                   // [IN] Max Resources to put.
        ULONG       *pcTokens)              // [OUT] Put # put here.
    {
        return m_pRawAssemblyImport->EnumManifestResources(phEnum, rManifestResources, cMax, pcTokens);
    }


    STDMETHODIMP GetAssemblyFromScope(        // S_OK or error
        mdAssembly  *ptkAssembly)           // [OUT] Put token here.
    {
        return m_pRawAssemblyImport->GetAssemblyFromScope(ptkAssembly);
    }


    STDMETHODIMP FindExportedTypeByName(     // S_OK or error
        LPCWSTR     wszName,                 // [IN] Name of the ExportedType.
        mdToken     mdtExportedType,         // [IN] ExportedType for the enclosing class.
        mdExportedType   *ptkExportedType)   // [OUT] Put the ExportedType token here.
    {
        if (wszName == NULL)
            return E_INVALIDARG;

        LPUTF8      szFullName;
        LPCUTF8     szNamespace;
        LPCUTF8     szName;
        // Convert the  name to UTF8.
        UTF8STR(wszName, szFullName);
        ns::SplitInline(szFullName, szNamespace, szName);
        return this->CommonFindExportedType(szNamespace, szName, mdtExportedType, ptkExportedType);
    }


    STDMETHODIMP FindManifestResourceByName(  // S_OK or error
        LPCWSTR     szName,                 // [IN] Name of the ManifestResource.
        mdManifestResource *ptkManifestResource)        // [OUT] Put the ManifestResource token here.
    {
        return m_pRawAssemblyImport->FindManifestResourceByName(szName, ptkManifestResource);
    }



    STDMETHODIMP FindAssembliesByName(        // S_OK or error
        LPCWSTR  szAppBase,                 // [IN] optional - can be NULL
        LPCWSTR  szPrivateBin,              // [IN] optional - can be NULL
        LPCWSTR  szAssemblyName,            // [IN] required - this is the assembly you are requesting
        IUnknown *ppIUnk[],                 // [OUT] put IMetaDataAssemblyImport pointers here
        ULONG    cMax,                      // [IN] The max number to put
        ULONG    *pcAssemblies)             // [OUT] The number of assemblies returned.
    {
        return m_pRawAssemblyImport->FindAssembliesByName(szAppBase, szPrivateBin, szAssemblyName, ppIUnk, cMax, pcAssemblies);
    }

    //=========================================================
    // IMetaDataValidate
    //=========================================================
    STDMETHODIMP ValidatorInit(               // S_OK or error.
        DWORD       dwModuleType,           // [IN] Specifies the type of the module.
        IUnknown    *pUnk)                  // [IN] Validation error handler.
    {
        if (m_pRawValidate == nullptr)
            return E_NOTIMPL;

        return m_pRawValidate->ValidatorInit(dwModuleType, pUnk);
    }

    STDMETHODIMP ValidateMetaData()            // S_OK or error.
    {
        if (m_pRawValidate == nullptr)
            return E_NOTIMPL;

        return m_pRawValidate->ValidateMetaData();
    }

    //=========================================================
    // IMDCommon methods
    //=========================================================
    STDMETHODIMP_(IMetaModelCommon*) GetMetaModelCommon()
    {
        return this;
    }

    STDMETHODIMP_(IMetaModelCommonRO*) GetMetaModelCommonRO()
    {
        _ASSERTE(!"WinMDImport does not support GetMetaModelCommonRO(). The most likely cause of this assert is that you're trying to wrap a WinMD adapter around another WinMD adapter.");
        return NULL;
    }

    STDMETHODIMP GetVersionString(LPCSTR *pszVersionString)
    {
        return m_pWinMDAdapter->GetVersionString(pszVersionString);
    }


    //=========================================================
    // IMetaModelCommon methods
    //=========================================================
    __checkReturn 
    virtual HRESULT CommonGetScopeProps(
        LPCUTF8     *pszName,
        GUID        *pMvid)
    {
        return m_pRawMetaModelCommonRO->CommonGetScopeProps(pszName, pMvid);
    }

    __checkReturn 
    virtual HRESULT CommonGetTypeRefProps(
        mdTypeRef tr,
        LPCUTF8     *pszNamespace,
        LPCUTF8     *pszName,
        mdToken     *ptkResolution)
    {
        return m_pWinMDAdapter->GetTypeRefProps(tr, pszNamespace, pszName, ptkResolution);
    }
    
    
    __checkReturn 
    virtual HRESULT CommonGetTypeDefProps(
        mdTypeDef td,
        LPCUTF8     *pszNameSpace,
        LPCUTF8     *pszName,
        DWORD       *pdwFlags,
        mdToken     *pdwExtends,
        ULONG       *pMethodList)
    {
        HRESULT hr;
        IfFailGo(m_pRawMetaModelCommonRO->CommonGetTypeDefProps(td, NULL, NULL, NULL, NULL, pMethodList));
        IfFailGo(m_pWinMDAdapter->GetTypeDefProps(td, pszNameSpace, pszName, pdwFlags, pdwExtends));
      ErrExit:
        return hr;
    }
    
    
    __checkReturn 
    virtual HRESULT CommonGetTypeSpecProps(
        mdTypeSpec ts,
        PCCOR_SIGNATURE *ppvSig,
        ULONG       *pcbSig)
    {
        return m_pWinMDAdapter->GetSignatureForToken<IMetaDataImport2, mdtTypeSpec>(
            ts, 
            NULL,     // ppOrigSig
            NULL,     // pcbOrigSig
            ppvSig, 
            pcbSig, 
            m_pRawImport);
    }
    
    
    __checkReturn 
    virtual HRESULT CommonGetEnclosingClassOfTypeDef(
        mdTypeDef  td, 
        mdTypeDef *ptkEnclosingTypeDef)
    {
        return m_pRawMetaModelCommonRO->CommonGetEnclosingClassOfTypeDef(td, ptkEnclosingTypeDef);
    }
    
    
    __checkReturn 
    virtual HRESULT CommonGetAssemblyProps(
        USHORT      *pusMajorVersion,
        USHORT      *pusMinorVersion,
        USHORT      *pusBuildNumber,
        USHORT      *pusRevisionNumber,
        DWORD       *pdwFlags,
        const void  **ppbPublicKey,
        ULONG       *pcbPublicKey,
        LPCUTF8     *pszName,
        LPCUTF8     *pszLocale)
    {
        return m_pRawMetaModelCommonRO->CommonGetAssemblyProps(pusMajorVersion, pusMinorVersion, pusBuildNumber, pusRevisionNumber, pdwFlags, ppbPublicKey, pcbPublicKey, pszName, pszLocale);
    }
    
    
    __checkReturn 
    virtual HRESULT CommonGetAssemblyRefProps(
        mdAssemblyRef tkAssemRef,
        USHORT      *pusMajorVersion,
        USHORT      *pusMinorVersion,
        USHORT      *pusBuildNumber,
        USHORT      *pusRevisionNumber,
        DWORD       *pdwFlags,
        const void  **ppbPublicKeyOrToken,
        ULONG       *pcbPublicKeyOrToken,
        LPCUTF8     *pszName,
        LPCUTF8     *pszLocale,
        const void  **ppbHashValue,
        ULONG       *pcbHashValue)
    {
        HRESULT hr;

        mdAssemblyRef md = tkAssemRef;
        if (RidFromToken(md) > m_pWinMDAdapter->GetRawAssemblyRefCount())
        {
            // The extra framework assemblies we add references to should all have the
            // same verion, key, culture, etc as those of mscorlib.
            // So we retrieve the mscorlib properties and change the name.
            md = m_pWinMDAdapter->GetAssemblyRefMscorlib();
        }

        IfFailRet(m_pRawMetaModelCommonRO->CommonGetAssemblyRefProps(
            md, 
            pusMajorVersion, 
            pusMinorVersion, 
            pusBuildNumber, 
            pusRevisionNumber, 
            pdwFlags, 
            ppbPublicKeyOrToken, 
            pcbPublicKeyOrToken, 
            pszName, 
            pszLocale, 
            ppbHashValue, 
            pcbHashValue));

        m_pWinMDAdapter->ModifyAssemblyRefProps(
            tkAssemRef,
            ppbPublicKeyOrToken,
            pcbPublicKeyOrToken,
            pszName,
            pusMajorVersion,
            pusMinorVersion,
            pusBuildNumber,
            pusRevisionNumber,
            ppbHashValue,
            pcbHashValue);

        return hr;
    }
    
    
    __checkReturn 
    virtual HRESULT CommonGetModuleRefProps(
        mdModuleRef tkModuleRef,
        LPCUTF8     *pszName)
    {
        return m_pRawMetaModelCommonRO->CommonGetModuleRefProps(tkModuleRef, pszName);
    }
    
    
    __checkReturn 
    virtual HRESULT CommonFindExportedType(
        LPCUTF8     szNamespace,
        LPCUTF8     szName,
        mdToken     tkEnclosingType,
        mdExportedType   *ptkExportedType)
    {
        return m_pWinMDAdapter->FindExportedType(szNamespace, szName, tkEnclosingType, ptkExportedType);
    }
    
    
    __checkReturn 
    virtual HRESULT CommonGetExportedTypeProps(
        mdToken     tkExportedType,
        LPCUTF8     *pszNamespace,
        LPCUTF8     *pszName,
        mdToken     *ptkImpl)
    {
        HRESULT hr;
        IfFailRet(m_pRawMetaModelCommonRO->CommonGetExportedTypeProps(tkExportedType, pszNamespace, pszName, ptkImpl));
        IfFailRet(m_pWinMDAdapter->ModifyExportedTypeName(tkExportedType, pszNamespace, pszName));
        return hr;
    }
    
    
    virtual int CommonIsRo()
    {
        return m_pRawMetaModelCommonRO->CommonIsRo();
    }
    
    
    __checkReturn 
    virtual HRESULT CommonGetCustomAttributeByNameEx( // S_OK or error.
        mdToken            tkObj,            // [IN] Object with Custom Attribute.
        LPCUTF8            szName,           // [IN] Name of desired Custom Attribute.
        mdCustomAttribute *ptkCA,            // [OUT] put custom attribute token here
        const void       **ppData,           // [OUT] Put pointer to data here.
        ULONG             *pcbData)          // [OUT] Put size of data here.
    {
        return m_pWinMDAdapter->GetCustomAttributeByName(tkObj, szName, ptkCA, ppData, pcbData);
    }
    

    __checkReturn 
    virtual HRESULT FindParentOfMethodHelper(mdMethodDef md, mdTypeDef *ptd)
    {
        return m_pRawMetaModelCommonRO->FindParentOfMethodHelper(md, ptd);
    }



 public:
    //=========================================================
    // IGetIMDInternalImport methods
    //=========================================================
    STDMETHODIMP GetIMDInternalImport(IMDInternalImport ** ppIMDInternalImport)
    {
        HRESULT hr = S_OK;
        ReleaseHolder<IMDCommon> pMDCommon;
        ReleaseHolder<IUnknown> pRawMDInternalImport;

        *ppIMDInternalImport = NULL;
        // Get the raw IMDInternalImport
        IfFailGo(GetMDInternalInterfaceFromPublic(m_pRawImport, IID_IMDInternalImport, (LPVOID*)(&pRawMDInternalImport)));
    
        // Create an adapter around the internal raw interface
        IfFailGo(pRawMDInternalImport->QueryInterface(IID_IMDCommon, (LPVOID*)(&pMDCommon)));
        IfFailGo(CreateWinMDInternalImportRO(pMDCommon, IID_IMDInternalImport, (void**)ppIMDInternalImport));
    
      ErrExit:
        return hr;
    }


    //=========================================================
    // Private methods
    //=========================================================

    //------------------------------------------------------------------------------------------------------
    // Deliver a result string (Unicode) to a caller's sized output buffer using the standard convention
    // followed by all metadata api.
    //------------------------------------------------------------------------------------------------------
    static HRESULT DeliverUnicodeString(LPCWSTR wszResult, __out_ecount_part(cchCallerBuffer, *pchSizeNeeded) LPWSTR wszCallerBuffer, ULONG cchCallerBuffer, ULONG *pchSizeNeeded)
    {
        ULONG cchActual = (ULONG)(wcslen(wszResult) + 1);
        if (pchSizeNeeded)
        {
            *pchSizeNeeded = cchActual;
        }
        if (wszCallerBuffer == NULL || cchCallerBuffer < cchActual)
        {
            if (wszCallerBuffer != NULL)
            {
                memcpy(wszCallerBuffer, wszResult, cchCallerBuffer * sizeof(WCHAR)); // If buffer too small, return truncated result to be compatible with metadata api conventions
                if (cchCallerBuffer > 0)
                {   // null-terminate the truncated output string
                    wszCallerBuffer[cchCallerBuffer - 1] = W('\0');
                }
            }
            return CLDB_S_TRUNCATION;
        }
        else
        {
            memcpy(wszCallerBuffer, wszResult, cchActual * sizeof(WCHAR));
            return S_OK;
        }
    }

    //------------------------------------------------------------------------------------------------------
    // Deliver a result string (Utf8) to a caller's sized output buffer using the standard convention
    // followed by all metadata api.
    //------------------------------------------------------------------------------------------------------
    static HRESULT DeliverUtf8String(LPCSTR szUtf8Result, __out_ecount_part(cchCallerBuffer, *pchSizeNeeded) LPWSTR wszCallerBuffer, ULONG cchCallerBuffer, ULONG *pchSizeNeeded)
    {
        MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzResult, szUtf8Result);
        if (wzResult == NULL)
            return E_OUTOFMEMORY;
        return DeliverUnicodeString(wzResult, wszCallerBuffer, cchCallerBuffer, pchSizeNeeded);
    }

    //------------------------------------------------------------------------------------------------------
    // Combine a result namespace/name string pair (Utf8) to a Unicode fullname and deliver to a caller's
    // sized output buffer using the standard convention followed by all metadata api.
    //------------------------------------------------------------------------------------------------------
    static HRESULT DeliverUtf8NamespaceAndName(LPCSTR szUtf8Namespace, LPCSTR szUtf8Name, __out_ecount_part(cchCallerBuffer, *pchSizeNeeded) LPWSTR wszCallerBuffer, ULONG cchCallerBuffer, ULONG *pchSizeNeeded)
    {
        HRESULT hr;
        if (wszCallerBuffer != NULL || pchSizeNeeded != NULL)
        {
            MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzNamespace, szUtf8Namespace);
            IfNullRet(wzNamespace);
            MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzName, szUtf8Name);
            IfNullRet(wzName);

            BOOL fTruncation = FALSE;
            if (wszCallerBuffer != NULL)
            {
                fTruncation = !(ns::MakePath(wszCallerBuffer, cchCallerBuffer, wzNamespace, wzName));
                if (fTruncation && (cchCallerBuffer > 0))
                {   // null-terminate the truncated output string
                    wszCallerBuffer[cchCallerBuffer - 1] = W('\0');
                }
            }
            if (pchSizeNeeded != NULL)
            {
                if (fTruncation || (wszCallerBuffer == NULL))
                {
                    *pchSizeNeeded = ns::GetFullLength(wzNamespace, wzName);
                }
                else
                {
                    *pchSizeNeeded = (ULONG)(wcslen(wszCallerBuffer) + 1);
                }
            }
            hr = fTruncation ? CLDB_S_TRUNCATION : S_OK;
        }
        else
        {
            hr = S_OK; // Caller did not request name back.
        }
        return hr;
    }

  private:
    //=========================================================
    // Private instance data
    //=========================================================
    IMDCommon               *m_pRawMDCommon;
    IMetaDataImport2        *m_pRawImport;
    IMetaDataAssemblyImport *m_pRawAssemblyImport;
    IMetaDataValidate       *m_pRawValidate;
    IMetaModelCommonRO      *m_pRawMetaModelCommonRO;
    IUnknown                *m_pFreeThreadedMarshaler;
    WinMDAdapter            *m_pWinMDAdapter;
    LONG                     m_cRef;

};  // class WinMDImport



//========================================================================================
// Entrypoint called by IMetaDataDispenser::OpenScope()
//========================================================================================
HRESULT CreateWinMDImport(IMDCommon * pRawMDCommon, REFIID riid, /*[out]*/ void **ppWinMDImport)
{
    HRESULT hr;
    *ppWinMDImport = NULL;
    WinMDImport *pWinMDImport = NULL;
    IfFailGo(WinMDImport::Create(pRawMDCommon, &pWinMDImport));
    IfFailGo(pWinMDImport->QueryInterface(riid, ppWinMDImport));
    hr = S_OK;
  ErrExit:
    if (pWinMDImport)
        pWinMDImport->Release();
    return hr;
}
