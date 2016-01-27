// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// NewMerger.cpp
// 

//
// contains utility code to MD directory
//
// This file provides Compiler Support functionality in metadata.
//*****************************************************************************
#include "stdafx.h"

#include "newmerger.h"
#include "regmeta.h"


#include "importhelper.h"
#include "rwutil.h"
#include "mdlog.h"
#include <posterror.h>
#include <sstring.h>
#include "ndpversion.h"

#ifdef FEATURE_METADATA_EMIT_ALL

#define MODULEDEFTOKEN         TokenFromRid(1, mdtModule)

#define COR_MSCORLIB_NAME "mscorlib"
#define COR_MSCORLIB_TYPEREF {0xb7, 0x7a, 0x5c, 0x56,0x19,0x34,0xe0,0x89}

#define COR_CONSTRUCTOR_METADATA_IDENTIFIER W(".ctor")

#define COR_COMPILERSERVICE_NAMESPACE "System.Runtime.CompilerServices"
#define COR_EXCEPTIONSERVICE_NAMESPACE "System.Runtime.ExceptionServices"
#define COR_SUPPRESS_MERGE_CHECK_ATTRIBUTE "SuppressMergeCheckAttribute"
#define COR_HANDLE_PROCESS_CORRUPTED_STATE_EXCEPTION_ATTRIBUTE "HandleProcessCorruptedStateExceptionsAttribute"
#define COR_MISCBITS_NAMESPACE "Microsoft.VisualC"
#define COR_MISCBITS_ATTRIBUTE "Microsoft.VisualC.MiscellaneousBitsAttribute"
#define COR_NATIVECPPCLASS_ATTRIBUTE "System.Runtime.CompilerServices.NativeCppClassAttribute"

// MODULE_CA_LOCATION W("System.Runtime.CompilerServices.AssemblyAttributesGoHere")
#define MODULE_CA_TYPENAME "AssemblyAttributesGoHere" // fake assembly type-ref for hanging Assembly-level CAs off of

//*****************************************************************************
// BEGIN: Security Critical Attributes and Enumeration
//*****************************************************************************
#define COR_SECURITYCRITICALSCOPE_ENUM_W            W("System.Security.SecurityCriticalScope")

#define COR_SECURITYCRITICAL_ATTRIBUTE_FULL "System.Security.SecurityCriticalAttribute"
#define COR_SECURITYTRANSPARENT_ATTRIBUTE_FULL  "System.Security.SecurityTransparentAttribute"
#define COR_SECURITYTREATASSAFE_ATTRIBUTE_FULL  "System.Security.SecurityTreatAsSafeAttribute"

#define COR_SECURITYCRITICAL_ATTRIBUTE_FULL_W W("System.Security.SecurityCriticalAttribute")
#define COR_SECURITYTRANSPARENT_ATTRIBUTE_FULL_W    W("System.Security.SecurityTransparentAttribute")
#define COR_SECURITYTREATASSAFE_ATTRIBUTE_FULL_W W("System.Security.SecurityTreatAsSafeAttribute")
#define COR_SECURITYSAFECRITICAL_ATTRIBUTE_FULL_W W("System.Security.SecuritySafeCriticalAttribute")

    // definitions of enumeration for System.Security.SecurityCriticalScope (Explicit or Everything)
#define COR_SECURITYCRITICAL_CTOR_ARGCOUNT_NO_SCOPE         0
#define COR_SECURITYCRITICAL_CTOR_ARGCOUNT_SCOPE_EVERYTHING 1
#define COR_SECURITYCRITICAL_CTOR_NO_SCOPE_SIG_MAX_SIZE     (3)
#define COR_SECURITYCRITICAL_CTOR_SCOPE_SIG_MAX_SIZE        (5 + sizeof(mdTypeRef) * 1)

#define COR_SECURITYCRITICAL_ATTRIBUTE_NAMESPACE "System.Security"
#define COR_SECURITYCRITICAL_ATTRIBUTE "SecurityCriticalAttribute" 
#define COR_SECURITYTRANSPARENT_ATTRIBUTE_NAMESPACE "System.Security"
#define COR_SECURITYTRANSPARENT_ATTRIBUTE "SecurityTransparentAttribute" 
#define COR_SECURITYTREATASSAFE_ATTRIBUTE_NAMESPACE "System.Security"
#define COR_SECURITYTREATASSAFE_ATTRIBUTE "SecurityTreatAsSafeAttribute"
#define COR_SECURITYSAFECRITICAL_ATTRIBUTE "SecuritySafeCriticalAttribute"


#define COR_SECURITYCRITICAL_ATTRIBUTE_VALUE_EVERYTHING { 0x01, 0x00 ,0x01, 0x00, 0x00, 0x00 ,0x00, 0x00 }
#define COR_SECURITYCRITICAL_ATTRIBUTE_VALUE_EXPLICIT {0x01, 0x00, 0x00 ,0x00}
#define COR_SECURITYTREATASSAFE_ATTRIBUTE_VALUE {0x01, 0x00, 0x00 ,0x00}


    // if true, then registry has been read for enabling or disabling SecurityCritical support
static BOOL g_fRefShouldMergeCriticalChecked = FALSE;

// by default, security critical attributes will be merged (e.g. unmarked CRT marked Critical/TAS)
//   - unless registry config explicitly disables merging
static BOOL g_fRefShouldMergeCritical = TRUE;
//*****************************************************************************
// END: Security Critical Attributes and Enumeration
//*****************************************************************************

//*****************************************************************************
// Checks to see if the given type is managed or native. We'll key off of the
// Custom Attribute "Microsoft.VisualC.MiscellaneousBitsAttribute". If the third
// byte has the 01000000 bit set then it is an unmanaged type.
// If we can't find the attribute, we will also check for the presence of the
// "System.Runtime.CompilerServices.NativeCppClassAttribute" Custom Attribute
// since the CPP compiler stopped emitting MiscellaneousBitsAttribute in Dev11.
//*****************************************************************************
HRESULT IsManagedType(CMiniMdRW* pMiniMd, 
                          mdTypeDef td, 
                          BOOL *fIsManagedType)
{
    // First look for the custom attribute
    HENUMInternal hEnum;
    HRESULT hr = S_OK;
    
    IfFailRet(pMiniMd->CommonEnumCustomAttributeByName(td, COR_MISCBITS_ATTRIBUTE, false, &hEnum));

    // If there aren't any custom attributes here, then this must be a managed type
    if (hEnum.m_ulCount > 0)
    {
        // Let's loop through these, and see if any of them have that magical bit set.
        mdCustomAttribute ca;
        CustomAttributeRec *pRec;
        ULONG cbData = 0;
        
        while(HENUMInternal::EnumNext(&hEnum, &ca))
        {
            const BYTE* pData = NULL;
            
            IfFailGo(pMiniMd->GetCustomAttributeRecord(RidFromToken(ca), &pRec));
            IfFailGo(pMiniMd->getValueOfCustomAttribute(pRec, &pData, &cbData));

            if (pData != NULL && cbData >=3)
            {
                // See if the magical bit is set to make this an unmanaged type
                if ((*(pData+2)&0x40) > 0)
                {
                    // Yes, this is an unmanaged type
                    HENUMInternal::ClearEnum(&hEnum);
                    *fIsManagedType = FALSE;
                    return S_OK;
                }
            }
        }
                
    }

    HENUMInternal::ClearEnum(&hEnum);
    
    // If this was emitted by a Dev11+ CPP compiler, we only have NativeCppClassAttribute
    // so let's check for that before calling this a managed class.
    IfFailRet(pMiniMd->CommonEnumCustomAttributeByName(td, COR_NATIVECPPCLASS_ATTRIBUTE, false, &hEnum));
    if (hEnum.m_ulCount > 0)
    {
        // Yes, this is an unmanaged type
        HENUMInternal::ClearEnum(&hEnum);
        *fIsManagedType = FALSE;
        return S_OK;
    }

    // Nope, this isn't an unmanaged type.... must be managed
    HENUMInternal::ClearEnum(&hEnum);
    *fIsManagedType = TRUE;
    hr = S_OK;
ErrExit:
    return hr;
}// IsManagedType


//*****************************************************************************
// "Is CustomAttribute from certain namespace and assembly" check helper
// Returns S_OK and fills **ppTypeRefRec.
// Returns error code or S_FALSE otherwise as not found and fills **ppTypeRefRec with NULL.
//*****************************************************************************
HRESULT IsAttributeFromNamespace(
    CMiniMdRW   *pMiniMd, 
    mdToken      tk, 
    LPCSTR       szNamespace, 
    LPCSTR       szAssembly, 
    TypeRefRec **ppTypeRefRec)
{
    HRESULT hr = S_OK;
    if(TypeFromToken(tk) == mdtMemberRef)
    {
        MemberRefRec *pMemRefRec;
        IfFailGo(pMiniMd->GetMemberRefRecord(RidFromToken(tk), &pMemRefRec));
        tk = pMiniMd->getClassOfMemberRef(pMemRefRec);
    }
    if(TypeFromToken(tk) == mdtTypeRef)
    {
        TypeRefRec *pTypeRefRec;
        IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(tk), &pTypeRefRec));
        LPCSTR szTypeRefNamespace;
        IfFailGo(pMiniMd->getNamespaceOfTypeRef(pTypeRefRec, &szTypeRefNamespace));
        if (strcmp(szTypeRefNamespace, szNamespace) == 0)
        {
            mdToken tkResTmp = pMiniMd->getResolutionScopeOfTypeRef(pTypeRefRec);
            if (TypeFromToken(tkResTmp) == mdtAssemblyRef)
            {
                AssemblyRefRec *pAsmRefRec;
                IfFailGo(pMiniMd->GetAssemblyRefRecord(RidFromToken(tkResTmp), &pAsmRefRec));
                LPCSTR szAssemblyRefName;
                IfFailGo(pMiniMd->getNameOfAssemblyRef(pAsmRefRec, &szAssemblyRefName));
                if(SString::_stricmp(szAssemblyRefName, szAssembly) == 0)
                {
                    *ppTypeRefRec = pTypeRefRec;
                    return S_OK;
                }
            }
        }
    }
    // Record not found
    hr = S_FALSE;
ErrExit:
    *ppTypeRefRec = NULL;
    return hr;
}

//*****************************************************************************
// constructor
//*****************************************************************************
NEWMERGER::NEWMERGER()
 :  m_pRegMetaEmit(0),
    m_pImportDataList(NULL),
    m_optimizeRefToDef(MDRefToDefDefault),
    m_isscsSecurityCritical(ISSCS_Unknown),
    m_isscsSecurityCriticalAllScopes(~ISSCS_Unknown)
{
    m_pImportDataTail = &(m_pImportDataList);
#if _DEBUG
    m_iImport = 0;
#endif // _DEBUG
} // NEWMERGER::NEWMERGER()

//*****************************************************************************
// initializer
//*****************************************************************************
HRESULT NEWMERGER::Init(RegMeta *pRegMeta) 
{
    HRESULT hr = NOERROR;
    MergeTypeData * pMTD;

    m_pRegMetaEmit = pRegMeta;

    // burn an entry so that the RID matches the array index
    IfNullGo(pMTD = m_rMTDs.Append());

    pMTD->m_bSuppressMergeCheck = false;
    pMTD->m_cMethods = 0;
    pMTD->m_cFields = 0;
    pMTD->m_cEvents = 0;
    pMTD->m_cProperties = 0;

ErrExit:
    return hr;
} // NEWMERGER::Init

//*****************************************************************************
// destructor
//*****************************************************************************
NEWMERGER::~NEWMERGER()
{
    if (m_pImportDataList)
    {
        // delete this list and release all AddRef'ed interfaces!
        MergeImportData *pNext;
        for (pNext = m_pImportDataList; pNext != NULL; )
        {
            pNext = m_pImportDataList->m_pNextImportData;
            if (m_pImportDataList->m_pHandler)
                m_pImportDataList->m_pHandler->Release();
            if (m_pImportDataList->m_pHostMapToken)
                m_pImportDataList->m_pHostMapToken->Release();
            if (m_pImportDataList->m_pMDTokenMap)
                delete m_pImportDataList->m_pMDTokenMap;
            m_pImportDataList->m_pRegMetaImport->Release();
            delete m_pImportDataList;
            m_pImportDataList = pNext;
        }
    }
} // NEWMERGER::~NEWMERGER

//*****************************************************************************
CMiniMdRW *NEWMERGER::GetMiniMdEmit() 
{
    return &(m_pRegMetaEmit->m_pStgdb->m_MiniMd); 
} // CMiniMdRW *NEWMERGER::GetMiniMdEmit()

//*****************************************************************************
// Adding a new import
//*****************************************************************************
HRESULT NEWMERGER::AddImport(
    IMetaDataImport2 *pImport,              // [IN] The scope to be merged.
    IMapToken   *pHostMapToken,             // [IN] Host IMapToken interface to receive token remap notification
    IUnknown    *pHandler)                  // [IN] An object to receive error notification.
{
    HRESULT             hr = NOERROR;
    MergeImportData     *pData;

    RegMeta     *pRM = static_cast<RegMeta*>(pImport);

    // Add a MergeImportData to track the information for this import scope
    pData = new (nothrow) MergeImportData;
    IfNullGo( pData );
    pData->m_pRegMetaImport = pRM;
    pData->m_pRegMetaImport->AddRef();
    pData->m_pHostMapToken = pHostMapToken;
    if (pData->m_pHostMapToken)
        pData->m_pHostMapToken->AddRef();
    if (pHandler)
    {
        pData->m_pHandler = pHandler;
        pData->m_pHandler->AddRef();
    }
    else
    {
        pData->m_pHandler = NULL;
    }

    pData->m_pMDTokenMap = NULL;
    pData->m_pNextImportData = NULL;
#if _DEBUG
    pData->m_iImport = ++m_iImport;
#endif // _DEBUG

    pData->m_tkHandleProcessCorruptedStateCtor = mdTokenNil;
    // add the newly create node to the tail of the list
    *m_pImportDataTail = pData;
    m_pImportDataTail = &(pData->m_pNextImportData);

ErrExit:

    return hr;
} // HRESULT NEWMERGER::AddImport()

HRESULT NEWMERGER::InitMergeTypeData() 
{
    CMiniMdRW   *pMiniMdEmit;
    ULONG       cTypeDefRecs;
    ULONG       i, j;
    bool        bSuppressMergeCheck;

    ULONG       ridStart, ridEnd;
    RID         ridMap;

    mdToken     tkSuppressMergeCheckCtor = mdTokenNil;
    mdToken     tkCA;
    mdMethodDef mdEmit;
    mdFieldDef  fdEmit;
    mdEvent     evEmit;
    mdProperty  prEmit;

    TypeDefRec  *pTypeDefRec;
    EventMapRec  *pEventMapRec;
    PropertyMapRec *pPropertyMapRec;

    MergeTypeData *pMTD;

    HRESULT     hr = NOERROR;

    pMiniMdEmit = GetMiniMdEmit();

    // cache the SuppressMergeCheckAttribute.ctor token
    ImportHelper::FindCustomAttributeCtorByName(
            pMiniMdEmit, COR_MSCORLIB_NAME, 
            COR_COMPILERSERVICE_NAMESPACE, COR_SUPPRESS_MERGE_CHECK_ATTRIBUTE, 
            &tkSuppressMergeCheckCtor);

    cTypeDefRecs = pMiniMdEmit->getCountTypeDefs();
    _ASSERTE(m_rMTDs.Count() > 0);

    for (i = m_rMTDs.Count(); i <= cTypeDefRecs; i++)
    { 
        IfNullGo(pMTD = m_rMTDs.Append());

        pMTD->m_cMethods = 0;
        pMTD->m_cFields = 0;
        pMTD->m_cEvents = 0;
        pMTD->m_cProperties = 0;
        pMTD->m_bSuppressMergeCheck = (tkSuppressMergeCheckCtor != mdTokenNil) && 
            (S_OK == ImportHelper::FindCustomAttributeByToken(pMiniMdEmit, 
                TokenFromRid(i, mdtTypeDef), tkSuppressMergeCheckCtor, 
                NULL, 0, &tkCA));

        IfFailGo(pMiniMdEmit->GetTypeDefRecord(i, &pTypeDefRec));

        // Count the number methods
        ridStart = pMiniMdEmit->getMethodListOfTypeDef(pTypeDefRec);
        IfFailGo(pMiniMdEmit->getEndMethodListOfTypeDef(i, &ridEnd));

        for (j = ridStart; j < ridEnd; j++)
        {
            IfFailGo(pMiniMdEmit->GetMethodRid(j, (ULONG *)&mdEmit));
            bSuppressMergeCheck = pMTD->m_bSuppressMergeCheck || 
               ((tkSuppressMergeCheckCtor != mdTokenNil) && 
                (S_OK == ImportHelper::FindCustomAttributeByToken(pMiniMdEmit, 
                    mdEmit, tkSuppressMergeCheckCtor, NULL, 0, &tkCA)));

            if (!bSuppressMergeCheck) 
            {
                pMTD->m_cMethods++;
            }
        }

        // Count the number fields
        ridStart = pMiniMdEmit->getFieldListOfTypeDef(pTypeDefRec);
        IfFailGo(pMiniMdEmit->getEndFieldListOfTypeDef(i, &ridEnd));

        for (j = ridStart; j < ridEnd; j++)
        {
            IfFailGo(pMiniMdEmit->GetFieldRid(j, (ULONG *)&fdEmit));
            bSuppressMergeCheck = pMTD->m_bSuppressMergeCheck || 
               ((tkSuppressMergeCheckCtor != mdTokenNil) && 
                (S_OK == ImportHelper::FindCustomAttributeByToken(pMiniMdEmit, 
                    fdEmit, tkSuppressMergeCheckCtor, NULL, 0, &tkCA)));

            if (!bSuppressMergeCheck) 
            {
                pMTD->m_cFields++;
            }
        }

        // Count the number of events
        IfFailGo(pMiniMdEmit->FindEventMapFor(i, &ridMap));
        if (!InvalidRid(ridMap)) 
        {
            IfFailGo(pMiniMdEmit->GetEventMapRecord(ridMap, &pEventMapRec));
            ridStart = pMiniMdEmit->getEventListOfEventMap(pEventMapRec);
            IfFailGo(pMiniMdEmit->getEndEventListOfEventMap(ridMap, &ridEnd));

            for (j = ridStart; j < ridEnd; j++)
            {
                IfFailGo(pMiniMdEmit->GetEventRid(j, (ULONG *)&evEmit));
                bSuppressMergeCheck = pMTD->m_bSuppressMergeCheck || 
                    ((tkSuppressMergeCheckCtor != mdTokenNil) && 
                    (S_OK == ImportHelper::FindCustomAttributeByToken(pMiniMdEmit, 
                        evEmit, tkSuppressMergeCheckCtor, NULL, 0, &tkCA)));

                if (!bSuppressMergeCheck) 
                {
                    pMTD->m_cEvents++;
                }
            }
        }

        // Count the number of properties
        IfFailGo(pMiniMdEmit->FindPropertyMapFor(i, &ridMap));
        if (!InvalidRid(ridMap)) 
        {
            IfFailGo(pMiniMdEmit->GetPropertyMapRecord(ridMap, &pPropertyMapRec));
            ridStart = pMiniMdEmit->getPropertyListOfPropertyMap(pPropertyMapRec);
            IfFailGo(pMiniMdEmit->getEndPropertyListOfPropertyMap(ridMap, &ridEnd));

            for (j = ridStart; j < ridEnd; j++)
            {
                IfFailGo(pMiniMdEmit->GetPropertyRid(j, (ULONG *)&prEmit));
                bSuppressMergeCheck = pMTD->m_bSuppressMergeCheck || 
                    ((tkSuppressMergeCheckCtor != mdTokenNil) && 
                    (S_OK == ImportHelper::FindCustomAttributeByToken(pMiniMdEmit, 
                        prEmit, tkSuppressMergeCheckCtor, NULL, 0, &tkCA)));

                if (!bSuppressMergeCheck) 
                {
                    pMTD->m_cProperties++;
                }
            }
        }
    }

ErrExit:
    return hr;
}

//*****************************************************************************
// Merge now
//*****************************************************************************
HRESULT NEWMERGER::Merge(MergeFlags dwMergeFlags, CorRefToDefCheck optimizeRefToDef)
{
    MergeImportData     *pImportData = m_pImportDataList;
    MDTOKENMAP          **pPrevMap = NULL;
    MDTOKENMAP          *pMDTokenMap;
    HRESULT             hr = NOERROR;
    MDTOKENMAP          *pCurTKMap;
    int                 i;

#if _DEBUG
    {
    LOG((LOGMD, "++++++++++++++++++++++++++++++++++++++++++++++++++++++++\n"));
    LOG((LOGMD, "Merge scope list\n"));
    i = 0;
    for (MergeImportData *pID = m_pImportDataList; pID != NULL; pID = pID->m_pNextImportData)
    {
        WCHAR szScope[1024], szGuid[40];
        GUID mvid;
        ULONG cchScope;
        pID->m_pRegMetaImport->GetScopeProps(szScope, 1024, &cchScope, &mvid);
        szScope[1023] = 0;
        GuidToLPWSTR(mvid, szGuid, 40);
        ++i; // Counter is 1-based.
        LOG((LOGMD, "%3d: %ls : %ls\n", i, szGuid, szScope));
    }
    LOG((LOGMD, "++++++++++++++++++++++++++++++++++++++++++++++++++++++++\n"));
    }
#endif // _DEBUG
    
    m_dwMergeFlags = dwMergeFlags;
    m_optimizeRefToDef = optimizeRefToDef;

    // check to see if we need to do dup check
    m_fDupCheck = ((m_dwMergeFlags & NoDupCheck) != NoDupCheck);

    while (pImportData)
    {
        // Verify that we have a filter for each import scope.
        IfNullGo( pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd.GetFilterTable() );

        // cache the SuppressMergeCheckAttribute.ctor token for each import scope
        ImportHelper::FindCustomAttributeCtorByName(
                &pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd, COR_MSCORLIB_NAME, 
                COR_COMPILERSERVICE_NAMESPACE, COR_SUPPRESS_MERGE_CHECK_ATTRIBUTE, 
                &pImportData->m_tkSuppressMergeCheckCtor);

        // cache the HandleProcessCorruptedStateExceptionsAttribute.ctor token for each import scope
        ImportHelper::FindCustomAttributeCtorByName(
                &pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd, COR_MSCORLIB_NAME, 
                COR_EXCEPTIONSERVICE_NAMESPACE, COR_HANDLE_PROCESS_CORRUPTED_STATE_EXCEPTION_ATTRIBUTE, 
                &pImportData->m_tkHandleProcessCorruptedStateCtor);

        // check for security critical attribute in the assembly (i.e. explicit annotations)
        InputScopeSecurityCriticalStatus isscsTemp = CheckInputScopeIsCritical(pImportData, hr);
        IfFailGo(hr);
            // clear the unset flag bits (e.g. if critical, clear transparent bit)
            // whatever bits remain are bits that have been set in all scopes
        if (ISSCS_Unknown == (isscsTemp & ISSCS_SECURITYCRITICAL_FLAGS))
            m_isscsSecurityCriticalAllScopes &= ISSCS_SECURITYCRITICAL_LEGACY;
        else
            m_isscsSecurityCriticalAllScopes &= isscsTemp;
            // set the flag bits (essentially, this allows us to see if _any_ scopes requested a bit)
        m_isscsSecurityCritical |= isscsTemp;
        
        // create the tokenmap class to track metadata token remap for each import scope
        pMDTokenMap = new (nothrow) MDTOKENMAP;
        IfNullGo(pMDTokenMap);
        IfFailGo(pMDTokenMap->Init((IMetaDataImport2*)pImportData->m_pRegMetaImport));
        pImportData->m_pMDTokenMap = pMDTokenMap;
        pImportData->m_pMDTokenMap->m_pMap = pImportData->m_pHostMapToken;
        if (pImportData->m_pHostMapToken)
            pImportData->m_pHostMapToken->AddRef();
        pImportData->m_pMDTokenMap->m_pNextMap = NULL;
        if (pPrevMap)
            *pPrevMap = pImportData->m_pMDTokenMap;
        pPrevMap = &(pImportData->m_pMDTokenMap->m_pNextMap);
        pImportData = pImportData->m_pNextImportData;
    }

    // Populate the m_rMTDs with the type info already defined in the emit scope
    IfFailGo( InitMergeTypeData() );

    // 1. Merge Module
    IfFailGo( MergeModule( ) );

    // 2. Merge TypeDef partially (i.e. only name)
    IfFailGo( MergeTypeDefNamesOnly() );

    // 3. Merge ModuleRef property and do ModuleRef to ModuleDef optimization
    IfFailGo( MergeModuleRefs() );

    // 4. Merge AssemblyRef. 
    IfFailGo( MergeAssemblyRefs() );

    // 5. Merge TypeRef with TypeRef to TypeDef optimization
    IfFailGo( MergeTypeRefs() );

    // 6. Merge TypeSpec & MethodSpec
    IfFailGo( MergeTypeSpecs() );

    // 7. Now Merge the remaining of TypeDef records
    IfFailGo( CompleteMergeTypeDefs() );

    // 8. Merge Methods and Fields. Such that Signature translation is respecting the TypeRef to TypeDef optimization.
    IfFailGo( MergeTypeDefChildren() );

    // 9. Merge MemberRef with MemberRef to MethodDef/FieldDef optimization
    IfFailGo( MergeMemberRefs( ) );

    // 10. Merge InterfaceImpl
    IfFailGo( MergeInterfaceImpls( ) );

    // merge all of the remaining in metadata ....

    // 11. constant has dependency on property, field, param
    IfFailGo( MergeConstants() );

    // 12. field marshal has dependency on param and field
    IfFailGo( MergeFieldMarshals() );

    // 13. in ClassLayout, move over the FieldLayout and deal with FieldLayout as well
    IfFailGo( MergeClassLayouts() );

    // 14. FieldLayout has dependency on FieldDef.
    IfFailGo( MergeFieldLayouts() );

    // 15. FieldRVA has dependency on FieldDef.
    IfFailGo( MergeFieldRVAs() );
        
    // 16. MethodImpl has dependency on MemberRef, MethodDef, TypeRef and TypeDef.
    IfFailGo( MergeMethodImpls() );

    // 17. pinvoke depends on MethodDef and ModuleRef
    IfFailGo( MergePinvoke() );

    IfFailGo( MergeStandAloneSigs() );

    IfFailGo( MergeMethodSpecs() );

    IfFailGo( MergeStrings() );

    if (m_dwMergeFlags & MergeManifest)
    {
        // keep the manifest!!
        IfFailGo( MergeAssembly() );
        IfFailGo( MergeFiles() );
        IfFailGo( MergeExportedTypes() );
        IfFailGo( MergeManifestResources() );
    }
    else if (m_dwMergeFlags & ::MergeExportedTypes)
    {
        IfFailGo( MergeFiles() );
        IfFailGo( MergeExportedTypes() );
    }

    IfFailGo( MergeCustomAttributes() );
    IfFailGo( MergeDeclSecuritys() );


    // Please don't add any MergeXxx() below here.  CustomAttributess must be
    // very late, because custom values are various other types.

    // Fixup list cannot be merged. Linker will need to re-emit them.

    // Now call back to host for the result of token remap
    // 
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // Send token remap information for each import scope
        pCurTKMap = pImportData->m_pMDTokenMap;
        TOKENREC    *pRec;
        if (pImportData->m_pHostMapToken)
        {
            for (i = 0; i < pCurTKMap->Count(); i++)
            {
                pRec = pCurTKMap->Get(i);
                if (!pRec->IsEmpty())
                    pImportData->m_pHostMapToken->Map(pRec->m_tkFrom, pRec->m_tkTo);
            }
        }
    }

    // And last, but not least, let's do Security critical module-level attribute consolidation
    //     and metadata fixups.
    IfFailGo( MergeSecurityCriticalAttributes() );





#if _DEBUG
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // dump the mapping
        LOG((LOGMD, "++++++++++++++++++++++++++++++++++++++++++++++++++++++++\n"));
        LOG((LOGMD, "Dumping token remap for one import scope!\n"));
        LOG((LOGMD, "This is the %d import scope for merge!\n", pImportData->m_iImport));

        pCurTKMap = pImportData->m_pMDTokenMap;
        TOKENREC    *pRec;
        for (i = 0; i < pCurTKMap->Count(); i++)
        {
            pRec = pCurTKMap->Get(i);
            if (!pRec->IsEmpty())
            {
                LOG((LOGMD, "   Token 0x%08x  ====>>>> Token 0x%08x\n", pRec->m_tkFrom, pRec->m_tkTo));
            }
        }
        LOG((LOGMD, "End dumping token remap!\n"));
        LOG((LOGMD, "++++++++++++++++++++++++++++++++++++++++++++++++++++++++\n"));
    }
#endif // _DEBUG

ErrExit:
    return hr;
} // HRESULT NEWMERGER::Merge()


//*****************************************************************************
// Merge ModuleDef
//*****************************************************************************
HRESULT NEWMERGER::MergeModule()
{
    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;
    HRESULT         hr = NOERROR;
    TOKENREC        *pTokenRec;

    // we don't really merge Module information but we create a one to one mapping for each module token into the TokenMap
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        // set the current MDTokenMap

        pCurTkMap = pImportData->m_pMDTokenMap;
        IfFailGo( pCurTkMap->InsertNotFound(MODULEDEFTOKEN, true, MODULEDEFTOKEN, &pTokenRec) );
    }
ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeModule()


//*****************************************************************************
// Merge TypeDef but only Names. This is a partial merge to support TypeRef to TypeDef optimization
//*****************************************************************************
HRESULT NEWMERGER::MergeTypeDefNamesOnly()
{
    HRESULT         hr = NOERROR;
    TypeDefRec      *pRecImport = NULL;
    TypeDefRec      *pRecEmit = NULL;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           iCount;
    ULONG           i;
    mdTypeDef       tdEmit;
    mdTypeDef       tdImport;
    bool            bDuplicate;
    DWORD           dwFlags;
    DWORD           dwExportFlags;
    NestedClassRec  *pNestedRec;
    RID             iNestedRec;
    mdTypeDef       tdNester;
    TOKENREC        *pTokenRec;

    LPCUTF8         szNameImp;
    LPCUTF8         szNamespaceImp;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    MergeTypeData   *pMTD;
    BOOL            bSuppressMergeCheck;
    mdCustomAttribute tkCA;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
        
        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;

        iCount = pMiniMdImport->getCountTypeDefs();

        // Merge the typedefs
        for (i = 1; i <= iCount; i++)
        {
            // only merge those TypeDefs that are marked
            if ( pMiniMdImport->GetFilterTable()->IsTypeDefMarked(TokenFromRid(i, mdtTypeDef)) == false)
                continue;

            // compare it with the emit scope
            IfFailGo(pMiniMdImport->GetTypeDefRecord(i, &pRecImport));
            IfFailGo(pMiniMdImport->getNameOfTypeDef(pRecImport, &szNameImp));
            IfFailGo(pMiniMdImport->getNamespaceOfTypeDef(pRecImport, &szNamespaceImp));

            // If the class is a Nested class, get the parent token.
            dwFlags = pMiniMdImport->getFlagsOfTypeDef(pRecImport);
            if (IsTdNested(dwFlags))
            {
                IfFailGo(pMiniMdImport->FindNestedClassHelper(TokenFromRid(i, mdtTypeDef), &iNestedRec));
                if (InvalidRid(iNestedRec))
                {
                    _ASSERTE(!"Bad state!");
                    IfFailGo(META_E_BADMETADATA);
                }
                else
                {
                    IfFailGo(pMiniMdImport->GetNestedClassRecord(iNestedRec, &pNestedRec));
                    tdNester = pMiniMdImport->getEnclosingClassOfNestedClass(pNestedRec);
                    _ASSERTE(!IsNilToken(tdNester));
                    IfFailGo(pCurTkMap->Remap(tdNester, &tdNester));
                }
            }
            else
                tdNester = mdTokenNil;

            bSuppressMergeCheck = (pImportData->m_tkSuppressMergeCheckCtor != mdTokenNil) && 
                (S_OK == ImportHelper::FindCustomAttributeByToken(pMiniMdImport, 
                    TokenFromRid(i, mdtTypeDef), pImportData->m_tkSuppressMergeCheckCtor, 
                    NULL, 0, &tkCA));
            
            // does this TypeDef already exist in the emit scope?
            if ( ImportHelper::FindTypeDefByName(
                pMiniMdEmit, 
                szNamespaceImp, 
                szNameImp, 
                tdNester, 
                &tdEmit) == S_OK )
            {
                // Yes, it does
                bDuplicate = true;

                // Let's look at their accessiblities.
                IfFailGo(pMiniMdEmit->GetTypeDefRecord(RidFromToken(tdEmit), &pRecEmit));
                dwExportFlags = pMiniMdEmit->getFlagsOfTypeDef(pRecEmit);

                // Managed types need to have the same accessiblity
                BOOL fManagedType = FALSE;
                IfFailGo(IsManagedType(pMiniMdImport, TokenFromRid(i, mdtTypeDef), &fManagedType));
                if (fManagedType)
                {
                    if ((dwFlags&tdVisibilityMask) != (dwExportFlags&tdVisibilityMask))
                    {
                        CheckContinuableErrorEx(META_E_MISMATCHED_VISIBLITY, pImportData, TokenFromRid(i, mdtTypeDef));
                    }

                }
                pMTD = m_rMTDs.Get(RidFromToken(tdEmit));
                if (pMTD->m_bSuppressMergeCheck != bSuppressMergeCheck)
                {
                    CheckContinuableErrorEx(META_E_MD_INCONSISTENCY, pImportData, TokenFromRid(i, mdtTypeDef));
                }
            }
            else
            {
                // No, it doesn't. Copy it over.
                bDuplicate = false;
                IfFailGo(pMiniMdEmit->AddTypeDefRecord(&pRecEmit, (RID *)&tdEmit));

                // make sure the index matches
                _ASSERTE(((mdTypeDef)m_rMTDs.Count()) == tdEmit);
                
                IfNullGo(pMTD = m_rMTDs.Append());

                pMTD->m_cMethods = 0;
                pMTD->m_cFields = 0;
                pMTD->m_cEvents = 0;
                pMTD->m_cProperties = 0;
                pMTD->m_bSuppressMergeCheck = bSuppressMergeCheck;

                tdEmit = TokenFromRid( tdEmit, mdtTypeDef );

                // Set Full Qualified Name.
                IfFailGo( CopyTypeDefPartially( pRecEmit, pMiniMdImport, pRecImport) );

                // Create a NestedClass record if the class is a Nested class.
                if (! IsNilToken(tdNester))
                {
                    IfFailGo(pMiniMdEmit->AddNestedClassRecord(&pNestedRec, &iNestedRec));

                    // copy over the information
                    IfFailGo( pMiniMdEmit->PutToken(TBL_NestedClass, NestedClassRec::COL_NestedClass, 
                                                    pNestedRec, tdEmit));

                    // tdNester has already been remapped above to the Emit scope.
                    IfFailGo( pMiniMdEmit->PutToken(TBL_NestedClass, NestedClassRec::COL_EnclosingClass, 
                                                    pNestedRec, tdNester));
                    IfFailGo( pMiniMdEmit->AddNestedClassToHash(iNestedRec) );

                }
            }

            // record the token movement
            tdImport = TokenFromRid(i, mdtTypeDef);
            IfFailGo( pCurTkMap->InsertNotFound(tdImport, bDuplicate, tdEmit, &pTokenRec) );
        }
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeTypeDefNamesOnly()


//*****************************************************************************
// Merge EnclosingType tables
//*****************************************************************************
HRESULT NEWMERGER::CopyTypeDefPartially( 
    TypeDefRec  *pRecEmit,                  // [IN] the emit record to fill
    CMiniMdRW   *pMiniMdImport,             // [IN] the importing scope
    TypeDefRec  *pRecImp)                   // [IN] the record to import

{
    HRESULT     hr;
    LPCUTF8     szNameImp;
    LPCUTF8     szNamespaceImp;
    CMiniMdRW   *pMiniMdEmit = GetMiniMdEmit();

    IfFailGo(pMiniMdImport->getNameOfTypeDef(pRecImp, &szNameImp));
    IfFailGo(pMiniMdImport->getNamespaceOfTypeDef(pRecImp, &szNamespaceImp));

    IfFailGo( pMiniMdEmit->PutString( TBL_TypeDef, TypeDefRec::COL_Name, pRecEmit, szNameImp) );
    IfFailGo( pMiniMdEmit->PutString( TBL_TypeDef, TypeDefRec::COL_Namespace, pRecEmit, szNamespaceImp) );

    pRecEmit->SetFlags(pRecImp->GetFlags());

    // Don't copy over the extends until TypeRef's remap is calculated

ErrExit:
    return hr;

} // HRESULT NEWMERGER::CopyTypeDefPartially()


//*****************************************************************************
// Merge ModuleRef tables including ModuleRef to ModuleDef optimization
//*****************************************************************************
HRESULT NEWMERGER::MergeModuleRefs()
{
    HRESULT         hr = NOERROR;
    ModuleRefRec    *pRecImport = NULL;
    ModuleRefRec    *pRecEmit = NULL;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           iCount;
    ULONG           i;
    mdModuleRef     mrEmit;
    bool            bDuplicate = false;
    TOKENREC        *pTokenRec;
    LPCUTF8         szNameImp;
    bool            isModuleDef;

    MergeImportData *pImportData;
    MergeImportData *pData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountModuleRefs();

        // loop through all ModuleRef
        for (i = 1; i <= iCount; i++)
        {
            // only merge those ModuleRefs that are marked
            if ( pMiniMdImport->GetFilterTable()->IsModuleRefMarked(TokenFromRid(i, mdtModuleRef)) == false)
                continue;

            isModuleDef = false;

            // compare it with the emit scope
            IfFailGo(pMiniMdImport->GetModuleRefRecord(i, &pRecImport));
            IfFailGo(pMiniMdImport->getNameOfModuleRef(pRecImport, &szNameImp));

            // Only do the ModuleRef to ModuleDef optimization if ModuleRef's name is meaningful!
            if ( szNameImp && szNameImp[0] != '\0')
            {

                // Check to see if this ModuleRef has become the ModuleDef token
                for (pData = m_pImportDataList; pData != NULL; pData = pData->m_pNextImportData)
                {
                    CMiniMdRW       *pMiniMd = &(pData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
                    ModuleRec       *pRec;
                    LPCUTF8         szName;

                    IfFailGo(pMiniMd->GetModuleRecord(MODULEDEFTOKEN, &pRec));
                    IfFailGo(pMiniMd->getNameOfModule(pRec, &szName));
                    if (szName && szName[0] != '\0' && strcmp(szNameImp, szName) == 0)
                    {
                        // We found an import Module for merging that has the same name as the ModuleRef
                        isModuleDef = true;
                        bDuplicate = true;
                        mrEmit = MODULEDEFTOKEN;       // set the resulting token to ModuleDef Token
                        break;
                    }
                }
            }

            if (isModuleDef == false)
            {
                // does this ModuleRef already exist in the emit scope?
                hr = ImportHelper::FindModuleRef(pMiniMdEmit, 
                                                szNameImp, 
                                                &mrEmit);
                if (hr == S_OK)
                {
                    // Yes, it does
                    bDuplicate = true;
                }
                else if (hr == CLDB_E_RECORD_NOTFOUND)
                {
                    // No, it doesn't. Copy it over.
                    bDuplicate = false;
                    IfFailGo(pMiniMdEmit->AddModuleRefRecord(&pRecEmit, (RID*)&mrEmit));
                    mrEmit = TokenFromRid(mrEmit, mdtModuleRef);

                    // Set ModuleRef Name.
                    IfFailGo( pMiniMdEmit->PutString(TBL_ModuleRef, ModuleRefRec::COL_Name, pRecEmit, szNameImp) );
                }
                else
                    IfFailGo(hr);
            }

            // record the token movement
            IfFailGo( pCurTkMap->InsertNotFound(
                TokenFromRid(i, mdtModuleRef), 
                bDuplicate, 
                mrEmit, 
                &pTokenRec) );
        }
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeModuleRefs()


//*****************************************************************************
// Merge AssemblyRef tables
//*****************************************************************************
HRESULT NEWMERGER::MergeAssemblyRefs()
{
    HRESULT         hr = NOERROR;
    AssemblyRefRec  *pRecImport = NULL;
    AssemblyRefRec  *pRecEmit = NULL;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    mdAssemblyRef   arEmit;
    bool            bDuplicate = false;
    LPCUTF8         szTmp;
    const void      *pbTmp;
    ULONG           cbTmp;
    ULONG           iCount;
    ULONG           i;
    ULONG           iRecord;
    TOKENREC        *pTokenRec;
    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountAssemblyRefs();

        // loope through all the AssemblyRefs.
        for (i = 1; i <= iCount; i++)
        {
            // Compare with the emit scope.
            IfFailGo(pMiniMdImport->GetAssemblyRefRecord(i, &pRecImport));
            IfFailGo(pMiniMdImport->getPublicKeyOrTokenOfAssemblyRef(pRecImport, (const BYTE **)&pbTmp, &cbTmp));
            hr = CLDB_E_RECORD_NOTFOUND;
            if (m_fDupCheck)
            {
                LPCSTR szAssemblyRefName;
                LPCSTR szAssemblyRefLocale;
                IfFailGo(pMiniMdImport->getNameOfAssemblyRef(pRecImport, &szAssemblyRefName));
                IfFailGo(pMiniMdImport->getLocaleOfAssemblyRef(pRecImport, &szAssemblyRefLocale));
                hr = ImportHelper::FindAssemblyRef(
                    pMiniMdEmit, 
                    szAssemblyRefName, 
                    szAssemblyRefLocale, 
                    pbTmp, 
                    cbTmp, 
                    pRecImport->GetMajorVersion(), 
                    pRecImport->GetMinorVersion(), 
                    pRecImport->GetBuildNumber(), 
                    pRecImport->GetRevisionNumber(), 
                    pRecImport->GetFlags(), 
                    &arEmit);
            }
            if (hr == S_OK)
            {
                // Yes, it does
                bDuplicate = true;

                // <TODO>@FUTURE: more verification?</TODO>
            }
            else if (hr == CLDB_E_RECORD_NOTFOUND)
            {
                // No, it doesn't.  Copy it over.
                bDuplicate = false;
                IfFailGo(pMiniMdEmit->AddAssemblyRefRecord(&pRecEmit, &iRecord));
                arEmit = TokenFromRid(iRecord, mdtAssemblyRef);

                pRecEmit->Copy(pRecImport);

                IfFailGo(pMiniMdImport->getPublicKeyOrTokenOfAssemblyRef(pRecImport, (const BYTE **)&pbTmp, &cbTmp));
                IfFailGo(pMiniMdEmit->PutBlob(TBL_AssemblyRef, AssemblyRefRec::COL_PublicKeyOrToken, 
                                            pRecEmit, pbTmp, cbTmp));

                IfFailGo(pMiniMdImport->getNameOfAssemblyRef(pRecImport, &szTmp));
                IfFailGo(pMiniMdEmit->PutString(TBL_AssemblyRef, AssemblyRefRec::COL_Name, 
                                            pRecEmit, szTmp));

                IfFailGo(pMiniMdImport->getLocaleOfAssemblyRef(pRecImport, &szTmp));
                IfFailGo(pMiniMdEmit->PutString(TBL_AssemblyRef, AssemblyRefRec::COL_Locale, 
                                            pRecEmit, szTmp));

                IfFailGo(pMiniMdImport->getHashValueOfAssemblyRef(pRecImport, (const BYTE **)&pbTmp, &cbTmp));
                IfFailGo(pMiniMdEmit->PutBlob(TBL_AssemblyRef, AssemblyRefRec::COL_HashValue, 
                                            pRecEmit, pbTmp, cbTmp));

            }
            else
                IfFailGo(hr);

            // record the token movement.
            IfFailGo(pCurTkMap->InsertNotFound(
                TokenFromRid(i, mdtAssemblyRef), 
                bDuplicate, 
                arEmit, 
                &pTokenRec));
        }
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeAssemblyRefs()


//*****************************************************************************
// Merge TypeRef tables also performing TypeRef to TypeDef opitimization. ie.
// we will not introduce a TypeRef record if we can optimize it to a TypeDef.
//*****************************************************************************
HRESULT NEWMERGER::MergeTypeRefs()
{
    HRESULT     hr = NOERROR;
    TypeRefRec  *pRecImport = NULL;
    TypeRefRec  *pRecEmit = NULL;
    CMiniMdRW   *pMiniMdImport;
    CMiniMdRW   *pMiniMdEmit;
    ULONG       iCount;
    ULONG       i;
    mdTypeRef   trEmit;
    bool        bDuplicate = false;
    TOKENREC    *pTokenRec;
    bool        isTypeDef;

    mdToken     tkResImp;
    mdToken     tkResEmit;
    LPCUTF8     szNameImp;
    LPCUTF8     szNamespaceImp;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountTypeRefs();

        // loop through all TypeRef
        for (i = 1; i <= iCount; i++)
        {
            // only merge those TypeRefs that are marked
            if ( pMiniMdImport->GetFilterTable()->IsTypeRefMarked(TokenFromRid(i, mdtTypeRef)) == false)
                continue;

            isTypeDef = false;

            // compare it with the emit scope
            IfFailGo(pMiniMdImport->GetTypeRefRecord(i, &pRecImport));
            tkResImp = pMiniMdImport->getResolutionScopeOfTypeRef(pRecImport);
            IfFailGo(pMiniMdImport->getNamespaceOfTypeRef(pRecImport, &szNamespaceImp));
            IfFailGo(pMiniMdImport->getNameOfTypeRef(pRecImport, &szNameImp));
            if (!IsNilToken(tkResImp))
            {
                IfFailGo(pCurTkMap->Remap(tkResImp, &tkResEmit));
            }
            else
            {
                tkResEmit = tkResImp;
            }

            // There are some interesting cases to consider here.
            // 1) If the TypeRef's ResolutionScope is a nil token, or is the MODULEDEFTOKEN (current module), 
            //      then the TypeRef refers to a type in the current scope, so we should find a corresponding
            //      TypeDef in the output scope.  If we find the TypeDef, we'll remap this TypeRef token
            //      to that TypeDef token. 
            //    If we don't find that TypeDef, or if "TypeRef to TypeDef" optimization is turned off, we'll
            //      create the TypeRef in the output scope.
            // 2) If the TypeRef's ResolutionScope has been resolved to a TypeDef, then this TypeRef was part
            //      of a nested type definition.  In that case, we'd better find a corresponding TypeDef
            //      or we have an error.
            if (IsNilToken(tkResEmit) || tkResEmit == MODULEDEFTOKEN || TypeFromToken(tkResEmit) == mdtTypeDef)
            {
                hr = ImportHelper::FindTypeDefByName(
                    pMiniMdEmit, 
                    szNamespaceImp, 
                    szNameImp, 
                    (TypeFromToken(tkResEmit) == mdtTypeDef) ? tkResEmit : mdTokenNil, 
                    &trEmit);
                if (hr == S_OK)
                {
                    isTypeDef = true;

                    // it really does not matter if we set the duplicate to true or false. 
                    bDuplicate = true;
                }
            }

            // If the ResolutionScope was merged as a TypeDef, and this token wasn't found as TypeDef, send the error.
            if (TypeFromToken(tkResEmit) == mdtTypeDef && !isTypeDef)
            {
                // Send the error notification.  Use the "continuable error" callback, but even if linker says it is
                //  ok, don't continue.
                CheckContinuableErrorEx(META_E_TYPEDEF_MISSING, pImportData, TokenFromRid(i, mdtTypeRef));
                IfFailGo(META_E_TYPEDEF_MISSING);
            }

            // If this TypeRef cannot be optmized to a TypeDef or the Ref to Def optimization is turned off, do the following.
            if (!isTypeDef || !((m_optimizeRefToDef & MDTypeRefToDef) == MDTypeRefToDef))
            {
                // does this TypeRef already exist in the emit scope?
                if ( m_fDupCheck && ImportHelper::FindTypeRefByName(
                    pMiniMdEmit, 
                    tkResEmit, 
                    szNamespaceImp, 
                    szNameImp, 
                    &trEmit) == S_OK )
                {
                    // Yes, it does
                    bDuplicate = true;
                }
                else
                {
                    // No, it doesn't. Copy it over.
                    bDuplicate = false;
                    IfFailGo(pMiniMdEmit->AddTypeRefRecord(&pRecEmit, (RID*)&trEmit));
                    trEmit = TokenFromRid(trEmit, mdtTypeRef);

                    // Set ResolutionScope.  tkResEmit has already been re-mapped.
                    IfFailGo(pMiniMdEmit->PutToken(TBL_TypeRef, TypeRefRec::COL_ResolutionScope, 
                                                    pRecEmit, tkResEmit));

                    // Set Name.
                    IfFailGo(pMiniMdEmit->PutString(TBL_TypeRef, TypeRefRec::COL_Name, 
                                                    pRecEmit, szNameImp));
                    IfFailGo(pMiniMdEmit->AddNamedItemToHash(TBL_TypeRef, trEmit, szNameImp, 0));
            
                    // Set Namespace.
                    IfFailGo(pMiniMdEmit->PutString(TBL_TypeRef, TypeRefRec::COL_Namespace, 
                                                    pRecEmit, szNamespaceImp));
                }
            }

            // record the token movement
            IfFailGo( pCurTkMap->InsertNotFound(
                TokenFromRid(i, mdtTypeRef), 
                bDuplicate, 
                trEmit, 
                &pTokenRec) );
        }
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeTypeRefs()
 

//*****************************************************************************
// copy over the remaining information of partially merged TypeDef records. Right now only
// extends field is delayed to here. The reason that we delay extends field is because we want
// to optimize TypeRef to TypeDef if possible.
//*****************************************************************************
HRESULT NEWMERGER::CompleteMergeTypeDefs()
{
    HRESULT         hr = NOERROR;
    TypeDefRec      *pRecImport = NULL;
    TypeDefRec      *pRecEmit = NULL;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           iCount;
    ULONG           i;
    TOKENREC        *pTokenRec;
    mdToken         tkExtendsImp;
    mdToken         tkExtendsEmit;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;

        iCount = pMiniMdImport->getCountTypeDefs();

        // Merge the typedefs
        for (i = 1; i <= iCount; i++)
        {
            // only merge those TypeDefs that are marked
            if ( pMiniMdImport->GetFilterTable()->IsTypeDefMarked(TokenFromRid(i, mdtTypeDef)) == false)
                continue;

            if ( !pCurTkMap->Find(TokenFromRid(i, mdtTypeDef), &pTokenRec) )
            {
                _ASSERTE( !"bad state!");
                IfFailGo( META_E_BADMETADATA );
            }

            if (pTokenRec->m_isDuplicate == false)
            {
                // get the extends token from the import
                IfFailGo(pMiniMdImport->GetTypeDefRecord(i, &pRecImport));
                tkExtendsImp = pMiniMdImport->getExtendsOfTypeDef(pRecImport);

                // map the extends token to an merged token
                IfFailGo( pCurTkMap->Remap(tkExtendsImp, &tkExtendsEmit) );

                // set the extends to the merged TypeDef records.
                IfFailGo(pMiniMdEmit->GetTypeDefRecord(RidFromToken(pTokenRec->m_tkTo), &pRecEmit));
                IfFailGo(pMiniMdEmit->PutToken(TBL_TypeDef, TypeDefRec::COL_Extends, pRecEmit, tkExtendsEmit));
            }
            else
            {
                // <TODO>@FUTURE: we can check to make sure the import extends maps to the one that is set to the emit scope.
                // Otherwise, it is a error to report to linker.</TODO>
            }
        }
    }
ErrExit:
    return hr;
} // HRESULT NEWMERGER::CompleteMergeTypeDefs()


//*****************************************************************************
// merging TypeSpecs
//*****************************************************************************
HRESULT NEWMERGER::MergeTypeSpecs()
{
    HRESULT         hr = NOERROR;
    TypeSpecRec     *pRecImport = NULL;
    TypeSpecRec     *pRecEmit = NULL;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           iCount;
    ULONG           i;
    TOKENREC        *pTokenRec;
    mdTypeSpec      tsImp;
    mdTypeSpec      tsEmit;
    bool            fDuplicate;
    PCCOR_SIGNATURE pbSig;
    ULONG           cbSig;
    ULONG           cbEmit;
    CQuickBytes     qbSig;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;

        iCount = pMiniMdImport->getCountTypeSpecs();

        // loop through all TypeSpec
        for (i = 1; i <= iCount; i++)
        {
            // only merge those TypeSpecs that are marked
            if ( pMiniMdImport->GetFilterTable()->IsTypeSpecMarked(TokenFromRid(i, mdtTypeSpec)) == false)
                continue;

            // compare it with the emit scope
            IfFailGo(pMiniMdImport->GetTypeSpecRecord(i, &pRecImport));
            IfFailGo(pMiniMdImport->getSignatureOfTypeSpec(pRecImport, &pbSig, &cbSig));

            // convert tokens contained in signature to new scope
            IfFailGo(ImportHelper::MergeUpdateTokenInFieldSig(
                NULL,                       // Assembly emit scope.
                pMiniMdEmit,                // The emit scope.
                NULL, NULL, 0,              // Import assembly information.
                pMiniMdImport,              // The scope to merge into the emit scope.
                pbSig,                      // signature from the imported scope
                pCurTkMap,                  // Internal token mapping structure.
                &qbSig,                     // [OUT] translated signature
                0,                          // start from first byte of the signature
                0,                          // don't care how many bytes consumed
                &cbEmit));                  // number of bytes write to cbEmit

            hr = CLDB_E_RECORD_NOTFOUND;
            if (m_fDupCheck)
                hr = ImportHelper::FindTypeSpec(
                    pMiniMdEmit, 
                    (PCOR_SIGNATURE) qbSig.Ptr(), 
                    cbEmit, 
                    &tsEmit );

            if ( hr == S_OK )
            {
                // find a duplicate
                fDuplicate = true;
            }
            else
            {
                // copy over
                fDuplicate = false;
                IfFailGo(pMiniMdEmit->AddTypeSpecRecord(&pRecEmit, (ULONG *)&tsEmit));
                tsEmit = TokenFromRid(tsEmit, mdtTypeSpec);
                IfFailGo( pMiniMdEmit->PutBlob(
                    TBL_TypeSpec, 
                    TypeSpecRec::COL_Signature, 
                    pRecEmit, 
                    (PCOR_SIGNATURE)qbSig.Ptr(), 
                    cbEmit));
            }
            tsImp = TokenFromRid(i, mdtTypeSpec);

            // Record the token movement
            IfFailGo( pCurTkMap->InsertNotFound(tsImp, fDuplicate, tsEmit, &pTokenRec) );
        }
    }
ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeTypeSpecs()


//*****************************************************************************
// merging Children of TypeDefs. This includes field, method, parameter, property, event
//*****************************************************************************
HRESULT NEWMERGER::MergeTypeDefChildren() 
{
    HRESULT         hr = NOERROR;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           iCount;
    ULONG           i;
    mdTypeDef       tdEmit;
    mdTypeDef       tdImport;
    TOKENREC        *pTokenRec;

#if _DEBUG
    TypeDefRec      *pRecImport = NULL;
    LPCUTF8         szNameImp;
    LPCUTF8         szNamespaceImp;
#endif // _DEBUG

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountTypeDefs();

        // loop through all TypeDef again to merge/copy Methods, fields, events, and properties
        // 
        for (i = 1; i <= iCount; i++)
        {
            // only merge those TypeDefs that are marked
            if ( pMiniMdImport->GetFilterTable()->IsTypeDefMarked(TokenFromRid(i, mdtTypeDef)) == false)
                continue;

#if _DEBUG
            IfFailGo(pMiniMdImport->GetTypeDefRecord(i, &pRecImport));
            IfFailGo(pMiniMdImport->getNameOfTypeDef(pRecImport, &szNameImp));
            IfFailGo(pMiniMdImport->getNamespaceOfTypeDef(pRecImport, &szNamespaceImp));
#endif // _DEBUG

            // check to see if the typedef is duplicate or not
            tdImport = TokenFromRid(i, mdtTypeDef);
            if ( pCurTkMap->Find( tdImport, &pTokenRec) == false)
            {
                _ASSERTE( !"bad state!");
                IfFailGo( META_E_BADMETADATA );
            }
            tdEmit = pTokenRec->m_tkTo;
            if (pTokenRec->m_isDuplicate == false)
            {
                // now move all of the children records over
                IfFailGo( CopyMethods(pImportData, tdImport, tdEmit) );
                IfFailGo( CopyFields(pImportData, tdImport, tdEmit) );

                IfFailGo( CopyEvents(pImportData, tdImport, tdEmit) );

                //  Property has dependency on events
                IfFailGo( CopyProperties(pImportData, tdImport, tdEmit) );

                // Generic Params.
                IfFailGo( CopyGenericParams(pImportData, tdImport, tdEmit) );
            }
            else
            {
                // verify the children records
                IfFailGo( VerifyMethods(pImportData, tdImport, tdEmit) );
                IfFailGo( VerifyFields(pImportData, tdImport, tdEmit) );
                IfFailGo( VerifyEvents(pImportData, tdImport, tdEmit) );

                // property has dependency on events
                IfFailGo( VerifyProperties(pImportData, tdImport, tdEmit) );

                IfFailGo( VerifyGenericParams(pImportData, tdImport, tdEmit) );
            }
        }
    }
ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeTypeDefChildren()


//*******************************************************************************
// Helper to copy an Method record
//*******************************************************************************
HRESULT NEWMERGER::CopyMethod(
    MergeImportData *pImportData,           // [IN] import scope
    MethodRec   *pRecImp,                   // [IN] the record to import
    MethodRec   *pRecEmit)                  // [IN] the emit record to fill
{
    HRESULT     hr;
    CMiniMdRW   *pMiniMdImp = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    CMiniMdRW   *pMiniMdEmit = GetMiniMdEmit();
    LPCUTF8     szName;
    PCCOR_SIGNATURE pbSig;
    ULONG       cbSig;
    ULONG       cbEmit;
    CQuickBytes qbSig;
    MDTOKENMAP  *pCurTkMap;

    pCurTkMap = pImportData->m_pMDTokenMap;

    // copy over the fix part of the record
    pRecEmit->Copy(pRecImp);

    // copy over the name
    IfFailGo(pMiniMdImp->getNameOfMethod(pRecImp, &szName));
    IfFailGo(pMiniMdEmit->PutString(TBL_Method, MethodRec::COL_Name, pRecEmit, szName));

    // copy over the signature
    IfFailGo(pMiniMdImp->getSignatureOfMethod(pRecImp, &pbSig, &cbSig));

    // convert rid contained in signature to new scope
    IfFailGo(ImportHelper::MergeUpdateTokenInSig(
        NULL,                       // Assembly emit scope.
        pMiniMdEmit,                // The emit scope.
        NULL, NULL, 0,              // Import assembly scope information.
        pMiniMdImp,                 // The scope to merge into the emit scope.
        pbSig,                      // signature from the imported scope
        pCurTkMap,                // Internal token mapping structure.
        &qbSig,                     // [OUT] translated signature
        0,                          // start from first byte of the signature
        0,                          // don't care how many bytes consumed
        &cbEmit));                  // number of bytes write to cbEmit

    IfFailGo(pMiniMdEmit->PutBlob(TBL_Method, MethodRec::COL_Signature, pRecEmit, qbSig.Ptr(), cbEmit));

ErrExit:
    return hr;
} // HRESULT NEWMERGER::CopyMethod()


//*******************************************************************************
// Helper to copy an field record
//*******************************************************************************
HRESULT NEWMERGER::CopyField(
    MergeImportData *pImportData,           // [IN] import scope
    FieldRec    *pRecImp,                   // [IN] the record to import
    FieldRec    *pRecEmit)                  // [IN] the emit record to fill
{
    HRESULT     hr;
    CMiniMdRW   *pMiniMdImp = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    CMiniMdRW   *pMiniMdEmit = GetMiniMdEmit();
    LPCUTF8     szName;
    PCCOR_SIGNATURE pbSig;
    ULONG       cbSig;
    ULONG       cbEmit;
    CQuickBytes qbSig;
    MDTOKENMAP  *pCurTkMap;

    pCurTkMap = pImportData->m_pMDTokenMap;

    // copy over the fix part of the record
    pRecEmit->SetFlags(pRecImp->GetFlags());

    // copy over the name
    IfFailGo(pMiniMdImp->getNameOfField(pRecImp, &szName));
    IfFailGo(pMiniMdEmit->PutString(TBL_Field, FieldRec::COL_Name, pRecEmit, szName));

    // copy over the signature
    IfFailGo(pMiniMdImp->getSignatureOfField(pRecImp, &pbSig, &cbSig));

    // convert rid contained in signature to new scope
    IfFailGo(ImportHelper::MergeUpdateTokenInSig(
        NULL,                       // Emit assembly scope.
        pMiniMdEmit,                // The emit scope.
        NULL, NULL, 0,              // Import assembly scope information.
        pMiniMdImp,                 // The scope to merge into the emit scope.
        pbSig,                      // signature from the imported scope
        pCurTkMap,                  // Internal token mapping structure.
        &qbSig,                     // [OUT] translated signature
        0,                          // start from first byte of the signature
        0,                          // don't care how many bytes consumed
        &cbEmit));                  // number of bytes write to cbEmit

    IfFailGo(pMiniMdEmit->PutBlob(TBL_Field, FieldRec::COL_Signature, pRecEmit, qbSig.Ptr(), cbEmit));

ErrExit:
    return hr;
} // HRESULT NEWMERGER::CopyField()

//*******************************************************************************
// Helper to copy an field record
//*******************************************************************************
HRESULT NEWMERGER::CopyParam(
    MergeImportData *pImportData,           // [IN] import scope
    ParamRec    *pRecImp,                   // [IN] the record to import
    ParamRec    *pRecEmit)                  // [IN] the emit record to fill
{
    HRESULT     hr;
    CMiniMdRW   *pMiniMdImp = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    CMiniMdRW   *pMiniMdEmit = GetMiniMdEmit();
    LPCUTF8     szName;
    MDTOKENMAP  *pCurTkMap;

    pCurTkMap = pImportData->m_pMDTokenMap;

    // copy over the fix part of the record
    pRecEmit->Copy(pRecImp);

    // copy over the name
    IfFailGo(pMiniMdImp->getNameOfParam(pRecImp, &szName));
    IfFailGo(pMiniMdEmit->PutString(TBL_Param, ParamRec::COL_Name, pRecEmit, szName));

ErrExit:
    return hr;
} // HRESULT NEWMERGER::CopyParam()

//*******************************************************************************
// Helper to copy an Event record
//*******************************************************************************
HRESULT NEWMERGER::CopyEvent(
    MergeImportData *pImportData,           // [IN] import scope
    EventRec    *pRecImp,                   // [IN] the record to import
    EventRec    *pRecEmit)                  // [IN] the emit record to fill
{
    HRESULT     hr = NOERROR;
    CMiniMdRW   *pMiniMdImp = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    CMiniMdRW   *pMiniMdEmit = GetMiniMdEmit();
    mdToken     tkEventTypeImp;
    mdToken     tkEventTypeEmit;            // could be TypeDef or TypeRef
    LPCUTF8     szName;
    MDTOKENMAP  *pCurTkMap;

    pCurTkMap = pImportData->m_pMDTokenMap;

    pRecEmit->SetEventFlags(pRecImp->GetEventFlags());

    //move over the event name
    IfFailGo(pMiniMdImp->getNameOfEvent(pRecImp, &szName));
    IfFailGo( pMiniMdEmit->PutString(TBL_Event, EventRec::COL_Name, pRecEmit, szName) );

    // move over the EventType
    tkEventTypeImp = pMiniMdImp->getEventTypeOfEvent(pRecImp);
    if ( !IsNilToken(tkEventTypeImp) )
    {
        IfFailGo( pCurTkMap->Remap(tkEventTypeImp, &tkEventTypeEmit) );
        IfFailGo(pMiniMdEmit->PutToken(TBL_Event, EventRec::COL_EventType, pRecEmit, tkEventTypeEmit));
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::CopyEvent()


//*******************************************************************************
// Helper to copy a property record
//*******************************************************************************
HRESULT NEWMERGER::CopyProperty(
    MergeImportData *pImportData,           // [IN] import scope
    PropertyRec *pRecImp,                   // [IN] the record to import
    PropertyRec *pRecEmit)                  // [IN] the emit record to fill
{
    HRESULT     hr = NOERROR;
    CMiniMdRW   *pMiniMdImp = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    CMiniMdRW   *pMiniMdEmit = GetMiniMdEmit();
    LPCUTF8     szName;
    PCCOR_SIGNATURE pbSig;
    ULONG       cbSig;
    ULONG       cbEmit;
    CQuickBytes qbSig;
    MDTOKENMAP  *pCurTkMap;

    pCurTkMap = pImportData->m_pMDTokenMap;

    // move over the flag value
    pRecEmit->SetPropFlags(pRecImp->GetPropFlags());

    //move over the property name
    IfFailGo(pMiniMdImp->getNameOfProperty(pRecImp, &szName));
    IfFailGo( pMiniMdEmit->PutString(TBL_Property, PropertyRec::COL_Name, pRecEmit, szName) );

    // move over the type of the property
    IfFailGo(pMiniMdImp->getTypeOfProperty(pRecImp, &pbSig, &cbSig));

    // convert rid contained in signature to new scope
    IfFailGo( ImportHelper::MergeUpdateTokenInSig(
        NULL,                       // Assembly emit scope.
        pMiniMdEmit,                // The emit scope.
        NULL, NULL, 0,              // Import assembly scope information.
        pMiniMdImp,                 // The scope to merge into the emit scope.
        pbSig,                      // signature from the imported scope
        pCurTkMap,                // Internal token mapping structure.
        &qbSig,                     // [OUT] translated signature
        0,                          // start from first byte of the signature
        0,                          // don't care how many bytes consumed
        &cbEmit) );                 // number of bytes write to cbEmit

    IfFailGo(pMiniMdEmit->PutBlob(TBL_Property, PropertyRec::COL_Type, pRecEmit, qbSig.Ptr(), cbEmit));

ErrExit:
    return hr;
} // HRESULT NEWMERGER::CopyProperty()


//*****************************************************************************
// Copy MethodSemantics for an event or a property
//*****************************************************************************
HRESULT NEWMERGER::CopyMethodSemantics(
    MergeImportData *pImportData, 
    mdToken     tkImport,               // Event or property in the import scope
    mdToken     tkEmit)                 // corresponding event or property in the emitting scope
{
    HRESULT     hr = NOERROR;
    MethodSemanticsRec  *pRecImport = NULL;
    MethodSemanticsRec  *pRecEmit = NULL;
    CMiniMdRW   *pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    CMiniMdRW   *pMiniMdEmit = GetMiniMdEmit();
    ULONG       i;
    ULONG       msEmit;                 // MethodSemantics are just index not tokens
    mdToken     tkMethodImp;
    mdToken     tkMethodEmit;
    MDTOKENMAP  *pCurTkMap;
    HENUMInternal hEnum;

    pCurTkMap = pImportData->m_pMDTokenMap;

    // copy over the associates
    IfFailGo( pMiniMdImport->FindMethodSemanticsHelper(tkImport, &hEnum) );
    while (HENUMInternal::EnumNext(&hEnum, (mdToken *) &i))
    {
        IfFailGo(pMiniMdImport->GetMethodSemanticsRecord(i, &pRecImport));
        IfFailGo(pMiniMdEmit->AddMethodSemanticsRecord(&pRecEmit, &msEmit));
        pRecEmit->SetSemantic(pRecImport->GetSemantic());

        // set the MethodSemantics
        tkMethodImp = pMiniMdImport->getMethodOfMethodSemantics(pRecImport);
        IfFailGo(  pCurTkMap->Remap(tkMethodImp, &tkMethodEmit) );
        IfFailGo( pMiniMdEmit->PutToken(TBL_MethodSemantics, MethodSemanticsRec::COL_Method, pRecEmit, tkMethodEmit));

        // set the associate
        _ASSERTE( pMiniMdImport->getAssociationOfMethodSemantics(pRecImport) == tkImport );
        IfFailGo( pMiniMdEmit->PutToken(TBL_MethodSemantics, MethodSemanticsRec::COL_Association, pRecEmit, tkEmit));

        // no need to record the movement since it is not a token
        IfFailGo( pMiniMdEmit->AddMethodSemanticsToHash(msEmit) );
    }
ErrExit:
    HENUMInternal::ClearEnum(&hEnum);
    return hr;
} // HRESULT NEWMERGER::CopyMethodSemantics()


//*****************************************************************************
// Copy Methods given a TypeDef
//*****************************************************************************
HRESULT NEWMERGER::CopyMethods(
    MergeImportData *pImportData, 
    mdTypeDef       tdImport, 
    mdTypeDef       tdEmit)
{
    HRESULT     hr = NOERROR;
    MethodRec   *pRecImport = NULL;
    MethodRec   *pRecEmit = NULL;
    TypeDefRec  *pTypeDefRec;
    CMiniMdRW   *pMiniMdImport;
    CMiniMdRW   *pMiniMdEmit;
    ULONG       ridStart, ridEnd;
    ULONG       i;
    mdMethodDef mdEmit;
    mdMethodDef mdImp;
    TOKENREC    *pTokenRec;
    MDTOKENMAP  *pCurTkMap;

    MergeTypeData *pMTD;
    BOOL        bSuppressMergeCheck;
    mdCustomAttribute tkCA;

    pMiniMdEmit = GetMiniMdEmit();
    pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    pCurTkMap = pImportData->m_pMDTokenMap;

    IfFailGo(pMiniMdImport->GetTypeDefRecord(RidFromToken(tdImport), &pTypeDefRec));
    ridStart = pMiniMdImport->getMethodListOfTypeDef(pTypeDefRec);
    IfFailGo(pMiniMdImport->getEndMethodListOfTypeDef(RidFromToken(tdImport), &ridEnd));

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

    pMTD = m_rMTDs.Get(RidFromToken(tdEmit));

    // make sure we didn't count the methods yet
    _ASSERTE(pMTD->m_cMethods == 0);

    // loop through all Methods
    for (i = ridStart; i < ridEnd; i++)
    {
        // compare it with the emit scope
        IfFailGo(pMiniMdImport->GetMethodRid(i, (ULONG *)&mdImp));

        // only merge those MethodDefs that are marked
        if ( pMiniMdImport->GetFilterTable()->IsMethodMarked(TokenFromRid(mdImp, mdtMethodDef)) == false)
            continue;

        IfFailGo(pMiniMdImport->GetMethodRecord(mdImp, &pRecImport));
        IfFailGo(pMiniMdEmit->AddMethodRecord(&pRecEmit, (RID *)&mdEmit));

        // copy the method content over 
        IfFailGo( CopyMethod(pImportData, pRecImport, pRecEmit) );

        IfFailGo( pMiniMdEmit->AddMethodToTypeDef(RidFromToken(tdEmit), mdEmit));

        // record the token movement
        mdImp = TokenFromRid(mdImp, mdtMethodDef);
        mdEmit = TokenFromRid(mdEmit, mdtMethodDef);
        IfFailGo( pMiniMdEmit->AddMemberDefToHash(
            mdEmit, 
            tdEmit) ); 

        IfFailGo( pCurTkMap->InsertNotFound(mdImp, false, mdEmit, &pTokenRec) );

        // copy over the children
        IfFailGo( CopyParams(pImportData, mdImp, mdEmit) );
        IfFailGo( CopyGenericParams(pImportData, mdImp, mdEmit) );

        bSuppressMergeCheck = pMTD->m_bSuppressMergeCheck || 
               ((pImportData->m_tkSuppressMergeCheckCtor != mdTokenNil) && 
                (S_OK == ImportHelper::FindCustomAttributeByToken(pMiniMdImport, 
                    mdImp, pImportData->m_tkSuppressMergeCheckCtor, NULL, 0, &tkCA)));


        if (!bSuppressMergeCheck) {
            pMTD->m_cMethods++;
        }
    }

    // make sure we don't count any methods if merge check is suppressed on the type
    _ASSERTE(pMTD->m_cMethods == 0 || !pMTD->m_bSuppressMergeCheck);
ErrExit:
    return hr;
} // HRESULT NEWMERGER::CopyMethods()


//*****************************************************************************
// Copy Fields given a TypeDef
//*****************************************************************************
HRESULT NEWMERGER::CopyFields(
    MergeImportData *pImportData, 
    mdTypeDef       tdImport, 
    mdTypeDef       tdEmit)
{
    HRESULT         hr = NOERROR;
    FieldRec        *pRecImport = NULL;
    FieldRec        *pRecEmit = NULL;
    TypeDefRec      *pTypeDefRec;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           ridStart, ridEnd;
    ULONG           i;
    mdFieldDef      fdEmit;
    mdFieldDef      fdImp;
    bool            bDuplicate;
    TOKENREC        *pTokenRec;
    PCCOR_SIGNATURE pvSigBlob;
    ULONG           cbSigBlob;
    MDTOKENMAP      *pCurTkMap;

    MergeTypeData   *pMTD;
    BOOL            bSuppressMergeCheck;
    mdCustomAttribute tkCA;

    pMiniMdEmit = GetMiniMdEmit();
    pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    pCurTkMap = pImportData->m_pMDTokenMap;

    IfFailGo(pMiniMdImport->GetTypeDefRecord(RidFromToken(tdImport), &pTypeDefRec));
    ridStart = pMiniMdImport->getFieldListOfTypeDef(pTypeDefRec);
    IfFailGo(pMiniMdImport->getEndFieldListOfTypeDef(RidFromToken(tdImport), &ridEnd));

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

    pMTD = m_rMTDs.Get(RidFromToken(tdEmit));

    // make sure we didn't count the methods yet
    _ASSERTE(pMTD->m_cFields == 0);

    // loop through all FieldDef of a TypeDef
    for (i = ridStart; i < ridEnd; i++)
    {
        // compare it with the emit scope
        IfFailGo(pMiniMdImport->GetFieldRid(i, (ULONG *)&fdImp));

        // only merge those FieldDefs that are marked
        if ( pMiniMdImport->GetFilterTable()->IsFieldMarked(TokenFromRid(fdImp, mdtFieldDef)) == false)
            continue;

        
        IfFailGo(pMiniMdImport->GetFieldRecord(fdImp, &pRecImport));
        bDuplicate = false;
        IfFailGo(pMiniMdEmit->AddFieldRecord(&pRecEmit, (RID *)&fdEmit));

        // copy the field content over 
        IfFailGo( CopyField(pImportData, pRecImport, pRecEmit) );
        
        IfFailGo( pMiniMdEmit->AddFieldToTypeDef(RidFromToken(tdEmit), fdEmit));

        // record the token movement
        fdImp = TokenFromRid(fdImp, mdtFieldDef);
        fdEmit = TokenFromRid(fdEmit, mdtFieldDef);
        IfFailGo(pMiniMdEmit->getSignatureOfField(pRecEmit, &pvSigBlob, &cbSigBlob));
        IfFailGo( pMiniMdEmit->AddMemberDefToHash(
            fdEmit, 
            tdEmit) ); 

        IfFailGo( pCurTkMap->InsertNotFound(fdImp, false, fdEmit, &pTokenRec) );

        // count the number of fields that didn't suppress merge check
        // non-static fields doesn't inherite the suppress merge check attribute from the type
        bSuppressMergeCheck = 
            (IsFdStatic(pRecEmit->GetFlags()) && pMTD->m_bSuppressMergeCheck) || 
               ((pImportData->m_tkSuppressMergeCheckCtor != mdTokenNil) && 
                (S_OK == ImportHelper::FindCustomAttributeByToken(pMiniMdImport, 
                    fdImp, pImportData->m_tkSuppressMergeCheckCtor, NULL, 0, &tkCA)));

        if (!bSuppressMergeCheck) {
            pMTD->m_cFields++;
        }
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::CopyFields()


//*****************************************************************************
// Copy Events given a TypeDef
//*****************************************************************************
HRESULT NEWMERGER::CopyEvents(
    MergeImportData *pImportData, 
    mdTypeDef       tdImport, 
    mdTypeDef       tdEmit)
{
    HRESULT     hr = NOERROR;
    CMiniMdRW   *pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    CMiniMdRW   *pMiniMdEmit = GetMiniMdEmit();
    RID         ridEventMap;
    EventMapRec *pEventMapRec;
    EventRec    *pRecImport;
    EventRec    *pRecEmit;
    ULONG       ridStart;
    ULONG       ridEnd;
    ULONG       i;
    mdEvent     evImp;
    mdEvent     evEmit;
    TOKENREC    *pTokenRec;
    ULONG       iEventMap;
    EventMapRec *pEventMap;
    MDTOKENMAP  *pCurTkMap;

    MergeTypeData *pMTD;
    BOOL        bSuppressMergeCheck;
    mdCustomAttribute tkCA;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

    pCurTkMap = pImportData->m_pMDTokenMap;

    pMTD = m_rMTDs.Get(RidFromToken(tdEmit));

    // make sure we didn't count the events yet
    _ASSERTE(pMTD->m_cEvents == 0);

    IfFailGo(pMiniMdImport->FindEventMapFor(RidFromToken(tdImport), &ridEventMap));
    if (!InvalidRid(ridEventMap))
    {
        IfFailGo(pMiniMdImport->GetEventMapRecord(ridEventMap, &pEventMapRec));
        ridStart = pMiniMdImport->getEventListOfEventMap(pEventMapRec);
        IfFailGo(pMiniMdImport->getEndEventListOfEventMap(ridEventMap, &ridEnd));

        if (ridEnd > ridStart)
        {
            // If there is any event, create the eventmap record in the emit scope
            // Create new record.
            IfFailGo(pMiniMdEmit->AddEventMapRecord(&pEventMap, &iEventMap));

            // Set parent.
            IfFailGo(pMiniMdEmit->PutToken(TBL_EventMap, EventMapRec::COL_Parent, pEventMap, tdEmit));
        }
        
        for (i = ridStart; i < ridEnd; i++)
        {
            // get the real event rid
            IfFailGo(pMiniMdImport->GetEventRid(i, (ULONG *)&evImp));

            // only merge those Events that are marked
            if ( pMiniMdImport->GetFilterTable()->IsEventMarked(TokenFromRid(evImp, mdtEvent)) == false)
                continue;
            
            IfFailGo(pMiniMdImport->GetEventRecord(evImp, &pRecImport));
            IfFailGo(pMiniMdEmit->AddEventRecord(&pRecEmit, (RID *)&evEmit));

            // copy the event record over 
            IfFailGo( CopyEvent(pImportData, pRecImport, pRecEmit) );
            
            // Add Event to the EventMap.
            IfFailGo( pMiniMdEmit->AddEventToEventMap(iEventMap, evEmit) );

            // record the token movement
            evImp = TokenFromRid(evImp, mdtEvent);
            evEmit = TokenFromRid(evEmit, mdtEvent);

            IfFailGo( pCurTkMap->InsertNotFound(evImp, false, evEmit, &pTokenRec) );

            // copy over the method semantics
            IfFailGo( CopyMethodSemantics(pImportData, evImp, evEmit) );

            bSuppressMergeCheck = pMTD->m_bSuppressMergeCheck || 
               ((pImportData->m_tkSuppressMergeCheckCtor != mdTokenNil) && 
                (S_OK == ImportHelper::FindCustomAttributeByToken(pMiniMdImport, 
                    evImp, pImportData->m_tkSuppressMergeCheckCtor, NULL, 0, &tkCA)));

            if (!bSuppressMergeCheck) {
                pMTD->m_cEvents++;
            }
        }
    }

    // make sure we don't count any events if merge check is suppressed on the type
    _ASSERTE(pMTD->m_cEvents == 0 || !pMTD->m_bSuppressMergeCheck);
ErrExit:
    return hr;
} // HRESULT NEWMERGER::CopyEvents()


//*****************************************************************************
// Copy Properties given a TypeDef
//*****************************************************************************
HRESULT NEWMERGER::CopyProperties(
    MergeImportData *pImportData, 
    mdTypeDef       tdImport, 
    mdTypeDef       tdEmit)
{
    HRESULT     hr = NOERROR;
    CMiniMdRW   *pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    CMiniMdRW   *pMiniMdEmit = GetMiniMdEmit();
    RID         ridPropertyMap;
    PropertyMapRec *pPropertyMapRec;
    PropertyRec *pRecImport;
    PropertyRec *pRecEmit;
    ULONG       ridStart;
    ULONG       ridEnd;
    ULONG       i;
    mdProperty  prImp;
    mdProperty  prEmit;
    TOKENREC    *pTokenRec;
    ULONG       iPropertyMap;
    PropertyMapRec  *pPropertyMap;
    MDTOKENMAP  *pCurTkMap;

    MergeTypeData *pMTD;
    BOOL        bSuppressMergeCheck;
    mdCustomAttribute tkCA;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

    pCurTkMap = pImportData->m_pMDTokenMap;

    pMTD = m_rMTDs.Get(RidFromToken(tdEmit));

    // make sure we didn't count the properties yet
    _ASSERTE(pMTD->m_cProperties == 0);

    IfFailGo(pMiniMdImport->FindPropertyMapFor(RidFromToken(tdImport), &ridPropertyMap));
    if (!InvalidRid(ridPropertyMap))
    {
        IfFailGo(pMiniMdImport->GetPropertyMapRecord(ridPropertyMap, &pPropertyMapRec));
        ridStart = pMiniMdImport->getPropertyListOfPropertyMap(pPropertyMapRec);
        IfFailGo(pMiniMdImport->getEndPropertyListOfPropertyMap(ridPropertyMap, &ridEnd));

        if (ridEnd > ridStart)
        {
            // If there is any event, create the PropertyMap record in the emit scope
            // Create new record.
            IfFailGo(pMiniMdEmit->AddPropertyMapRecord(&pPropertyMap, &iPropertyMap));

            // Set parent.
            IfFailGo(pMiniMdEmit->PutToken(TBL_PropertyMap, PropertyMapRec::COL_Parent, pPropertyMap, tdEmit));
        }

        for (i = ridStart; i < ridEnd; i++)
        {
            // get the property rid
            IfFailGo(pMiniMdImport->GetPropertyRid(i, (ULONG *)&prImp));

            // only merge those Properties that are marked
            if ( pMiniMdImport->GetFilterTable()->IsPropertyMarked(TokenFromRid(prImp, mdtProperty)) == false)
                continue;
            
            
            IfFailGo(pMiniMdImport->GetPropertyRecord(prImp, &pRecImport));
            IfFailGo(pMiniMdEmit->AddPropertyRecord(&pRecEmit, (RID *)&prEmit));

            // copy the property record over 
            IfFailGo( CopyProperty(pImportData, pRecImport, pRecEmit) );

            // Add Property to the PropertyMap.
            IfFailGo( pMiniMdEmit->AddPropertyToPropertyMap(iPropertyMap, prEmit) );

            // record the token movement
            prImp = TokenFromRid(prImp, mdtProperty);
            prEmit = TokenFromRid(prEmit, mdtProperty);

            IfFailGo( pCurTkMap->InsertNotFound(prImp, false, prEmit, &pTokenRec) );

            // copy over the method semantics
            IfFailGo( CopyMethodSemantics(pImportData, prImp, prEmit) );

            bSuppressMergeCheck = pMTD->m_bSuppressMergeCheck || 
               ((pImportData->m_tkSuppressMergeCheckCtor != mdTokenNil) && 
                (S_OK == ImportHelper::FindCustomAttributeByToken(pMiniMdImport, 
                    prImp, pImportData->m_tkSuppressMergeCheckCtor, NULL, 0, &tkCA)));

            if (!bSuppressMergeCheck) {
                pMTD->m_cProperties++;
            }
        }
    }

    // make sure we don't count any properties if merge check is suppressed on the type
    _ASSERTE(pMTD->m_cProperties == 0 || !pMTD->m_bSuppressMergeCheck);

ErrExit:
    return hr;
} // HRESULT NEWMERGER::CopyProperties()


//*****************************************************************************
// Copy Parameters given a TypeDef
//*****************************************************************************
HRESULT NEWMERGER::CopyParams(
    MergeImportData *pImportData, 
    mdMethodDef     mdImport, 
    mdMethodDef     mdEmit)
{
    HRESULT     hr = NOERROR;
    ParamRec    *pRecImport = NULL;
    ParamRec    *pRecEmit = NULL;
    MethodRec   *pMethodRec;
    CMiniMdRW   *pMiniMdImport;
    CMiniMdRW   *pMiniMdEmit;
    ULONG       ridStart, ridEnd;
    ULONG       i;
    mdParamDef  pdEmit;
    mdParamDef  pdImp;
    TOKENREC    *pTokenRec;
    MDTOKENMAP  *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    pCurTkMap = pImportData->m_pMDTokenMap;


    IfFailGo(pMiniMdImport->GetMethodRecord(RidFromToken(mdImport), &pMethodRec));
    ridStart = pMiniMdImport->getParamListOfMethod(pMethodRec);
    IfFailGo(pMiniMdImport->getEndParamListOfMethod(RidFromToken(mdImport), &ridEnd));

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

    // loop through all InterfaceImpl
    for (i = ridStart; i < ridEnd; i++)
    {
        // Get the param rid
        IfFailGo(pMiniMdImport->GetParamRid(i, (ULONG *)&pdImp));

        // only merge those Params that are marked
        if ( pMiniMdImport->GetFilterTable()->IsParamMarked(TokenFromRid(pdImp, mdtParamDef)) == false)
            continue;
            
        
        IfFailGo(pMiniMdImport->GetParamRecord(pdImp, &pRecImport));
        IfFailGo(pMiniMdEmit->AddParamRecord(&pRecEmit, (RID *)&pdEmit));

        // copy the Parameter record over 
        IfFailGo( CopyParam(pImportData, pRecImport, pRecEmit) );

        // warning!! warning!!
        // We cannot add paramRec to method list until it is fully set.
        // AddParamToMethod will use the ulSequence in the record
        IfFailGo( pMiniMdEmit->AddParamToMethod(RidFromToken(mdEmit), pdEmit));

        // record the token movement
        pdImp = TokenFromRid(pdImp, mdtParamDef);
        pdEmit = TokenFromRid(pdEmit, mdtParamDef);

        IfFailGo( pCurTkMap->InsertNotFound(pdImp, false, pdEmit, &pTokenRec) );
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::CopyParams()


//*****************************************************************************
// Copy GenericParams given a TypeDef
//*****************************************************************************
HRESULT NEWMERGER::CopyGenericParams(
    MergeImportData *pImportData, 
    mdToken         tkImport, 
    mdToken         tkEmit)
{
    HRESULT         hr = NOERROR;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    TOKENREC        *pTokenRec;
    GenericParamRec *pRecImport = NULL;
    GenericParamRec *pRecEmit = NULL;
    MDTOKENMAP      *pCurTkMap;
    HENUMInternal   hEnum;
    mdGenericParam  gpImport;
    mdGenericParam  gpEmit;
    LPCSTR          szGenericParamName;

    pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    pMiniMdEmit = GetMiniMdEmit();
    pCurTkMap = pImportData->m_pMDTokenMap;

    IfFailGo( pMiniMdImport->FindGenericParamHelper(tkImport, &hEnum) );
    
    while (HENUMInternal::EnumNext(&hEnum, (mdToken *) &gpImport))
    {
        // Get the import GenericParam record
        _ASSERTE(TypeFromToken(gpImport) == mdtGenericParam);
        IfFailGo(pMiniMdImport->GetGenericParamRecord(RidFromToken(gpImport), &pRecImport));
        
        // Create new emit record.
        IfFailGo(pMiniMdEmit->AddGenericParamRecord(&pRecEmit, (RID *)&gpEmit));

        // copy the GenericParam content 
        pRecEmit->SetNumber( pRecImport->GetNumber());
        pRecEmit->SetFlags( pRecImport->GetFlags());
        
        IfFailGo( pMiniMdEmit->PutToken(TBL_GenericParam, GenericParamRec::COL_Owner, pRecEmit, tkEmit));
        
        IfFailGo(pMiniMdImport->getNameOfGenericParam(pRecImport, &szGenericParamName));
        IfFailGo( pMiniMdEmit->PutString(TBL_GenericParam, GenericParamRec::COL_Name, pRecEmit, szGenericParamName));
        
        // record the token movement
        gpImport = TokenFromRid(gpImport, mdtGenericParam);
        gpEmit = TokenFromRid(gpEmit, mdtGenericParam);

        IfFailGo( pCurTkMap->InsertNotFound(gpImport, false, gpEmit, &pTokenRec) );

        // copy over any constraints
        IfFailGo( CopyGenericParamConstraints(pImportData, gpImport, gpEmit) );
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::CopyGenericParams()


//*****************************************************************************
// Copy GenericParamConstraints given a GenericParam
//*****************************************************************************
HRESULT NEWMERGER::CopyGenericParamConstraints(
    MergeImportData *pImportData, 
    mdGenericParamConstraint tkImport, 
    mdGenericParamConstraint tkEmit)
{
    HRESULT         hr = NOERROR;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    TOKENREC        *pTokenRec;
    GenericParamConstraintRec *pRecImport = NULL;
    GenericParamConstraintRec *pRecEmit = NULL;
    MDTOKENMAP      *pCurTkMap;
    HENUMInternal   hEnum;
    mdGenericParamConstraint  gpImport;
    mdGenericParamConstraint  gpEmit;
    mdToken         tkConstraint;

    pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    pMiniMdEmit = GetMiniMdEmit();
    pCurTkMap = pImportData->m_pMDTokenMap;

    IfFailGo( pMiniMdImport->FindGenericParamConstraintHelper(tkImport, &hEnum) );
    
    while (HENUMInternal::EnumNext(&hEnum, (mdToken *) &gpImport))
    {
        // Get the import GenericParam record
        _ASSERTE(TypeFromToken(gpImport) == mdtGenericParamConstraint);
        IfFailGo(pMiniMdImport->GetGenericParamConstraintRecord(RidFromToken(gpImport), &pRecImport));
        
        // Translate the constraint before creating new record.
        tkConstraint = pMiniMdImport->getConstraintOfGenericParamConstraint(pRecImport);
        if (pCurTkMap->Find(tkConstraint, &pTokenRec) == false)
        {
            // This should never fire unless the TypeDefs/Refs weren't merged
            // before this code runs.
            _ASSERTE(!"GenericParamConstraint Constraint not found in MERGER::CopyGenericParamConstraints.  Bad state!");
            IfFailGo( META_E_BADMETADATA );
        }
        tkConstraint = pTokenRec->m_tkTo;

        // Create new emit record.
        IfFailGo(pMiniMdEmit->AddGenericParamConstraintRecord(&pRecEmit, (RID *)&gpEmit));

        // copy the GenericParamConstraint content 
        IfFailGo( pMiniMdEmit->PutToken(TBL_GenericParamConstraint, GenericParamConstraintRec::COL_Owner, pRecEmit, tkEmit));
        
        IfFailGo( pMiniMdEmit->PutToken(TBL_GenericParamConstraint, GenericParamConstraintRec::COL_Constraint, pRecEmit, tkConstraint));
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::CopyGenericParamConstraints()


//*****************************************************************************
// Verify GenericParams given a TypeDef
//*****************************************************************************
HRESULT NEWMERGER::VerifyGenericParams(
    MergeImportData *pImportData, 
    mdToken         tkImport, 
    mdToken         tkEmit)
{
    HRESULT         hr = NOERROR;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    TOKENREC        *pTokenRec;
    MDTOKENMAP      *pCurTkMap;
    HENUMInternal   hEnumImport;        // Enumerator for import scope.
    HENUMInternal   hEnumEmit;          // Enumerator for emit scope.
    ULONG           cImport, cEmit;     // Count of import & emit records.
    ULONG           i;                  // Enumerating records in import scope.
    ULONG           iEmit;              // Tracking records in emit scope.
    mdGenericParam  gpImport;           // Import scope GenericParam token.
    mdGenericParam  gpEmit;             // Emit scope GenericParam token.
    GenericParamRec *pRecImport = NULL;
    GenericParamRec *pRecEmit = NULL;
    LPCSTR          szNameImport;       // Name of param in import scope.
    LPCSTR          szNameEmit;         // Name of param in emit scope.

    pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    pMiniMdEmit = GetMiniMdEmit();
    pCurTkMap = pImportData->m_pMDTokenMap;

    // Get enumerators for the input and output scopes.
    IfFailGo(pMiniMdImport->FindGenericParamHelper(tkImport, &hEnumImport));
    IfFailGo(pMiniMdEmit->FindGenericParamHelper(tkEmit, &hEnumEmit));
    
    // The counts should be the same.
    IfFailGo(HENUMInternal::GetCount(&hEnumImport, &cImport));
    IfFailGo(HENUMInternal::GetCount(&hEnumEmit, &cEmit));

    if (cImport != cEmit)
    {
        CheckContinuableErrorEx(META_E_GENERICPARAM_INCONSISTENT, pImportData, tkImport);
        // If we are here, the linker says this error is OK.
    }

    for (i=iEmit=0; i<cImport; ++i)
    {
        // Get the import GenericParam record
        IfFailGo(HENUMInternal::GetElement(&hEnumImport, i, &gpImport));
        _ASSERTE(TypeFromToken(gpImport) == mdtGenericParam);
        IfFailGo(pMiniMdImport->GetGenericParamRecord(RidFromToken(gpImport), &pRecImport));
        
        // Find the emit record.  If the import and emit scopes are ordered the same
        //  this is easy; otherwise go looking for it.
        // Get the "next" emit record.
        if (iEmit < cEmit)
        {
            IfFailGo(HENUMInternal::GetElement(&hEnumEmit, iEmit, &gpEmit));
            _ASSERTE(TypeFromToken(gpEmit) == mdtGenericParam);
            IfFailGo(pMiniMdEmit->GetGenericParamRecord(RidFromToken(gpEmit), &pRecEmit));
        }

        // If the import and emit sequence numbers don't match, go looking.
        //  Also, if we would have walked off end of array, go looking.
        if (iEmit >= cEmit || pRecImport->GetNumber() != pRecEmit->GetNumber())
        {
            for (iEmit=0; iEmit<cEmit; ++iEmit)
            {
                IfFailGo( HENUMInternal::GetElement(&hEnumEmit, iEmit, &gpEmit));
                _ASSERTE(TypeFromToken(gpEmit) == mdtGenericParam);
                IfFailGo(pMiniMdEmit->GetGenericParamRecord(RidFromToken(gpEmit), &pRecEmit));

                // The one we want?
                if (pRecImport->GetNumber() == pRecEmit->GetNumber())
                    break;
            }
            if (iEmit >= cEmit)
                goto Error; // Didn't find it
        }

        // Check that these "n'th" GenericParam records match.

        // Flags.
        if (pRecImport->GetFlags() != pRecEmit->GetFlags())
            goto Error;

        // Name.
        IfFailGo(pMiniMdImport->getNameOfGenericParam(pRecImport, &szNameImport));
        IfFailGo(pMiniMdEmit->getNameOfGenericParam(pRecEmit, &szNameEmit));
        if (strcmp(szNameImport, szNameEmit) != 0)
            goto Error;

        // Verify any constraints.
        gpImport = TokenFromRid(gpImport, mdtGenericParam);
        gpEmit = TokenFromRid(gpEmit, mdtGenericParam);
        hr =  VerifyGenericParamConstraints(pImportData, gpImport, gpEmit);

        if (SUCCEEDED(hr))
        {
            // record the token movement
            IfFailGo( pCurTkMap->InsertNotFound(gpImport, true, gpEmit, &pTokenRec) );
        }
        else
        {
Error:
            // inconsistent in GenericParams
            hr = S_OK; // discard old error; new error will be returned from CheckContinuableError
            CheckContinuableErrorEx(META_E_GENERICPARAM_INCONSISTENT, pImportData, tkImport);
        }
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::VerifyGenericParams()


//*****************************************************************************
// Verify GenericParamConstraints given a GenericParam
//*****************************************************************************
HRESULT NEWMERGER::VerifyGenericParamConstraints(
    MergeImportData *pImportData,           // The import scope.
    mdGenericParam  gpImport,               // Import GenericParam.
    mdGenericParam  gpEmit)                 // Emit GenericParam.
{
    HRESULT         hr = NOERROR;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    TOKENREC        *pTokenRec;
    HENUMInternal   hEnumImport;        // Enumerator for import scope.
    HENUMInternal   hEnumEmit;          // Enumerator for emit scope.
    ULONG           cImport, cEmit;     // Count of import & emit records.
    ULONG           i;                  // Enumerating records in import scope.
    ULONG           iEmit;              // Tracking records in emit scope.
    GenericParamConstraintRec *pRecImport = NULL;
    GenericParamConstraintRec *pRecEmit = NULL;
    MDTOKENMAP      *pCurTkMap;
    mdToken         tkConstraintImport = mdTokenNil;
    mdToken         tkConstraintEmit = mdTokenNil;

    pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    pMiniMdEmit = GetMiniMdEmit();
    pCurTkMap = pImportData->m_pMDTokenMap;

    // Get enumerators for the input and output scopes.
    IfFailGo(pMiniMdImport->FindGenericParamConstraintHelper(gpImport, &hEnumImport));
    IfFailGo(pMiniMdEmit->FindGenericParamConstraintHelper(gpEmit, &hEnumEmit));
    
    // The counts should be the same.
    IfFailGo(HENUMInternal::GetCount(&hEnumImport, &cImport));
    IfFailGo(HENUMInternal::GetCount(&hEnumEmit, &cEmit));
    
    if (cImport != cEmit)
        IfFailGo(META_E_GENERICPARAM_INCONSISTENT); // Different numbers of constraints.

    for (i=iEmit=0; i<cImport; ++i)
    {
        // Get the import GenericParam record
        IfFailGo( HENUMInternal::GetElement(&hEnumImport, i, &gpImport));
        _ASSERTE(TypeFromToken(gpImport) == mdtGenericParamConstraint);
        IfFailGo(pMiniMdImport->GetGenericParamConstraintRecord(RidFromToken(gpImport), &pRecImport));
        
        // Get the constraint.
        tkConstraintImport = pMiniMdImport->getConstraintOfGenericParamConstraint(pRecImport);
        if (pCurTkMap->Find(tkConstraintImport, &pTokenRec) == false)
        {
            // This should never fire unless the TypeDefs/Refs weren't merged
            // before this code runs.
            _ASSERTE(!"GenericParamConstraint Constraint not found in MERGER::VerifyGenericParamConstraints.  Bad state!");
            IfFailGo( META_E_BADMETADATA );
        }
        tkConstraintImport = pTokenRec->m_tkTo;
        
        // Find the emit record.  If the import and emit scopes are ordered the same
        //  this is easy; otherwise go looking for it.
        // Get the "next" emit record.
        if (iEmit < cEmit)
        {
            IfFailGo( HENUMInternal::GetElement(&hEnumEmit, iEmit, &gpEmit));
            _ASSERTE(TypeFromToken(gpEmit) == mdtGenericParamConstraint);
            IfFailGo(pMiniMdEmit->GetGenericParamConstraintRecord(RidFromToken(gpEmit), &pRecEmit));
            tkConstraintEmit = pMiniMdEmit->getConstraintOfGenericParamConstraint(pRecEmit);
        }

        // If the import and emit constraints don't match, go looking.
        if (iEmit >= cEmit || tkConstraintEmit != tkConstraintImport)
        {
            for (iEmit=0; iEmit<cEmit; ++iEmit)
            {
                IfFailGo( HENUMInternal::GetElement(&hEnumEmit, iEmit, &gpEmit));
                _ASSERTE(TypeFromToken(gpEmit) == mdtGenericParamConstraint);
                IfFailGo(pMiniMdEmit->GetGenericParamConstraintRecord(RidFromToken(gpEmit), &pRecEmit));
                tkConstraintEmit = pMiniMdEmit->getConstraintOfGenericParamConstraint(pRecEmit);

                // The one we want?
                if (tkConstraintEmit == tkConstraintImport)
                    break;
            }
            if (iEmit >= cEmit)
            {
                IfFailGo(META_E_GENERICPARAM_INCONSISTENT); // Didn't find the constraint
            }
        }
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::VerifyGenericParamConstraints()


//*****************************************************************************
// Verify Methods
//*****************************************************************************
HRESULT NEWMERGER::VerifyMethods(
    MergeImportData *pImportData, 
    mdTypeDef       tdImport, 
    mdTypeDef       tdEmit)
{
    HRESULT     hr = NOERROR;
    CMiniMdRW   *pMiniMdImport;
    CMiniMdRW   *pMiniMdEmit;
    MethodRec   *pRecImp;
    MethodRec   *pRecEmit;
    ULONG       ridStart;
    ULONG       ridEnd;
    ULONG       i;
    
    TypeDefRec  *pTypeDefRec;
    LPCUTF8     szName;
    PCCOR_SIGNATURE pbSig;
    ULONG       cbSig;
    ULONG       cbEmit;
    CQuickBytes qbSig;
    TOKENREC    *pTokenRec;
    mdMethodDef mdImp;
    mdMethodDef mdEmit;
    MDTOKENMAP  *pCurTkMap;

    MergeTypeData *pMTD;
    BOOL        bSuppressMergeCheck;
    ULONG       cImport = 0;        // count of non-merge check suppressed methods
    mdCustomAttribute tkCA;

    pMiniMdEmit = GetMiniMdEmit();
    pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    pCurTkMap = pImportData->m_pMDTokenMap;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

    // Get a count of records in the import scope; prepare to enumerate them.
    IfFailGo(pMiniMdImport->GetTypeDefRecord(RidFromToken(tdImport), &pTypeDefRec));
    ridStart = pMiniMdImport->getMethodListOfTypeDef(pTypeDefRec);
    IfFailGo(pMiniMdImport->getEndMethodListOfTypeDef(RidFromToken(tdImport), &ridEnd));

    pMTD = m_rMTDs.Get(RidFromToken(tdEmit));

    // loop through all Methods of the TypeDef
    for (i = ridStart; i < ridEnd; i++)
    {
        IfFailGo(pMiniMdImport->GetMethodRid(i, (ULONG *)&mdImp));

        // only verify those Methods that are marked
        if ( pMiniMdImport->GetFilterTable()->IsMethodMarked(TokenFromRid(mdImp, mdtMethodDef)) == false)
            continue;
            
        IfFailGo(pMiniMdImport->GetMethodRecord(mdImp, &pRecImp));

        if (m_fDupCheck == FALSE && tdImport == pImportData->m_pRegMetaImport->m_tdModule) //   TokenFromRid(1, mdtTypeDef))
        {
            // No dup check. This is the scenario that we only have one import scope. Just copy over the
            // globals.
            goto CopyMethodLabel;
        }
          
        IfFailGo(pMiniMdImport->getNameOfMethod(pRecImp, &szName));
        IfFailGo(pMiniMdImport->getSignatureOfMethod(pRecImp, &pbSig, &cbSig));

        mdImp = TokenFromRid(mdImp, mdtMethodDef);

        if ( IsMdPrivateScope( pRecImp->GetFlags() ) )
        {
            // Trigger additive merge
            goto CopyMethodLabel;
        }

        // convert rid contained in signature to new scope
        IfFailGo(ImportHelper::MergeUpdateTokenInSig(
            NULL,                       // Assembly emit scope.
            pMiniMdEmit,                // The emit scope.
            NULL, NULL, 0,              // Import assembly scope information.
            pMiniMdImport,              // The scope to merge into the emit scope.
            pbSig,                      // signature from the imported scope
            pCurTkMap,                // Internal token mapping structure.
            &qbSig,                     // [OUT] translated signature
            0,                          // start from first byte of the signature
            0,                          // don't care how many bytes consumed
            &cbEmit));                  // number of bytes write to cbEmit

        hr = ImportHelper::FindMethod(
            pMiniMdEmit, 
            tdEmit, 
            szName, 
            (const COR_SIGNATURE *)qbSig.Ptr(), 
            cbEmit, 
            &mdEmit);
        
        bSuppressMergeCheck = pMTD->m_bSuppressMergeCheck || 
               ((pImportData->m_tkSuppressMergeCheckCtor != mdTokenNil) && 
                (S_OK == ImportHelper::FindCustomAttributeByToken(pMiniMdImport, 
                    mdImp, pImportData->m_tkSuppressMergeCheckCtor, NULL, 0, &tkCA)));

        if (bSuppressMergeCheck || (tdImport == pImportData->m_pRegMetaImport->m_tdModule))
        {
            // global functions! Make sure that we move over the non-duplicate global function
            // declaration
            //
            if (hr == S_OK)
            {
                // found the duplicate
                IfFailGo( VerifyMethod(pImportData, mdImp, mdEmit) );
            }
            else
            {
CopyMethodLabel:
                // not a duplicate! Copy over the 
                IfFailGo(pMiniMdEmit->AddMethodRecord(&pRecEmit, (RID *)&mdEmit));

                // copy the method content over
                IfFailGo( CopyMethod(pImportData, pRecImp, pRecEmit) );

                IfFailGo( pMiniMdEmit->AddMethodToTypeDef(RidFromToken(tdEmit), mdEmit));

                // record the token movement
                mdEmit = TokenFromRid(mdEmit, mdtMethodDef);
                IfFailGo( pMiniMdEmit->AddMemberDefToHash(
                    mdEmit, 
                    tdEmit) ); 

                mdImp = TokenFromRid(mdImp, mdtMethodDef);
                IfFailGo( pCurTkMap->InsertNotFound(mdImp, false, mdEmit, &pTokenRec) );

                // copy over the children
                IfFailGo( CopyParams(pImportData, mdImp, mdEmit) );
                IfFailGo( CopyGenericParams(pImportData, mdImp, mdEmit) );

            }
        }
        else
        {
            if (hr == S_OK)
            {
                // Good! We are supposed to find a duplicate
                IfFailGo( VerifyMethod(pImportData, mdImp, mdEmit) );
            }
            else
            {
                // Oops! The typedef is duplicated but the method is not!!
                hr = S_OK; // discard old error; new error will be returned from CheckContinuableError
                CheckContinuableErrorEx(META_E_METHD_NOT_FOUND, pImportData, mdImp);
            }
            
            cImport++;
        }
    }

    // The counts should be the same, unless this is <module>
    if (cImport != pMTD->m_cMethods && tdImport != pImportData->m_pRegMetaImport->m_tdModule)
    {
        CheckContinuableErrorEx(META_E_METHOD_COUNTS, pImportData, tdImport);
        // If we are here, the linker says this error is OK.
    }
ErrExit:
    return hr;
} // HRESULT NEWMERGER::VerifyMethods()


//*****************************************************************************
// verify a duplicated method
//*****************************************************************************
HRESULT NEWMERGER::VerifyMethod(
    MergeImportData *pImportData, 
    mdMethodDef mdImp,                      // [IN] the emit record to fill
    mdMethodDef mdEmit)                     // [IN] the record to import
{
    HRESULT     hr;
    MethodRec   *pRecImp;
    MethodRec   *pRecEmit;
    TOKENREC    *pTokenRec;
    CMiniMdRW   *pMiniMdImport;
    CMiniMdRW   *pMiniMdEmit;
    MDTOKENMAP  *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    pCurTkMap = pImportData->m_pMDTokenMap;

    IfFailGo( pCurTkMap->InsertNotFound(mdImp, true, mdEmit, &pTokenRec) );
    
    IfFailGo(pMiniMdImport->GetMethodRecord(RidFromToken(mdImp), &pRecImp));

    // We need to make sure that the impl flags are propagated .
    // Rules are: if the first method has miForwardRef flag set but the new method does not, 
    // we want to disable the miForwardRef flag. If the one found in the emit scope does not have
    // miForwardRef set and the second one doesn't either, we want to make sure that the rest of
    // impl flags are the same.
    //
    if ( !IsMiForwardRef( pRecImp->GetImplFlags() ) )
    {
        IfFailGo(pMiniMdEmit->GetMethodRecord(RidFromToken(mdEmit), &pRecEmit));
        if (!IsMiForwardRef(pRecEmit->GetImplFlags()))
        {
            // make sure the rest of ImplFlags are the same
            if (pRecEmit->GetImplFlags() != pRecImp->GetImplFlags())
            {
                // inconsistent in implflags
                CheckContinuableErrorEx(META_E_METHDIMPL_INCONSISTENT, pImportData, mdImp);
            }
        }
        else
        {
            // propagate the importing ImplFlags
            pRecEmit->SetImplFlags(pRecImp->GetImplFlags());
        }
    }

    // verify the children
    IfFailGo( VerifyParams(pImportData, mdImp, mdEmit) );
    IfFailGo( VerifyGenericParams(pImportData, mdImp, mdEmit) );

ErrExit:
    return hr;
} // HRESULT NEWMERGER::VerifyMethod()


//*****************************************************************************
// Verify Fields
//*****************************************************************************
HRESULT NEWMERGER::VerifyFields(
    MergeImportData *pImportData, 
    mdTypeDef       tdImport, 
    mdTypeDef       tdEmit)
{
    HRESULT     hr = NOERROR;
    CMiniMdRW   *pMiniMdImport;
    CMiniMdRW   *pMiniMdEmit;
    FieldRec    *pRecImp;
    FieldRec    *pRecEmit;
    mdFieldDef  fdImp;
    mdFieldDef  fdEmit;
    ULONG       ridStart;
    ULONG       ridEnd;
    ULONG       i;

    TypeDefRec  *pTypeDefRec;
    LPCUTF8     szName;
    PCCOR_SIGNATURE pbSig;
    ULONG       cbSig;
    ULONG       cbEmit;
    CQuickBytes qbSig;
    TOKENREC    *pTokenRec;
    MDTOKENMAP  *pCurTkMap;

    MergeTypeData *pMTD;
    BOOL        bSuppressMergeCheck;
    ULONG       cImport = 0;        // count of non-merge check suppressed fields
    mdCustomAttribute tkCA;

    pMiniMdEmit = GetMiniMdEmit();
    pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    pCurTkMap = pImportData->m_pMDTokenMap;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );
   
    // Get a count of records in the import scope; prepare to enumerate them.
    IfFailGo(pMiniMdImport->GetTypeDefRecord(RidFromToken(tdImport), &pTypeDefRec));
    ridStart = pMiniMdImport->getFieldListOfTypeDef(pTypeDefRec);
    IfFailGo(pMiniMdImport->getEndFieldListOfTypeDef(RidFromToken(tdImport), &ridEnd));
    
    pMTD = m_rMTDs.Get(RidFromToken(tdEmit));
 
    // loop through all fields of the TypeDef
    for (i = ridStart; i < ridEnd; i++)
    {
        IfFailGo(pMiniMdImport->GetFieldRid(i, (ULONG *)&fdImp));

        // only verify those fields that are marked
        if ( pMiniMdImport->GetFilterTable()->IsFieldMarked(TokenFromRid(fdImp, mdtFieldDef)) == false)
            continue;

        IfFailGo(pMiniMdImport->GetFieldRecord(fdImp, &pRecImp));

        if (m_fDupCheck == FALSE && tdImport == pImportData->m_pRegMetaImport->m_tdModule)
        {
            // No dup check. This is the scenario that we only have one import scope. Just copy over the
            // globals.
            goto CopyFieldLabel;
        }

        IfFailGo(pMiniMdImport->getNameOfField(pRecImp, &szName));
        IfFailGo(pMiniMdImport->getSignatureOfField(pRecImp, &pbSig, &cbSig));

        if ( IsFdPrivateScope(pRecImp->GetFlags()))
        {
            // Trigger additive merge
            fdImp = TokenFromRid(fdImp, mdtFieldDef);
            goto CopyFieldLabel;
        }

        // convert rid contained in signature to new scope
        IfFailGo(ImportHelper::MergeUpdateTokenInSig(
            NULL,                       // Assembly emit scope.
            pMiniMdEmit,                // The emit scope.
            NULL, NULL, 0,              // Import assembly scope information.
            pMiniMdImport,              // The scope to merge into the emit scope.
            pbSig,                      // signature from the imported scope
            pCurTkMap,                // Internal token mapping structure.
            &qbSig,                     // [OUT] translated signature
            0,                          // start from first byte of the signature
            0,                          // don't care how many bytes consumed
            &cbEmit));                  // number of bytes write to cbEmit

        hr = ImportHelper::FindField(
            pMiniMdEmit, 
            tdEmit, 
            szName, 
            (const COR_SIGNATURE *)qbSig.Ptr(), 
            cbEmit, 
            &fdEmit);

        fdImp = TokenFromRid(fdImp, mdtFieldDef);

        bSuppressMergeCheck = 
            (IsFdStatic(pRecImp->GetFlags()) && pMTD->m_bSuppressMergeCheck) || 
               ((pImportData->m_tkSuppressMergeCheckCtor != mdTokenNil) && 
                (S_OK == ImportHelper::FindCustomAttributeByToken(pMiniMdImport, 
                    fdImp, pImportData->m_tkSuppressMergeCheckCtor, NULL, 0, &tkCA)));

        if (bSuppressMergeCheck || (tdImport == pImportData->m_pRegMetaImport->m_tdModule))
        {
            // global data! Make sure that we move over the non-duplicate global function
            // declaration
            //
            if (hr == S_OK)
            {
                // found the duplicate
                IfFailGo( pCurTkMap->InsertNotFound(fdImp, true, fdEmit, &pTokenRec) );
            }
            else
            {
CopyFieldLabel:
                // not a duplicate! Copy over the 
                IfFailGo(pMiniMdEmit->AddFieldRecord(&pRecEmit, (RID *)&fdEmit));

                // copy the field record over 
                IfFailGo( CopyField(pImportData, pRecImp, pRecEmit) );

                IfFailGo( pMiniMdEmit->AddFieldToTypeDef(RidFromToken(tdEmit), fdEmit));

                // record the token movement
                fdEmit = TokenFromRid(fdEmit, mdtFieldDef);
                IfFailGo( pMiniMdEmit->AddMemberDefToHash(
                    fdEmit, 
                    tdEmit) ); 

                fdImp = TokenFromRid(fdImp, mdtFieldDef);
                IfFailGo( pCurTkMap->InsertNotFound(fdImp, false, fdEmit, &pTokenRec) );
            }
        }
        else
        {
            if (hr == S_OK)
            {
                // Good! We are supposed to find a duplicate
                IfFailGo( pCurTkMap->InsertNotFound(fdImp, true, fdEmit, &pTokenRec) );
            }
            else
            {
                // Oops! The typedef is duplicated but the field is not!!
                hr = S_OK; // discard old error; new error will be returned from CheckContinuableError
                CheckContinuableErrorEx(META_E_FIELD_NOT_FOUND, pImportData, fdImp);
            }
            
            cImport++;
        }
    }

    // The counts should be the same, unless this is <module>
    if (cImport != pMTD->m_cFields && tdImport != pImportData->m_pRegMetaImport->m_tdModule)
    {
        CheckContinuableErrorEx(META_E_FIELD_COUNTS, pImportData, tdImport);
        // If we are here, the linker says this error is OK.
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::VerifyFields()


//*****************************************************************************
// Verify Events
//*****************************************************************************
HRESULT NEWMERGER::VerifyEvents(
    MergeImportData *pImportData, 
    mdTypeDef       tdImport, 
    mdTypeDef       tdEmit)
{
    HRESULT     hr = NOERROR;
    CMiniMdRW   *pMiniMdImport;
    CMiniMdRW   *pMiniMdEmit;
    RID         ridEventMap, ridEventMapEmit;
    EventMapRec *pEventMapRec;
    EventRec    *pRecImport;
    ULONG       ridStart;
    ULONG       ridEnd;
    ULONG       i;
    mdEvent     evImport;
    mdEvent     evEmit;
    TOKENREC    *pTokenRec;
    LPCUTF8     szName;
    mdToken     tkType;
    MDTOKENMAP  *pCurTkMap;

    EventMapRec *pEventMapEmit;
    EventRec    *pRecEmit;
    MergeTypeData *pMTD;
    BOOL        bSuppressMergeCheck;
    ULONG       cImport = 0;        // count of non-merge check suppressed events
    mdCustomAttribute tkCA;

    pMiniMdEmit = GetMiniMdEmit();
    pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    pCurTkMap = pImportData->m_pMDTokenMap;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

    IfFailGo(pMiniMdImport->FindEventMapFor(RidFromToken(tdImport), &ridEventMap));
    if (!InvalidRid(ridEventMap))
    {
        // Get a count of records already in emit scope.
        IfFailGo(pMiniMdEmit->FindEventMapFor(RidFromToken(tdEmit), &ridEventMapEmit));

        if (InvalidRid(ridEventMapEmit)) {
            // If there is any event, create the eventmap record in the emit scope
            // Create new record.
            IfFailGo(pMiniMdEmit->AddEventMapRecord(&pEventMapEmit, &ridEventMapEmit));

            // Set parent.
            IfFailGo(pMiniMdEmit->PutToken(TBL_EventMap, EventMapRec::COL_Parent, pEventMapEmit, tdEmit));
        }

        // Get a count of records in the import scope; prepare to enumerate them.
        IfFailGo(pMiniMdImport->GetEventMapRecord(ridEventMap, &pEventMapRec));
        ridStart = pMiniMdImport->getEventListOfEventMap(pEventMapRec);
        IfFailGo(pMiniMdImport->getEndEventListOfEventMap(ridEventMap, &ridEnd));

        pMTD = m_rMTDs.Get(RidFromToken(tdEmit));

        for (i = ridStart; i < ridEnd; i++)
        {
            // get the property rid
            IfFailGo(pMiniMdImport->GetEventRid(i, (ULONG *)&evImport));

            // only verify those Events that are marked
            if ( pMiniMdImport->GetFilterTable()->IsEventMarked(TokenFromRid(evImport, mdtEvent)) == false)
                continue;
            
            IfFailGo(pMiniMdImport->GetEventRecord(evImport, &pRecImport));
            IfFailGo(pMiniMdImport->getNameOfEvent(pRecImport, &szName));
            tkType = pMiniMdImport->getEventTypeOfEvent( pRecImport );
            IfFailGo( pCurTkMap->Remap(tkType, &tkType) );
            evImport = TokenFromRid( evImport, mdtEvent);

            hr = ImportHelper::FindEvent(
                pMiniMdEmit, 
                tdEmit, 
                szName, 
                &evEmit);

            bSuppressMergeCheck = pMTD->m_bSuppressMergeCheck || 
               ((pImportData->m_tkSuppressMergeCheckCtor != mdTokenNil) && 
                (S_OK == ImportHelper::FindCustomAttributeByToken(pMiniMdImport, 
                    evImport, pImportData->m_tkSuppressMergeCheckCtor, NULL, 0, &tkCA)));

            if (bSuppressMergeCheck) 
            {

                if (hr == S_OK )
                {
                    // Good. We found the matching event when we have a duplicate typedef
                    IfFailGo( pCurTkMap->InsertNotFound(evImport, true, evEmit, &pTokenRec) );
                }
                else
                {
                    // not a duplicate! Copy over the 
                    IfFailGo(pMiniMdEmit->AddEventRecord(&pRecEmit, (RID *)&evEmit));

                    // copy the event record over 
                    IfFailGo( CopyEvent(pImportData, pRecImport, pRecEmit) );
                    
                    // Add Event to the EventMap.
                    IfFailGo( pMiniMdEmit->AddEventToEventMap(ridEventMapEmit, evEmit) );

                    // record the token movement
                    evEmit = TokenFromRid(evEmit, mdtEvent);

                    IfFailGo( pCurTkMap->InsertNotFound(evImport, false, evEmit, &pTokenRec) );

                    // copy over the method semantics
                    IfFailGo( CopyMethodSemantics(pImportData, evImport, evEmit) );
                }
            }
            else
            {
                if (hr == S_OK )
                {
                    // Good. We found the matching event when we have a duplicate typedef
                    IfFailGo( pCurTkMap->InsertNotFound(evImport, true, evEmit, &pTokenRec) );
                }
                else
                {
                    // Oops! The typedef is duplicated but the event is not!!
                    hr = S_OK; // discard old error; new error will be returned from CheckContinuableError
                    CheckContinuableErrorEx(META_E_EVENT_NOT_FOUND, pImportData, evImport);

                }

                cImport++;
            }
        }

        // The counts should be the same, unless this is <module>
        if (cImport != pMTD->m_cEvents && tdImport != pImportData->m_pRegMetaImport->m_tdModule)
        {
            CheckContinuableErrorEx(META_E_EVENT_COUNTS, pImportData, tdImport);
            // If we are here, the linker says this error is OK.
        }
    }
ErrExit:
    return hr;
} // HRESULT NEWMERGER::VerifyEvents()


//*****************************************************************************
// Verify Properties
//*****************************************************************************
HRESULT NEWMERGER::VerifyProperties(
    MergeImportData *pImportData, 
    mdTypeDef       tdImport, 
    mdTypeDef       tdEmit)
{
    HRESULT     hr = NOERROR;
    CMiniMdRW   *pMiniMdImport;
    CMiniMdRW   *pMiniMdEmit;
    RID         ridPropertyMap, ridPropertyMapEmit;
    PropertyMapRec *pPropertyMapRec;
    PropertyRec *pRecImport;
    ULONG       ridStart;
    ULONG       ridEnd;
    ULONG       i;
    mdProperty  prImp;
    mdProperty  prEmit;
    TOKENREC    *pTokenRec;
    LPCUTF8     szName;
    PCCOR_SIGNATURE pbSig;
    ULONG       cbSig;
    ULONG       cbEmit;
    CQuickBytes qbSig;
    MDTOKENMAP  *pCurTkMap;

    PropertyMapRec *pPropertyMapEmit;
    PropertyRec *pRecEmit;
    MergeTypeData *pMTD;
    BOOL        bSuppressMergeCheck;
    ULONG       cImport = 0;        // count of non-merge check suppressed properties
    mdCustomAttribute tkCA;

    pMiniMdEmit = GetMiniMdEmit();
    pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    pCurTkMap = pImportData->m_pMDTokenMap;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

    IfFailGo(pMiniMdImport->FindPropertyMapFor(RidFromToken(tdImport), &ridPropertyMap));
    if (!InvalidRid(ridPropertyMap))
    {
        // Get a count of records already in emit scope.
        IfFailGo(pMiniMdEmit->FindPropertyMapFor(RidFromToken(tdEmit), &ridPropertyMapEmit));

        if (InvalidRid(ridPropertyMapEmit))
        {
            // If there is any event, create the PropertyMap record in the emit scope
            // Create new record.
            IfFailGo(pMiniMdEmit->AddPropertyMapRecord(&pPropertyMapEmit, &ridPropertyMapEmit));

            // Set parent.
            IfFailGo(pMiniMdEmit->PutToken(TBL_PropertyMap, PropertyMapRec::COL_Parent, pPropertyMapEmit, tdEmit));
        }

        // Get a count of records in the import scope; prepare to enumerate them.
        IfFailGo(pMiniMdImport->GetPropertyMapRecord(ridPropertyMap, &pPropertyMapRec));
        ridStart = pMiniMdImport->getPropertyListOfPropertyMap(pPropertyMapRec);
        IfFailGo(pMiniMdImport->getEndPropertyListOfPropertyMap(ridPropertyMap, &ridEnd));

        pMTD = m_rMTDs.Get(RidFromToken(tdEmit));

        for (i = ridStart; i < ridEnd; i++)
        {
            // get the property rid
            IfFailGo(pMiniMdImport->GetPropertyRid(i, (ULONG *)&prImp));

            // only verify those Properties that are marked
            if ( pMiniMdImport->GetFilterTable()->IsPropertyMarked(TokenFromRid(prImp, mdtProperty)) == false)
                continue;
                        
            IfFailGo(pMiniMdImport->GetPropertyRecord(prImp, &pRecImport));
            IfFailGo(pMiniMdImport->getNameOfProperty(pRecImport, &szName));
            IfFailGo(pMiniMdImport->getTypeOfProperty(pRecImport, &pbSig, &cbSig));
            prImp = TokenFromRid( prImp, mdtProperty);

            // convert rid contained in signature to new scope
            IfFailGo( ImportHelper::MergeUpdateTokenInSig(
                NULL,                       // Emit assembly.
                pMiniMdEmit,                // The emit scope.
                NULL, NULL, 0,              // Import assembly scope information.
                pMiniMdImport,              // The scope to merge into the emit scope.
                pbSig,                      // signature from the imported scope
                pCurTkMap,                // Internal token mapping structure.
                &qbSig,                     // [OUT] translated signature
                0,                          // start from first byte of the signature
                0,                          // don't care how many bytes consumed
                &cbEmit) );                 // number of bytes write to cbEmit

            hr = ImportHelper::FindProperty(
                pMiniMdEmit, 
                tdEmit, 
                szName, 
                (PCCOR_SIGNATURE) qbSig.Ptr(), 
                cbEmit, 
                &prEmit);

            bSuppressMergeCheck = pMTD->m_bSuppressMergeCheck || 
               ((pImportData->m_tkSuppressMergeCheckCtor != mdTokenNil) && 
                (S_OK == ImportHelper::FindCustomAttributeByToken(pMiniMdImport, 
                    prImp, pImportData->m_tkSuppressMergeCheckCtor, NULL, 0, &tkCA)));
            
            if (bSuppressMergeCheck) 
            {
                if (hr == S_OK)
                {
                    // Good. We found the matching property when we have a duplicate typedef
                    IfFailGo( pCurTkMap->InsertNotFound(prImp, true, prEmit, &pTokenRec) );
                }
                else
                {
                    IfFailGo(pMiniMdEmit->AddPropertyRecord(&pRecEmit, (RID *)&prEmit));

                    // copy the property record over 
                    IfFailGo( CopyProperty(pImportData, pRecImport, pRecEmit) );

                    // Add Property to the PropertyMap.
                    IfFailGo( pMiniMdEmit->AddPropertyToPropertyMap(ridPropertyMapEmit, prEmit) );

                    // record the token movement
                    prEmit = TokenFromRid(prEmit, mdtProperty);

                    IfFailGo( pCurTkMap->InsertNotFound(prImp, false, prEmit, &pTokenRec) );

                    // copy over the method semantics
                    IfFailGo( CopyMethodSemantics(pImportData, prImp, prEmit) );
                }
            }
            else
            {
                if (hr == S_OK)
                {
                    // Good. We found the matching property when we have a duplicate typedef
                    IfFailGo( pCurTkMap->InsertNotFound(prImp, true, prEmit, &pTokenRec) );
                }
                else
                {
                    hr = S_OK; // discard old error; new error will be returned from CheckContinuableError
                    CheckContinuableErrorEx(META_E_PROP_NOT_FOUND, pImportData, prImp);
                }

                cImport++;
            }
        }

        // The counts should be the same, unless this is <module>
        if (cImport != pMTD->m_cProperties && tdImport != pImportData->m_pRegMetaImport->m_tdModule)
        {
            CheckContinuableErrorEx(META_E_PROPERTY_COUNTS, pImportData, tdImport);
            // If we are here, the linker says this error is OK.
        }
    }
ErrExit:
    return hr;
} // HRESULT NEWMERGER::VerifyProperties()


//*****************************************************************************
// Verify Parameters given a Method
//*****************************************************************************
HRESULT NEWMERGER::VerifyParams(
    MergeImportData *pImportData, 
    mdMethodDef     mdImport, 
    mdMethodDef     mdEmit)
{
    HRESULT     hr = NOERROR;
    ParamRec    *pRecImport = NULL;
    ParamRec    *pRecEmit = NULL;
    MethodRec   *pMethodRec;
    CMiniMdRW   *pMiniMdImport;
    CMiniMdRW   *pMiniMdEmit;
    ULONG       ridStart, ridEnd;
    ULONG       ridStartEmit, ridEndEmit;
    ULONG       cImport, cEmit;
    ULONG       i, j;
    mdParamDef  pdEmit = 0;
    mdParamDef  pdImp;
    TOKENREC    *pTokenRec;
    MDTOKENMAP  *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    pCurTkMap = pImportData->m_pMDTokenMap;

    // We know that the filter table is not null here.  Tell PREFIX that we know it.
    PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

    // Get count of params in import scope; prepare to enumerate.
    IfFailGo(pMiniMdImport->GetMethodRecord(RidFromToken(mdImport), &pMethodRec));
    ridStart = pMiniMdImport->getParamListOfMethod(pMethodRec);
    IfFailGo(pMiniMdImport->getEndParamListOfMethod(RidFromToken(mdImport), &ridEnd));
    cImport = ridEnd - ridStart;

    // Get count of params in emit scope; prepare to enumerate.
    IfFailGo(pMiniMdEmit->GetMethodRecord(RidFromToken(mdEmit), &pMethodRec));
    ridStartEmit = pMiniMdEmit->getParamListOfMethod(pMethodRec);
    IfFailGo(pMiniMdEmit->getEndParamListOfMethod(RidFromToken(mdEmit), &ridEndEmit));
    cEmit = ridEndEmit - ridStartEmit;
    
    // The counts should be the same.
    if (cImport != cEmit)
    {
        // That is, unless this is <module>, so get the method's parent.
        mdTypeDef tdImport;
        IfFailGo(pMiniMdImport->FindParentOfMethodHelper(mdImport, &tdImport));
        if (tdImport != pImportData->m_pRegMetaImport->m_tdModule)
            CheckContinuableErrorEx(META_E_PARAM_COUNTS, pImportData, mdImport);
            // If we are here, the linker says this error is OK.
    }

    // loop through all Parameters
    for (i = ridStart; i < ridEnd; i++)
    {
        // Get the importing param row
        IfFailGo(pMiniMdImport->GetParamRid(i, (ULONG *)&pdImp));

        // only verify those Params that are marked
        if ( pMiniMdImport->GetFilterTable()->IsParamMarked(TokenFromRid(pdImp, mdtParamDef)) == false)
            continue;
            

        IfFailGo(pMiniMdImport->GetParamRecord(pdImp, &pRecImport));
        pdImp = TokenFromRid(pdImp, mdtParamDef);

        // It turns out when we merge a typelib with itself, the emit and import scope
        // has different sequence of parameter
        //
        // find the corresponding emit param row
        for (j = ridStartEmit; j < ridEndEmit; j++)
        {
            IfFailGo(pMiniMdEmit->GetParamRid(j, (ULONG *)&pdEmit));
            IfFailGo(pMiniMdEmit->GetParamRecord(pdEmit, &pRecEmit));
            if (pRecEmit->GetSequence() == pRecImport->GetSequence())
                break;
        }

        if (j == ridEndEmit)
        {
            // did not find the corresponding parameter in the emiting scope
            hr = S_OK; // discard old error; new error will be returned from CheckContinuableError
            CheckContinuableErrorEx(META_S_PARAM_MISMATCH, pImportData, pdImp);
        }

        else
        {
            _ASSERTE( pRecEmit->GetSequence() == pRecImport->GetSequence() );

            pdEmit = TokenFromRid(pdEmit, mdtParamDef);
    
            // record the token movement
#ifdef WE_DONT_NEED_TO_CHECK_NAMES__THEY_DONT_AFFECT_ANYTHING
            LPCUTF8 szNameImp;
            LPCUTF8 szNameEmit;
            IfFailGo(pMiniMdImport->getNameOfParam(pRecImport, &szNameImp));
            IfFailGo(pMiniMdEmit->getNameOfParam(pRecEmit, &szNameEmit));
            if (szNameImp && szNameEmit && strcmp(szNameImp, szNameEmit) != 0)
            {
                // parameter name doesn't match
                CheckContinuableErrorEx(META_S_PARAM_MISMATCH, pImportData, pdImp);
            }
#endif
            if (pRecEmit->GetFlags() != pRecImport->GetFlags())
            {
                // flags doesn't match
                CheckContinuableErrorEx(META_S_PARAM_MISMATCH, pImportData, pdImp);
            }

            // record token movement. This is a duplicate.
            IfFailGo( pCurTkMap->InsertNotFound(pdImp, true, pdEmit, &pTokenRec) );
        }
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::VerifyParams()


//*****************************************************************************
// merging MemberRef
//*****************************************************************************
HRESULT NEWMERGER::MergeMemberRefs( ) 
{
    HRESULT         hr = NOERROR;
    MemberRefRec    *pRecImport = NULL;
    MemberRefRec    *pRecEmit = NULL;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           iCount;
    ULONG           i;
    mdMemberRef     mrEmit;
    mdMemberRef     mrImp;
    bool            bDuplicate = false;
    TOKENREC        *pTokenRec;
    mdToken         tkParentImp;
    mdToken         tkParentEmit;

    LPCUTF8         szNameImp;
    PCCOR_SIGNATURE pbSig;
    ULONG           cbSig;
    ULONG           cbEmit;
    CQuickBytes     qbSig;

    bool            isRefOptimizedToDef;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;

        iCount = pMiniMdImport->getCountMemberRefs();

        // loop through all MemberRef
        for (i = 1; i <= iCount; i++)
        {

            // only merge those MemberRefs that are marked
            if ( pMiniMdImport->GetFilterTable()->IsMemberRefMarked(TokenFromRid(i, mdtMemberRef)) == false)
                continue;

            isRefOptimizedToDef = false;

            // compare it with the emit scope
            IfFailGo(pMiniMdImport->GetMemberRefRecord(i, &pRecImport));
            IfFailGo(pMiniMdImport->getNameOfMemberRef(pRecImport, &szNameImp));
            IfFailGo(pMiniMdImport->getSignatureOfMemberRef(pRecImport, &pbSig, &cbSig));
            tkParentImp = pMiniMdImport->getClassOfMemberRef(pRecImport);

            IfFailGo( pCurTkMap->Remap(tkParentImp, &tkParentEmit) );

            // convert rid contained in signature to new scope
            IfFailGo(ImportHelper::MergeUpdateTokenInSig(
                NULL,                       // Assembly emit scope.
                pMiniMdEmit,                // The emit scope.
                NULL, NULL, 0,              // Import assembly information.
                pMiniMdImport,              // The scope to merge into the emit scope.
                pbSig,                      // signature from the imported scope
                pCurTkMap,                // Internal token mapping structure.
                &qbSig,                     // [OUT] translated signature
                0,                          // start from first byte of the signature
                0,                          // don't care how many bytes consumed
                &cbEmit));                  // number of bytes write to cbEmit

            // We want to know if we can optimize this MemberRef to a FieldDef or MethodDef
            if (TypeFromToken(tkParentEmit) == mdtTypeDef && RidFromToken(tkParentEmit) != 0)
            {
                // The parent of this MemberRef has been successfully optimized to a TypeDef. Then this MemberRef should be 
                // be able to optimized to a MethodDef or FieldDef unless one of the parent in the inheritance hierachy
                // is through TypeRef. Then this MemberRef stay as MemberRef. If This is a VarArg calling convention, then 
                // we will remap the MemberRef's parent to a MethodDef or stay as TypeRef.
                //
                mdToken     tkParent = tkParentEmit;
                mdToken     tkMethDefOrFieldDef;
                PCCOR_SIGNATURE pbSigTmp = (const COR_SIGNATURE *) qbSig.Ptr();

                while (TypeFromToken(tkParent) == mdtTypeDef && RidFromToken(tkParent) != 0)
                {
                    TypeDefRec      *pRec;
                    hr = ImportHelper::FindMember(pMiniMdEmit, tkParent, szNameImp, pbSigTmp, cbEmit, &tkMethDefOrFieldDef);
                    if (hr == S_OK)
                    {
                        // We have found a match!!
                        if (isCallConv(CorSigUncompressCallingConv(pbSigTmp), IMAGE_CEE_CS_CALLCONV_VARARG))
                        {
                            // The found MethodDef token will replace this MemberRef's parent token
                            _ASSERTE(TypeFromToken(tkMethDefOrFieldDef) == mdtMethodDef);
                            tkParentEmit = tkMethDefOrFieldDef;
                            break;
                        }
                        else
                        {
                            // The found MethodDef/FieldDef token will replace this MemberRef token and we won't introduce a MemberRef 
                            // record.
                            //
                            mrEmit = tkMethDefOrFieldDef;
                            isRefOptimizedToDef = true;
                            bDuplicate = true;
                            break;
                        }
                    }

                    // now walk up to the parent class of tkParent and try to resolve this MemberRef
                    IfFailGo(pMiniMdEmit->GetTypeDefRecord(RidFromToken(tkParent), &pRec));
                    tkParent = pMiniMdEmit->getExtendsOfTypeDef(pRec);
                }

                // When we exit the loop, there are several possibilities:
                // 1. We found a MethodDef/FieldDef to replace the MemberRef
                // 2. We found a MethodDef matches the MemberRef but the MemberRef is VarArg, thus we want to use the MethodDef in the 
                // parent column but not replacing it.
                // 3. We exit because we run out the TypeDef on the parent chain. If it is because we encounter a TypeRef, this TypeRef will
                // replace the parent column of the MemberRef. Or we encounter nil token! (This can be unresolved global MemberRef or
                // compiler error to put an undefined MemberRef. In this case, we should just use the old tkParentEmit
                // on the parent column for the MemberRef.

                if (TypeFromToken(tkParent) == mdtTypeRef && RidFromToken(tkParent) != 0)
                {
                    // we had walked up the parent's chain to resolve it but we have not been successful and got stopped by a TypeRef.
                    // Then we will use this TypeRef as the parent of the emit MemberRef record
                    //
                    tkParentEmit = tkParent;
                }
            }
            else if ((TypeFromToken(tkParentEmit) == mdtMethodDef && 
                      !isCallConv(CorSigUncompressCallingConv(pbSig), IMAGE_CEE_CS_CALLCONV_VARARG)) || 
                     (TypeFromToken(tkParentEmit) == mdtFieldDef))
            {
                // If the MemberRef's parent is already a non-vararg MethodDef or FieldDef, we can also
                // safely drop the MemberRef
                mrEmit = tkParentEmit;
                isRefOptimizedToDef = true;
                bDuplicate = true;
            }

            // If the Ref cannot be optimized to a Def or MemberRef to Def optmization is turned off, do the following.
            if (isRefOptimizedToDef == false || !((m_optimizeRefToDef & MDMemberRefToDef) == MDMemberRefToDef))
            {
                // does this MemberRef already exist in the emit scope?
                if ( m_fDupCheck && ImportHelper::FindMemberRef(
                    pMiniMdEmit, 
                    tkParentEmit, 
                    szNameImp, 
                    (const COR_SIGNATURE *) qbSig.Ptr(), 
                    cbEmit, 
                    &mrEmit) == S_OK )
                {
                    // Yes, it does
                    bDuplicate = true;
                }
                else
                {
                    // No, it doesn't. Copy it over.
                    bDuplicate = false;
                    IfFailGo(pMiniMdEmit->AddMemberRefRecord(&pRecEmit, (RID *)&mrEmit));
                    mrEmit = TokenFromRid( mrEmit, mdtMemberRef );

                    // Copy over the MemberRef context
                    IfFailGo(pMiniMdEmit->PutString(TBL_MemberRef, MemberRefRec::COL_Name, pRecEmit, szNameImp));
                    IfFailGo(pMiniMdEmit->PutToken(TBL_MemberRef, MemberRefRec::COL_Class, pRecEmit, tkParentEmit));
                    IfFailGo(pMiniMdEmit->PutBlob(TBL_MemberRef, MemberRefRec::COL_Signature, pRecEmit, 
                                                qbSig.Ptr(), cbEmit));
                    IfFailGo(pMiniMdEmit->AddMemberRefToHash(mrEmit) );
                }
            }
            // record the token movement
            mrImp = TokenFromRid(i, mdtMemberRef);
            IfFailGo( pCurTkMap->InsertNotFound(mrImp, bDuplicate, mrEmit, &pTokenRec) );
        }
    }


ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeMemberRefs()


//*****************************************************************************
// merge interface impl
//*****************************************************************************
HRESULT NEWMERGER::MergeInterfaceImpls( ) 
{
    HRESULT         hr = NOERROR;
    InterfaceImplRec    *pRecImport = NULL;
    InterfaceImplRec    *pRecEmit = NULL;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           iCount;
    ULONG           i;
    mdTypeDef       tkParent;
    mdInterfaceImpl iiEmit;
    bool            bDuplicate;
    TOKENREC        *pTokenRec;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountInterfaceImpls();

        // loop through all InterfaceImpl
        for (i = 1; i <= iCount; i++)
        {
            // only merge those InterfaceImpls that are marked
            if ( pMiniMdImport->GetFilterTable()->IsInterfaceImplMarked(TokenFromRid(i, mdtInterfaceImpl)) == false)
                continue;

            // compare it with the emit scope
            IfFailGo(pMiniMdImport->GetInterfaceImplRecord(i, &pRecImport));
            tkParent = pMiniMdImport->getClassOfInterfaceImpl(pRecImport);

            // does this TypeRef already exist in the emit scope?
            if ( pCurTkMap->Find(tkParent, &pTokenRec) )
            {
                if ( pTokenRec->m_isDuplicate )
                {
                    // parent in the emit scope
                    mdToken     tkParentEmit;
                    mdToken     tkInterface;

                    // remap the typedef token
                    tkParentEmit = pTokenRec->m_tkTo;

                    // remap the implemented interface token
                    tkInterface = pMiniMdImport->getInterfaceOfInterfaceImpl(pRecImport);
                    IfFailGo( pCurTkMap->Remap( tkInterface, &tkInterface) );

                    // Set duplicate flag
                    bDuplicate = true;

                    // find the corresponding interfaceimpl in the emit scope
                    if ( ImportHelper::FindInterfaceImpl(pMiniMdEmit, tkParentEmit, tkInterface, &iiEmit) != S_OK )
                    {
                        // bad state!! We have a duplicate typedef but the interface impl is not the same!!

                        // continuable error
                        CheckContinuableErrorEx(
                            META_E_INTFCEIMPL_NOT_FOUND, 
                            pImportData, 
                            TokenFromRid(i, mdtInterfaceImpl));

                        iiEmit = mdTokenNil;
                    }
                }
                else
                {
                    // No, it doesn't. Copy it over.
                    bDuplicate = false;
                    IfFailGo(pMiniMdEmit->AddInterfaceImplRecord(&pRecEmit, (RID *)&iiEmit));

                    // copy the interfaceimp record over 
                    IfFailGo( CopyInterfaceImpl( pRecEmit, pImportData, pRecImport) );
                }
            }
            else
            {
                _ASSERTE( !"bad state!");
                IfFailGo( META_E_BADMETADATA );
            }

            // record the token movement
            IfFailGo( pCurTkMap->InsertNotFound(
                TokenFromRid(i, mdtInterfaceImpl), 
                bDuplicate, 
                TokenFromRid( iiEmit, mdtInterfaceImpl ), 
                &pTokenRec) );
        }
    }


ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeInterfaceImpls()


//*****************************************************************************
// merge all of the constant for field, property, and parameter
//*****************************************************************************
HRESULT NEWMERGER::MergeConstants() 
{
    HRESULT         hr = NOERROR;
    ConstantRec     *pRecImport = NULL;
    ConstantRec     *pRecEmit = NULL;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           iCount;
    ULONG           i;
    ULONG           csEmit;                 // constant value is not a token
    mdToken         tkParentImp;
    TOKENREC        *pTokenRec;
    void const      *pValue;
    ULONG           cbBlob;
#if _DEBUG
    ULONG           typeParent;
#endif // _DEBUG

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountConstants();

        // loop through all Constants
        for (i = 1; i <= iCount; i++)
        {
            // compare it with the emit scope
            IfFailGo(pMiniMdImport->GetConstantRecord(i, &pRecImport));
            tkParentImp = pMiniMdImport->getParentOfConstant(pRecImport);

            // only move those constant over if their parents are marked
            // If MDTOKENMAP::Find returns false, we don't need to copy the constant value over
            if ( pCurTkMap->Find(tkParentImp, &pTokenRec) )
            {
                // If the parent is duplicated, no need to move over the constant value
                if ( !pTokenRec->m_isDuplicate )
                {
                    IfFailGo(pMiniMdEmit->AddConstantRecord(&pRecEmit, &csEmit));
                    pRecEmit->SetType(pRecImport->GetType());

                    // set the parent
                    IfFailGo( pMiniMdEmit->PutToken(TBL_Constant, ConstantRec::COL_Parent, pRecEmit, pTokenRec->m_tkTo) );

                    // move over the constant blob value
                    IfFailGo(pMiniMdImport->getValueOfConstant(pRecImport, (const BYTE **)&pValue, &cbBlob));
                    IfFailGo( pMiniMdEmit->PutBlob(TBL_Constant, ConstantRec::COL_Value, pRecEmit, pValue, cbBlob) );
                    IfFailGo( pMiniMdEmit->AddConstantToHash(csEmit) );
                }
                else
                {
                    // <TODO>@FUTURE: more verification on the duplicate??</TODO>
                }
            }
#if _DEBUG
            // Include this block only under Debug build. The reason is that 
            // the linker chooses all the errors that we report (such as unmatched MethodDef or FieldDef)
            // as a continuable error. It is likely to hit this else while the tkparentImp is marked if there
            // is any error reported earlier!!
            else
            {
                typeParent = TypeFromToken(tkParentImp);
                if (typeParent == mdtFieldDef)
                {
                    // FieldDef should not be marked.
                    if ( pMiniMdImport->GetFilterTable()->IsFieldMarked(tkParentImp) == false)
                        continue;
                }
                else if (typeParent == mdtParamDef)
                {
                    // ParamDef should not be marked.
                    if ( pMiniMdImport->GetFilterTable()->IsParamMarked(tkParentImp) == false)
                        continue;
                }
                else
                {
                    _ASSERTE(typeParent == mdtProperty);
                    // Property should not be marked.
                    if ( pMiniMdImport->GetFilterTable()->IsPropertyMarked(tkParentImp) == false)
                        continue;
                }

                // If we come to here, we have a constant whose parent is marked but we could not
                // find it in the map!! Bad state.

                _ASSERTE(!"Ignore this error if you have seen error reported earlier! Otherwise bad token map or bad metadata!");
            }
#endif // _DEBUG
            // Note that we don't need to record the token movement since constant is not a valid token kind.
        }
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeConstants()


//*****************************************************************************
// Merge field marshal information
//*****************************************************************************
HRESULT NEWMERGER::MergeFieldMarshals() 
{
    HRESULT     hr = NOERROR;
    FieldMarshalRec *pRecImport = NULL;
    FieldMarshalRec *pRecEmit = NULL;
    CMiniMdRW   *pMiniMdImport;
    CMiniMdRW   *pMiniMdEmit;
    ULONG       iCount;
    ULONG       i;
    ULONG       fmEmit;                 // FieldMarhsal is not a token 
    mdToken     tkParentImp;
    TOKENREC    *pTokenRec;
    void const  *pValue;
    ULONG       cbBlob;
#if _DEBUG
    ULONG       typeParent;
#endif // _DEBUG

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountFieldMarshals();

        // loop through all TypeRef
        for (i = 1; i <= iCount; i++)
        {
            // compare it with the emit scope
            IfFailGo(pMiniMdImport->GetFieldMarshalRecord(i, &pRecImport));
            tkParentImp = pMiniMdImport->getParentOfFieldMarshal(pRecImport);

            // We want to merge only those field marshals that parents are marked.
            // Find will return false if the parent is not marked
            //
            if ( pCurTkMap->Find(tkParentImp, &pTokenRec) )
            {
                // If the parent is duplicated, no need to move over the constant value
                if ( !pTokenRec->m_isDuplicate )
                {
                    IfFailGo(pMiniMdEmit->AddFieldMarshalRecord(&pRecEmit, &fmEmit));

                    // set the parent
                    IfFailGo( pMiniMdEmit->PutToken(
                        TBL_FieldMarshal, 
                        FieldMarshalRec::COL_Parent, 
                        pRecEmit, 
                        pTokenRec->m_tkTo) );

                    // move over the constant blob value
                    IfFailGo(pMiniMdImport->getNativeTypeOfFieldMarshal(pRecImport, (const BYTE **)&pValue, &cbBlob));
                    IfFailGo( pMiniMdEmit->PutBlob(TBL_FieldMarshal, FieldMarshalRec::COL_NativeType, pRecEmit, pValue, cbBlob) );
                    IfFailGo( pMiniMdEmit->AddFieldMarshalToHash(fmEmit) );

                }
                else
                {
                    // <TODO>@FUTURE: more verification on the duplicate??</TODO>
                }
            }
#if _DEBUG
            else
            {
                typeParent = TypeFromToken(tkParentImp);

                if (typeParent == mdtFieldDef)
                {
                    // FieldDefs should not be marked
                    if ( pMiniMdImport->GetFilterTable()->IsFieldMarked(tkParentImp) == false)
                        continue;
                }
                else
                {
                    _ASSERTE(typeParent == mdtParamDef);
                    // ParamDefs should not be  marked
                    if ( pMiniMdImport->GetFilterTable()->IsParamMarked(tkParentImp) == false)
                        continue;
                }

                // If we come to here, that is we have a FieldMarshal whose parent is marked and we don't find it
                // in the map!!!

                // either bad lookup map or bad metadata
                _ASSERTE(!"Ignore this assert if you have seen error reported earlier. Otherwise, it is bad state!");
            }
#endif // _DEBUG
        }
        // Note that we don't need to record the token movement since FieldMarshal is not a valid token kind.
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeFieldMarshals()


//*****************************************************************************
// Merge class layout information
//*****************************************************************************
HRESULT NEWMERGER::MergeClassLayouts() 
{
    HRESULT         hr = NOERROR;
    ClassLayoutRec  *pRecImport = NULL;
    ClassLayoutRec  *pRecEmit = NULL;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           iCount;
    ULONG           i;
    ULONG           iRecord;                    // class layout is not a token
    mdToken         tkParentImp;
    TOKENREC        *pTokenRec;
    RID             ridClassLayout;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountClassLayouts();

        // loop through all TypeRef
        for (i = 1; i <= iCount; i++)
        {
            // compare it with the emit scope
            IfFailGo(pMiniMdImport->GetClassLayoutRecord(i, &pRecImport));
            tkParentImp = pMiniMdImport->getParentOfClassLayout(pRecImport);

            // only merge those TypeDefs that are marked
            if ( pMiniMdImport->GetFilterTable()->IsTypeDefMarked(tkParentImp) == false)
                continue;

            if ( pCurTkMap->Find(tkParentImp, &pTokenRec) )
            {
                if ( !pTokenRec->m_isDuplicate )
                {
                    // If the parent is not duplicated, just copy over the classlayout information
                    IfFailGo(pMiniMdEmit->AddClassLayoutRecord(&pRecEmit, &iRecord));

                    // copy over the fix part information
                    pRecEmit->Copy(pRecImport);
                    IfFailGo( pMiniMdEmit->PutToken(TBL_ClassLayout, ClassLayoutRec::COL_Parent, pRecEmit, pTokenRec->m_tkTo));
                    IfFailGo( pMiniMdEmit->AddClassLayoutToHash(iRecord) );
                }
                else
                {

                    IfFailGo(pMiniMdEmit->FindClassLayoutHelper(pTokenRec->m_tkTo, &ridClassLayout));

                    if (InvalidRid(ridClassLayout))
                    {
                        // class is duplicated but not class layout info
                        CheckContinuableErrorEx(META_E_CLASS_LAYOUT_INCONSISTENT, pImportData, tkParentImp);
                    }
                    else
                    {
                        IfFailGo(pMiniMdEmit->GetClassLayoutRecord(RidFromToken(ridClassLayout), &pRecEmit));
                        if (pMiniMdImport->getPackingSizeOfClassLayout(pRecImport) != pMiniMdEmit->getPackingSizeOfClassLayout(pRecEmit) || 
                            pMiniMdImport->getClassSizeOfClassLayout(pRecImport) != pMiniMdEmit->getClassSizeOfClassLayout(pRecEmit) )
                        {
                            CheckContinuableErrorEx(META_E_CLASS_LAYOUT_INCONSISTENT, pImportData, tkParentImp);
                        }
                    }
                }
            }
            else
            {
                // bad lookup map
                _ASSERTE( !"bad state!");
                IfFailGo( META_E_BADMETADATA );
            }
            // no need to record the index movement. Classlayout is not a token.
        }
    }
ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeClassLayouts()

//*****************************************************************************
// Merge field layout information
//*****************************************************************************
HRESULT NEWMERGER::MergeFieldLayouts() 
{
    HRESULT         hr = NOERROR;
    FieldLayoutRec *pRecImport = NULL;
    FieldLayoutRec *pRecEmit = NULL;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           iCount;
    ULONG           i;
    ULONG           iRecord;                    // field layout2 is not a token.
    mdToken         tkFieldImp;
    TOKENREC        *pTokenRec;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountFieldLayouts();

        // loop through all FieldLayout records.
        for (i = 1; i <= iCount; i++)
        {
            // compare it with the emit scope
            IfFailGo(pMiniMdImport->GetFieldLayoutRecord(i, &pRecImport));
            tkFieldImp = pMiniMdImport->getFieldOfFieldLayout(pRecImport);
        
            // only merge those FieldDefs that are marked
            if ( pMiniMdImport->GetFilterTable()->IsFieldMarked(tkFieldImp) == false)
                continue;

            if ( pCurTkMap->Find(tkFieldImp, &pTokenRec) )
            {
                if ( !pTokenRec->m_isDuplicate )
                {
                    // If the Field is not duplicated, just copy over the FieldLayout information
                    IfFailGo(pMiniMdEmit->AddFieldLayoutRecord(&pRecEmit, &iRecord));

                    // copy over the fix part information
                    pRecEmit->Copy(pRecImport);
                    IfFailGo( pMiniMdEmit->PutToken(TBL_FieldLayout, FieldLayoutRec::COL_Field, pRecEmit, pTokenRec->m_tkTo));
                    IfFailGo( pMiniMdEmit->AddFieldLayoutToHash(iRecord) );
                }
                else
                {
                    // <TODO>@FUTURE: more verification??</TODO>
                }
            }
            else
            {
                // bad lookup map
                _ASSERTE( !"bad state!");
                IfFailGo( META_E_BADMETADATA );
            }
            // no need to record the index movement. fieldlayout2 is not a token.
        }
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeFieldLayouts()


//*****************************************************************************
// Merge field RVAs
//*****************************************************************************
HRESULT NEWMERGER::MergeFieldRVAs() 
{
    HRESULT         hr = NOERROR;
    FieldRVARec     *pRecImport = NULL;
    FieldRVARec     *pRecEmit = NULL;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           iCount;
    ULONG           i;
    ULONG           iRecord;                    // FieldRVA is not a token.
    mdToken         tkFieldImp;
    TOKENREC        *pTokenRec;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountFieldRVAs();

        // loop through all FieldRVA records.
        for (i = 1; i <= iCount; i++)
        {
            // compare it with the emit scope
            IfFailGo(pMiniMdImport->GetFieldRVARecord(i, &pRecImport));
            tkFieldImp = pMiniMdImport->getFieldOfFieldRVA(pRecImport);
        
            // only merge those FieldDefs that are marked
            if ( pMiniMdImport->GetFilterTable()->IsFieldMarked(TokenFromRid(tkFieldImp, mdtFieldDef)) == false)
                continue;

            if ( pCurTkMap->Find(tkFieldImp, &pTokenRec) )
            {
                if ( !pTokenRec->m_isDuplicate )
                {
                    // If the Field is not duplicated, just copy over the FieldRVA information
                    IfFailGo(pMiniMdEmit->AddFieldRVARecord(&pRecEmit, &iRecord));

                    // copy over the fix part information
                    pRecEmit->Copy(pRecImport);
                    IfFailGo( pMiniMdEmit->PutToken(TBL_FieldRVA, FieldRVARec::COL_Field, pRecEmit, pTokenRec->m_tkTo));
                    IfFailGo( pMiniMdEmit->AddFieldRVAToHash(iRecord) );
                }
                else
                {
                    // <TODO>@FUTURE: more verification??</TODO>
                }
            }
            else
            {
                // bad lookup map
                _ASSERTE( !"bad state!");
                IfFailGo( META_E_BADMETADATA );
            }
            // no need to record the index movement. FieldRVA is not a token.
        }
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeFieldRVAs()


//*****************************************************************************
// Merge MethodImpl information
//*****************************************************************************
HRESULT NEWMERGER::MergeMethodImpls() 
{
    HRESULT     hr = NOERROR;
    MethodImplRec   *pRecImport = NULL;
    MethodImplRec   *pRecEmit = NULL;
    CMiniMdRW   *pMiniMdImport;
    CMiniMdRW   *pMiniMdEmit;
    ULONG       iCount;
    ULONG       i;
    RID         iRecord;
    mdTypeDef   tkClassImp;
    mdToken     tkBodyImp;
    mdToken     tkDeclImp;
    TOKENREC    *pTokenRecClass;
    mdToken     tkBodyEmit;
    mdToken     tkDeclEmit;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountMethodImpls();

        // loop through all the MethodImpls.
        for (i = 1; i <= iCount; i++)
        {
            // only merge those MethodImpls that are marked.
            if ( pMiniMdImport->GetFilterTable()->IsMethodImplMarked(i) == false)
                continue;

            // compare it with the emit scope
            IfFailGo(pMiniMdImport->GetMethodImplRecord(i, &pRecImport));
            tkClassImp = pMiniMdImport->getClassOfMethodImpl(pRecImport);
            tkBodyImp = pMiniMdImport->getMethodBodyOfMethodImpl(pRecImport);
            tkDeclImp = pMiniMdImport->getMethodDeclarationOfMethodImpl(pRecImport);

            if ( pCurTkMap->Find(tkClassImp, &pTokenRecClass))
            {
                // If the TypeDef is duplicated, no need to move over the MethodImpl record.
                if ( !pTokenRecClass->m_isDuplicate )
                {
                    // Create a new record and set the data.

                    // <TODO>@FUTURE: We might want to consider changing the error for the remap into a continuable error.
                    // Because we probably can continue merging for more data...</TODO>

                    IfFailGo( pCurTkMap->Remap(tkBodyImp, &tkBodyEmit) );
                    IfFailGo( pCurTkMap->Remap(tkDeclImp, &tkDeclEmit) );
                    IfFailGo(pMiniMdEmit->AddMethodImplRecord(&pRecEmit, &iRecord));
                    IfFailGo( pMiniMdEmit->PutToken(TBL_MethodImpl, MethodImplRec::COL_Class, pRecEmit, pTokenRecClass->m_tkTo) );
                    IfFailGo( pMiniMdEmit->PutToken(TBL_MethodImpl, MethodImplRec::COL_MethodBody, pRecEmit, tkBodyEmit) );
                    IfFailGo( pMiniMdEmit->PutToken(TBL_MethodImpl, MethodImplRec::COL_MethodDeclaration, pRecEmit, tkDeclEmit) );
                    IfFailGo( pMiniMdEmit->AddMethodImplToHash(iRecord) );
                }
                else
                {
                    // <TODO>@FUTURE: more verification on the duplicate??</TODO>
                }
                // No need to record the token movement, MethodImpl is not a token.
            }
            else
            {
                // either bad lookup map or bad metadata
                _ASSERTE(!"bad state");
                IfFailGo( META_E_BADMETADATA );
            }
        }
    }
ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeMethodImpls()


//*****************************************************************************
// Merge PInvoke
//*****************************************************************************
HRESULT NEWMERGER::MergePinvoke() 
{
    HRESULT         hr = NOERROR;
    ImplMapRec      *pRecImport = NULL;
    ImplMapRec      *pRecEmit = NULL;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           iCount;
    ULONG           i;
    mdModuleRef     mrImp;
    mdModuleRef     mrEmit;
    mdMethodDef     mdImp;
    RID             mdImplMap;
    TOKENREC        *pTokenRecMR;
    TOKENREC        *pTokenRecMD;

    USHORT          usMappingFlags;
    LPCUTF8         szImportName;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountImplMaps();

        // loop through all ImplMaps
        for (i = 1; i <= iCount; i++)
        {
            // compare it with the emit scope
            IfFailGo(pMiniMdImport->GetImplMapRecord(i, &pRecImport));

            // Get the MethodDef token in the new space.
            mdImp = pMiniMdImport->getMemberForwardedOfImplMap(pRecImport);

            // only merge those MethodDefs that are marked
            if ( pMiniMdImport->GetFilterTable()->IsMethodMarked(mdImp) == false)
                continue;

            // Get the ModuleRef token in the new space.
            mrImp = pMiniMdImport->getImportScopeOfImplMap(pRecImport);

            // map the token to the new scope
            if (pCurTkMap->Find(mrImp, &pTokenRecMR) == false)
            {
                // This should never fire unless the module refs weren't merged
                // before this code ran.
                _ASSERTE(!"Parent ModuleRef not found in MERGER::MergePinvoke.  Bad state!");
                IfFailGo( META_E_BADMETADATA );
            }

            // If the ModuleRef has been remapped to the "module token", we need to undo that
            //  for the pinvokeimpl.  A pinvoke can only have a ModuleRef for the ImportScope.
            mrEmit = pTokenRecMR->m_tkTo;
            if (mrEmit == MODULEDEFTOKEN)
            {   // Yes, the ModuleRef has been remapped to the module token.  So, 
                //  find the ModuleRef in the output scope; if it is not found, add
                //  it.
                ModuleRefRec    *pModRefImport;
                LPCUTF8         szNameImp;
                IfFailGo(pMiniMdImport->GetModuleRefRecord(RidFromToken(mrImp), &pModRefImport));
                IfFailGo(pMiniMdImport->getNameOfModuleRef(pModRefImport, &szNameImp));

                // does this ModuleRef already exist in the emit scope?
                hr = ImportHelper::FindModuleRef(pMiniMdEmit, 
                                                szNameImp, 
                                                &mrEmit);

                if (hr == CLDB_E_RECORD_NOTFOUND)
                {   // No, it doesn't. Copy it over.
                    ModuleRefRec    *pModRefEmit;
                    IfFailGo(pMiniMdEmit->AddModuleRefRecord(&pModRefEmit, (RID*)&mrEmit));
                    mrEmit = TokenFromRid(mrEmit, mdtModuleRef);

                    // Set ModuleRef Name.
                    IfFailGo( pMiniMdEmit->PutString(TBL_ModuleRef, ModuleRefRec::COL_Name, pModRefEmit, szNameImp) );
                }
                else
                    IfFailGo(hr);
            }


            if (pCurTkMap->Find(mdImp, &pTokenRecMD) == false)
            {
                // This should never fire unless the method defs weren't merged
                // before this code ran.
                _ASSERTE(!"Parent MethodDef not found in MERGER::MergePinvoke.  Bad state!");
                IfFailGo( META_E_BADMETADATA );
            }


            // Get copy of rest of data.
            usMappingFlags = pMiniMdImport->getMappingFlagsOfImplMap(pRecImport);
            IfFailGo(pMiniMdImport->getImportNameOfImplMap(pRecImport, &szImportName));

            // If the method associated with PInvokeMap is not duplicated, then don't bother to look up the 
            // duplicated PInvokeMap information.
            if (pTokenRecMD->m_isDuplicate == true)
            {
                // Does the correct ImplMap entry exist in the emit scope?
                IfFailGo(pMiniMdEmit->FindImplMapHelper(pTokenRecMD->m_tkTo, &mdImplMap));
            }
            else
            {
                mdImplMap = mdTokenNil;
            }
            if (!InvalidRid(mdImplMap))
            {
                // Verify that the rest of the data is identical, else it's an error.
                IfFailGo(pMiniMdEmit->GetImplMapRecord(mdImplMap, &pRecEmit));
                _ASSERTE(pMiniMdEmit->getMemberForwardedOfImplMap(pRecEmit) == pTokenRecMD->m_tkTo);
                LPCSTR szImplMapImportName;
                IfFailGo(pMiniMdEmit->getImportNameOfImplMap(pRecEmit, &szImplMapImportName));
                if (pMiniMdEmit->getImportScopeOfImplMap(pRecEmit) != mrEmit || 
                    pMiniMdEmit->getMappingFlagsOfImplMap(pRecEmit) != usMappingFlags || 
                    strcmp(szImplMapImportName, szImportName))
                {
                    // Mismatched p-invoke entries are found.
                    _ASSERTE(!"Mismatched P-invoke entries during merge.  Bad State!");
                    IfFailGo(E_FAIL);
                }
            }
            else
            {
                IfFailGo(pMiniMdEmit->AddImplMapRecord(&pRecEmit, &mdImplMap));

                // Copy rest of data.
                IfFailGo( pMiniMdEmit->PutToken(TBL_ImplMap, ImplMapRec::COL_MemberForwarded, pRecEmit, pTokenRecMD->m_tkTo) );
                IfFailGo( pMiniMdEmit->PutToken(TBL_ImplMap, ImplMapRec::COL_ImportScope, pRecEmit, mrEmit) );
                IfFailGo( pMiniMdEmit->PutString(TBL_ImplMap, ImplMapRec::COL_ImportName, pRecEmit, szImportName) );
                pRecEmit->SetMappingFlags(usMappingFlags);
                IfFailGo( pMiniMdEmit->AddImplMapToHash(mdImplMap) );
            }
        }
    }


ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergePinvoke()


//*****************************************************************************
// Merge StandAloneSigs
//*****************************************************************************
HRESULT NEWMERGER::MergeStandAloneSigs() 
{
    HRESULT         hr = NOERROR;
    StandAloneSigRec    *pRecImport = NULL;
    StandAloneSigRec    *pRecEmit = NULL;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           iCount;
    ULONG           i;
    TOKENREC        *pTokenRec;
    mdSignature     saImp;
    mdSignature     saEmit;
    bool            fDuplicate;
    PCCOR_SIGNATURE pbSig;
    ULONG           cbSig;
    ULONG           cbEmit;
    CQuickBytes     qbSig;
    PCOR_SIGNATURE  rgSig;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountStandAloneSigs();

        // loop through all Signatures
        for (i = 1; i <= iCount; i++)
        {
            // only merge those Signatures that are marked
            if ( pMiniMdImport->GetFilterTable()->IsSignatureMarked(TokenFromRid(i, mdtSignature)) == false)
                continue;

            // compare it with the emit scope
            IfFailGo(pMiniMdImport->GetStandAloneSigRecord(i, &pRecImport));
            IfFailGo(pMiniMdImport->getSignatureOfStandAloneSig(pRecImport, &pbSig, &cbSig));

            // This is a signature containing the return type after count of args
            // convert rid contained in signature to new scope
            IfFailGo(ImportHelper::MergeUpdateTokenInSig(
                NULL,                       // Assembly emit scope.
                pMiniMdEmit,                // The emit scope.
                NULL, NULL, 0,              // Assembly import scope info.
                pMiniMdImport,              // The scope to merge into the emit scope.
                pbSig,                      // signature from the imported scope
                pCurTkMap,                // Internal token mapping structure.
                &qbSig,                     // [OUT] translated signature
                0,                          // start from first byte of the signature
                0,                          // don't care how many bytes consumed
                &cbEmit));                  // number of bytes write to cbEmit
            rgSig = ( PCOR_SIGNATURE ) qbSig.Ptr();

            hr = ImportHelper::FindStandAloneSig(
                pMiniMdEmit, 
                rgSig, 
                cbEmit, 
                &saEmit );
            if ( hr == S_OK )
            {
                // find a duplicate
                fDuplicate = true;
            }
            else
            {
                // copy over
                fDuplicate = false;
                IfFailGo(pMiniMdEmit->AddStandAloneSigRecord(&pRecEmit, (ULONG *)&saEmit));
                saEmit = TokenFromRid(saEmit, mdtSignature);
                IfFailGo( pMiniMdEmit->PutBlob(TBL_StandAloneSig, StandAloneSigRec::COL_Signature, pRecEmit, rgSig, cbEmit));
            }
            saImp = TokenFromRid(i, mdtSignature);

            // Record the token movement
            IfFailGo( pCurTkMap->InsertNotFound(saImp, fDuplicate, saEmit, &pTokenRec) );
        }
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeStandAloneSigs()

//*****************************************************************************
// Merge MethodSpecs
//*****************************************************************************
HRESULT NEWMERGER::MergeMethodSpecs() 
{
    HRESULT         hr = NOERROR;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           iCount;
    ULONG           i;
    mdToken         tk;
    ULONG           iRecord;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;

        // Loop through all MethodSpec
        iCount = pMiniMdImport->getCountMethodSpecs();
        for (i=1; i<=iCount; ++i)
        {
            MethodSpecRec   *pRecImport;
            MethodSpecRec   *pRecEmit;
            TOKENREC        *pTokenRecMethod;
            TOKENREC        *pTokenRecMethodNew;
            PCCOR_SIGNATURE pvSig;
            ULONG           cbSig;
            CQuickBytes     qbSig;
            ULONG           cbEmit;
            
            // Only copy marked records.
            if (!pMiniMdImport->GetFilterTable()->IsMethodSpecMarked(i))
                continue;

            IfFailGo(pMiniMdImport->GetMethodSpecRecord(i, &pRecImport));
            tk = pMiniMdImport->getMethodOfMethodSpec(pRecImport);

            // Map the token to the new scope.
            if (pCurTkMap->Find(tk, &pTokenRecMethod) == false)
            {
                // This should never fire unless the TypeDefs/Refs weren't merged
                // before this code runs.
                _ASSERTE(!"MethodSpec method not found in MERGER::MergeGenericsInfo.  Bad state!");
                IfFailGo( META_E_BADMETADATA );
            }
            // Copy to output scope.
            IfFailGo(pMiniMdEmit->AddMethodSpecRecord(&pRecEmit, &iRecord));
            IfFailGo( pMiniMdEmit->PutToken(TBL_MethodSpec, MethodSpecRec::COL_Method, pRecEmit, pTokenRecMethod->m_tkTo));

            // Copy the signature, translating any embedded tokens.
            IfFailGo(pMiniMdImport->getInstantiationOfMethodSpec(pRecImport, &pvSig, &cbSig));
            
            //  ...convert rid contained in signature to new scope
            IfFailGo(ImportHelper::MergeUpdateTokenInSig(
                NULL,                       // Assembly emit scope.
                pMiniMdEmit,                // The emit scope.
                NULL, NULL, 0,              // Import assembly scope information.
                pMiniMdImport,              // The scope to merge into the emit scope.
                pvSig,                      // signature from the imported scope
                pCurTkMap,                  // Internal token mapping structure.
                &qbSig,                     // [OUT] translated signature
                0,                          // start from first byte of the signature
                0,                          // don't care how many bytes consumed
                &cbEmit));                  // number of bytes write to cbEmit

            // ...persist the converted signature
            IfFailGo( pMiniMdEmit->PutBlob(TBL_MethodSpec, MethodSpecRec::COL_Instantiation, pRecEmit, qbSig.Ptr(), cbEmit) );
            
            IfFailGo( pCurTkMap->InsertNotFound(TokenFromRid(i, mdtMethodSpec), false, 
                                                TokenFromRid(iRecord, mdtMethodSpec), &pTokenRecMethodNew) );
        }
    }

    ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeMethodSpecs()
    
//*****************************************************************************
// Merge DeclSecuritys
//*****************************************************************************
HRESULT NEWMERGER::MergeDeclSecuritys() 
{
    HRESULT         hr = NOERROR;
    DeclSecurityRec *pRecImport = NULL;
    DeclSecurityRec *pRecEmit = NULL;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           iCount;
    ULONG           i;
    mdToken         tkParentImp;
    TOKENREC        *pTokenRec;
    void const      *pValue;
    ULONG           cbBlob;
    mdPermission    pmImp;
    mdPermission    pmEmit;
    bool            fDuplicate;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountDeclSecuritys();

        // loop through all DeclSecurity
        for (i = 1; i <= iCount; i++)
        {
            // only merge those DeclSecurities that are marked
            if ( pMiniMdImport->GetFilterTable()->IsDeclSecurityMarked(TokenFromRid(i, mdtPermission)) == false)
                continue;
        
            // compare it with the emit scope
            IfFailGo(pMiniMdImport->GetDeclSecurityRecord(i, &pRecImport));
            tkParentImp = pMiniMdImport->getParentOfDeclSecurity(pRecImport);
            if ( pCurTkMap->Find(tkParentImp, &pTokenRec) )
            {
                if ( !pTokenRec->m_isDuplicate )
                {
                    // If the parent is not duplicated, just copy over the custom value
                    goto CopyPermission;
                }
                else
                {
                    // Try to see if the Permission is there in the emit scope or not.
                    // If not, move it over still
                    if ( ImportHelper::FindPermission(
                        pMiniMdEmit, 
                        pTokenRec->m_tkTo, 
                        pRecImport->GetAction(), 
                        &pmEmit) == S_OK )
                    {
                        // found a match
                        // <TODO>@FUTURE: more verification??</TODO>
                        fDuplicate = true;
                    }
                    else
                    {
                        // Parent is duplicated but the Permission is not. Still copy over the
                        // Permission.
CopyPermission:
                        fDuplicate = false;
                        IfFailGo(pMiniMdEmit->AddDeclSecurityRecord(&pRecEmit, (ULONG *)&pmEmit));
                        pmEmit = TokenFromRid(pmEmit, mdtPermission);

                        pRecEmit->Copy(pRecImport);

                        // set the parent
                        IfFailGo( pMiniMdEmit->PutToken(
                            TBL_DeclSecurity, 
                            DeclSecurityRec::COL_Parent, 
                            pRecEmit, 
                            pTokenRec->m_tkTo) );

                        // move over the CustomAttribute blob value
                        IfFailGo(pMiniMdImport->getPermissionSetOfDeclSecurity(pRecImport, (const BYTE **)&pValue, &cbBlob));
                        IfFailGo(pMiniMdEmit->PutBlob(
                            TBL_DeclSecurity, 
                            DeclSecurityRec::COL_PermissionSet, 
                            pRecEmit, 
                            pValue, 
                            cbBlob));
                    }
                }
                pmEmit = TokenFromRid(pmEmit, mdtPermission);
                pmImp = TokenFromRid(i, mdtPermission);

                // Record the token movement
                IfFailGo( pCurTkMap->InsertNotFound(pmImp, fDuplicate, pmEmit, &pTokenRec) );
            }
            else
            {
                // bad lookup map
                _ASSERTE(!"bad state");
                IfFailGo( META_E_BADMETADATA );
            }
        }
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeDeclSecuritys()


//*****************************************************************************
// Merge Strings
//*****************************************************************************
HRESULT NEWMERGER::MergeStrings() 
{
    HRESULT         hr = NOERROR;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    TOKENREC        *pTokenRec;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        
        for (UINT32 nIndex = 0; ;)
        {
            MetaData::DataBlob userString;
            UINT32 nNextIndex;
            UINT32 nEmitIndex;
            
            hr = pMiniMdImport->GetUserStringAndNextIndex(
                nIndex, 
                &userString, 
                &nNextIndex);
            IfFailGo(hr);
            if (hr == S_FALSE)
            {   // We reached the last user string
                hr = S_OK;
                break;
            }
            _ASSERTE(hr == S_OK);
            
            // Skip empty strings
            if (userString.IsEmpty())
            {
                nIndex = nNextIndex;
                continue;
            }
            
            if (pMiniMdImport->GetFilterTable()->IsUserStringMarked(TokenFromRid(nIndex, mdtString)) == false)
            {
                // Process next user string in the heap
                nIndex = nNextIndex;
                continue;
            }
            
            IfFailGo(pMiniMdEmit->PutUserString(
                userString, 
                &nEmitIndex));
            
            IfFailGo(pCurTkMap->InsertNotFound(
                TokenFromRid(nIndex, mdtString), 
                false, 
                TokenFromRid(nEmitIndex, mdtString), 
                &pTokenRec));
            
            // Process next user string in the heap
            nIndex = nNextIndex;
        }
    }
ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeStrings()

// Helper method to merge the module-level security critical attributes
// Strips all module-level security critical attribute [that won't be ultimately needed]
// Returns:
//      FAILED(hr): Failure occurred retrieving metadata or parsing scopes
//      S_OK: Attribute should be merged into final output scope
//      S_FALSE: Attribute should be ignored/dropped from output scope
HRESULT NEWMERGER::MergeSecurityCriticalModuleLevelAttributes(
    MergeImportData* pImportData,               // import scope
    mdToken tkParentImp,                        // parent token with attribute
    TOKENREC* pTypeRec,                         // token record of attribute ctor
    mdToken mrSecurityTreatAsSafeAttributeCtor, // 'generic' TAS attribute token
    mdToken mrSecurityTransparentAttributeCtor, // 'generic' Transparent attribute token
    mdToken mrSecurityCriticalExplicitAttributeCtor,    // 'generic' Critical attribute token
    mdToken mrSecurityCriticalEverythingAttributeCtor)
{
    HRESULT hr = S_OK;
    
        // if ANY assembly-level critical attributes were specified, then we'll output
        //      one assembly-level Critical(Explicit) attribute only
        // AND if this scope has tags
    if (ISSCS_Unknown != pImportData->m_isscsSecurityCriticalStatus)
    {
        _ASSERTE(ISSCS_Unknown != m_isscsSecurityCritical);
        // drop only assembly-level attributes
        TypeRefRec* pTypeRefRec;
            // metadata emitter
        CMiniMdRW* pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
        
        // if compiler is generating a module - then this will be a module token
        LPCSTR szTypeRefName;
        if (tkParentImp == MODULEDEFTOKEN || 
                // otherwise, if merging assemblies, we have a fake type ref called MODULE_CA_LOCATION
            (TypeFromToken(tkParentImp) == mdtTypeRef && 
            (IsAttributeFromNamespace(pMiniMdImport, tkParentImp, 
                COR_COMPILERSERVICE_NAMESPACE, COR_MSCORLIB_NAME, 
                &pTypeRefRec) == S_OK) && 
            (pMiniMdImport->getNameOfTypeRef(pTypeRefRec, &szTypeRefName) == S_OK) && 
            (strcmp(MODULE_CA_TYPENAME, szTypeRefName) == 0)))
        {
                // drop the TAS attribute (unless all scopes have TAS)
            if ( pTypeRec->m_tkTo == mrSecurityTreatAsSafeAttributeCtor )
            {
                if ((m_isscsSecurityCriticalAllScopes & ISSCS_SecurityTreatAsSafe) ==
                    ISSCS_SecurityTreatAsSafe)
                {
                    _ASSERTE((pImportData->m_isscsSecurityCriticalStatus & ISSCS_SecurityTreatAsSafe) ==
                             ISSCS_SecurityTreatAsSafe);
                        return S_OK;
                }
                return S_FALSE;
            }
                // drop the Transparent attribute (unless all scopes have Transparent)
            else if (pTypeRec->m_tkTo == mrSecurityTransparentAttributeCtor)
            {
                if ((m_isscsSecurityCriticalAllScopes & ISSCS_SecurityTransparent) ==
                    ISSCS_SecurityTransparent)
                {
                    _ASSERTE((pImportData->m_isscsSecurityCriticalStatus & ISSCS_SecurityTransparent) ==
                             ISSCS_SecurityTransparent);
                    return S_OK;
                }
                return S_FALSE;
            }
            else if (pTypeRec->m_tkTo == mrSecurityCriticalExplicitAttributeCtor)
            {
                // if NOT Critical Everything, then leave the Critical.Explicit attribute
                // the Critical.Explicit attribute will be used as the final global attribute
                if ((m_isscsSecurityCriticalAllScopes & ISSCS_SecurityCriticalEverything) !=
                    ISSCS_SecurityCriticalEverything)
                {
                    _ASSERTE((pImportData->m_isscsSecurityCriticalStatus & ISSCS_SecurityCriticalExplicit) ==
                             ISSCS_SecurityCriticalExplicit);
                    return S_OK;
                }
                else
                {
                        // drop this attribute
                    return S_FALSE;
                }
            }
            else if (pTypeRec->m_tkTo == mrSecurityCriticalEverythingAttributeCtor)
            {
                    // OPTIMIZATION: if all attributes are Critical.Everything, 
                    //      then leave the global Critical attribute
                if ((m_isscsSecurityCriticalAllScopes & ISSCS_SecurityCriticalEverything) ==
                    ISSCS_SecurityCriticalEverything)
                {
                    _ASSERTE((pImportData->m_isscsSecurityCriticalStatus & ISSCS_SecurityCriticalEverything) ==
                             ISSCS_SecurityCriticalEverything);
                    return S_OK;
                }
                else
                {
                        // drop this attribute
                    return S_FALSE;
                }
            }
        }
    }

    return hr;
} // NEWMERGER::MergeSecurityCriticalModuleLevelAttributes

// HELPER: Retrieve the meta-data info related to SecurityCritical
HRESULT NEWMERGER::RetrieveStandardSecurityCriticalMetaData(
        mdAssemblyRef& tkMscorlib, 
        mdTypeRef& securityEnum, 
        BYTE*& rgSigBytesSecurityCriticalEverythingCtor, 
        DWORD& dwSigEverythingSize, 
        BYTE*& rgSigBytesSecurityCriticalExplicitCtor, 
        DWORD& dwSigExplicitSize)
{
    HRESULT hr = S_OK;
    
    CMiniMdRW* emit = GetMiniMdEmit();

    // get typeref for mscorlib
    BYTE pbMscorlibToken[] = COR_MSCORLIB_TYPEREF;
    BYTE* pCurr = rgSigBytesSecurityCriticalEverythingCtor;

    IfFailGo(ImportHelper::FindAssemblyRef(emit, 
                                            COR_MSCORLIB_NAME, 
                                            NULL, 
                                            pbMscorlibToken, 
                                            sizeof(pbMscorlibToken), 
                                            asm_rmj, 
                                            asm_rmm, 
                                            asm_rup, 
                                            asm_rpt, 
                                            0, 
                                            &tkMscorlib));

    IfFailGo(m_pRegMetaEmit->DefineTypeRefByName(tkMscorlib, 
                                    COR_SECURITYCRITICALSCOPE_ENUM_W, 
                                    &securityEnum));

    // build the constructor sig that takes SecurityCriticalScope argument
    if (rgSigBytesSecurityCriticalEverythingCtor)
    {
        *pCurr++ = IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS;
        *pCurr++ = COR_SECURITYCRITICAL_CTOR_ARGCOUNT_SCOPE_EVERYTHING; // one argument to constructor
        *pCurr++ = ELEMENT_TYPE_VOID;
        *pCurr++ = ELEMENT_TYPE_VALUETYPE;
        pCurr += CorSigCompressToken(securityEnum, pCurr);
        dwSigEverythingSize = (DWORD)(pCurr - rgSigBytesSecurityCriticalEverythingCtor);
        _ASSERTE(dwSigEverythingSize <= COR_SECURITYCRITICAL_CTOR_SCOPE_SIG_MAX_SIZE);
    }
    
        // if Explicit ctor is requested
    if (rgSigBytesSecurityCriticalExplicitCtor)
    {
        // build the constructor sig that has NO arguments
        pCurr = rgSigBytesSecurityCriticalExplicitCtor;
        *pCurr++ = IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS;
        *pCurr++ = COR_SECURITYCRITICAL_CTOR_ARGCOUNT_NO_SCOPE; // no arguments to constructor
        *pCurr++ = ELEMENT_TYPE_VOID;
        dwSigExplicitSize = (DWORD)(pCurr - rgSigBytesSecurityCriticalExplicitCtor);
        _ASSERTE(dwSigExplicitSize <= COR_SECURITYCRITICAL_CTOR_NO_SCOPE_SIG_MAX_SIZE);
    }

ErrExit:
    return hr;
} // NEWMERGER::RetrieveStandardSecurityCriticalMetaData

//*****************************************************************************
// Merge CustomAttributes
//*****************************************************************************
HRESULT NEWMERGER::MergeCustomAttributes() 
{

    HRESULT         hr = NOERROR;
    CustomAttributeRec  *pRecImport = NULL;
    CustomAttributeRec  *pRecEmit = NULL;
    CMiniMdRW       *pMiniMdImport;
    CMiniMdRW       *pMiniMdEmit;
    ULONG           iCount;
    ULONG           i;
    mdToken         tkParentImp;            // Token of attributed object (parent).
    TOKENREC        *pTokenRec;             // Parent's remap.
    mdToken         tkType;                 // Token of attribute's type.
    TOKENREC        *pTypeRec;              // Type's remap.
    void const      *pValue;                // The actual value.
    ULONG           cbBlob;                 // Size of the value.
    mdToken         cvImp;
    mdToken         cvEmit;
    bool            fDuplicate;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    TypeRefRec      *pTypeRefRec;
    ULONG           cTypeRefRecs;
    mdToken         mrSuppressMergeCheckAttributeCtor = mdTokenNil;
    mdToken         mrSecurityCriticalExplicitAttributeCtor = mdTokenNil;
    mdToken         mrSecurityCriticalEverythingAttributeCtor = mdTokenNil;
    mdToken         mrSecurityTransparentAttributeCtor      = mdTokenNil;
    mdToken         mrSecurityTreatAsSafeAttributeCtor      = mdTokenNil;

    pMiniMdEmit = GetMiniMdEmit();

    // Find out the TypeRef referring to our library's System.CompilerServices.SuppressMergeCheckAttribute, 
    //   System.Security.SecurityCriticalAttribute, System.Security.SecurityTransparentAttribute, and 
    //   System.Security.SecurityTreatAsSafeAttibute
    cTypeRefRecs = pMiniMdEmit->getCountTypeRefs();

    { // retrieve global attribute TypeRefs
        
        mdAssemblyRef tkMscorlib = mdTokenNil;
        mdTypeRef securityEnum = mdTokenNil;

        NewArrayHolder<BYTE> rgSigBytesSecurityCriticalEverythingCtor(new (nothrow)BYTE[COR_SECURITYCRITICAL_CTOR_SCOPE_SIG_MAX_SIZE]);
        BYTE* pSigBytesSecurityCriticalEverythingCtor = rgSigBytesSecurityCriticalEverythingCtor.GetValue();
        IfFailGo((pSigBytesSecurityCriticalEverythingCtor == NULL)?E_OUTOFMEMORY:S_OK);
        DWORD dwSigEverythingSize = 0;

        NewArrayHolder<BYTE> rgSigBytesSecurityCriticalExplicitCtor(new (nothrow)BYTE[COR_SECURITYCRITICAL_CTOR_NO_SCOPE_SIG_MAX_SIZE]);
        BYTE* pSigBytesSecurityCriticalExplicitCtor = rgSigBytesSecurityCriticalExplicitCtor.GetValue();
        IfFailGo((pSigBytesSecurityCriticalExplicitCtor == NULL)?E_OUTOFMEMORY:S_OK);
        DWORD dwSigExplicitSize = 0;

            // retrieve security critical metadata info if necessary
        if(ISSCS_Unknown != m_isscsSecurityCritical)
        {

            hr = RetrieveStandardSecurityCriticalMetaData(
                    tkMscorlib, 
                    securityEnum, 
                    pSigBytesSecurityCriticalEverythingCtor, 
                    dwSigEverythingSize, 
                    pSigBytesSecurityCriticalExplicitCtor, 
                    dwSigExplicitSize);

        }
    
        // Search for the TypeRef.
        for (i = 1; i <= cTypeRefRecs; i++)
        {
            mdToken tkTmp = TokenFromRid(i,mdtTypeRef);

            if (IsAttributeFromNamespace(pMiniMdEmit, tkTmp, 
                COR_COMPILERSERVICE_NAMESPACE, COR_MSCORLIB_NAME, 
                &pTypeRefRec) == S_OK)
            {
                LPCSTR szNameOfTypeRef;
                IfFailGo(pMiniMdEmit->getNameOfTypeRef(pTypeRefRec, &szNameOfTypeRef));
                if (strcmp(szNameOfTypeRef, COR_SUPPRESS_MERGE_CHECK_ATTRIBUTE) == 0)
                {
                    hr = ImportHelper::FindMemberRef(
                                        pMiniMdEmit, tkTmp, 
                                        COR_CTOR_METHOD_NAME, 
                                        NULL, 0, 
                                        &mrSuppressMergeCheckAttributeCtor);
                    if (S_OK == hr) continue;
                }
            }
            else
                // if we are merging security critical attributes, then look for transparent-related attributes
            if ((ISSCS_Unknown != m_isscsSecurityCritical) && 
               (IsAttributeFromNamespace(pMiniMdEmit, tkTmp, 
                    COR_SECURITYCRITICAL_ATTRIBUTE_NAMESPACE, COR_MSCORLIB_NAME, 
                    &pTypeRefRec) == S_OK))
            {
                LPCSTR szNameOfTypeRef;
                IfFailGo(pMiniMdEmit->getNameOfTypeRef(pTypeRefRec, &szNameOfTypeRef));

                // look for the SecurityCritical attribute
                if (strcmp(szNameOfTypeRef, COR_SECURITYCRITICAL_ATTRIBUTE) == 0)
                {
                    // since the SecurityCritical attribute can be either
                    //  parameterless constructor or SecurityCriticalScope constructor, we
                    //  look for both
                    hr = ImportHelper::FindMemberRef(
                                            pMiniMdEmit, tkTmp, 
                                            COR_CTOR_METHOD_NAME, 
                                            rgSigBytesSecurityCriticalEverythingCtor.GetValue(), dwSigEverythingSize, 
                                            &mrSecurityCriticalEverythingAttributeCtor);
                    if (S_OK == hr) continue;
                    hr = ImportHelper::FindMemberRef(
                                            pMiniMdEmit, tkTmp, 
                                            COR_CTOR_METHOD_NAME, 
                                            rgSigBytesSecurityCriticalExplicitCtor.GetValue(), dwSigExplicitSize, 
                                            &mrSecurityCriticalExplicitAttributeCtor);
                    if (S_OK == hr) continue;
                }
                else
                // look for the SecurityTransparent attribute
                if (strcmp(szNameOfTypeRef, COR_SECURITYTRANSPARENT_ATTRIBUTE) == 0)
                {
                    hr = ImportHelper::FindMemberRef(
                                        pMiniMdEmit, tkTmp, 
                                        COR_CTOR_METHOD_NAME, 
                                        NULL, 0, 
                                        &mrSecurityTransparentAttributeCtor);
                    if (S_OK == hr) continue;
                }
                else
                // look for the SecurityTreatAsSafe attribute
                if (strcmp(szNameOfTypeRef, COR_SECURITYTREATASSAFE_ATTRIBUTE) == 0)
                {
                    hr = ImportHelper::FindMemberRef(
                                            pMiniMdEmit, tkTmp, 
                                            COR_CTOR_METHOD_NAME, 
                                            NULL, 0, 
                                            &mrSecurityTreatAsSafeAttributeCtor);
                    if (S_OK == hr) continue;
                }
            }
            hr = S_OK; // ignore failures since the attribute may not be used
        }
    }
    
        // Loop over every module scope
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // We know that the filter table is not null here.  Tell PREFIX that we know it.
        PREFIX_ASSUME( pMiniMdImport->GetFilterTable() != NULL );

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountCustomAttributes();

        // loop through all CustomAttribute
        for (i = 1; i <= iCount; i++)
        {
            // compare it with the emit scope
            IfFailGo(pMiniMdImport->GetCustomAttributeRecord(i, &pRecImport));
            tkParentImp = pMiniMdImport->getParentOfCustomAttribute(pRecImport);
            tkType = pMiniMdImport->getTypeOfCustomAttribute(pRecImport);
            IfFailGo(pMiniMdImport->getValueOfCustomAttribute(pRecImport, (const BYTE **)&pValue, &cbBlob));

            // only merge those CustomAttributes that are marked
            if ( pMiniMdImport->GetFilterTable()->IsCustomAttributeMarked(TokenFromRid(i, mdtCustomAttribute)) == false)
                continue;

            // Check the type of the CustomAttribute. If it is not marked, then we don't need to move over the CustomAttributes.
            // This will only occur for compiler defined discardable CAs during linking.
            //
            if ( pMiniMdImport->GetFilterTable()->IsTokenMarked(tkType) == false)
                continue;
        
            if ( pCurTkMap->Find(tkParentImp, &pTokenRec) )
            {
                // If the From token type is different from the To token's type, we have optimized the ref to def. 
                // In this case, we are dropping the CA associated with the Ref tokens.
                //
                if (TypeFromToken(tkParentImp) == TypeFromToken(pTokenRec->m_tkTo))
                {

                    // If tkParentImp is a MemberRef and it is also mapped to a MemberRef in the merged scope with a MethodDef
                    // parent, then it is a MemberRef optimized to a MethodDef. We are keeping the MemberRef because it is a
                    // vararg call. So we can drop CAs on this MemberRef.
                    if (TypeFromToken(tkParentImp) == mdtMemberRef)
                    {
                        MemberRefRec *pTempRec;
                        IfFailGo(pMiniMdEmit->GetMemberRefRecord(RidFromToken(pTokenRec->m_tkTo), &pTempRec));
                        if (TypeFromToken(pMiniMdEmit->getClassOfMemberRef(pTempRec)) == mdtMethodDef)
                            continue;
                    }


                    if (! pCurTkMap->Find(tkType, &pTypeRec) )
                    {
                        _ASSERTE(!"CustomAttribute Type not found in output scope");
                        IfFailGo(META_E_BADMETADATA);
                    }

                        // Determine if we need to copy or ignore security-critical-related attributes
                    hr = MergeSecurityCriticalModuleLevelAttributes(
                            pImportData, tkParentImp, pTypeRec, 
                            mrSecurityTreatAsSafeAttributeCtor, mrSecurityTransparentAttributeCtor, 
                            mrSecurityCriticalExplicitAttributeCtor, 
                            mrSecurityCriticalEverythingAttributeCtor);
                    IfFailGo(hr);
                        // S_FALSE means skip attribute
                    if (hr == S_FALSE) continue;
                         // S_OK means consider copying attribute

                    // if it's the SuppressMergeCheckAttribute, don't copy it
                    if ( pTypeRec->m_tkTo == mrSuppressMergeCheckAttributeCtor )
                    {
                        continue;
                    }

                    if ( pTokenRec->m_isDuplicate)
                    {
                        // Try to see if the custom value is there in the emit scope or not.
                        // If not, move it over still
                        hr = ImportHelper::FindCustomAttributeByToken(
                            pMiniMdEmit, 
                            pTokenRec->m_tkTo, 
                            pTypeRec->m_tkTo, 
                            pValue, 
                            cbBlob, 
                            &cvEmit);
                
                        if ( hr == S_OK )
                        {
                            // found a match
                            // <TODO>@FUTURE: more verification??</TODO>
                            fDuplicate = true;
                        }
                        else
                        {
                            TypeRefRec *pAttributeTypeRefRec;
                            // We need to allow additive merge on TypeRef for CustomAttributes because compiler
                            // could build module but not assembly. They are hanging of Assembly level CAs on a bogus
                            // TypeRef.
                            // Also allow additive merge for CAs from CompilerServices and Microsoft.VisualC
                            if (tkParentImp == MODULEDEFTOKEN
                                || TypeFromToken(tkParentImp) == mdtTypeRef
                                || (IsAttributeFromNamespace(pMiniMdImport, tkType, 
                                        COR_COMPILERSERVICE_NAMESPACE, COR_MSCORLIB_NAME, 
                                        &pAttributeTypeRefRec) == S_OK) 
                                || (IsAttributeFromNamespace(pMiniMdImport, tkType, 
                                        COR_MISCBITS_NAMESPACE, COR_MISCBITS_NAMESPACE, 
                                        &pAttributeTypeRefRec) == S_OK))
                            {
                                // clear the error
                                hr = NOERROR;

                                // custom value of module token!  Copy over the custom value
                                goto CopyCustomAttribute;
                            }

                            // another case to support additive merge if the CA on MehtodDef is
                            //  HandleProcessCorruptedStateExceptionsAttribute
                            if ( TypeFromToken(tkParentImp) == mdtMethodDef && tkType == pImportData->m_tkHandleProcessCorruptedStateCtor)
                            {
                                // clear the error
                                hr = NOERROR;

                                // custom value of module token!  Copy over the custom value
                                goto CopyCustomAttribute;
                            }
                            CheckContinuableErrorEx(META_E_MD_INCONSISTENCY, pImportData, TokenFromRid(i, mdtCustomAttribute));
                        }
                    }
                    else
                    {
CopyCustomAttribute:
                        if ((m_dwMergeFlags & DropMemberRefCAs) && TypeFromToken(pTokenRec->m_tkTo) == mdtMemberRef)
                        {
                            // CustomAttributes associated with MemberRef. If the parent of MemberRef is a MethodDef or FieldDef, drop
                            // the custom attribute.
                            MemberRefRec *pMemberRefRec;
                            IfFailGo(pMiniMdEmit->GetMemberRefRecord(RidFromToken(pTokenRec->m_tkTo), &pMemberRefRec));
                            mdToken         mrParent = pMiniMdEmit->getClassOfMemberRef(pMemberRefRec);
                            if (TypeFromToken(mrParent) == mdtMethodDef || TypeFromToken(mrParent) == mdtFieldDef)
                            {
                                // Don't bother to copy over
                                continue;
                            }
                        }

                        // Parent is duplicated but the custom value is not. Still copy over the
                        // custom value.
                        fDuplicate = false;
                        IfFailGo(pMiniMdEmit->AddCustomAttributeRecord(&pRecEmit, (ULONG *)&cvEmit));
                        cvEmit = TokenFromRid(cvEmit, mdtCustomAttribute);

                        // set the parent
                        IfFailGo( pMiniMdEmit->PutToken(TBL_CustomAttribute, CustomAttributeRec::COL_Parent, pRecEmit, pTokenRec->m_tkTo) );
                        // set the type
                        IfFailGo( pMiniMdEmit->PutToken(TBL_CustomAttribute, CustomAttributeRec::COL_Type, pRecEmit, pTypeRec->m_tkTo));

                        // move over the CustomAttribute blob value
                        IfFailGo(pMiniMdImport->getValueOfCustomAttribute(pRecImport, (const BYTE **)&pValue, &cbBlob));

                        IfFailGo( pMiniMdEmit->PutBlob(TBL_CustomAttribute, CustomAttributeRec::COL_Value, pRecEmit, pValue, cbBlob));
                        IfFailGo( pMiniMdEmit->AddCustomAttributesToHash(cvEmit) );
                    }
                    cvEmit = TokenFromRid(cvEmit, mdtCustomAttribute);
                    cvImp = TokenFromRid(i, mdtCustomAttribute);

                    // Record the token movement
                    IfFailGo( pCurTkMap->InsertNotFound(cvImp, pTokenRec->m_isDuplicate, cvEmit, &pTokenRec) );
                }
            }
            else
            {

                // either bad lookup map or bad metadata
                _ASSERTE(!"Bad state");
                IfFailGo( META_E_BADMETADATA );
            }
        }
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeCustomAttributes()

//*******************************************************************************
// Helper to check if input scope has assembly-level security transparent awareness (either SecurityTransparent or SecurityCritical)
// SIDE EFFECT: pImportData->m_isscsSecurityCriticalStatus will be explicitly set [same value as return value]
// SecurityCritical.Explicit attribute injection occurs for all scopes that have tags (e.g. NOT ISSCS_Unknown)
// If the tagged scopes are all SecurityCritical.Everything, then the final attribute should be SecurityCritical.Everything
//  anyway. Otherwise, at least one SecurityCritical.Explicit tag will be used/injected
//*******************************************************************************
InputScopeSecurityCriticalStatus NEWMERGER::CheckInputScopeIsCritical(MergeImportData* pImportData, HRESULT& hr)
{
    hr = S_OK;
    
    // the attribute should be in a known state no matter how we return from this function
    //  default to no attribute explicitly specified
    pImportData->m_isscsSecurityCriticalStatus = ISSCS_Unknown;

    mdTypeRef fakeModuleTypeRef = mdTokenNil;
    mdAssemblyRef tkMscorlib = mdTokenNil;

        // TODO: Should we remove the ability to disable merging critical attributes?
    if (g_fRefShouldMergeCriticalChecked == FALSE)
    {
        // shouldn't require thread safety lock
        g_fRefShouldMergeCritical = (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_MergeCriticalAttributes) != 0);
        g_fRefShouldMergeCriticalChecked = TRUE;
    }

    // return no merge needed, if the merge critical attribute setting is not enabled.
    if (!g_fRefShouldMergeCritical) return ISSCS_Unknown;
    
     // get typeref for mscorlib
    BYTE pbMscorlibToken[] = COR_MSCORLIB_TYPEREF;

    CMiniMdRW*   pImportedMiniMd = &pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd;

    if (S_OK != ImportHelper::FindAssemblyRef(pImportedMiniMd, 
                                                COR_MSCORLIB_NAME, 
                                                NULL, 
                                                pbMscorlibToken, 
                                                sizeof(pbMscorlibToken), 
                                                asm_rmj, 
                                                asm_rmm, 
                                                asm_rup, 
                                                asm_rpt, 
                                                0, 
                                                &tkMscorlib))
    {
        // there isn't an mscorlib ref here... we can't have the security critical attribute
        return ISSCS_Unknown;
    }

    if (S_OK != ImportHelper::FindTypeRefByName(pImportedMiniMd, 
                                            tkMscorlib, 
                                            COR_COMPILERSERVICE_NAMESPACE, 
                                            MODULE_CA_TYPENAME, 
                                            &fakeModuleTypeRef))
    {
        // for now let use the fake module ref as the assembly def
        fakeModuleTypeRef = 0x000001;
    }

        // Check the input scope for TreatAsSafe
    if (S_OK == ImportHelper::GetCustomAttributeByName(pImportedMiniMd, 
                                                       fakeModuleTypeRef, // This is the assembly def token
                                                       COR_SECURITYTREATASSAFE_ATTRIBUTE_FULL, 
                                                       NULL, 
                                                       NULL))
    {
        pImportData->m_isscsSecurityCriticalStatus |= ISSCS_SecurityTreatAsSafe;
    }

    // Check the input scope for security transparency awareness
    // For example, the assembly is marked SecurityTransparent, SecurityCritical(Explicit), or SecurityCritical(Everything)


    const void  *pbData = NULL; // [OUT] Put pointer to data here.
    ULONG       cbData = 0;     // number of bytes in pbData

        // Check if the SecurityTransparent attribute is present
    if (S_OK == ImportHelper::GetCustomAttributeByName(pImportedMiniMd, 
                                                       fakeModuleTypeRef, // This is the assembly def token
                                                       COR_SECURITYTRANSPARENT_ATTRIBUTE_FULL, 
                                                       NULL, 
                                                       NULL))
    {
        pImportData->m_isscsSecurityCriticalStatus |= ISSCS_SecurityTransparent;
    }
    else
       // Check if the SecurityCritical attribute is present
    if (S_OK == ImportHelper::GetCustomAttributeByName(pImportedMiniMd, 
                                                       fakeModuleTypeRef, // This is the assembly def token
                                                       COR_SECURITYCRITICAL_ATTRIBUTE_FULL, 
                                                       &pbData, 
                                                       &cbData))
    {
            // find out if critical everything or explicit

            // default to critical
        pImportData->m_isscsSecurityCriticalStatus = ISSCS_SecurityCritical;

        BYTE rgSecurityCriticalEverythingCtorValue[] = COR_SECURITYCRITICAL_ATTRIBUTE_VALUE_EVERYTHING;
            // if value is non-0 (i.e. 1), then mark as SecurityCritical everything, otherwise, explicit
        if (NULL != pbData && cbData == 8 && 
            memcmp(rgSecurityCriticalEverythingCtorValue, pbData, cbData) == 0)
        {
            pImportData->m_isscsSecurityCriticalStatus |= ISSCS_SecurityCriticalEverything;
        }
        else
        {
            pImportData->m_isscsSecurityCriticalStatus |= ISSCS_SecurityCriticalExplicit;
        }
    }

    return pImportData->m_isscsSecurityCriticalStatus;
} // HRESULT NEWMERGER::CheckInputScopeIsCritical()

//*******************************************************************************
// Helper to merge security critical annotations across assemblies and types
//*******************************************************************************
HRESULT NEWMERGER::MergeSecurityCriticalAttributes()
{
        // if no assembly-level critical attributes were specified, then none are needed, 
        //      and no need to do special attribute merging
    if (ISSCS_Unknown == m_isscsSecurityCritical)
    {
        return S_OK;
    }
        // or if the global-scope already has TAS/Critical.Everything, then ignore individual type fixes
    else if ((ISSCS_SECURITYCRITICAL_LEGACY & m_isscsSecurityCriticalAllScopes) == ISSCS_SECURITYCRITICAL_LEGACY)
    {
        return S_OK;
    }

    HRESULT hr = S_OK;

    CMiniMdRW* emit = GetMiniMdEmit();
    // The attribute we want to decorate all of the types with has not been defined.
    mdMemberRef tkSecurityCriticalEverythingAttribute = mdTokenNil;
    mdMemberRef tkSecurityTreatAsSafeAttribute = mdTokenNil;
    mdMemberRef tkSecuritySafeCriticalAttribute = mdTokenNil;

    mdAssemblyRef tkMscorlib = mdTokenNil;
    mdTypeRef fakeModuleTypeRef = mdTokenNil;
    mdTypeRef securityEnum = mdTokenNil;

    DWORD dwSigSize;
    BYTE* rgSigBytesSecurityCriticalExplicitCtor = 0;
    DWORD dwSigSize_TEMP;

    BYTE rgSigBytesTreatAsSafeCtor[] = {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0x00, ELEMENT_TYPE_VOID};

    BYTE rgSecurityCriticalEverythingCtorValue[] = COR_SECURITYCRITICAL_ATTRIBUTE_VALUE_EVERYTHING;
                               

    mdTypeRef tkSecurityCriticalEverythingAttributeType = mdTokenNil;
    mdTypeRef tkSecurityTreatAsSafeAttributeType = mdTokenNil;

    NewArrayHolder<BYTE> rgSigBytesSecurityCriticalEverythingCtor(new (nothrow)BYTE[COR_SECURITYCRITICAL_CTOR_SCOPE_SIG_MAX_SIZE]);
    BYTE* pSigBytesSecurityCriticalEverythingCtor = rgSigBytesSecurityCriticalEverythingCtor.GetValue();
    IfFailGo((pSigBytesSecurityCriticalEverythingCtor == NULL)?E_OUTOFMEMORY:S_OK);
    
    IfFailGo(RetrieveStandardSecurityCriticalMetaData(
            tkMscorlib, 
            securityEnum, 
            pSigBytesSecurityCriticalEverythingCtor, 
            dwSigSize, 
            rgSigBytesSecurityCriticalExplicitCtor, 
            dwSigSize_TEMP));

    if (S_OK != ImportHelper::FindTypeRefByName(emit, 
                                            tkMscorlib, 
                                            COR_COMPILERSERVICE_NAMESPACE, 
                                            MODULE_CA_TYPENAME, 
                                            &fakeModuleTypeRef))
    {
        // for now let use the fake module ref as the assembly def
        fakeModuleTypeRef = 0x000001;
    }

    IfFailGo(m_pRegMetaEmit->DefineTypeRefByName(
            tkMscorlib, COR_SECURITYCRITICAL_ATTRIBUTE_FULL_W, &tkSecurityCriticalEverythingAttributeType));

    IfFailGo(m_pRegMetaEmit->DefineMemberRef(tkSecurityCriticalEverythingAttributeType, 
                                                    COR_CONSTRUCTOR_METADATA_IDENTIFIER, 
                                                    rgSigBytesSecurityCriticalEverythingCtor.GetValue(), 
                                                    dwSigSize, 
                                                    &tkSecurityCriticalEverythingAttribute));
                                                                  
    IfFailGo(m_pRegMetaEmit->DefineTypeRefByName(
                                        tkMscorlib, COR_SECURITYTREATASSAFE_ATTRIBUTE_FULL_W, 
                                        &tkSecurityTreatAsSafeAttributeType));
    
    IfFailGo(m_pRegMetaEmit->DefineMemberRef(tkSecurityTreatAsSafeAttributeType, 
                                                    COR_CONSTRUCTOR_METADATA_IDENTIFIER, 
                                                    rgSigBytesTreatAsSafeCtor, 
                                                    sizeof(rgSigBytesTreatAsSafeCtor), 
                                                    &tkSecurityTreatAsSafeAttribute));


    // place this block in a new scope so that we can safely goto past it
    {
        mdTypeRef tkSecuritySafeCriticalAttributeType = mdTokenNil;
        if (FAILED (hr = m_pRegMetaEmit->DefineTypeRefByName(tkMscorlib, COR_SECURITYSAFECRITICAL_ATTRIBUTE_FULL_W, 
                                                &tkSecuritySafeCriticalAttributeType)))
        {
            _ASSERTE(!"Couldn't Emit a Typeref for SafeCritical attribute");
            return hr;
        }

        BYTE rgSigBytesSafeCriticalCtor[] = {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0x00, ELEMENT_TYPE_VOID};
        if (FAILED(hr = m_pRegMetaEmit->DefineMemberRef(tkSecuritySafeCriticalAttributeType, 
                                                                      W(".ctor"), 
                                                                      rgSigBytesSafeCriticalCtor, 
                                                                      sizeof(rgSigBytesSafeCriticalCtor), 
                                                                      &tkSecuritySafeCriticalAttribute)))
        {
            _ASSERTE(!"Couldn't Emit a MemberRef for SafeCritical attribute .ctor");
            return hr;
        }
    }


    for (MergeImportData* pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
            // if the import is marked TAS, then we need to explicitly mark each type as TAS
            // if the import is marked Crit/Everything, then we need to explicitly mark each type as Crit
            // if the import is not marked at all, then we need to explicitly mark each type TAS/Crit

            // if the import is marked ONLY Crit/Explicit or ONLY Transparent then we can ignore it
        if (ISSCS_SecurityTransparent == pImportData->m_isscsSecurityCriticalStatus || 
            ISSCS_SecurityCriticalExplicit == pImportData->m_isscsSecurityCriticalStatus) continue;

        // Run through the scopes that need to have their types decorated with this attribute
        MDTOKENMAP*pCurTKMap = pImportData->m_pMDTokenMap;
        BYTE rgTreatAsSafeCtorValue[] = COR_SECURITYTREATASSAFE_ATTRIBUTE_VALUE;

        BOOL fMarkEachTokenAsCritical = FALSE;
        // if the import is unmarked or marked Crit/Everything, 
        //      then we need to explicitly mark each token as Crit
        // Unless the the global scope already has SecurityCritical.Everything
        if (((ISSCS_SecurityCriticalEverything & m_isscsSecurityCriticalAllScopes) != ISSCS_SecurityCriticalEverything) && 
            (((ISSCS_SecurityCriticalEverything & pImportData->m_isscsSecurityCriticalStatus) == ISSCS_SecurityCriticalEverything ) || 

                // OR this scope is NOT transparent or critical-explicit
             (ISSCS_SecurityTransparent & pImportData->m_isscsSecurityCriticalStatus) == 0 || 
             (ISSCS_SecurityCritical & pImportData->m_isscsSecurityCriticalStatus) == 0 || 

                // OR this scope is UNKNOWN
             (ISSCS_Unknown == (ISSCS_SECURITYCRITICAL_FLAGS & pImportData->m_isscsSecurityCriticalStatus))))
        {
            fMarkEachTokenAsCritical = TRUE;
        }

        BOOL fMarkEachTokenAsSafe = FALSE;
            // if the import is unmarked or marked TAS, 
            //      then we need to explicitly mark each token as TAS
            // Unless the the global scope already has SecurityTreatAsSafe
        if (((ISSCS_SecurityTreatAsSafe & m_isscsSecurityCriticalAllScopes) != ISSCS_SecurityTreatAsSafe) && 
             ((ISSCS_SecurityTreatAsSafe & pImportData->m_isscsSecurityCriticalStatus) || 
                ISSCS_Unknown == (pImportData->m_isscsSecurityCriticalStatus & ISSCS_SECURITYCRITICAL_FLAGS)))
        {
            fMarkEachTokenAsSafe = TRUE;
        }
        
        BYTE rgSafeCriticalCtorValue[] = {0x01, 0x00, 0x00 ,0x00};

        for (int i = 0; i < pCurTKMap->Count(); i++)
        {
            TOKENREC* pRec = pCurTKMap->Get(i);
            BOOL fInjectSecurityAttributes = FALSE;

            // skip empty records
            if (pRec->IsEmpty()) continue;

            // If this scope contained a typeref that was resolved to a typedef, let's not mark it. We'll let the owner
            // of the actual typedef decide if that type should be marked.
            if ((TypeFromToken(pRec->m_tkFrom) == mdtTypeRef) && (TypeFromToken(pRec->m_tkTo) == mdtTypeDef))
                continue;

            // Same for method refs/method defs
            if ((TypeFromToken(pRec->m_tkFrom) == mdtMemberRef) && (TypeFromToken(pRec->m_tkTo) == mdtMethodDef))
                continue;
                       
            // check for typedefs, but don't put this on the global typedef
            if ((TypeFromToken(pRec->m_tkTo) == mdtTypeDef) && (pRec->m_tkTo != TokenFromRid(1, mdtTypeDef)))
            {
                // by default we will inject
                fInjectSecurityAttributes = TRUE;
                // except for Enums
                DWORD           dwClassAttrs = 0;
                mdTypeRef       crExtends = mdTokenNil;

                if (FAILED(hr = m_pRegMetaEmit->GetTypeDefProps(pRec->m_tkTo, NULL, NULL, 0 , &dwClassAttrs, &crExtends)))
                {
                    // TODO: should we fail ??
                }

                // check for Enum types
                if (!IsNilToken(crExtends) && (TypeFromToken(crExtends)==mdtTypeRef))
                {
                    // get the namespace and the name for this token
                    CMiniMdRW   *pMiniMd = GetMiniMdEmit();
                    TypeRefRec *pTypeRefRec;
                    IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(crExtends), &pTypeRefRec));
                    LPCSTR szNamespace;
                    LPCSTR szName;
                    IfFailGo(pMiniMd->getNamespaceOfTypeRef(pTypeRefRec, &szNamespace));;
                    IfFailGo(pMiniMd->getNameOfTypeRef(pTypeRefRec, &szName));
                        // check for System.Enum
                    BOOL bIsEnum = (!strcmp(szNamespace,"System"))&&(!strcmp(szName,"Enum"));
                    if (bIsEnum)
                    {
                        fInjectSecurityAttributes = FALSE;
                    }
                }
            }
            else // check for global method defs
            if (TypeFromToken(pRec->m_tkTo) == mdtMethodDef)
            {
                int isGlobal = 0;
                if (!FAILED(m_pRegMetaEmit->IsGlobal(pRec->m_tkTo, &isGlobal)))
                {
                    // check for global methods
                    if (isGlobal != 0)
                    {
                        fInjectSecurityAttributes = TRUE;
                    }
                }
            }
            
            if (fInjectSecurityAttributes)
            {
                // check to see if the token already has a custom attribute
                const void  *pbData = NULL;               // [OUT] Put pointer to data here.
                ULONG       cbData = 0;

                if (fMarkEachTokenAsCritical)
                {
                        // Check if the Type already has SecurityCritical
                    BOOL fInjectSecurityCriticalEverything = TRUE;
                    if (S_OK == m_pRegMetaEmit->GetCustomAttributeByName(pRec->m_tkTo, COR_SECURITYCRITICAL_ATTRIBUTE_FULL_W, &pbData, &cbData))
                    {
                            // if value is non-0 (i.e. 1), then it is SecurityCritical.Everything - so do not inject another
                        fInjectSecurityCriticalEverything = !(NULL != pbData && cbData == 8 && 
                            memcmp(rgSecurityCriticalEverythingCtorValue, pbData, cbData) == 0);
                    }

                        // either inject or overwrite SecurityCritical.Everything
                    if (fInjectSecurityCriticalEverything)
                    {
                        IfFailGo(m_pRegMetaEmit->DefineCustomAttribute(
                                    pRec->m_tkTo, tkSecurityCriticalEverythingAttribute, 
                                    rgSecurityCriticalEverythingCtorValue, // Use this if you need specific custom attribute data (presence of the attribute isn't enough)
                                    sizeof(rgSecurityCriticalEverythingCtorValue), // Length of your custom attribute data
                                    NULL));
                    }
                }

                    // If the Type does NOT already have TAS then add it
                if (fMarkEachTokenAsSafe && 
                    S_OK != m_pRegMetaEmit->GetCustomAttributeByName(pRec->m_tkTo, COR_SECURITYTREATASSAFE_ATTRIBUTE_FULL_W, &pbData, &cbData))
                {
                    IfFailGo(m_pRegMetaEmit->DefineCustomAttribute(
                                pRec->m_tkTo, tkSecurityTreatAsSafeAttribute, 
                                rgTreatAsSafeCtorValue, // Use this if you need specific custom attribute data (presence of the attribute isn't enough)
                                sizeof(rgTreatAsSafeCtorValue), // Length of your custom attribute data
                                NULL));
                }

                hr = m_pRegMetaEmit->DefineCustomAttribute(pRec->m_tkTo, tkSecuritySafeCriticalAttribute, 
                        rgSafeCriticalCtorValue, // Use this if you need specific custom attribute data (presence of the attribute isn't enough)
                        sizeof(rgSafeCriticalCtorValue), // Length of your custom attribute data
                        NULL);

            }
        }
    }

        // If the global scope is not Transparent, we should emit SecurityCritical.Explicit || Everything
    if ((m_isscsSecurityCriticalAllScopes & ISSCS_SecurityTransparent) != ISSCS_SecurityTransparent && 
        (m_isscsSecurityCritical & ISSCS_SecurityCriticalEverything) != ISSCS_SecurityCriticalExplicit)
    {
        BOOL fEmitSecurityEverything = FALSE;
            // in the case of Unmarked and TAS/Unmarked, we need to emit the SecurityCritical.Everything attribute
            //      if it hasn't already been emitted
        if ((m_isscsSecurityCriticalAllScopes & ISSCS_SecurityCriticalEverything) ==
            ISSCS_SecurityCriticalEverything)
        {
            fEmitSecurityEverything = TRUE;
        }
            // otherwise, emit the SecurityCritical.Explicit attribute

        BOOL fSecurityCriticalExists = FALSE;
        // check to see if the assembly already has the appropriate SecurityCritical attribute
        //      [from one of the input scopes]
        const void  *pbData = NULL;
        ULONG       cbData = 0;
        if (S_OK == ImportHelper::GetCustomAttributeByName(emit, 
                                                           fakeModuleTypeRef, // This is the assembly def token
                                                           COR_SECURITYCRITICAL_ATTRIBUTE_FULL, 
                                                           &pbData, 
                                                           &cbData))
        {
                // find out if critical everything or explicit
                // default to critical
                // if value is non-0 (i.e. 1), then mark as SecurityCritical everything, otherwise, explicit
                if (NULL != pbData && cbData == 8 && 
                memcmp(rgSecurityCriticalEverythingCtorValue, pbData, cbData) == 0)
            {
                if (!fEmitSecurityEverything)
                {
                    _ASSERTE(!"Unexpected SecurityCritical.Everything attribute detected");
                    IfFailGo(META_E_BADMETADATA);
                }
            }
            else
            {
                if (fEmitSecurityEverything)
                {
                    _ASSERTE(!"Unexpected SecurityCritical.Explicit attribute detected");
                    IfFailGo(META_E_BADMETADATA);
                }
            }
            fSecurityCriticalExists = TRUE;
        }

        if (!fSecurityCriticalExists)
        {
                // retrieve the type and CustomAttribute
            mdCustomAttribute tkSecurityCriticalAttributeExplicit;

            mdTypeRef tkSecurityCriticalExplicitAttributeType = mdTokenNil;

            IfFailGo(m_pRegMetaEmit->DefineTypeRefByName(
                                                tkMscorlib, COR_SECURITYCRITICAL_ATTRIBUTE_FULL_W, 
                                                &tkSecurityCriticalExplicitAttributeType));
        
            BYTE rgSigBytesSecurityCriticalExplicitCtorLocal[] = {IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0x00, ELEMENT_TYPE_VOID};
            BYTE rgSecurityCriticalExplicitCtorValue[] = COR_SECURITYCRITICAL_ATTRIBUTE_VALUE_EXPLICIT;

            IfFailGo(m_pRegMetaEmit->DefineMemberRef(tkSecurityCriticalExplicitAttributeType, 
                                                            COR_CONSTRUCTOR_METADATA_IDENTIFIER, 
                                                            rgSigBytesSecurityCriticalExplicitCtorLocal, 
                                                            sizeof(rgSigBytesSecurityCriticalExplicitCtorLocal), 
                                                            &tkSecurityCriticalAttributeExplicit));

            IfFailGo(m_pRegMetaEmit->DefineCustomAttribute(
                        fakeModuleTypeRef, 
                        fEmitSecurityEverything?tkSecurityCriticalEverythingAttribute:tkSecurityCriticalAttributeExplicit, 
                        fEmitSecurityEverything?rgSecurityCriticalEverythingCtorValue:rgSecurityCriticalExplicitCtorValue, 
                        fEmitSecurityEverything?sizeof(rgSecurityCriticalEverythingCtorValue):sizeof(rgSecurityCriticalExplicitCtorValue), 
                        NULL));

        }
    }

ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeSecurityCriticalAttributes()

//*******************************************************************************
// Helper to copy an InterfaceImpl record
//*******************************************************************************
HRESULT NEWMERGER::CopyInterfaceImpl(
    InterfaceImplRec    *pRecEmit,          // [IN] the emit record to fill
    MergeImportData     *pImportData,       // [IN] the importing context
    InterfaceImplRec    *pRecImp)           // [IN] the record to import
{
    HRESULT     hr;
    mdToken     tkParent;
    mdToken     tkInterface;
    CMiniMdRW   *pMiniMdEmit = GetMiniMdEmit();
    CMiniMdRW   *pMiniMdImp;
    MDTOKENMAP  *pCurTkMap;

    pMiniMdImp = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

    // set the current MDTokenMap
    pCurTkMap = pImportData->m_pMDTokenMap;

    tkParent = pMiniMdImp->getClassOfInterfaceImpl(pRecImp);
    tkInterface = pMiniMdImp->getInterfaceOfInterfaceImpl(pRecImp);

    IfFailGo( pCurTkMap->Remap(tkParent, &tkParent) );
    IfFailGo( pCurTkMap->Remap(tkInterface, &tkInterface) );

    IfFailGo( pMiniMdEmit->PutToken( TBL_InterfaceImpl, InterfaceImplRec::COL_Class, pRecEmit, tkParent) );
    IfFailGo( pMiniMdEmit->PutToken( TBL_InterfaceImpl, InterfaceImplRec::COL_Interface, pRecEmit, tkInterface) );

ErrExit:
    return hr;
} // HRESULT NEWMERGER::CopyInterfaceImpl()


//*****************************************************************************
// Merge Assembly table
//*****************************************************************************
HRESULT NEWMERGER::MergeAssembly()
{
    HRESULT     hr = NOERROR;
    AssemblyRec *pRecImport = NULL;
    AssemblyRec *pRecEmit = NULL;
    CMiniMdRW   *pMiniMdImport;
    CMiniMdRW   *pMiniMdEmit;
    LPCUTF8     szTmp;
    const BYTE  *pbTmp;
    ULONG       cbTmp;
    ULONG       iRecord;
    TOKENREC    *pTokenRec;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        if (!pMiniMdImport->getCountAssemblys())
            goto ErrExit;       // There is no Assembly in the import scope to merge.

        // Copy the Assembly map record to the Emit scope and send a token remap notifcation
        // to the client.  No duplicate checking needed since the Assembly can be present in
        // only one scope and there can be atmost one entry.
        IfFailGo(pMiniMdImport->GetAssemblyRecord(1, &pRecImport));
        IfFailGo(pMiniMdEmit->AddAssemblyRecord(&pRecEmit, &iRecord));

        pRecEmit->Copy(pRecImport);
    
        IfFailGo(pMiniMdImport->getPublicKeyOfAssembly(pRecImport, &pbTmp, &cbTmp));
        IfFailGo(pMiniMdEmit->PutBlob(TBL_Assembly, AssemblyRec::COL_PublicKey, pRecEmit, 
                                    pbTmp, cbTmp));

        IfFailGo(pMiniMdImport->getNameOfAssembly(pRecImport, &szTmp));
        IfFailGo(pMiniMdEmit->PutString(TBL_Assembly, AssemblyRec::COL_Name, pRecEmit, szTmp));

        IfFailGo(pMiniMdImport->getLocaleOfAssembly(pRecImport, &szTmp));
        IfFailGo(pMiniMdEmit->PutString(TBL_Assembly, AssemblyRec::COL_Locale, pRecEmit, szTmp));

        // record the token movement.
        IfFailGo(pCurTkMap->InsertNotFound(
            TokenFromRid(1, mdtAssembly), 
            false, 
            TokenFromRid(iRecord, mdtAssembly), 
            &pTokenRec));
    }
ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeAssembly()




//*****************************************************************************
// Merge File table
//*****************************************************************************
HRESULT NEWMERGER::MergeFiles()
{
    HRESULT     hr = NOERROR;
    FileRec     *pRecImport = NULL;
    FileRec     *pRecEmit = NULL;
    CMiniMdRW   *pMiniMdImport;
    CMiniMdRW   *pMiniMdEmit;
    LPCUTF8     szTmp;
    const void  *pbTmp;
    ULONG       cbTmp;
    ULONG       iCount;
    ULONG       i;
    ULONG       iRecord;
    TOKENREC    *pTokenRec;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountFiles();

        // Loop through all File records and copy them to the Emit scope.
        // Since there can only be one File table in all the scopes combined, 
        // there isn't any duplicate checking that needs to be done.
        for (i = 1; i <= iCount; i++)
        {
            IfFailGo(pMiniMdImport->GetFileRecord(i, &pRecImport));
            IfFailGo(pMiniMdEmit->AddFileRecord(&pRecEmit, &iRecord));

            pRecEmit->Copy(pRecImport);

            IfFailGo(pMiniMdImport->getNameOfFile(pRecImport, &szTmp));
            IfFailGo(pMiniMdEmit->PutString(TBL_File, FileRec::COL_Name, pRecEmit, szTmp));

            IfFailGo(pMiniMdImport->getHashValueOfFile(pRecImport, (const BYTE **)&pbTmp, &cbTmp));
            IfFailGo(pMiniMdEmit->PutBlob(TBL_File, FileRec::COL_HashValue, pRecEmit, pbTmp, cbTmp));

            // record the token movement.
            IfFailGo(pCurTkMap->InsertNotFound(
                TokenFromRid(i, mdtFile), 
                false, 
                TokenFromRid(iRecord, mdtFile), 
                &pTokenRec));
        }
    }
ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeFiles()


//*****************************************************************************
// Merge ExportedType table
//*****************************************************************************
HRESULT NEWMERGER::MergeExportedTypes()
{
    HRESULT     hr = NOERROR;
    ExportedTypeRec  *pRecImport = NULL;
    ExportedTypeRec  *pRecEmit = NULL;
    CMiniMdRW   *pMiniMdImport;
    CMiniMdRW   *pMiniMdEmit;
    LPCUTF8     szTmp;
    mdToken     tkTmp;
    ULONG       iCount;
    ULONG       i;
    ULONG       iRecord;
    TOKENREC    *pTokenRec;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountExportedTypes();

        // Loop through all ExportedType records and copy them to the Emit scope.
        // Since there can only be one ExportedType table in all the scopes combined, 
        // there isn't any duplicate checking that needs to be done.
        for (i = 1; i <= iCount; i++)
        {
            IfFailGo(pMiniMdImport->GetExportedTypeRecord(i, &pRecImport));
            IfFailGo(pMiniMdEmit->AddExportedTypeRecord(&pRecEmit, &iRecord));

            pRecEmit->Copy(pRecImport);

            IfFailGo(pMiniMdImport->getTypeNameOfExportedType(pRecImport, &szTmp));
            IfFailGo(pMiniMdEmit->PutString(TBL_ExportedType, ExportedTypeRec::COL_TypeName, pRecEmit, szTmp));

            IfFailGo(pMiniMdImport->getTypeNamespaceOfExportedType(pRecImport, &szTmp));
            IfFailGo(pMiniMdEmit->PutString(TBL_ExportedType, ExportedTypeRec::COL_TypeNamespace, pRecEmit, szTmp));

            tkTmp = pMiniMdImport->getImplementationOfExportedType(pRecImport);
            IfFailGo(pCurTkMap->Remap(tkTmp, &tkTmp));
            IfFailGo(pMiniMdEmit->PutToken(TBL_ExportedType, ExportedTypeRec::COL_Implementation, 
                                        pRecEmit, tkTmp));


            // record the token movement.
            IfFailGo(pCurTkMap->InsertNotFound(
                TokenFromRid(i, mdtExportedType), 
                false, 
                TokenFromRid(iRecord, mdtExportedType), 
                &pTokenRec));
        }
    }
ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeExportedTypes()


//*****************************************************************************
// Merge ManifestResource table
//*****************************************************************************
HRESULT NEWMERGER::MergeManifestResources()
{
    HRESULT     hr = NOERROR;
    ManifestResourceRec *pRecImport = NULL;
    ManifestResourceRec *pRecEmit = NULL;
    CMiniMdRW   *pMiniMdImport;
    CMiniMdRW   *pMiniMdEmit;
    LPCUTF8     szTmp;
    mdToken     tkTmp;
    ULONG       iCount;
    ULONG       i;
    ULONG       iRecord;
    TOKENREC    *pTokenRec;

    MergeImportData *pImportData;
    MDTOKENMAP      *pCurTkMap;

    pMiniMdEmit = GetMiniMdEmit();
    
    for (pImportData = m_pImportDataList; pImportData != NULL; pImportData = pImportData->m_pNextImportData)
    {
        // for each import scope
        pMiniMdImport = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);

        // set the current MDTokenMap
        pCurTkMap = pImportData->m_pMDTokenMap;
        iCount = pMiniMdImport->getCountManifestResources();

        // Loop through all ManifestResource records and copy them to the Emit scope.
        // Since there can only be one ManifestResource table in all the scopes combined, 
        // there isn't any duplicate checking that needs to be done.
        for (i = 1; i <= iCount; i++)
        {
            IfFailGo(pMiniMdImport->GetManifestResourceRecord(i, &pRecImport));
            IfFailGo(pMiniMdEmit->AddManifestResourceRecord(&pRecEmit, &iRecord));

            pRecEmit->Copy(pRecImport);

            IfFailGo(pMiniMdImport->getNameOfManifestResource(pRecImport, &szTmp));
            IfFailGo(pMiniMdEmit->PutString(TBL_ManifestResource, ManifestResourceRec::COL_Name, 
                                        pRecEmit, szTmp));

            tkTmp = pMiniMdImport->getImplementationOfManifestResource(pRecImport);
            IfFailGo(pCurTkMap->Remap(tkTmp, &tkTmp));
            IfFailGo(pMiniMdEmit->PutToken(TBL_ManifestResource, ManifestResourceRec::COL_Implementation, 
                                        pRecEmit, tkTmp));

            // record the token movement.
            IfFailGo(pCurTkMap->InsertNotFound(
                TokenFromRid(i, mdtManifestResource), 
                false, 
                TokenFromRid(iRecord, mdtManifestResource), 
                &pTokenRec));
        }
    }
ErrExit:
    return hr;
} // HRESULT NEWMERGER::MergeManifestResources()





//*****************************************************************************
// Error handling. Call back to host to see what they want to do.
//*****************************************************************************
HRESULT NEWMERGER::OnError(
    HRESULT     hrIn,                   // The error HR we're reporting.
    MergeImportData *pImportData,       // The input scope with the error.
    mdToken     token)                  // The token with the error.
{
    // This function does a QI and a Release on every call.  However, it should be 
    //  called very infrequently, and lets the scope just keep a generic handler.
    IMetaDataError  *pIErr = NULL;
    IUnknown        *pHandler = pImportData->m_pHandler;
    CMiniMdRW       *pMiniMd = &(pImportData->m_pRegMetaImport->m_pStgdb->m_MiniMd);
    CQuickArray<WCHAR> rName;           // Name of the TypeDef in unicode.
    LPCUTF8         szTypeName;
    LPCUTF8         szNSName;
    TypeDefRec      *pTypeRec;
    int             iLen;               // Length of a name.
    mdToken         tkParent;
    HRESULT         hr = NOERROR;

    if (pHandler && pHandler->QueryInterface(IID_IMetaDataError, (void**)&pIErr)==S_OK)
    {
        switch (hrIn)
        {
           
            case META_E_PARAM_COUNTS:
            case META_E_METHD_NOT_FOUND:
            case META_E_METHDIMPL_INCONSISTENT:
            {
                LPCUTF8     szMethodName;
                MethodRec   *pMethodRec;

                // Method name.
                _ASSERTE(TypeFromToken(token) == mdtMethodDef);
                IfFailGo(pMiniMd->GetMethodRecord(RidFromToken(token), &pMethodRec));
                IfFailGo(pMiniMd->getNameOfMethod(pMethodRec, &szMethodName));
                MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzMethodName, szMethodName);
                IfNullGo(wzMethodName);

                // Type and its name.
                IfFailGo( pMiniMd->FindParentOfMethodHelper(token, &tkParent) );
                IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkParent), &pTypeRec));
                IfFailGo(pMiniMd->getNameOfTypeDef(pTypeRec, &szTypeName));
                IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTypeRec, &szNSName));
                // Put namespace + name together.
                iLen = ns::GetFullLength(szNSName, szTypeName);
                IfFailGo(rName.ReSizeNoThrow(iLen+1));
                ns::MakePath(rName.Ptr(), iLen+1, szNSName, szTypeName);

                PostError(hrIn, (LPWSTR) rName.Ptr(), wzMethodName, token);
                break;
            }
            case META_E_FIELD_NOT_FOUND:
            {
                LPCUTF8     szFieldName;
                FieldRec   *pFieldRec;

                // Field name.
                _ASSERTE(TypeFromToken(token) == mdtFieldDef);
                IfFailGo(pMiniMd->GetFieldRecord(RidFromToken(token), &pFieldRec));
                IfFailGo(pMiniMd->getNameOfField(pFieldRec, &szFieldName));
                MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzFieldName, szFieldName);
                IfNullGo(wzFieldName);

                // Type and its name.
                IfFailGo( pMiniMd->FindParentOfFieldHelper(token, &tkParent) );
                IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkParent), &pTypeRec));
                IfFailGo(pMiniMd->getNameOfTypeDef(pTypeRec, &szTypeName));
                IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTypeRec, &szNSName));
                // Put namespace + name together.
                iLen = ns::GetFullLength(szNSName, szTypeName);
                IfFailGo(rName.ReSizeNoThrow(iLen+1));
                ns::MakePath(rName.Ptr(), iLen+1, szNSName, szTypeName);

                PostError(hrIn, (LPWSTR) rName.Ptr(), wzFieldName, token);
                break;
            }
            case META_E_EVENT_NOT_FOUND:
            {
                LPCUTF8     szEventName;
                EventRec   *pEventRec;

                // Event name.
                _ASSERTE(TypeFromToken(token) == mdtEvent);
                IfFailGo(pMiniMd->GetEventRecord(RidFromToken(token), &pEventRec));
                IfFailGo(pMiniMd->getNameOfEvent(pEventRec, &szEventName));
                MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzEventName, szEventName);
                IfNullGo(wzEventName);

                // Type and its name.
                IfFailGo( pMiniMd->FindParentOfEventHelper(token, &tkParent) );
                IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkParent), &pTypeRec));
                IfFailGo(pMiniMd->getNameOfTypeDef(pTypeRec, &szTypeName));
                IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTypeRec, &szNSName));
                // Put namespace + name together.
                iLen = ns::GetFullLength(szNSName, szTypeName);
                IfFailGo(rName.ReSizeNoThrow(iLen+1));
                ns::MakePath(rName.Ptr(), iLen+1, szNSName, szTypeName);

                PostError(hrIn, (LPWSTR) rName.Ptr(), wzEventName, token);
                break;
            }
            case META_E_PROP_NOT_FOUND:
            {
                LPCUTF8     szPropertyName;
                PropertyRec   *pPropertyRec;

                // Property name.
                _ASSERTE(TypeFromToken(token) == mdtProperty);
                IfFailGo(pMiniMd->GetPropertyRecord(RidFromToken(token), &pPropertyRec));
                IfFailGo(pMiniMd->getNameOfProperty(pPropertyRec, &szPropertyName));
                MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzPropertyName, szPropertyName);
                IfNullGo(wzPropertyName);

                // Type and its name.
                IfFailGo( pMiniMd->FindParentOfPropertyHelper(token, &tkParent) );
                IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkParent), &pTypeRec));
                IfFailGo(pMiniMd->getNameOfTypeDef(pTypeRec, &szTypeName));
                IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTypeRec, &szNSName));
                // Put namespace + name together.
                iLen = ns::GetFullLength(szNSName, szTypeName);
                IfFailGo(rName.ReSizeNoThrow(iLen+1));
                ns::MakePath(rName.Ptr(), iLen+1, szNSName, szTypeName);

                PostError(hrIn, (LPWSTR) rName.Ptr(), wzPropertyName, token);
                break;
            }
            case META_S_PARAM_MISMATCH:
            {
                LPCUTF8     szMethodName;
                MethodRec   *pMethodRec;
                mdToken     tkMethod;

                // Method name.
                _ASSERTE(TypeFromToken(token) == mdtParamDef);
                IfFailGo( pMiniMd->FindParentOfParamHelper(token, &tkMethod) );
                IfFailGo(pMiniMd->GetMethodRecord(RidFromToken(tkMethod), &pMethodRec));
                IfFailGo(pMiniMd->getNameOfMethod(pMethodRec, &szMethodName));
                MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzMethodName, szMethodName);
                IfNullGo(wzMethodName);

                // Type and its name.
                IfFailGo( pMiniMd->FindParentOfMethodHelper(token, &tkParent) );
                IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkParent), &pTypeRec));
                IfFailGo(pMiniMd->getNameOfTypeDef(pTypeRec, &szTypeName));
                IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTypeRec, &szNSName));
                // Put namespace + name together.
                iLen = ns::GetFullLength(szNSName, szTypeName);
                IfFailGo(rName.ReSizeNoThrow(iLen+1));
                ns::MakePath(rName.Ptr(), iLen+1, szNSName, szTypeName);

                // use the error hresult so that we can post the correct error.
                PostError(META_E_PARAM_MISMATCH, wzMethodName, (LPWSTR) rName.Ptr(), token);
                break;
            }
            case META_E_INTFCEIMPL_NOT_FOUND:
            {
                InterfaceImplRec    *pRec;          // The InterfaceImpl 
                mdToken             tkIface;        // Token of the implemented interface.
                CQuickArray<WCHAR>  rIface;         // Name of the Implemented Interface in unicode.
                TypeRefRec          *pRef;          // TypeRef record when II is a typeref.
                InterfaceImplRec   *pInterfaceImplRec;

                // Get the record.
                _ASSERTE(TypeFromToken(token) == mdtInterfaceImpl);
                IfFailGo(pMiniMd->GetInterfaceImplRecord(RidFromToken(token), &pRec));
                // Get the name of the class.
                tkParent = pMiniMd->getClassOfInterfaceImpl(pRec);
                IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkParent), &pTypeRec));
                IfFailGo(pMiniMd->getNameOfTypeDef(pTypeRec, &szTypeName));
                IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTypeRec, &szNSName));
                // Put namespace + name together.
                iLen = ns::GetFullLength(szNSName, szTypeName);
                IfFailGo(rName.ReSizeNoThrow(iLen+1));
                ns::MakePath(rName.Ptr(), iLen+1, szNSName, szTypeName);

                // Get the name of the implemented interface.
                IfFailGo(pMiniMd->GetInterfaceImplRecord(RidFromToken(token), &pInterfaceImplRec));
                tkIface = pMiniMd->getInterfaceOfInterfaceImpl(pInterfaceImplRec);
                if (TypeFromToken(tkIface) == mdtTypeDef)
                {   // If it is a typedef...
                    IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tkIface), &pTypeRec));
                    IfFailGo(pMiniMd->getNameOfTypeDef(pTypeRec, &szTypeName));
                    IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTypeRec, &szNSName));
                }
                else
                {   // If it is a typeref...
                    _ASSERTE(TypeFromToken(tkIface) == mdtTypeRef);
                    IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(tkIface), &pRef));
                    IfFailGo(pMiniMd->getNameOfTypeRef(pRef, &szTypeName));
                    IfFailGo(pMiniMd->getNamespaceOfTypeRef(pRef, &szNSName));
                }
                // Put namespace + name together.
                iLen = ns::GetFullLength(szNSName, szTypeName);
                IfFailGo(rIface.ReSizeNoThrow(iLen+1));
                ns::MakePath(rIface.Ptr(), iLen+1, szNSName, szTypeName);


                PostError(hrIn, (LPWSTR) rName.Ptr(), (LPWSTR)rIface.Ptr(), token);
                break;
            }
            case META_E_CLASS_LAYOUT_INCONSISTENT:
            case META_E_METHOD_COUNTS:
            case META_E_FIELD_COUNTS:
            case META_E_EVENT_COUNTS:
            case META_E_PROPERTY_COUNTS:
            {
                // get the type name.
                _ASSERTE(TypeFromToken(token) == mdtTypeDef);
                IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(token), &pTypeRec));
                IfFailGo(pMiniMd->getNameOfTypeDef(pTypeRec, &szTypeName));
                IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTypeRec, &szNSName));
                // Put namespace + name together.
                iLen = ns::GetFullLength(szNSName, szTypeName);
                IfFailGo(rName.ReSizeNoThrow(iLen+1));
                ns::MakePath(rName.Ptr(), iLen+1, szNSName, szTypeName);

                PostError(hrIn, (LPWSTR) rName.Ptr(), token);
                break;
            }
            case META_E_GENERICPARAM_INCONSISTENT:
            {
                // If token is type, get type name; if method, get method name.
                LPWSTR      wzName;
                LPCUTF8     szMethodName;
                MethodRec   *pMethodRec;

                if ((TypeFromToken(token) == mdtMethodDef))
                {
                    // Get the method name.
                    IfFailGo(pMiniMd->GetMethodRecord(RidFromToken(token), &pMethodRec));
                    IfFailGo(pMiniMd->getNameOfMethod(pMethodRec, &szMethodName));
                    MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzMethodName, szMethodName);
                    IfNullGo(wzMethodName);
                    wzName = wzMethodName;
                }
                else
                {
                    // Get the type name.
                    _ASSERTE(TypeFromToken(token) == mdtTypeDef);
                    IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(token), &pTypeRec));
                    IfFailGo(pMiniMd->getNameOfTypeDef(pTypeRec, &szTypeName));
                    IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTypeRec, &szNSName));
                    // Put namespace + name together.
                    iLen = ns::GetFullLength(szNSName, szTypeName);
                    IfFailGo(rName.ReSizeNoThrow(iLen+1));
                    ns::MakePath(rName.Ptr(), iLen+1, szNSName, szTypeName);
                    wzName = (LPWSTR)rName.Ptr();
                }

                PostError(hrIn, wzName, token);
                break;
            }
            case META_E_TYPEDEF_MISSING:
            {
                TypeRefRec          *pRef;          // TypeRef record when II is a typeref.

                // Get the record.
                _ASSERTE(TypeFromToken(token) == mdtTypeRef);
                IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(token), &pRef));
                IfFailGo(pMiniMd->getNameOfTypeRef(pRef, &szTypeName));
                IfFailGo(pMiniMd->getNamespaceOfTypeRef(pRef, &szNSName));
                
                // Put namespace + name together.
                iLen = ns::GetFullLength(szNSName, szTypeName);
                IfFailGo(rName.ReSizeNoThrow(iLen+1));
                ns::MakePath(rName.Ptr(), iLen+1, szNSName, szTypeName);


                PostError(hrIn, (LPWSTR) rName.Ptr(), token);
                break;
            }
            default:
            {
                PostError(hrIn, token);
                break;
            }
        }
        hr = pIErr->OnError(hrIn, token);
    }
    else
        hr = S_FALSE;
ErrExit:
    if (pIErr)
        pIErr->Release();
    return (hr);
} // NEWMERGER::OnError

#endif //FEATURE_METADATA_EMIT_ALL
