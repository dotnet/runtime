// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// Import.cpp
//

//
// Methods of code:RegMeta class which implement public API interfaces:
//  * code:IMetaDataImport, and
//  * code:IMetaDataImport2.
//
//*****************************************************************************
#include "stdafx.h"
#include "regmeta.h"
#include "metadata.h"
#include "corerror.h"
#include "mdutil.h"
#include "rwutil.h"
#include "corpriv.h"
#include "importhelper.h"
#include "mdlog.h"
#include "mdperf.h"
#include "stgio.h"

//*****************************************************************************
// Enumerate over all the Methods in a TypeDef.
//*****************************************************************************
STDMETHODIMP RegMeta::EnumMembers(            // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
    mdToken     rMembers[],             // [OUT] Put MemberDefs here.
    ULONG       cMax,                   // [IN] Max MemberDefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal   **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    RID             ridStartMethod;
    RID             ridEndMethod;
    RID             ridStartField;
    RID             ridEndField;
    RID             index;
    RID             indexField;
    TypeDefRec      *pRec;
    HENUMInternal   *pEnum = NULL;

    LOG((LOGMD, "MD RegMeta::EnumMembers(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, cl, rMembers, cMax, pcTokens));

    START_MD_PERF();
    LOCKREAD();

    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);

        if ( IsGlobalMethodParentTk(cl) )
        {
            cl = m_tdModule;
        }

        IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(cl), &pRec));

        ridStartMethod = m_pStgdb->m_MiniMd.getMethodListOfTypeDef(pRec);
        IfFailGo(m_pStgdb->m_MiniMd.getEndMethodListOfTypeDef(RidFromToken(cl), &ridEndMethod));

        ridStartField = m_pStgdb->m_MiniMd.getFieldListOfTypeDef(pRec);
        IfFailGo(m_pStgdb->m_MiniMd.getEndFieldListOfTypeDef(RidFromToken(cl), &ridEndField));


        IfFailGo( HENUMInternal::CreateDynamicArrayEnum( mdtMethodDef, &pEnum) );

        // add all methods to the dynamic array
        for (index = ridStartMethod; index < ridEndMethod; index++ )
        {
            RID rid;
            IfFailGo(pMiniMd->GetMethodRid(index, &rid));
            IfFailGo(HENUMInternal::AddElementToEnum(
                pEnum,
                TokenFromRid(rid, mdtMethodDef)));
        }

        // add all fields to the dynamic array
        for (indexField = ridStartField; indexField < ridEndField; indexField++ )
        {
            RID rid;
            IfFailGo(pMiniMd->GetFieldRid(indexField, &rid));
            IfFailGo(HENUMInternal::AddElementToEnum(
                pEnum,
                TokenFromRid(rid, mdtFieldDef)));
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rMembers, pcTokens);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumMembers);

    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::EnumMembers()

//*****************************************************************************
// Enumerate over all the Methods in a TypeDef that has szName
//*****************************************************************************
STDMETHODIMP RegMeta::EnumMembersWithName(    // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
    LPCWSTR     szName,                 // [IN] Limit results to those with this name.
    mdToken     rMembers[],             // [OUT] Put MemberDefs here.
    ULONG       cMax,                   // [IN] Max MemberDefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    RID                 ridStart;
    RID                 ridEnd;
    RID                 index;
    TypeDefRec          *pRec;
    MethodRec           *pMethod;
    FieldRec            *pField;
    HENUMInternal       *pEnum = NULL;
    LPUTF8              szNameUtf8;
    UTF8STR(szName, szNameUtf8);
    LPCUTF8             szNameUtf8Tmp;

    LOG((LOGMD, "MD RegMeta::EnumMembersWithName(0x%08x, 0x%08x, %S, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, cl, MDSTR(szName), rMembers, cMax, pcTokens));

    START_MD_PERF();
    LOCKREAD();

    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);

        // create the enumerator
        IfFailGo( HENUMInternal::CreateDynamicArrayEnum( mdtMethodDef, &pEnum) );

        if ( IsGlobalMethodParentTk(cl) )
        {
            cl = m_tdModule;
        }

        // get the range of method rids given a typedef
        IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(cl), &pRec));
        ridStart = pMiniMd->getMethodListOfTypeDef(pRec);
        IfFailGo(pMiniMd->getEndMethodListOfTypeDef(RidFromToken(cl), &ridEnd));

        for (index = ridStart; index < ridEnd; index++ )
        {
            if (szNameUtf8 == NULL)
            {
                RID rid;
                IfFailGo(pMiniMd->GetMethodRid(index, &rid));
                IfFailGo(HENUMInternal::AddElementToEnum(
                    pEnum,
                    TokenFromRid(rid, mdtMethodDef)));
            }
            else
            {
                RID rid;
                IfFailGo(pMiniMd->GetMethodRid(index, &rid));
                IfFailGo(pMiniMd->GetMethodRecord(rid, &pMethod));
                IfFailGo(pMiniMd->getNameOfMethod(pMethod, &szNameUtf8Tmp));
                if ( strcmp(szNameUtf8Tmp, szNameUtf8) == 0 )
                {
                    IfFailGo(pMiniMd->GetMethodRid(index, &rid));
                    IfFailGo(HENUMInternal::AddElementToEnum(pEnum, TokenFromRid(rid, mdtMethodDef)));
                }
            }
        }

        ridStart = m_pStgdb->m_MiniMd.getFieldListOfTypeDef(pRec);
        IfFailGo(m_pStgdb->m_MiniMd.getEndFieldListOfTypeDef(RidFromToken(cl), &ridEnd));

        for (index = ridStart; index < ridEnd; index++ )
        {
            if (szNameUtf8 == NULL)
            {
                RID rid;
                IfFailGo(pMiniMd->GetFieldRid(index, &rid));
                IfFailGo(HENUMInternal::AddElementToEnum(pEnum, TokenFromRid(rid, mdtFieldDef)));
            }
            else
            {
                RID rid;
                IfFailGo(pMiniMd->GetFieldRid(index, &rid));
                IfFailGo(pMiniMd->GetFieldRecord(rid, &pField));
                IfFailGo(pMiniMd->getNameOfField(pField, &szNameUtf8Tmp));
                if ( strcmp(szNameUtf8Tmp, szNameUtf8) == 0 )
                {
                    IfFailGo(pMiniMd->GetFieldRid(index, &rid));
                    IfFailGo(HENUMInternal::AddElementToEnum(
                        pEnum,
                        TokenFromRid(rid, mdtFieldDef)));
                }
            }
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rMembers, pcTokens);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumMembersWithName);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::EnumMembersWithName()

//*****************************************************************************
// enumerating through methods given a Typedef and the flag
//*****************************************************************************
STDMETHODIMP RegMeta::EnumMethods(
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
    mdMethodDef rMethods[],             // [OUT] Put MethodDefs here.
    ULONG       cMax,                   // [IN] Max MethodDefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    RID                 ridStart;
    RID                 ridEnd;
    TypeDefRec          *pRec;
    HENUMInternal       *pEnum = NULL;

    LOG((LOGMD, "MD RegMeta::EnumMethods(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, td, rMethods, cMax, pcTokens));



    START_MD_PERF();
    LOCKREAD();

    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);

        // Check for mdTypeDefNil (representing <Module>).
        // If so, this will map it to its token.
        //
        if ( IsGlobalMethodParentTk(td) )
        {
            td = m_tdModule;
        }

        IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(td), &pRec));
        ridStart = m_pStgdb->m_MiniMd.getMethodListOfTypeDef(pRec);
        IfFailGo(m_pStgdb->m_MiniMd.getEndMethodListOfTypeDef(RidFromToken(td), &ridEnd));

        if (pMiniMd->HasIndirectTable(TBL_Method) || pMiniMd->HasDelete())
        {
            IfFailGo( HENUMInternal::CreateDynamicArrayEnum( mdtMethodDef, &pEnum) );

            // add all methods to the dynamic array
            for (ULONG index = ridStart; index < ridEnd; index++ )
            {
                if (pMiniMd->HasDelete() &&
                    ((m_OptionValue.m_ImportOption & MDImportOptionAllMethodDefs) == 0))
                {
                    MethodRec *pMethRec;
                    RID rid;
                    IfFailGo(pMiniMd->GetMethodRid(index, &rid));
                    IfFailGo(pMiniMd->GetMethodRecord(rid, &pMethRec));
                    LPCSTR szMethodName;
                    IfFailGo(pMiniMd->getNameOfMethod(pMethRec, &szMethodName));
                    if (IsMdRTSpecialName(pMethRec->GetFlags()) && IsDeletedName(szMethodName) )
                    {
                        continue;
                    }
                }
                RID rid;
                IfFailGo(pMiniMd->GetMethodRid(index, &rid));
                IfFailGo(HENUMInternal::AddElementToEnum(
                    pEnum,
                    TokenFromRid(rid, mdtMethodDef)));
            }
        }
        else
        {
            IfFailGo( HENUMInternal::CreateSimpleEnum( mdtMethodDef, ridStart, ridEnd, &pEnum) );
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rMethods, pcTokens);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumMethods);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::EnumMethods()




//*****************************************************************************
// Enumerate over all the methods with szName in a TypeDef.
//*****************************************************************************
STDMETHODIMP RegMeta::EnumMethodsWithName(    // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
    LPCWSTR     szName,                 // [IN] Limit results to those with this name.
    mdMethodDef rMethods[],             // [OU] Put MethodDefs here.
    ULONG       cMax,                   // [IN] Max MethodDefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    RID                 ridStart;
    RID                 ridEnd;
    RID                 index;
    TypeDefRec          *pRec;
    MethodRec           *pMethod;
    HENUMInternal       *pEnum = NULL;
    LPUTF8              szNameUtf8;
    UTF8STR(szName, szNameUtf8);
    LPCUTF8             szNameUtf8Tmp;

    LOG((LOGMD, "MD RegMeta::EnumMethodsWithName(0x%08x, 0x%08x, %S, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, cl, MDSTR(szName), rMethods, cMax, pcTokens));



    START_MD_PERF();
    LOCKREAD();


    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);

        // Check for mdTypeDefNil (representing <Module>).
        // If so, this will map it to its token.
        //
        if ( IsGlobalMethodParentTk(cl) )
        {
            cl = m_tdModule;
        }


        // create the enumerator
        IfFailGo( HENUMInternal::CreateDynamicArrayEnum( mdtMethodDef, &pEnum) );

        // get the range of method rids given a typedef
        IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(cl), &pRec));
        ridStart = pMiniMd->getMethodListOfTypeDef(pRec);
        IfFailGo(pMiniMd->getEndMethodListOfTypeDef(RidFromToken(cl), &ridEnd));

        for (index = ridStart; index < ridEnd; index++ )
        {
            if ( szNameUtf8 == NULL )
            {
                RID rid;
                IfFailGo(pMiniMd->GetMethodRid(index, &rid));
                IfFailGo(HENUMInternal::AddElementToEnum(
                    pEnum,
                    TokenFromRid(rid, mdtMethodDef)));
            }
            else
            {
                RID rid;
                IfFailGo(pMiniMd->GetMethodRid(index, &rid));
                IfFailGo(pMiniMd->GetMethodRecord(rid, &pMethod));
                IfFailGo(pMiniMd->getNameOfMethod(pMethod, &szNameUtf8Tmp));
                if ( strcmp(szNameUtf8Tmp, szNameUtf8) == 0 )
                {
                    IfFailGo(pMiniMd->GetMethodRid(index, &rid));
                    IfFailGo(HENUMInternal::AddElementToEnum(
                        pEnum,
                        TokenFromRid(rid, mdtMethodDef)));
                }
            }
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rMethods, pcTokens);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumMethodsWithName);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::EnumMethodsWithName()



//*****************************************************************************
// Enumerate over all the fields in a TypeDef and a flag.
//*****************************************************************************
STDMETHODIMP
RegMeta::EnumFields(
    HCORENUM  *phEnum,      // [IN|OUT] Pointer to the enum.
    mdTypeDef  td,          // [IN] TypeDef to scope the enumeration.
    mdFieldDef rFields[],   // [OUT] Put FieldDefs here.
    ULONG      cMax,        // [IN] Max FieldDefs to put.
    ULONG     *pcTokens)    // [OUT] Put # put here.
{
    HRESULT hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **>(phEnum);
    RID             ridStart;
    RID             ridEnd;
    TypeDefRec     *pRec;
    HENUMInternal  *pEnum = NULL;

    LOG((LOGMD, "MD RegMeta::EnumFields(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, td, rFields, cMax, pcTokens));

    START_MD_PERF();
    LOCKREAD();

    if (*ppmdEnum == NULL)
    {
        // instantiating a new ENUM
        CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

        // Check for mdTypeDefNil (representing <Module>).
        // If so, this will map it to its token.
        //
        if (IsGlobalMethodParentTk(td))
        {
            td = m_tdModule;
        }

        IfFailGo(m_pStgdb->m_MiniMd.GetTypeDefRecord(RidFromToken(td), &pRec));
        ridStart = m_pStgdb->m_MiniMd.getFieldListOfTypeDef(pRec);
        IfFailGo(m_pStgdb->m_MiniMd.getEndFieldListOfTypeDef(RidFromToken(td), &ridEnd));

        if (pMiniMd->HasIndirectTable(TBL_Field) || pMiniMd->HasDelete())
        {
            IfFailGo(HENUMInternal::CreateDynamicArrayEnum(mdtFieldDef, &pEnum));

            // add all methods to the dynamic array
            for (ULONG index = ridStart; index < ridEnd; index++)
            {
                if (pMiniMd->HasDelete() &&
                    ((m_OptionValue.m_ImportOption & MDImportOptionAllFieldDefs) == 0))
                {
                    FieldRec *pFieldRec;
                    RID       rid;
                    IfFailGo(pMiniMd->GetFieldRid(index, &rid));
                    IfFailGo(pMiniMd->GetFieldRecord(rid, &pFieldRec));
                    LPCUTF8 szFieldName;
                    IfFailGo(pMiniMd->getNameOfField(pFieldRec, &szFieldName));
                    if (IsFdRTSpecialName(pFieldRec->GetFlags()) && IsDeletedName(szFieldName))
                    {
                        continue;
                    }
                }
                RID rid;
                IfFailGo(pMiniMd->GetFieldRid(index, &rid));
                IfFailGo(HENUMInternal::AddElementToEnum(
                    pEnum,
                    TokenFromRid(rid, mdtFieldDef)));
            }
        }
        else
        {
            IfFailGo(HENUMInternal::CreateSimpleEnum(mdtFieldDef, ridStart, ridEnd, &pEnum));
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rFields, pcTokens);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumFields);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // RegMeta::EnumFields



//*****************************************************************************
// Enumerate over all the fields with szName in a TypeDef.
//*****************************************************************************
STDMETHODIMP RegMeta::EnumFieldsWithName(     // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
    LPCWSTR     szName,                 // [IN] Limit results to those with this name.
    mdFieldDef  rFields[],              // [OUT] Put MemberDefs here.
    ULONG       cMax,                   // [IN] Max MemberDefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    RID                 ridStart;
    RID                 ridEnd;
    ULONG               index;
    TypeDefRec          *pRec;
    FieldRec            *pField;
    HENUMInternal       *pEnum = NULL;
    LPUTF8              szNameUtf8;
    UTF8STR(szName, szNameUtf8);
    LPCUTF8             szNameUtf8Tmp;

    LOG((LOGMD, "MD RegMeta::EnumFields(0x%08x, 0x%08x, %S, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, cl, MDSTR(szName), rFields, cMax, pcTokens));



    START_MD_PERF();
    LOCKREAD();

    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);

        // Check for mdTypeDefNil (representing <Module>).
        // If so, this will map it to its token.
        //
        if ( IsGlobalMethodParentTk(cl) )
        {
            cl = m_tdModule;
        }

        // create the enumerator
        IfFailGo( HENUMInternal::CreateDynamicArrayEnum( mdtMethodDef, &pEnum) );

        // get the range of field rids given a typedef
        IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(cl), &pRec));
        ridStart = m_pStgdb->m_MiniMd.getFieldListOfTypeDef(pRec);
        IfFailGo(m_pStgdb->m_MiniMd.getEndFieldListOfTypeDef(RidFromToken(cl), &ridEnd));

        for (index = ridStart; index < ridEnd; index++ )
        {
            if ( szNameUtf8 == NULL )
            {
                RID rid;
                IfFailGo(pMiniMd->GetFieldRid(index, &rid));
                IfFailGo(HENUMInternal::AddElementToEnum(
                    pEnum,
                    TokenFromRid(rid, mdtFieldDef)));
            }
            else
            {
                RID rid;
                IfFailGo(pMiniMd->GetFieldRid(index, &rid));
                IfFailGo(pMiniMd->GetFieldRecord(rid, &pField));
                IfFailGo(pMiniMd->getNameOfField(pField, &szNameUtf8Tmp));
                if ( strcmp(szNameUtf8Tmp, szNameUtf8) == 0 )
                {
                    IfFailGo(pMiniMd->GetFieldRid(index, &rid));
                    IfFailGo( HENUMInternal::AddElementToEnum(
                        pEnum,
                        TokenFromRid(rid, mdtFieldDef) ) );
                }
            }
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rFields, pcTokens);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumFieldsWithName);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::EnumFieldsWithName()


//*****************************************************************************
// Enumerate over the ParamDefs in a Method.
//*****************************************************************************
STDMETHODIMP RegMeta::EnumParams(             // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdMethodDef mb,                     // [IN] MethodDef to scope the enumeration.
    mdParamDef  rParams[],              // [OUT] Put ParamDefs here.
    ULONG       cMax,                   // [IN] Max ParamDefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    RID                 ridStart;
    RID                 ridEnd;
    MethodRec           *pRec;
    HENUMInternal       *pEnum = NULL;

    LOG((LOGMD, "MD RegMeta::EnumParams(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, mb, rParams, cMax, pcTokens));
  	START_MD_PERF();
    LOCKREAD();


    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);
        IfFailGo(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(mb), &pRec));
        ridStart = m_pStgdb->m_MiniMd.getParamListOfMethod(pRec);
        IfFailGo(m_pStgdb->m_MiniMd.getEndParamListOfMethod(RidFromToken(mb), &ridEnd));

        if (pMiniMd->HasIndirectTable(TBL_Param))
        {
            IfFailGo( HENUMInternal::CreateDynamicArrayEnum( mdtParamDef, &pEnum) );

            // add all methods to the dynamic array
            for (ULONG index = ridStart; index < ridEnd; index++ )
            {
                RID rid;
                IfFailGo(pMiniMd->GetParamRid(index, &rid));
                IfFailGo(HENUMInternal::AddElementToEnum(
                    pEnum,
                    TokenFromRid(rid, mdtParamDef)));
            }
        }
        else
        {
            IfFailGo( HENUMInternal::CreateSimpleEnum( mdtParamDef, ridStart, ridEnd, &pEnum) );
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rParams, pcTokens);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumParams);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::EnumParams()



//*****************************************************************************
// Enumerate the MemberRefs given the parent token.
//*****************************************************************************
STDMETHODIMP RegMeta::EnumMemberRefs(         // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdToken     tkParent,               // [IN] Parent token to scope the enumeration.
    mdMemberRef rMemberRefs[],          // [OUT] Put MemberRefs here.
    ULONG       cMax,                   // [IN] Max MemberRefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    ULONG               ridEnd;
    ULONG               index;
    MemberRefRec        *pRec;
    HENUMInternal       *pEnum = NULL;

    LOG((LOGMD, "MD RegMeta::EnumMemberRefs(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, tkParent, rMemberRefs, cMax, pcTokens));



    START_MD_PERF();
    LOCKREAD();

    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);
        mdToken     tk;

        // Check for mdTypeDefNil (representing <Module>).
        // If so, this will map it to its token.
        //
        IsGlobalMethodParent(&tkParent);

        // create the enumerator
        IfFailGo( HENUMInternal::CreateDynamicArrayEnum( mdtMemberRef, &pEnum) );

        // get the range of field rids given a typedef
        ridEnd = pMiniMd->getCountMemberRefs();

        for (index = 1; index <= ridEnd; index++ )
        {
            IfFailGo(pMiniMd->GetMemberRefRecord(index, &pRec));
            tk = pMiniMd->getClassOfMemberRef(pRec);
            if ( tk == tkParent )
            {
                // add the matched ones to the enumerator
                IfFailGo( HENUMInternal::AddElementToEnum(pEnum, TokenFromRid(index, mdtMemberRef) ) );
            }
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        *ppmdEnum = 0;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rMemberRefs, pcTokens);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumMemberRefs);

    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::EnumMemberRefs()


//*****************************************************************************
// Enumerate methodimpls given a typedef
//*****************************************************************************
STDMETHODIMP RegMeta::EnumMethodImpls(        // S_OK, S_FALSE, or error
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
    mdToken     rMethodBody[],          // [OUT] Put Method Body tokens here.
    mdToken     rMethodDecl[],          // [OUT] Put Method Declaration tokens here.
    ULONG       cMax,                   // [IN] Max tokens to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    MethodImplRec       *pRec;
    HENUMInternal       *pEnum = NULL;
    HENUMInternal hEnum;


    LOG((LOGMD, "MD RegMeta::EnumMethodImpls(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, td, rMethodBody, rMethodDecl, cMax, pcTokens));



    START_MD_PERF();
    LOCKREAD();

    HENUMInternal::ZeroEnum(&hEnum);

    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);
        mdToken         tkMethodBody;
        mdToken         tkMethodDecl;
        RID             ridCur;

        // Get the range of rids.
        IfFailGo( pMiniMd->FindMethodImplHelper(td, &hEnum) );

        // Create the enumerator, DynamicArrayEnum does not use the token type.
        IfFailGo( HENUMInternal::CreateDynamicArrayEnum( (TBL_MethodImpl << 24), &pEnum) );

        while (HENUMInternal::EnumNext(&hEnum, (mdToken *)&ridCur))
        {
            // Get the MethodBody and MethodDeclaration tokens for the current
            // MethodImpl record.
            IfFailGo(pMiniMd->GetMethodImplRecord(ridCur, &pRec));
            tkMethodBody = pMiniMd->getMethodBodyOfMethodImpl(pRec);
            tkMethodDecl = pMiniMd->getMethodDeclarationOfMethodImpl(pRec);

            // Add the Method body/declaration pairs to the Enum
            IfFailGo( HENUMInternal::AddElementToEnum(pEnum, tkMethodBody ) );
            IfFailGo( HENUMInternal::AddElementToEnum(pEnum, tkMethodDecl ) );
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rMethodBody, rMethodDecl, pcTokens);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);
    HENUMInternal::ClearEnum(&hEnum);

    STOP_MD_PERF(EnumMethodImpls);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::EnumMethodImpls()


//*****************************************************************************
// Enumerate over PermissionSets.  Optionally limit to an object and/or an
//  action.
//*****************************************************************************
STDMETHODIMP RegMeta::EnumPermissionSets(     // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdToken     tk,                     // [IN] if !NIL, token to scope the enumeration.
    DWORD       dwActions,              // [IN] if !0, return only these actions.
    mdPermission rPermission[],         // [OUT] Put Permissions here.
    ULONG       cMax,                   // [IN] Max Permissions to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    RID                 ridStart;
    RID                 ridEnd;
    RID                 index;
    DeclSecurityRec     *pRec;
    HENUMInternal       *pEnum = NULL;
    bool                fCompareParent = false;
    mdToken             typ = TypeFromToken(tk);
    mdToken             tkParent;

    LOG((LOGMD, "MD RegMeta::EnumPermissionSets(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, tk, dwActions, rPermission, cMax, pcTokens));

    START_MD_PERF();
    LOCKREAD();

    if ( *ppmdEnum == 0 )
    {
        // Does this token type even have security?
        if (tk != 0 &&
            !(typ == mdtTypeDef || typ == mdtMethodDef || typ == mdtAssembly))
        {
            if (pcTokens)
                *pcTokens = 0;
            hr = S_FALSE;
            goto ErrExit;
        }

        // instantiating a new ENUM
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);

        if (!IsNilToken(tk))
        {
            // parent is provided for lookup
            if ( pMiniMd->IsSorted( TBL_DeclSecurity ) )
            {
                IfFailGo(pMiniMd->getDeclSecurityForToken(tk, &ridEnd, &ridStart));
            }
            else
            {
                // table is not sorted. So we have to do a table scan
                ridStart = 1;
                ridEnd = pMiniMd->getCountDeclSecuritys() + 1;
                fCompareParent = true;
            }
        }
        else
        {
            ridStart = 1;
            ridEnd = pMiniMd->getCountDeclSecuritys() + 1;
        }

        if (IsDclActionNil(dwActions) && !fCompareParent && !m_pStgdb->m_MiniMd.HasDelete())
        {
            // create simple enumerator
            IfFailGo( HENUMInternal::CreateSimpleEnum( mdtPermission, ridStart, ridEnd, &pEnum) );
        }
        else
        {
            // create the dynamic enumerator
            IfFailGo( HENUMInternal::CreateDynamicArrayEnum( mdtPermission, &pEnum) );

            for (index = ridStart; index < ridEnd; index++ )
            {
                IfFailGo(pMiniMd->GetDeclSecurityRecord(index, &pRec));
                tkParent = pMiniMd->getParentOfDeclSecurity(pRec);
                if ( (fCompareParent && tk != tkParent) ||
                      IsNilToken(tkParent) )
                {
                    // We need to compare parent token and they are not equal so skip
                    // over this row.
                    //
                    continue;
                }
                if ( IsDclActionNil(dwActions) ||
                    ( (DWORD)(pMiniMd->getActionOfDeclSecurity(pRec))) ==  dwActions )
                {
                    // If we don't need to compare the action, just add to the enum.
                    // Or we need to compare the action and the action values are equal, add to enum as well.
                    //
                    IfFailGo( HENUMInternal::AddElementToEnum(pEnum, TokenFromRid(index, mdtPermission) ) );
                }
            }
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rPermission, pcTokens);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumPermissionSets);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::EnumPermissionSets()


//*****************************************************************************
// Find a given member in a TypeDef (typically a class).
//*****************************************************************************
STDMETHODIMP RegMeta::FindMember(
    mdTypeDef   td,                     // [IN] given typedef
    LPCWSTR     szName,                 // [IN] member name
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature
    ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
    mdToken     *pmb)                   // [OUT] matching memberdef
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;


    LOG((LOGMD, "MD RegMeta::FindMember(0x%08x, %S, 0x%08x, 0x%08x, 0x%08x)\n",
        td, MDSTR(szName), pvSigBlob, cbSigBlob, pmb));

    START_MD_PERF();

    // Don't lock this function. All of the functions that it calls are public APIs. keep it that way.

    // try to match with method first of all
    hr = FindMethod(
        td,
        szName,
        pvSigBlob,
        cbSigBlob,
        pmb);

    if ( hr == CLDB_E_RECORD_NOTFOUND )
    {
        // now try field table
        IfFailGo( FindField(
            td,
            szName,
            pvSigBlob,
            cbSigBlob,
            pmb) );
    }
ErrExit:
    STOP_MD_PERF(FindMember);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::FindMember()



//*****************************************************************************
// Find a given member in a TypeDef (typically a class).
//*****************************************************************************
STDMETHODIMP RegMeta::FindMethod(
    mdTypeDef   td,                     // [IN] given typedef
    LPCWSTR     szName,                 // [IN] member name
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature
    ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
    mdMethodDef *pmb)                   // [OUT] matching memberdef
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW           *pMiniMd = &(m_pStgdb->m_MiniMd);
    LPUTF8              szNameUtf8;
    UTF8STR(szName, szNameUtf8);

    LOG((LOGMD, "MD RegMeta::FindMethod(0x%08x, %S, 0x%08x, 0x%08x, 0x%08x)\n",
        td, MDSTR(szName), pvSigBlob, cbSigBlob, pmb));

    START_MD_PERF();
    LOCKREAD();

    if (szName == NULL)
        IfFailGo(E_INVALIDARG);
    PREFIX_ASSUME(szName != NULL);

    // If this is a global method, then use the <Module> typedef as parent.
    IsGlobalMethodParent(&td);

    IfFailGo(ImportHelper::FindMethod(pMiniMd,
        td,
        szNameUtf8,
        pvSigBlob,
        cbSigBlob,
        pmb));

ErrExit:
    STOP_MD_PERF(FindMethod);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // RegMeta::FindMethod


//*****************************************************************************
// Find a given member in a TypeDef (typically a class).
//*****************************************************************************
STDMETHODIMP
RegMeta::FindField(
    mdTypeDef       td,             // [IN] given typedef
    LPCWSTR         szName,         // [IN] member name
    PCCOR_SIGNATURE pvSigBlob,      // [IN] point to a blob value of COM+ signature
    ULONG           cbSigBlob,      // [IN] count of bytes in the signature blob
    mdFieldDef     *pmb)            // [OUT] matching memberdef
{
    HRESULT hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

    LOG((LOGMD, "MD RegMeta::FindField(0x%08x, %S, 0x%08x, 0x%08x, 0x%08x)\n",
        td, MDSTR(szName), pvSigBlob, cbSigBlob, pmb));

    START_MD_PERF();
    LOCKREAD();

    if (szName == NULL)
        IfFailGo(E_INVALIDARG);

    LPUTF8 szNameUtf8;
    UTF8STR(szName, szNameUtf8);

    // If this is a global method, then use the <Module> typedef as parent.
    IsGlobalMethodParent(&td);

    IfFailGo(ImportHelper::FindField(pMiniMd,
        td,
        szNameUtf8,
        pvSigBlob,
        cbSigBlob,
        pmb));

ErrExit:
    STOP_MD_PERF(FindField);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // RegMeta::FindField


//*****************************************************************************
// Find a given MemberRef in a TypeRef (typically a class).  If no TypeRef
//  is specified, the query will be for a random member in the scope.
//*****************************************************************************
STDMETHODIMP RegMeta::FindMemberRef(
    mdToken     tkPar,                  // [IN] given parent token.
    LPCWSTR     szName,                 // [IN] member name
    PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of COM+ signature
    ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
    mdMemberRef *pmr)                   // [OUT] matching memberref
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW           *pMiniMd = &(m_pStgdb->m_MiniMd);
    LPUTF8              szNameUtf8;
    UTF8STR(szName, szNameUtf8);

    LOG((LOGMD, "MD RegMeta::FindMemberRef(0x%08x, %S, 0x%08x, 0x%08x, 0x%08x)\n",
        tkPar, MDSTR(szName), pvSigBlob, cbSigBlob, pmr));



    START_MD_PERF();

    // <TODO>@todo: Can this causing building hash table? If so, should this consider the write lock?</TODO>
    LOCKREAD();

    // get the range of field rids given a typedef
    _ASSERTE(TypeFromToken(tkPar) == mdtTypeRef || TypeFromToken(tkPar) == mdtMethodDef ||
            TypeFromToken(tkPar) == mdtModuleRef || TypeFromToken(tkPar) == mdtTypeDef ||
            TypeFromToken(tkPar) == mdtTypeSpec);

    // Set parent to global class m_tdModule if mdTokenNil is passed.
    if (IsNilToken(tkPar))
        tkPar = m_tdModule;

    IfFailGo( ImportHelper::FindMemberRef(pMiniMd, tkPar, szNameUtf8, pvSigBlob, cbSigBlob, pmr) );

ErrExit:

    STOP_MD_PERF(FindMemberRef);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::FindMemberRef()


//*****************************************************************************
// Return the property of a MethodDef
//*****************************************************************************
STDMETHODIMP RegMeta::GetMethodProps(
    mdMethodDef mb,                     // The method for which to get props.
    mdTypeDef   *pClass,                // Put method's class here.
    __out_ecount_opt (cchMethod) LPWSTR szMethod, // Put method's name here.
    ULONG       cchMethod,              // Size of szMethod buffer in wide chars.
    ULONG       *pchMethod,             // Put actual size here
    DWORD       *pdwAttr,               // Put flags here.
    PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
    ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
    ULONG       *pulCodeRVA,            // [OUT] codeRVA
    DWORD       *pdwImplFlags)          // [OUT] Impl. Flags
{
    HRESULT             hr = NOERROR;
    BEGIN_ENTRYPOINT_NOTHROW;

    MethodRec           *pMethodRec;
    CMiniMdRW           *pMiniMd = &(m_pStgdb->m_MiniMd);

    LOG((LOGMD, "MD RegMeta::GetMethodProps(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        mb, pClass, szMethod, cchMethod, pchMethod, pdwAttr, ppvSigBlob, pcbSigBlob,
        pulCodeRVA, pdwImplFlags));



    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(mb) == mdtMethodDef);

    IfFailGo(pMiniMd->GetMethodRecord(RidFromToken(mb), &pMethodRec));

    if (pClass)
    {
        // caller wants parent typedef
        IfFailGo( pMiniMd->FindParentOfMethodHelper(mb, pClass) );

        if ( IsGlobalMethodParentToken(*pClass) )
        {
            // If the parent of Method is the <Module>, return mdTypeDefNil instead.
            *pClass = mdTypeDefNil;
        }

    }
    if (ppvSigBlob || pcbSigBlob)
    {
        // caller wants signature information
        PCCOR_SIGNATURE pvSigTmp;
        ULONG           cbSig;
        IfFailGo(pMiniMd->getSignatureOfMethod(pMethodRec, &pvSigTmp, &cbSig));
        if ( ppvSigBlob )
            *ppvSigBlob = pvSigTmp;
        if ( pcbSigBlob)
            *pcbSigBlob = cbSig;
    }
    if ( pdwAttr )
    {
        *pdwAttr = pMiniMd->getFlagsOfMethod(pMethodRec);
    }
    if ( pulCodeRVA )
    {
        *pulCodeRVA = pMiniMd->getRVAOfMethod(pMethodRec);
    }
    if ( pdwImplFlags )
    {
        *pdwImplFlags = (DWORD )pMiniMd->getImplFlagsOfMethod(pMethodRec);
    }
    // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten with S_OK
    if (szMethod || pchMethod)
    {
        IfFailGo( pMiniMd->getNameOfMethod(pMethodRec, szMethod, cchMethod, pchMethod) );
    }

ErrExit:
    STOP_MD_PERF(GetMethodProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::GetMethodProps()


//*****************************************************************************
// Return the property of a MemberRef
//*****************************************************************************
STDMETHODIMP RegMeta::GetMemberRefProps(      // S_OK or error.
    mdMemberRef mr,                     // [IN] given memberref
    mdToken     *ptk,                   // [OUT] Put classref or classdef here.
    __out_ecount_opt (cchMember) LPWSTR szMember, // [OUT] buffer to fill for member's name
    ULONG       cchMember,              // [IN] the count of char of szMember
    ULONG       *pchMember,             // [OUT] actual count of char in member name
    PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to meta data blob value
    ULONG       *pbSig)                 // [OUT] actual size of signature blob
{
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);
    MemberRefRec    *pMemberRefRec;

    LOG((LOGMD, "MD RegMeta::GetMemberRefProps(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        mr, ptk, szMember, cchMember, pchMember, ppvSigBlob, pbSig));



    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(mr) == mdtMemberRef);

    IfFailGo(pMiniMd->GetMemberRefRecord(RidFromToken(mr), &pMemberRefRec));

    if (ptk)
    {
        *ptk = pMiniMd->getClassOfMemberRef(pMemberRefRec);
        if ( IsGlobalMethodParentToken(*ptk) )
        {
            // If the parent of MemberRef is the <Module>, return mdTypeDefNil instead.
            *ptk = mdTypeDefNil;
        }

    }
    if (ppvSigBlob || pbSig)
    {
        // caller wants signature information
        PCCOR_SIGNATURE pvSigTmp;
        ULONG           cbSig;
        IfFailGo(pMiniMd->getSignatureOfMemberRef(pMemberRefRec, &pvSigTmp, &cbSig));
        if ( ppvSigBlob )
            *ppvSigBlob = pvSigTmp;
        if ( pbSig)
            *pbSig = cbSig;
    }
    // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten with S_OK
    if (szMember || pchMember)
    {
        IfFailGo( pMiniMd->getNameOfMemberRef(pMemberRefRec, szMember, cchMember, pchMember) );
    }

ErrExit:

    STOP_MD_PERF(GetMemberRefProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::GetMemberRefProps()


//*****************************************************************************
// enumerate Property tokens for a typedef
//*****************************************************************************
STDMETHODIMP RegMeta::EnumProperties(         // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
    mdProperty  rProperties[],          // [OUT] Put Properties here.
    ULONG       cMax,                   // [IN] Max properties to put.
    ULONG       *pcProperties)          // [OUT] Put # put here.
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    RID                 ridStart = 0;
    RID                 ridEnd = 0;
    RID                 ridMax = 0;
    HENUMInternal       *pEnum = NULL;

    LOG((LOGMD, "MD RegMeta::EnumProperties(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, td, rProperties, cMax, pcProperties));

    START_MD_PERF();
    LOCKREAD();

    if (IsNilToken(td))
    {
        if (pcProperties)
            *pcProperties = 0;
        hr = S_FALSE;
        goto ErrExit;
    }

    _ASSERTE(TypeFromToken(td) == mdtTypeDef);


    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);
        RID         ridPropertyMap;
        PropertyMapRec *pPropertyMapRec;

        // get the starting/ending rid of properties of this typedef
        IfFailGo(pMiniMd->FindPropertyMapFor(RidFromToken(td), &ridPropertyMap));
        if (!InvalidRid(ridPropertyMap))
        {
            IfFailGo(m_pStgdb->m_MiniMd.GetPropertyMapRecord(ridPropertyMap, &pPropertyMapRec));
            ridStart = pMiniMd->getPropertyListOfPropertyMap(pPropertyMapRec);
            IfFailGo(pMiniMd->getEndPropertyListOfPropertyMap(ridPropertyMap, &ridEnd));
            ridMax = pMiniMd->getCountPropertys() + 1;
            if(ridStart == 0) ridStart = 1;
            if(ridEnd > ridMax) ridEnd = ridMax;
            if(ridStart > ridEnd) ridStart=ridEnd;
        }

        if (pMiniMd->HasIndirectTable(TBL_Property) || pMiniMd->HasDelete())
        {
            IfFailGo( HENUMInternal::CreateDynamicArrayEnum( mdtProperty, &pEnum) );

            // add all methods to the dynamic array
            for (ULONG index = ridStart; index < ridEnd; index++ )
            {
                if (pMiniMd->HasDelete() &&
                    ((m_OptionValue.m_ImportOption & MDImportOptionAllProperties) == 0))
                {
                    PropertyRec *pRec;
                    RID rid;
                    IfFailGo(pMiniMd->GetPropertyRid(index, &rid));
                    IfFailGo(pMiniMd->GetPropertyRecord(rid, &pRec));
                    LPCUTF8 szPropertyName;
                    IfFailGo(pMiniMd->getNameOfProperty(pRec, &szPropertyName));
                    if (IsPrRTSpecialName(pRec->GetPropFlags()) && IsDeletedName(szPropertyName))
                    {
                        continue;
                    }
                }
                RID rid;
                IfFailGo(pMiniMd->GetPropertyRid(index, &rid));
                IfFailGo(HENUMInternal::AddElementToEnum(
                    pEnum,
                    TokenFromRid(rid, mdtProperty)));
            }
        }
        else
        {
            IfFailGo( HENUMInternal::CreateSimpleEnum( mdtProperty, ridStart, ridEnd, &pEnum) );
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rProperties, pcProperties);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);


    STOP_MD_PERF(EnumProperties);
    END_ENTRYPOINT_NOTHROW;

    return hr;

} // STDMETHODIMP RegMeta::EnumProperties()


//*****************************************************************************
// enumerate event tokens for a typedef
//*****************************************************************************
STDMETHODIMP RegMeta::EnumEvents(              // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
    mdEvent     rEvents[],              // [OUT] Put events here.
    ULONG       cMax,                   // [IN] Max events to put.
    ULONG       *pcEvents)              // [OUT] Put # put here.
{
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal   **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    RID             ridStart = 0;
    RID             ridEnd = 0;
    RID             ridMax = 0;
    HENUMInternal   *pEnum = NULL;

    LOG((LOGMD, "MD RegMeta::EnumEvents(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, td, rEvents,  cMax, pcEvents));

    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(td) == mdtTypeDef);


    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);
        RID         ridEventMap;
        EventMapRec *pEventMapRec;

        // get the starting/ending rid of properties of this typedef
        IfFailGo(pMiniMd->FindEventMapFor(RidFromToken(td), &ridEventMap));
        if (!InvalidRid(ridEventMap))
        {
            IfFailGo(pMiniMd->GetEventMapRecord(ridEventMap, &pEventMapRec));
            ridStart = pMiniMd->getEventListOfEventMap(pEventMapRec);
            IfFailGo(pMiniMd->getEndEventListOfEventMap(ridEventMap, &ridEnd));
            ridMax = pMiniMd->getCountEvents() + 1;
            if(ridStart == 0) ridStart = 1;
            if(ridEnd > ridMax) ridEnd = ridMax;
            if(ridStart > ridEnd) ridStart=ridEnd;
        }

        if (pMiniMd->HasIndirectTable(TBL_Event) || pMiniMd->HasDelete())
        {
            IfFailGo( HENUMInternal::CreateDynamicArrayEnum( mdtEvent, &pEnum) );

            // add all methods to the dynamic array
            for (ULONG index = ridStart; index < ridEnd; index++ )
            {
                if (pMiniMd->HasDelete() &&
                    ((m_OptionValue.m_ImportOption & MDImportOptionAllEvents) == 0))
                {
                    EventRec *pRec;
                    RID rid;
                    IfFailGo(pMiniMd->GetEventRid(index, &rid));
                    IfFailGo(pMiniMd->GetEventRecord(rid, &pRec));
                    LPCSTR szEventName;
                    IfFailGo(pMiniMd->getNameOfEvent(pRec, &szEventName));
                    if (IsEvRTSpecialName(pRec->GetEventFlags()) && IsDeletedName(szEventName))
                    {
                        continue;
                    }
                }
                RID rid;
                IfFailGo(pMiniMd->GetEventRid(index, &rid));
                IfFailGo(HENUMInternal::AddElementToEnum(
                    pEnum,
                    TokenFromRid(rid, mdtEvent)));
            }
        }
        else
        {
            IfFailGo( HENUMInternal::CreateSimpleEnum( mdtEvent, ridStart, ridEnd, &pEnum) );
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rEvents, pcEvents);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumEvents);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::EnumEvents()



//*****************************************************************************
// return the properties of an event token
//*****************************************************************************
STDMETHODIMP RegMeta::GetEventProps(          // S_OK, S_FALSE, or error.
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
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);
    EventRec        *pRec;
    HENUMInternal   hEnum;

    LOG((LOGMD, "MD RegMeta::GetEventProps(0x%08x, 0x%08x, %S, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        ev, pClass, MDSTR(szEvent), cchEvent, pchEvent, pdwEventFlags, ptkEventType,
        pmdAddOn, pmdRemoveOn, pmdFire, rmdOtherMethod, cMax, pcOtherMethod));

    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(ev) == mdtEvent);

    HENUMInternal::ZeroEnum(&hEnum);
    IfFailGo(pMiniMd->GetEventRecord(RidFromToken(ev), &pRec));

    if ( pClass )
    {
        // find the event map entry corresponding to this event
        IfFailGo( pMiniMd->FindParentOfEventHelper( ev, pClass ) );
    }
    if ( pdwEventFlags )
    {
        *pdwEventFlags = pMiniMd->getEventFlagsOfEvent(pRec);
    }
    if ( ptkEventType )
    {
        *ptkEventType = pMiniMd->getEventTypeOfEvent(pRec);
    }
    {
        MethodSemanticsRec *pSemantics;
        RID         ridCur;
        ULONG       cCurOtherMethod = 0;
        ULONG       ulSemantics;
        mdMethodDef tkMethod;

        // initialize output parameters
        if (pmdAddOn)
            *pmdAddOn = mdMethodDefNil;
        if (pmdRemoveOn)
            *pmdRemoveOn = mdMethodDefNil;
        if (pmdFire)
            *pmdFire = mdMethodDefNil;

        IfFailGo( pMiniMd->FindMethodSemanticsHelper(ev, &hEnum) );
        while (HENUMInternal::EnumNext(&hEnum, (mdToken *)&ridCur))
        {
            IfFailGo(pMiniMd->GetMethodSemanticsRecord(ridCur, &pSemantics));
            ulSemantics = pMiniMd->getSemanticOfMethodSemantics(pSemantics);
            tkMethod = TokenFromRid( pMiniMd->getMethodOfMethodSemantics(pSemantics), mdtMethodDef );
            switch (ulSemantics)
            {
            case msAddOn:
                if (pmdAddOn) *pmdAddOn = tkMethod;
                break;
            case msRemoveOn:
                if (pmdRemoveOn) *pmdRemoveOn = tkMethod;
                break;
            case msFire:
                if (pmdFire) *pmdFire = tkMethod;
                break;
            case msOther:
                if (cCurOtherMethod < cMax)
                    rmdOtherMethod[cCurOtherMethod] = tkMethod;
                cCurOtherMethod++;
                break;
            default:
                _ASSERTE(!"BadKind!");
            }
        }

        // set the output parameter
        if (pcOtherMethod)
            *pcOtherMethod = cCurOtherMethod;
    }
    // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten with S_OK
    if (szEvent || pchEvent)
    {
        IfFailGo( pMiniMd->getNameOfEvent(pRec, (LPWSTR) szEvent, cchEvent, pchEvent) );
    }

ErrExit:
    HENUMInternal::ClearEnum(&hEnum);
    STOP_MD_PERF(GetEventProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::GetEventProps()


//*****************************************************************************
// given a method, return an arra of event/property tokens for each accessor role
// it is defined to have
//*****************************************************************************
STDMETHODIMP RegMeta::EnumMethodSemantics(    // S_OK, S_FALSE, or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdMethodDef mb,                     // [IN] MethodDef to scope the enumeration.
    mdToken     rEventProp[],           // [OUT] Put Event/Property here.
    ULONG       cMax,                   // [IN] Max properties to put.
    ULONG       *pcEventProp)           // [OUT] Put # put here.
{
    HRESULT             hr = NOERROR;
    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    ULONG               ridEnd;
    ULONG               index;
    HENUMInternal       *pEnum = NULL;
    MethodSemanticsRec  *pRec;

    LOG((LOGMD, "MD RegMeta::EnumMethodSemantics(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, mb, rEventProp, cMax, pcEventProp));

    START_MD_PERF();
    LOCKREAD();


    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);

        // create the enumerator
        IfFailGo( HENUMInternal::CreateDynamicArrayEnum( (DWORD) -1, &pEnum) );

        // get the range of method rids given a typedef
        ridEnd = pMiniMd->getCountMethodSemantics();

        for (index = 1; index <= ridEnd; index++ )
        {
            IfFailGo(pMiniMd->GetMethodSemanticsRecord(index, &pRec));
            if ( pMiniMd->getMethodOfMethodSemantics(pRec) ==  mb )
            {
                IfFailGo( HENUMInternal::AddElementToEnum(pEnum, pMiniMd->getAssociationOfMethodSemantics(pRec) ) );
            }
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rEventProp, pcEventProp);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumMethodSemantics);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::EnumMethodSemantics()



//*****************************************************************************
// return the role flags for the method/propevent pair
//*****************************************************************************
STDMETHODIMP RegMeta::GetMethodSemantics(     // S_OK, S_FALSE, or error.
    mdMethodDef mb,                     // [IN] method token
    mdToken     tkEventProp,            // [IN] event/property token.
    DWORD       *pdwSemanticsFlags)     // [OUT] the role flags for the method/propevent pair
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW           *pMiniMd = &(m_pStgdb->m_MiniMd);
    MethodSemanticsRec *pRec;
    ULONG               ridCur;
    HENUMInternal       hEnum;

    LOG((LOGMD, "MD RegMeta::GetMethodSemantics(0x%08x, 0x%08x, 0x%08x)\n",
        mb, tkEventProp, pdwSemanticsFlags));



    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(mb) == mdtMethodDef);
    _ASSERTE( pdwSemanticsFlags );

    *pdwSemanticsFlags = 0;
    HENUMInternal::ZeroEnum(&hEnum);

    // loop through all methods associated with this tkEventProp
    IfFailGo( pMiniMd->FindMethodSemanticsHelper(tkEventProp, &hEnum) );
    while (HENUMInternal::EnumNext(&hEnum, (mdToken *)&ridCur))
    {
        IfFailGo(pMiniMd->GetMethodSemanticsRecord(ridCur, &pRec));
        if ( pMiniMd->getMethodOfMethodSemantics(pRec) ==  mb )
        {
            // we findd the match
            *pdwSemanticsFlags = pMiniMd->getSemanticOfMethodSemantics(pRec);
            goto ErrExit;
        }
    }

    IfFailGo( CLDB_E_RECORD_NOTFOUND );

ErrExit:
    HENUMInternal::ClearEnum(&hEnum);
    STOP_MD_PERF(GetMethodSemantics);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::GetMethodSemantics()



//*****************************************************************************
// return the class layout information
//*****************************************************************************
STDMETHODIMP RegMeta::GetClassLayout(
    mdTypeDef   td,                     // [IN] give typedef
    DWORD       *pdwPackSize,           // [OUT] 1, 2, 4, 8, or 16
    COR_FIELD_OFFSET rFieldOffset[],    // [OUT] field offset array
    ULONG       cMax,                   // [IN] size of the array
    ULONG       *pcFieldOffset,         // [OUT] needed array size
    ULONG       *pulClassSize)          // [OUT] the size of the class
{
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);
    ClassLayoutRec  *pRec;
    RID             ridClassLayout;
    int             bLayout=0;          // Was any layout information found?

    _ASSERTE(TypeFromToken(td) == mdtTypeDef);

    LOG((LOGMD, "MD RegMeta::GetClassLayout(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        td, pdwPackSize, rFieldOffset, cMax, pcFieldOffset, pulClassSize));

    START_MD_PERF();
    LOCKREAD();

    IfFailGo(pMiniMd->FindClassLayoutHelper(td, &ridClassLayout));

    if (InvalidRid(ridClassLayout))
    {   // Nothing specified - return default values of 0.
        if ( pdwPackSize )
            *pdwPackSize = 0;
        if ( pulClassSize )
            *pulClassSize = 0;
    }
    else
    {
        IfFailGo(pMiniMd->GetClassLayoutRecord(RidFromToken(ridClassLayout), &pRec));
        if ( pdwPackSize )
            *pdwPackSize = pMiniMd->getPackingSizeOfClassLayout(pRec);
        if ( pulClassSize )
            *pulClassSize = pMiniMd->getClassSizeOfClassLayout(pRec);
        bLayout = 1;
    }

    // fill the layout array
    if (rFieldOffset || pcFieldOffset)
    {
        ULONG       iFieldOffset = 0;
        RID         ridFieldStart;
        RID         ridFieldEnd;
        RID         ridFieldLayout;
        ULONG       ulOffset;
        TypeDefRec  *pTypeDefRec;
        FieldLayoutRec *pLayout2Rec;
        mdFieldDef  fd;

        // record for this typedef in TypeDef Table
        IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(td), &pTypeDefRec));

        // find the starting and end field for this typedef
        ridFieldStart = pMiniMd->getFieldListOfTypeDef(pTypeDefRec);
        IfFailGo(pMiniMd->getEndFieldListOfTypeDef(RidFromToken(td), &ridFieldEnd));

        // loop through the field table

        for(; ridFieldStart < ridFieldEnd; ridFieldStart++)
        {
            // Calculate the field token.
            RID rid;
            IfFailGo(pMiniMd->GetFieldRid(ridFieldStart, &rid));
            fd = TokenFromRid(rid, mdtFieldDef);

            // Calculate the FieldLayout rid for the current field.
            IfFailGo(pMiniMd->FindFieldLayoutHelper(fd, &ridFieldLayout));

            // Calculate the offset.
            if (InvalidRid(ridFieldLayout))
                ulOffset = (ULONG) -1;
            else
            {
                // get the FieldLayout record.
                IfFailGo(pMiniMd->GetFieldLayoutRecord(ridFieldLayout, &pLayout2Rec));
                ulOffset = pMiniMd->getOffSetOfFieldLayout(pLayout2Rec);
                bLayout = 1;
            }

            // fill in the field layout if output buffer still has space.
            if (cMax > iFieldOffset && rFieldOffset)
            {
                rFieldOffset[iFieldOffset].ridOfField = fd;
                rFieldOffset[iFieldOffset].ulOffset = ulOffset;
            }

            // advance the index to the buffer.
            iFieldOffset++;
        }

        if (bLayout && pcFieldOffset)
            *pcFieldOffset = iFieldOffset;
    }

    if (!bLayout)
        hr = CLDB_E_RECORD_NOTFOUND;

ErrExit:
    STOP_MD_PERF(GetClassLayout);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::GetClassLayout()



//*****************************************************************************
// return the native type of a field
//*****************************************************************************
STDMETHODIMP RegMeta::GetFieldMarshal(
    mdToken     tk,                     // [IN] given a field's memberdef
    PCCOR_SIGNATURE *ppvNativeType,     // [OUT] native type of this field
    ULONG       *pcbNativeType)         // [OUT] the count of bytes of *ppvNativeType
{
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);
    RID             rid;
    FieldMarshalRec *pFieldMarshalRec;


    _ASSERTE(ppvNativeType != NULL && pcbNativeType != NULL);

    LOG((LOGMD, "MD RegMeta::GetFieldMarshal(0x%08x, 0x%08x, 0x%08x)\n",
        tk, ppvNativeType, pcbNativeType));

    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(tk) == mdtParamDef || TypeFromToken(tk) == mdtFieldDef);

    // find the row containing the marshal definition for tk
    IfFailGo(pMiniMd->FindFieldMarshalHelper(tk, &rid));
    if (InvalidRid(rid))
    {
        IfFailGo( CLDB_E_RECORD_NOTFOUND );
    }
    IfFailGo(pMiniMd->GetFieldMarshalRecord(rid, &pFieldMarshalRec));

    // get the native type
    IfFailGo(pMiniMd->getNativeTypeOfFieldMarshal(pFieldMarshalRec, ppvNativeType, pcbNativeType));

ErrExit:
    STOP_MD_PERF(GetFieldMarshal);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::GetFieldMarshal()



//*****************************************************************************
// return the RVA and implflag for MethodDef or FieldDef token
//*****************************************************************************
STDMETHODIMP
RegMeta::GetRVA(
    mdToken tk,             // Member for which to set offset
    ULONG  *pulCodeRVA,     // The offset
    DWORD  *pdwImplFlags)   // the implementation flags
{
    HRESULT hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

    LOG((LOGMD, "MD RegMeta::GetRVA(0x%08x, 0x%08x, 0x%08x)\n",
        tk, pulCodeRVA, pdwImplFlags));

    START_MD_PERF();
    LOCKREAD();

    if (TypeFromToken(tk) == mdtMethodDef)
    {
        if (tk == mdMethodDefNil)
        {   // Backward compatibility with CLR 2.0 implementation
            if (pulCodeRVA != NULL)
                *pulCodeRVA = 0;
            if (pdwImplFlags != NULL)
                *pdwImplFlags = 0;

            hr = S_OK;
            goto ErrExit;
        }

        // MethodDef token
        MethodRec *pMethodRec;
        IfFailGo(pMiniMd->GetMethodRecord(RidFromToken(tk), &pMethodRec));

        if (pulCodeRVA != NULL)
        {
            *pulCodeRVA = pMiniMd->getRVAOfMethod(pMethodRec);
        }
        if (pdwImplFlags != NULL)
        {
            *pdwImplFlags = pMiniMd->getImplFlagsOfMethod(pMethodRec);
        }
    }
    else
    {   // FieldDef token or invalid type of token (not mdtMethodDef)
        uint32_t iRecord;

        IfFailGo(pMiniMd->FindFieldRVAHelper(tk, &iRecord));

        if (InvalidRid(iRecord))
        {
            if (pulCodeRVA != NULL)
                *pulCodeRVA = 0;

            IfFailGo(CLDB_E_RECORD_NOTFOUND);
        }

        FieldRVARec *pFieldRVARec;
        IfFailGo(pMiniMd->GetFieldRVARecord(iRecord, &pFieldRVARec));

        if (pulCodeRVA != NULL)
        {
            *pulCodeRVA = pMiniMd->getRVAOfFieldRVA(pFieldRVARec);
        }
        if (pdwImplFlags != NULL)
        {
            *pdwImplFlags = 0;
        }
    }
ErrExit:
    STOP_MD_PERF(GetRVA);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // RegMeta::GetRVA



//*****************************************************************************
// Get the Action and Permissions blob for a given PermissionSet.
//*****************************************************************************
STDMETHODIMP RegMeta::GetPermissionSetProps(
    mdPermission pm,                    // [IN] the permission token.
    DWORD       *pdwAction,             // [OUT] CorDeclSecurity.
    void const  **ppvPermission,        // [OUT] permission blob.
    ULONG       *pcbPermission)         // [OUT] count of bytes of pvPermission.
{
    HRESULT             hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    LOG((LOGMD, "MD RegMeta::GetPermissionSetProps(0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        pm, pdwAction, ppvPermission, pcbPermission));

    CMiniMdRW           *pMiniMd = NULL;
    DeclSecurityRec     *pRecord = NULL;

    START_MD_PERF();
    LOCKREAD();

    pMiniMd = &(m_pStgdb->m_MiniMd);
    IfFailGo(pMiniMd->GetDeclSecurityRecord(RidFromToken(pm), &pRecord));

    _ASSERTE(TypeFromToken(pm) == mdtPermission && RidFromToken(pm));

    // If you want the BLOB, better get the BLOB size as well.
    _ASSERTE(!ppvPermission || pcbPermission);

    if (pdwAction)
        *pdwAction = pMiniMd->getActionOfDeclSecurity(pRecord);

    if (ppvPermission != NULL)
    {
        IfFailGo(pMiniMd->getPermissionSetOfDeclSecurity(pRecord, (const BYTE **)ppvPermission, pcbPermission));
    }

ErrExit:

    STOP_MD_PERF(GetPermissionSetProps);
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // STDMETHODIMP RegMeta::GetPermissionSetProps()



//*****************************************************************************
// Given a signature token, get return a pointer to the signature to the caller.
//
//<TODO>@FUTURE: for short term we have a problem where there is no way to get a
// fixed up address for a blob and do Merge at the same time.  So we've created
// this dummy table called StandAloneSig which you hand out a rid for.  This
// makes finding the sig an extra indirection that is not required.  The
// Model Compression save code needs to map the token into a byte offset in
// the heap.  Perhaps we can have another mdt* type to switch on the difference.
// But ultimately it has to simply be "pBlobHeapBase + RidFromToken(mdSig)".</TODO>
//*****************************************************************************
STDMETHODIMP RegMeta::GetSigFromToken(        // S_OK or error.
    mdSignature mdSig,                  // [IN] Signature token.
    PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to token.
    ULONG       *pcbSig)                // [OUT] return size of signature.
{
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);
    StandAloneSigRec *pRec;

    LOG((LOGMD, "MD RegMeta::GetSigFromToken(0x%08x, 0x%08x, 0x%08x)\n",
        mdSig, ppvSig, pcbSig));



    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(mdSig) == mdtSignature);
    _ASSERTE(ppvSig && pcbSig);

    IfFailGo(pMiniMd->GetStandAloneSigRecord(RidFromToken(mdSig), &pRec));
    IfFailGo(pMiniMd->getSignatureOfStandAloneSig(pRec, ppvSig, pcbSig));


ErrExit:

    STOP_MD_PERF(GetSigFromToken);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::GetSigFromToken()


//*******************************************************************************
// return the ModuleRef properties
//*******************************************************************************
STDMETHODIMP RegMeta::GetModuleRefProps(      // S_OK or error.
    mdModuleRef mur,                    // [IN] moduleref token.
    __out_ecount_opt (cchName) LPWSTR szName, // [OUT] buffer to fill with the moduleref name.
    ULONG       cchName,                // [IN] size of szName in wide characters.
    ULONG       *pchName)               // [OUT] actual count of characters in the name.
{
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);
    ModuleRefRec    *pModuleRefRec;



    LOG((LOGMD, "MD RegMeta::GetModuleRefProps(0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        mur, szName, cchName, pchName));
    START_MD_PERF();
    LOCKREAD();

    IfFailGo(pMiniMd->GetModuleRefRecord(RidFromToken(mur), &pModuleRefRec));

    _ASSERTE(TypeFromToken(mur) == mdtModuleRef);

    // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten with S_OK
    if (szName || pchName)
    {
        IfFailGo( pMiniMd->getNameOfModuleRef(pModuleRefRec, szName, cchName, pchName) );
    }

ErrExit:

    STOP_MD_PERF(GetModuleRefProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::GetModuleRefProps()



//*******************************************************************************
// enumerating through all of the ModuleRefs
//*******************************************************************************
STDMETHODIMP RegMeta::EnumModuleRefs(         // S_OK or error.
    HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
    mdModuleRef rModuleRefs[],          // [OUT] put modulerefs here.
    ULONG       cMax,                   // [IN] max memberrefs to put.
    ULONG       *pcModuleRefs)          // [OUT] put # put here.
{
    HRESULT hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    HENUMInternal  *pEnum;

    LOG((LOGMD, "MD RegMeta::EnumModuleRefs(0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, rModuleRefs, cMax, pcModuleRefs));

    START_MD_PERF();
    LOCKREAD();

    if (*ppmdEnum == NULL)
    {
        // instantiating a new ENUM
        CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

        // create the enumerator
        IfFailGo(HENUMInternal::CreateSimpleEnum(
            mdtModuleRef,
            1,
            pMiniMd->getCountModuleRefs() + 1,
            &pEnum));

        // set the output parameter
        *ppmdEnum = pEnum;
    }
    else
    {
        pEnum = *ppmdEnum;
    }

    // we can only fill the minimun of what caller asked for or what we have left
    IfFailGo(HENUMInternal::EnumWithCount(pEnum, cMax, rModuleRefs, pcModuleRefs));

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);

    STOP_MD_PERF(EnumModuleRefs);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::EnumModuleRefs()


//*******************************************************************************
// return properties regarding a TypeSpec
//*******************************************************************************
STDMETHODIMP RegMeta::GetTypeSpecFromToken(   // S_OK or error.
    mdTypeSpec typespec,                // [IN] Signature token.
    PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to token.
    ULONG       *pcbSig)                // [OUT] return size of signature.
{
    HRESULT             hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW           *pMiniMd = &(m_pStgdb->m_MiniMd);
    TypeSpecRec *pRec = NULL;

    LOG((LOGMD, "MD RegMeta::GetTypeSpecFromToken(0x%08x, 0x%08x, 0x%08x)\n",
        typespec, ppvSig, pcbSig));



    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(typespec) == mdtTypeSpec);
    _ASSERTE(ppvSig && pcbSig);

    IfFailGo(pMiniMd->GetTypeSpecRecord(RidFromToken(typespec), &pRec));
    IfFailGo(pMiniMd->getSignatureOfTypeSpec(pRec, ppvSig, pcbSig));

ErrExit:

    STOP_MD_PERF(GetTypeSpecFromToken);
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // STDMETHODIMP RegMeta::GetTypeSpecFromToken()


//*****************************************************************************
// For those items that have a name, retrieve a direct pointer to the name
// off of the heap.  This reduces copies made for the caller.
//*****************************************************************************
#define NAME_FROM_TOKEN_TYPE(RecType, TokenType) \
        case mdt ## TokenType: \
        { \
            RecType ## Rec  *pRecord; \
            IfFailGo(pMiniMd->Get ## RecType ## Record(RidFromToken(tk), &pRecord)); \
            IfFailGo(pMiniMd->getNameOf ## RecType (pRecord, pszUtf8NamePtr)); \
        } \
        break;
#define NAME_FROM_TOKEN(RecType) NAME_FROM_TOKEN_TYPE(RecType, RecType)

STDMETHODIMP RegMeta::GetNameFromToken(       // S_OK or error.
    mdToken     tk,                     // [IN] Token to get name from.  Must have a name.
    MDUTF8CSTR  *pszUtf8NamePtr)        // [OUT] Return pointer to UTF8 name in heap.
{
    HRESULT     hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;


    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);

    LOG((LOGMD, "MD RegMeta::GetNameFromToken(0x%08x, 0x%08x)\n",
        tk, pszUtf8NamePtr));

    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(pszUtf8NamePtr);

    switch (TypeFromToken(tk))
    {
        NAME_FROM_TOKEN(Module);
        NAME_FROM_TOKEN(TypeRef);
        NAME_FROM_TOKEN(TypeDef);
        NAME_FROM_TOKEN_TYPE(Field, FieldDef);
        NAME_FROM_TOKEN_TYPE(Method, MethodDef);
        NAME_FROM_TOKEN_TYPE(Param, ParamDef);
        NAME_FROM_TOKEN(MemberRef);
        NAME_FROM_TOKEN(Event);
        NAME_FROM_TOKEN(Property);
        NAME_FROM_TOKEN(ModuleRef);

        default:
        hr = E_INVALIDARG;
    }

ErrExit:

    STOP_MD_PERF(GetNameFromToken);

    END_ENTRYPOINT_NOTHROW;

    return (hr);
} // RegMeta::GetNameFromToken


//*****************************************************************************
// Get the symbol binding data back from the module if it is there.  It is
// stored as a custom value.
//*****************************************************************************
STDMETHODIMP RegMeta::EnumUnresolvedMethods(  // S_OK or error.
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdToken     rMethods[],             // [OUT] Put MemberDefs here.
    ULONG       cMax,                   // [IN] Max MemberDefs to put.
    ULONG       *pcTokens)              // [OUT] Put # put here.
{
#ifdef FEATURE_METADATA_EMIT
    HRESULT hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal ** ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    uint32_t         iCountTypeDef;      // Count of TypeDefs.
    uint32_t         ulStart, ulEnd;     // Bounds of methods on a given TypeDef.
    uint32_t         index;              // For counting methods on a TypeDef.
    uint32_t         indexTypeDef;       // For counting TypeDefs.
    bool             bIsInterface;       // Is a given TypeDef an interface?
    HENUMInternal *  pEnum = NULL; // Enum we're working with.
    CMiniMdRW *      pMiniMd = &(m_pStgdb->m_MiniMd);

    LOG((LOGMD, "MD RegMeta::EnumUnresolvedMethods(0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, rMethods, cMax, pcTokens));

    START_MD_PERF();

    // take the write lock. Because we should have not have two EnumUnresolvedMethods being called at the
    // same time. Ref to Def map may be calculated incorrectly.
    LOCKWRITE();

    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM
        MethodRec       *pMethodRec;
        TypeDefRec      *pTypeDefRec;

        // make sure our ref to def optimization is up to date
        IfFailGo( RefToDefOptimization() );
        IfFailGo( HENUMInternal::CreateDynamicArrayEnum( (DWORD) -1, &pEnum) );

        // Loop through all of the methoddef except global functions.
        // If methoddef has RVA 0 and not miRuntime, mdAbstract, mdVirtual, mdNative,
        // we will fill it into the enumerator.
        //
        iCountTypeDef = pMiniMd->getCountTypeDefs();

        for (indexTypeDef = 2; indexTypeDef <= iCountTypeDef; indexTypeDef ++ )
        {
            IfFailGo(pMiniMd->GetTypeDefRecord(indexTypeDef, &pTypeDefRec));

            // If the type is an interface, check the static methods.
            bIsInterface = IsTdInterface(pTypeDefRec->GetFlags());

            ulStart = pMiniMd->getMethodListOfTypeDef(pTypeDefRec);
            IfFailGo(pMiniMd->getEndMethodListOfTypeDef(indexTypeDef, &ulEnd));

            // always report errors even with any unimplemented methods
            for (index = ulStart; index < ulEnd; index++)
            {
                RID methodRid;
                IfFailGo(pMiniMd->GetMethodRid(index, &methodRid));
                IfFailGo(pMiniMd->GetMethodRecord(methodRid, &pMethodRec));

                // If the type is an interface, and the method is not static, on to next.
                if (bIsInterface && !IsMdStatic(pMethodRec->GetFlags()))
                    continue;

                if ( IsMiForwardRef(pMethodRec->GetImplFlags()) )
                {
                    if ( IsMdPinvokeImpl(pMethodRec->GetFlags()) )
                    {
                        continue;
                    }
                    if ( IsMiRuntime(pMethodRec->GetImplFlags()) || IsMiInternalCall(pMethodRec->GetImplFlags()))
                    {
                        continue;
                    }

                    if (IsMdAbstract(pMethodRec->GetFlags()))
                        continue;

                    // If a methoddef has RVA 0 and it is not an abstract or virtual method.
                    // Nor it is a runtime generated method nore a native method, then we add it
                    // to the unresolved list.
                    //
                    IfFailGo(pMiniMd->GetMethodRid(index, &methodRid));
                    IfFailGo(HENUMInternal::AddElementToEnum(
                        pEnum,
                        TokenFromRid(methodRid, mdtMethodDef)));

                    LOG((LOGMD, "MD   adding unresolved MethodDef:  token=%08x, flags=%08x, impl flags=%08x\n",
                        TokenFromRid(methodRid, mdtMethodDef),
                        pMethodRec->GetFlags(), pMethodRec->GetImplFlags()));
                }
            }
        }

        MemberRefRec    *pMemberRefRec;
        ULONG           iCount;

        // loop through MemberRef tables and find all of the unsats
        iCount = pMiniMd->getCountMemberRefs();
        for (index = 1; index <= iCount; index++ )
        {
            mdToken     defToken;
            mdMemberRef refToken = TokenFromRid(index, mdtMemberRef);
            IfFailGo(pMiniMd->GetMemberRefRecord(index, &pMemberRefRec));
            pMiniMd->GetTokenRemapManager()->ResolveRefToDef(refToken, &defToken);

            if ( pMiniMd->getClassOfMemberRef(pMemberRefRec) == m_tdModule && defToken == refToken )
            {
                // unresovled externals reference if parent token is not resolved and this ref token does not
                // map to any def token (can be MethodDef or FieldDef).
                //
                IfFailGo( HENUMInternal::AddElementToEnum(pEnum, refToken) );

                LOG((LOGMD, "MD   adding unresolved MemberRef:  token=%08x, doesn't have a proper parent\n",
                    refToken ));
            }
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rMethods, pcTokens);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumUnresolvedMethods);
    END_ENTRYPOINT_NOTHROW;

    return hr;
#else //!FEATURE_METADATA_EMIT
    return E_NOTIMPL;
#endif //!FEATURE_METADATA_EMIT
} // RegMeta::EnumUnresolvedMethods

//*****************************************************************************
// Return the User string given the token.  The offset into the Blob pool where
// the string is stored in Unicode is embedded inside the token.
//*****************************************************************************
STDMETHODIMP RegMeta::GetUserString(          // S_OK or error.
                                    mdString stk,               // [IN] String token.
    __out_ecount_opt(cchStringSize) LPWSTR   wszString,         // [OUT] Copy of string.
                                    ULONG    cchStringSize,     // [IN] Max chars of room in szString.
                                    ULONG   *pcchStringSize)    // [OUT] How many chars in actual string.
{
    HRESULT hr = S_OK;
    ULONG   cchStringSize_Dummy;
    MetaData::DataBlob userString;

    BEGIN_ENTRYPOINT_NOTHROW;

    LOG((LOGMD, "MD RegMeta::GetUserString(0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        stk, wszString, cchStringSize, pcchStringSize));

    START_MD_PERF();
    LOCKREAD();

    // Get the string data.
    IfFailGo(m_pStgdb->m_MiniMd.GetUserString(RidFromToken(stk), &userString));
    // Want to get whole characters, followed by byte to indicate whether there
    // are extended characters (>= 0x80).
    if ((userString.GetSize() % sizeof(WCHAR)) == 0)
    {
        Debug_ReportError("User strings should have 1 byte terminator (either 0x00 or 0x80).");
        IfFailGo(CLDB_E_FILE_CORRUPT);
    }

    // Strip off the last byte.
    if (!userString.TruncateBySize(1))
    {
        Debug_ReportInternalError("There's a bug, because previous % 2 check didn't return 0.");
        IfFailGo(METADATA_E_INTERNAL_ERROR);
    }

    // Convert bytes to characters.
    if (pcchStringSize == NULL)
    {
        pcchStringSize = &cchStringSize_Dummy;
    }
    *pcchStringSize = userString.GetSize() / sizeof(WCHAR);

    // Copy the string back to the caller.
    if ((wszString != NULL) && (cchStringSize > 0))
    {
        ULONG cbStringSize = cchStringSize * sizeof(WCHAR);
        memcpy(
            wszString,
            userString.GetDataPointer(),
            min(userString.GetSize(), cbStringSize));
        if (cbStringSize < userString.GetSize())
        {
            if ((wszString != NULL) && (cchStringSize > 0))
            {   // null-terminate the truncated output string
                wszString[cchStringSize - 1] = W('\0');
            }

            hr = CLDB_S_TRUNCATION;
        }
    }

 ErrExit:
    STOP_MD_PERF(GetUserString);
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetUserString

//*****************************************************************************
// Return contents of Pinvoke given the forwarded member token.
//*****************************************************************************
STDMETHODIMP RegMeta::GetPinvokeMap(          // S_OK or error.
    mdToken     tk,                     // [IN] FieldDef or MethodDef.
    DWORD       *pdwMappingFlags,       // [OUT] Flags used for mapping.
    __out_ecount_opt (cchImportName) LPWSTR szImportName, // [OUT] Import name.
    ULONG       cchImportName,          // [IN] Size of the name buffer.
    ULONG       *pchImportName,         // [OUT] Actual number of characters stored.
    mdModuleRef *pmrImportDLL)          // [OUT] ModuleRef token for the target DLL.
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    ImplMapRec * pRecord;
    uint32_t     iRecord;

    LOG((LOGMD, "MD RegMeta::GetPinvokeMap(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        tk, pdwMappingFlags, szImportName, cchImportName, pchImportName, pmrImportDLL));

    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(tk) == mdtFieldDef ||
             TypeFromToken(tk) == mdtMethodDef);

    IfFailGo(m_pStgdb->m_MiniMd.FindImplMapHelper(tk, &iRecord));
    if (InvalidRid(iRecord))
    {
        IfFailGo( CLDB_E_RECORD_NOTFOUND );
    }
    else
        IfFailGo(m_pStgdb->m_MiniMd.GetImplMapRecord(iRecord, &pRecord));

    if (pdwMappingFlags)
        *pdwMappingFlags = m_pStgdb->m_MiniMd.getMappingFlagsOfImplMap(pRecord);
    if (pmrImportDLL)
        *pmrImportDLL = m_pStgdb->m_MiniMd.getImportScopeOfImplMap(pRecord);
    // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten with S_OK
    if (szImportName || pchImportName)
        IfFailGo(m_pStgdb->m_MiniMd.getImportNameOfImplMap(pRecord, szImportName, cchImportName, pchImportName));
ErrExit:
    STOP_MD_PERF(GetPinvokeMap);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // HRESULT RegMeta::GetPinvokeMap()

//*****************************************************************************
// Enumerate through all the local sigs.
//*****************************************************************************
STDMETHODIMP RegMeta::EnumSignatures(         // S_OK or error.
    HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
    mdModuleRef rSignatures[],          // [OUT] put signatures here.
    ULONG       cmax,                   // [IN] max signatures to put.
    ULONG       *pcSignatures)          // [OUT] put # put here.
{
    HRESULT hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal **ppsigEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    HENUMInternal  *pEnum;

    LOG((LOGMD, "MD RegMeta::EnumSignatures(0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, rSignatures, cmax, pcSignatures));

    START_MD_PERF();
    LOCKREAD();

    if (*ppsigEnum == NULL)
    {
        // instantiating a new ENUM
        CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

        // create the enumerator.
        IfFailGo(HENUMInternal::CreateSimpleEnum(
            mdtSignature,
            1,
            pMiniMd->getCountStandAloneSigs() + 1,
            &pEnum));

        // set the output parameter
        *ppsigEnum = pEnum;
    }
    else
    {
        pEnum = *ppsigEnum;
    }

    // we can only fill the minimum of what caller asked for or what we have left.
    IfFailGo(HENUMInternal::EnumWithCount(pEnum, cmax, rSignatures, pcSignatures));

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppsigEnum);

    STOP_MD_PERF(EnumSignatures);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}   // RegMeta::EnumSignatures


//*****************************************************************************
// Enumerate through all the TypeSpec
//*****************************************************************************
STDMETHODIMP RegMeta::EnumTypeSpecs(          // S_OK or error.
    HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
    mdTypeSpec  rTypeSpecs[],           // [OUT] put TypeSpecs here.
    ULONG       cmax,                   // [IN] max TypeSpecs to put.
    ULONG       *pcTypeSpecs)           // [OUT] put # put here.
{
    HRESULT hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal   **ppEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    HENUMInternal   *pEnum;

    LOG((LOGMD, "MD RegMeta::EnumTypeSpecs(0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, rTypeSpecs, cmax, pcTypeSpecs));

    START_MD_PERF();
    LOCKREAD();

    if (*ppEnum == NULL)
    {
        // instantiating a new ENUM
        CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);

        // create the enumerator.
        IfFailGo(HENUMInternal::CreateSimpleEnum(
            mdtTypeSpec,
            1,
            pMiniMd->getCountTypeSpecs() + 1,
            &pEnum));

        // set the output parameter
        *ppEnum = pEnum;
    }
    else
    {
        pEnum = *ppEnum;
    }

    // we can only fill the minimum of what caller asked for or what we have left.
    IfFailGo(HENUMInternal::EnumWithCount(pEnum, cmax, rTypeSpecs, pcTypeSpecs));

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppEnum);

    STOP_MD_PERF(EnumTypeSpecs);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}   // RegMeta::EnumTypeSpecs


//*****************************************************************************
// Enumerate through all the User Strings.
//*****************************************************************************
STDMETHODIMP RegMeta::EnumUserStrings(        // S_OK or error.
    HCORENUM    *phEnum,                // [IN/OUT] pointer to the enum.
    mdString    rStrings[],             // [OUT] put Strings here.
    ULONG       cmax,                   // [IN] max Strings to put.
    ULONG       *pcStrings)             // [OUT] put # put here.
{
    HRESULT hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal **ppEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    HENUMInternal  *pEnum = NULL;

    LOG((LOGMD, "MD RegMeta::EnumUserStrings(0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        phEnum, rStrings, cmax, pcStrings));

    START_MD_PERF();
    LOCKREAD();

    if (*ppEnum == NULL)
    {
        // instantiating a new ENUM.
        CMiniMdRW *pMiniMd = &(m_pStgdb->m_MiniMd);
        IfFailGo(HENUMInternal::CreateDynamicArrayEnum(mdtString, &pEnum));

        // Add all strings to the dynamic array
        for (UINT32 nIndex = 0; ;)
        {
            MetaData::DataBlob userString;
            UINT32 nNextIndex;
            hr = pMiniMd->GetUserStringAndNextIndex(
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
            // Add the user string into dynamic array
            IfFailGo(HENUMInternal::AddElementToEnum(
                pEnum,
                TokenFromRid(nIndex, mdtString)));

            // Process next user string in the heap
            nIndex = nNextIndex;
        }

        // set the output parameter.
        *ppEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer.
    hr = HENUMInternal::EnumWithCount(*ppEnum, cmax, rStrings, pcStrings);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumUserStrings);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}   // RegMeta::EnumUserStrings


//*****************************************************************************
// This routine gets the param token given a method and index of the parameter.
//*****************************************************************************
STDMETHODIMP RegMeta::GetParamForMethodIndex( // S_OK or error.
    mdMethodDef md,                     // [IN] Method token.
    ULONG       ulParamSeq,             // [IN] Parameter sequence.
    mdParamDef  *ppd)                   // [IN] Put Param token here.
{
    HRESULT     hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;


    LOG((LOGMD, "MD RegMeta::GetParamForMethodIndex(0x%08x, 0x%08x, 0x%08x)\n",
        md, ulParamSeq, ppd));

    START_MD_PERF();
    LOCKREAD();

    _ASSERTE((TypeFromToken(md) == mdtMethodDef) && (ulParamSeq != UINT32_MAX) && (ppd != NULL));

    IfFailGo(_FindParamOfMethod(md, ulParamSeq, ppd));
ErrExit:

    STOP_MD_PERF(GetParamForMethodIndex);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}   // RegMeta::GetParamForMethodIndex()

//*****************************************************************************
// Return the property of a MethodDef or a FieldDef
//*****************************************************************************
HRESULT RegMeta::GetMemberProps(
    mdToken     mb,                     // The member for which to get props.
    mdTypeDef   *pClass,                // Put member's class here.
    __out_ecount_opt (cchMember) LPWSTR szMember, // Put member's name here.
    ULONG       cchMember,              // Size of szMember buffer in wide chars.
    ULONG       *pchMember,             // Put actual size here
    DWORD       *pdwAttr,               // Put flags here.
    PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
    ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
    ULONG       *pulCodeRVA,            // [OUT] codeRVA
    DWORD       *pdwImplFlags,          // [OUT] Impl. Flags
    DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*
    UVCP_CONSTANT *ppValue,             // [OUT] constant value
    ULONG       *pchValue)              // [OUT] size of constant value, string only, wide chars
{
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    LOG((LOGMD, "MD RegMeta::GetMemberProps(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        mb, pClass, szMember, cchMember, pchMember, pdwAttr, ppvSigBlob, pcbSigBlob,
        pulCodeRVA, pdwImplFlags, pdwCPlusTypeFlag, ppValue, pchValue));



    START_MD_PERF();

    _ASSERTE(TypeFromToken(mb) == mdtMethodDef || TypeFromToken(mb) == mdtFieldDef);

    // No need to lock this function. It is calling public APIs. Keep it that way.

    if (TypeFromToken(mb) == mdtMethodDef)
    {
        // It is a Method
        IfFailGo( GetMethodProps(
            mb,
            pClass,
            szMember,
            cchMember,
            pchMember,
            pdwAttr,
            ppvSigBlob,
            pcbSigBlob,
            pulCodeRVA,
            pdwImplFlags) );
    }
    else
    {
        // It is a Field
        IfFailGo( GetFieldProps(
            mb,
            pClass,
            szMember,
            cchMember,
            pchMember,
            pdwAttr,
            ppvSigBlob,
            pcbSigBlob,
            pdwCPlusTypeFlag,
            ppValue,
            pchValue) );
    }
ErrExit:
    STOP_MD_PERF(GetMemberProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // HRESULT RegMeta::GetMemberProps()

//*****************************************************************************
// Return the property of a FieldDef
//*****************************************************************************
HRESULT RegMeta::GetFieldProps(
    mdFieldDef  fd,                     // The field for which to get props.
    mdTypeDef   *pClass,                // Put field's class here.
    __out_ecount_opt (cchField) LPWSTR szField, // Put field's name here.
    ULONG       cchField,               // Size of szField buffer in wide chars.
    ULONG       *pchField,              // Put actual size here
    DWORD       *pdwAttr,               // Put flags here.
    PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
    ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
    DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*
    UVCP_CONSTANT *ppValue,             // [OUT] constant value
    ULONG       *pchValue)              // [OUT] size of constant value, string only, wide chars
{
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    FieldRec        *pFieldRec;
    CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);

    LOG((LOGMD, "MD RegMeta::GetFieldProps(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        fd, pClass, szField, cchField, pchField, pdwAttr, ppvSigBlob, pcbSigBlob, pdwCPlusTypeFlag,
        ppValue, pchValue));

    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(fd) == mdtFieldDef);

    IfFailGo(pMiniMd->GetFieldRecord(RidFromToken(fd), &pFieldRec));

    if (pClass)
    {
        // caller wants parent typedef
        IfFailGo( pMiniMd->FindParentOfFieldHelper(fd, pClass) );

        if ( IsGlobalMethodParentToken(*pClass) )
        {
            // If the parent of Field is the <Module>, return mdTypeDefNil instead.
            *pClass = mdTypeDefNil;
        }
    }
    if (ppvSigBlob || pcbSigBlob)
    {
        // caller wants signature information
        PCCOR_SIGNATURE pvSigTmp;
        ULONG           cbSig;
        IfFailGo(pMiniMd->getSignatureOfField(pFieldRec, &pvSigTmp, &cbSig));
        if ( ppvSigBlob )
            *ppvSigBlob = pvSigTmp;
        if ( pcbSigBlob)
            *pcbSigBlob = cbSig;
    }
    if ( pdwAttr )
    {
        *pdwAttr = pMiniMd->getFlagsOfField(pFieldRec);
    }
    if ( pdwCPlusTypeFlag || ppValue || pchValue)
    {
        // get the constant value
        ULONG   cbValue;
        RID rid;
        IfFailGo(pMiniMd->FindConstantHelper(fd, &rid));

        if (pchValue)
            *pchValue = 0;

        if (InvalidRid(rid))
        {
            // There is no constant value associate with it
            if (pdwCPlusTypeFlag)
                *pdwCPlusTypeFlag = ELEMENT_TYPE_VOID;

            if ( ppValue )
                *ppValue = NULL;
        }
        else
        {
            ConstantRec *pConstantRec;
            IfFailGo(m_pStgdb->m_MiniMd.GetConstantRecord(rid, &pConstantRec));
            DWORD dwType;

            // get the type of constant value
            dwType = pMiniMd->getTypeOfConstant(pConstantRec);
            if ( pdwCPlusTypeFlag )
                *pdwCPlusTypeFlag = dwType;

            // get the value blob
            if (ppValue != NULL)
            {
                IfFailGo(pMiniMd->getValueOfConstant(pConstantRec, (const BYTE **)ppValue, &cbValue));
                if (pchValue && dwType == ELEMENT_TYPE_STRING)
                    *pchValue = cbValue / sizeof(WCHAR);
            }
        }
    }
    // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten with S_OK
    if (szField || pchField)
    {
        IfFailGo( pMiniMd->getNameOfField(pFieldRec, szField, cchField, pchField) );
    }

ErrExit:
    STOP_MD_PERF(GetFieldProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // HRESULT RegMeta::GetFieldProps()

//*****************************************************************************
// return the properties of a property token
//*****************************************************************************
HRESULT RegMeta::GetPropertyProps(      // S_OK, S_FALSE, or error.
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
    ULONG       *pchDefaultValue,       // [OUT] size of constant value, string only, wide chars
    mdMethodDef *pmdSetter,             // [OUT] setter method of the property
    mdMethodDef *pmdGetter,             // [OUT] getter method of the property
    mdMethodDef rmdOtherMethod[],       // [OUT] other method of the property
    ULONG       cMax,                   // [IN] size of rmdOtherMethod
    ULONG       *pcOtherMethod)         // [OUT] total number of other method of this property
{
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW       *pMiniMd;
    PropertyRec     *pRec;
    HENUMInternal   hEnum;

    LOG((LOGMD, "MD RegMeta::GetPropertyProps(0x%08x, 0x%08x, %S, 0x%08x, 0x%08x, "
                "0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, "
                "0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, "
                "0x%08x)\n",
                prop, pClass, MDSTR(szProperty),  cchProperty, pchProperty,
                pdwPropFlags, ppvSig, pbSig, pdwCPlusTypeFlag, ppDefaultValue,
                pchDefaultValue, pmdSetter, pmdGetter, rmdOtherMethod, cMax,
                pcOtherMethod));



    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(prop) == mdtProperty);

    pMiniMd = &(m_pStgdb->m_MiniMd);

    HENUMInternal::ZeroEnum(&hEnum);
    IfFailGo(pMiniMd->GetPropertyRecord(RidFromToken(prop), &pRec));

    if ( pClass )
    {
        // find the property map entry corresponding to this property
        IfFailGo( pMiniMd->FindParentOfPropertyHelper( prop, pClass) );
    }
    if ( pdwPropFlags )
    {
        *pdwPropFlags = pMiniMd->getPropFlagsOfProperty(pRec);
    }
    if ( ppvSig || pbSig )
    {
        // caller wants the signature
        //
        ULONG               cbSig;
        PCCOR_SIGNATURE     pvSig;
        IfFailGo(pMiniMd->getTypeOfProperty(pRec, &pvSig, &cbSig));
        if ( ppvSig )
        {
            *ppvSig = pvSig;
        }
        if ( pbSig )
        {
            *pbSig = cbSig;
        }
    }
    if ( pdwCPlusTypeFlag || ppDefaultValue || pchDefaultValue)
    {
        // get the constant value
        ULONG   cbValue;
        RID rid;
        IfFailGo(pMiniMd->FindConstantHelper(prop, &rid));

        if (pchDefaultValue)
            *pchDefaultValue = 0;

        if (InvalidRid(rid))
        {
            // There is no constant value associate with it
            if (pdwCPlusTypeFlag)
                *pdwCPlusTypeFlag = ELEMENT_TYPE_VOID;

            if ( ppDefaultValue )
                *ppDefaultValue = NULL;
        }
        else
        {
            ConstantRec *pConstantRec;
            IfFailGo(m_pStgdb->m_MiniMd.GetConstantRecord(rid, &pConstantRec));
            DWORD dwType;

            // get the type of constant value
            dwType = pMiniMd->getTypeOfConstant(pConstantRec);
            if ( pdwCPlusTypeFlag )
                *pdwCPlusTypeFlag = dwType;

            // get the value blob
            if (ppDefaultValue != NULL)
            {
                IfFailGo(pMiniMd->getValueOfConstant(pConstantRec, (const BYTE **)ppDefaultValue, &cbValue));
                if (pchDefaultValue && dwType == ELEMENT_TYPE_STRING)
                    *pchDefaultValue = cbValue / sizeof(WCHAR);
            }
        }
    }
    {
        MethodSemanticsRec *pSemantics;
        RID         ridCur;
        ULONG       cCurOtherMethod = 0;
        ULONG       ulSemantics;
        mdMethodDef tkMethod;

        // initialize output parameters
        if (pmdSetter)
            *pmdSetter = mdMethodDefNil;
        if (pmdGetter)
            *pmdGetter = mdMethodDefNil;

        IfFailGo( pMiniMd->FindMethodSemanticsHelper(prop, &hEnum) );
        while (HENUMInternal::EnumNext(&hEnum, (mdToken *)&ridCur))
        {
            IfFailGo(pMiniMd->GetMethodSemanticsRecord(ridCur, &pSemantics));
            ulSemantics = pMiniMd->getSemanticOfMethodSemantics(pSemantics);
            tkMethod = TokenFromRid( pMiniMd->getMethodOfMethodSemantics(pSemantics), mdtMethodDef );
            switch (ulSemantics)
            {
            case msSetter:
                if (pmdSetter) *pmdSetter = tkMethod;
                break;
            case msGetter:
                if (pmdGetter) *pmdGetter = tkMethod;
                break;
            case msOther:
                if (cCurOtherMethod < cMax)
                    rmdOtherMethod[cCurOtherMethod] = tkMethod;
                cCurOtherMethod ++;
                break;
            default:
                _ASSERTE(!"BadKind!");
            }
        }

        // set the output parameter
        if (pcOtherMethod)
            *pcOtherMethod = cCurOtherMethod;
    }
    // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten with S_OK
    if (szProperty || pchProperty)
    {
        IfFailGo( pMiniMd->getNameOfProperty(pRec, (LPWSTR) szProperty, cchProperty, pchProperty) );
    }

ErrExit:
    HENUMInternal::ClearEnum(&hEnum);
    STOP_MD_PERF(GetPropertyProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // HRESULT RegMeta::GetPropertyProps()


//*****************************************************************************
// This routine gets the properties for the given Param token.
//*****************************************************************************
HRESULT RegMeta::GetParamProps(         // S_OK or error.
    mdParamDef  pd,                     // [IN]The Parameter.
    mdMethodDef *pmd,                   // [OUT] Parent Method token.
    ULONG       *pulSequence,           // [OUT] Parameter sequence.
    __out_ecount_opt (cchName) LPWSTR szName, // [OUT] Put name here.
    ULONG       cchName,                // [OUT] Size of name buffer.
    ULONG       *pchName,               // [OUT] Put actual size of name here.
    DWORD       *pdwAttr,               // [OUT] Put flags here.
    DWORD       *pdwCPlusTypeFlag,      // [OUT] Flag for value type. selected ELEMENT_TYPE_*.
    UVCP_CONSTANT *ppValue,             // [OUT] Constant value.
    ULONG       *pchValue)              // [OUT] size of constant value, string only, wide chars
{
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    ParamRec        *pParamRec;
    CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);

    LOG((LOGMD, "MD RegMeta::GetParamProps(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        pd, pmd, pulSequence, szName, cchName, pchName, pdwAttr, pdwCPlusTypeFlag, ppValue, pchValue));

    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(pd) == mdtParamDef);

    IfFailGo(pMiniMd->GetParamRecord(RidFromToken(pd), &pParamRec));

    if (pmd)
    {
        IfFailGo(pMiniMd->FindParentOfParamHelper(pd, pmd));
        _ASSERTE(TypeFromToken(*pmd) == mdtMethodDef);
    }
    if (pulSequence)
        *pulSequence = pMiniMd->getSequenceOfParam(pParamRec);
    if (pdwAttr)
    {
        *pdwAttr = pMiniMd->getFlagsOfParam(pParamRec);
    }
    if ( pdwCPlusTypeFlag || ppValue || pchValue)
    {
        // get the constant value
        ULONG   cbValue;
        RID rid;
        IfFailGo(pMiniMd->FindConstantHelper(pd, &rid));

        if (pchValue)
            *pchValue = 0;

        if (InvalidRid(rid))
        {
            // There is no constant value associate with it
            if (pdwCPlusTypeFlag)
                *pdwCPlusTypeFlag = ELEMENT_TYPE_VOID;

            if ( ppValue )
                *ppValue = NULL;
        }
        else
        {
            ConstantRec *pConstantRec;
            IfFailGo(m_pStgdb->m_MiniMd.GetConstantRecord(rid, &pConstantRec));
            DWORD dwType;

            // get the type of constant value
            dwType = pMiniMd->getTypeOfConstant(pConstantRec);
            if ( pdwCPlusTypeFlag )
                *pdwCPlusTypeFlag = dwType;

            // get the value blob
            if (ppValue != NULL)
            {
                IfFailGo(pMiniMd->getValueOfConstant(pConstantRec, (const BYTE **)ppValue, &cbValue));
                if (pchValue && dwType == ELEMENT_TYPE_STRING)
                    *pchValue = cbValue / sizeof(WCHAR);
            }
        }
    }
    // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten with S_OK
    if (szName || pchName)
        IfFailGo( pMiniMd->getNameOfParam(pParamRec, szName, cchName, pchName) );

ErrExit:
    STOP_MD_PERF(GetParamProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // HRESULT RegMeta::GetParamProps()

//*****************************************************************************
// This routine gets the properties for the given GenericParam token.
//*****************************************************************************
HRESULT RegMeta::GetGenericParamProps(        // S_OK or error.
        mdGenericParam rd,                  // [IN] The type parameter
        ULONG* pulSequence,                 // [OUT] Parameter sequence number
        DWORD* pdwAttr,                     // [OUT] Type parameter flags (for future use)
        mdToken *ptOwner,                   // [OUT] The owner (TypeDef or MethodDef)
        DWORD *reserved,                    // [OUT] The kind (TypeDef/Ref/Spec, for future use)
        __out_ecount_opt (cchName) LPWSTR szName, // [OUT] The name
        ULONG cchName,                      // [IN] Size of name buffer
        ULONG *pchName)                     // [OUT] Actual size of name
{
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    GenericParamRec  *pGenericParamRec;
    CMiniMdRW       *pMiniMd = NULL;
    RID             ridRD = RidFromToken(rd);


    LOG((LOGMD, "MD RegMeta::GetGenericParamProps(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        rd, pulSequence, pdwAttr, ptOwner, reserved, szName, cchName, pchName));

    START_MD_PERF();
    LOCKREAD();

    pMiniMd = &(m_pStgdb->m_MiniMd);

    // See if this version of the metadata can do Generics
    if (!pMiniMd->SupportsGenerics())
        IfFailGo(CLDB_E_INCOMPATIBLE);


    if((TypeFromToken(rd) == mdtGenericParam) && (ridRD != 0))
    {
        IfFailGo(pMiniMd->GetGenericParamRecord(RidFromToken(rd), &pGenericParamRec));

        if (pulSequence)
            *pulSequence = pMiniMd->getNumberOfGenericParam(pGenericParamRec);
        if (pdwAttr)
          *pdwAttr = pMiniMd->getFlagsOfGenericParam(pGenericParamRec);
        if (ptOwner)
          *ptOwner = pMiniMd->getOwnerOfGenericParam(pGenericParamRec);
        // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten with S_OK
        if (pchName || szName)
            IfFailGo(pMiniMd->getNameOfGenericParam(pGenericParamRec, szName, cchName, pchName));
    }
    else
        hr =  META_E_BAD_INPUT_PARAMETER;

ErrExit:
    STOP_MD_PERF(GetGenericParamProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // HRESULT RegMeta::GetGenericParamProps()

//*****************************************************************************
// This routine gets the properties for the given GenericParamConstraint token.
//*****************************************************************************
HRESULT RegMeta::GetGenericParamConstraintProps(      // S_OK or error.
        mdGenericParamConstraint rd,        // [IN] The constraint token
        mdGenericParam *ptGenericParam,     // [OUT] GenericParam that is constrained
        mdToken      *ptkConstraintType)    // [OUT] TypeDef/Ref/Spec constraint
{
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    GenericParamConstraintRec  *pGPCRec;
    CMiniMdRW       *pMiniMd = NULL;
    RID             ridRD = RidFromToken(rd);

    LOG((LOGMD, "MD RegMeta::GetGenericParamConstraintProps(0x%08x, 0x%08x, 0x%08x)\n",
        rd, ptGenericParam, ptkConstraintType));

    START_MD_PERF();
    LOCKREAD();

    pMiniMd = &(m_pStgdb->m_MiniMd);

    // See if this version of the metadata can do Generics
    if (!pMiniMd->SupportsGenerics())
        IfFailGo(CLDB_E_INCOMPATIBLE);


    if((TypeFromToken(rd) == mdtGenericParamConstraint) && (ridRD != 0))
    {
        IfFailGo(pMiniMd->GetGenericParamConstraintRecord(ridRD, &pGPCRec));

        if (ptGenericParam)
            *ptGenericParam = TokenFromRid(pMiniMd->getOwnerOfGenericParamConstraint(pGPCRec),mdtGenericParam);
        if (ptkConstraintType)
            *ptkConstraintType = pMiniMd->getConstraintOfGenericParamConstraint(pGPCRec);
    }
    else
        hr =  META_E_BAD_INPUT_PARAMETER;

ErrExit:
    STOP_MD_PERF(GetGenericParamConstraintProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // HRESULT RegMeta::GetGenericParamConstraintProps()

//*****************************************************************************
// This routine gets the properties for the given MethodSpec token.
//*****************************************************************************
HRESULT RegMeta::GetMethodSpecProps(         // S_OK or error.
        mdMethodSpec mi,           // [IN] The method instantiation
        mdToken *tkParent,                  // [OUT] MethodDef or MemberRef
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob)            // [OUT] actual size of signature blob
{
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    MethodSpecRec  *pMethodSpecRec;
    CMiniMdRW       *pMiniMd = NULL;

    LOG((LOGMD, "MD RegMeta::GetMethodSpecProps(0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        mi, tkParent, ppvSigBlob, pcbSigBlob));
    START_MD_PERF();
    LOCKREAD();

    pMiniMd = &(m_pStgdb->m_MiniMd);


    // See if this version of the metadata can do Generics
    if (!pMiniMd->SupportsGenerics())
        IfFailGo(CLDB_E_INCOMPATIBLE);

    _ASSERTE(TypeFromToken(mi) == mdtMethodSpec && RidFromToken(mi));

    IfFailGo(pMiniMd->GetMethodSpecRecord(RidFromToken(mi), &pMethodSpecRec));

    if (tkParent)
        *tkParent = pMiniMd->getMethodOfMethodSpec(pMethodSpecRec);

    if (ppvSigBlob || pcbSigBlob)
    {
        // caller wants signature information
        PCCOR_SIGNATURE pvSigTmp;
        ULONG           cbSig;
        IfFailGo(pMiniMd->getInstantiationOfMethodSpec(pMethodSpecRec, &pvSigTmp, &cbSig));
        if ( ppvSigBlob )
            *ppvSigBlob = pvSigTmp;
        if ( pcbSigBlob)
            *pcbSigBlob = cbSig;
    }


ErrExit:

    STOP_MD_PERF(GetMethodSpecProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // HRESULT RegMeta::GetMethodSpecProps()

//*****************************************************************************
// This routine gets the type and machine of the PE file the scope is opened on.
//*****************************************************************************
HRESULT RegMeta::GetPEKind(             // S_OK or error.
    DWORD       *pdwPEKind,             // [OUT] The kind of PE (0 - not a PE)
    DWORD       *pdwMachine)            // [OUT] Machine as defined in NT header
{
    HRESULT     hr = NOERROR;
    MAPPINGTYPE mt = MTYPE_NOMAPPING;

    BEGIN_ENTRYPOINT_NOTHROW;

    LOG((LOGMD, "MD RegMeta::GetPEKind(0x%08x, 0x%08x)\n",pdwPEKind,pdwMachine));

    START_MD_PERF();
    LOCKREAD();


    if (m_pStgdb->m_pStgIO != NULL)
        mt = m_pStgdb->m_pStgIO->GetMemoryMappedType();

    hr = m_pStgdb->GetPEKind(mt, pdwPEKind, pdwMachine);

    ErrExit:

    STOP_MD_PERF(GetPEKind);
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // HRESULT RegMeta::GetPEKind()

//*****************************************************************************
// This function gets the "built for" version of a metadata scope.
//  NOTE: if the scope has never been saved, it will not have a built-for
//  version, and an empty string will be returned.
//*****************************************************************************
HRESULT RegMeta::GetVersionString(    // S_OK or error.
    __out_ecount_opt (cchBufSize) LPWSTR pwzBuf, // [OUT] Put version string here.
    DWORD       cchBufSize,             // [in] size of the buffer, in wide chars
    DWORD       *pchBufSize)            // [out] Size of the version string, wide chars, including terminating nul.
{
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;
    REGMETA_POSSIBLE_INTERNAL_POINTER_EXPOSED();

    DWORD       cch;                    // Length of WideChar string.
    LPCSTR      pVer;                   // Pointer to version string.

    LOG((LOGMD, "MD RegMeta::GetVersionString(0x%08x, 0x%08x, 0x%08x)\n",pwzBuf,cchBufSize,pchBufSize));

    START_MD_PERF();
    LOCKREAD();

    if (m_pStgdb->m_pvMd != NULL)
    {
        // For convenience, get a pointer to the version string.
        // @todo: get from alternate locations when there is no STOREAGESIGNATURE.
        pVer = reinterpret_cast<const char*>(reinterpret_cast<const STORAGESIGNATURE*>(m_pStgdb->m_pvMd)->pVersion);
        // Attempt to convert into caller's buffer.
        cch = WszMultiByteToWideChar(CP_UTF8,0, pVer,-1, pwzBuf,cchBufSize);
        // Did the string fit?
        if (cch == 0)
        {   // No, didn't fit.  Find out space required.
            cch = WszMultiByteToWideChar(CP_UTF8,0, pVer,-1, pwzBuf,0);
            // NUL terminate string.
            if (cchBufSize > 0)
                pwzBuf[cchBufSize-1] = W('\0');
            // Truncation return code.
            hr = CLDB_S_TRUNCATION;
        }
    }
    else
    {   // No string.
        if (cchBufSize > 0)
            *pwzBuf = W('\0');
        cch = 0;
    }

    if (pchBufSize)
        *pchBufSize = cch;

ErrExit:

    STOP_MD_PERF(GetVersionString);
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // HRESULT RegMeta::GetVersionString()

//*****************************************************************************
// This routine gets the parent class for the nested class.
//*****************************************************************************
HRESULT RegMeta::GetNestedClassProps(   // S_OK or error.
    mdTypeDef   tdNestedClass,          // [IN] NestedClass token.
    mdTypeDef   *ptdEnclosingClass)     // [OUT] EnclosingClass token.
{
    HRESULT         hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    NestedClassRec  *pRecord;
    uint32_t        iRecord;
    CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);


    LOG((LOGMD, "MD RegMeta::GetNestedClassProps(0x%08x, 0x%08x)\n",
        tdNestedClass, ptdEnclosingClass));

    START_MD_PERF();
    LOCKREAD();

    // If not a typedef -- return error.
    if (TypeFromToken(tdNestedClass) != mdtTypeDef)
    {
        IfFailGo(META_E_INVALID_TOKEN_TYPE); // PostError(META_E_INVALID_TOKEN_TYPE, tdNestedClass));
    }

    _ASSERTE(TypeFromToken(tdNestedClass) && !IsNilToken(tdNestedClass) && ptdEnclosingClass);

    IfFailGo(pMiniMd->FindNestedClassHelper(tdNestedClass, &iRecord));

    if (InvalidRid(iRecord))
    {
        hr = CLDB_E_RECORD_NOTFOUND;
        goto ErrExit;
    }

    IfFailGo(pMiniMd->GetNestedClassRecord(iRecord, &pRecord));

    _ASSERTE(tdNestedClass == pMiniMd->getNestedClassOfNestedClass(pRecord));
    *ptdEnclosingClass = pMiniMd->getEnclosingClassOfNestedClass(pRecord);

ErrExit:
    STOP_MD_PERF(GetNestedClassProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // HRESULT RegMeta::GetNestedClassProps()

//*****************************************************************************
// Given a signature, parse it for custom modifier with calling convention.
//*****************************************************************************
HRESULT RegMeta::GetNativeCallConvFromSig( // S_OK or error.
    void const  *pvSig,                 // [IN] Pointer to signature.
    ULONG       cbSig,                  // [IN] Count of signature bytes.
    ULONG       *pCallConv)             // [OUT] Put calling conv here (see CorPinvokemap).
{
    HRESULT     hr = NOERROR;

    BEGIN_ENTRYPOINT_NOTHROW;

    PCCOR_SIGNATURE pvSigBlob = reinterpret_cast<PCCOR_SIGNATURE>(pvSig);
    ULONG       cbTotal = 0;            // total of number bytes for return type + all fixed arguments
    ULONG       cbCur = 0;              // index through the pvSigBlob
    ULONG       cb;
    ULONG       cArg;
    ULONG       cTyArg = 0;
    ULONG       callingconv;
    ULONG       cArgsIndex;
    ULONG       callConv = pmCallConvWinapi;  // The calling convention.




    *pCallConv = pmCallConvWinapi;

    // remember the number of bytes to represent the calling convention
    cb = CorSigUncompressData (pvSigBlob, &callingconv);
    if (cb == ((ULONG)(-1)))
    {
        hr = CORSEC_E_INVALID_IMAGE_FORMAT;
        goto ErrExit;
    }
    cbCur += cb;

    // remember the number of bytes to represent the type parameter count
    if (callingconv & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
      cb= CorSigUncompressData (&pvSigBlob[cbCur], &cTyArg);
      if (cb == ((ULONG)(-1)))
      {
          hr = CORSEC_E_INVALID_IMAGE_FORMAT;
          goto ErrExit;
      }
      cbCur += cb;
    }


    // remember number of bytes to represent the arg counts
    cb= CorSigUncompressData (&pvSigBlob[cbCur], &cArg);
    if (cb == ((ULONG)(-1)))
    {
        hr = CORSEC_E_INVALID_IMAGE_FORMAT;
        goto ErrExit;
    }

    cbCur += cb;

    // Look at the return type.
    hr = _SearchOneArgForCallConv( &pvSigBlob[cbCur], &cb, &callConv);
    if (hr == (HRESULT)-1)
    {
        *pCallConv = callConv;
        hr = S_OK;
        goto ErrExit;
    }
    IfFailGo(hr);
    cbCur += cb;
    cbTotal += cb;

    // loop through argument until we found ELEMENT_TYPE_SENTINEL or run
    // out of arguments
    for (cArgsIndex = 0; cArgsIndex < cArg; cArgsIndex++)
    {
        _ASSERTE(cbCur < cbSig);
        hr = _SearchOneArgForCallConv( &pvSigBlob[cbCur], &cb, &callConv);
        if (hr == (HRESULT)-1)
        {
            *pCallConv = callConv;
            hr = S_OK;
            goto ErrExit;
        }
        IfFailGo(hr);
        cbTotal += cb;
        cbCur += cb;
    }

ErrExit:
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // HRESULT RegMeta::GetNativeCallConvFromSig()

//*****************************************************************************
// Helper used by GetNativeCallingConvFromSig.
//*****************************************************************************
HRESULT RegMeta::_CheckCmodForCallConv( // S_OK, -1 if found, or error.
    PCCOR_SIGNATURE pbSig,              // [IN] Signature to check.
    ULONG       *pcbTotal,              // [OUT] Put bytes consumed here.
    ULONG       *pCallConv)             // [OUT] If found, put calling convention here.
{
    ULONG       cbTotal = 0;            // Bytes consumed.
    mdToken     tk;                     // Token for callconv.
    HRESULT     hr = NOERROR;           // A result.
    LPCUTF8     szName=0;               // Callconv name.
    LPCUTF8     szNamespace=0;          // Callconv namespace.
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);

    _ASSERTE(pcbTotal);




    // count the bytes for the token compression
    cbTotal += CorSigUncompressToken(&pbSig[cbTotal], &tk);

    // workaround to skip nil tokens and TypeSpec tokens.
    if (IsNilToken(tk) || TypeFromToken(tk) == mdtTypeSpec)
    {
        *pcbTotal = cbTotal;
        goto ErrExit;
    }

    // See if this token is a calling convention.
    if (TypeFromToken(tk) == mdtTypeRef)
    {
        TypeRefRec *pTypeRefRec;
        IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(tk), &pTypeRefRec));
        IfFailGo(pMiniMd->getNameOfTypeRef(pTypeRefRec, &szName));
        IfFailGo(pMiniMd->getNamespaceOfTypeRef(pTypeRefRec, &szNamespace));
    }
    else
    if (TypeFromToken(tk) == mdtTypeDef)
    {
        TypeDefRec *pTypeDefRec;
        IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(tk), &pTypeDefRec));
        IfFailGo(pMiniMd->getNameOfTypeDef(pTypeDefRec, &szName));
        IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTypeDefRec, &szNamespace));
    }

    if ((szNamespace && szName) &&
        (strcmp(szNamespace, CMOD_CALLCONV_NAMESPACE) == 0 ||
         strcmp(szNamespace, CMOD_CALLCONV_NAMESPACE_OLD) == 0) )
    {
        // Set the hr to -1, which is an unspecified 'error'.  This will percolate
        //  back up to the caller, where the 'error' should be recognized.
        hr=(HRESULT)-1;
        if (strcmp(szName, CMOD_CALLCONV_NAME_CDECL) == 0)
            *pCallConv = pmCallConvCdecl;
        else
        if (strcmp(szName, CMOD_CALLCONV_NAME_STDCALL) == 0)
            *pCallConv = pmCallConvStdcall;
        else
        if (strcmp(szName, CMOD_CALLCONV_NAME_THISCALL) == 0)
            *pCallConv = pmCallConvThiscall;
        else
        if (strcmp(szName, CMOD_CALLCONV_NAME_FASTCALL) == 0)
            *pCallConv = pmCallConvFastcall;
        else
            hr = S_OK; // keep looking
        IfFailGo(hr);
    }
    *pcbTotal = cbTotal;

ErrExit:

    return hr;
} // HRESULT RegMeta::_CheckCmodForCallConv()

//*****************************************************************************
// Helper used by GetNativeCallingConvFromSig.
//*****************************************************************************
HRESULT RegMeta::_SearchOneArgForCallConv(// S_OK, -1 if found, or error.
    PCCOR_SIGNATURE pbSig,              // [IN] Signature to check.
    ULONG       *pcbTotal,              // [OUT] Put bytes consumed here.
    ULONG       *pCallConv)             // [OUT] If found, put calling convention here.
{
    ULONG       cb;
    ULONG       cbTotal = 0;
    CorElementType ulElementType;
    ULONG       ulData;
    ULONG       ulTemp;
    int         iData;
    mdToken     tk;
    ULONG       cArg;
    ULONG       callingconv;
    ULONG       cArgsIndex;
    HRESULT     hr = NOERROR;

    _ASSERTE(pcbTotal);

    cbTotal += CorSigUncompressElementType(&pbSig[cbTotal], &ulElementType);
    while (CorIsModifierElementType(ulElementType) || ulElementType == ELEMENT_TYPE_SENTINEL)
    {
        cbTotal += CorSigUncompressElementType(&pbSig[cbTotal], &ulElementType);
    }
    switch (ulElementType)
    {
        case ELEMENT_TYPE_SZARRAY:
            // skip over base type
            IfFailGo( _SearchOneArgForCallConv(&pbSig[cbTotal], &cb, pCallConv) );
            cbTotal += cb;
            break;

        case ELEMENT_TYPE_VAR :
        case ELEMENT_TYPE_MVAR :
        // skip over index
            cbTotal += CorSigUncompressData(&pbSig[cbTotal], &ulData);
            break;

        case ELEMENT_TYPE_GENERICINST :
            // skip over generic type
            IfFailGo( _SearchOneArgForCallConv(&pbSig[cbTotal], &cb, pCallConv) );
            cbTotal += cb;

            // skip over number of parameters
            cbTotal += CorSigUncompressData(&pbSig[cbTotal], &cArg);

            // loop through type parameters
            for (cArgsIndex = 0; cArgsIndex < cArg; cArgsIndex++)
            {
                IfFailGo( _SearchOneArgForCallConv( &pbSig[cbTotal], &cb, pCallConv) );
                cbTotal += cb;
            }

            break;

        case ELEMENT_TYPE_FNPTR:
            cbTotal += CorSigUncompressData (&pbSig[cbTotal], &callingconv);

            // remember number of bytes to represent the arg counts
            cbTotal += CorSigUncompressData (&pbSig[cbTotal], &cArg);

            // how many bytes to represent the return type
            IfFailGo( _SearchOneArgForCallConv( &pbSig[cbTotal], &cb, pCallConv) );
            cbTotal += cb;

            // loop through argument
            for (cArgsIndex = 0; cArgsIndex < cArg; cArgsIndex++)
            {
                IfFailGo( _SearchOneArgForCallConv( &pbSig[cbTotal], &cb, pCallConv) );
                cbTotal += cb;
            }

            break;

        case ELEMENT_TYPE_ARRAY:
            // syntax : ARRAY BaseType <rank> [i size_1... size_i] [j lowerbound_1 ... lowerbound_j]

            // skip over base type
            IfFailGo( _SearchOneArgForCallConv(&pbSig[cbTotal], &cb, pCallConv) );
            cbTotal += cb;

            // Parse for the rank
            cbTotal += CorSigUncompressData(&pbSig[cbTotal], &ulData);

            // if rank == 0, we are done
            if (ulData == 0)
                break;

            // any size of dimension specified?
            cbTotal += CorSigUncompressData(&pbSig[cbTotal], &ulData);
            while (ulData--)
            {
                cbTotal += CorSigUncompressData(&pbSig[cbTotal], &ulTemp);
            }

            // any lower bound specified?
            cbTotal = CorSigUncompressData(&pbSig[cbTotal], &ulData);

            while (ulData--)
            {
                cbTotal += CorSigUncompressSignedInt(&pbSig[cbTotal], &iData);
            }

            break;
        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_CLASS:
            // count the bytes for the token compression
            cbTotal += CorSigUncompressToken(&pbSig[cbTotal], &tk);
            break;
        case ELEMENT_TYPE_CMOD_REQD:
        case ELEMENT_TYPE_CMOD_OPT:
            // Check for the calling convention.
            IfFailGo(_CheckCmodForCallConv(&pbSig[cbTotal], &cb, pCallConv));
            cbTotal += cb;
            // skip over base type
            IfFailGo( _SearchOneArgForCallConv(&pbSig[cbTotal], &cb, pCallConv) );
            cbTotal += cb;
            break;
        default:
            break;
    }
    *pcbTotal = cbTotal;

ErrExit:

    return hr;
} // HRESULT RegMeta::_SearchOneArgForCallConv()
