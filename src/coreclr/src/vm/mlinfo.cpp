// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
#include "winrttypenameconverter.h"
#endif // FEATURE_COMINTEROP


#ifndef lengthof
    #define lengthof(rg)    (sizeof(rg)/sizeof(rg[0]))
#endif


#ifdef FEATURE_COMINTEROP
    DEFINE_ASM_QUAL_TYPE_NAME(ENUMERATOR_TO_ENUM_VARIANT_CM_NAME, g_EnumeratorToEnumClassName, g_CorelibAsmName, VER_ASSEMBLYVERSION_STR, g_ECMAKeyToken);

    static const int        ENUMERATOR_TO_ENUM_VARIANT_CM_NAME_LEN    = lengthof(ENUMERATOR_TO_ENUM_VARIANT_CM_NAME);
    static const char       ENUMERATOR_TO_ENUM_VARIANT_CM_COOKIE[]    = {""};
    static const int        ENUMERATOR_TO_ENUM_VARIANT_CM_COOKIE_LEN  = lengthof(ENUMERATOR_TO_ENUM_VARIANT_CM_COOKIE);

    DEFINE_ASM_QUAL_TYPE_NAME(COLOR_TRANSLATOR_ASM_QUAL_TYPE_NAME, g_ColorTranslatorClassName, g_DrawingAsmName, VER_ASSEMBLYVERSION_STR, g_FXKeyToken);
    DEFINE_ASM_QUAL_TYPE_NAME(COLOR_ASM_QUAL_TYPE_NAME, g_ColorClassName, g_DrawingAsmName, VER_ASSEMBLYVERSION_STR, g_FXKeyToken);

    DEFINE_ASM_QUAL_TYPE_NAME(URI_ASM_QUAL_TYPE_NAME, g_SystemUriClassName, g_SystemRuntimeAsmName, VER_ASSEMBLYVERSION_STR, g_FXKeyToken);

    DEFINE_ASM_QUAL_TYPE_NAME(NCCEVENTARGS_ASM_QUAL_TYPE_NAME, g_NotifyCollectionChangedEventArgsName, g_ObjectModelAsmName, VER_ASSEMBLYVERSION_STR, g_FXKeyToken);
    DEFINE_ASM_QUAL_TYPE_NAME(NCCEVENTARGS_MARSHALER_ASM_QUAL_TYPE_NAME, g_NotifyCollectionChangedEventArgsMarshalerName, g_SystemRuntimeWindowsRuntimeAsmName, VER_ASSEMBLYVERSION_STR, g_ECMAKeyToken);


    DEFINE_ASM_QUAL_TYPE_NAME(PCEVENTARGS_ASM_QUAL_TYPE_NAME, g_PropertyChangedEventArgsName, g_ObjectModelAsmName, VER_ASSEMBLYVERSION_STR, g_FXKeyToken);
    DEFINE_ASM_QUAL_TYPE_NAME(PCEVENTARGS_MARSHALER_ASM_QUAL_TYPE_NAME, g_PropertyChangedEventArgsMarshalerName, g_SystemRuntimeWindowsRuntimeAsmName, VER_ASSEMBLYVERSION_STR, g_ECMAKeyToken);


    #define OLECOLOR_TO_SYSTEMCOLOR_METH_NAME   "FromOle"
    #define SYSTEMCOLOR_TO_OLECOLOR_METH_NAME   "ToOle"

    #define EVENTARGS_TO_WINRT_EVENTARGS_METH_NAME  "ConvertToNative"
    #define WINRT_EVENTARGS_TO_EVENTARGS_METH_NAME  "ConvertToManaged"

    #define ORIGINALSTRING_PROPERTY_NAME        "OriginalString"
#endif // FEATURE_COMINTEROP



#define INITIAL_NUM_CMHELPER_HASHTABLE_BUCKETS 32
#define INITIAL_NUM_CMINFO_HASHTABLE_BUCKETS 32
#define DEBUG_CONTEXT_STR_LEN 2000


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

    if (token == mdParamDefNil || pScope->GetFieldMarshal(token, &pvNativeType, &cbNativeType) != S_OK)
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

//==========================================================================
// Determines whether *pManagedElemType is really normalized (i.e. differs
// from what sigPtr points to modulo generic instantiation). If it is the
// case, all types that have been normalized away are checked for valid
// managed/unmanaged type combination, and *pNativeType is updated to contain
// the native type of the primitive type field inside. On error (a generic
// type is encountered or managed/unmanaged type mismatch) or non-default
// native type of the primitive type inside, *pManagedElemType is un-normalized
// so that the calling code can deal with the situation in its own way.
//==========================================================================
void VerifyAndAdjustNormalizedType(
                         Module *                   pModule,
                         SigPointer                 sigPtr,
                         const SigTypeContext *     pTypeContext,
                         CorElementType *           pManagedElemType,
                         CorNativeType *            pNativeType)
{
    CorElementType sigElemType = sigPtr.PeekElemTypeClosed(pModule, pTypeContext);

    if (*pManagedElemType != sigElemType)
    {
        // Normalized element type differs from closed element type, which means that
        // normalization has occurred.
        _ASSERTE(sigElemType == ELEMENT_TYPE_VALUETYPE);

        // Now we know that this is a normalized value type - we have to verify the removed
        // value type(s) and get to the true primitive type inside.
        TypeHandle th = sigPtr.GetTypeHandleThrowing(pModule,
                                                     pTypeContext,
                                                     ClassLoader::LoadTypes,
                                                     CLASS_LOAD_UNRESTORED,
                                                     TRUE);
        _ASSERTE(!th.IsNull() && !th.IsTypeDesc());

        CorNativeType ntype = *pNativeType;

        if (!th.AsMethodTable()->IsTruePrimitive() &&
            !th.IsEnum())
        {
            // This is a trivial (yet non-primitive) value type that has been normalized.
            // Loop until we eventually hit the primitive type or enum inside.
            do
            {
                if (th.HasInstantiation())
                {
                    // generic structures are either not marshalable or special-cased - the caller needs to know either way
                    *pManagedElemType = sigElemType;
                    return;
                }

                // verify the native type of the value type (must be default or Struct)
                if (!(ntype == NATIVE_TYPE_DEFAULT || ntype == NATIVE_TYPE_STRUCT))
                {
                    *pManagedElemType = sigElemType;
                    return;
                }

                MethodTable *pMT = th.GetMethodTable();
                _ASSERTE(pMT != NULL && pMT->IsValueType() && pMT->GetNumInstanceFields() == 1);

                // get the only instance field
                PTR_FieldDesc fieldDesc = pMT->GetApproxFieldDescListRaw();

                // retrieve the MarshalAs of the field
                NativeTypeParamInfo paramInfo;
                if (!ParseNativeTypeInfo(fieldDesc->GetMemberDef(), th.GetModule()->GetMDImport(), &paramInfo))
                {
                    *pManagedElemType = sigElemType;
                    return;
                }

                ntype = paramInfo.m_NativeType;

                th = fieldDesc->GetApproxFieldTypeHandleThrowing();
            }
            while (!th.IsTypeDesc() &&
                   !th.AsMethodTable()->IsTruePrimitive() &&
                   !th.IsEnum());

            // now ntype contains the native type of *pManagedElemType
            if (ntype == NATIVE_TYPE_DEFAULT)
            {
                // Let's update the caller's native type with default type only.
                // Updating with a non-default native type that is not allowed
                // for the given managed type would result in confusing exception
                // messages.
                *pNativeType = ntype;
            }
            else
            {
                *pManagedElemType = sigElemType;
            }
        }
    }
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

void *EventArgsMarshalingInfo::operator new(size_t size, LoaderHeap *pHeap)
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

void EventArgsMarshalingInfo::operator delete(void *pMem)
{
    LIMITED_METHOD_CONTRACT;
    // Instances of this class are always allocated on the loader heap so
    // the delete operator has nothing to do.
}

EventArgsMarshalingInfo::EventArgsMarshalingInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Load the System.Collections.Specialized.NotifyCollectionChangedEventArgs class.
    SString qualifiedNCCEventArgsTypeName(SString::Utf8, NCCEVENTARGS_ASM_QUAL_TYPE_NAME);
    m_hndSystemNCCEventArgsType = TypeName::GetTypeFromAsmQualifiedName(qualifiedNCCEventArgsTypeName.GetUnicode());
    _ASSERTE(!m_hndSystemNCCEventArgsType.IsNull() && "Cannot load System.Collections.Specialized.NotifyCollectionChangedEventArgs!");

    // Load the System.ComponentModel.PropertyChangedEventArgs class.
    SString qualifiedPCEventArgsTypeName(SString::Utf8, PCEVENTARGS_ASM_QUAL_TYPE_NAME);
    m_hndSystemPCEventArgsType = TypeName::GetTypeFromAsmQualifiedName(qualifiedPCEventArgsTypeName.GetUnicode());
    _ASSERTE(!m_hndSystemPCEventArgsType.IsNull() && "Cannot load System.ComponentModel.PropertyChangedEventArgs!");

    // Load the NCCEventArgs marshaler class.
    SString qualifiedNCCEventArgsMarshalerTypeName(SString::Utf8, NCCEVENTARGS_MARSHALER_ASM_QUAL_TYPE_NAME);
    TypeHandle hndNCCEventArgsMarshalerType = TypeName::GetTypeFromAsmQualifiedName(qualifiedNCCEventArgsMarshalerTypeName.GetUnicode());

    // Retrieve the method to convert a .NET NCCEventArgs to a WinRT NCCEventArgs.
    m_pSystemNCCEventArgsToWinRTNCCEventArgsMD = MemberLoader::FindMethodByName(hndNCCEventArgsMarshalerType.GetMethodTable(), EVENTARGS_TO_WINRT_EVENTARGS_METH_NAME);
    _ASSERTE(m_pSystemNCCEventArgsToWinRTNCCEventArgsMD && "Unable to find the marshaler method to convert a .NET NCCEventArgs to a WinRT NCCEventArgs!");

    // Retrieve the method to convert a WinRT NCCEventArgs to a .NET NCCEventArgs.
    m_pWinRTNCCEventArgsToSystemNCCEventArgsMD = MemberLoader::FindMethodByName(hndNCCEventArgsMarshalerType.GetMethodTable(), WINRT_EVENTARGS_TO_EVENTARGS_METH_NAME);
    _ASSERTE(m_pWinRTNCCEventArgsToSystemNCCEventArgsMD && "Unable to find the marshaler method to convert a WinRT NCCEventArgs to a .NET NCCEventArgs!");

    // Load the PCEventArgs marshaler class.
    SString qualifiedPCEventArgsMarshalerTypeName(SString::Utf8, PCEVENTARGS_MARSHALER_ASM_QUAL_TYPE_NAME);
    TypeHandle hndPCEventArgsMarshalerType = TypeName::GetTypeFromAsmQualifiedName(qualifiedPCEventArgsMarshalerTypeName.GetUnicode());

    // Retrieve the method to convert a .NET PCEventArgs to a WinRT PCEventArgs.
    m_pSystemPCEventArgsToWinRTPCEventArgsMD = MemberLoader::FindMethodByName(hndPCEventArgsMarshalerType.GetMethodTable(), EVENTARGS_TO_WINRT_EVENTARGS_METH_NAME);
    _ASSERTE(m_pSystemPCEventArgsToWinRTPCEventArgsMD && "Unable to find the marshaler method to convert a .NET PCEventArgs to a WinRT PCEventArgs!");

    // Retrieve the method to convert a WinRT PCEventArgs to a .NET PCEventArgs.
    m_pWinRTPCEventArgsToSystemPCEventArgsMD = MemberLoader::FindMethodByName(hndPCEventArgsMarshalerType.GetMethodTable(), WINRT_EVENTARGS_TO_EVENTARGS_METH_NAME);
    _ASSERTE(m_pWinRTPCEventArgsToSystemPCEventArgsMD && "Unable to find the marshaler method to convert a WinRT PCEventArgs to a .NET PCEventArgs!");
}

EventArgsMarshalingInfo::~EventArgsMarshalingInfo()
{
   LIMITED_METHOD_CONTRACT;
}

void *UriMarshalingInfo::operator new(size_t size, LoaderHeap *pHeap)
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


void UriMarshalingInfo::operator delete(void *pMem)
{
    LIMITED_METHOD_CONTRACT;
    // Instances of this class are always allocated on the loader heap so
    // the delete operator has nothing to do.
}

UriMarshalingInfo::UriMarshalingInfo()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Create on-demand as we don't want to create the factories in NGEN time
    m_pUriFactory = NULL;

    // Load the System.Uri class.
    SString qualifiedUriTypeName(SString::Utf8, URI_ASM_QUAL_TYPE_NAME);
    m_hndSystemUriType = TypeName::GetTypeFromAsmQualifiedName(qualifiedUriTypeName.GetUnicode());
    _ASSERTE(!m_hndSystemUriType.IsNull() && "Cannot load System.Uri!");
    
    m_SystemUriOriginalStringGetterMD = MemberLoader::FindPropertyMethod(m_hndSystemUriType.GetMethodTable(), ORIGINALSTRING_PROPERTY_NAME, PropertyGet);
    _ASSERTE(m_SystemUriOriginalStringGetterMD && "Unable to find the System.Uri.get_OriginalString()!");
    _ASSERTE(!m_SystemUriOriginalStringGetterMD->IsStatic() && "System.Uri.get_OriginalString() is static!");

    // Windows.Foundation.Uri..ctor(string) and System.Uri..ctor(string)
    MethodTable* pSystemUriMT = m_hndSystemUriType.AsMethodTable();
    m_SystemUriCtorMD = MemberLoader::FindConstructor(pSystemUriMT, &gsig_IM_Str_RetVoid);
    _ASSERTE(m_SystemUriCtorMD && "Unable to find the constructor on System.Uri that takes a string!");
    _ASSERTE(m_SystemUriCtorMD->IsClassConstructorOrCtor() && !m_SystemUriCtorMD->IsStatic() && "The method retrieved from System.Uri is not a constructor!");
}

UriMarshalingInfo::~UriMarshalingInfo()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
#ifndef CROSSGEN_COMPILE
    if (m_pUriFactory)
    {
        SafeRelease(m_pUriFactory);
        m_pUriFactory = NULL;
    }
#endif
}

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
    
    if (m_pUriInfo)
    {
        delete m_pUriInfo;
        m_pUriInfo = NULL;
    }
    
    if (m_pEventArgsInfo)
    {
        delete m_pEventArgsInfo;
        m_pEventArgsInfo = NULL;
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
UriMarshalingInfo *EEMarshalingData::GetUriMarshalingInfo()
{
    CONTRACT (UriMarshalingInfo*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;
       
    if (m_pUriInfo == NULL)
    {
        UriMarshalingInfo *pUriInfo = new (m_pHeap) UriMarshalingInfo();

        if (InterlockedCompareExchangeT(&m_pUriInfo, pUriInfo, NULL) != NULL)
        {
            // Another thread beat us to it. Delete on UriMarshalingInfo is an empty operation
            // which is OK, since the possible leak is rare, small, and constant. This is the same
            // pattern as in code:GetCustomMarshalerInfo.
            delete pUriInfo;
        }
    }

    RETURN m_pUriInfo;
}

EventArgsMarshalingInfo *EEMarshalingData::GetEventArgsMarshalingInfo()
{
    CONTRACT (EventArgsMarshalingInfo*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;
       
    if (m_pEventArgsInfo == NULL)
    {
        EventArgsMarshalingInfo *pEventArgsInfo = new (m_pHeap) EventArgsMarshalingInfo();

        if (InterlockedCompareExchangeT(&m_pEventArgsInfo, pEventArgsInfo, NULL) != NULL)
        {
            // Another thread beat us to it. Delete on EventArgsMarshalingInfo is an empty operation
            // which is OK, since the possible leak is rare, small, and constant. This is the same
            // pattern as in code:GetCustomMarshalerInfo.
            delete pEventArgsInfo;
        }
    }

    RETURN m_pEventArgsInfo;
}

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
                         BOOL onInstanceMethod,
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
    m_fAnsi                         = (ms == MARSHAL_SCENARIO_NDIRECT) && (nlType == nltAnsi);
    m_managedArgSize                = 0;
    m_nativeArgSize                 = 0;
    m_pCMHelper                     = NULL;
    m_CMVt                          = VT_EMPTY;
    m_args.m_pMarshalInfo           = this;
    m_args.m_pMT                    = NULL;
    m_pModule                       = pModule;
    CorElementType mtype            = ELEMENT_TYPE_END;
    CorElementType corElemType      = ELEMENT_TYPE_END;
    m_pMT                           = NULL;
    m_pMD                           = pMD;

#ifdef FEATURE_COMINTEROP
    m_fDispItf                      = FALSE;
    m_fInspItf                      = FALSE;
    m_fErrorNativeType              = FALSE;
    m_hiddenLengthParamIndex        = (UINT16)-1;
    m_dwHiddenLengthManagedHomeLocal= 0xFFFFFFFF;
    m_dwHiddenLengthNativeHomeLocal = 0xFFFFFFFF;

    m_pDefaultItfMT                 = NULL;
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

#ifdef FEATURE_COMINTEROP
    if (IsWinRTScenario() && nativeType != NATIVE_TYPE_DEFAULT)
    {
        // Do not allow any MarshalAs in WinRT scenarios - marshaling is fully described by the parameter type.
        m_type = MARSHAL_TYPE_UNKNOWN; 
        m_resID = IDS_EE_BADMARSHAL_WINRT_MARSHAL_AS;
        IfFailGoto(E_FAIL, lFail);
    }
#endif // FEATURE_COMINTEROP

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
#ifdef FEATURE_COMINTEROP
        // WinRT does not support ET_PTR
        if (IsWinRTScenario())
        {
            m_type = MARSHAL_TYPE_UNKNOWN; 
            m_resID = IDS_EE_BADMARSHAL_WINRT_ILLEGAL_TYPE;
            IfFailGoto(E_FAIL, lFail);
        }
#endif // FEATURE_COMINTEROP

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
                // It should be blittable
                if (!th.IsBlittable())
                {
                    m_resID = IDS_EE_BADMARSHAL_PTRNONBLITTABLE;
                    IfFailGoto(E_FAIL, lFail);
                }

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
        else
        {
            if (!(mtype2 != ELEMENT_TYPE_CLASS &&
                  mtype2 != ELEMENT_TYPE_STRING &&
                  mtype2 != ELEMENT_TYPE_OBJECT &&
                  mtype2 != ELEMENT_TYPE_SZARRAY))
            {
                m_resID = IDS_EE_BADMARSHAL_PTRSUBTYPE;
                IfFailGoto(E_FAIL, lFail);
            }
        }
    }


    // System primitive types (System.Int32, et.al.) will be marshaled as expected
    // because the mtype CorElementType is normalized (e.g. ELEMENT_TYPE_I4).
#ifdef _TARGET_X86_
    // We however need to detect if such a normalization occurred for non-system
    // trivial value types, because we hold CorNativeType belonging to the original
    // "un-normalized" signature type. It has to be verified that all the value types
    // that have been normalized away have default marshaling or MarshalAs(Struct).
    // In addition, the nativeType must be updated with the type of the real primitive inside.
    // We don't normalize on return values of member functions since struct return values need to be treated as structures.
    if (isParam || !onInstanceMethod)
    {
        VerifyAndAdjustNormalizedType(pModule, sig, pTypeContext, &mtype, &nativeType);
    }
    else
    {
        SigPointer sigtmp = sig;
        CorElementType closedElemType = sigtmp.PeekElemTypeClosed(pModule, pTypeContext);
        if (closedElemType == ELEMENT_TYPE_VALUETYPE)
        {
            TypeHandle th = sigtmp.GetTypeHandleThrowing(pModule, pTypeContext); 
            // If the return type of an instance method is a value-type we need the actual return type.
            // However, if the return type is an enum, we can normalize it.
            if (!th.IsEnum())
            {
                mtype = closedElemType;
            }
        }

    }
#endif // _TARGET_X86_


    if (nativeType == NATIVE_TYPE_CUSTOMMARSHALER)
    {
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
                    else if (m_ms == MARSHAL_SCENARIO_WINRT)
                    {
                        // 1-byte WinRT bool
                        m_type = MARSHAL_TYPE_CBOOL;
                    }
                    else
#endif // FEATURE_COMINTEROP
                    {
                        // 4-byte Windows BOOL
                        _ASSERTE(m_ms == MARSHAL_SCENARIO_NDIRECT);
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
                    m_type = ( (m_ms == MARSHAL_SCENARIO_NDIRECT && m_fAnsi) ? MARSHAL_TYPE_ANSICHAR : MARSHAL_TYPE_GENERIC_U2 );
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

#ifdef FEATURE_COMINTEROP
                case NATIVE_TYPE_ERROR:
                    m_fErrorNativeType = TRUE;
                    break;
#endif // FEATURE_COMINTEROP

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

#ifdef FEATURE_COMINTEROP
                case NATIVE_TYPE_ERROR:
                    m_fErrorNativeType = TRUE;
                    break;
#endif // FEATURE_COMINTEROP

                default:
                m_resID = IDS_EE_BADMARSHAL_I4;
                IfFailGoto(E_FAIL, lFail);
            }
            m_type = MARSHAL_TYPE_GENERIC_4;
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
            m_type = (sizeof(LPVOID) == 4 ? MARSHAL_TYPE_GENERIC_4 : MARSHAL_TYPE_GENERIC_8);
            break;

        case ELEMENT_TYPE_U:

            if (!(nativeType == NATIVE_TYPE_UINT || nativeType == NATIVE_TYPE_INT || nativeType == NATIVE_TYPE_DEFAULT))
            {
                m_resID = IDS_EE_BADMARSHAL_I;
                IfFailGoto(E_FAIL, lFail);
            }
            m_type = (sizeof(LPVOID) == 4 ? MARSHAL_TYPE_GENERIC_4 : MARSHAL_TYPE_GENERIC_8);
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
#ifdef FEATURE_COMINTEROP
            _ASSERTE(!IsWinRTScenario()); // we checked for this earlier
#endif // FEATURE_COMINTEROP

            if (nativeType != NATIVE_TYPE_DEFAULT)
            {
                m_resID = IDS_EE_BADMARSHAL_PTR;
                IfFailGoto(E_FAIL, lFail);
            }
            m_type = ( (sizeof(void*)==4) ? MARSHAL_TYPE_GENERIC_4 : MARSHAL_TYPE_GENERIC_8 );
            break;

        case ELEMENT_TYPE_FNPTR:
#ifdef FEATURE_COMINTEROP
            if (IsWinRTScenario())
            {
                m_resID = IDS_EE_BADMARSHAL_WINRT_ILLEGAL_TYPE;
                IfFailGoto(E_FAIL, lFail);
            }
#endif // FEATURE_COMINTEROP

            if (!(nativeType == NATIVE_TYPE_FUNC || nativeType == NATIVE_TYPE_DEFAULT))
            {
                m_resID = IDS_EE_BADMARSHAL_FNPTR;
                IfFailGoto(E_FAIL, lFail);
            }
            m_type = ( (sizeof(void*)==4) ? MARSHAL_TYPE_GENERIC_4 : MARSHAL_TYPE_GENERIC_8 );
            break;

        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_VAR:
        {                
            TypeHandle sigTH = sig.GetTypeHandleThrowing(pModule, pTypeContext);

            // Disallow marshaling generic types except for WinRT interfaces.
            if (sigTH.HasInstantiation())
            {
#ifdef FEATURE_COMINTEROP
                if (!sigTH.SupportsGenericInterop(TypeHandle::Interop_NativeToManaged))
#endif // FEATURE_COMINTEROP
                {
                    m_resID = IDS_EE_BADMARSHAL_GENERICS_RESTRICTION;
                    IfFailGoto(E_FAIL, lFail);
                }
            }
            
            m_pMT = sigTH.GetMethodTable();
            if (m_pMT == NULL)
                IfFailGoto(COR_E_TYPELOAD, lFail);

#ifdef FEATURE_COMINTEROP
            MethodTable* pDefaultMT = NULL;

            // Look for marshaling of WinRT runtime classes
            if ((m_pMT->IsProjectedFromWinRT() || m_pMT->IsExportedToWinRT()) && !m_pMT->HasExplicitGuid())
            {
                // The type loader guarantees that there are no WinRT interfaces without explicit GUID
                _ASSERTE(!m_pMT->IsInterface());

                // Make sure that this is really a legal runtime class and not a custom attribute or delegate
                if (!m_pMT->IsLegalNonArrayWinRTType() || m_pMT->IsDelegate())
                {
                    m_resID = IDS_EE_BADMARSHAL_WINRT_ILLEGAL_TYPE;
                    IfFailGoto(E_FAIL, lFail);
                }

                // This class must have a default interface that describes how it is marshaled
                pDefaultMT = m_pMT->GetDefaultWinRTInterface();
                if (pDefaultMT == NULL)
                {
                    m_resID = IDS_EE_BADMARSHAL_WINRT_MISSING_GUID;
                    IfFailGoto(E_FAIL, lFail);
                }
            }

            if (nativeType == NATIVE_TYPE_INTF)
            {
                // whatever...
                if (sig.IsStringType(pModule, pTypeContext)) 
                {
                    m_resID = IDS_EE_BADMARSHALPARAM_STRING;
                    IfFailGoto(E_FAIL, lFail);
                }

                if (COMDelegate::IsDelegate(m_pMT))
                {
                    if (m_ms == MARSHAL_SCENARIO_WINRT)
                    {
                        // In WinRT scenarios delegates must be WinRT delegates
                        if (!m_pMT->IsProjectedFromWinRT() && !WinRTTypeNameConverter::IsRedirectedType(m_pMT))
                        {
                            m_resID = IDS_EE_BADMARSHAL_WINRT_DELEGATE;
                            IfFailGoto(E_FAIL, lFail);
                        }
                    }
                    else
                    {
                        // UnmanagedType.Interface for delegates used to mean the .NET Framework _Delegate interface.
                        // We don't support that interface in .NET Core, so we disallow marshalling as it here.
                        // The user can specify UnmanagedType.IDispatch and use the delegate through the IDispatch interface
                        // if they need an interface pointer.
                        m_resID = IDS_EE_BADMARSHAL_DELEGATE_TLB_INTERFACE;
                        IfFailGoto(E_FAIL, lFail);
                    }
                }
                m_type = MARSHAL_TYPE_INTERFACE;
            }
            else if (pDefaultMT != NULL && nativeType == NATIVE_TYPE_DEFAULT)
            {
                // Pretend this is really marshaling as the default interface type

                // Validate it's a WinRT interface with GUID
                if (!pDefaultMT->IsInterface() ||
                    (!pDefaultMT->IsProjectedFromWinRT() && !pDefaultMT->IsExportedToWinRT()) ||
                    !pDefaultMT->HasExplicitGuid())
                {
                    // This might also be a redirected interface - which is also allowed
                    if (!pDefaultMT->IsWinRTRedirectedInterface(TypeHandle::Interop_NativeToManaged))
                    {
                        m_resID = IDS_EE_BADMARSHAL_DEFAULTIFACE_NOT_WINRT_IFACE;
                        IfFailGoto(E_FAIL, lFail);
                    }
                }

                // Validate that it's one of the component interfaces of the class in the signature
                if (!m_pMT->ImplementsEquivalentInterface(pDefaultMT))
                {
                    m_resID = IDS_EE_BADMARSHAL_DEFAULTIFACE_NOT_SUBTYPE;
                    IfFailGoto(E_FAIL, lFail);
                }

                // Make sure it's not an unexpected generic case (not clear we can actually get here in practice due
                // to the above Implements check)
                if (pDefaultMT->HasInstantiation() && !pDefaultMT->SupportsGenericInterop(TypeHandle::Interop_NativeToManaged))
                {
                    m_resID = IDS_EE_BADMARSHAL_GENERICS_RESTRICTION;
                    IfFailGoto(E_FAIL, lFail);
                }

                // Store the marshal data just as if we were marshaling as this default interface type
                m_type = MARSHAL_TYPE_INTERFACE;
                m_pDefaultItfMT = pDefaultMT;
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
                            if (m_ms != MARSHAL_SCENARIO_NDIRECT)
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

                            m_type = MARSHAL_TYPE_HSTRING;
                            break;
                        }
#endif // FEATURE_COMINTEROP
    
                        case NATIVE_TYPE_DEFAULT:
                        {
#ifdef FEATURE_COMINTEROP
                            if (m_ms == MARSHAL_SCENARIO_WINRT)
                            {
                                if (builder)
                                {
                                    m_resID = IDS_EE_BADMARSHALPARAM_STRINGBUILDER;
                                    IfFailGoto(E_FAIL, lFail);
                                }

                                m_type = MARSHAL_TYPE_HSTRING;
                            }
                            else if (m_ms != MARSHAL_SCENARIO_NDIRECT)
                            {
                                _ASSERTE(m_ms == MARSHAL_SCENARIO_COMINTEROP);
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
                            m_resID = builder ? IDS_EE_BADMARSHALPARAM_STRINGBUILDER : IDS_EE_BADMARSHALPARAM_STRING;
                            IfFailGoto(E_FAIL, lFail);
                            break;
                    }
                }
#ifdef FEATURE_COMINTEROP
                else if (sig.IsClassThrowing(pModule, g_CollectionsEnumeratorClassName, pTypeContext) && 
                         nativeType == NATIVE_TYPE_DEFAULT)
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
                else if (sigTH.CanCastTo(TypeHandle(MscorlibBinder::GetClass(CLASS__SAFE_HANDLE))))
                {
                    if (nativeType != NATIVE_TYPE_DEFAULT)
                    {
                        m_resID = IDS_EE_BADMARSHAL_SAFEHANDLE;
                        IfFailGoto(E_FAIL, lFail);
                    }
                    m_args.m_pMT = m_pMT;
                    m_type = MARSHAL_TYPE_SAFEHANDLE;
                }
                else if (sigTH.CanCastTo(TypeHandle(MscorlibBinder::GetClass(CLASS__CRITICAL_HANDLE))))
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

                    if (m_ms == MARSHAL_SCENARIO_WINRT)
                    {
                        // all interfaces marshaled in WinRT scenarios are IInspectable-based
                        m_fInspItf = TRUE;
                    }
                }
                // Check for Windows.Foundation.HResult <-> Exception
                else if (m_ms == MARSHAL_SCENARIO_WINRT && MscorlibBinder::IsClass(m_pMT, CLASS__EXCEPTION))
                {
                    m_args.m_pMT = m_pMT;
                    m_type = MARSHAL_TYPE_EXCEPTION;
                }
#endif // FEATURE_COMINTEROP
                else if (COMDelegate::IsDelegate(m_pMT))
                {
                    m_args.m_pMT = m_pMT;
#ifdef FEATURE_COMINTEROP
                    if (m_ms == MARSHAL_SCENARIO_WINRT)
                    {
                        // Delegates must be imported from WinRT and marshaled as Interface
                        if (!m_pMT->IsProjectedFromWinRT() && !WinRTTypeNameConverter::IsRedirectedType(m_pMT))
                        {
                            m_resID = IDS_EE_BADMARSHAL_WINRT_DELEGATE;
                            IfFailGoto(E_FAIL, lFail);
                        }
                    }
#endif // FEATURE_COMINTEROP

                    switch (nativeType)
                    {
                        case NATIVE_TYPE_FUNC:
                            m_type = MARSHAL_TYPE_DELEGATE;
                            break;

                        case NATIVE_TYPE_DEFAULT:
#ifdef FEATURE_COMINTEROP
                            if (m_ms == MARSHAL_SCENARIO_WINRT || m_pMT->IsProjectedFromWinRT() || WinRTTypeNameConverter::IsRedirectedType(m_pMT))
                            {
                                m_type = MARSHAL_TYPE_INTERFACE;
                            }
                            else if (m_ms == MARSHAL_SCENARIO_COMINTEROP)
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
                    if (!(nativeType == NATIVE_TYPE_DEFAULT || nativeType == NATIVE_TYPE_LPSTRUCT))
                    {
                        m_resID = IDS_EE_BADMARSHAL_CLASS;
                        IfFailGoto(E_FAIL, lFail);
                    }
                    m_type = MARSHAL_TYPE_BLITTABLEPTR;
                    m_args.m_pMT = m_pMT;
                }
                else if (m_pMT->HasLayout())
                {
                    if (!(nativeType == NATIVE_TYPE_DEFAULT || nativeType == NATIVE_TYPE_LPSTRUCT))
                    {
                        m_resID = IDS_EE_BADMARSHAL_CLASS;
                        IfFailGoto(E_FAIL, lFail);
                    }
                    m_type = MARSHAL_TYPE_LAYOUTCLASSPTR;
                    m_args.m_pMT = m_pMT;
                }
#ifdef FEATURE_COMINTEROP
                else if (m_ms == MARSHAL_SCENARIO_WINRT && sig.IsClassThrowing(pModule, g_SystemUriClassName, pTypeContext))
                {
                    m_type = MARSHAL_TYPE_URI;
                }
                else if (m_ms == MARSHAL_SCENARIO_WINRT && sig.IsClassThrowing(pModule, g_NotifyCollectionChangedEventArgsName, pTypeContext))
                {
                    m_type = MARSHAL_TYPE_NCCEVENTARGS;
                }
                else if (m_ms == MARSHAL_SCENARIO_WINRT && sig.IsClassThrowing(pModule, g_PropertyChangedEventArgsName, pTypeContext))
                {
                    m_type = MARSHAL_TYPE_PCEVENTARGS;
                }
#endif // FEATURE_COMINTEROP
                else if (m_pMT->IsObjectClass())
                {
                    switch(nativeType)
                    {
#ifdef FEATURE_COMINTEROP
                        case NATIVE_TYPE_DEFAULT:
                            if (ms == MARSHAL_SCENARIO_WINRT)
                            {
                                m_fInspItf = TRUE;
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
                            m_fInspItf = TRUE;
                            m_type = MARSHAL_TYPE_INTERFACE;
                            break;
#else
                        case NATIVE_TYPE_DEFAULT:
                        case NATIVE_TYPE_STRUCT:
                            m_resID = IDS_EE_OBJECT_TO_VARIANT_NOT_SUPPORTED;
                            IfFailGoto(E_FAIL, lFail);

                        case NATIVE_TYPE_INTF:
                        case NATIVE_TYPE_IUNKNOWN:
                        case NATIVE_TYPE_IDISPATCH:
                            m_resID = IDS_EE_OBJECT_TO_ITF_NOT_SUPPORTED;
                            IfFailGoto(E_FAIL, lFail);
#endif // FEATURE_COMINTEROP

                        case NATIVE_TYPE_ASANY:
                            m_type = m_fAnsi ? MARSHAL_TYPE_ASANYA : MARSHAL_TYPE_ASANYW;
                            break;

                        default:
                            m_resID = IDS_EE_BADMARSHAL_OBJECT;
                            IfFailGoto(E_FAIL, lFail);
                    }
                }
               
#ifdef FEATURE_COMINTEROP
                else if (sig.IsClassThrowing(pModule, g_ArrayClassName, pTypeContext))
                {            
                    if (IsWinRTScenario())
                    {
                        m_resID = IDS_EE_BADMARSHAL_WINRT_ILLEGAL_TYPE;
                        IfFailGoto(E_FAIL, lFail);
                    }

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
                            
                            IfFailGoto(HandleArrayElemType(&ParamInfo, thElement, -1, FALSE, isParam, pAssembly), lFail);
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
                else if ((m_ms == MARSHAL_SCENARIO_WINRT) && sig.IsClassThrowing(pModule, g_TypeClassName, pTypeContext))
                {
                    m_type = MARSHAL_TYPE_SYSTEMTYPE;
                }
#endif // FEATURE_COMINTEROP
                else if (!m_pMT->IsValueType())
                {
#ifdef FEATURE_COMINTEROP
                    if (IsWinRTScenario() && !m_pMT->IsLegalNonArrayWinRTType())
                    {
                        m_resID = IDS_EE_BADMARSHAL_WINRT_ILLEGAL_TYPE;
                        IfFailGoto(E_FAIL, lFail);
                    }
#endif // FEATURE_COMINTEROP

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
                        m_type = MARSHAL_TYPE_DECIMAL_PTR;
                        break;

                    case NATIVE_TYPE_CURRENCY:
                        m_type = MARSHAL_TYPE_CURRENCY;
                        break;

                    default:
                        m_resID = IDS_EE_BADMARSHALPARAM_DECIMAL;
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
                        m_type = MARSHAL_TYPE_GUID_PTR;
                        break;

                    default:
                        m_resID = IDS_EE_BADMARSHAL_GUID;
                        IfFailGoto(E_FAIL, lFail);
                }
            }
#ifdef FEATURE_COMINTEROP
            else if (sig.IsClassThrowing(pModule, g_DateTimeOffsetClassName, pTypeContext))
            {
                if (!(nativeType == NATIVE_TYPE_DEFAULT || nativeType == NATIVE_TYPE_STRUCT))
                {
                    m_resID = IDS_EE_BADMARSHAL_DATETIMEOFFSET;
                    IfFailGoto(E_FAIL, lFail);
                }
                m_type = MARSHAL_TYPE_DATETIME;
                m_pMT = MscorlibBinder::GetClass(CLASS__DATE_TIME_OFFSET);
            }           
#endif  // FEATURE_COMINTEROP
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
                if (!(nativeType == NATIVE_TYPE_DEFAULT))
                {
                    IfFailGoto(E_FAIL, lFail);
                }
                m_type = MARSHAL_TYPE_ARRAYWITHOFFSET;
            }
            else if (sig.IsClassThrowing(pModule, "System.Runtime.InteropServices.HandleRef", pTypeContext))
            {
                if (!(nativeType == NATIVE_TYPE_DEFAULT))
                {
                    IfFailGoto(E_FAIL, lFail);
                }
                m_type = MARSHAL_TYPE_HANDLEREF;
            }
            else if (sig.IsClassThrowing(pModule, "System.ArgIterator", pTypeContext))
            {
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
                if (nativeType != NATIVE_TYPE_DEFAULT)
                {
                    IfFailGoto(E_FAIL, lFail);
                }

                m_type = MARSHAL_TYPE_RUNTIMETYPEHANDLE;
            }
            else if (sig.IsClassThrowing(pModule, g_RuntimeFieldHandleClassName, pTypeContext))
            {
                if (nativeType != NATIVE_TYPE_DEFAULT)
                {
                    IfFailGoto(E_FAIL, lFail);
                }

                m_type = MARSHAL_TYPE_RUNTIMEFIELDHANDLE;
            }
            else if (sig.IsClassThrowing(pModule, g_RuntimeMethodHandleClassName, pTypeContext))
            {
                if (nativeType != NATIVE_TYPE_DEFAULT)
                {
                    IfFailGoto(E_FAIL, lFail);
                }

                m_type = MARSHAL_TYPE_RUNTIMEMETHODHANDLE;
            }
            else
            {
                m_pMT = sig.GetTypeHandleThrowing(pModule, pTypeContext).GetMethodTable();
                if (m_pMT == NULL)
                    break;

#ifdef FEATURE_COMINTEROP
                // Handle Nullable<T> and KeyValuePair<K, V> for WinRT
                if (m_ms == MARSHAL_SCENARIO_WINRT)
                {
                    if (m_pMT->HasSameTypeDefAs(g_pNullableClass))
                    {
                        m_type = MARSHAL_TYPE_NULLABLE;
                        m_args.m_pMT = m_pMT;
                        break;
                    }

                    if (m_pMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__KEYVALUEPAIRGENERIC)))
                    {
                        m_type = MARSHAL_TYPE_KEYVALUEPAIR;
                        m_args.m_pMT = m_pMT;
                        break;
                    }

                    if (!m_pMT->IsLegalNonArrayWinRTType())
                    {
                        m_resID = IDS_EE_BADMARSHAL_WINRT_ILLEGAL_TYPE;
                        IfFailGoto(E_FAIL, lFail);
                    }
                }
#endif // FEATURE_COMINTEROP

                if (m_pMT->HasInstantiation())
                {
                    m_resID = IDS_EE_BADMARSHAL_GENERICS_RESTRICTION;
                    IfFailGoto(E_FAIL, lFail);
                }

                UINT managedSize = m_pMT->GetAlignedNumInstanceFieldBytes();
                UINT  nativeSize = m_pMT->GetNativeSize();
                
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

                    if (m_byref && !isParam)
                    {
                        // Override the prohibition on byref returns so that IJW works
                        m_byref = FALSE;
                        m_type = ((sizeof(void*) == 4) ? MARSHAL_TYPE_GENERIC_4 : MARSHAL_TYPE_GENERIC_8);
                    }
                    else
                    {
                        if (fNeedsCopyCtor)
                        {
#ifdef FEATURE_COMINTEROP
                            if (m_ms == MARSHAL_SCENARIO_WINRT)
                            {
                                // our WinRT-optimized GetCOMIPFromRCW helpers don't support copy
                                // constructor stubs so make sure that this marshaler will not be used
                                m_resID = IDS_EE_BADMARSHAL_WINRT_COPYCTOR;
                                IfFailGoto(E_FAIL, lFail);
                            }
#endif

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
#ifdef _TARGET_X86_
                        // JIT64 is not aware of normalized value types and this optimization
                        // (returning small value types by value in registers) is already done in JIT64.
                        if (        !m_byref   // Permit register-sized structs as return values
                                 && !isParam
                                 && !onInstanceMethod
                                 && CorIsPrimitiveType(m_pMT->GetInternalCorElementType())
                                 && !IsUnmanagedValueTypeReturnedByRef(nativeSize)
                                 && managedSize <= sizeof(void*)
                                 && nativeSize <= sizeof(void*))
                        {
                            m_type = MARSHAL_TYPE_GENERIC_4;
                            m_args.m_pMT = m_pMT;
                        }
                        else
#endif // _TARGET_X86_
                        {
                            m_args.m_pMT = m_pMT;
                            m_type = MARSHAL_TYPE_BLITTABLEVALUECLASS;
                        }
                    }
                }
                else if (m_pMT->HasLayout())
                {
                    if (!(nativeType == NATIVE_TYPE_DEFAULT || nativeType == NATIVE_TYPE_STRUCT))
                    {
                        m_resID = IDS_EE_BADMARSHAL_VALUETYPE;
                        IfFailGoto(E_FAIL, lFail);
                    }

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

            ArrayTypeDesc* asArray = arrayTypeHnd.AsArray();
            if (asArray == NULL)
                IfFailGoto(E_FAIL, lFail);

            TypeHandle thElement = asArray->GetTypeParam();

#ifdef FEATURE_COMINTEROP
            if (m_ms != MARSHAL_SCENARIO_WINRT)
#endif // FEATURE_COMINTEROP
            {
                if (thElement.HasInstantiation())
                {
                    m_resID = IDS_EE_BADMARSHAL_GENERICS_RESTRICTION;
                    IfFailGoto(E_FAIL, lFail);
                }
            }

            // Handle retrieving the information for the array type.
            IfFailGoto(HandleArrayElemType(&ParamInfo, thElement, asArray->GetRank(), mtype == ELEMENT_TYPE_SZARRAY, isParam, pAssembly), lFail);
            break;
        }
        
        default:
            m_resID = IDS_EE_BADMARSHAL_BADMANAGED;
    }

lExit:
#ifdef FEATURE_COMINTEROP
//Field scenario is not blocked here because we don't want to block loading structs that 
//have the types which we are blocking, but never pass it to Interop.

    if (AppX::IsAppXProcess() && ms != MarshalInfo::MARSHAL_SCENARIO_FIELD)
    {
        bool set_error = false;
        switch (m_type)
        {
            case MARSHAL_TYPE_ANSIBSTR: 
                m_resID = IDS_EE_BADMARSHAL_TYPE_ANSIBSTR;
                set_error = true;
                break;
            case MARSHAL_TYPE_VBBYVALSTR:
            case MARSHAL_TYPE_VBBYVALSTRW:
                m_resID = IDS_EE_BADMARSHAL_TYPE_VBBYVALSTR;
                set_error = true;
                break;
            case MARSHAL_TYPE_REFERENCECUSTOMMARSHALER:
                m_resID = IDS_EE_BADMARSHAL_TYPE_REFERENCECUSTOMMARSHALER;
                set_error = true;
                break;
            case MARSHAL_TYPE_ASANYA: 
            case MARSHAL_TYPE_ASANYW: 
                m_resID = IDS_EE_BADMARSHAL_TYPE_ASANYA;
                set_error = true;
                break;
            case MARSHAL_TYPE_INTERFACE:
                if (m_fDispItf)
                {
                    m_resID = IDS_EE_BADMARSHAL_TYPE_IDISPATCH;
                    set_error = true;
                }
                break;
        }

        if (set_error)
            COMPlusThrow(kPlatformNotSupportedException, m_resID);

    }
    
    if (IsWinRTScenario() && !IsSupportedForWinRT(m_type))
    {
        // the marshaler we came up with is not supported in WinRT scenarios
        m_type = MARSHAL_TYPE_UNKNOWN;
        m_resID = IDS_EE_BADMARSHAL_WINRT_ILLEGAL_TYPE;
        goto lReallyExit;
    }
#endif // FEATURE_COMINTEROP

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
        
        // If neither IN nor OUT are true, this signals the URT to use the default
        // rules.
        if (!m_in && !m_out)
        {
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
    if ((m_ms == MARSHAL_SCENARIO_COMINTEROP || m_ms == MARSHAL_SCENARIO_WINRT) && fMngToNative)
    {
        psl->SetInteropParamExceptionInfo(resID, paramIdx);
        return;
    }
#endif // FEATURE_COMINTEROP

    ThrowInteropParamException(resID, paramIdx);
}


HRESULT MarshalInfo::HandleArrayElemType(NativeTypeParamInfo *pParamInfo, TypeHandle thElement, int iRank, BOOL fNoLowerBounds, BOOL isParam, Assembly *pAssembly)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
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
    if (m_ms == MARSHAL_SCENARIO_WINRT)
    {
        m_type = MARSHAL_TYPE_HIDDENLENGTHARRAY;
    }
    else if (pParamInfo->m_NativeType == NATIVE_TYPE_SAFEARRAY)
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
    else
    {
        m_resID = IDS_EE_BADMARSHAL_ARRAY;
        return E_FAIL;
    }

#ifdef FEATURE_COMINTEROP
    if (m_type == MARSHAL_TYPE_SAFEARRAY)
    {
        arrayMarshalInfo.InitForSafeArray(m_ms, thElement, pParamInfo->m_SafeArrayElementVT, m_fAnsi);
    }
    else if (m_type == MARSHAL_TYPE_HIDDENLENGTHARRAY)
    {
        arrayMarshalInfo.InitForHiddenLengthArray(thElement);
    }
    else
#endif // FEATURE_COMINTEROP
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

    if (m_type == MARSHAL_TYPE_NATIVEARRAY)
    {
        // Retrieve the extra information associated with the native array marshaling.
        m_args.na.m_vt  = m_arrayElementType;
        m_countParamIdx = pParamInfo->m_CountParamIdx;
        m_multiplier    = pParamInfo->m_Multiplier;
        m_additive      = pParamInfo->m_Additive;
    }
#ifdef FEATURE_COMINTEROP
    else if (m_type == MARSHAL_TYPE_HIDDENLENGTHARRAY)
    {
        m_args.na.m_vt  = m_arrayElementType;
        m_args.na.m_cbElementSize = arrayMarshalInfo.GetElementSize();
        m_args.na.m_redirectedTypeIndex = arrayMarshalInfo.GetRedirectedTypeIndex();
    }
#endif // FEATURE_COMINTEROP

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

#define DEFINE_MARSHALER_TYPE(mt, mclass, fWinRTSupported) \
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

    pMarshaler->EmitSetupArgument(pcsDispatch);
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

        ILCodeStream* pcsMarshal    = psl->GetMarshalCodeStream();
        ILCodeStream* pcsUnmarshal  = psl->GetReturnUnmarshalCodeStream();
        ILCodeStream* pcsDispatch   = psl->GetDispatchCodeStream();
            
        pcsMarshal->EmitNOP("// return { ");
        pcsUnmarshal->EmitNOP("// return { ");
            
        UINT16 wNativeSize = GetNativeSize(m_type, m_ms);

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

void MarshalInfo::SetupArgumentSizes()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_byref)
    {
        m_managedArgSize = StackElemSize(sizeof(void*));
        m_nativeArgSize = StackElemSize(sizeof(void*));
    }
    else
    {
        m_managedArgSize = StackElemSize(GetManagedSize(m_type, m_ms));
        m_nativeArgSize = StackElemSize(GetNativeSize(m_type, m_ms));
    }

#ifdef ENREGISTERED_PARAMTYPE_MAXSIZE
    if (m_managedArgSize > ENREGISTERED_PARAMTYPE_MAXSIZE)
        m_managedArgSize = StackElemSize(sizeof(void*));

    if (m_nativeArgSize > ENREGISTERED_PARAMTYPE_MAXSIZE)
        m_nativeArgSize = StackElemSize(sizeof(void*));
#endif // ENREGISTERED_PARAMTYPE_MAXSIZE
}

UINT16 MarshalInfo::GetManagedSize(MarshalType mtype, MarshalScenario ms)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    static const BYTE managedSizes[]=
    {
        #define DEFINE_MARSHALER_TYPE(mt, mclass, fWinRTSupported) IL##mclass::c_CLRSize,
        #include "mtypes.h"
    };

    _ASSERTE((SIZE_T)mtype < COUNTOF(managedSizes));
    BYTE managedSize = managedSizes[mtype];

    if (managedSize == VARIABLESIZE)
    {
        switch (mtype)
        {

            case MARSHAL_TYPE_BLITTABLEVALUECLASS:
            case MARSHAL_TYPE_VALUECLASS:
#ifdef FEATURE_COMINTEROP
            case MARSHAL_TYPE_DATETIME:
            case MARSHAL_TYPE_NULLABLE:
            case MARSHAL_TYPE_KEYVALUEPAIR:
#endif // FEATURE_COMINTEROP
                return (UINT16) m_pMT->GetAlignedNumInstanceFieldBytes();
                break;

            default:
                _ASSERTE(0);
        }
    }

    return managedSize;
}

UINT16 MarshalInfo::GetNativeSize(MarshalType mtype, MarshalScenario ms)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    static const BYTE nativeSizes[]=
    {
        #define DEFINE_MARSHALER_TYPE(mt, mclass, fWinRTSupported) IL##mclass::c_nativeSize,
        #include "mtypes.h"
    };

    _ASSERTE((SIZE_T)mtype < COUNTOF(nativeSizes));
    BYTE nativeSize = nativeSizes[mtype];

    if (nativeSize == VARIABLESIZE)
    {
        switch (mtype)
        {
            case MARSHAL_TYPE_BLITTABLEVALUECLASS:
            case MARSHAL_TYPE_VALUECLASS:
            case MARSHAL_TYPE_BLITTABLEVALUECLASSWITHCOPYCTOR:
                return (UINT16) m_pMT->GetNativeSize();

            default:
                _ASSERTE(0);
        }
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
        #define DEFINE_MARSHALER_TYPE(mt, mclass, fWinRTSupported) \
            (IL##mclass::c_fInOnly ? true : false),

        #include "mtypes.h"
    };

    return ILMarshalerIsInOnly[mtype];
}

bool MarshalInfo::IsSupportedForWinRT(MarshalType mtype)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    static const bool MarshalerSupportsWinRT[] =
    {
        #define DEFINE_MARSHALER_TYPE(mt, mclass, fWinRTSupported) \
            fWinRTSupported,

        #include "mtypes.h"
    };

    return MarshalerSupportsWinRT[mtype];
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
        #define DEFINE_MARSHALER_TYPE(mt, mclass, fWinRTSupported) IL##mclass::ArgumentOverride,
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
        #define DEFINE_MARSHALER_TYPE(mt, mclass, fWinRTSupported) IL##mclass::ReturnOverride,
        #include "mtypes.h"
    };

    _ASSERTE((SIZE_T)mtype < COUNTOF(ILReturnOverrideProcs));
    return ILReturnOverrideProcs[mtype];
}

void MarshalInfo::GetItfMarshalInfo(ItfMarshalInfo* pInfo)
{
    STANDARD_VM_CONTRACT;

    GetItfMarshalInfo(TypeHandle(m_pMT),
#ifdef FEATURE_COMINTEROP
        TypeHandle(m_pDefaultItfMT),
#else // FEATURE_COMINTEROP
        TypeHandle(),
#endif // FEATURE_COMINTEROP
        m_fDispItf,
        m_fInspItf,
        m_ms,
        pInfo);
}

void MarshalInfo::GetItfMarshalInfo(TypeHandle th, TypeHandle thItf, BOOL fDispItf, BOOL fInspItf, MarshalScenario ms, ItfMarshalInfo *pInfo)
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

            if (th.IsTypeDesc() || !th.AsMethodTable()->IsWinRTDelegate())
            {
                // If this is not a WinRT delegate, retrieve the default interface method table.
                TypeHandle hndDefItfClass;
                DefaultInterfaceType DefItfType;
           
                if (!thItf.IsNull())
                {
                    hndDefItfClass = thItf;
                    DefItfType = DefaultInterfaceType_Explicit;
                }
                else if (th.IsProjectedFromWinRT() || th.IsExportedToWinRT())
                {
                    // WinRT classes use their WinRT default interface
                    hndDefItfClass = th.GetMethodTable()->GetDefaultWinRTInterface();
                    DefItfType = DefaultInterfaceType_Explicit;
                }
                else
                {
                    DefItfType = GetDefaultInterfaceForClassWrapper(th, &hndDefItfClass);
                }
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

                            case ifInspectable:
                                pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_INSP_ITF;
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
        }
        else
        {
            // The type will be marshalled as an IUnknown, IInspectable, or IDispatch pointer depending
            // on the value of fDispItf and fInspItf
            if (fDispItf)
            {
                pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_DISP_ITF;
            }
            else if (fInspItf)
            {
                pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_INSP_ITF;
            }
            
            pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_USE_BASIC_ITF;
        }
    }
    else if (fInspItf)
    {
        // IInspectable-based interfaces are simple
        pInfo->thItf = th;
        pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_INSP_ITF;
    }
    else
    {
        // Determine the interface this type will be marshalled as. 
        if (th.IsComClassInterface())
            pInfo->thItf = th.GetDefItfForComClassItf();
        else
            pInfo->thItf = th;

        // Determine if we are dealing with an IDispatch, IInspectable, or IUnknown based interface.
        switch (pInfo->thItf.GetComInterfaceType())
        {
            case ifDispatch:
            case ifDual:
                pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_DISP_ITF;
                break;

            case ifInspectable:
                pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_INSP_ITF;
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

    if (ms == MARSHAL_SCENARIO_WINRT)
    {
        // Use the "class is hint" flag so GetObjectRefFromComIP doesn't verify that the
        // WinRT object really supports IInspectable - note that we'll do the verification
        // in UnmarshalObjectFromInterface for this exact pInfo->thItf.
        pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_CLASS_IS_HINT;

        pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_WINRT_SCENARIO;
        
        // Perform interface redirection statically here. When the resulting ItfMarshalInfo
        // is used for CLR->WinRT marshaling, this is necessary so we know which COM vtable
        // to pass out (for instance IList could be marshaled out as IList or IBindableVector
        // depending on the marshal scenario). In the WinRT->CLR direction, it's just an
        // optimization which saves us from performing redirection at run-time.

        if (!pInfo->thItf.IsNull())
        {
            MethodTable *pNewItfMT1;
            MethodTable *pNewItfMT2;
            switch (RCW::GetInterfacesForQI(pInfo->thItf.GetMethodTable(), &pNewItfMT1, &pNewItfMT2))
            {
                case RCW::InterfaceRedirection_None:
                case RCW::InterfaceRedirection_UnresolvedIEnumerable:
                    break;

                case RCW::InterfaceRedirection_IEnumerable_RetryOnFailure:
                case RCW::InterfaceRedirection_IEnumerable:
                case RCW::InterfaceRedirection_Other:
                    pInfo->thNativeItf = pNewItfMT1;
                    break;

                case RCW::InterfaceRedirection_Other_RetryOnFailure:
                    pInfo->thNativeItf = pNewItfMT2;
                    break;
            }
        }

        if (!pInfo->thNativeItf.IsNull())
        {
            // The native interface is redirected WinRT interface - need to change the flags
            _ASSERTE(pInfo->thNativeItf.AsMethodTable()->IsProjectedFromWinRT());
            
            pInfo->dwFlags &= ~ItfMarshalInfo::ITF_MARSHAL_DISP_ITF;
            pInfo->dwFlags |= ItfMarshalInfo::ITF_MARSHAL_INSP_ITF;            
        }
    }

#else // FEATURE_COMINTEROP
    if (!th.IsInterface())
        pInfo->thClass = th;
    else
        pInfo->thItf = th;
#endif // FEATURE_COMINTEROP
}

HRESULT MarshalInfo::TryGetItfMarshalInfo(TypeHandle th, BOOL fDispItf, BOOL fInspItf, ItfMarshalInfo *pInfo)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(!th.IsNull());
        PRECONDITION(CheckPointer(pInfo));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    EX_TRY
    {
        GetItfMarshalInfo(th, TypeHandle(), fDispItf, fInspItf,
#ifdef FEATURE_COMINTEROP
            MARSHAL_SCENARIO_COMINTEROP,
#else // FEATURE_COMINTEROP
            MARSHAL_SCENARIO_NDIRECT,
#endif // FEATURE_COMINTEROP
            pInfo);
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(RethrowTerminalExceptions);
    
    return hr;
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
                    XXXXX(NATIVE_TYPE_HSTRING)
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
            #define DEFINE_MARSHALER_TYPE(mt, mc, fWinRTSupported) case mt: logbuf.AppendASCII( #mt " (IL" #mc ")"); break;
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

#ifndef CROSSGEN_COMPILE
#ifdef FEATURE_COMINTEROP
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
            GetItfMarshalInfo(TypeHandle(m_pMT), TypeHandle(m_pDefaultItfMT), m_fDispItf, m_fInspItf, m_ms, &itfInfo);
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

#ifdef FEATURE_CLASSIC_COMINTEROP
        case MARSHAL_TYPE_SAFEARRAY:
            pDispParamMarshaler = new DispParamArrayMarshaler(m_arrayElementType, m_hndArrayElemType.GetMethodTable());
            break;
#endif

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
            GetItfMarshalInfo(TypeHandle(m_pMT), TypeHandle(m_pDefaultItfMT), m_fDispItf, m_fInspItf, m_ms, &itfInfo);
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

#endif // FEATURE_COMINTEROP


VOID MarshalInfo::MarshalTypeToString(SString& strMarshalType, BOOL fSizeIsSpecified)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    LPCWSTR strRetVal;

    if (m_type == MARSHAL_TYPE_NATIVEARRAY)
    {
        SString strVarType;
        VarTypeToString(m_arrayElementType, strVarType);

        if (!fSizeIsSpecified)
        {
            strMarshalType.Printf(W("native array of %s (size not specified by a parameter)"),
                                  strVarType.GetUnicode());
        }
        else
        {
            strMarshalType.Printf(W("native array of %s (size specified by parameter %i)"),
                                  strVarType.GetUnicode(), m_countParamIdx);
        }

        return;
    }
#ifdef FEATURE_COMINTEROP
    // Some MarshalTypes have extra information and require special handling
    else if (m_type == MARSHAL_TYPE_INTERFACE)
    {
        ItfMarshalInfo itfInfo;
        GetItfMarshalInfo(TypeHandle(m_pMT), TypeHandle(m_pDefaultItfMT), m_fDispItf, m_fInspItf, m_ms, &itfInfo);

        if (!itfInfo.thItf.IsNull())
        {
            StackSString ssClassName;
            itfInfo.thItf.GetMethodTable()->_GetFullyQualifiedNameForClass(ssClassName);

            if (!!(itfInfo.dwFlags & ItfMarshalInfo::ITF_MARSHAL_DISP_ITF))
            {
                strMarshalType.SetLiteral(W("IDispatch "));
            }
            else if (!!(itfInfo.dwFlags & ItfMarshalInfo::ITF_MARSHAL_INSP_ITF))
            {
                strMarshalType.SetLiteral(W("IInspectable"));
            }
            else
            {
                strMarshalType.SetLiteral(W("IUnknown "));
            }

            if (itfInfo.dwFlags & ItfMarshalInfo::ITF_MARSHAL_USE_BASIC_ITF)
            {
                strMarshalType.Append(W("(basic) "));
            }

            strMarshalType.Append(ssClassName);
            return;
        }
        else
        {
            if (!!(itfInfo.dwFlags & ItfMarshalInfo::ITF_MARSHAL_DISP_ITF))
                strRetVal = W("IDispatch");
            else if (!!(itfInfo.dwFlags & ItfMarshalInfo::ITF_MARSHAL_INSP_ITF))
                strRetVal = W("IInspectable");
            else
                strRetVal = W("IUnknown");
        }
    }
    else if (m_type == MARSHAL_TYPE_SAFEARRAY)
    {
        StackSString strVarType;
        VarTypeToString(m_arrayElementType, strVarType);

        strMarshalType = SL(W("SafeArray of "));
        strMarshalType.Append(strVarType);

        return;
    }
#endif // FEATURE_COMINTEROP
    else if (m_type == MARSHAL_TYPE_REFERENCECUSTOMMARSHALER)
    {
        GCX_COOP();
        
        OBJECTHANDLE objHandle = m_pCMHelper->GetCustomMarshalerInfo()->GetCustomMarshaler();
        {
            OBJECTREF pObjRef = ObjectFromHandle(objHandle);
            DefineFullyQualifiedNameForClassW();

            strMarshalType.Printf(W("custom marshaler (%s)"),
                                  GetFullyQualifiedNameForClassW(pObjRef->GetMethodTable()));
        }        
        
        return;
    }
    else
    {
        // All other MarshalTypes with no special handling
        switch (m_type)
        {
            case MARSHAL_TYPE_GENERIC_1:
                strRetVal = W("BYTE");
                break;
            case MARSHAL_TYPE_GENERIC_U1:
                strRetVal = W("unsigned BYTE");
                break;
            case MARSHAL_TYPE_GENERIC_2:
                strRetVal = W("WORD");
                break;
            case MARSHAL_TYPE_GENERIC_U2:
                strRetVal = W("unsigned WORD");
                break;
            case MARSHAL_TYPE_GENERIC_4:
                strRetVal = W("DWORD");
                break;
            case MARSHAL_TYPE_GENERIC_8:
                strRetVal = W("QUADWORD");
                break;
            case MARSHAL_TYPE_WINBOOL:
                strRetVal = W("Windows Bool");
                break;
#ifdef FEATURE_COMINTEROP
            case MARSHAL_TYPE_VTBOOL:
                strRetVal = W("VARIANT Bool");
                break;
#endif // FEATURE_COMINTEROP
            case MARSHAL_TYPE_ANSICHAR:
                strRetVal = W("Ansi character");
                break;
            case MARSHAL_TYPE_CBOOL:
                strRetVal = W("CBool");
                break;
            case MARSHAL_TYPE_FLOAT:
                strRetVal = W("float");
                break;
            case MARSHAL_TYPE_DOUBLE:
                strRetVal = W("double");
                break;
            case MARSHAL_TYPE_CURRENCY:
                strRetVal = W("CURRENCY");
                break;
            case MARSHAL_TYPE_DECIMAL:
                strRetVal = W("DECIMAL");
                break;
            case MARSHAL_TYPE_DECIMAL_PTR:
                strRetVal = W("DECIMAL pointer");
                break;
            case MARSHAL_TYPE_GUID:
                strRetVal = W("GUID");
                break;
            case MARSHAL_TYPE_GUID_PTR:
                strRetVal = W("GUID pointer");
                break;
            case MARSHAL_TYPE_DATE:
                strRetVal = W("DATE");
                break;
             case MARSHAL_TYPE_BSTR:
                strRetVal = W("BSTR");
                break;
            case MARSHAL_TYPE_LPWSTR:
                strRetVal = W("LPWSTR");
                break;
            case MARSHAL_TYPE_LPSTR:
                strRetVal = W("LPSTR");
                break;
            case MARSHAL_TYPE_LPUTF8STR:
                strRetVal = W("LPUTF8STR");
                break;
#ifdef FEATURE_COMINTEROP
            case MARSHAL_TYPE_ANSIBSTR:
                strRetVal = W("AnsiBStr");
                break;
#endif // FEATURE_COMINTEROP
            case MARSHAL_TYPE_LPWSTR_BUFFER:
                strRetVal = W("LPWSTR buffer");
                break;
            case MARSHAL_TYPE_LPSTR_BUFFER:
                strRetVal = W("LPSTR buffer");
                break;
            case MARSHAL_TYPE_UTF8_BUFFER:
                strRetVal = W("UTF8 buffer");
                break;
            case MARSHAL_TYPE_ASANYA:
                strRetVal = W("AsAnyA");
                break;
            case MARSHAL_TYPE_ASANYW:
                strRetVal = W("AsAnyW");
                break;
            case MARSHAL_TYPE_DELEGATE:
                strRetVal = W("Delegate");
                break;
            case MARSHAL_TYPE_BLITTABLEPTR:
                strRetVal = W("blittable pointer");
                break;
#ifdef FEATURE_COMINTEROP
            case MARSHAL_TYPE_VBBYVALSTR:
                strRetVal = W("VBByValStr");
                break;
            case MARSHAL_TYPE_VBBYVALSTRW:
                strRetVal = W("VBByRefStr");
                break;
#endif // FEATURE_COMINTEROP
            case MARSHAL_TYPE_LAYOUTCLASSPTR:
                strRetVal = W("Layout class pointer");
                break;
            case MARSHAL_TYPE_ARRAYWITHOFFSET:
                strRetVal = W("ArrayWithOffset");
                break;
            case MARSHAL_TYPE_BLITTABLEVALUECLASS:
                strRetVal = W("blittable value class");
                break;
            case MARSHAL_TYPE_VALUECLASS:
                strRetVal = W("value class");
                break;
            case MARSHAL_TYPE_ARGITERATOR:
                strRetVal = W("ArgIterator");
                break;
            case MARSHAL_TYPE_BLITTABLEVALUECLASSWITHCOPYCTOR:
                strRetVal = W("blittable value class with copy constructor");
                break;
#ifdef FEATURE_COMINTEROP
            case MARSHAL_TYPE_OBJECT:
                strRetVal = W("VARIANT");
                break;
#endif // FEATURE_COMINTEROP
            case MARSHAL_TYPE_HANDLEREF:
                strRetVal = W("HandleRef");
                break;
#ifdef FEATURE_COMINTEROP
            case MARSHAL_TYPE_OLECOLOR:
                strRetVal = W("OLE_COLOR");
                break;
#endif // FEATURE_COMINTEROP
            case MARSHAL_TYPE_RUNTIMETYPEHANDLE:
                strRetVal = W("RuntimeTypeHandle");
                break;
            case MARSHAL_TYPE_RUNTIMEFIELDHANDLE:
                strRetVal = W("RuntimeFieldHandle");
                break;
            case MARSHAL_TYPE_RUNTIMEMETHODHANDLE:
                strRetVal = W("RuntimeMethodHandle");
                break;
            default:
                strRetVal = W("<UNKNOWN>");
                break;
        }
    }

    strMarshalType.Set(strRetVal);
    return;
}

VOID MarshalInfo::VarTypeToString(VARTYPE vt, SString& strVarType)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    
    LPCWSTR strRetVal;
    
    switch(vt)
    {
        case VT_I2:
            strRetVal = W("2-byte signed int");
            break;
        case VT_I4:
            strRetVal = W("4-byte signed int");
            break;
        case VT_R4:
            strRetVal = W("4-byte real");
            break;
        case VT_R8:
            strRetVal = W("8-byte real");
            break;
        case VT_CY:
            strRetVal = W("currency");
            break;
        case VT_DATE:
            strRetVal = W("date");
            break;
        case VT_BSTR:
            strRetVal = W("binary string");
            break;
        case VT_DISPATCH:
            strRetVal = W("IDispatch *");
            break;
        case VT_ERROR:
            strRetVal = W("Scode");
            break;
        case VT_BOOL:
            strRetVal = W("boolean");
            break;
        case VT_VARIANT:
            strRetVal = W("VARIANT *");
            break;
        case VT_UNKNOWN:
            strRetVal = W("IUnknown *");
            break;
        case VT_DECIMAL:
            strRetVal = W("16-byte fixed point");
            break;
        case VT_RECORD:
            strRetVal = W("user defined structure");
            break;
        case VT_I1:
            strRetVal = W("signed char");
            break;
        case VT_UI1:
            strRetVal = W("unsigned char");
            break;
        case VT_UI2:
            strRetVal = W("unsigned short");
            break;
        case VT_UI4:
            strRetVal = W("unsigned short");
            break;
        case VT_INT:
            strRetVal = W("signed int");
            break;
        case VT_UINT:
            strRetVal = W("unsigned int");
            break;
        case VT_LPSTR:
            strRetVal = W("LPSTR");
            break;
        case VT_LPWSTR:
            strRetVal = W("LPWSTR");
            break;
        case VT_HRESULT:
            strRetVal = W("HResult");
            break;
        case VT_I8:
            strRetVal = W("8-byte signed int");
            break;
        case VT_NULL:
            strRetVal = W("null");
            break;
        case VT_UI8:
            strRetVal = W("8-byte unsigned int");
            break;
        case VT_VOID:
            strRetVal = W("void");
            break;
        case VTHACK_WINBOOL:
            strRetVal = W("boolean");
            break;
        case VTHACK_ANSICHAR:
            strRetVal = W("char");
            break;
        case VTHACK_CBOOL:
            strRetVal = W("1-byte C bool");
            break;
        default:
            strRetVal = W("unknown");
            break;
    }

    strVarType.Set(strRetVal);    
    return;
}

#endif // CROSSGEN_COMPILE

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

        case MARSHAL_TYPE_URI:
        case MARSHAL_TYPE_KEYVALUEPAIR:
        case MARSHAL_TYPE_NULLABLE:
        case MARSHAL_TYPE_SYSTEMTYPE:
        case MARSHAL_TYPE_EXCEPTION:
        case MARSHAL_TYPE_HIDDENLENGTHARRAY:
        case MARSHAL_TYPE_HSTRING:
        case MARSHAL_TYPE_NCCEVENTARGS:
        case MARSHAL_TYPE_PCEVENTARGS:
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

#ifdef FEATURE_COMINTEROP
MarshalInfo::MarshalType MarshalInfo::GetHiddenLengthParamMarshalType()
{
    LIMITED_METHOD_CONTRACT;
    return MARSHAL_TYPE_GENERIC_U4;
}

CorElementType MarshalInfo::GetHiddenLengthParamElementType()
{
    LIMITED_METHOD_CONTRACT;
    return ELEMENT_TYPE_U4;
}

UINT16 MarshalInfo::GetHiddenLengthParamStackSize()
{
    LIMITED_METHOD_CONTRACT;
    return StackElemSize(GetNativeSize(GetHiddenLengthParamMarshalType(), m_ms));
}

void MarshalInfo::MarshalHiddenLengthArgument(NDirectStubLinker *psl, BOOL managedToNative, BOOL isForReturnArray)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(psl));
        PRECONDITION(m_type == MARSHAL_TYPE_HIDDENLENGTHARRAY);
        PRECONDITION(m_dwHiddenLengthManagedHomeLocal == 0xFFFFFFFF);
        PRECONDITION(m_dwHiddenLengthNativeHomeLocal == 0xFFFFFFFF);
    }
    CONTRACTL_END;
        
    NewHolder<ILMarshaler> pHiddenLengthMarshaler = CreateILMarshaler(GetHiddenLengthParamMarshalType(), psl);


    ILCodeStream *pcsMarshal = psl->GetMarshalCodeStream();
    ILCodeStream *pcsUnmarshal = psl->GetUnmarshalCodeStream();

    pcsMarshal->EmitNOP("// hidden length argument { ");
    pcsUnmarshal->EmitNOP("// hidden length argument { ");

    DWORD dwMarshalFlags = MARSHAL_FLAG_HIDDENLENPARAM;
    if (isForReturnArray)
    {
        // This is a hidden length argument for an [out, retval] argument, so setup flags to match that
        dwMarshalFlags |= CalculateArgumentMarshalFlags(TRUE, FALSE, TRUE, managedToNative);
    }
    else
    {
        // The length parameter needs to be an [in] parameter if the array itself is an [in] parameter.
        // Additionally, in order to support the FillArray pattern:
        //   FillArray([in] UInt32 length, [out, size_is(length)] ElementType* value)
        //
        // We need to make sure that the length parameter is [in] if the array pointer is not byref, since
        // this means that the caller is allocating the array.  This includes array buffers which are [out]
        // but not byref, since the [out] marshaling applies to the array contents but not the array pointer
        // value itself.
        BOOL marshalHiddenLengthIn = m_in || !m_byref;
        dwMarshalFlags |= CalculateArgumentMarshalFlags(m_byref, marshalHiddenLengthIn, m_out, managedToNative);
    }
    pHiddenLengthMarshaler->EmitMarshalHiddenLengthArgument(pcsMarshal,
                                                            pcsUnmarshal,
                                                            this,
                                                            m_paramidx,
                                                            dwMarshalFlags,
                                                            HiddenLengthParamIndex(),
                                                            &m_args,
                                                            &m_dwHiddenLengthManagedHomeLocal,
                                                            &m_dwHiddenLengthNativeHomeLocal);

    pcsMarshal->EmitNOP("// } hidden length argument");
    pcsUnmarshal->EmitNOP("// } hidden length argument");

    // Only emit into the dispatch stream for CLR -> Native cases - in the reverse, there is no argument
    // to pass to the managed method.  Instead, the length is encoded in the marshaled array.
    if (managedToNative)
    {
        ILCodeStream* pcsDispatch = psl->GetDispatchCodeStream();
        pHiddenLengthMarshaler->EmitSetupArgument(pcsDispatch);
    }
}

#endif // FEATURE_COMINTEROP

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

void ArrayMarshalInfo::InitForHiddenLengthArray(TypeHandle thElement)
{
    STANDARD_VM_CONTRACT;
    
    MethodTable *pMT = NULL;

    // WinRT supports arrays of any WinRT-legal types
    if (thElement.IsArray())
    {
        ReportInvalidArrayMarshalInfo(IDS_EE_BADMARSHAL_NESTEDARRAY);
    }
    else if (thElement.IsTypeDesc() || !thElement.GetMethodTable()->IsLegalNonArrayWinRTType())
    {
        ReportInvalidArrayMarshalInfo(IDS_EE_BADMARSHAL_WINRT_ILLEGAL_TYPE);
    }

    m_thElement = thElement;

    pMT = thElement.GetMethodTable();
    if (pMT->IsString())
    {
        m_vtElement = VTHACK_HSTRING;
        m_cbElementSize = sizeof(HSTRING);
    }
    else if (WinRTTypeNameConverter::ResolveRedirectedType(pMT, &m_redirectedTypeIndex))
    {
        m_vtElement = VTHACK_REDIRECTEDTYPE;

        switch (m_redirectedTypeIndex)
        {
            case WinMDAdapter::RedirectedTypeIndex_System_DateTimeOffset:
                m_cbElementSize = ILDateTimeMarshaler::c_nativeSize;
                break;

            case WinMDAdapter::RedirectedTypeIndex_System_Type:
                m_cbElementSize = ILSystemTypeMarshaler::c_nativeSize;
                break;

            case WinMDAdapter::RedirectedTypeIndex_System_Exception:
                m_cbElementSize = ILHResultExceptionMarshaler::c_nativeSize;
                break;

                // WinRT delegates are IUnknown pointers
            case WinMDAdapter::RedirectedTypeIndex_System_EventHandlerGeneric:
                m_vtElement = VTHACK_INSPECTABLE;
                m_cbElementSize = sizeof(IUnknown*);
                break;

            case WinMDAdapter::RedirectedTypeIndex_System_Collections_Generic_KeyValuePair:
            case WinMDAdapter::RedirectedTypeIndex_System_Nullable:
            case WinMDAdapter::RedirectedTypeIndex_System_Uri:
            case WinMDAdapter::RedirectedTypeIndex_System_Collections_Specialized_NotifyCollectionChangedEventArgs:
            case WinMDAdapter::RedirectedTypeIndex_System_ComponentModel_PropertyChangedEventArgs:
            {
                m_cbElementSize = sizeof(IInspectable *);
                break;
            }

            default:
            {
                if (pMT->IsValueType())
                {
                    // other redirected structs are blittable and don't need special marshaling
                    m_vtElement = VTHACK_BLITTABLERECORD;
                    m_cbElementSize = pMT->GetNativeSize();
                }
                else
                {
                    // redirected interfaces should be treated as interface pointers
                    _ASSERTE(pMT->IsInterface());
                    m_vtElement = VTHACK_INSPECTABLE;
                    m_cbElementSize = sizeof(IInspectable *);
                }
                break;
            }
        }
    }
    else if (pMT->IsBlittable() || pMT->IsTruePrimitive() || pMT->IsEnum())
    {
        m_vtElement = VTHACK_BLITTABLERECORD;

        CorElementType elemType = pMT->GetInternalCorElementType();
        if (CorTypeInfo::IsPrimitiveType(elemType))
        {
            // .NET and WinRT primitives have the same size
            m_cbElementSize = CorTypeInfo::Size(elemType);
        }
        else
        {
            m_cbElementSize = pMT->GetNativeSize();
        }
    }
    else if (pMT->IsValueType())
    {
        m_vtElement = VTHACK_NONBLITTABLERECORD;
        m_cbElementSize = pMT->GetNativeSize();
    }
    else
    {
        m_vtElement = VTHACK_INSPECTABLE;
        m_cbElementSize = sizeof(IInspectable *);
    }

LExit:;
}
#endif // FEATURE_COMINTEROP

void ArrayMarshalInfo::InitElementInfo(CorNativeType arrayNativeType, MarshalInfo::MarshalScenario ms, TypeHandle thElement, CorNativeType ntElement, BOOL isAnsi)
{
    CONTRACT_VOID
    {
        STANDARD_VM_CHECK;
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
                    if (ms == MarshalInfo::MARSHAL_SCENARIO_COMINTEROP || IsAMIExport(m_flags))
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
        else if (m_thElement.CanCastTo(TypeHandle(MscorlibBinder::GetClass(CLASS__SAFE_HANDLE))))
        {
            // Array's of SAFEHANDLEs are not supported.
            ReportInvalidArrayMarshalInfo(IDS_EE_BADMARSHAL_SAFEHANDLEARRAY);
        }
        else if (m_thElement.CanCastTo(TypeHandle(MscorlibBinder::GetClass(CLASS__CRITICAL_HANDLE))))
        {
            // Array's of CRITICALHANDLEs are not supported.
            ReportInvalidArrayMarshalInfo(IDS_EE_BADMARSHAL_CRITICALHANDLEARRAY);
        }
        else if (etElement == ELEMENT_TYPE_VALUETYPE) 
        {
            if (m_thElement == TypeHandle(MscorlibBinder::GetClass(CLASS__DATE_TIME)))
            {
                if (ntElement == NATIVE_TYPE_STRUCT || ntElement == NATIVE_TYPE_DEFAULT)
                    m_vtElement = VT_DATE;
                else
                    ReportInvalidArrayMarshalInfo(IDS_EE_BADMARSHAL_DATETIMEARRAY);
            }
            else if (m_thElement == TypeHandle(MscorlibBinder::GetClass(CLASS__DECIMAL)))
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
                // When exporting, we need to handle enums specially.
                if (IsAMIExport(m_flags) && m_thElement.IsEnum())
                {
                    // Get the element type of the underlying type.
                    CorElementType et = m_thElement.GetInternalCorElementType();
                    
                    // If it is not a 32-bit type, convert as the underlying type.
                    if ((et == ELEMENT_TYPE_I4) || (et == ELEMENT_TYPE_U4))
                        m_vtElement = VT_RECORD;             
                    else
                        m_vtElement = OleVariant::GetVarTypeForTypeHandle(m_thElement);
                }             
                else
                {                   
                    m_vtElement = OleVariant::GetVarTypeForTypeHandle(m_thElement);
                }
            }
        }
#ifdef FEATURE_COMINTEROP
        else if (m_thElement == TypeHandle(MscorlibBinder::GetClass(CLASS__ERROR_WRAPPER)))
        {
            m_vtElement = VT_ERROR;
        }
#endif
        else
        {
#ifdef FEATURE_COMINTEROP

            // Compat: Even if the classes have layout, we still convert them to interface pointers.

            ItfMarshalInfo itfInfo;
            MarshalInfo::GetItfMarshalInfo(m_thElement, TypeHandle(), FALSE, FALSE, ms, &itfInfo);

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

   // Avoid throwing exceptions for any managed structs that have layouts and have types of fields that gets default to those banned types by default 
   // We don't know if they will be passed to native code anyway, and the right place to make the check is in the marshallers
   if (AppX::IsAppXProcess() && ms != MarshalInfo::MARSHAL_SCENARIO_FIELD)
    {
       bool set_error = false;
       UINT m_resID = 0;  
       switch (m_vtElement)
       {
           case VT_DISPATCH:
                 m_resID = IDS_EE_BADMARSHAL_TYPE_IDISPATCH ;
                 set_error = true;
                break;
       }
        if (set_error)
            COMPlusThrow(kPlatformNotSupportedException, m_resID);
    }

    // If we are exporting, we need to substitute the VTHACK_* VARTYPE with the actual
    // types as expressed in the type library.
    if (IsAMIExport(m_flags))
    {
        if (m_vtElement == VTHACK_ANSICHAR)
            m_vtElement = VT_UI1;
        else if (m_vtElement == VTHACK_WINBOOL)
            m_vtElement = VT_I4;
		else if (m_vtElement == VTHACK_CBOOL)
		    m_vtElement = VT_UI1;
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
