// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#include "stdafx.h"
#include "winmdinterfaces.h"
#include "inc/adapter.h"


#ifdef FEATURE_METADATA_INTERNAL_APIS


__checkReturn
HRESULT TranslateSigHelper(                 // S_OK or error.
    IMDInternalImport       *pImport,       // [IN] import scope.
    IMDInternalImport       *pAssemImport,  // [IN] import assembly scope.
    const void              *pbHashValue,   // [IN] hash value for the import assembly.
    ULONG                   cbHashValue,    // [IN] count of bytes in the hash value.
    PCCOR_SIGNATURE         pbSigBlob,      // [IN] signature in the importing scope
    ULONG                   cbSigBlob,      // [IN] count of bytes of signature
    IMetaDataAssemblyEmit   *pAssemEmit,    // [IN] assembly emit scope.
    IMetaDataEmit           *emit,          // [IN] emit interface
    CQuickBytes             *pqkSigEmit,    // [OUT] buffer to hold translated signature
    ULONG                   *pcbSig);       // [OUT] count of bytes in the translated signature


//========================================================================================
// This metadata importer is used internally by the runtime and exposes an
// IMDInternalImport* on .winmd files. It applies a small number of on-the-fly
// conversions to make the .winmd file look like a regular .NET assembly.
//
// All those places in src\vm where the runtime calls an IMDInternalImport*
// pointer, it may now be talking to a WinMDInternalImportRO. Ideally, the
// runtime will never know the difference (but this being an internal interface,
// we may tolerate the occasional leakiness in the name of expediency.)
//========================================================================================
class WinMDInternalImportRO : public IMDInternalImport, IWinMDImport, IMetaModelCommon
{
  public:
    //=========================================================
    // Factory
    //=========================================================
    static HRESULT Create(IMDCommon *pRawMDCommon, WinMDInternalImportRO **ppWinMDInternalImportRO)
    {
        HRESULT hr;
        *ppWinMDInternalImportRO = NULL;
        WinMDInternalImportRO *pNewWinMDInternalImport = new (nothrow) WinMDInternalImportRO(pRawMDCommon);
        IfFailGo(pRawMDCommon->QueryInterface(IID_IMDInternalImport, (void**)&(pNewWinMDInternalImport->m_pRawInternalImport)));
        IfFailGo(WinMDAdapter::Create(pNewWinMDInternalImport->m_pRawMDCommon, &(pNewWinMDInternalImport->m_pWinMDAdapter)));
        (*ppWinMDInternalImportRO = pNewWinMDInternalImport)->AddRef();
        hr = S_OK;

      ErrExit:
        if (pNewWinMDInternalImport)
            pNewWinMDInternalImport->Release();
        return hr;
    }

  private:
    //=========================================================
    // Ctors, Dtors
    //=========================================================
    WinMDInternalImportRO(IMDCommon * pRawMDCommon)
        : m_cRef(1)
        , m_pWinMDAdapter(NULL)
        , m_pRawInternalImport(NULL)
        , m_pRawMDCommon(pRawMDCommon)
        , m_pRawMetaModelCommon(pRawMDCommon->GetMetaModelCommon())
    {
        m_pRawMDCommon->AddRef();
    }

    //---------------------------------------------------------
    ~WinMDInternalImportRO()
    {
        m_pRawMDCommon->Release();
        m_pRawInternalImport->Release();
        delete m_pWinMDAdapter;
    }

  public:
    //=========================================================
    // IUnknown methods
    //=========================================================
    STDMETHODIMP QueryInterface(REFIID riid, void** ppUnk)
    {
        *ppUnk = 0;
        if (riid == IID_IUnknown || riid == IID_IWinMDImport)
            *ppUnk = (IWinMDImport *) this;
        else if (riid == IID_IMDInternalImport)
            *ppUnk = (IMDInternalImport *) this;
        else
        {
#ifndef DACCESS_COMPILE
#ifdef _DEBUG
            if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MD_WinMD_AssertOnIllegalUsage))
            {
                if (riid == IID_IMDInternalImportENC)
                    _ASSERTE(!"WinMDInternalImportRO::QueryInterface(IID_IMDInternalImportENC) returning E_NOINTERFACE");
                else if (riid == IID_IMarshal)
                    _ASSERTE(!"WinMDInternalImportRO::QueryInterface(IID_IMarshal) returning E_NOINTERFACE");
                else
                    _ASSERTE(!"WinMDInternalImportRO::QueryInterface() returning E_NOINTERFACE");
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
    // IMDInternalImport methods
    //=========================================================
    //*****************************************************************************
    // return the count of entries of a given kind in a scope
    // For example, pass in mdtMethodDef will tell you how many MethodDef
    // contained in a scope
    //*****************************************************************************
    STDMETHODIMP_(ULONG) GetCountWithTokenKind(// return hresult
        DWORD       tkKind)            // [IN] pass in the kind of token.
    {
        if (tkKind == mdtAssemblyRef)
        {
            return m_pRawInternalImport->GetCountWithTokenKind(tkKind) + m_pWinMDAdapter->GetExtraAssemblyRefCount();
        }
        else
        {
            return m_pRawInternalImport->GetCountWithTokenKind(tkKind);
        }
    }

    //*****************************************************************************
    // enumerator for typedef
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP EnumTypeDefInit(             // return hresult
        HENUMInternal *phEnum)         // [OUT] buffer to fill for enumerator data
    {
        return m_pRawInternalImport->EnumTypeDefInit(phEnum);
    }

    //*****************************************************************************
    // enumerator for MethodImpl
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP EnumMethodImplInit(        // return hresult
        mdTypeDef       td,                 // [IN] TypeDef over which to scope the enumeration.
        HENUMInternal   *phEnumBody,        // [OUT] buffer to fill for enumerator data for MethodBody tokens.
        HENUMInternal   *)                  // [OUT] used only on RW imports
    {
        HRESULT hr;
        HENUMInternal::InitDynamicArrayEnum(phEnumBody);
        phEnumBody->m_tkKind = TBL_MethodImpl << 24;
        IfFailGo(m_pWinMDAdapter->AddMethodImplsToEnum(td, phEnumBody));
        hr = S_OK;

      ErrExit:
        if (FAILED(hr))
        {
            HENUMInternal::ClearEnum(phEnumBody);
            INDEBUG(HENUMInternal::ZeroEnum(phEnumBody));
        }
        return hr;
    }

    STDMETHODIMP_(ULONG) EnumMethodImplGetCount(
        HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
        HENUMInternal   *)                  // [IN] used only on RW imports

    {
        return phEnumBody->m_ulCount / 2;
    }

    STDMETHODIMP_(void) EnumMethodImplReset(
        HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
        HENUMInternal   *)                  // [IN] used only on RW imports
    {
        phEnumBody->u.m_ulCur = phEnumBody->u.m_ulStart;
        return;
    }

    __checkReturn
    STDMETHODIMP EnumMethodImplNext(        // return hresult (S_OK = TRUE, S_FALSE = FALSE or error code)
        HENUMInternal   *phEnumBody,        // [IN] input enum for MethodBody
        HENUMInternal   *,                  // [IN] used only on RW imports
        mdToken         *ptkBody,           // [OUT] return token for MethodBody
        mdToken         *ptkDecl)           // [OUT] return token for MethodDecl
    {
        _ASSERTE(ptkBody && ptkDecl);
        return HENUMInternal::EnumWithCount(phEnumBody, 1, ptkBody, ptkDecl, NULL);
    }

    STDMETHODIMP_(void) EnumMethodImplClose(
        HENUMInternal   *phEnumBody,        // [IN] MethodBody enumerator.
        HENUMInternal   *)                  // [IN] used only on RW imports
    {
        HENUMInternal::ClearEnum(phEnumBody);
    }

    //*****************************************
    // Enumerator helpers for memberdef, memberref, interfaceimp,
    // event, property, exception, param
    //*****************************************

    __checkReturn
    STDMETHODIMP EnumGlobalFunctionsInit(     // return hresult
        HENUMInternal   *phEnum)       // [OUT] buffer to fill for enumerator data
    {
        return m_pRawInternalImport->EnumGlobalFunctionsInit(phEnum);
    }

    __checkReturn
    STDMETHODIMP EnumGlobalFieldsInit(        // return hresult
        HENUMInternal   *phEnum)       // [OUT] buffer to fill for enumerator data
    {
        return m_pRawInternalImport->EnumGlobalFieldsInit(phEnum);
    }

    __checkReturn
    STDMETHODIMP EnumInit(                    // return S_FALSE if record not found
        DWORD       tkKind,                 // [IN] which table to work on
        mdToken     tkParent,               // [IN] token to scope the search
        HENUMInternal *phEnum)         // [OUT] the enumerator to fill
    {
        if (tkKind == (TBL_MethodImpl << 24))
            return EnumMethodImplInit(tkParent, phEnum, NULL);

        HRESULT hr;
        IfFailGo(m_pRawInternalImport->EnumInit(tkKind, tkParent, phEnum));

        _ASSERTE(phEnum->m_EnumType == MDSimpleEnum);

        if (tkKind == mdtAssemblyRef)
        {
            _ASSERTE( phEnum->m_ulCount == m_pWinMDAdapter->GetRawAssemblyRefCount());
            int n = m_pWinMDAdapter->GetExtraAssemblyRefCount();
            phEnum->m_ulCount += n;
            phEnum->u.m_ulEnd += n;
        }


ErrExit:
        return hr;
    }

    __checkReturn
    STDMETHODIMP EnumAllInit(                 // return S_FALSE if record not found
        DWORD       tkKind,                 // [IN] which table to work on
        HENUMInternal *phEnum)         // [OUT] the enumerator to fill
    {
        HRESULT hr;
        IfFailGo(m_pRawInternalImport->EnumAllInit(tkKind, phEnum));

        _ASSERTE(phEnum->m_EnumType == MDSimpleEnum);

        if (tkKind == mdtAssemblyRef)
        {
            _ASSERTE( phEnum->m_ulCount == m_pWinMDAdapter->GetRawAssemblyRefCount());
            int n = m_pWinMDAdapter->GetExtraAssemblyRefCount();
            phEnum->m_ulCount += n;
            phEnum->u.m_ulEnd += n;
        }

ErrExit:
        return hr;
    }

    //*****************************************
    // Enumerator helpers for CustomAttribute
    //*****************************************
    __checkReturn
    STDMETHODIMP EnumCustomAttributeByNameInit(// return S_FALSE if record not found
        mdToken     tkParent,               // [IN] token to scope the search
        LPCSTR      szName,                 // [IN] CustomAttribute's name to scope the search
        HENUMInternal *phEnum)         // [OUT] the enumerator to fill
    {
        WinMDAdapter::ConvertWellKnownTypeNameFromClrToWinRT(&szName);
        return m_pRawInternalImport->EnumCustomAttributeByNameInit(tkParent, szName, phEnum);
    }

    //*****************************************
    // Nagivator helper to navigate back to the parent token given a token.
    // For example, given a memberdef token, it will return the containing typedef.
    //
    // the mapping is as following:
    //  ---given child type---------parent type
    //  mdMethodDef                 mdTypeDef
    //  mdFieldDef                  mdTypeDef
    //  mdInterfaceImpl             mdTypeDef
    //  mdParam                     mdMethodDef
    //  mdProperty                  mdTypeDef
    //  mdEvent                     mdTypeDef
    //
    //*****************************************
    __checkReturn
    STDMETHODIMP GetParentToken(
        mdToken     tkChild,                // [IN] given child token
        mdToken     *ptkParent)        // [OUT] returning parent
    {
        return m_pRawInternalImport->GetParentToken(tkChild, ptkParent);
    }

    //*****************************************
    // Custom value helpers
    //*****************************************
    __checkReturn
    STDMETHODIMP GetCustomAttributeProps(     // S_OK or error.
        mdCustomAttribute at,               // [IN] The attribute.
        mdToken     *ptkType)          // [OUT] Put attribute type here.
    {
        return m_pRawInternalImport->GetCustomAttributeProps(at, ptkType);
    }

    __checkReturn
    STDMETHODIMP GetCustomAttributeAsBlob(
        mdCustomAttribute cv,               // [IN] given custom value token
        void const  **ppBlob,               // [OUT] return the pointer to internal blob
        ULONG       *pcbSize)          // [OUT] return the size of the blob
    {
        return m_pWinMDAdapter->GetCustomAttributeBlob(cv, ppBlob, pcbSize);
    }

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHODIMP GetScopeProps(
        LPCSTR      *pszName,               // [OUT] scope name
        GUID        *pmvid)            // [OUT] version id
    {
        return m_pRawInternalImport->GetScopeProps(pszName, pmvid);
    }

    // The default signature comparison function.
    static BOOL CompareSignatures(PCCOR_SIGNATURE pvFirstSigBlob,       // First signature
                                  DWORD           cbFirstSigBlob,       //
                                  PCCOR_SIGNATURE pvSecondSigBlob,      // Second signature
                                  DWORD           cbSecondSigBlob,      //
                                  void *          SigArguments)         // No additional arguments required
    {
        if (cbFirstSigBlob != cbSecondSigBlob || memcmp(pvFirstSigBlob, pvSecondSigBlob, cbSecondSigBlob))
            return FALSE;
        else
            return TRUE;
    }

    // finding a particular method
    __checkReturn
    STDMETHODIMP FindMethodDef(
        mdTypeDef   classdef,               // [IN] given typedef
        LPCSTR      szName,                 // [IN] member name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdMethodDef *pmd)              // [OUT] matching memberdef
    {
        return FindMethodDefUsingCompare(classdef,
                                         szName,
                                         pvSigBlob,
                                         cbSigBlob,
                                         CompareSignatures,
                                         NULL,
                                         pmd);
    }

    // return a iSeq's param given a MethodDef
    __checkReturn
    STDMETHODIMP FindParamOfMethod(           // S_OK or error.
        mdMethodDef md,                     // [IN] The owning method of the param.
        ULONG       iSeq,                   // [IN] The sequence # of the param.
        mdParamDef  *pparamdef)        // [OUT] Put ParamDef token here.
    {
        return m_pRawInternalImport->FindParamOfMethod(md, iSeq, pparamdef);
    }

    //*****************************************
    //
    // GetName* functions
    //
    //*****************************************

    // return the name and namespace of typedef
    __checkReturn
    STDMETHODIMP GetNameOfTypeDef(
        mdTypeDef   classdef,               // given classdef
        LPCSTR      *pszname,               // return class name(unqualified)
        LPCSTR      *psznamespace)     // return the name space name
    {
        if (TypeFromToken(classdef) != mdtTypeDef)
            return CLDB_E_INTERNALERROR;

        HRESULT hr;
        IfFailRet(m_pWinMDAdapter->GetTypeDefProps(classdef, psznamespace, pszname, NULL, NULL));
        return hr;
    }

    __checkReturn
    STDMETHODIMP GetIsDualOfTypeDef(
        mdTypeDef   classdef,               // [IN] given classdef.
        ULONG       *pDual)            // [OUT] return dual flag here.
    {
        return m_pRawInternalImport->GetIsDualOfTypeDef(classdef, pDual);
    }

    __checkReturn
    STDMETHODIMP GetIfaceTypeOfTypeDef(
        mdTypeDef   classdef,               // [IN] given classdef.
        ULONG       *pIface)           // [OUT] 0=dual, 1=vtable, 2=dispinterface
    {
        return m_pRawInternalImport->GetIfaceTypeOfTypeDef(classdef, pIface);
    }

    // get the name of either methoddef
    __checkReturn
    STDMETHODIMP GetNameOfMethodDef(  // return the name of the memberdef in UTF8
        mdMethodDef md,             // given memberdef
        LPCSTR     *pszName)
    {
        HRESULT hr = S_OK;
        IfFailRet(m_pRawInternalImport->GetNameOfMethodDef(md, pszName));
        return m_pWinMDAdapter->ModifyMethodProps(md, NULL, NULL, NULL, pszName);
    }

    __checkReturn
    STDMETHODIMP GetNameAndSigOfMethodDef(
        mdMethodDef      methoddef,         // [IN] given memberdef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to a blob value of CLR signature
        ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
        LPCSTR          *pszName)
    {
        PCCOR_SIGNATURE pOrigSig;
        ULONG           cbOrigSig;
        HRESULT hr = S_OK;
        IfFailRet(m_pRawInternalImport->GetNameAndSigOfMethodDef(methoddef, &pOrigSig, &cbOrigSig, pszName));
        IfFailRet(m_pWinMDAdapter->ModifyMethodProps(methoddef, NULL, NULL, NULL, pszName));

        return m_pWinMDAdapter->GetSignatureForToken<IMDInternalImport, mdtMethodDef>(
            methoddef,
            &pOrigSig,          // ppOrigSig
            &cbOrigSig,         // pcbOrigSig
            ppvSigBlob,
            pcbSigBlob,
            m_pRawInternalImport);
    }

    // return the name of a FieldDef
    __checkReturn
    STDMETHODIMP GetNameOfFieldDef(
        mdFieldDef fd,              // given memberdef
        LPCSTR    *pszName)
    {
        return m_pRawInternalImport->GetNameOfFieldDef(fd, pszName);
    }

    // return the name of typeref
    __checkReturn
    STDMETHODIMP GetNameOfTypeRef(
        mdTypeRef   classref,               // [IN] given typeref
        LPCSTR      *psznamespace,          // [OUT] return typeref name
        LPCSTR      *pszname)          // [OUT] return typeref namespace
    {
        mdToken resolutionScope;
        return m_pWinMDAdapter->GetTypeRefProps(classref, psznamespace, pszname, &resolutionScope);
    }

    // return the resolutionscope of typeref
    __checkReturn
    STDMETHODIMP GetResolutionScopeOfTypeRef(
        mdTypeRef classref,                     // given classref
        mdToken  *ptkResolutionScope)
    {
        LPCSTR sznamespace;
        LPCSTR szname;
        return m_pWinMDAdapter->GetTypeRefProps(classref, &sznamespace, &szname, ptkResolutionScope);
    }

    // Find the type token given the name.
    __checkReturn
    STDMETHODIMP FindTypeRefByName(
        LPCSTR      szNamespace,            // [IN] Namespace for the TypeRef.
        LPCSTR      szName,                 // [IN] Name of the TypeRef.
        mdToken     tkResolutionScope,      // [IN] Resolution Scope fo the TypeRef.
        mdTypeRef   *ptk)              // [OUT] TypeRef token returned.
    {
        return m_pWinMDAdapter->FindTypeRef(szNamespace, szName, tkResolutionScope, ptk);
    }

    // return the TypeDef properties
    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHODIMP GetTypeDefProps(
        mdTypeDef   classdef,               // given classdef
        DWORD       *pdwAttr,               // return flags on class, tdPublic, tdAbstract
        mdToken     *ptkExtends)       // [OUT] Put base class TypeDef/TypeRef here
    {
        HRESULT hr;
        IfFailGo(m_pWinMDAdapter->GetTypeDefProps(classdef, NULL, NULL, pdwAttr, ptkExtends));
      ErrExit:
        return hr;
    }

    // return the item's guid
    __checkReturn
    STDMETHODIMP GetItemGuid(
        mdToken     tkObj,                  // [IN] given item.
        CLSID       *pGuid)            // [out[ put guid here.
    {
        HRESULT hr;
        IfFailGo(m_pWinMDAdapter->GetItemGuid(tkObj, pGuid));

        if (hr == S_FALSE)
        {
            // if this is not a WinRT type, also look for System.Guid by falling back to the raw internal MD import
            if (TypeFromToken(tkObj) == mdtTypeDef)
            {
                DWORD dwAttr;
                IfFailGo(m_pWinMDAdapter->GetTypeDefProps(tkObj, NULL, NULL, &dwAttr, NULL));

                if (!IsTdWindowsRuntime(dwAttr))
                {
                    IfFailGo(m_pRawInternalImport->GetItemGuid(tkObj, pGuid));
                }
                else
                {
                    // reset the return value to S_FALSE if the type is WinRT
                    hr = S_FALSE;
                }
            }
        }

      ErrExit:
        return hr;
    }

    // Get enclosing class of the NestedClass.
    __checkReturn
    STDMETHODIMP GetNestedClassProps(         // S_OK or error
        mdTypeDef   tkNestedClass,          // [IN] NestedClass token.
        mdTypeDef   *ptkEnclosingClass)  // [OUT] EnclosingClass token.
    {
        return m_pRawInternalImport->GetNestedClassProps(tkNestedClass, ptkEnclosingClass);
    }

    // Get count of Nested classes given the enclosing class.
    __checkReturn
    STDMETHODIMP GetCountNestedClasses(   // return count of Nested classes.
        mdTypeDef   tkEnclosingClass,   // Enclosing class.
        ULONG      *pcNestedClassesCount)
    {
        return m_pRawInternalImport->GetCountNestedClasses(tkEnclosingClass, pcNestedClassesCount);
    }

    // Return array of Nested classes given the enclosing class.
    __checkReturn
    STDMETHODIMP GetNestedClasses(        // Return actual count.
        mdTypeDef   tkEnclosingClass,       // [IN] Enclosing class.
        mdTypeDef   *rNestedClasses,        // [OUT] Array of nested class tokens.
        ULONG       ulNestedClasses,        // [IN] Size of array.
        ULONG      *pcNestedClasses)
    {
        return m_pRawInternalImport->GetNestedClasses(tkEnclosingClass, rNestedClasses, ulNestedClasses, pcNestedClasses);
    }

    // return the ModuleRef properties
    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHODIMP GetModuleRefProps(
        mdModuleRef mur,                    // [IN] moduleref token
        LPCSTR      *pszName)          // [OUT] buffer to fill with the moduleref name
    {
        return m_pRawInternalImport->GetModuleRefProps(mur, pszName);
    }

    //*****************************************
    //
    // GetSig* functions
    //
    //*****************************************
    __checkReturn
    STDMETHODIMP GetSigOfMethodDef(
        mdMethodDef      methoddef,     // [IN] given memberdef
        ULONG           *pcbSigBlob,    // [OUT] count of bytes in the signature blob
        PCCOR_SIGNATURE *ppSig)
    {
        return m_pWinMDAdapter->GetSignatureForToken<IMDInternalImport, mdtMethodDef>(
            methoddef,
            NULL,
            NULL,
            ppSig,
            pcbSigBlob,
            m_pRawInternalImport);
    }

    __checkReturn
    STDMETHODIMP GetSigOfFieldDef(
        mdFieldDef       fielddef,      // [IN] given fielddef
        ULONG           *pcbSigBlob,    // [OUT] count of bytes in the signature blob
        PCCOR_SIGNATURE *ppSig)
    {
        return m_pWinMDAdapter->GetSignatureForToken<IMDInternalImport, mdtFieldDef>(
            fielddef,
            NULL,
            NULL,
            ppSig,
            pcbSigBlob,
            m_pRawInternalImport);
    }

    __checkReturn
    STDMETHODIMP GetSigFromToken(
        mdToken           tk, // FieldDef, MethodDef, Signature or TypeSpec token
        ULONG *           pcbSig,
        PCCOR_SIGNATURE * ppSig)
    {
        if (TypeFromToken(tk) == mdtMethodDef)
        {
            return GetSigOfMethodDef(tk, pcbSig, ppSig);
        }
        else if (TypeFromToken(tk) == mdtFieldDef)
        {
            return GetSigOfFieldDef(tk, pcbSig, ppSig);
        }
        else if (TypeFromToken(tk) == mdtTypeSpec)
        {
            return GetTypeSpecFromToken(tk, ppSig, pcbSig);
        }
        // Note: mdtSignature is not part of public WinMD surface, so it does not need signature rewriting - just call the underlying "raw" implementation
        return m_pRawInternalImport->GetSigFromToken(tk, pcbSig, ppSig);
    }



    //*****************************************
    // get method property
    //*****************************************
    __checkReturn
    STDMETHODIMP GetMethodDefProps(
        mdMethodDef md,                 // The method for which to get props.
        DWORD      *pdwFlags)
    {
        HRESULT hr;
        IfFailGo(m_pRawInternalImport->GetMethodDefProps(md, pdwFlags));
        IfFailGo(m_pWinMDAdapter->ModifyMethodProps(md, pdwFlags, NULL, NULL, NULL));
      ErrExit:
        return hr;
    }

    //*****************************************
    // return method implementation informaiton, like RVA and implflags
    //*****************************************
    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHODIMP GetMethodImplProps(
        mdToken     tk,                     // [IN] MethodDef
        ULONG       *pulCodeRVA,            // [OUT] CodeRVA
        DWORD       *pdwImplFlags)     // [OUT] Impl. Flags
    {
        HRESULT hr;
        IfFailGo(m_pRawInternalImport->GetMethodImplProps(tk, pulCodeRVA, pdwImplFlags));
        IfFailGo(m_pWinMDAdapter->ModifyMethodProps(tk, NULL, pdwImplFlags, pulCodeRVA, NULL));
      ErrExit:
        return hr;
    }

    //*****************************************
    // return method implementation informaiton, like RVA and implflags
    //*****************************************
    __checkReturn
    STDMETHODIMP GetFieldRVA(
        mdFieldDef  fd,                     // [IN] fielddef
        ULONG       *pulCodeRVA)       // [OUT] CodeRVA
    {
        return m_pRawInternalImport->GetFieldRVA(fd, pulCodeRVA);
    }

    //*****************************************
    // get field property
    //*****************************************
    __checkReturn
    STDMETHODIMP GetFieldDefProps(
        mdFieldDef fd,              // [IN] given fielddef
        DWORD     *pdwFlags)   // [OUT] return fdPublic, fdPrive, etc flags
    {
        HRESULT hr;
        IfFailGo(m_pRawInternalImport->GetFieldDefProps(fd, pdwFlags));
        IfFailGo(m_pWinMDAdapter->ModifyFieldDefProps(fd, pdwFlags));
ErrExit:
        return hr;
    }

    //*****************************************************************************
    // return default value of a token(could be paramdef, fielddef, or property
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP GetDefaultValue(
        mdToken     tk,                     // [IN] given FieldDef, ParamDef, or Property
        MDDefaultValue *pDefaultValue) // [OUT] default value to fill
    {
        return m_pRawInternalImport->GetDefaultValue(tk, pDefaultValue);
    }


    //*****************************************
    // get dispid of a MethodDef or a FieldDef
    //*****************************************
    __checkReturn
    STDMETHODIMP GetDispIdOfMemberDef(        // return hresult
        mdToken     tk,                     // [IN] given methoddef or fielddef
        ULONG       *pDispid)          // [OUT] Put the dispid here.
    {
        return m_pRawInternalImport->GetDispIdOfMemberDef(tk, pDispid);
    }

    //*****************************************
    // return TypeRef/TypeDef given an InterfaceImpl token
    //*****************************************
    __checkReturn
    STDMETHODIMP GetTypeOfInterfaceImpl(  // return the TypeRef/typedef token for the interfaceimpl
        mdInterfaceImpl iiImpl,         // given a interfaceimpl
        mdToken        *ptkType)
    {
        return m_pRawInternalImport->GetTypeOfInterfaceImpl(iiImpl, ptkType);
    }

    //*****************************************
    // look up function for TypeDef
    //*****************************************
    __checkReturn
    STDMETHODIMP FindTypeDef(
        LPCSTR      szNamespace,            // [IN] Namespace for the TypeDef.
        LPCSTR      szName,                 // [IN] Name of the TypeDef.
        mdToken     tkEnclosingClass,       // [IN] TypeRef/TypeDef Token for the enclosing class.
        mdTypeDef   *ptypedef)         // [IN] return typedef
    {
        return m_pWinMDAdapter->FindTypeDef(szNamespace, szName, tkEnclosingClass, ptypedef);
    }

    //*****************************************
    // return name and sig of a memberref
    //*****************************************
    __checkReturn
    STDMETHODIMP GetNameAndSigOfMemberRef(    // return name here
        mdMemberRef      memberref,         // given memberref
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to a blob value of CLR signature
        ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
        LPCSTR          *pszName)
    {
        HRESULT hr = S_OK;
        PCCOR_SIGNATURE pOrigSig;
        ULONG           cbOrigSig;

        IfFailRet(m_pRawInternalImport->GetNameAndSigOfMemberRef(memberref, &pOrigSig, &cbOrigSig, pszName));
        IfFailRet(m_pWinMDAdapter->ModifyMemberProps(memberref, NULL, NULL, NULL, pszName));

        return m_pWinMDAdapter->GetSignatureForToken<IMDInternalImport, mdtMemberRef>(
            memberref,
            &pOrigSig,          // ppOrigSig
            &cbOrigSig,         // pcbOrigSig
            ppvSigBlob,
            pcbSigBlob,
            m_pRawInternalImport);
    }

    //*****************************************************************************
    // Given memberref, return the parent. It can be TypeRef, ModuleRef, MethodDef
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP GetParentOfMemberRef(
        mdMemberRef memberref,          // given memberref
        mdToken    *ptkParent)     // return the parent token
    {
        return m_pRawInternalImport->GetParentOfMemberRef(memberref, ptkParent);
    }

    __checkReturn
    STDMETHODIMP GetParamDefProps(
        mdParamDef paramdef,            // given a paramdef
        USHORT    *pusSequence,         // [OUT] slot number for this parameter
        DWORD     *pdwAttr,             // [OUT] flags
        LPCSTR    *pszName)        // [OUT] return the name of the parameter
    {
        return m_pRawInternalImport->GetParamDefProps(paramdef, pusSequence, pdwAttr, pszName);
    }

    __checkReturn
    STDMETHODIMP GetPropertyInfoForMethodDef( // Result.
        mdMethodDef md,                     // [IN] memberdef
        mdProperty  *ppd,                   // [OUT] put property token here
        LPCSTR      *pName,                 // [OUT] put pointer to name here
        ULONG       *pSemantic)        // [OUT] put semantic here
    {
        return m_pRawInternalImport->GetPropertyInfoForMethodDef(md, ppd, pName, pSemantic);
    }

    //*****************************************
    // class layout/sequence information
    //*****************************************
    __checkReturn
    STDMETHODIMP GetClassPackSize(            // return error if class doesn't have packsize
        mdTypeDef   td,                     // [IN] give typedef
        ULONG       *pdwPackSize)      // [OUT] 1, 2, 4, 8, or 16
    {
        return m_pRawInternalImport->GetClassPackSize(td, pdwPackSize);
    }

    __checkReturn
    STDMETHODIMP GetClassTotalSize(           // return error if class doesn't have total size info
        mdTypeDef   td,                     // [IN] give typedef
        ULONG       *pdwClassSize)     // [OUT] return the total size of the class
    {
        return m_pRawInternalImport->GetClassTotalSize(td, pdwClassSize);
    }

    __checkReturn
    STDMETHODIMP GetClassLayoutInit(
        mdTypeDef   td,                     // [IN] give typedef
        MD_CLASS_LAYOUT *pLayout)      // [OUT] set up the status of query here
    {
        return m_pRawInternalImport->GetClassLayoutInit(td, pLayout);
    }

    __checkReturn
    STDMETHODIMP GetClassLayoutNext(
        MD_CLASS_LAYOUT *pLayout,           // [IN|OUT] set up the status of query here
        mdFieldDef  *pfd,                   // [OUT] return the fielddef
        ULONG       *pulOffset)        // [OUT] return the offset/ulSequence associate with it
    {
        return m_pRawInternalImport->GetClassLayoutNext(pLayout, pfd, pulOffset);
    }

    //*****************************************
    // marshal information of a field
    //*****************************************
    __checkReturn
    STDMETHODIMP GetFieldMarshal(             // return error if no native type associate with the token
        mdFieldDef  fd,                     // [IN] given fielddef
        PCCOR_SIGNATURE *pSigNativeType,    // [OUT] the native type signature
        ULONG       *pcbNativeType)    // [OUT] the count of bytes of *ppvNativeType
    {
        return m_pRawInternalImport->GetFieldMarshal(fd, pSigNativeType, pcbNativeType);
    }


    //*****************************************
    // property APIs
    //*****************************************
    // find a property by name
    __checkReturn
    STDMETHODIMP FindProperty(
        mdTypeDef   td,                     // [IN] given a typdef
        LPCSTR      szPropName,             // [IN] property name
        mdProperty  *pProp)            // [OUT] return property token
    {
        return m_pRawInternalImport->FindProperty(td, szPropName, pProp);
    }

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHODIMP GetPropertyProps(
        mdProperty  prop,                   // [IN] property token
        LPCSTR      *szProperty,            // [OUT] property name
        DWORD       *pdwPropFlags,          // [OUT] property flags.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] property type. pointing to meta data internal blob
        ULONG       *pcbSig)                // [OUT] count of bytes in *ppvSig
    {
        HRESULT hr = S_OK;
        PCCOR_SIGNATURE pOrigSig;
        ULONG           cbOrigSig;

        IfFailRet(m_pRawInternalImport->GetPropertyProps(prop, szProperty, pdwPropFlags, &pOrigSig, &cbOrigSig));

        return m_pWinMDAdapter->GetSignatureForToken<IMDInternalImport, mdtProperty>(
            prop,
            &pOrigSig,          // ppOrigSig
            &cbOrigSig,         // pcbOrigSig
            ppvSig,
            pcbSig,
            m_pRawInternalImport);
    }

    //**********************************
    // Event APIs
    //**********************************
    __checkReturn
    STDMETHODIMP FindEvent(
        mdTypeDef   td,                     // [IN] given a typdef
        LPCSTR      szEventName,            // [IN] event name
        mdEvent     *pEvent)           // [OUT] return event token
    {
        return m_pRawInternalImport->FindEvent(td, szEventName, pEvent);
    }

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHODIMP GetEventProps(
        mdEvent     ev,                     // [IN] event token
        LPCSTR      *pszEvent,              // [OUT] Event name
        DWORD       *pdwEventFlags,         // [OUT] Event flags.
        mdToken     *ptkEventType)     // [OUT] EventType class
    {
        return m_pRawInternalImport->GetEventProps(ev, pszEvent, pdwEventFlags, ptkEventType);
    }


    //**********************************
    // find a particular associate of a property or an event
    //**********************************
    __checkReturn
    STDMETHODIMP FindAssociate(
        mdToken     evprop,                 // [IN] given a property or event token
        DWORD       associate,              // [IN] given a associate semantics(setter, getter, testdefault, reset, AddOn, RemoveOn, Fire)
        mdMethodDef *pmd)              // [OUT] return method def token
    {
        return m_pRawInternalImport->FindAssociate(evprop, associate, pmd);
    }

    // Note, void function in v1.0/v1.1
    __checkReturn
    STDMETHODIMP EnumAssociateInit(
        mdToken     evprop,                 // [IN] given a property or an event token
        HENUMInternal *phEnum)         // [OUT] cursor to hold the query result
    {
        return m_pRawInternalImport->EnumAssociateInit(evprop, phEnum);
    }

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHODIMP GetAllAssociates(
        HENUMInternal *phEnum,              // [IN] query result form GetPropertyAssociateCounts
        ASSOCIATE_RECORD *pAssociateRec,    // [OUT] struct to fill for output
        ULONG       cAssociateRec)     // [IN] size of the buffer
    {
        return m_pRawInternalImport->GetAllAssociates(phEnum, pAssociateRec, cAssociateRec);
    }


    //**********************************
    // Get info about a PermissionSet.
    //**********************************
    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHODIMP GetPermissionSetProps(
        mdPermission pm,                    // [IN] the permission token.
        DWORD       *pdwAction,             // [OUT] CorDeclSecurity.
        void const  **ppvPermission,        // [OUT] permission blob.
        ULONG       *pcbPermission)    // [OUT] count of bytes of pvPermission.
    {
        return m_pRawInternalImport->GetPermissionSetProps(pm, pdwAction, ppvPermission, pcbPermission);
    }

    //****************************************
    // Get the String given the String token.
    // Returns a pointer to the string, or NULL in case of error.
    //****************************************
    __checkReturn
    STDMETHODIMP GetUserString(
        mdString stk,                   // [IN] the string token.
        ULONG   *pchString,             // [OUT] count of characters in the string.
        BOOL    *pbIs80Plus,            // [OUT] specifies where there are extended characters >= 0x80.
        LPCWSTR *pwszUserString)
    {
        return m_pRawInternalImport->GetUserString(stk, pchString, pbIs80Plus, pwszUserString);
    }

    //*****************************************************************************
    // p-invoke APIs.
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP GetPinvokeMap(
        mdToken     tk,                     // [IN] FieldDef, MethodDef.
        DWORD       *pdwMappingFlags,       // [OUT] Flags used for mapping.
        LPCSTR      *pszImportName,         // [OUT] Import name.
        mdModuleRef *pmrImportDLL)     // [OUT] ModuleRef token for the target DLL.
    {
        return m_pRawInternalImport->GetPinvokeMap(tk, pdwMappingFlags, pszImportName, pmrImportDLL);
    }

    //*****************************************************************************
    // helpers to convert a text signature to a com format
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP ConvertTextSigToComSig(      // Return hresult.
        BOOL        fCreateTrIfNotFound,    // [IN] create typeref if not found
        LPCSTR      pSignature,             // [IN] class file format signature
        CQuickBytes *pqbNewSig,             // [OUT] place holder for CLR signature
        ULONG       *pcbCount)         // [OUT] the result size of signature
    {
        return m_pRawInternalImport->ConvertTextSigToComSig(fCreateTrIfNotFound, pSignature, pqbNewSig, pcbCount);
    }

    //*****************************************************************************
    // Assembly MetaData APIs.
    //*****************************************************************************
    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHODIMP GetAssemblyProps(
        mdAssembly  mda,                    // [IN] The Assembly for which to get the properties.
        const void  **ppbPublicKey,         // [OUT] Pointer to the public key.
        ULONG       *pcbPublicKey,          // [OUT] Count of bytes in the public key.
        ULONG       *pulHashAlgId,          // [OUT] Hash Algorithm.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        AssemblyMetaDataInternal *pMetaData,// [OUT] Assembly MetaData.
        DWORD       *pdwAssemblyFlags) // [OUT] Flags.
    {
        return m_pRawInternalImport->GetAssemblyProps(mda, ppbPublicKey, pcbPublicKey, pulHashAlgId, pszName, pMetaData, pdwAssemblyFlags);
    }

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHODIMP GetAssemblyRefProps(
        mdAssemblyRef mdar,                 // [IN] The AssemblyRef for which to get the properties.
        const void  **ppbPublicKeyOrToken,  // [OUT] Pointer to the public key or token.
        ULONG       *pcbPublicKeyOrToken,   // [OUT] Count of bytes in the public key or token.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        AssemblyMetaDataInternal *pMetaData,// [OUT] Assembly MetaData.
        const void  **ppbHashValue,         // [OUT] Hash blob.
        ULONG       *pcbHashValue,          // [OUT] Count of bytes in the hash blob.
        DWORD       *pdwAssemblyRefFlags)  // [OUT] Flags.
    {
        HRESULT hr;
        mdAssemblyRef md = mdar;
        if (RidFromToken(md) > m_pWinMDAdapter->GetRawAssemblyRefCount())
        {
            // The extra framework assemblies we add references to should all have the
            // same verion, key, culture, etc as those of mscorlib.
            // So we retrieve the mscorlib properties and change the name.
            md = m_pWinMDAdapter->GetAssemblyRefMscorlib();
        }

        IfFailRet(m_pRawInternalImport->GetAssemblyRefProps(md, ppbPublicKeyOrToken, pcbPublicKeyOrToken, pszName, pMetaData, ppbHashValue, pcbHashValue, pdwAssemblyRefFlags));

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
            pszName,
            pusMajorVersion,
            pusMinorVersion,
            pusBuildNumber,
            pusRevisionNumber,
            ppbHashValue,
            pcbHashValue);

        return hr;
    }

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHODIMP GetFileProps(
        mdFile      mdf,                    // [IN] The File for which to get the properties.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        const void  **ppbHashValue,         // [OUT] Pointer to the Hash Value Blob.
        ULONG       *pcbHashValue,          // [OUT] Count of bytes in the Hash Value Blob.
        DWORD       *pdwFileFlags)     // [OUT] Flags.
    {
        return m_pRawInternalImport->GetFileProps(mdf, pszName, ppbHashValue, pcbHashValue, pdwFileFlags);
    }

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHODIMP GetExportedTypeProps(
        mdExportedType   mdct,              // [IN] The ExportedType for which to get the properties.
        LPCSTR      *pszNamespace,          // [OUT] Namespace.
        LPCSTR      *pszName,               // [OUT] Name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
        mdTypeDef   *ptkTypeDef,            // [OUT] TypeDef token within the file.
        DWORD       *pdwExportedTypeFlags)  // [OUT] Flags.
    {
        HRESULT hr;
        IfFailRet(m_pRawInternalImport->GetExportedTypeProps(mdct, pszNamespace, pszName, ptkImplementation, ptkTypeDef, pdwExportedTypeFlags));
        IfFailRet(m_pWinMDAdapter->ModifyExportedTypeName(mdct, pszNamespace, pszName));
        return hr;
    }

    // returned void in v1.0/v1.1
    __checkReturn
    STDMETHODIMP GetManifestResourceProps(
        mdManifestResource  mdmr,           // [IN] The ManifestResource for which to get the properties.
        LPCSTR      *pszName,               // [OUT] Buffer to fill with name.
        mdToken     *ptkImplementation,     // [OUT] mdFile or mdAssemblyRef that provides the ExportedType.
        DWORD       *pdwOffset,             // [OUT] Offset to the beginning of the resource within the file.
        DWORD       *pdwResourceFlags) // [OUT] Flags.
    {
        return m_pRawInternalImport->GetManifestResourceProps(mdmr, pszName, ptkImplementation, pdwOffset, pdwResourceFlags);
    }

    __checkReturn
    STDMETHODIMP FindExportedTypeByName(      // S_OK or error
        LPCSTR      szNamespace,            // [IN] Namespace of the ExportedType.
        LPCSTR      szName,                 // [IN] Name of the ExportedType.
        mdExportedType   tkEnclosingType,   // [IN] ExportedType for the enclosing class.
        mdExportedType   *pmct)        // [OUT] Put ExportedType token here.
    {
        return m_pWinMDAdapter->FindExportedType(szNamespace, szName, tkEnclosingType, pmct);
    }

    __checkReturn
    STDMETHODIMP FindManifestResourceByName(  // S_OK or error
        LPCSTR      szName,                 // [IN] Name of the ManifestResource.
        mdManifestResource *pmmr)      // [OUT] Put ManifestResource token here.
    {
        return m_pRawInternalImport->FindManifestResourceByName(szName, pmmr);
    }

    __checkReturn
    STDMETHODIMP GetAssemblyFromScope(        // S_OK or error
        mdAssembly  *ptkAssembly)      // [OUT] Put token here.
    {
        return m_pRawInternalImport->GetAssemblyFromScope(ptkAssembly);
    }

    __checkReturn
    STDMETHODIMP GetCustomAttributeByName(    // S_OK or error
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCUTF8     szName,                 // [IN] Name of desired Custom Attribute.
        const void  **ppData,               // [OUT] Put pointer to data here.
        ULONG       *pcbData)          // [OUT] Put size of data here.
    {
        return m_pWinMDAdapter->GetCustomAttributeByName(tkObj, szName, NULL, ppData, pcbData);
    }

    // Note: The return type of this method was void in v1
    __checkReturn
    STDMETHODIMP GetTypeSpecFromToken(      // S_OK or error.
        mdTypeSpec typespec,                // [IN] Signature token.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to token.
        ULONG       *pcbSig)                // [OUT] return size of signature.
    {
        return m_pWinMDAdapter->GetSignatureForToken<IMDInternalImport, mdtTypeSpec>(
            typespec,
            NULL,          // ppOrigSig
            NULL,          // pcbOrigSig
            ppvSig,
            pcbSig,
            m_pRawInternalImport);
    }

    __checkReturn
    STDMETHODIMP SetUserContextData(          // S_OK or E_NOTIMPL
        IUnknown    *pIUnk)            // The user context.
    {
        return m_pRawInternalImport->SetUserContextData(pIUnk);
    }

    __checkReturn
    STDMETHODIMP_(BOOL) IsValidToken(         // True or False.
        mdToken     tk)                // [IN] Given token.
    {
        if (TypeFromToken(tk) == mdtAssemblyRef)
            return m_pWinMDAdapter->IsValidAssemblyRefToken(tk);

        return m_pRawInternalImport->IsValidToken(tk);
    }


    __checkReturn
    STDMETHODIMP TranslateSigWithScope(
        IMDInternalImport*      pAssemImport,   // [IN] import assembly scope.
        const void*             pbHashValue,    // [IN] hash value for the import assembly.
        ULONG                   cbHashValue,    // [IN] count of bytes in the hash value.
        PCCOR_SIGNATURE         pbSigBlob,      // [IN] signature in the importing scope
        ULONG                   cbSigBlob,      // [IN] count of bytes of signature
        IMetaDataAssemblyEmit*  pAssemEmit,     // [IN] assembly emit scope.
        IMetaDataEmit*          emit,           // [IN] emit interface
        CQuickBytes*            pqkSigEmit,     // [OUT] buffer to hold translated signature
        ULONG*                  pcbSig)         // [OUT] count of bytes in the translated signature
    {
        return TranslateSigHelper(
            this,
            pAssemImport,
            pbHashValue,
            cbHashValue,
            pbSigBlob,
            cbSigBlob,
            pAssemEmit,
            emit,
            pqkSigEmit,
            pcbSig);
    }


    STDMETHODIMP_(IMetaModelCommon*) GetMetaModelCommon()   // Return MetaModelCommon interface.
    {
        return static_cast<IMetaModelCommon*>(this);
    }

    STDMETHODIMP_(IUnknown *) GetCachedPublicInterface(BOOL fWithLock)    // return the cached public interface
    {
        return m_pRawInternalImport->GetCachedPublicInterface(fWithLock);
    }
    __checkReturn

    STDMETHODIMP SetCachedPublicInterface(IUnknown *pUnk)   // no return value
    {
        return m_pRawInternalImport->SetCachedPublicInterface(pUnk);
    }

    STDMETHODIMP_(UTSemReadWrite*) GetReaderWriterLock()    // return the reader writer lock
    {
        return m_pRawInternalImport->GetReaderWriterLock();
    }

    __checkReturn
    STDMETHODIMP SetReaderWriterLock(UTSemReadWrite * pSem)
    {
        return m_pRawInternalImport->SetReaderWriterLock(pSem);
    }

    STDMETHODIMP_(mdModule) GetModuleFromScope()              // [OUT] Put mdModule token here.
    {
        return m_pRawInternalImport->GetModuleFromScope();
    }


    //-----------------------------------------------------------------
    // Additional custom methods

    // finding a particular method
    __checkReturn
    STDMETHODIMP FindMethodDefUsingCompare(
        mdTypeDef   classdef,               // [IN] given typedef
        LPCSTR      szName,                 // [IN] member name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        PSIGCOMPARE pSignatureCompare,      // [IN] Routine to compare signatures
        void*       pSignatureArgs,         // [IN] Additional info to supply the compare function
        mdMethodDef *pmd)                   // [OUT] matching memberdef
    {
        if (pvSigBlob == NULL || cbSigBlob == 0 || pSignatureCompare == NULL)
        {
            // if signature matching is not needed, we can delegate to the underlying implementation
            return m_pRawInternalImport->FindMethodDefUsingCompare(classdef, szName, pvSigBlob, cbSigBlob, pSignatureCompare, pSignatureArgs, pmd);
        }

        // The following code emulates MDInternalRO::FindMethodDefUsingCompare. We cannot call the underlying
        // implementation because we need to compare pvSigBlob to reinterpreted signatures.
        _ASSERTE(szName && pmd);

        HRESULT hr = S_OK;
        HENUMInternal hEnum;

        CQuickBytes qbSig; // holds non-varargs signature

        *pmd = mdMethodDefNil;

        // check to see if this is a vararg signature
        PCCOR_SIGNATURE pvSigTemp = pvSigBlob;
        if (isCallConv(CorSigUncompressCallingConv(pvSigTemp), IMAGE_CEE_CS_CALLCONV_VARARG))
        {
            // Get the fixed part of VARARG signature
            IfFailGo(_GetFixedSigOfVarArg(pvSigBlob, cbSigBlob, &qbSig, &cbSigBlob));
            pvSigBlob = (PCCOR_SIGNATURE) qbSig.Ptr();
        }

        // now iterate all methods in td and compare name and signature
        IfFailGo(EnumInit(mdtMethodDef, classdef, &hEnum));

        mdMethodDef md;
        while(EnumNext(&hEnum, &md))
        {
            PCCOR_SIGNATURE pvMethodSigBlob;
            ULONG cbMethodSigBlob;
            LPCSTR szMethodName;

            IfFailGo(GetNameAndSigOfMethodDef(md, &pvMethodSigBlob, &cbMethodSigBlob, &szMethodName));

            if (strcmp(szName, szMethodName) == 0)
            {
                // we have a name match, check signature
                if (pSignatureCompare(pvMethodSigBlob, cbMethodSigBlob, pvSigBlob, cbSigBlob, pSignatureArgs) == FALSE)
                    continue;

                // ignore PrivateScope methods
                DWORD dwMethodAttr;
                IfFailGo(GetMethodDefProps(md, &dwMethodAttr));

                if (IsMdPrivateScope(dwMethodAttr))
                    continue;

                // we found the method
                *pmd = md;
                goto ErrExit;
            }
        }
        hr = CLDB_E_RECORD_NOTFOUND;

    ErrExit:
        EnumClose(&hEnum);
        return hr;
    }

    // Additional v2 methods.

    //*****************************************
    // return a field offset for a given field
    //*****************************************
    __checkReturn
    STDMETHODIMP GetFieldOffset(
        mdFieldDef  fd,                     // [IN] fielddef
        ULONG       *pulOffset)             // [OUT] FieldOffset
    {
        return m_pRawInternalImport->GetFieldOffset(fd, pulOffset);
    }

    __checkReturn
    STDMETHODIMP GetMethodSpecProps(
        mdMethodSpec ms,                    // [IN] The method instantiation
        mdToken *tkParent,                  // [OUT] MethodDef or MemberRef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob)            // [OUT] actual size of signature blob
    {
        HRESULT hr = S_OK;
        PCCOR_SIGNATURE pOrigSig;
        ULONG           cbOrigSig;

        IfFailRet(m_pRawInternalImport->GetMethodSpecProps(ms, tkParent, &pOrigSig, &cbOrigSig));

        return m_pWinMDAdapter->GetSignatureForToken<IMDInternalImport, mdtMethodSpec>(
            ms,
            &pOrigSig,          // ppOrigSig
            &cbOrigSig,         // pcbOrigSig
            ppvSigBlob,
            pcbSigBlob,
            m_pRawInternalImport);
    }

    __checkReturn
    STDMETHODIMP GetTableInfoWithIndex(
        ULONG      index,                   // [IN] pass in the table index
        void       **pTable,                // [OUT] pointer to table at index
        void       **pTableSize)            // [OUT] size of table at index
    {
        // This abstraction breaker is apparently used only by SOS... at this time of writing.
        return m_pRawInternalImport->GetTableInfoWithIndex(index, pTable, pTableSize);
    }

    __checkReturn
    STDMETHODIMP ApplyEditAndContinue(
        void        *pDeltaMD,              // [IN] the delta metadata
        ULONG       cbDeltaMD,              // [IN] length of pData
        IMDInternalImport **ppv)       // [OUT] the resulting metadata interface
    {
        WINMD_COMPAT_ASSERT("IMDInternalImport::ApplyEditAndContinue() not supported on .winmd files.");
        return E_NOTIMPL;
    }

    //**********************************
    // Generics APIs
    //**********************************
    __checkReturn
    STDMETHODIMP GetGenericParamProps(        // S_OK or error.
        mdGenericParam rd,                  // [IN] The type parameter
        ULONG* pulSequence,                 // [OUT] Parameter sequence number
        DWORD* pdwAttr,                     // [OUT] Type parameter flags (for future use)
        mdToken *ptOwner,                   // [OUT] The owner (TypeDef or MethodDef)
        DWORD *reserved,                    // [OUT] The kind (TypeDef/Ref/Spec, for future use)
        LPCSTR *szName)                // [OUT] The name
    {
        return m_pRawInternalImport->GetGenericParamProps(rd, pulSequence, pdwAttr, ptOwner, reserved, szName);
    }

    __checkReturn
    STDMETHODIMP GetGenericParamConstraintProps(      // S_OK or error.
        mdGenericParamConstraint rd,            // [IN] The constraint token
        mdGenericParam *ptGenericParam,         // [OUT] GenericParam that is constrained
        mdToken      *ptkConstraintType)   // [OUT] TypeDef/Ref/Spec constraint
    {
        return m_pRawInternalImport->GetGenericParamConstraintProps(rd, ptGenericParam, ptkConstraintType);
    }

    //*****************************************************************************
    // This function gets the "built for" version of a metadata scope.
    //  NOTE: if the scope has never been saved, it will not have a built-for
    //  version, and an empty string will be returned.
    //*****************************************************************************
    __checkReturn
    STDMETHODIMP GetVersionString(    // S_OK or error.
        LPCSTR      *pVer)        // [OUT] Put version string here.
    {
        return m_pWinMDAdapter->GetVersionString(pVer);
    }

    __checkReturn
    STDMETHODIMP GetTypeDefRefTokenInTypeSpec(// return S_FALSE if enclosing type does not have a token
        mdTypeSpec  tkTypeSpec,               // [IN] TypeSpec token to look at
        mdToken    *tkEnclosedToken)          // [OUT] The enclosed type token
    {
        return m_pRawInternalImport->GetTypeDefRefTokenInTypeSpec(tkTypeSpec, tkEnclosedToken);
    }

    STDMETHODIMP_(DWORD) GetMetadataStreamVersion()   //returns DWORD with major version of
                                // MD stream in senior word and minor version--in junior word
    {
        return m_pRawInternalImport->GetMetadataStreamVersion();
    }

    __checkReturn
    STDMETHODIMP GetNameOfCustomAttribute(// S_OK or error
        mdCustomAttribute mdAttribute,      // [IN] The Custom Attribute
        LPCUTF8          *pszNamespace,     // [OUT] Namespace of Custom Attribute.
        LPCUTF8          *pszName)     // [OUT] Name of Custom Attribute.
    {
        HRESULT hr;
        IfFailRet(m_pRawInternalImport->GetNameOfCustomAttribute(mdAttribute, pszNamespace, pszName));
        WinMDAdapter::ConvertWellKnownTypeNameFromWinRTToClr(pszNamespace, pszName);
        return hr;
    }

    STDMETHODIMP SetOptimizeAccessForSpeed(// S_OK or error
        BOOL    fOptSpeed)
    {
        return m_pRawInternalImport->SetOptimizeAccessForSpeed(fOptSpeed);
    }

    STDMETHODIMP SetVerifiedByTrustedSource(// S_OK or error
        BOOL    fVerified)
    {
        return m_pRawInternalImport->SetVerifiedByTrustedSource(fVerified);
    }

    STDMETHODIMP GetRvaOffsetData(// S_OK or error
        DWORD   *pFirstMethodRvaOffset,     // [OUT] Offset (from start of metadata) to the first RVA field in MethodDef table.
        DWORD   *pMethodDefRecordSize,      // [OUT] Size of each record in MethodDef table.
        DWORD   *pMethodDefCount,           // [OUT] Number of records in MethodDef table.
        DWORD   *pFirstFieldRvaOffset,      // [OUT] Offset (from start of metadata) to the first RVA field in FieldRVA table.
        DWORD   *pFieldRvaRecordSize,       // [OUT] Size of each record in FieldRVA table.
        DWORD   *pFieldRvaCount)            // [OUT] Number of records in FieldRVA table.
    {
        return m_pRawInternalImport->GetRvaOffsetData(
            pFirstMethodRvaOffset,
            pMethodDefRecordSize,
            pMethodDefCount,
            pFirstFieldRvaOffset,
            pFieldRvaRecordSize,
            pFieldRvaCount);
    }


    // ********************************************************************************
    // **************** Implementation of IMetaModelCommon methods ********************
    // ********************************************************************************

    __checkReturn
    HRESULT CommonGetScopeProps(
        LPCUTF8     *pszName,
        GUID        *pMvid)
    {
        return m_pRawMetaModelCommon->CommonGetScopeProps(pszName, pMvid);
    }

    __checkReturn
    HRESULT CommonGetTypeRefProps(
        mdTypeRef   tr,
        LPCUTF8     *pszNamespace,
        LPCUTF8     *pszName,
        mdToken     *ptkResolution)
    {
        return m_pWinMDAdapter->GetTypeRefProps(tr, pszNamespace, pszName, ptkResolution);
    }

    __checkReturn
    HRESULT CommonGetTypeDefProps(
        mdTypeDef   td,
        LPCUTF8     *pszNameSpace,
        LPCUTF8     *pszName,
        DWORD       *pdwFlags,
        mdToken     *pdwExtends,
        ULONG       *pMethodList)
    {
        // We currently don't support retrieving the method list.
        if (pMethodList)
            return E_NOTIMPL;

        return m_pWinMDAdapter->GetTypeDefProps(td, pszNameSpace, pszName, pdwFlags, pdwExtends);
    }

    __checkReturn
    HRESULT CommonGetTypeSpecProps(
        mdTypeSpec      ts,
        PCCOR_SIGNATURE *ppvSig,
        ULONG           *pcbSig)
    {
        return m_pWinMDAdapter->GetSignatureForToken<IMDInternalImport, mdtTypeSpec>(
            ts,
            NULL,          // ppOrigSig
            NULL,          // pcbOrigSig
            ppvSig,
            pcbSig,
            m_pRawInternalImport);
    }

    __checkReturn
    HRESULT CommonGetEnclosingClassOfTypeDef(
        mdTypeDef  td,
        mdTypeDef *ptkEnclosingTypeDef)
    {
        return m_pRawMetaModelCommon->CommonGetEnclosingClassOfTypeDef(td, ptkEnclosingTypeDef);
    }

    __checkReturn
    HRESULT CommonGetAssemblyProps(
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
        HRESULT hr;
        AssemblyMetaDataInternal MetaData;
        mdToken tkAssembly = TokenFromRid(1, mdtAssembly);

        IfFailRet(GetAssemblyProps(
            tkAssembly,
            ppbPublicKey,
            pcbPublicKey,
            NULL,
            pszName,
            &MetaData,
            pdwFlags));

        if (pusMajorVersion)
            *pusMajorVersion = MetaData.usMajorVersion;
        if (pusMinorVersion)
            *pusMinorVersion = MetaData.usMinorVersion;
        if (pusBuildNumber)
            *pusBuildNumber = MetaData.usBuildNumber;
        if (pusRevisionNumber)
            *pusRevisionNumber = MetaData.usRevisionNumber;
        if (pszLocale)
            *pszLocale = MetaData.szLocale;

        return S_OK;
    }

    __checkReturn
    HRESULT CommonGetAssemblyRefProps(
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

        AssemblyMetaDataInternal MetaData;
        IfFailRet(GetAssemblyRefProps(
            tkAssemRef,
            ppbPublicKeyOrToken,
            pcbPublicKeyOrToken,
            pszName,
            &MetaData,
            ppbHashValue,
            pcbHashValue,
            pdwFlags));

        if (pusMajorVersion)
            *pusMajorVersion = MetaData.usMajorVersion;
        if (pusMinorVersion)
            *pusMinorVersion = MetaData.usMinorVersion;
        if (pusBuildNumber)
            *pusBuildNumber = MetaData.usBuildNumber;
        if (pusRevisionNumber)
            *pusRevisionNumber = MetaData.usRevisionNumber;
        if (pszLocale)
            *pszLocale = MetaData.szLocale;

        return S_OK;
    }

    __checkReturn
    HRESULT CommonGetModuleRefProps(
        mdModuleRef tkModuleRef,
        LPCUTF8     *pszName)
    {
        return m_pRawMetaModelCommon->CommonGetModuleRefProps(tkModuleRef, pszName);
    }

    __checkReturn
    HRESULT CommonFindExportedType(
        LPCUTF8     szNamespace,
        LPCUTF8     szName,
        mdToken     tkEnclosingType,
        mdExportedType   *ptkExportedType)
    {
        return m_pWinMDAdapter->FindExportedType(szNamespace, szName, tkEnclosingType, ptkExportedType);
    }

    __checkReturn
    HRESULT CommonGetExportedTypeProps(
        mdToken     tkExportedType,
        LPCUTF8     *pszNamespace,
        LPCUTF8     *pszName,
        mdToken     *ptkImpl)
    {
        return GetExportedTypeProps(
            tkExportedType,
            pszNamespace,
            pszName,
            ptkImpl,
            NULL,
            NULL);
    }

    __checkReturn
    HRESULT CommonGetCustomAttributeByNameEx( // S_OK or error.
        mdToken            tkObj,             // [IN] Object with Custom Attribute.
        LPCUTF8            szName,            // [IN] Name of desired Custom Attribute.
        mdCustomAttribute *ptkCA,             // [OUT] put custom attribute token here
        const void       **ppData,            // [OUT] Put pointer to data here.
        ULONG             *pcbData)           // [OUT] Put size of data here.
    {
        return m_pWinMDAdapter->GetCustomAttributeByName(tkObj, szName, ptkCA, ppData, pcbData);
    }

    __checkReturn
    HRESULT FindParentOfMethodHelper(mdMethodDef md, mdTypeDef *ptd)
    {
        return m_pRawMetaModelCommon->FindParentOfMethodHelper(md, ptd);
    }

    int CommonIsRo()
    {
        return m_pRawMetaModelCommon->CommonIsRo();
    }

  private:
    //=========================================================
    // Private instance data
    //=========================================================
    IMDCommon         *m_pRawMDCommon;
    IMetaModelCommon  *m_pRawMetaModelCommon;
    IMDInternalImport *m_pRawInternalImport;
    WinMDAdapter      *m_pWinMDAdapter;
    LONG               m_cRef;
};  // class WinMDInternalImportRO




//========================================================================================
// Entrypoint called by GetMDInternalInterface()
//========================================================================================
HRESULT CreateWinMDInternalImportRO(IMDCommon * pRawMDCommon, REFIID riid, /*[out]*/ void **ppWinMDInternalImport)
{
    HRESULT hr;
    *ppWinMDInternalImport = NULL;
    WinMDInternalImportRO *pNewImport = NULL;
    IfFailGo(WinMDInternalImportRO::Create(pRawMDCommon, &pNewImport));
    IfFailGo(pNewImport->QueryInterface(riid, ppWinMDInternalImport));
    hr = S_OK;

  ErrExit:
    if (pNewImport)
        pNewImport->Release();
    return hr;

}

#endif // FEATURE_METADATA_INTERNAL_APIS



