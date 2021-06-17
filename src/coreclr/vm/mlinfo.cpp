// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: mlinfo.cpp
//

//


#include "common.h"
#include "mlinfo.h"
#include "dllimport.h"
#include "sigformat.h"
#include "eeconfig.h"
#include "eehash.h"
#include "../dlls/mscorrc/resource.h"
#include "typeparse.h"
#include "comdelegate.h"
#include "olevariant.h"
#include "ilmarshalers.h"
#include "interoputil.h"

#ifdef FEATURE_PREJIT
    #include "dataimage.h"
#endif

#ifdef FEATURE_COMINTEROP
#include "comcallablewrapper.h"
#include "runtimecallablewrapper.h"
#include "dispparammarshaler.h"
#endif // FEATURE_COMINTEROP


#ifndef lengthof
    #define lengthof(rg)    (sizeof(rg)/sizeof(rg[0]))
#endif


#ifdef FEATURE_COMINTEROP
    DEFINE_ASM_QUAL_TYPE_NAME(ENUMERATOR_TO_ENUM_VARIANT_CM_NAME, g_EnumeratorToEnumClassName, g_CorelibAsmName);

    static const int        ENUMERATOR_TO_ENUM_VARIANT_CM_NAME_LEN    = lengthof(ENUMERATOR_TO_ENUM_VARIANT_CM_NAME);
    static const char       ENUMERATOR_TO_ENUM_VARIANT_CM_COOKIE[]    = {""};
    static const int        ENUMERATOR_TO_ENUM_VARIANT_CM_COOKIE_LEN  = lengthof(ENUMERATOR_TO_ENUM_VARIANT_CM_COOKIE);

    DEFINE_ASM_QUAL_TYPE_NAME(COLOR_TRANSLATOR_ASM_QUAL_TYPE_NAME, g_ColorTranslatorClassName, g_DrawingAsmName);
    DEFINE_ASM_QUAL_TYPE_NAME(COLOR_ASM_QUAL_TYPE_NAME, g_ColorClassName, g_DrawingAsmName);

    #define OLECOLOR_TO_SYSTEMCOLOR_METH_NAME   "FromOle"
    #define SYSTEMCOLOR_TO_OLECOLOR_METH_NAME   "ToOle"
#endif // FEATURE_COMINTEROP



#define INITIAL_NUM_CMHELPER_HASHTABLE_BUCKETS 32
#define INITIAL_NUM_CMINFO_HASHTABLE_BUCKETS 32
#define DEBUG_CONTEXT_STR_LEN 2000

namespace
{
    //-------------------------------------------------------------------------------------
    // Return the copy ctor for a VC class (if any exists)
    //-------------------------------------------------------------------------------------
    void FindCopyCtor(Module *pModule, MethodTable *pMT, MethodDesc **pMDOut)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;    // CompareTypeTokens may trigger GC
            MODE_ANY;
        }
        CONTRACTL_END;

        *pMDOut = NULL;

        HRESULT     hr;
        mdMethodDef tk;
        mdTypeDef cl = pMT->GetCl();
        TypeHandle th = TypeHandle(pMT);
        SigTypeContext typeContext(th);

        IMDInternalImport *pInternalImport = pModule->GetMDImport();
        MDEnumHolder      hEnumMethod(pInternalImport);

        //
        // First try for the new syntax: <MarshalCopy>
        //
        IfFailThrow(pInternalImport->EnumInit(mdtMethodDef, cl, &hEnumMethod));

        while (pInternalImport->EnumNext(&hEnumMethod, &tk))
        {
            _ASSERTE(TypeFromToken(tk) == mdtMethodDef);
            DWORD dwMemberAttrs;
            IfFailThrow(pInternalImport->GetMethodDefProps(tk, &dwMemberAttrs));

            if (IsMdSpecialName(dwMemberAttrs))
            {
                ULONG cSig;
                PCCOR_SIGNATURE pSig;
                LPCSTR pName;
                IfFailThrow(pInternalImport->GetNameAndSigOfMethodDef(tk, &pSig, &cSig, &pName));

                const char *pBaseName = "<MarshalCopy>";
                int ncBaseName = (int)strlen(pBaseName);
                int nc = (int)strlen(pName);
                if (nc >= ncBaseName && 0 == strcmp(pName + nc - ncBaseName, pBaseName))
                {
                    MetaSig msig(pSig, cSig, pModule, &typeContext);

                    // Looking for the prototype   void <MarshalCopy>(Ptr VC, Ptr VC);
                    if (msig.NumFixedArgs() == 2)
                    {
                        if (msig.GetReturnType() == ELEMENT_TYPE_VOID)
                        {
                            if (msig.NextArg() == ELEMENT_TYPE_PTR)
                            {
                                SigPointer sp1 = msig.GetArgProps();
                                IfFailThrow(sp1.GetElemType(NULL));
                                CorElementType eType;
                                IfFailThrow(sp1.GetElemType(&eType));
                                if (eType == ELEMENT_TYPE_VALUETYPE)
                                {
                                    mdToken tk1;
                                    IfFailThrow(sp1.GetToken(&tk1));
                                    hr = CompareTypeTokensNT(tk1, cl, pModule, pModule);
                                    if (FAILED(hr))
                                    {
                                        pInternalImport->EnumClose(&hEnumMethod);
                                        ThrowHR(hr);
                                    }

                                    if (hr == S_OK)
                                    {
                                        if (msig.NextArg() == ELEMENT_TYPE_PTR)
                                        {
                                            SigPointer sp2 = msig.GetArgProps();
                                            IfFailThrow(sp2.GetElemType(NULL));
                                            IfFailThrow(sp2.GetElemType(&eType));
                                            if (eType == ELEMENT_TYPE_VALUETYPE)
                                            {
                                                mdToken tk2;
                                                IfFailThrow(sp2.GetToken(&tk2));

                                                hr = (tk2 == tk1) ? S_OK : CompareTypeTokensNT(tk2, cl, pModule, pModule);
                                                if (hr == S_OK)
                                                {
                                                    *pMDOut = pModule->LookupMethodDef(tk);
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        //
        // Next try the old syntax: global .__ctor
        //
        IfFailThrow(pInternalImport->EnumGlobalFunctionsInit(&hEnumMethod));

        while (pInternalImport->EnumNext(&hEnumMethod, &tk))
        {
            _ASSERTE(TypeFromToken(tk) == mdtMethodDef);
            DWORD dwMemberAttrs;
            IfFailThrow(pInternalImport->GetMethodDefProps(tk, &dwMemberAttrs));

            if (IsMdSpecialName(dwMemberAttrs))
            {
                ULONG cSig;
                PCCOR_SIGNATURE pSig;
                LPCSTR pName;
                IfFailThrow(pInternalImport->GetNameAndSigOfMethodDef(tk, &pSig, &cSig, &pName));

                const char *pBaseName = ".__ctor";
                int ncBaseName = (int)strlen(pBaseName);
                int nc = (int)strlen(pName);
                if (nc >= ncBaseName && 0 == strcmp(pName + nc - ncBaseName, pBaseName))
                {

                    MetaSig msig(pSig, cSig, pModule, &typeContext);

                    // Looking for the prototype   Ptr VC __ctor(Ptr VC, ByRef VC);
                    if (msig.NumFixedArgs() == 2)
                    {
                        if (msig.GetReturnType() == ELEMENT_TYPE_PTR)
                        {
                            SigPointer spret = msig.GetReturnProps();
                            IfFailThrow(spret.GetElemType(NULL));
                            CorElementType eType;
                            IfFailThrow(spret.GetElemType(&eType));
                            if (eType == ELEMENT_TYPE_VALUETYPE)
                            {
                                mdToken tk0;
                                IfFailThrow(spret.GetToken(&tk0));
                                hr = CompareTypeTokensNT(tk0, cl, pModule, pModule);
                                if (FAILED(hr))
                                {
                                    pInternalImport->EnumClose(&hEnumMethod);
                                    ThrowHR(hr);
                                }

                                if (hr == S_OK)
                                {
                                    if (msig.NextArg() == ELEMENT_TYPE_PTR)
                                    {
                                        SigPointer sp1 = msig.GetArgProps();
                                        IfFailThrow(sp1.GetElemType(NULL));
                                        IfFailThrow(sp1.GetElemType(&eType));
                                        if (eType == ELEMENT_TYPE_VALUETYPE)
                                        {
                                            mdToken tk1;
                                            IfFailThrow(sp1.GetToken(&tk1));
                                            hr = (tk1 == tk0) ? S_OK : CompareTypeTokensNT(tk1, cl, pModule, pModule);
                                            if (FAILED(hr))
                                            {
                                                pInternalImport->EnumClose(&hEnumMethod);
                                                ThrowHR(hr);
                                            }

                                            if (hr == S_OK)
                                            {
                                                if (msig.NextArg() == ELEMENT_TYPE_PTR &&
                                                    msig.GetArgProps().HasCustomModifier(pModule, "Microsoft.VisualC.IsCXXReferenceModifier", ELEMENT_TYPE_CMOD_OPT))
                                                {
                                                    SigPointer sp2 = msig.GetArgProps();
                                                    IfFailThrow(sp2.GetElemType(NULL));
                                                    IfFailThrow(sp2.GetElemType(&eType));
                                                    if (eType == ELEMENT_TYPE_VALUETYPE)
                                                    {
                                                        mdToken tk2;
                                                        IfFailThrow(sp2.GetToken(&tk2));

                                                        hr = (tk2 == tk0) ? S_OK : CompareTypeTokensNT(tk2, cl, pModule, pModule);
                                                        if (hr == S_OK)
                                                        {
                                                            *pMDOut = pModule->LookupMethodDef(tk);
                                                            return;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }


    //-------------------------------------------------------------------------------------
    // Return the destructor for a VC class (if any exists)
    //-------------------------------------------------------------------------------------
    void FindDtor(Module *pModule, MethodTable *pMT, MethodDesc **pMDOut)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;    // CompareTypeTokens may trigger GC
            MODE_ANY;
        }
        CONTRACTL_END;

        *pMDOut = NULL;

        HRESULT     hr;
        mdMethodDef tk;
        mdTypeDef cl = pMT->GetCl();
        TypeHandle th = TypeHandle(pMT);
        SigTypeContext typeContext(th);

        IMDInternalImport *pInternalImport = pModule->GetMDImport();
        MDEnumHolder       hEnumMethod(pInternalImport);

        //
        // First try for the new syntax: <MarshalDestroy>
        //
        IfFailThrow(pInternalImport->EnumInit(mdtMethodDef, cl, &hEnumMethod));

        while (pInternalImport->EnumNext(&hEnumMethod, &tk))
        {
            _ASSERTE(TypeFromToken(tk) == mdtMethodDef);
            DWORD dwMemberAttrs;
            IfFailThrow(pInternalImport->GetMethodDefProps(tk, &dwMemberAttrs));

            if (IsMdSpecialName(dwMemberAttrs))
            {
                ULONG cSig;
                PCCOR_SIGNATURE pSig;
                LPCSTR pName;
                IfFailThrow(pInternalImport->GetNameAndSigOfMethodDef(tk, &pSig, &cSig, &pName));

                const char *pBaseName = "<MarshalDestroy>";
                int ncBaseName = (int)strlen(pBaseName);
                int nc = (int)strlen(pName);
                if (nc >= ncBaseName && 0 == strcmp(pName + nc - ncBaseName, pBaseName))
                {
                    MetaSig msig(pSig, cSig, pModule, &typeContext);

                    // Looking for the prototype   void <MarshalDestroy>(Ptr VC);
                    if (msig.NumFixedArgs() == 1)
                    {
                        if (msig.GetReturnType() == ELEMENT_TYPE_VOID)
                        {
                            if (msig.NextArg() == ELEMENT_TYPE_PTR)
                            {
                                SigPointer sp1 = msig.GetArgProps();
                                IfFailThrow(sp1.GetElemType(NULL));
                                CorElementType eType;
                                IfFailThrow(sp1.GetElemType(&eType));
                                if (eType == ELEMENT_TYPE_VALUETYPE)
                                {
                                    mdToken tk1;
                                    IfFailThrow(sp1.GetToken(&tk1));

                                    hr = CompareTypeTokensNT(tk1, cl, pModule, pModule);
                                    IfFailThrow(hr);

                                    if (hr == S_OK)
                                    {
                                        *pMDOut = pModule->LookupMethodDef(tk);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        //
        // Next try the old syntax: global .__dtor
        //
        IfFailThrow(pInternalImport->EnumGlobalFunctionsInit(&hEnumMethod));

        while (pInternalImport->EnumNext(&hEnumMethod, &tk))
        {
            _ASSERTE(TypeFromToken(tk) == mdtMethodDef);
            ULONG cSig;
            PCCOR_SIGNATURE pSig;
            LPCSTR pName;
            IfFailThrow(pInternalImport->GetNameAndSigOfMethodDef(tk, &pSig, &cSig, &pName));

            const char *pBaseName = ".__dtor";
            int ncBaseName = (int)strlen(pBaseName);
            int nc = (int)strlen(pName);
            if (nc >= ncBaseName && 0 == strcmp(pName + nc - ncBaseName, pBaseName))
            {
                MetaSig msig(pSig, cSig, pModule, &typeContext);

                // Looking for the prototype   void __dtor(Ptr VC);
                if (msig.NumFixedArgs() == 1)
                {
                    if (msig.GetReturnType() == ELEMENT_TYPE_VOID)
                    {
                        if (msig.NextArg() == ELEMENT_TYPE_PTR)
                        {
                            SigPointer sp1 = msig.GetArgProps();
                            IfFailThrow(sp1.GetElemType(NULL));
                            CorElementType eType;
                            IfFailThrow(sp1.GetElemType(&eType));
                            if (eType == ELEMENT_TYPE_VALUETYPE)
                            {
                                mdToken tk1;
                                IfFailThrow(sp1.GetToken(&tk1));
                                hr = CompareTypeTokensNT(tk1, cl, pModule, pModule);
                                if (FAILED(hr))
                                {
                                    pInternalImport->EnumClose(&hEnumMethod);
                                    ThrowHR(hr);
                                }

                                if (hr == S_OK)
                                {
                                    *pMDOut = pModule->LookupMethodDef(tk);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

//==========================================================================
// Set's up the custom marshaler information.
//==========================================================================
CustomMarshalerHelper *SetupCustomMarshalerHelper(LPCUTF8 strMarshalerTypeName, DWORD cMarshalerTypeNameBytes, LPCUTF8 strCookie, DWORD cCookieStrBytes, Assembly *pAssembly, TypeHandle hndManagedType)
{
#ifndef CROSSGEN_COMPILE
    CONTRACT (CustomMarshalerHelper*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pAssembly));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    EEMarshalingData *pMarshalingData = NULL;

    // The assembly is not shared so we use the current app domain's marshaling data.
    pMarshalingData = pAssembly->GetLoaderAllocator()->GetMarshalingData();

    // Retrieve the custom marshaler helper from the EE marshaling data.
    RETURN pMarshalingData->GetCustomMarshalerHelper(pAssembly, hndManagedType, strMarshalerTypeName, cMarshalerTypeNameBytes, strCookie, cCookieStrBytes);
#else
    _ASSERTE(false);
    RETURN NULL;
#endif
}

namespace
{
    //==========================================================================
    // Return: S_OK if there is valid data to compress
    //         S_FALSE if at end of data block
    //         E_FAIL if corrupt data found
    //==========================================================================
    HRESULT CheckForCompressedData(PCCOR_SIGNATURE pvNativeTypeStart, PCCOR_SIGNATURE pvNativeType, ULONG cbNativeType)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        if (pvNativeTypeStart + cbNativeType == pvNativeType)
        {   // end of data block
            return S_FALSE;
        }

        ULONG ulDummy;
        BYTE const *pbDummy;
        return CPackedLen::SafeGetLength((BYTE const *)pvNativeType,
                                        (BYTE const *)pvNativeTypeStart + cbNativeType,
                                        &ulDummy,
                                        &pbDummy);
    }
}

//==========================================================================
// Parse and validate the NATIVE_TYPE_ metadata.
// Note! NATIVE_TYPE_ metadata is optional. If it's not present, this
// routine sets NativeTypeParamInfo->m_NativeType to NATIVE_TYPE_DEFAULT.
//==========================================================================
BOOL ParseNativeTypeInfo(NativeTypeParamInfo* pParamInfo, PCCOR_SIGNATURE pvNativeType, ULONG cbNativeType);

BOOL ParseNativeTypeInfo(mdToken                    token,
                         IMDInternalImport*         pScope,
                         NativeTypeParamInfo*       pParamInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    PCCOR_SIGNATURE pvNativeType;
    ULONG           cbNativeType;

    if (token == mdParamDefNil || token == mdFieldDefNil || pScope->GetFieldMarshal(token, &pvNativeType, &cbNativeType) != S_OK)
        return TRUE;

    return ParseNativeTypeInfo(pParamInfo, pvNativeType, cbNativeType);
}

BOOL ParseNativeTypeInfo(NativeTypeParamInfo* pParamInfo,
                         PCCOR_SIGNATURE pvNativeType,
                         ULONG cbNativeType)
{
    LIMITED_METHOD_CONTRACT;
    HRESULT hr;

    PCCOR_SIGNATURE pvNativeTypeStart = pvNativeType;
    PCCOR_SIGNATURE pvNativeTypeEnd = pvNativeType + cbNativeType;

    if (cbNativeType == 0)
        return FALSE;  // Zero-length NATIVE_TYPE block

    pParamInfo->m_NativeType = (CorNativeType)*(pvNativeType++);
    ULONG strLen = 0;

    // Retrieve any extra information associated with the native type.
    switch (pParamInfo->m_NativeType)
    {
#ifdef FEATURE_COMINTEROP
        case NATIVE_TYPE_INTF:
        case NATIVE_TYPE_IUNKNOWN:
        case NATIVE_TYPE_IDISPATCH:
            if (S_OK != CheckForCompressedData(pvNativeTypeStart, pvNativeType, cbNativeType))
                return TRUE;

            pParamInfo->m_IidParamIndex = (int)CorSigUncompressData(pvNativeType);
            break;
#endif

        case NATIVE_TYPE_FIXEDARRAY:

            if (S_OK != CheckForCompressedData(pvNativeTypeStart, pvNativeType, cbNativeType))
                return FALSE;

            pParamInfo->m_Additive = CorSigUncompressData(pvNativeType);

            if (S_OK != CheckForCompressedData(pvNativeTypeStart, pvNativeType, cbNativeType))
                return TRUE;

            pParamInfo->m_ArrayElementType = (CorNativeType)CorSigUncompressData(pvNativeType);
            break;

        case NATIVE_TYPE_FIXEDSYSSTRING:
            if (S_OK != CheckForCompressedData(pvNativeTypeStart, pvNativeType, cbNativeType))
                return FALSE;

            pParamInfo->m_Additive = CorSigUncompressData(pvNativeType);
            break;

#ifdef FEATURE_COMINTEROP
        case NATIVE_TYPE_SAFEARRAY:
            // Check for the safe array element type.
            hr = CheckForCompressedData(pvNativeTypeStart, pvNativeType, cbNativeType);
            if (FAILED(hr))
                return FALSE;

            if (hr == S_OK)
                pParamInfo->m_SafeArrayElementVT = (VARTYPE) (CorSigUncompressData(/*modifies*/pvNativeType));

            // Extract the name of the record type's.
            if (S_OK == CheckForCompressedData(pvNativeTypeStart, pvNativeType, cbNativeType))
            {
                hr = CPackedLen::SafeGetData((BYTE const *)pvNativeType,
                                             (BYTE const *)pvNativeTypeEnd,
                                             &strLen,
                                             (BYTE const **)&pvNativeType);
                if (FAILED(hr))
                {
                    return FALSE;
                }

                pParamInfo->m_strSafeArrayUserDefTypeName = (LPUTF8)pvNativeType;
                pParamInfo->m_cSafeArrayUserDefTypeNameBytes = strLen;
                _ASSERTE((ULONG)(pvNativeType + strLen - pvNativeTypeStart) == cbNativeType);
            }
            break;

#endif // FEATURE_COMINTEROP

        case NATIVE_TYPE_ARRAY:
            hr = CheckForCompressedData(pvNativeTypeStart, pvNativeType, cbNativeType);
            if (FAILED(hr))
                return FALSE;

            if (hr == S_OK)
                pParamInfo->m_ArrayElementType = (CorNativeType) (CorSigUncompressData(/*modifies*/pvNativeType));

            // Check for "sizeis" param index
            hr = CheckForCompressedData(pvNativeTypeStart, pvNativeType, cbNativeType);
            if (FAILED(hr))
                return FALSE;

            if (hr == S_OK)
            {
                pParamInfo->m_SizeIsSpecified = TRUE;
                pParamInfo->m_CountParamIdx = (UINT16)(CorSigUncompressData(/*modifies*/pvNativeType));

                // If an "sizeis" param index is present, the defaults for multiplier and additive change
                pParamInfo->m_Multiplier = 1;
                pParamInfo->m_Additive   = 0;

                // Check for "sizeis" additive
                hr = CheckForCompressedData(pvNativeTypeStart, pvNativeType, cbNativeType);
                if (FAILED(hr))
                    return FALSE;

                if (hr == S_OK)
                {
                    // Extract the additive.
                    pParamInfo->m_Additive = (DWORD)CorSigUncompressData(/*modifies*/pvNativeType);

                    // Check to see if the flags field is present.
                    hr = CheckForCompressedData(pvNativeTypeStart, pvNativeType, cbNativeType);
                    if (FAILED(hr))
                        return FALSE;

                    if (hr == S_OK)
                    {
                        // If the param index specified flag isn't set then we need to reset the
                        // multiplier to 0 to indicate no size param index was specified.
                        NativeTypeArrayFlags flags = (NativeTypeArrayFlags)CorSigUncompressData(/*modifies*/pvNativeType);;
                        if (!(flags & ntaSizeParamIndexSpecified))
                            pParamInfo->m_Multiplier = 0;
                    }
                }
            }

            break;

        case NATIVE_TYPE_CUSTOMMARSHALER:
            // Skip the typelib guid.
            if (S_OK != CheckForCompressedData(pvNativeTypeStart, pvNativeType, cbNativeType))
                return FALSE;

            if (FAILED(CPackedLen::SafeGetData(pvNativeType, pvNativeTypeEnd, &strLen, (void const **)&pvNativeType)))
                return FALSE;

            pvNativeType += strLen;
            _ASSERTE((ULONG)(pvNativeType - pvNativeTypeStart) < cbNativeType);

            // Skip the name of the native type.
            if (S_OK != CheckForCompressedData(pvNativeTypeStart, pvNativeType, cbNativeType))
                return FALSE;

            if (FAILED(CPackedLen::SafeGetData(pvNativeType, pvNativeTypeEnd, &strLen, (void const **)&pvNativeType)))
                return FALSE;

            pvNativeType += strLen;
            _ASSERTE((ULONG)(pvNativeType - pvNativeTypeStart) < cbNativeType);

            // Extract the name of the custom marshaler.
            if (S_OK != CheckForCompressedData(pvNativeTypeStart, pvNativeType, cbNativeType))
                return FALSE;

            if (FAILED(CPackedLen::SafeGetData(pvNativeType, pvNativeTypeEnd, &strLen, (void const **)&pvNativeType)))
                return FALSE;

            pParamInfo->m_strCMMarshalerTypeName = (LPUTF8)pvNativeType;
            pParamInfo->m_cCMMarshalerTypeNameBytes = strLen;
            pvNativeType += strLen;
            _ASSERTE((ULONG)(pvNativeType - pvNativeTypeStart) < cbNativeType);

            // Extract the cookie string.
            if (S_OK != CheckForCompressedData(pvNativeTypeStart, pvNativeType, cbNativeType))
                return FALSE;

            if (FAILED(CPackedLen::SafeGetData(pvNativeType, pvNativeTypeEnd, &strLen, (void const **)&pvNativeType)))
                return FALSE;

            pParamInfo->m_strCMCookie = (LPUTF8)pvNativeType;
            pParamInfo->m_cCMCookieStrBytes = strLen;
            _ASSERTE((ULONG)(pvNativeType + strLen - pvNativeTypeStart) == cbNativeType);
            break;

        default:
            break;
    }

    return TRUE;
}

VOID ThrowInteropParamException(UINT resID, UINT paramIdx)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    SString paramString;
    if (paramIdx == 0)
        paramString.Set(W("return value"));
    else
        paramString.Printf(W("parameter #%u"), paramIdx);

    SString errorString(W("Unknown error."));
    errorString.LoadResource(CCompRC::Error, resID);

    COMPlusThrow(kMarshalDirectiveException, IDS_EE_BADMARSHAL_ERROR_MSG, paramString.GetUnicode(), errorString.GetUnicode());
}

#ifdef _DEBUG
BOOL IsFixedBuffer(mdFieldDef field, IMDInternalImport* pInternalImport)
{
    HRESULT hr = pInternalImport->GetCustomAttributeByName(field, g_FixedBufferAttribute, NULL, NULL);

    return hr == S_OK ? TRUE : FALSE;
}
#endif


//===============================================================
// Collects paraminfo's in an indexed array so that:
//
//   aParams[0] == param token for return value
//   aParams[1] == param token for argument #1...
//   aParams[numargs] == param token for argument #n...
//
// If no param token exists, the corresponding array element
// is set to mdParamDefNil.
//
// Inputs:
//    pInternalImport  -- ifc for metadata api
//    md       -- token of method. If token is mdMethodNil,
//                all aParam elements will be set to mdParamDefNil.
//    numargs  -- # of arguments in mdMethod
//    aParams  -- uninitialized array with numargs+1 elements.
//                on exit, will be filled with param tokens.
//===============================================================
VOID CollateParamTokens(IMDInternalImport *pInternalImport, mdMethodDef md, ULONG numargs, mdParamDef *aParams)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    for (ULONG i = 0; i < numargs + 1; i++)
        aParams[i] = mdParamDefNil;

    if (md != mdMethodDefNil)
    {
        MDEnumHolder hEnumParams(pInternalImport);
        HRESULT hr = pInternalImport->EnumInit(mdtParamDef, md, &hEnumParams);
        if (FAILED(hr))
        {
            // no param info: nothing left to do here
        }
        else
        {
            mdParamDef CurrParam = mdParamDefNil;
            while (pInternalImport->EnumNext(&hEnumParams, &CurrParam))
            {
                USHORT usSequence;
                DWORD dwAttr;
                LPCSTR szParamName_Ignore;
                if (SUCCEEDED(pInternalImport->GetParamDefProps(CurrParam, &usSequence, &dwAttr, &szParamName_Ignore)))
                {
                    if (usSequence > numargs)
                    {   // Invalid argument index
                        ThrowHR(COR_E_BADIMAGEFORMAT);
                    }
                    if (aParams[usSequence] != mdParamDefNil)
                    {   // Duplicit argument index
                        ThrowHR(COR_E_BADIMAGEFORMAT);
                    }
                    aParams[usSequence] = CurrParam;
                }
            }
        }
    }
}


#ifdef FEATURE_COMINTEROP
OleColorMarshalingInfo::OleColorMarshalingInfo() :
    m_OleColorToSystemColorMD(NULL),
    m_SystemColorToOleColorMD(NULL)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    SString qualifiedColorTranslatorTypeName(SString::Utf8, COLOR_TRANSLATOR_ASM_QUAL_TYPE_NAME);

    // Load the color translator class.
    TypeHandle hndColorTranslatorType = TypeName::GetTypeFromAsmQualifiedName(qualifiedColorTranslatorTypeName.GetUnicode());


    SString qualifiedColorTypeName(SString::Utf8, COLOR_ASM_QUAL_TYPE_NAME);
    // Load the color class.
    m_hndColorType = TypeName::GetTypeFromAsmQualifiedName(qualifiedColorTypeName.GetUnicode());

    // Retrieve the method to convert an OLE_COLOR to a System.Drawing.Color.
    m_OleColorToSystemColorMD = MemberLoader::FindMethodByName(hndColorTranslatorType.GetMethodTable(), OLECOLOR_TO_SYSTEMCOLOR_METH_NAME);
    _ASSERTE(m_OleColorToSystemColorMD && "Unable to find the translator method to convert an OLE_COLOR to a System.Drawing.Color!");
    _ASSERTE(m_OleColorToSystemColorMD->IsStatic() && "The translator method to convert an OLE_COLOR to a System.Drawing.Color must be static!");

    // Retrieve the method to convert a System.Drawing.Color to an OLE_COLOR.
    m_SystemColorToOleColorMD = MemberLoader::FindMethodByName(hndColorTranslatorType.GetMethodTable(), SYSTEMCOLOR_TO_OLECOLOR_METH_NAME);
    _ASSERTE(m_SystemColorToOleColorMD && "Unable to find the translator method to convert a System.Drawing.Color to an OLE_COLOR!");
    _ASSERTE(m_SystemColorToOleColorMD->IsStatic() && "The translator method to convert a System.Drawing.Color to an OLE_COLOR must be static!");
}


void *OleColorMarshalingInfo::operator new(size_t size, LoaderHeap *pHeap)
{
    CONTRACT (void*)
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pHeap));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    void* mem = pHeap->AllocMem(S_SIZE_T(size));

    RETURN mem;
}


void OleColorMarshalingInfo::operator delete(void *pMem)
{
    LIMITED_METHOD_CONTRACT;
    // Instances of this class are always allocated on the loader heap so
    // the delete operator has nothing to do.
}

#endif // FEATURE_COMINTEROP

EEMarshalingData::EEMarshalingData(LoaderAllocator* pAllocator, CrstBase *pCrst) :
    m_pAllocator(pAllocator),
    m_pHeap(pAllocator->GetLowFrequencyHeap()),
    m_lock(pCrst)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LockOwner lock = {pCrst, IsOwnerOfCrst};
#ifndef CROSSGEN_COMPILE
    m_CMHelperHashtable.Init(INITIAL_NUM_CMHELPER_HASHTABLE_BUCKETS, &lock);
    m_SharedCMHelperToCMInfoMap.Init(INITIAL_NUM_CMINFO_HASHTABLE_BUCKETS, &lock);
#endif // CROSSGEN_COMPILE
}


EEMarshalingData::~EEMarshalingData()
{
    WRAPPER_NO_CONTRACT;

    CustomMarshalerInfo *pCMInfo;

    // <TODO>@TODO(DM): Remove the linked list of CMInfo's and instead hang the OBJECTHANDLE
    // contained inside the CMInfo off the AppDomain directly. The AppDomain can have
    // a list of tasks to do when it gets teared down and we could leverage that
    // to release the object handles.</TODO>

    // Walk through the linked list and delete all the custom marshaler info's.
    while ((pCMInfo = m_pCMInfoList.RemoveHead()) != NULL)
        delete pCMInfo;

#ifdef FEATURE_COMINTEROP
    if (m_pOleColorInfo)
    {
        delete m_pOleColorInfo;
        m_pOleColorInfo = NULL;
    }
#endif
}


void *EEMarshalingData::operator new(size_t size, LoaderHeap *pHeap)
{
    CONTRACT (void*)
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pHeap));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    void* mem = pHeap->AllocMem(S_SIZE_T(sizeof(EEMarshalingData)));

    RETURN mem;
}


void EEMarshalingData::operator delete(void *pMem)
{
    LIMITED_METHOD_CONTRACT;
    // Instances of this class are always allocated on the loader heap so
    // the delete operator has nothing to do.
}

#ifndef CROSSGEN_COMPILE

CustomMarshalerHelper *EEMarshalingData::GetCustomMarshalerHelper(Assembly *pAssembly, TypeHandle hndManagedType, LPCUTF8 strMarshalerTypeName, DWORD cMarshalerTypeNameBytes, LPCUTF8 strCookie, DWORD cCookieStrBytes)
{
    CONTRACT (CustomMarshalerHelper*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pAssembly));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    CustomMarshalerHelper *pCMHelper = NULL;
    CustomMarshalerHelper* pNewCMHelper = NULL;
    NewHolder<CustomMarshalerInfo> pNewCMInfo(NULL);

    TypeHandle hndCustomMarshalerType;

    // Create the key that will be used to lookup in the hashtable.
    EECMHelperHashtableKey Key(cMarshalerTypeNameBytes, strMarshalerTypeName, cCookieStrBytes, strCookie, hndManagedType.GetInstantiation(), pAssembly);

    // Lookup the custom marshaler helper in the hashtable.
    if (m_CMHelperHashtable.GetValue(&Key, (HashDatum*)&pCMHelper))
        RETURN pCMHelper;

    {
        GCX_COOP();

        // Validate the arguments.
        _ASSERTE(strMarshalerTypeName && strCookie && !hndManagedType.IsNull());

        // Append a NULL terminator to the marshaler type name.
        SString strCMMarshalerTypeName(SString::Utf8, strMarshalerTypeName, cMarshalerTypeNameBytes);

        // Load the custom marshaler class.
        BOOL fNameIsAsmQualified = FALSE;
        hndCustomMarshalerType = TypeName::GetTypeUsingCASearchRules(strCMMarshalerTypeName.GetUTF8NoConvert(), pAssembly, &fNameIsAsmQualified);

        if (hndCustomMarshalerType.IsGenericTypeDefinition())
        {
            // Instantiate generic custom marshalers using the instantiation of the type being marshaled.
            hndCustomMarshalerType = hndCustomMarshalerType.Instantiate(hndManagedType.GetInstantiation());
        }

        // Set the assembly to null to indicate that the custom marshaler name is assembly
        // qualified.
        if (fNameIsAsmQualified)
            pAssembly = NULL;


        // Create the custom marshaler info in the specified heap.
        pNewCMInfo = new (m_pHeap) CustomMarshalerInfo(m_pAllocator, hndCustomMarshalerType, hndManagedType, strCookie, cCookieStrBytes);

        // Create the custom marshaler helper in the specified heap.
        pNewCMHelper = new (m_pHeap) NonSharedCustomMarshalerHelper(pNewCMInfo);
    }

    {
        CrstHolder lock(m_lock);

        // Verify that the custom marshaler helper has not already been added by another thread.
        if (m_CMHelperHashtable.GetValue(&Key, (HashDatum*)&pCMHelper))
        {
            RETURN pCMHelper;
        }

        // Add the custom marshaler helper to the hash table.
        m_CMHelperHashtable.InsertValue(&Key, pNewCMHelper, FALSE);

        // If we create the CM info, then add it to the linked list.
        if (pNewCMInfo)
        {
            m_pCMInfoList.InsertHead(pNewCMInfo);
            pNewCMInfo.SuppressRelease();
        }

        // Release the lock and return the custom marshaler info.
    }

    RETURN pNewCMHelper;
}

CustomMarshalerInfo *EEMarshalingData::GetCustomMarshalerInfo(SharedCustomMarshalerHelper *pSharedCMHelper)
{
    CONTRACT (CustomMarshalerInfo*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    CustomMarshalerInfo *pCMInfo = NULL;
    NewHolder<CustomMarshalerInfo> pNewCMInfo(NULL);
    TypeHandle hndCustomMarshalerType;

    // Lookup the custom marshaler helper in the hashtable.
    if (m_SharedCMHelperToCMInfoMap.GetValue(pSharedCMHelper, (HashDatum*)&pCMInfo))
        RETURN pCMInfo;

    // Append a NULL terminator to the marshaler type name.
    CQuickArray<char> strCMMarshalerTypeName;
    DWORD strLen = pSharedCMHelper->GetMarshalerTypeNameByteCount();
    strCMMarshalerTypeName.ReSizeThrows(pSharedCMHelper->GetMarshalerTypeNameByteCount() + 1);
    memcpy(strCMMarshalerTypeName.Ptr(), pSharedCMHelper->GetMarshalerTypeName(), strLen);
    strCMMarshalerTypeName[strLen] = 0;

    // Load the custom marshaler class.
    hndCustomMarshalerType = TypeName::GetTypeUsingCASearchRules(strCMMarshalerTypeName.Ptr(), pSharedCMHelper->GetAssembly());
    if (hndCustomMarshalerType.IsGenericTypeDefinition())
    {
        // Instantiate generic custom marshalers using the instantiation of the type being marshaled.
        hndCustomMarshalerType = hndCustomMarshalerType.Instantiate(pSharedCMHelper->GetManagedType().GetInstantiation());
    }

    // Create the custom marshaler info in the specified heap.
    pNewCMInfo = new (m_pHeap) CustomMarshalerInfo(m_pAllocator,
                                                   hndCustomMarshalerType,
                                                   pSharedCMHelper->GetManagedType(),
                                                   pSharedCMHelper->GetCookieString(),
                                                   pSharedCMHelper->GetCookieStringByteCount());

    {
        CrstHolder lock(m_lock);

        // Verify that the custom marshaler info has not already been added by another thread.
        if (m_SharedCMHelperToCMInfoMap.GetValue(pSharedCMHelper, (HashDatum*)&pCMInfo))
        {
            RETURN pCMInfo;
        }

        // Add the custom marshaler helper to the hash table.
        m_SharedCMHelperToCMInfoMap.InsertValue(pSharedCMHelper, pNewCMInfo, FALSE);

        // Add the custom marshaler into the linked list.
        m_pCMInfoList.InsertHead(pNewCMInfo);

        // Release the lock and return the custom marshaler info.
    }

    pNewCMInfo.SuppressRelease();
    RETURN pNewCMInfo;
}
#endif // CROSSGEN_COMPILE

#ifdef FEATURE_COMINTEROP
OleColorMarshalingInfo *EEMarshalingData::GetOleColorMarshalingInfo()
{
    CONTRACT (OleColorMarshalingInfo*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    if (m_pOleColorInfo == NULL)
    {
        OleColorMarshalingInfo *pOleColorInfo = new (m_pHeap) OleColorMarshalingInfo();

    if (InterlockedCompareExchangeT(&m_pOleColorInfo, pOleColorInfo, NULL) != NULL)
        {
            // Another thread beat us to it. Delete on OleColorMarshalingInfo is an empty operation
            // which is OK, since the possible leak is rare, small, and constant. This is the same
            // pattern as in code:GetCustomMarshalerInfo.
            delete pOleColorInfo;
        }
    }

    RETURN m_pOleColorInfo;
}
#endif // FEATURE_COMINTEROP

//==========================================================================
// Constructs MarshalInfo.
//==========================================================================
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
MarshalInfo::MarshalInfo(Module* pModule,
                         SigPointer sig,
                         const SigTypeContext *pTypeContext,
                         mdToken token,
                         MarshalScenario ms,
                         CorNativeLinkType nlType,
                         CorNativeLinkFlags nlFlags,
                         BOOL isParam,
                         UINT paramidx,   // parameter # for use in error messages (ignored if not parameter)
                         UINT numArgs,    // number of arguments
                         BOOL BestFit,
                         BOOL ThrowOnUnmappableChar,
                         BOOL fEmitsIL,
                         MethodDesc* pMD,
                         BOOL fLoadCustomMarshal
#ifdef _DEBUG
                         ,
                         LPCUTF8 pDebugName,
                         LPCUTF8 pDebugClassName,
                         UINT    argidx  // 0 for return value, -1 for field
#endif
)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr;
    NativeTypeParamInfo ParamInfo;

    // we expect a 1-based paramidx, but we like to use a 0-based paramidx
    m_paramidx                      = paramidx - 1;

    // if no one overwrites this with a better message, we'll still at least say something
    m_resID                         = IDS_EE_BADMARSHAL_GENERIC;

    // flag for uninitialized type
    m_type                          = MARSHAL_TYPE_UNKNOWN;

    CorNativeType nativeType        = NATIVE_TYPE_DEFAULT;
    Assembly *pAssembly             = pModule->GetAssembly();
    BOOL fNeedsCopyCtor             = FALSE;
    m_BestFit                       = BestFit;
    m_ThrowOnUnmappableChar         = ThrowOnUnmappableChar;
    m_ms                            = ms;
    m_fAnsi                         = (ms == MARSHAL_SCENARIO_NDIRECT || ms == MARSHAL_SCENARIO_FIELD) && (nlType == nltAnsi);
    m_nativeArgSize                 = 0;
    m_pCMHelper                     = NULL;
    m_CMVt                          = VT_EMPTY;
    m_args.m_pMarshalInfo           = this;
    m_args.m_pMT                    = NULL;
    m_pModule                       = pModule;
    m_token                         = token;
    CorElementType mtype            = ELEMENT_TYPE_END;
    CorElementType corElemType      = ELEMENT_TYPE_END;
    m_pMT                           = NULL;
    m_pMD                           = pMD;

#ifdef FEATURE_COMINTEROP
    m_fDispItf                      = FALSE;
    m_fErrorNativeType              = FALSE;
#endif // FEATURE_COMINTEROP


#ifdef _DEBUG

    CHAR achDbgContext[DEBUG_CONTEXT_STR_LEN] = "";
    if (!pDebugName)
    {
        strncpy_s(achDbgContext, COUNTOF(achDbgContext), "<Unknown>", _TRUNCATE);
    }
    else
    {
        strncat_s(achDbgContext, COUNTOF(achDbgContext), pDebugClassName, _TRUNCATE);
        strncat_s(achDbgContext, COUNTOF(achDbgContext), NAMESPACE_SEPARATOR_STR, _TRUNCATE);
        strncat_s(achDbgContext, COUNTOF(achDbgContext), pDebugName, _TRUNCATE);
        strncat_s(achDbgContext, COUNTOF(achDbgContext), " ", _TRUNCATE);
        switch (argidx)
        {
            case (UINT)-1:
                strncat_s(achDbgContext, COUNTOF(achDbgContext), "field", _TRUNCATE);
                break;
            case 0:
                strncat_s(achDbgContext, COUNTOF(achDbgContext), "return value", _TRUNCATE);
                break;
            default:
            {
                char buf[30];
                sprintf_s(buf, COUNTOF(buf), "param #%lu", (ULONG)argidx);
                strncat_s(achDbgContext, COUNTOF(achDbgContext), buf, _TRUNCATE);
            }
        }
    }

    m_strDebugMethName = pDebugName;
    m_strDebugClassName = pDebugClassName;
    m_iArg = argidx;

    m_in = m_out = FALSE;
    m_byref = TRUE;
#endif



    // Retrieve the native type for the current parameter.
    if (!ParseNativeTypeInfo(token, pModule->GetMDImport(), &ParamInfo))
    {
        IfFailGoto(E_FAIL, lFail);
    }

    nativeType = ParamInfo.m_NativeType;

    corElemType = sig.PeekElemTypeNormalized(pModule, pTypeContext);
    mtype = corElemType;

    // Make sure SizeParamIndex < numArgs when marshalling native arrays
    if (nativeType == NATIVE_TYPE_ARRAY && ParamInfo.m_SizeIsSpecified)
    {
        if (ParamInfo.m_Multiplier > 0 && ParamInfo.m_CountParamIdx >= numArgs)
        {
            // Do not throw exception here.
            // We'll use EmitOrThrowInteropException to throw exception in non-COM interop
            // and emit exception throwing code directly in STUB in COM interop
            m_type = MARSHAL_TYPE_UNKNOWN;
            m_resID = IDS_EE_SIZECONTROLOUTOFRANGE;
            IfFailGoto(E_FAIL, lFail);
        }
    }

    // Parse ET_BYREF signature
    if (mtype == ELEMENT_TYPE_BYREF)
    {
        m_byref = TRUE;
        SigPointer sigtmp = sig;
        IfFailGoto(sig.GetElemType(NULL), lFail);
        mtype = sig.PeekElemTypeNormalized(pModule, pTypeContext);

        // Check for Copy Constructor Modifier - peek closed elem type here to prevent ELEMENT_TYPE_VALUETYPE
        // turning into a primitive.
        if (sig.PeekElemTypeClosed(pModule, pTypeContext) == ELEMENT_TYPE_VALUETYPE)
        {
            // Skip ET_BYREF
            IfFailGoto(sigtmp.GetByte(NULL), lFail);

            if (sigtmp.HasCustomModifier(pModule, "Microsoft.VisualC.NeedsCopyConstructorModifier", ELEMENT_TYPE_CMOD_REQD) ||
                sigtmp.HasCustomModifier(pModule, "System.Runtime.CompilerServices.IsCopyConstructed", ELEMENT_TYPE_CMOD_REQD) )
            {
                mtype = ELEMENT_TYPE_VALUETYPE;
                fNeedsCopyCtor = TRUE;
                m_byref = FALSE;
            }
        }
    }
    else
    {
        m_byref = FALSE;
    }

    // Check for valid ET_PTR signature
    if (mtype == ELEMENT_TYPE_PTR)
    {
        SigPointer sigtmp = sig;
        IfFailGoto(sigtmp.GetElemType(NULL), lFail);

        // Peek closed elem type here to prevent ELEMENT_TYPE_VALUETYPE turning into a primitive.
        CorElementType mtype2 = sigtmp.PeekElemTypeClosed(pModule, pTypeContext);

        if (mtype2 == ELEMENT_TYPE_VALUETYPE)
        {
            TypeHandle th = sigtmp.GetTypeHandleThrowing(pModule, pTypeContext);
            _ASSERTE(!th.IsNull());

            // We want to leave out enums as they surely don't have copy constructors
            // plus they are not marked as blittable.
            if (!th.IsEnum())
            {
                // Check for Copy Constructor Modifier
                if (sigtmp.HasCustomModifier(pModule, "Microsoft.VisualC.NeedsCopyConstructorModifier", ELEMENT_TYPE_CMOD_REQD) ||
                    sigtmp.HasCustomModifier(pModule, "System.Runtime.CompilerServices.IsCopyConstructed", ELEMENT_TYPE_CMOD_REQD) )
                {
                    mtype = mtype2;

                    // Keep the sig pointer in sync with mtype (skip ELEMENT_TYPE_PTR) because for the rest
                    // of this method we are pretending that the parameter is a value type passed by-value.
                    IfFailGoto(sig.GetElemType(NULL), lFail);

                    fNeedsCopyCtor = TRUE;
                    m_byref = FALSE;
                }
            }
        }
    }

    if (nativeType == NATIVE_TYPE_CUSTOMMARSHALER)
    {
        if (IsFieldScenario())
        {
            m_resID = IDS_EE_BADMARSHALFIELD_NOCUSTOMMARSH;
            IfFailGoto(E_FAIL, lFail);
        }

        switch (mtype)
        {
            case ELEMENT_TYPE_VAR:
            case ELEMENT_TYPE_CLASS:
            case ELEMENT_TYPE_OBJECT:
                m_CMVt = VT_UNKNOWN;
                break;

            case ELEMENT_TYPE_STRING:
            case ELEMENT_TYPE_SZARRAY:
            case ELEMENT_TYPE_ARRAY:
                m_CMVt = VT_I4;
                break;

            default:
                m_resID = IDS_EE_BADMARSHAL_CUSTOMMARSHALER;
                IfFailGoto(E_FAIL, lFail);
        }

        // Set m_type to MARSHAL_TYPE_UNKNOWN in case SetupCustomMarshalerHelper throws.
        m_type = MARSHAL_TYPE_UNKNOWN;

        if (fLoadCustomMarshal)
        {
            // Set up the custom marshaler info.
            TypeHandle hndManagedType = sig.GetTypeHandleThrowing(pModule, pTypeContext);

            if (!fEmitsIL)
            {
                m_pCMHelper = SetupCustomMarshalerHelper(ParamInfo.m_strCMMarshalerTypeName,
                                                        ParamInfo.m_cCMMarshalerTypeNameBytes,
                                                        ParamInfo.m_strCMCookie,
                                                        ParamInfo.m_cCMCookieStrBytes,
                                                        pAssembly,
                                                        hndManagedType);
            }
            else
            {
                m_pCMHelper = NULL;
                MethodDesc* pMDforModule = pMD;
                if (pMD->IsILStub())
                {
                    pMDforModule = pMD->AsDynamicMethodDesc()->GetILStubResolver()->GetStubTargetMethodDesc();
                }
                m_args.rcm.m_pMD = pMDforModule;
                m_args.rcm.m_paramToken = token;
                m_args.rcm.m_hndManagedType = hndManagedType.AsPtr();
                CONSISTENCY_CHECK(pModule == pMDforModule->GetModule());
            }
        }

        // Specify which custom marshaler to use.
        m_type = MARSHAL_TYPE_REFERENCECUSTOMMARSHALER;

        goto lExit;
    }

    switch (mtype)
    {
        case ELEMENT_TYPE_BOOLEAN:
            switch (nativeType)
            {
                case NATIVE_TYPE_BOOLEAN:
                    m_type = MARSHAL_TYPE_WINBOOL;
                    break;

#ifdef FEATURE_COMINTEROP
                case NATIVE_TYPE_VARIANTBOOL:
                    m_type = MARSHAL_TYPE_VTBOOL;
                    break;
#endif // FEATURE_COMINTEROP

                case NATIVE_TYPE_U1:
                case NATIVE_TYPE_I1:
                    m_type = MARSHAL_TYPE_CBOOL;
                    break;

                case NATIVE_TYPE_DEFAULT:
#ifdef FEATURE_COMINTEROP
                    if (m_ms == MARSHAL_SCENARIO_COMINTEROP)
                    {
                        // 2-byte COM VARIANT_BOOL
                        m_type = MARSHAL_TYPE_VTBOOL;
                    }
                    else
#endif // FEATURE_COMINTEROP
                    {
                        // 4-byte Windows BOOL
                        _ASSERTE(m_ms == MARSHAL_SCENARIO_NDIRECT || m_ms == MARSHAL_SCENARIO_FIELD);
                        m_type = MARSHAL_TYPE_WINBOOL;
                    }
                    break;

                default:
                    m_resID = IDS_EE_BADMARSHAL_BOOLEAN;
                    IfFailGoto(E_FAIL, lFail);
            }
            break;

        case ELEMENT_TYPE_CHAR:
            switch (nativeType)
            {
                case NATIVE_TYPE_I1: //fallthru
                case NATIVE_TYPE_U1:
                    m_type = MARSHAL_TYPE_ANSICHAR;
                    break;

                case NATIVE_TYPE_I2: //fallthru
                case NATIVE_TYPE_U2:
                    m_type = MARSHAL_TYPE_GENERIC_U2;
                    break;

                case NATIVE_TYPE_DEFAULT:
                    m_type = ( ((m_ms == MARSHAL_SCENARIO_NDIRECT || m_ms == MARSHAL_SCENARIO_FIELD) && m_fAnsi) ? MARSHAL_TYPE_ANSICHAR : MARSHAL_TYPE_GENERIC_U2 );
                    break;

                default:
                    m_resID = IDS_EE_BADMARSHAL_CHAR;
                    IfFailGoto(E_FAIL, lFail);

            }
            break;

        case ELEMENT_TYPE_I1:
            if (!(nativeType == NATIVE_TYPE_I1 || nativeType == NATIVE_TYPE_U1 || nativeType == NATIVE_TYPE_DEFAULT))
            {
                m_resID = IDS_EE_BADMARSHAL_I1;
                IfFailGoto(E_FAIL, lFail);
            }
            m_type = MARSHAL_TYPE_GENERIC_1;
            break;

        case ELEMENT_TYPE_U1:
            if (!(nativeType == NATIVE_TYPE_U1 || nativeType == NATIVE_TYPE_I1 || nativeType == NATIVE_TYPE_DEFAULT))
            {
                m_resID = IDS_EE_BADMARSHAL_I1;
                IfFailGoto(E_FAIL, lFail);
            }
            m_type = MARSHAL_TYPE_GENERIC_U1;
            break;

        case ELEMENT_TYPE_I2:
            if (!(nativeType == NATIVE_TYPE_I2 || nativeType == NATIVE_TYPE_U2 || nativeType == NATIVE_TYPE_DEFAULT))
            {
                m_resID = IDS_EE_BADMARSHAL_I2;
                IfFailGoto(E_FAIL, lFail);
            }
            m_type = MARSHAL_TYPE_GENERIC_2;
            break;

        case ELEMENT_TYPE_U2:
            if (!(nativeType == NATIVE_TYPE_U2 || nativeType == NATIVE_TYPE_I2 || nativeType == NATIVE_TYPE_DEFAULT))
            {
                m_resID = IDS_EE_BADMARSHAL_I2;
                IfFailGoto(E_FAIL, lFail);
            }
            m_type = MARSHAL_TYPE_GENERIC_U2;
            break;

        case ELEMENT_TYPE_I4:
            switch (nativeType)
            {
                case NATIVE_TYPE_I4:
                case NATIVE_TYPE_U4:
                case NATIVE_TYPE_DEFAULT:
                    break;

                case NATIVE_TYPE_ERROR:
#ifdef FEATURE_COMINTEROP
                    m_fErrorNativeType = TRUE;
#endif // FEATURE_COMINTEROP
                    break;

                default:
                m_resID = IDS_EE_BADMARSHAL_I4;
                IfFailGoto(E_FAIL, lFail);
            }
            m_type = MARSHAL_TYPE_GENERIC_4;
            break;

        case ELEMENT_TYPE_U4:
            switch (nativeType)
            {
                case NATIVE_TYPE_I4:
                case NATIVE_TYPE_U4:
                case NATIVE_TYPE_DEFAULT:
                    break;

                case NATIVE_TYPE_ERROR:
#ifdef FEATURE_COMINTEROP
                    m_fErrorNativeType = TRUE;
#endif // FEATURE_COMINTEROP
                    break;

                default:
                m_resID = IDS_EE_BADMARSHAL_I4;
                IfFailGoto(E_FAIL, lFail);
            }
            m_type = MARSHAL_TYPE_GENERIC_U4;
            break;

        case ELEMENT_TYPE_I8:
            if (!(nativeType == NATIVE_TYPE_I8 || nativeType == NATIVE_TYPE_U8 || nativeType == NATIVE_TYPE_DEFAULT))
            {
                m_resID = IDS_EE_BADMARSHAL_I8;
                IfFailGoto(E_FAIL, lFail);
            }
            m_type = MARSHAL_TYPE_GENERIC_8;
            break;

        case ELEMENT_TYPE_U8:
            if (!(nativeType == NATIVE_TYPE_U8 || nativeType == NATIVE_TYPE_I8 || nativeType == NATIVE_TYPE_DEFAULT))
            {
                m_resID = IDS_EE_BADMARSHAL_I8;
                IfFailGoto(E_FAIL, lFail);
            }
            m_type = MARSHAL_TYPE_GENERIC_8;
            break;

        case ELEMENT_TYPE_I:
            // Technically the "native int" and "native uint" types aren't supported in the WinRT scenario,
            // but we need to not block ourselves from using them to enable accurate managed->native marshalling of
            // projected types such as NotifyCollectionChangedEventArgs and NotifyPropertyChangedEventArgs.

            if (!(nativeType == NATIVE_TYPE_INT || nativeType == NATIVE_TYPE_UINT || nativeType == NATIVE_TYPE_DEFAULT))
            {
                m_resID = IDS_EE_BADMARSHAL_I;
                IfFailGoto(E_FAIL, lFail);
            }
#ifdef TARGET_64BIT
            m_type = MARSHAL_TYPE_GENERIC_8;
#else
            m_type = MARSHAL_TYPE_GENERIC_4;
#endif
            break;

        case ELEMENT_TYPE_U:

            if (!(nativeType == NATIVE_TYPE_UINT || nativeType == NATIVE_TYPE_INT || nativeType == NATIVE_TYPE_DEFAULT))
            {
                m_resID = IDS_EE_BADMARSHAL_I;
                IfFailGoto(E_FAIL, lFail);
            }
#ifdef TARGET_64BIT
            m_type = MARSHAL_TYPE_GENERIC_8;
#else
            m_type = MARSHAL_TYPE_GENERIC_4;
#endif
            break;


        case ELEMENT_TYPE_R4:
            if (!(nativeType == NATIVE_TYPE_R4 || nativeType == NATIVE_TYPE_DEFAULT))
            {
                m_resID = IDS_EE_BADMARSHAL_R4;
                IfFailGoto(E_FAIL, lFail);
            }
            m_type = MARSHAL_TYPE_FLOAT;
            break;

        case ELEMENT_TYPE_R8:
            if (!(nativeType == NATIVE_TYPE_R8 || nativeType == NATIVE_TYPE_DEFAULT))
            {
                m_resID = IDS_EE_BADMARSHAL_R8;
                IfFailGoto(E_FAIL, lFail);
            }
            m_type = MARSHAL_TYPE_DOUBLE;
            break;

        case ELEMENT_TYPE_PTR:
            if (nativeType != NATIVE_TYPE_DEFAULT)
            {
                m_resID = IDS_EE_BADMARSHAL_PTR;
                IfFailGoto(E_FAIL, lFail);
            }
#ifdef TARGET_64BIT
            m_type = MARSHAL_TYPE_GENERIC_8;
#else
            m_type = MARSHAL_TYPE_GENERIC_4;
#endif
            break;

        case ELEMENT_TYPE_FNPTR:
            if (!(nativeType == NATIVE_TYPE_FUNC || nativeType == NATIVE_TYPE_DEFAULT))
            {
                m_resID = IDS_EE_BADMARSHAL_FNPTR;
                IfFailGoto(E_FAIL, lFail);
            }
#ifdef TARGET_64BIT
            m_type = MARSHAL_TYPE_GENERIC_8;
#else
            m_type = MARSHAL_TYPE_GENERIC_4;
#endif
            break;

        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_VAR:
        {
            TypeHandle sigTH = sig.GetTypeHandleThrowing(pModule, pTypeContext);

            // Disallow marshaling generic types.
            if (sigTH.HasInstantiation())
            {
                m_resID = IDS_EE_BADMARSHAL_GENERICS_RESTRICTION;
                IfFailGoto(E_FAIL, lFail);
            }

            m_pMT = sigTH.GetMethodTable();
            if (m_pMT == NULL)
                IfFailGoto(COR_E_TYPELOAD, lFail);

#ifdef FEATURE_COMINTEROP
            if (nativeType == NATIVE_TYPE_INTF)
            {
                // whatever...
                if (sig.IsStringType(pModule, pTypeContext))
                {
                    m_resID = IsFieldScenario() ? IDS_EE_BADMARSHALFIELD_STRING : IDS_EE_BADMARSHALPARAM_STRING;
                    IfFailGoto(E_FAIL, lFail);
                }

                if (COMDelegate::IsDelegate(m_pMT))
                {
                    // UnmanagedType.Interface for delegates used to mean the .NET Framework _Delegate interface.
                    // We don't support that interface in .NET Core, so we disallow marshalling as it here.
                    // The user can specify UnmanagedType.IDispatch and use the delegate through the IDispatch interface
                    // if they need an interface pointer.
                    m_resID = IDS_EE_BADMARSHAL_DELEGATE_TLB_INTERFACE;
                    IfFailGoto(E_FAIL, lFail);
                }
                m_type = MARSHAL_TYPE_INTERFACE;
            }
            else
#endif // FEATURE_COMINTEROP
            {
                bool builder = false;
                if (sig.IsStringTypeThrowing(pModule, pTypeContext)
                    || ((builder = true), 0)
                    || sig.IsClassThrowing(pModule, g_StringBufferClassName, pTypeContext)
                    )
                {
                    if (builder && m_ms == MARSHAL_SCENARIO_FIELD)
                    {
                        m_resID = IDS_EE_BADMARSHALFIELD_NOSTRINGBUILDER;
                        IfFailGoto(E_FAIL, lFail);
                    }

                    switch ( nativeType )
                    {
                        case NATIVE_TYPE_LPWSTR:
                            m_type = builder ? MARSHAL_TYPE_LPWSTR_BUFFER : MARSHAL_TYPE_LPWSTR;
                            break;

                        case NATIVE_TYPE_LPSTR:
                            m_type = builder ? MARSHAL_TYPE_LPSTR_BUFFER : MARSHAL_TYPE_LPSTR;
                            break;

                        case NATIVE_TYPE_LPUTF8STR:
                            m_type = builder ? MARSHAL_TYPE_UTF8_BUFFER : MARSHAL_TYPE_LPUTF8STR;
                            break;

                        case NATIVE_TYPE_LPTSTR:
                        {
#ifdef FEATURE_COMINTEROP
                            if (m_ms != MARSHAL_SCENARIO_NDIRECT && m_ms != MARSHAL_SCENARIO_FIELD)
                            {
                                _ASSERTE(m_ms == MARSHAL_SCENARIO_COMINTEROP);
                                // We disallow NATIVE_TYPE_LPTSTR for COM.
                                IfFailGoto(E_FAIL, lFail);
                            }
#endif // FEATURE_COMINTEROP
                            // We no longer support Win9x so LPTSTR always maps to a Unicode string.
                            m_type = builder ? MARSHAL_TYPE_LPWSTR_BUFFER : MARSHAL_TYPE_LPWSTR;
                            break;
                        }

                        case NATIVE_TYPE_BSTR:
                            if (builder)
                            {
                                m_resID = IDS_EE_BADMARSHALPARAM_STRINGBUILDER;
                                IfFailGoto(E_FAIL, lFail);
                            }
                            m_type = MARSHAL_TYPE_BSTR;
                            break;

                        case NATIVE_TYPE_ANSIBSTR:
                            if (builder)
                            {
                                m_resID = IDS_EE_BADMARSHALPARAM_STRINGBUILDER;
                                IfFailGoto(E_FAIL, lFail);
                            }
                            m_type = MARSHAL_TYPE_ANSIBSTR;
                            break;

                        case NATIVE_TYPE_TBSTR:
                        {
                            if (builder)
                            {
                                m_resID = IDS_EE_BADMARSHALPARAM_STRINGBUILDER;
                                IfFailGoto(E_FAIL, lFail);
                            }

                            // We no longer support Win9x so TBSTR always maps to a normal (unicode) BSTR.
                            m_type = MARSHAL_TYPE_BSTR;
                            break;
                        }

#ifdef FEATURE_COMINTEROP
                        case NATIVE_TYPE_BYVALSTR:
                        {
                            if (builder)
                            {
                                m_resID = IDS_EE_BADMARSHALPARAM_STRINGBUILDER;
                                IfFailGoto(E_FAIL, lFail);
                            }
                            m_type = m_fAnsi ? MARSHAL_TYPE_VBBYVALSTR : MARSHAL_TYPE_VBBYVALSTRW;
                            break;
                        }

                        case NATIVE_TYPE_HSTRING:
                        {
                            if (builder)
                            {
                                m_resID = IDS_EE_BADMARSHALPARAM_STRINGBUILDER;
                                IfFailGoto(E_FAIL, lFail);
                            }

                            IfFailGoto(E_FAIL, lFail);
                            break;
                        }
#endif // FEATURE_COMINTEROP
                        case NATIVE_TYPE_FIXEDSYSSTRING:
                        {
                            if (m_ms == MARSHAL_SCENARIO_FIELD)
                            {
                                if (ParamInfo.m_Additive == 0)
                                {
                                    m_resID = IDS_EE_BADMARSHALFIELD_ZEROLENGTHFIXEDSTRING;
                                    IfFailGoto(E_FAIL, lFail);
                                }

                                m_args.fs.fixedStringLength = ParamInfo.m_Additive;

                                m_type = m_fAnsi ? MARSHAL_TYPE_FIXED_CSTR : MARSHAL_TYPE_FIXED_WSTR;
                            }
                            break;
                        }

                        case NATIVE_TYPE_DEFAULT:
                        {
#ifdef FEATURE_COMINTEROP
                            if (m_ms == MARSHAL_SCENARIO_COMINTEROP)
                            {
                                m_type = builder ? MARSHAL_TYPE_LPWSTR_BUFFER : MARSHAL_TYPE_BSTR;
                            }
                            else
#endif // FEATURE_COMINTEROP
                            if (m_fAnsi)
                            {
                                m_type = builder ? MARSHAL_TYPE_LPSTR_BUFFER : MARSHAL_TYPE_LPSTR;
                            }
                            else
                            {
                                m_type = builder ? MARSHAL_TYPE_LPWSTR_BUFFER : MARSHAL_TYPE_LPWSTR;
                            }
                            break;
                        }

                        default:
                            if (IsFieldScenario())
                            {
                                m_resID = IDS_EE_BADMARSHALFIELD_STRING;
                            }
                            else if (builder)
                            {
                                m_resID = IDS_EE_BADMARSHALPARAM_STRINGBUILDER;
                            }
                            else
                            {
                                m_resID = IDS_EE_BADMARSHALPARAM_STRING;
                            }

                            IfFailGoto(E_FAIL, lFail);
                            break;
                    }
                }
#ifdef FEATURE_COMINTEROP
                else if (sig.IsClassThrowing(pModule, g_CollectionsEnumeratorClassName, pTypeContext) &&
                         nativeType == NATIVE_TYPE_DEFAULT &&
                         !IsFieldScenario())
                {
                    m_CMVt = VT_UNKNOWN;
                    m_type = MARSHAL_TYPE_REFERENCECUSTOMMARSHALER;

                    if (fLoadCustomMarshal)
                    {
                        if (!fEmitsIL)
                        {
                            m_pCMHelper = SetupCustomMarshalerHelper(ENUMERATOR_TO_ENUM_VARIANT_CM_NAME,
                                                                     ENUMERATOR_TO_ENUM_VARIANT_CM_NAME_LEN,
                                                                     ENUMERATOR_TO_ENUM_VARIANT_CM_COOKIE,
                                                                     ENUMERATOR_TO_ENUM_VARIANT_CM_COOKIE_LEN,
                                                                     pAssembly, sigTH);
                        }
                        else
                        {
                            m_pCMHelper = NULL;
                            MethodDesc* pMDforModule = pMD;
                            if (pMD->IsILStub())
                            {
                                pMDforModule = pMD->AsDynamicMethodDesc()->GetILStubResolver()->GetStubTargetMethodDesc();
                            }
                            m_args.rcm.m_pMD = pMDforModule;
                            m_args.rcm.m_paramToken = token;
                            m_args.rcm.m_hndManagedType = sigTH.AsPtr();
                            CONSISTENCY_CHECK(pModule == pMDforModule->GetModule());
                        }
                    }
                }
#endif // FEATURE_COMINTEROP
                else if (sigTH.CanCastTo(TypeHandle(CoreLibBinder::GetClass(CLASS__SAFE_HANDLE))))
                {
                    if (nativeType != NATIVE_TYPE_DEFAULT)
                    {
                        m_resID = IDS_EE_BADMARSHAL_SAFEHANDLE;
                        IfFailGoto(E_FAIL, lFail);
                    }
                    m_args.m_pMT = m_pMT;
                    m_type = MARSHAL_TYPE_SAFEHANDLE;
                }
                else if (sigTH.CanCastTo(TypeHandle(CoreLibBinder::GetClass(CLASS__CRITICAL_HANDLE))))
                {
                    if (nativeType != NATIVE_TYPE_DEFAULT)
                    {
                        m_resID = IDS_EE_BADMARSHAL_CRITICALHANDLE;
                        IfFailGoto(E_FAIL, lFail);
                    }
                    m_args.m_pMT = m_pMT;
                    m_type = MARSHAL_TYPE_CRITICALHANDLE;
                }
#ifdef FEATURE_COMINTEROP
                else if (m_pMT->IsInterface())
                {
                    if (!(nativeType == NATIVE_TYPE_DEFAULT ||
                          nativeType == NATIVE_TYPE_INTF))
                    {
                        m_resID = IDS_EE_BADMARSHAL_INTERFACE;
                        IfFailGoto(E_FAIL, lFail);
                    }
                    m_type = MARSHAL_TYPE_INTERFACE;
                }
#endif // FEATURE_COMINTEROP
                else if (COMDelegate::IsDelegate(m_pMT))
                {
                    m_args.m_pMT = m_pMT;

                    switch (nativeType)
                    {
                        case NATIVE_TYPE_FUNC:
                            m_type = MARSHAL_TYPE_DELEGATE;
                            break;

                        case NATIVE_TYPE_DEFAULT:
#ifdef FEATURE_COMINTEROP
                            if (m_ms == MARSHAL_SCENARIO_COMINTEROP)
                            {
                                // Default for COM marshalling for delegates used to mean the .NET Framework _Delegate interface.
                                // We don't support that interface in .NET Core, so we disallow marshalling as it here.
                                // The user can specify UnmanagedType.IDispatch and use the delegate through the IDispatch interface
                                // if they need an interface pointer.
                                m_resID = IDS_EE_BADMARSHAL_DELEGATE_TLB_INTERFACE;
                                IfFailGoto(E_FAIL, lFail);
                            }
                            else
#endif // FEATURE_COMINTEROP
                                m_type = MARSHAL_TYPE_DELEGATE;

                            break;
#ifdef FEATURE_COMINTEROP
                        case NATIVE_TYPE_IDISPATCH:
                            m_type = MARSHAL_TYPE_INTERFACE;
                            break;
#endif
                        default:
                        m_resID = IDS_EE_BADMARSHAL_DELEGATE;
                        IfFailGoto(E_FAIL, lFail);
                            break;
                    }
                }
                else if (m_pMT->IsBlittable())
                {
                    if (!(nativeType == NATIVE_TYPE_DEFAULT || nativeType == (IsFieldScenario() ? NATIVE_TYPE_STRUCT : NATIVE_TYPE_LPSTRUCT)))
                    {
                        m_resID = IsFieldScenario() ? IDS_EE_BADMARSHALFIELD_LAYOUTCLASS : IDS_EE_BADMARSHAL_CLASS;
                        IfFailGoto(E_FAIL, lFail);
                    }
                    m_type = IsFieldScenario() ? MARSHAL_TYPE_BLITTABLE_LAYOUTCLASS : MARSHAL_TYPE_BLITTABLEPTR;
                    m_args.m_pMT = m_pMT;
                }
                else if (m_pMT->HasLayout())
                {
                    if (!(nativeType == NATIVE_TYPE_DEFAULT || nativeType == (IsFieldScenario() ? NATIVE_TYPE_STRUCT : NATIVE_TYPE_LPSTRUCT)))
                    {
                        m_resID = IsFieldScenario() ? IDS_EE_BADMARSHALFIELD_LAYOUTCLASS : IDS_EE_BADMARSHAL_CLASS;
                        IfFailGoto(E_FAIL, lFail);
                    }
                    m_type = IsFieldScenario() ? MARSHAL_TYPE_LAYOUTCLASS : MARSHAL_TYPE_LAYOUTCLASSPTR;
                    m_args.m_pMT = m_pMT;
                }
                else if (m_pMT->IsObjectClass())
                {
                    switch(nativeType)
                    {
#ifdef FEATURE_COMINTEROP
                        case NATIVE_TYPE_DEFAULT:
                            if (ms == MARSHAL_SCENARIO_FIELD)
                            {
                                m_type = MARSHAL_TYPE_INTERFACE;
                                break;
                            }
                            // fall through
                        case NATIVE_TYPE_STRUCT:
                            m_type = MARSHAL_TYPE_OBJECT;
                            break;

                        case NATIVE_TYPE_INTF:
                        case NATIVE_TYPE_IUNKNOWN:
                            m_type = MARSHAL_TYPE_INTERFACE;
                            break;

                        case NATIVE_TYPE_IDISPATCH:
                            m_fDispItf = TRUE;
                            m_type = MARSHAL_TYPE_INTERFACE;
                            break;

                        case NATIVE_TYPE_IINSPECTABLE:
                            m_resID = IDS_EE_NO_IINSPECTABLE;
                            break;
#else
                        case NATIVE_TYPE_DEFAULT:
                        case NATIVE_TYPE_STRUCT:
                            m_resID = IDS_EE_OBJECT_TO_VARIANT_NOT_SUPPORTED;
                            IfFailGoto(E_FAIL, lFail);
                            break;

                        case NATIVE_TYPE_INTF:
                        case NATIVE_TYPE_IUNKNOWN:
                        case NATIVE_TYPE_IDISPATCH:
                            m_resID = IDS_EE_OBJECT_TO_ITF_NOT_SUPPORTED;
                            IfFailGoto(E_FAIL, lFail);
                            break;
#endif // FEATURE_COMINTEROP

                        case NATIVE_TYPE_ASANY:
                            m_type = m_fAnsi ? MARSHAL_TYPE_ASANYA : MARSHAL_TYPE_ASANYW;
                            break;

                        default:
                            m_resID = IsFieldScenario() ? IDS_EE_BADMARSHALFIELD_OBJECT : IDS_EE_BADMARSHAL_OBJECT;
                            IfFailGoto(E_FAIL, lFail);
                    }
                }

#ifdef FEATURE_COMINTEROP
                else if (sig.IsClassThrowing(pModule, g_ArrayClassName, pTypeContext))
                {
                    switch(nativeType)
                    {
                        case NATIVE_TYPE_DEFAULT:
                        case NATIVE_TYPE_INTF:
                            m_type = MARSHAL_TYPE_INTERFACE;
                            break;

                        case NATIVE_TYPE_SAFEARRAY:
                        {
                            TypeHandle thElement = TypeHandle(g_pObjectClass);

                            if (ParamInfo.m_SafeArrayElementVT != VT_EMPTY)
                            {
                                if (ParamInfo.m_cSafeArrayUserDefTypeNameBytes > 0)
                                {
                                    // Load the type. Use an SString for the string since we need to NULL terminate the string
                                    // that comes from the metadata.
                                    StackScratchBuffer utf8Name;
                                    SString safeArrayUserDefTypeName(SString::Utf8, ParamInfo.m_strSafeArrayUserDefTypeName, ParamInfo.m_cSafeArrayUserDefTypeNameBytes);
                                    thElement = TypeName::GetTypeUsingCASearchRules(safeArrayUserDefTypeName.GetUTF8(utf8Name), pAssembly);
                                }
                            }
                            else
                            {
                                // Compat: If no safe array VT was specified, default to VT_VARIANT.
                                ParamInfo.m_SafeArrayElementVT = VT_VARIANT;
                            }

                            IfFailGoto(HandleArrayElemType(&ParamInfo, thElement, -1, FALSE, isParam, pAssembly, TRUE), lFail);
                            break;
                        }

                        default:
                            m_resID = IDS_EE_BADMARSHAL_SYSARRAY;
                            IfFailGoto(E_FAIL, lFail);

                    }
                }

                else if (m_pMT->IsArray())
                {
                    _ASSERTE(!"This invalid signature should never be hit!");
                    IfFailGoto(E_FAIL, lFail);
                }
#endif // FEATURE_COMINTEROP
                else if (!m_pMT->IsValueType())
                {
                    if (!(nativeType == NATIVE_TYPE_INTF || nativeType == NATIVE_TYPE_DEFAULT))
                    {
                        m_resID = IDS_EE_BADMARSHAL_NOLAYOUT;
                        IfFailGoto(E_FAIL, lFail);
                    }
#ifdef FEATURE_COMINTEROP
                    // default marshalling is interface
                    m_type = MARSHAL_TYPE_INTERFACE;
#else // FEATURE_COMINTEROP
                    m_resID = IDS_EE_OBJECT_TO_ITF_NOT_SUPPORTED;
                    IfFailGoto(E_FAIL, lFail);
#endif // FEATURE_COMINTEROP
                }

                else
                {
                    _ASSERTE(m_pMT->IsValueType());
                    goto lValueClass;
                }
            }
            break;
        }


        case ELEMENT_TYPE_VALUETYPE:
        lValueClass:
        {
            if (sig.IsClassThrowing(pModule, g_DecimalClassName, pTypeContext))
            {
                switch (nativeType)
                {
                    case NATIVE_TYPE_DEFAULT:
                    case NATIVE_TYPE_STRUCT:
                        m_type = MARSHAL_TYPE_DECIMAL;
                        break;

                    case NATIVE_TYPE_LPSTRUCT:
                        if (IsFieldScenario())
                        {
                            m_resID = IDS_EE_BADMARSHALFIELD_DECIMAL;
                            IfFailGoto(E_FAIL, lFail);
                        }
                        m_type = MARSHAL_TYPE_DECIMAL_PTR;
                        break;

                    case NATIVE_TYPE_CURRENCY:
                        m_type = MARSHAL_TYPE_CURRENCY;
                        break;

                    default:
                        m_resID = IsFieldScenario() ? IDS_EE_BADMARSHALFIELD_DECIMAL : IDS_EE_BADMARSHALPARAM_DECIMAL;
                        IfFailGoto(E_FAIL, lFail);
                }
            }
            else if (sig.IsClassThrowing(pModule, g_GuidClassName, pTypeContext))
            {
                switch (nativeType)
                {
                    case NATIVE_TYPE_DEFAULT:
                    case NATIVE_TYPE_STRUCT:
                        m_type = MARSHAL_TYPE_GUID;
                        break;

                    case NATIVE_TYPE_LPSTRUCT:
                        if (IsFieldScenario())
                        {
                            m_resID = IDS_EE_BADMARSHAL_GUID;
                            IfFailGoto(E_FAIL, lFail);
                        }
                        m_type = MARSHAL_TYPE_GUID_PTR;
                        break;

                    default:
                        m_resID = IDS_EE_BADMARSHAL_GUID;
                        IfFailGoto(E_FAIL, lFail);
                }
            }
            else if (sig.IsClassThrowing(pModule, g_DateClassName, pTypeContext))
            {
                if (!(nativeType == NATIVE_TYPE_DEFAULT || nativeType == NATIVE_TYPE_STRUCT))
                {
                    m_resID = IDS_EE_BADMARSHAL_DATETIME;
                    IfFailGoto(E_FAIL, lFail);
                }
                m_type = MARSHAL_TYPE_DATE;
            }
            else if (sig.IsClassThrowing(pModule, "System.Runtime.InteropServices.ArrayWithOffset", pTypeContext))
            {
                if (m_ms == MARSHAL_SCENARIO_FIELD)
                {
                    IfFailGoto(E_FAIL, lFail);
                }
                if (!(nativeType == NATIVE_TYPE_DEFAULT))
                {
                    IfFailGoto(E_FAIL, lFail);
                }
                m_type = MARSHAL_TYPE_ARRAYWITHOFFSET;
            }
            else if (sig.IsClassThrowing(pModule, "System.Runtime.InteropServices.HandleRef", pTypeContext))
            {
                if (m_ms == MARSHAL_SCENARIO_FIELD)
                {
                    IfFailGoto(E_FAIL, lFail);
                }
                if (!(nativeType == NATIVE_TYPE_DEFAULT))
                {
                    IfFailGoto(E_FAIL, lFail);
                }
                m_type = MARSHAL_TYPE_HANDLEREF;
            }
            else if (sig.IsClassThrowing(pModule, "System.ArgIterator", pTypeContext))
            {
                if (m_ms == MARSHAL_SCENARIO_FIELD)
                {
                    IfFailGoto(E_FAIL, lFail);
                }
                if (!(nativeType == NATIVE_TYPE_DEFAULT))
                {
                    IfFailGoto(E_FAIL, lFail);
                }
                m_type = MARSHAL_TYPE_ARGITERATOR;
            }
#ifdef FEATURE_COMINTEROP
            else if (sig.IsClassThrowing(pModule, g_ColorClassName, pTypeContext))
            {
                if (!(nativeType == NATIVE_TYPE_DEFAULT))
                {
                    IfFailGoto(E_FAIL, lFail);
                }

                // This is only supported for COM interop.
                if (m_ms != MARSHAL_SCENARIO_COMINTEROP)
                {
                    IfFailGoto(E_FAIL, lFail);
                }

                m_type = MARSHAL_TYPE_OLECOLOR;
            }
#endif // FEATURE_COMINTEROP
            else if (sig.IsClassThrowing(pModule, g_RuntimeTypeHandleClassName, pTypeContext))
            {
                if (nativeType != NATIVE_TYPE_DEFAULT || IsFieldScenario())
                {
                    IfFailGoto(E_FAIL, lFail);
                }

                m_type = MARSHAL_TYPE_RUNTIMETYPEHANDLE;
            }
            else if (sig.IsClassThrowing(pModule, g_RuntimeFieldHandleClassName, pTypeContext))
            {
                if (nativeType != NATIVE_TYPE_DEFAULT || IsFieldScenario())
                {
                    IfFailGoto(E_FAIL, lFail);
                }

                m_type = MARSHAL_TYPE_RUNTIMEFIELDHANDLE;
            }
            else if (sig.IsClassThrowing(pModule, g_RuntimeMethodHandleClassName, pTypeContext))
            {
                if (nativeType != NATIVE_TYPE_DEFAULT || IsFieldScenario())
                {
                    IfFailGoto(E_FAIL, lFail);
                }

                m_type = MARSHAL_TYPE_RUNTIMEMETHODHANDLE;
            }
            else
            {
                m_pMT =  sig.GetTypeHandleThrowing(pModule, pTypeContext).GetMethodTable();
                if (m_pMT == NULL)
                    break;

                // Blittable generics are allowed to be marshalled with the following exceptions:
                // * ByReference<T>: This represents an interior pointer and is not actually blittable
                // * Nullable<T>: We don't want to be locked into the default behavior as we may want special handling later
                // * Vector64<T>: Represents the __m64 ABI primitive which requires currently unimplemented handling
                // * Vector128<T>: Represents the __m128 ABI primitive which requires currently unimplemented handling
                // * Vector256<T>: Represents the __m256 ABI primitive which requires currently unimplemented handling
                // * Vector<T>: Has a variable size (either __m128 or __m256) and isn't readily usable for interop scenarios
                // We can't block these types for field scenarios for back-compat reasons.
                if (m_pMT->HasInstantiation() && !IsFieldScenario()
                    && (!m_pMT->IsBlittable()
                        || (m_pMT->HasSameTypeDefAs(g_pNullableClass)
                        || m_pMT->HasSameTypeDefAs(g_pByReferenceClass)
                        || m_pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__SPAN))
                        || m_pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__READONLY_SPAN))
                        || m_pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR64T))
                        || m_pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR128T))
                        || m_pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTOR256T))
#ifndef CROSSGEN_COMPILE
                            // Crossgen scenarios block Vector<T> from even being loaded
                            || m_pMT->HasSameTypeDefAs(CoreLibBinder::GetClass(CLASS__VECTORT))
#endif // !CROSSGEN_COMPILE
                    )))
                {
                    m_resID = IDS_EE_BADMARSHAL_GENERICS_RESTRICTION;
                    IfFailGoto(E_FAIL, lFail);
                }

                if (!m_pMT->HasLayout())
                {
                    m_resID = IDS_EE_BADMARSHAL_AUTOLAYOUT;
                    IfFailGoto(E_FAIL, lFail);
                }

                UINT managedSize = m_pMT->GetAlignedNumInstanceFieldBytes();
                UINT  nativeSize = 0;

                if ( nativeSize > 0xfff0 ||
                    managedSize > 0xfff0)
                {
                    m_resID = IDS_EE_STRUCTTOOCOMPLEX;
                    IfFailGoto(E_FAIL, lFail);
                }

                if (m_pMT->IsBlittable())
                {
                    if (!(nativeType == NATIVE_TYPE_DEFAULT || nativeType == NATIVE_TYPE_STRUCT))
                    {
                        m_resID = IDS_EE_BADMARSHAL_VALUETYPE;
                        IfFailGoto(E_FAIL, lFail);
                    }

                    if (m_byref && !isParam && !IsFieldScenario())
                    {
                        // Override the prohibition on byref returns so that IJW works
                        m_byref = FALSE;
                        m_type = ((TARGET_POINTER_SIZE == 4) ? MARSHAL_TYPE_GENERIC_4 : MARSHAL_TYPE_GENERIC_8);
                    }
                    else
                    {
                        if (fNeedsCopyCtor && !IsFieldScenario()) // We don't support automatically discovering copy constructors for fields.
                        {
                            MethodDesc *pCopyCtor;
                            MethodDesc *pDtor;
                            FindCopyCtor(pModule, m_pMT, &pCopyCtor);
                            FindDtor(pModule, m_pMT, &pDtor);

                            m_args.mm.m_pMT = m_pMT;
                            m_args.mm.m_pCopyCtor = pCopyCtor;
                            m_args.mm.m_pDtor = pDtor;
                            m_type = MARSHAL_TYPE_BLITTABLEVALUECLASSWITHCOPYCTOR;
                        }
                        else
                        {
                            m_args.m_pMT = m_pMT;
                            m_type = MARSHAL_TYPE_BLITTABLEVALUECLASS;
                        }
                    }
                }
                else
                {
                    if (!(nativeType == NATIVE_TYPE_DEFAULT || nativeType == NATIVE_TYPE_STRUCT))
                    {
                        m_resID = IDS_EE_BADMARSHAL_VALUETYPE;
                        IfFailGoto(E_FAIL, lFail);
                    }
#ifdef _DEBUG
                    if (ms == MARSHAL_SCENARIO_FIELD && fEmitsIL)
                    {
                        _ASSERTE_MSG(!IsFixedBuffer(token, pModule->GetMDImport()), "Cannot correctly marshal fixed buffers of non-blittable types");
                    }
#endif
                    m_args.m_pMT = m_pMT;
                    m_type = MARSHAL_TYPE_VALUECLASS;
                }
            }
            break;
        }

        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_ARRAY:
        {
            // Get class info from array.
            TypeHandle arrayTypeHnd = sig.GetTypeHandleThrowing(pModule, pTypeContext);
            _ASSERTE(!arrayTypeHnd.IsNull());

            TypeHandle thElement = arrayTypeHnd.GetArrayElementTypeHandle();

            if (thElement.HasInstantiation() && !thElement.IsBlittable())
            {
                m_resID = IDS_EE_BADMARSHAL_GENERICS_RESTRICTION;
                IfFailGoto(E_FAIL, lFail);
            }

            m_args.na.m_pArrayMT = arrayTypeHnd.AsMethodTable();

            // Handle retrieving the information for the array type.
            IfFailGoto(HandleArrayElemType(&ParamInfo, thElement, arrayTypeHnd.GetRank(), mtype == ELEMENT_TYPE_SZARRAY, isParam, pAssembly), lFail);
            break;
        }

        default:
            m_resID = IDS_EE_BADMARSHAL_BADMANAGED;
    }

lExit:
    if (m_byref && !isParam)
    {
        // byref returns don't work: the thing pointed to lives on
        // a stack that disappears!
        m_type = MARSHAL_TYPE_UNKNOWN;
        goto lReallyExit;
    }

    //---------------------------------------------------------------------
    // Now, figure out the IN/OUT status.
    // Also set the m_fOleVarArgCandidate here to save perf of invoking Metadata API
    //---------------------------------------------------------------------
    m_fOleVarArgCandidate = FALSE;
    if (m_type != MARSHAL_TYPE_UNKNOWN && IsInOnly(m_type) && !m_byref)
    {
        // If we got here, the parameter is something like an "int" where
        // [in] is the only semantically valid choice. Since there is no
        // possible way to interpret an [out] for such a type, we will ignore
        // the metadata and force the bits to "in". We could have defined
        // it as an error instead but this is less likely to cause problems
        // with metadata autogenerated from typelibs and poorly
        // defined C headers.
        //
        m_in = TRUE;
        m_out = FALSE;
    }
    else
    {

        // Capture and save away "In/Out" bits. If none is present, set both to FALSE (they will be properly defaulted downstream)
        if (token == mdParamDefNil)
        {
            m_in = FALSE;
            m_out = FALSE;
        }
        else if (TypeFromToken(token) != mdtParamDef)
        {
            _ASSERTE(TypeFromToken(token) == mdtFieldDef);

            // Field setters are always In, the flags are ignored for return values of getters
            m_in = TRUE;
            m_out = FALSE;
        }
        else
        {
            IMDInternalImport *pInternalImport = pModule->GetMDImport();
            USHORT             usSequence;
            DWORD              dwAttr;
            LPCSTR             szParamName_Ignore;

            if (FAILED(pInternalImport->GetParamDefProps(token, &usSequence, &dwAttr, &szParamName_Ignore)))
            {
                m_in = FALSE;
                m_out = FALSE;
            }
            else
            {
                m_in = IsPdIn(dwAttr) != 0;
                m_out = IsPdOut(dwAttr) != 0;
#ifdef FEATURE_COMINTEROP
                // set m_fOleVarArgCandidate. The rule is same as the one defined in vm\tlbexp.cpp
                if(paramidx == numArgs &&                   // arg is the last arg of the method
                   !(dwAttr & PARAMFLAG_FOPT) &&            // arg is not a optional arg
                   !IsNilToken(token) &&                    // token is not a nil token
                   (m_type == MARSHAL_TYPE_SAFEARRAY) &&    // arg is marshaled as SafeArray
                   (m_arrayElementType == VT_VARIANT))      // the element of the safearray is VARIANT
                {
                    // check if it has default value
                    MDDefaultValue defaultValue;
                    if (SUCCEEDED(pInternalImport->GetDefaultValue(token, &defaultValue)) && defaultValue.m_bType == ELEMENT_TYPE_VOID)
                    {
                        // check if it has params attribute
                        if (pModule->GetCustomAttribute(token, WellKnownAttribute::ParamArray, 0,0) == S_OK)
                            m_fOleVarArgCandidate = TRUE;
                    }
                }
#endif
            }
        }

        if (!m_in && !m_out)
        {
            // If neither IN nor OUT are true, this signals the URT to use the default
            // rules.
            if (m_byref ||
                 (mtype == ELEMENT_TYPE_CLASS
                  && !(sig.IsStringType(pModule, pTypeContext))
                  && sig.IsClass(pModule, g_StringBufferClassName, pTypeContext)))
            {
                m_in = TRUE;
                m_out = TRUE;
            }
            else
            {
                m_in = TRUE;
                m_out = FALSE;
            }
        }
    }

lReallyExit:

#ifdef _DEBUG
    DumpMarshalInfo(pModule, sig, pTypeContext, token, ms, nlType, nlFlags);
#endif
    return;


  lFail:
    // We got here because of an illegal ELEMENT_TYPE/NATIVE_TYPE combo.
    m_type = MARSHAL_TYPE_UNKNOWN;
    //_ASSERTE(!"Invalid ELEMENT_TYPE/NATIVE_TYPE combination");
    goto lExit;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

VOID MarshalInfo::EmitOrThrowInteropParamException(NDirectStubLinker* psl, BOOL fMngToNative, UINT resID, UINT paramIdx)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef FEATURE_COMINTEROP
    // If this is not forward COM interop, throw the exception right away. We rely on this
    // for example in code:ComPreStubWorker when we fire the InvalidMemberDeclaration MDA.
    if (m_ms == MARSHAL_SCENARIO_COMINTEROP && fMngToNative && !IsFieldScenario())
    {
        psl->SetInteropParamExceptionInfo(resID, paramIdx);
        return;
    }
#endif // FEATURE_COMINTEROP

    ThrowInteropParamException(resID, paramIdx);
}

void MarshalInfo::ThrowTypeLoadExceptionForInvalidFieldMarshal(FieldDesc* pFieldDesc, UINT resID)
{
    DefineFullyQualifiedNameForClassW();

    StackSString ssFieldName(SString::Utf8, pFieldDesc->GetName());

    StackSString errorString(W("Unknown error."));
    errorString.LoadResource(CCompRC::Error, resID);

    COMPlusThrow(kTypeLoadException, IDS_EE_BADMARSHALFIELD_ERROR_MSG,
        GetFullyQualifiedNameForClassW(pFieldDesc->GetEnclosingMethodTable()),
        ssFieldName.GetUnicode(), errorString.GetUnicode());
}


HRESULT MarshalInfo::HandleArrayElemType(NativeTypeParamInfo *pParamInfo, TypeHandle thElement, int iRank, BOOL fNoLowerBounds, BOOL isParam, Assembly *pAssembly, BOOL isArrayClass /* = FALSE */)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pParamInfo));
    }
    CONTRACTL_END;

    ArrayMarshalInfo arrayMarshalInfo(amiRuntime);


    //
    // Store rank and bound information.
    //

    m_iArrayRank = iRank;
    m_nolowerbounds = fNoLowerBounds;


    //
    // Determine which type of marshaler to use.
    //

#ifdef FEATURE_COMINTEROP
    if (pParamInfo->m_NativeType == NATIVE_TYPE_SAFEARRAY)
    {
        m_type = MARSHAL_TYPE_SAFEARRAY;
    }
    else
#endif // FEATURE_COMINTEROP
    if (pParamInfo->m_NativeType == NATIVE_TYPE_ARRAY)
    {
        m_type = MARSHAL_TYPE_NATIVEARRAY;
    }
    else if (pParamInfo->m_NativeType == NATIVE_TYPE_DEFAULT)
    {
        if (m_ms == MARSHAL_SCENARIO_FIELD)
        {
#ifdef FEATURE_COMINTEROP
            m_type = MARSHAL_TYPE_SAFEARRAY;
#else
            m_resID = IDS_EE_BADMARSHALFIELD_ARRAY;
            return E_FAIL;
#endif
        }
        else
#ifdef FEATURE_COMINTEROP
        if (m_ms != MARSHAL_SCENARIO_NDIRECT)
        {
            m_type = MARSHAL_TYPE_SAFEARRAY;
        }
        else
#endif // FEATURE_COMINTEROP
        {
            m_type = MARSHAL_TYPE_NATIVEARRAY;
        }
    }
    else if (pParamInfo->m_NativeType == NATIVE_TYPE_FIXEDARRAY && m_ms == MARSHAL_SCENARIO_FIELD)
    {
        m_type = MARSHAL_TYPE_FIXED_ARRAY;
    }
    else
    {
        m_resID = IsFieldScenario() ? IDS_EE_BADMARSHALFIELD_ARRAY : IDS_EE_BADMARSHAL_ARRAY;
        return E_FAIL;
    }

#ifdef FEATURE_COMINTEROP
    if (m_type == MARSHAL_TYPE_SAFEARRAY)
    {
        arrayMarshalInfo.InitForSafeArray(m_ms, thElement, pParamInfo->m_SafeArrayElementVT, m_fAnsi);
    }
    else
#endif // FEATURE_COMINTEROP
    if (m_type == MARSHAL_TYPE_FIXED_ARRAY)
    {
        arrayMarshalInfo.InitForFixedArray(thElement, pParamInfo->m_ArrayElementType, m_fAnsi);
    }
    else
    {
        _ASSERTE(m_type == MARSHAL_TYPE_NATIVEARRAY);
        arrayMarshalInfo.InitForNativeArray(m_ms, thElement, pParamInfo->m_ArrayElementType, m_fAnsi);
    }

    // Make sure the marshalling information is valid.
    if (!arrayMarshalInfo.IsValid())
    {
        m_resID = arrayMarshalInfo.GetErrorResourceId();
        return E_FAIL;
    }

    // Set the array type handle and VARTYPE to use for marshalling.
    m_hndArrayElemType = arrayMarshalInfo.GetElementTypeHandle();
    m_arrayElementType = arrayMarshalInfo.GetElementVT();

    if (m_type == MARSHAL_TYPE_NATIVEARRAY || m_type == MARSHAL_TYPE_FIXED_ARRAY)
    {
        // Retrieve the extra information associated with the native array marshaling.
        m_args.na.m_vt  = m_arrayElementType;
        m_countParamIdx = pParamInfo->m_CountParamIdx;
        m_multiplier    = pParamInfo->m_Multiplier;
        m_additive      = pParamInfo->m_Additive;

        if (m_type == MARSHAL_TYPE_FIXED_ARRAY)
        {
            if (m_additive == 0)
            {
                m_resID = IDS_EE_BADMARSHALFIELD_FIXEDARRAY_ZEROSIZE;
                return E_FAIL;
            }

            if (isArrayClass == TRUE)
            {
                // Compat: FixedArrays of System.Arrays map to fixed arrays of BSTRs.
                m_arrayElementType = VT_BSTR;
                m_args.na.m_vt = VT_BSTR;
                m_hndArrayElemType = g_pStringClass;
            }
        }
    }

    return S_OK;
}

ILMarshaler* CreateILMarshaler(MarshalInfo::MarshalType mtype, NDirectStubLinker* psl)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    ILMarshaler* pMarshaler = NULL;
    switch (mtype)
    {

#define DEFINE_MARSHALER_TYPE(mt, mclass) \
        case MarshalInfo::mt: \
            pMarshaler = new IL##mclass(); \
            break;
#include "mtypes.h"
#undef DEFINE_MARSHALER_TYPE

        default:
            UNREACHABLE_MSG("unexpected MarshalType passed to CreateILMarshaler");
    }

    pMarshaler->SetNDirectStubLinker(psl);
    return pMarshaler;
}

namespace
{
    DWORD CalculateArgumentMarshalFlags(BOOL byref, BOOL in, BOOL out, BOOL fMngToNative)
    {
        LIMITED_METHOD_CONTRACT;
        DWORD dwMarshalFlags = 0;

        if (byref)
        {
            dwMarshalFlags |= MARSHAL_FLAG_BYREF;
        }

        if (in)
        {
            dwMarshalFlags |= MARSHAL_FLAG_IN;
        }

        if (out)
        {
            dwMarshalFlags |= MARSHAL_FLAG_OUT;
        }

        if (fMngToNative)
        {
            dwMarshalFlags |= MARSHAL_FLAG_CLR_TO_NATIVE;
        }

        return dwMarshalFlags;
    }

    DWORD CalculateReturnMarshalFlags(BOOL hrSwap, BOOL fMngToNative)
    {
        LIMITED_METHOD_CONTRACT;
        DWORD dwMarshalFlags = MARSHAL_FLAG_RETVAL;

        if (hrSwap)
        {
            dwMarshalFlags |= MARSHAL_FLAG_HRESULT_SWAP;
        }

        if (fMngToNative)
        {
            dwMarshalFlags |= MARSHAL_FLAG_CLR_TO_NATIVE;
        }

        return dwMarshalFlags;
    }
}

void MarshalInfo::GenerateArgumentIL(NDirectStubLinker* psl,
                                     int argOffset, // the argument's index is m_paramidx + argOffset
                                     UINT nativeStackOffset, // offset of the argument on the native stack
                                     BOOL fMngToNative)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(psl));
    }
    CONTRACTL_END;

    if (m_type == MARSHAL_TYPE_UNKNOWN)
    {
        EmitOrThrowInteropParamException(psl, fMngToNative, m_resID, m_paramidx + 1); // m_paramidx is 0-based, but the user wants to see a 1-based index
        return;
    }

    // set up m_corArgSize and m_nativeArgSize
    SetupArgumentSizes();

    MarshalerOverrideStatus amostat;
    UINT resID = IDS_EE_BADMARSHAL_RESTRICTION;
    amostat = (GetArgumentOverrideProc(m_type)) (psl,
                                             m_byref,
                                             m_in,
                                             m_out,
                                             fMngToNative,
                                             &m_args,
                                             &resID,
                                             m_paramidx + argOffset,
                                             nativeStackOffset);


    if (amostat == OVERRIDDEN)
    {
        return;
    }

    if (amostat == DISALLOWED)
    {
        EmitOrThrowInteropParamException(psl, fMngToNative, resID, m_paramidx + 1); // m_paramidx is 0-based, but the user wants to see a 1-based index
        return;
    }

    CONSISTENCY_CHECK(amostat == HANDLEASNORMAL);

    NewHolder<ILMarshaler> pMarshaler = CreateILMarshaler(m_type, psl);
    DWORD dwMarshalFlags = CalculateArgumentMarshalFlags(m_byref, m_in, m_out, fMngToNative);

    if (!pMarshaler->SupportsArgumentMarshal(dwMarshalFlags, &resID))
    {
        EmitOrThrowInteropParamException(psl, fMngToNative, resID, m_paramidx + 1); // m_paramidx is 0-based, but the user wants to see a 1-based index
        return;
    }

    ILCodeStream* pcsMarshal    = psl->GetMarshalCodeStream();
    ILCodeStream* pcsUnmarshal  = psl->GetUnmarshalCodeStream();
    ILCodeStream* pcsDispatch   = psl->GetDispatchCodeStream();

    pcsMarshal->EmitNOP("// argument { ");
    pcsUnmarshal->EmitNOP("// argument { ");

    pMarshaler->EmitMarshalArgument(pcsMarshal, pcsUnmarshal, m_paramidx + argOffset, dwMarshalFlags, &m_args);

    //
    // Increment a counter so that when the finally clause
    // is run, we only run the cleanup that is needed.
    //
    if (pMarshaler->NeedsMarshalCleanupIndex())
    {
        // we don't bother writing to the counter if marshaling does not need cleanup
        psl->EmitSetArgMarshalIndex(pcsMarshal, NDirectStubLinker::CLEANUP_INDEX_ARG0_MARSHAL + m_paramidx + argOffset);
    }
    if (pMarshaler->NeedsUnmarshalCleanupIndex())
    {
        // we don't bother writing to the counter if unmarshaling does not need exception cleanup
        psl->EmitSetArgMarshalIndex(pcsUnmarshal, NDirectStubLinker::CLEANUP_INDEX_ARG0_UNMARSHAL + m_paramidx + argOffset);
    }

    pcsMarshal->EmitNOP("// } argument");
    pcsUnmarshal->EmitNOP("// } argument");

    pMarshaler->EmitSetupArgumentForDispatch(pcsDispatch);
    if (m_paramidx == 0)
    {
        CorCallingConvention callConv = psl->GetStubTargetCallingConv();
        if ((callConv & IMAGE_CEE_CS_CALLCONV_MASK) == IMAGE_CEE_UNMANAGED_CALLCONV_THISCALL)
        {
            // Make sure the 'this' argument to thiscall is of native int type; JIT asserts this.
            pcsDispatch->EmitCONV_I();
        }
    }
}

void MarshalInfo::GenerateReturnIL(NDirectStubLinker* psl,
    int argOffset,
    BOOL fMngToNative,
    BOOL fieldGetter,
    BOOL retval)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(psl));
    }
    CONTRACTL_END;

    MarshalerOverrideStatus amostat;
    UINT resID = IDS_EE_BADMARSHAL_RESTRICTION;

    if (m_type == MARSHAL_TYPE_UNKNOWN)
    {
        amostat = HANDLEASNORMAL;
    }
    else
    {
        amostat = (GetReturnOverrideProc(m_type)) (psl,
            fMngToNative,
            retval,
            &m_args,
            &resID);
    }

    if (amostat == DISALLOWED)
    {
        EmitOrThrowInteropParamException(psl, fMngToNative, resID, 0);
        return;
    }

    if (amostat == HANDLEASNORMAL)
    {
        // Historically we have always allowed reading fields that are marshaled as C arrays.
        if (m_type == MARSHAL_TYPE_UNKNOWN || (!fieldGetter && m_type == MARSHAL_TYPE_NATIVEARRAY))
        {
            EmitOrThrowInteropParamException(psl, fMngToNative, m_resID, 0);
            return;
        }

        NewHolder<ILMarshaler> pMarshaler = CreateILMarshaler(m_type, psl);
        DWORD dwMarshalFlags = CalculateReturnMarshalFlags(retval, fMngToNative);

        if (!pMarshaler->SupportsReturnMarshal(dwMarshalFlags, &resID))
        {
            EmitOrThrowInteropParamException(psl, fMngToNative, resID, 0);
            return;
        }

        ILCodeStream* pcsMarshal = psl->GetMarshalCodeStream();
        ILCodeStream* pcsUnmarshal = psl->GetReturnUnmarshalCodeStream();
        ILCodeStream* pcsDispatch = psl->GetDispatchCodeStream();

        pcsMarshal->EmitNOP("// return { ");
        pcsUnmarshal->EmitNOP("// return { ");

        UINT16 wNativeSize = GetNativeSize(m_type);

        // The following statement behaviour has existed for a long time. By aligning the size of the return
        // value up to stack slot size, we prevent EmitMarshalReturnValue from distinguishing between, say, 3-byte
        // structure and 4-byte structure. The former is supposed to be returned by-ref using a secret argument
        // (at least in MSVC compiled code) while the latter is returned in EAX. We are keeping the behavior for
        // now for backward compatibility.
        X86_ONLY(wNativeSize = StackElemSize(wNativeSize));

        pMarshaler->EmitMarshalReturnValue(pcsMarshal, pcsUnmarshal, pcsDispatch, m_paramidx + argOffset, wNativeSize, dwMarshalFlags, &m_args);

        pcsMarshal->EmitNOP("// } return");
        pcsUnmarshal->EmitNOP("// } return");

        return;
    }
}

void MarshalInfo::GenerateFieldIL(NDirectStubLinker* psl,
    UINT32 managedOffset,
    UINT32 nativeOffset,
    FieldDesc* pFieldDesc)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(psl));
        PRECONDITION(IsFieldScenario());
    }
    CONTRACTL_END;

    if (m_type == MARSHAL_TYPE_UNKNOWN)
    {
        ThrowTypeLoadExceptionForInvalidFieldMarshal(pFieldDesc, m_resID);
        return;
    }

    UINT resID = IDS_EE_BADMARSHAL_RESTRICTION;
    NewHolder<ILMarshaler> pMarshaler = CreateILMarshaler(m_type, psl);

    if (!pMarshaler->SupportsFieldMarshal(&resID))
    {
        ThrowTypeLoadExceptionForInvalidFieldMarshal(pFieldDesc, resID);
        return;
    }

    ILCodeStream* pcsMarshal = psl->GetMarshalCodeStream();
    ILCodeStream* pcsUnmarshal = psl->GetUnmarshalCodeStream();

    pcsMarshal->EmitNOP("// field { ");
    pcsUnmarshal->EmitNOP("// field { ");

    pMarshaler->EmitMarshalField(pcsMarshal, pcsUnmarshal, m_paramidx, managedOffset, nativeOffset, &m_args);

    pcsMarshal->EmitNOP("// } field");
    pcsUnmarshal->EmitNOP("// } field");

    return;
}

void MarshalInfo::SetupArgumentSizes()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    const unsigned targetPointerSize = TARGET_POINTER_SIZE;
    const bool pointerIsValueType = false;
    const bool pointerIsFloatHfa = false;
    _ASSERTE(targetPointerSize == StackElemSize(TARGET_POINTER_SIZE, pointerIsValueType, pointerIsFloatHfa));

    if (m_byref)
    {
        m_nativeArgSize = targetPointerSize;
    }
    else
    {
        const bool isValueType = IsValueClass(m_type);
        const bool isFloatHfa = isValueType && (m_pMT->GetHFAType() == CORINFO_HFA_ELEM_FLOAT);
        m_nativeArgSize = StackElemSize(GetNativeSize(m_type), isValueType, isFloatHfa);
    }

#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
    if (m_nativeArgSize > ENREGISTERED_PARAMTYPE_MAXSIZE)
    {
        m_nativeArgSize = targetPointerSize;
    }
#endif // ENREGISTERED_PARAMTYPE_MAXSIZE
}

UINT16 MarshalInfo::GetNativeSize(MarshalType mtype)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    static const BYTE nativeSizes[]=
    {
        #define DEFINE_MARSHALER_TYPE(mt, mclass) IL##mclass::c_nativeSize,
        #include "mtypes.h"
    };

    _ASSERTE((SIZE_T)mtype < COUNTOF(nativeSizes));
    BYTE nativeSize = nativeSizes[mtype];

    if (nativeSize == VARIABLESIZE)
    {
        _ASSERTE(IsValueClass(mtype));
        return (UINT16) m_pMT->GetNativeSize();
    }

    return nativeSize;
}

bool MarshalInfo::IsInOnly(MarshalType mtype)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    static const bool ILMarshalerIsInOnly[] =
    {
        #define DEFINE_MARSHALER_TYPE(mt, mclass) \
            (IL##mclass::c_fInOnly ? true : false),

        #include "mtypes.h"
    };

    return ILMarshalerIsInOnly[mtype];
}

bool MarshalInfo::IsValueClass(MarshalType mtype)
{
    switch (mtype)
    {
    case MARSHAL_TYPE_BLITTABLEVALUECLASS:
    case MARSHAL_TYPE_VALUECLASS:
    case MARSHAL_TYPE_BLITTABLEVALUECLASSWITHCOPYCTOR:
        return true;

    default:
        return false;
    }
}

OVERRIDEPROC MarshalInfo::GetArgumentOverrideProc(MarshalType mtype)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    static const OVERRIDEPROC ILArgumentOverrideProcs[] =
    {
        #define DEFINE_MARSHALER_TYPE(mt, mclass) IL##mclass::ArgumentOverride,
        #include "mtypes.h"
    };

    _ASSERTE((SIZE_T)mtype < COUNTOF(ILArgumentOverrideProcs));
    return ILArgumentOverrideProcs[mtype];
}

RETURNOVERRIDEPROC MarshalInfo::GetReturnOverrideProc(MarshalType mtype)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    static const RETURNOVERRIDEPROC ILReturnOverrideProcs[] =
    {
        #define DEFINE_MARSHALER_TYPE(mt, mclass) IL##mclass::ReturnOverride,
        #include "mtypes.h"
    };

    _ASSERTE((SIZE_T)mtype < COUNTOF(ILReturnOverrideProcs));
    return ILReturnOverrideProcs[mtype];
}

void MarshalInfo::GetItfMarshalInfo(ItfMarshalInfo* pInfo)
{
    STANDARD_VM_CONTRACT;

    GetItfMarshalInfo(TypeHandle(m_pMT),
        m_fDispItf,
        m_ms,
        pInfo);
}

void MarshalInfo::GetItfMarshalInfo(TypeHandle th, BOOL fDispItf, MarshalScenario ms, ItfMarshalInfo *pInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pInfo));
        PRECONDITION(!th.IsNull());
        PRECONDITION(!th.IsTypeDesc());
    }
    CONTRACTL_END;

#ifdef FEATURE_COMINTEROP

    // Initialize the output parameter.
    pInfo->dwFlags = 0;
    pInfo->thItf = TypeHandle();
    pInfo->thClass = TypeHandle();

    if (!th.IsInterface())
    {
        // If the parameter is not System.Object.
        if (!th.IsObjectType())
        {
            // Set the class method table.
            pInfo->thClass = th;

            TypeHandle hndDefItfClass;
            DefaultInterfaceType DefItfType;

            DefItfType = GetDefaultInterfaceForClassWrapper(th, &hndDefItfClass);
            switch (DefItfType)
            {
                case DefaultInterfaceType_Explicit:
                {
                    pInfo->thItf = hndDefItfClass;
                    switch (hndDefItfClass.GetComInterfaceType())
                    {
                        case ifDispatch:
                        case ifDual:
                            pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_DISP_ITF;
                            break;
                    }
                    break;
                }

                case DefaultInterfaceType_AutoDual:
                {
                    pInfo->thItf = hndDefItfClass;
                    pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_DISP_ITF;
                    break;
                }

                case DefaultInterfaceType_IUnknown:
                case DefaultInterfaceType_BaseComClass:
                {
                    break;
                }

                case DefaultInterfaceType_AutoDispatch:
                {
                    pInfo->thItf = hndDefItfClass;
                    pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_DISP_ITF;
                    break;
                }

                default:
                {
                    _ASSERTE(!"Invalid default interface type!");
                    break;
                }
            }
        }
        else
        {
            // The type will be marshalled as an IUnknown or IDispatch pointer depending
            // on the value of fDispItf and fInspItf
            if (fDispItf)
            {
                pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_DISP_ITF;
            }

            pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_USE_BASIC_ITF;
        }
    }
    else
    {
        // Determine the interface this type will be marshalled as.
        if (th.IsComClassInterface())
            pInfo->thItf = th.GetDefItfForComClassItf();
        else
            pInfo->thItf = th;

        // Determine if we are dealing with an IDispatch or IUnknown based interface.
        switch (pInfo->thItf.GetComInterfaceType())
        {
            case ifDispatch:
            case ifDual:
                pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_DISP_ITF;
                break;
        }

        // Look to see if the interface has a coclass defined
        pInfo->thClass = th.GetCoClassForInterface();
        if (!pInfo->thClass.IsNull())
        {
            pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_CLASS_IS_HINT;
        }
    }

    // store the pre-redirection interface type as thNativeItf
    pInfo->thNativeItf = pInfo->thItf;

#else // FEATURE_COMINTEROP
    if (!th.IsInterface())
        pInfo->thClass = th;
    else
        pInfo->thItf = th;
#endif // FEATURE_COMINTEROP
}

#ifdef _DEBUG
VOID MarshalInfo::DumpMarshalInfo(Module* pModule, SigPointer sig, const SigTypeContext *pTypeContext, mdToken token,
                                  MarshalScenario ms, CorNativeLinkType nlType, CorNativeLinkFlags nlFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (LoggingOn(LF_MARSHALER, LL_INFO10))
    {
        SString logbuf;
        StackScratchBuffer scratch;

        IMDInternalImport *pInternalImport = pModule->GetMDImport();

        logbuf.AppendASCII("------------------------------------------------------------\n");
        LOG((LF_MARSHALER, LL_INFO10, logbuf.GetANSI(scratch)));
        logbuf.Clear();

        logbuf.AppendASCII("Managed type: ");
        if (m_byref)
            logbuf.AppendASCII("Byref ");

        TypeHandle th = sig.GetTypeHandleNT(pModule, pTypeContext);
        if (th.IsNull())
            logbuf.AppendASCII("<error>");
        else
        {
            SigFormat sigfmt;
            sigfmt.AddType(th);
            logbuf.AppendUTF8(sigfmt.GetCString());
        }

        logbuf.AppendASCII("\n");
        LOG((LF_MARSHALER, LL_INFO10, logbuf.GetANSI(scratch)));
        logbuf.Clear();

        logbuf.AppendASCII("NativeType  : ");
        PCCOR_SIGNATURE pvNativeType;
        ULONG           cbNativeType;
        if (token == mdParamDefNil
            || pInternalImport->GetFieldMarshal(token,
                                                 &pvNativeType,
                                                 &cbNativeType) != S_OK)
        {
            logbuf.AppendASCII("<absent>");
        }
        else
        {

            while (cbNativeType--)
            {
                char num[100];
                sprintf_s(num, COUNTOF(num), "0x%lx ", (ULONG)*pvNativeType);
                logbuf.AppendASCII(num);
                switch (*(pvNativeType++))
                {
#define XXXXX(nt) case nt: logbuf.AppendASCII("(" #nt ")"); break;

                    XXXXX(NATIVE_TYPE_BOOLEAN)
                    XXXXX(NATIVE_TYPE_I1)

                    XXXXX(NATIVE_TYPE_U1)
                    XXXXX(NATIVE_TYPE_I2)
                    XXXXX(NATIVE_TYPE_U2)
                    XXXXX(NATIVE_TYPE_I4)

                    XXXXX(NATIVE_TYPE_U4)
                    XXXXX(NATIVE_TYPE_I8)
                    XXXXX(NATIVE_TYPE_U8)
                    XXXXX(NATIVE_TYPE_R4)

                    XXXXX(NATIVE_TYPE_R8)

                    XXXXX(NATIVE_TYPE_LPSTR)
                    XXXXX(NATIVE_TYPE_LPWSTR)
                    XXXXX(NATIVE_TYPE_LPTSTR)
                    XXXXX(NATIVE_TYPE_FIXEDSYSSTRING)

                    XXXXX(NATIVE_TYPE_STRUCT)

                    XXXXX(NATIVE_TYPE_INT)
                    XXXXX(NATIVE_TYPE_FIXEDARRAY)

                    XXXXX(NATIVE_TYPE_UINT)

                    XXXXX(NATIVE_TYPE_FUNC)

                    XXXXX(NATIVE_TYPE_ASANY)

                    XXXXX(NATIVE_TYPE_ARRAY)
                    XXXXX(NATIVE_TYPE_LPSTRUCT)

                    XXXXX(NATIVE_TYPE_IUNKNOWN)

                    XXXXX(NATIVE_TYPE_BSTR)
#ifdef FEATURE_COMINTEROP
                    XXXXX(NATIVE_TYPE_TBSTR)
                    XXXXX(NATIVE_TYPE_ANSIBSTR)
                    XXXXX(NATIVE_TYPE_BYVALSTR)

                    XXXXX(NATIVE_TYPE_VARIANTBOOL)
                    XXXXX(NATIVE_TYPE_SAFEARRAY)

                    XXXXX(NATIVE_TYPE_IDISPATCH)
                    XXXXX(NATIVE_TYPE_INTF)
#endif // FEATURE_COMINTEROP

#undef XXXXX


                    case NATIVE_TYPE_CUSTOMMARSHALER:
                    {
                        int strLen = 0;
                        logbuf.AppendASCII("(NATIVE_TYPE_CUSTOMMARSHALER)");

                        // Skip the typelib guid.
                        logbuf.AppendASCII(" ");

                        strLen = CPackedLen::GetLength(pvNativeType, (void const **)&pvNativeType);
                        if (strLen)
                        {
                            BYTE* p = (BYTE*)logbuf.OpenANSIBuffer(strLen);
                            memcpyNoGCRefs(p, pvNativeType, strLen);
                            logbuf.CloseBuffer();
                            logbuf.AppendASCII("\0");

                            pvNativeType += strLen;
                            cbNativeType -= strLen + 1;

                            // Skip the name of the native type.
                            logbuf.AppendASCII(" ");
                        }


                        strLen = CPackedLen::GetLength(pvNativeType, (void const **)&pvNativeType);
                        if (strLen)
                        {
                            BYTE* p = (BYTE*)logbuf.OpenANSIBuffer(strLen);
                            memcpyNoGCRefs(p, pvNativeType, strLen);
                            logbuf.CloseBuffer();
                            logbuf.AppendASCII("\0");

                            pvNativeType += strLen;
                            cbNativeType -= strLen + 1;

                            // Extract the name of the custom marshaler.
                            logbuf.AppendASCII(" ");
                        }


                        strLen = CPackedLen::GetLength(pvNativeType, (void const **)&pvNativeType);
                        if (strLen)
                        {
                            BYTE* p = (BYTE*)logbuf.OpenANSIBuffer(strLen);
                            memcpyNoGCRefs(p, pvNativeType, strLen);
                            logbuf.CloseBuffer();
                            logbuf.AppendASCII("\0");

                            pvNativeType += strLen;
                            cbNativeType -= strLen + 1;

                            // Extract the cookie string.
                            logbuf.AppendASCII(" ");
                        }

                        strLen = CPackedLen::GetLength(pvNativeType, (void const **)&pvNativeType);
                        if (strLen)
                        {
                            BYTE* p = (BYTE*)logbuf.OpenANSIBuffer(strLen);
                            memcpyNoGCRefs(p, pvNativeType, strLen);
                            logbuf.CloseBuffer();
                            logbuf.AppendASCII("\0");

                            pvNativeType += strLen;
                            cbNativeType -= strLen + 1;
                        }

                        break;
                    }

                    default:
                        logbuf.AppendASCII("(?)");
                }

                logbuf.AppendASCII("   ");
            }
        }
        logbuf.AppendASCII("\n");
        LOG((LF_MARSHALER, LL_INFO10, logbuf.GetANSI(scratch)));
        logbuf.Clear();

        logbuf.AppendASCII("MarshalType : ");
        {
            char num[100];
            sprintf_s(num, COUNTOF(num), "0x%lx ", (ULONG)m_type);
            logbuf.AppendASCII(num);
        }
        switch (m_type)
        {
            #define DEFINE_MARSHALER_TYPE(mt, mc) case mt: logbuf.AppendASCII( #mt " (IL" #mc ")"); break;
            #include "mtypes.h"

            case MARSHAL_TYPE_UNKNOWN:
                logbuf.AppendASCII("MARSHAL_TYPE_UNKNOWN (illegal combination)");
                break;

            default:
                logbuf.AppendASCII("MARSHAL_TYPE_???");
                break;
        }

        logbuf.AppendASCII("\n");


        logbuf.AppendASCII("Metadata In/Out     : ");
        if (TypeFromToken(token) != mdtParamDef || token == mdParamDefNil)
            logbuf.AppendASCII("<absent>");

        else
        {
            DWORD dwAttr = 0;
            USHORT usSequence;
            LPCSTR szParamName_Ignore;
            if (FAILED(pInternalImport->GetParamDefProps(token, &usSequence, &dwAttr, &szParamName_Ignore)))
            {
                logbuf.AppendASCII("Invalid ParamDef record ");
            }
            else
            {
                if (IsPdIn(dwAttr))
                    logbuf.AppendASCII("In ");

                if (IsPdOut(dwAttr))
                    logbuf.AppendASCII("Out ");
            }
        }

        logbuf.AppendASCII("\n");

        logbuf.AppendASCII("Effective In/Out     : ");
        if (m_in)
            logbuf.AppendASCII("In ");

        if (m_out)
            logbuf.AppendASCII("Out ");

        logbuf.AppendASCII("\n");

        LOG((LF_MARSHALER, LL_INFO10, logbuf.GetANSI(scratch)));
        logbuf.Clear();
    }
} // MarshalInfo::DumpMarshalInfo
#endif //_DEBUG

#if defined(FEATURE_COMINTEROP) && !defined(CROSSGEN_COMPILE)
DispParamMarshaler *MarshalInfo::GenerateDispParamMarshaler()
{
    CONTRACT (DispParamMarshaler*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    NewHolder<DispParamMarshaler> pDispParamMarshaler = NULL;

    switch (m_type)
    {
        case MARSHAL_TYPE_OLECOLOR:
            pDispParamMarshaler = new DispParamOleColorMarshaler();
            break;

        case MARSHAL_TYPE_CURRENCY:
            pDispParamMarshaler = new DispParamCurrencyMarshaler();
            break;

        case MARSHAL_TYPE_GENERIC_4:
            if (m_fErrorNativeType)
                pDispParamMarshaler = new DispParamErrorMarshaler();
            break;

        case MARSHAL_TYPE_INTERFACE:
        {
            ItfMarshalInfo itfInfo;
            GetItfMarshalInfo(TypeHandle(m_pMT), m_fDispItf, m_ms, &itfInfo);
            pDispParamMarshaler = new DispParamInterfaceMarshaler(
                itfInfo.dwFlags & ItfMarshalInfo::ITF_MARSHAL_DISP_ITF,
                itfInfo.thItf.GetMethodTable(),
                itfInfo.thClass.GetMethodTable(),
                itfInfo.dwFlags & ItfMarshalInfo::ITF_MARSHAL_CLASS_IS_HINT);
            break;
        }

        case MARSHAL_TYPE_VALUECLASS:
        case MARSHAL_TYPE_BLITTABLEVALUECLASS:
        case MARSHAL_TYPE_BLITTABLEPTR:
        case MARSHAL_TYPE_LAYOUTCLASSPTR:
        case MARSHAL_TYPE_BLITTABLEVALUECLASSWITHCOPYCTOR:
            pDispParamMarshaler = new DispParamRecordMarshaler(m_pMT);
            break;

        case MARSHAL_TYPE_SAFEARRAY:
            pDispParamMarshaler = new DispParamArrayMarshaler(m_arrayElementType, m_hndArrayElemType.GetMethodTable());
            break;

        case MARSHAL_TYPE_DELEGATE:
            pDispParamMarshaler = new DispParamDelegateMarshaler(m_pMT);
            break;

        case MARSHAL_TYPE_REFERENCECUSTOMMARSHALER:
            pDispParamMarshaler = new DispParamCustomMarshaler(m_pCMHelper, m_CMVt);
            break;
    }

    pDispParamMarshaler.SuppressRelease();
    RETURN pDispParamMarshaler;
}

DispatchWrapperType MarshalInfo::GetDispWrapperType()
{
    STANDARD_VM_CONTRACT;

    DispatchWrapperType WrapperType = (DispatchWrapperType)0;

    switch (m_type)
    {
        case MARSHAL_TYPE_CURRENCY:
            WrapperType = DispatchWrapperType_Currency;
            break;

        case MARSHAL_TYPE_BSTR:
            WrapperType = DispatchWrapperType_BStr;
            break;

        case MARSHAL_TYPE_GENERIC_4:
            if (m_fErrorNativeType)
                WrapperType = DispatchWrapperType_Error;
            break;

        case MARSHAL_TYPE_INTERFACE:
        {
            ItfMarshalInfo itfInfo;
            GetItfMarshalInfo(TypeHandle(m_pMT), m_fDispItf, m_ms, &itfInfo);
            WrapperType = !!(itfInfo.dwFlags & ItfMarshalInfo::ITF_MARSHAL_DISP_ITF) ? DispatchWrapperType_Dispatch : DispatchWrapperType_Unknown;
            break;
        }

        case MARSHAL_TYPE_SAFEARRAY:
            switch (m_arrayElementType)
            {
                case VT_CY:
                    WrapperType = (DispatchWrapperType)(DispatchWrapperType_SafeArray | DispatchWrapperType_Currency);
                    break;
                case VT_UNKNOWN:
                    WrapperType = (DispatchWrapperType)(DispatchWrapperType_SafeArray | DispatchWrapperType_Unknown);
                    break;
                case VT_DISPATCH:
                    WrapperType = (DispatchWrapperType)(DispatchWrapperType_SafeArray | DispatchWrapperType_Dispatch);
                    break;
                case VT_ERROR:
                    WrapperType = (DispatchWrapperType)(DispatchWrapperType_SafeArray | DispatchWrapperType_Error);
                    break;
                case VT_BSTR:
                    WrapperType = (DispatchWrapperType)(DispatchWrapperType_SafeArray | DispatchWrapperType_BStr);
                    break;
            }
            break;
    }

    return WrapperType;
}

#endif // defined(FEATURE_COMINTEROP) && !defined(CROSSGEN_COMPILE)

// Returns true if the marshaler represented by this instance requires COM to have been started.
bool MarshalInfo::MarshalerRequiresCOM()
{
    LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_COMINTEROP
    switch (m_type)
    {
        case MARSHAL_TYPE_REFERENCECUSTOMMARSHALER:

        case MARSHAL_TYPE_BSTR:
        case MARSHAL_TYPE_ANSIBSTR:
        case MARSHAL_TYPE_OBJECT:
        case MARSHAL_TYPE_OLECOLOR:
        case MARSHAL_TYPE_SAFEARRAY:
        case MARSHAL_TYPE_INTERFACE:
        {
            // some of these types do not strictly require COM for the actual marshaling
            // but they tend to be used in COM context so we keep the logic we had in
            // previous versions and return true here
            return true;
        }

        case MARSHAL_TYPE_LAYOUTCLASSPTR:
        case MARSHAL_TYPE_VALUECLASS:
        {
            // pessimistic guess, but in line with previous versions
            return true;
        }

        case MARSHAL_TYPE_NATIVEARRAY:
        {
            return (m_arrayElementType == VT_UNKNOWN ||
                    m_arrayElementType == VT_DISPATCH ||
                    m_arrayElementType == VT_VARIANT);
        }
    }
#endif // FEATURE_COMINTEROP

    return false;
}

#define ReportInvalidArrayMarshalInfo(resId)    \
    do                                          \
    {                                           \
        m_vtElement = VT_EMPTY;                 \
        m_errorResourceId = resId;              \
        m_thElement = TypeHandle();             \
        goto LExit;                             \
    }                                           \
    while (0)

void ArrayMarshalInfo::InitForNativeArray(MarshalInfo::MarshalScenario ms, TypeHandle thElement, CorNativeType ntElement, BOOL isAnsi)
{
    WRAPPER_NO_CONTRACT;
    InitElementInfo(NATIVE_TYPE_ARRAY, ms, thElement, ntElement, isAnsi);
}

void ArrayMarshalInfo::InitForFixedArray(TypeHandle thElement, CorNativeType ntElement, BOOL isAnsi)
{
    WRAPPER_NO_CONTRACT;
    InitElementInfo(NATIVE_TYPE_FIXEDARRAY, MarshalInfo::MARSHAL_SCENARIO_FIELD, thElement, ntElement, isAnsi);
}

#ifdef FEATURE_COMINTEROP
void ArrayMarshalInfo::InitForSafeArray(MarshalInfo::MarshalScenario ms, TypeHandle thElement, VARTYPE vtElement, BOOL isAnsi)
{
    STANDARD_VM_CONTRACT;

    InitElementInfo(NATIVE_TYPE_SAFEARRAY, ms, thElement, NATIVE_TYPE_DEFAULT, isAnsi);

    if (IsValid() && vtElement != VT_EMPTY)
    {
        if (vtElement == VT_USERDEFINED)
        {
            // If the user explicitly sets the VARTYPE to VT_USERDEFINED, we simply ignore it
            // since the exporter will take care of transforming the vt to VT_USERDEFINED and the
            // marshallers needs the actual type.
        }
        else
        {
            m_flags = (ArrayMarshalInfoFlags)(m_flags | amiSafeArraySubTypeExplicitlySpecified);
            m_vtElement = vtElement;
        }
    }
}
#endif // FEATURE_COMINTEROP

void ArrayMarshalInfo::InitElementInfo(CorNativeType arrayNativeType, MarshalInfo::MarshalScenario ms, TypeHandle thElement, CorNativeType ntElement, BOOL isAnsi)
{
    CONTRACT_VOID
    {
        STANDARD_VM_CHECK;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(!thElement.IsNull());
        POSTCONDITION(!IsValid() || !m_thElement.IsNull());
    }
    CONTRACT_END;

    CorElementType etElement = ELEMENT_TYPE_END;

    //
    // IMPORTANT: The error resource IDs used in this function must not contain any placeholders!
    //
    // Also please maintain the standard of using IDS_EE_BADMARSHAL_XXX when defining new error
    // message resource IDs.
    //

    if (thElement.IsArray())
        ReportInvalidArrayMarshalInfo(IDS_EE_BADMARSHAL_NESTEDARRAY);

    m_thElement = thElement;

    if (m_thElement.IsPointer())
    {
        m_flags = (ArrayMarshalInfoFlags)(m_flags | amiIsPtr);
        m_thElement = ((ParamTypeDesc*)m_thElement.AsTypeDesc())->GetModifiedType();
    }

    etElement = m_thElement.GetSignatureCorElementType();

    if (IsAMIPtr(m_flags) && (etElement > ELEMENT_TYPE_R8))
    {
        ReportInvalidArrayMarshalInfo(IDS_EE_BADMARSHAL_UNSUPPORTED_SIG);
    }

    if (etElement == ELEMENT_TYPE_CHAR)
    {
        switch (ntElement)
        {
            case NATIVE_TYPE_I1: //fallthru
            case NATIVE_TYPE_U1:
                m_vtElement = VTHACK_ANSICHAR;
                break;

            case NATIVE_TYPE_I2: //fallthru
            case NATIVE_TYPE_U2:
                m_vtElement = VT_UI2;
                break;

            // Compat: If the native type doesn't make sense, we need to ignore it and not report an error.
            case NATIVE_TYPE_DEFAULT: //fallthru
            default:
#ifdef FEATURE_COMINTEROP
                if (ms == MarshalInfo::MARSHAL_SCENARIO_COMINTEROP)
                    m_vtElement = VT_UI2;
                else
#endif // FEATURE_COMINTEROP
                    m_vtElement = isAnsi ? VTHACK_ANSICHAR : VT_UI2;
        }
    }
    else if (etElement == ELEMENT_TYPE_BOOLEAN)
    {
        switch (ntElement)
        {
            case NATIVE_TYPE_BOOLEAN:
                m_vtElement = VTHACK_WINBOOL;
                break;

#ifdef FEATURE_COMINTEROP
            case NATIVE_TYPE_VARIANTBOOL:
                m_vtElement = VT_BOOL;
                break;
#endif // FEATURE_COMINTEROP

            case NATIVE_TYPE_I1 :
            case NATIVE_TYPE_U1 :
                m_vtElement = VTHACK_CBOOL;
                break;

            // Compat: if the native type doesn't make sense, we need to ignore it and not report an error.
            case NATIVE_TYPE_DEFAULT: //fallthru
            default:
#ifdef FEATURE_COMINTEROP
                if (ms == MarshalInfo::MARSHAL_SCENARIO_COMINTEROP ||
                    arrayNativeType == NATIVE_TYPE_SAFEARRAY)
                {
                    m_vtElement = VT_BOOL;
                }
                else
#endif // FEATURE_COMINTEROP
                {
                    m_vtElement = VTHACK_WINBOOL;
                }
                break;
        }
    }
    else if (etElement == ELEMENT_TYPE_I)
    {
        m_vtElement = static_cast<VARTYPE>((GetPointerSize() == 4) ? VT_I4 : VT_I8);
    }
    else if (etElement == ELEMENT_TYPE_U)
    {
        m_vtElement = static_cast<VARTYPE>((GetPointerSize() == 4) ? VT_UI4 : VT_UI8);
    }
    else if (etElement <= ELEMENT_TYPE_R8)
    {
        static const BYTE map [] =
        {
            VT_NULL,    // ELEMENT_TYPE_END
            VT_VOID,    // ELEMENT_TYPE_VOID
            VT_NULL,    // ELEMENT_TYPE_BOOLEAN
            VT_NULL,    // ELEMENT_TYPE_CHAR
            VT_I1,      // ELEMENT_TYPE_I1
            VT_UI1,     // ELEMENT_TYPE_U1
            VT_I2,      // ELEMENT_TYPE_I2
            VT_UI2,     // ELEMENT_TYPE_U2
            VT_I4,      // ELEMENT_TYPE_I4
            VT_UI4,     // ELEMENT_TYPE_U4
            VT_I8,      // ELEMENT_TYPE_I8
            VT_UI8,     // ELEMENT_TYPE_U8
            VT_R4,      // ELEMENT_TYPE_R4
            VT_R8       // ELEMENT_TYPE_R8

        };

        _ASSERTE(map[etElement] != VT_NULL);
        m_vtElement = map[etElement];
    }
    else
    {
        if (m_thElement == TypeHandle(g_pStringClass))
        {
            switch (ntElement)
            {
                case NATIVE_TYPE_DEFAULT:
#ifdef FEATURE_COMINTEROP
                    if (arrayNativeType == NATIVE_TYPE_SAFEARRAY || ms == MarshalInfo::MARSHAL_SCENARIO_COMINTEROP)
                    {
                        m_vtElement = VT_BSTR;
                    }
                    else
#endif // FEATURE_COMINTEROP
                    {
                        m_vtElement = static_cast<VARTYPE>(isAnsi ? VT_LPSTR : VT_LPWSTR);
                    }
                    break;
                case NATIVE_TYPE_BSTR:
                    m_vtElement = VT_BSTR;
                    break;
                case NATIVE_TYPE_LPSTR:
                    m_vtElement = VT_LPSTR;
                    break;
                case NATIVE_TYPE_LPWSTR:
                    m_vtElement = VT_LPWSTR;
                    break;
                case NATIVE_TYPE_LPTSTR:
                {
#ifdef FEATURE_COMINTEROP
                    if (ms == MarshalInfo::MARSHAL_SCENARIO_COMINTEROP)
                    {
                        // We disallow NATIVE_TYPE_LPTSTR for COM or if we are exporting.
                        ReportInvalidArrayMarshalInfo(IDS_EE_BADMARSHALPARAM_NO_LPTSTR);
                    }
                    else
#endif // FEATURE_COMINTEROP
                    {
                        // We no longer support Win9x so LPTSTR always maps to a Unicode string.
                        m_vtElement = VT_LPWSTR;
                    }
                    break;
                }

                default:
                    ReportInvalidArrayMarshalInfo(IDS_EE_BADMARSHAL_STRINGARRAY);
            }
        }
        else if (m_thElement == TypeHandle(g_pObjectClass))
        {
#ifdef FEATURE_COMINTEROP
            switch(ntElement)
            {
                case NATIVE_TYPE_DEFAULT:
                    if (ms == MarshalInfo::MARSHAL_SCENARIO_FIELD)
                        m_vtElement = VT_UNKNOWN;
                    else
                        m_vtElement = VT_VARIANT;
                    break;

                case NATIVE_TYPE_STRUCT:
                    m_vtElement = VT_VARIANT;
                    break;

                case NATIVE_TYPE_INTF:
                case NATIVE_TYPE_IUNKNOWN:
                    m_vtElement = VT_UNKNOWN;
                    break;

                case NATIVE_TYPE_IDISPATCH:
                    m_vtElement = VT_DISPATCH;
                    break;

                default:
                    ReportInvalidArrayMarshalInfo(IDS_EE_BADMARSHAL_OBJECTARRAY);
            }

#else // FEATURE_COMINTEROP
            switch (ntElement)
            {
                case NATIVE_TYPE_IUNKNOWN:
                    m_vtElement = VT_UNKNOWN;
                    break;

                default:
                    ReportInvalidArrayMarshalInfo(IDS_EE_BADMARSHAL_UNSUPPORTED_SIG);
            }
#endif // FEATURE_COMINTEROP
        }
        else if (m_thElement.CanCastTo(TypeHandle(CoreLibBinder::GetClass(CLASS__SAFE_HANDLE))))
        {
            // Array's of SAFEHANDLEs are not supported.
            ReportInvalidArrayMarshalInfo(IDS_EE_BADMARSHAL_SAFEHANDLEARRAY);
        }
        else if (m_thElement.CanCastTo(TypeHandle(CoreLibBinder::GetClass(CLASS__CRITICAL_HANDLE))))
        {
            // Array's of CRITICALHANDLEs are not supported.
            ReportInvalidArrayMarshalInfo(IDS_EE_BADMARSHAL_CRITICALHANDLEARRAY);
        }
        else if (etElement == ELEMENT_TYPE_VALUETYPE)
        {
            if (m_thElement == TypeHandle(CoreLibBinder::GetClass(CLASS__DATE_TIME)))
            {
                if (ntElement == NATIVE_TYPE_STRUCT || ntElement == NATIVE_TYPE_DEFAULT)
                    m_vtElement = VT_DATE;
                else
                    ReportInvalidArrayMarshalInfo(IDS_EE_BADMARSHAL_DATETIMEARRAY);
            }
            else if (m_thElement == TypeHandle(CoreLibBinder::GetClass(CLASS__DECIMAL)))
            {
                if (ntElement == NATIVE_TYPE_STRUCT || ntElement == NATIVE_TYPE_DEFAULT)
                    m_vtElement = VT_DECIMAL;
#ifdef FEATURE_COMINTEROP
                else if (ntElement == NATIVE_TYPE_CURRENCY)
                    m_vtElement = VT_CY;
#endif // FEATURE_COMINTEROP
                else
                    ReportInvalidArrayMarshalInfo(IDS_EE_BADMARSHAL_DECIMALARRAY);
            }
            else
            {
                m_vtElement = OleVariant::GetVarTypeForTypeHandle(m_thElement);
            }
        }
#ifdef FEATURE_COMINTEROP
        else if (m_thElement == TypeHandle(CoreLibBinder::GetClass(CLASS__ERROR_WRAPPER)))
        {
            m_vtElement = VT_ERROR;
        }
#endif
        else
        {
#ifdef FEATURE_COMINTEROP

            // Compat: Even if the classes have layout, we still convert them to interface pointers.

            ItfMarshalInfo itfInfo;
            MarshalInfo::GetItfMarshalInfo(m_thElement, FALSE, ms, &itfInfo);

            // Compat: We must always do VT_UNKNOWN marshaling for parameters, even if the interface is marked late-bound.
            if (ms == MarshalInfo::MARSHAL_SCENARIO_FIELD)
                m_vtElement = static_cast<VARTYPE>(!!(itfInfo.dwFlags & ItfMarshalInfo::ITF_MARSHAL_DISP_ITF) ? VT_DISPATCH : VT_UNKNOWN);
            else
                m_vtElement = VT_UNKNOWN;

            m_thElement = itfInfo.thItf.IsNull() ? TypeHandle(g_pObjectClass) : itfInfo.thItf;
            m_thInterfaceArrayElementClass = itfInfo.thClass;

#else // FEATURE_COMINTEROP
            ReportInvalidArrayMarshalInfo(IDS_EE_BADMARSHAL_UNSUPPORTED_SIG);
#endif // FEATURE_COMINTEROP
        }
    }

LExit:;

    RETURN;
}

bool IsUnsupportedTypedrefReturn(MetaSig& msig)
{
    WRAPPER_NO_CONTRACT;

    return msig.GetReturnTypeNormalized() == ELEMENT_TYPE_TYPEDBYREF;
}

#ifndef CROSSGEN_COMPILE

#include "stubhelpers.h"
FCIMPL3(void*, StubHelpers::CreateCustomMarshalerHelper,
            MethodDesc* pMD,
            mdToken paramToken,
            TypeHandle hndManagedType)
{
    FCALL_CONTRACT;

    CustomMarshalerHelper* pCMHelper = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    Module* pModule = pMD->GetModule();
    Assembly* pAssembly = pModule->GetAssembly();


#ifdef FEATURE_COMINTEROP
    if (!hndManagedType.IsTypeDesc() &&
        IsTypeRefOrDef(g_CollectionsEnumeratorClassName, hndManagedType.GetModule(), hndManagedType.GetCl()))
    {
        pCMHelper = SetupCustomMarshalerHelper(ENUMERATOR_TO_ENUM_VARIANT_CM_NAME,
                                               ENUMERATOR_TO_ENUM_VARIANT_CM_NAME_LEN,
                                               ENUMERATOR_TO_ENUM_VARIANT_CM_COOKIE,
                                               ENUMERATOR_TO_ENUM_VARIANT_CM_COOKIE_LEN,
                                               pAssembly, hndManagedType);
    }
    else
#endif // FEATURE_COMINTEROP
    {
        //
        // Retrieve the native type for the current parameter.
        //

        BOOL result;
        NativeTypeParamInfo ParamInfo;
        result = ParseNativeTypeInfo(paramToken, pModule->GetMDImport(), &ParamInfo);

        //
        // this should all have been done at stub creation time
        //
        CONSISTENCY_CHECK(result != 0);
        CONSISTENCY_CHECK(ParamInfo.m_NativeType == NATIVE_TYPE_CUSTOMMARSHALER);

        // Set up the custom marshaler info.
        pCMHelper = SetupCustomMarshalerHelper(ParamInfo.m_strCMMarshalerTypeName,
                                                ParamInfo.m_cCMMarshalerTypeNameBytes,
                                                ParamInfo.m_strCMCookie,
                                                ParamInfo.m_cCMCookieStrBytes,
                                                pAssembly,
                                                hndManagedType);
    }

    HELPER_METHOD_FRAME_END();

    return (void*)pCMHelper;
}
FCIMPLEND

#endif // CROSSGEN_COMPILE
