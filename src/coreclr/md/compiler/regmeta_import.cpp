// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: RegMeta_IMetaDataImport.cpp
//

//
// Methods of code:RegMeta class which implement public API interfaces:
//  * code:IMetaDataImport
//  * code:IMetaDataImport2
//
// ======================================================================================

#include "stdafx.h"
#include "regmeta.h"
#include "metadata.h"
#include "corerror.h"
#include "mdutil.h"
#include "rwutil.h"
#include "mdlog.h"
#include "importhelper.h"
#include "filtermanager.h"
#include "mdperf.h"
#include "switches.h"
#include "posterror.h"
#include "stgio.h"
#include "sstring.h"

#include <metamodelrw.h>

#define DEFINE_CUSTOM_NODUPCHECK    1
#define DEFINE_CUSTOM_DUPCHECK      2
#define SET_CUSTOM                  3

#if defined(_DEBUG) && defined(_TRACE_REMAPS)
#define LOGGING
#endif
#include <log.h>

#ifdef _MSC_VER
#pragma warning(disable: 4102)
#endif

//*****************************************************************************
// determine if a token is valid or not
//*****************************************************************************
BOOL RegMeta::IsValidToken(             // true if tk is valid token
    mdToken     tk)                     // [IN] token to be checked
{
    BOOL fRet = FALSE;
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_VOIDRET;

    LOCKREADNORET();

    // If acquiring the lock failed...
    IfFailGo(hr);

    fRet = m_pStgdb->m_MiniMd._IsValidToken(tk);

ErrExit:
    END_ENTRYPOINT_VOIDRET;
    return fRet;
} // RegMeta::IsValidToken

//*****************************************************************************
// Close an enumerator.
//*****************************************************************************
void STDMETHODCALLTYPE RegMeta::CloseEnum(
    HCORENUM        hEnum)          // The enumerator.
{
    BEGIN_CLEANUP_ENTRYPOINT;

    LOG((LOGMD, "RegMeta::CloseEnum(0x%08x)\n", hEnum));

    // No need to lock this function.
    HENUMInternal   *pmdEnum = reinterpret_cast<HENUMInternal *> (hEnum);

    if (pmdEnum == NULL)
        return;

    HENUMInternal::DestroyEnum(pmdEnum);
    END_CLEANUP_ENTRYPOINT;
} // RegMeta::CloseEnum

//*****************************************************************************
// Query the count of items represented by an enumerator.
//*****************************************************************************
HRESULT CountEnum(
    HCORENUM        hEnum,              // The enumerator.
    ULONG           *pulCount)          // Put the count here.
{
    HENUMInternal   *pmdEnum = reinterpret_cast<HENUMInternal *> (hEnum);
    HRESULT         hr = S_OK;

    // No need to lock this function.

    LOG((LOGMD, "RegMeta::CountEnum(0x%08x, 0x%08x)\n", hEnum, pulCount));
    START_MD_PERF();

    _ASSERTE( pulCount );

    if (pmdEnum == NULL)
    {
        *pulCount = 0;
        goto ErrExit;
    }

    if (pmdEnum->m_tkKind == (TBL_MethodImpl << 24))
    {
        // Number of tokens must always be a multiple of 2.
        _ASSERTE(! (pmdEnum->m_ulCount % 2) );
        // There are two entries in the Enumerator for each MethodImpl.
        *pulCount = pmdEnum->m_ulCount / 2;
    }
    else
        *pulCount = pmdEnum->m_ulCount;
ErrExit:
    STOP_MD_PERF(CountEnum);
    return hr;
} // ::CountEnum

STDMETHODIMP RegMeta::CountEnum(
    HCORENUM        hEnum,              // The enumerator.
    ULONG           *pulCount)          // Put the count here.
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    hr = ::CountEnum(hEnum, pulCount);
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::CountEnum

//*****************************************************************************
// Reset an enumerator to any position within the enumerator.
//*****************************************************************************
STDMETHODIMP RegMeta::ResetEnum(
    HCORENUM        hEnum,              // The enumerator.
    ULONG           ulPos)              // Seek position.
{
    HRESULT         hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal   *pmdEnum = reinterpret_cast<HENUMInternal *> (hEnum);

    // No need to lock this function.

    LOG((LOGMD, "RegMeta::ResetEnum(0x%08x, 0x%08x)\n", hEnum, ulPos));
    START_MD_PERF();

    if (pmdEnum == NULL)
        goto ErrExit;

    pmdEnum->u.m_ulCur = pmdEnum->u.m_ulStart + ulPos;

ErrExit:
    STOP_MD_PERF(ResetEnum);
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::ResetEnum

//*****************************************************************************
// Enumerate Sym.TypeDef.
//*****************************************************************************
STDMETHODIMP RegMeta::EnumTypeDefs(
    HCORENUM    *phEnum,                // Pointer to the enumerator.
    mdTypeDef   rTypeDefs[],            // Put TypeDefs here.
    ULONG       cMax,                   // Max TypeDefs to put.
    ULONG       *pcTypeDefs)            // Put # put here.
{
    HRESULT         hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal   **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    HENUMInternal   *pEnum = NULL;

    LOG((LOGMD, "RegMeta::EnumTypeDefs(0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
            phEnum, rTypeDefs, cMax, pcTypeDefs));
    START_MD_PERF();
    LOCKREAD();


    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);

        if (pMiniMd->HasDelete() &&
            ((m_OptionValue.m_ImportOption & MDImportOptionAllTypeDefs) == 0))
        {
            IfFailGo( HENUMInternal::CreateDynamicArrayEnum( mdtTypeDef, &pEnum) );

            // add all Types to the dynamic array if name is not _Delete
            for (ULONG index = 2; index <= pMiniMd->getCountTypeDefs(); index ++ )
            {
                TypeDefRec *pRec;
                IfFailGo(pMiniMd->GetTypeDefRecord(index, &pRec));
                LPCSTR szTypeDefName;
                IfFailGo(pMiniMd->getNameOfTypeDef(pRec, &szTypeDefName));
                if (IsDeletedName(szTypeDefName))
                {
                    continue;
                }
                IfFailGo( HENUMInternal::AddElementToEnum(pEnum, TokenFromRid(index, mdtTypeDef) ) );
            }
        }
        else
        {
            // create the enumerator
            IfFailGo( HENUMInternal::CreateSimpleEnum(
                mdtTypeDef,
                2,
                pMiniMd->getCountTypeDefs() + 1,
                &pEnum) );
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // we can only fill the minimum of what caller asked for or what we have left
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rTypeDefs, pcTypeDefs);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumTypeDefs);

    END_ENTRYPOINT_NOTHROW;

    return hr;
} // RegMeta::EnumTypeDefs

//*****************************************************************************
// Enumerate Sym.InterfaceImpl where Coclass == td
//*****************************************************************************
STDMETHODIMP RegMeta::EnumInterfaceImpls(
    HCORENUM        *phEnum,            // Pointer to the enum.
    mdTypeDef       td,                 // TypeDef to scope the enumeration.
    mdInterfaceImpl rImpls[],           // Put InterfaceImpls here.
    ULONG           cMax,               // Max InterfaceImpls to put.
    ULONG           *pcImpls)           // Put # put here.
{
    HRESULT             hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    RID                 ridStart;
    RID                 ridEnd;
    HENUMInternal       *pEnum = NULL;
    InterfaceImplRec    *pRec;
    RID                 index;

    LOG((LOGMD, "RegMeta::EnumInterfaceImpls(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
            phEnum, td, rImpls, cMax, pcImpls));
    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(td) == mdtTypeDef);


    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);
        if ( pMiniMd->IsSorted( TBL_InterfaceImpl ) )
        {
            IfFailGo(pMiniMd->getInterfaceImplsForTypeDef(RidFromToken(td), &ridEnd, &ridStart));
            IfFailGo( HENUMInternal::CreateSimpleEnum( mdtInterfaceImpl, ridStart, ridEnd, &pEnum) );
        }
        else
        {
            // table is not sorted so we have to create dynmaic array
            // create the dynamic enumerator
            //
            ridStart = 1;
            ridEnd = pMiniMd->getCountInterfaceImpls() + 1;

            IfFailGo( HENUMInternal::CreateDynamicArrayEnum( mdtInterfaceImpl, &pEnum) );

            for (index = ridStart; index < ridEnd; index ++ )
            {
                IfFailGo(pMiniMd->GetInterfaceImplRecord(index, &pRec));
                if ( td == pMiniMd->getClassOfInterfaceImpl(pRec) )
                {
                    IfFailGo( HENUMInternal::AddElementToEnum(pEnum, TokenFromRid(index, mdtInterfaceImpl) ) );
                }
            }
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rImpls, pcImpls);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumInterfaceImpls);

    END_ENTRYPOINT_NOTHROW;

    return hr;
} // RegMeta::EnumInterfaceImpls

STDMETHODIMP RegMeta::EnumGenericParams(HCORENUM *phEnum, mdToken tkOwner,
        mdGenericParam rTokens[], ULONG cMaxTokens, ULONG *pcTokens)
{
    HRESULT             hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    RID                 ridStart;
    RID                 ridEnd;
    HENUMInternal       *pEnum = NULL;
    GenericParamRec     *pRec;
    RID                 index;
    CMiniMdRW           *pMiniMd = NULL;


    LOG((LOGMD, "RegMeta::EnumGenericParams(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
            phEnum, tkOwner, rTokens, cMaxTokens, pcTokens));
    START_MD_PERF();
    LOCKREAD();

    pMiniMd = &(m_pStgdb->m_MiniMd);


    // See if this version of the metadata can do Generics
    if (!pMiniMd->SupportsGenerics())
    {
        if (pcTokens)
            *pcTokens = 0;
        hr = S_FALSE;
        goto ErrExit;
    }


    _ASSERTE(TypeFromToken(tkOwner) == mdtTypeDef || TypeFromToken(tkOwner) == mdtMethodDef);


    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM

        //@todo GENERICS: review this. Are we expecting a sorted table or not?
        if ( pMiniMd->IsSorted( TBL_GenericParam ) )
        {
            if (TypeFromToken(tkOwner) == mdtTypeDef)
            {
                IfFailGo(pMiniMd->getGenericParamsForTypeDef(RidFromToken(tkOwner), &ridEnd, &ridStart));
            }
            else
            {
                IfFailGo(pMiniMd->getGenericParamsForMethodDef(RidFromToken(tkOwner), &ridEnd, &ridStart));
            }

            IfFailGo( HENUMInternal::CreateSimpleEnum(mdtGenericParam, ridStart, ridEnd, &pEnum) );
        }
        else
        {
            // table is not sorted so we have to create dynamic array
            // create the dynamic enumerator
            //
            ridStart = 1;
            ridEnd = pMiniMd->getCountGenericParams() + 1;

            IfFailGo( HENUMInternal::CreateDynamicArrayEnum(mdtGenericParam, &pEnum) );

            for (index = ridStart; index < ridEnd; index ++ )
            {
                IfFailGo(pMiniMd->GetGenericParamRecord(index, &pRec));
                if ( tkOwner == pMiniMd->getOwnerOfGenericParam(pRec) )
                {
                    IfFailGo( HENUMInternal::AddElementToEnum(pEnum, TokenFromRid(index, mdtGenericParam) ) );
                }
            }
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMaxTokens, rTokens, pcTokens);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumGenericPars);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // RegMeta::EnumGenericParams

STDMETHODIMP RegMeta::EnumMethodSpecs(
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken      tkOwner,               // [IN] MethodDef or MemberRef whose MethodSpecs are requested
        mdMethodSpec rTokens[],             // [OUT] Put MethodSpecs here.
        ULONG       cMaxTokens,             // [IN] Max tokens to put.
        ULONG       *pcTokens)              // [OUT] Put actual count here.
{
    HRESULT             hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    RID                 ridStart;
    RID                 ridEnd;
    HENUMInternal       *pEnum = NULL;
    MethodSpecRec       *pRec;
    RID                 index;
    CMiniMdRW       *pMiniMd = NULL;

    LOG((LOGMD, "RegMeta::EnumMethodSpecs(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
            phEnum, tkOwner, rTokens, cMaxTokens, pcTokens));
    START_MD_PERF();
    LOCKREAD();

    pMiniMd = &(m_pStgdb->m_MiniMd);

    // See if this version of the metadata can do Generics
    if (!pMiniMd->SupportsGenerics())
    {
        if (pcTokens)
            *pcTokens = 0;
        hr = S_FALSE;
        goto ErrExit;
    }


    _ASSERTE(RidFromToken(tkOwner)==0 || TypeFromToken(tkOwner) == mdtMethodDef || TypeFromToken(tkOwner) == mdtMemberRef);


    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM

        if(RidFromToken(tkOwner)==0) // enumerate all MethodSpecs
        {
            ridStart = 1;
            ridEnd = pMiniMd->getCountMethodSpecs() + 1;

            IfFailGo( HENUMInternal::CreateSimpleEnum( mdtMethodSpec, ridStart, ridEnd, &pEnum) );
        }
        else
        {
            //@todo GENERICS: review this. Are we expecting a sorted table or not?
            if ( pMiniMd->IsSorted( TBL_MethodSpec ) )
            {
                if (TypeFromToken(tkOwner) == mdtMemberRef)
                {
                    IfFailGo(pMiniMd->getMethodSpecsForMemberRef(RidFromToken(tkOwner), &ridEnd, &ridStart));
                }
                else
                {
                    IfFailGo(pMiniMd->getMethodSpecsForMethodDef(RidFromToken(tkOwner), &ridEnd, &ridStart));
                }

                IfFailGo( HENUMInternal::CreateSimpleEnum(mdtMethodSpec, ridStart, ridEnd, &pEnum) );
            }
            else
            {
                // table is not sorted so we have to create dynamic array
                // create the dynamic enumerator
                //
                ridStart = 1;
                ridEnd = pMiniMd->getCountMethodSpecs() + 1;

                IfFailGo( HENUMInternal::CreateDynamicArrayEnum(mdtMethodSpec, &pEnum) );

                for (index = ridStart; index < ridEnd; index ++ )
                {
                    IfFailGo(pMiniMd->GetMethodSpecRecord(index, &pRec));
                    if ( tkOwner == pMiniMd->getMethodOfMethodSpec(pRec) )
                    {
                        IfFailGo( HENUMInternal::AddElementToEnum(pEnum, TokenFromRid(index, mdtMethodSpec) ) );
                    }
                }
            }
        }
        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMaxTokens, rTokens, pcTokens);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumMethodSpecs);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::EnumMethodSpecs()

STDMETHODIMP RegMeta::EnumGenericParamConstraints(
    HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
    mdGenericParam tkOwner,             // [IN] GenericParam whose constraints are requested
    mdGenericParamConstraint rTokens[],    // [OUT] Put GenericParamConstraints here.
    ULONG       cMaxTokens,                   // [IN] Max GenericParamConstraints to put.
    ULONG       *pcTokens)              // [OUT] Put # of tokens here.
{
    HRESULT             hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal       **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    RID                 ridStart;
    RID                 ridEnd;
    HENUMInternal       *pEnum = NULL;
    GenericParamConstraintRec     *pRec;
    RID                 index;
    CMiniMdRW       *pMiniMd = NULL;

    LOG((LOGMD, "RegMeta::EnumGenericParamConstraints(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
            phEnum, tkOwner, rTokens, cMaxTokens, pcTokens));
    START_MD_PERF();
    LOCKREAD();

    pMiniMd = &(m_pStgdb->m_MiniMd);


    if(TypeFromToken(tkOwner) != mdtGenericParam)
        IfFailGo(META_E_BAD_INPUT_PARAMETER);

    // See if this version of the metadata can do Generics
    if (!pMiniMd->SupportsGenerics())
    {
        if (pcTokens)
            *pcTokens = 0;
        hr = S_FALSE;
        goto ErrExit;
    }

    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM

        //<TODO> GENERICS: review this. Are we expecting a sorted table or not? </TODO>
        if ( pMiniMd->IsSorted( TBL_GenericParamConstraint ) )
        {
            IfFailGo(pMiniMd->getGenericParamConstraintsForGenericParam(RidFromToken(tkOwner), &ridEnd, &ridStart));
            IfFailGo( HENUMInternal::CreateSimpleEnum(mdtGenericParamConstraint, ridStart, ridEnd, &pEnum) );
        }
        else
        {
            // table is not sorted so we have to create dynamic array
            // create the dynamic enumerator
            //
            ridStart = 1;
            ridEnd = pMiniMd->getCountGenericParamConstraints() + 1;

            IfFailGo( HENUMInternal::CreateDynamicArrayEnum(mdtGenericParamConstraint, &pEnum));

            for (index = ridStart; index < ridEnd; index ++ )
            {
                IfFailGo(pMiniMd->GetGenericParamConstraintRecord(index, &pRec));
                if ( tkOwner == pMiniMd->getOwnerOfGenericParamConstraint(pRec))
                {
                    IfFailGo( HENUMInternal::AddElementToEnum(pEnum, TokenFromRid(index,
                                                                       mdtGenericParamConstraint)));
                }
            }
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMaxTokens, rTokens, pcTokens);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumGenericParamConstraints);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

//*****************************************************************************
// Enumerate Sym.TypeRef
//*****************************************************************************
STDMETHODIMP RegMeta::EnumTypeRefs(
    HCORENUM        *phEnum,            // Pointer to the enumerator.
    mdTypeRef       rTypeRefs[],        // Put TypeRefs here.
    ULONG           cMax,               // Max TypeRefs to put.
    ULONG           *pcTypeRefs)        // Put # put here.
{
    HRESULT         hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal   **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    ULONG           cTotal;
    HENUMInternal   *pEnum = *ppmdEnum;



    LOG((LOGMD, "RegMeta::EnumTypeRefs(0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
            phEnum, rTypeRefs, cMax, pcTypeRefs));
    START_MD_PERF();
    LOCKREAD();

    if ( pEnum == 0 )
    {
        // instantiating a new ENUM
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);
        cTotal = pMiniMd->getCountTypeRefs();

        IfFailGo( HENUMInternal::CreateSimpleEnum( mdtTypeRef, 1, cTotal + 1, &pEnum) );

        // set the output parameter
        *ppmdEnum = pEnum;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(pEnum, cMax, rTypeRefs, pcTypeRefs);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);


    STOP_MD_PERF(EnumTypeRefs);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::EnumTypeRefs()

//*****************************************************************************
// Given a namespace and a class name, return the typedef
//*****************************************************************************
STDMETHODIMP RegMeta::FindTypeDefByName(// S_OK or error.
    LPCWSTR     wzTypeDef,              // [IN] Name of the Type.
    mdToken     tkEnclosingClass,       // [IN] Enclosing class.
    mdTypeDef   *ptd)                   // [OUT] Put the TypeDef token here.
{
    HRESULT     hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW

    LOG((LOGMD, "{%08x} RegMeta::FindTypeDefByName(%S, 0x%08x, 0x%08x)\n",
            this, MDSTR(wzTypeDef), tkEnclosingClass, ptd));
    START_MD_PERF();
    LOCKREAD();


    if (wzTypeDef == NULL)
        IfFailGo(E_INVALIDARG);
    PREFIX_ASSUME(wzTypeDef != NULL);
    LPSTR       szTypeDef;
    UTF8STR(wzTypeDef, szTypeDef);
    LPCSTR      szNamespace;
    LPCSTR      szName;

    _ASSERTE(ptd);
    _ASSERTE(TypeFromToken(tkEnclosingClass) == mdtTypeDef ||
             TypeFromToken(tkEnclosingClass) == mdtTypeRef ||
             IsNilToken(tkEnclosingClass));

    // initialize output parameter
    *ptd = mdTypeDefNil;

    ns::SplitInline(szTypeDef, szNamespace, szName);
    hr = ImportHelper::FindTypeDefByName(&(m_pStgdb->m_MiniMd),
                                        szNamespace,
                                        szName,
                                        tkEnclosingClass,
                                        ptd);
ErrExit:

    STOP_MD_PERF(FindTypeDefByName);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::FindTypeDefByName()

//*****************************************************************************
// Get values from Sym.Module
//*****************************************************************************
STDMETHODIMP RegMeta::GetScopeProps(
    _Out_writes_opt_ (cchName) LPWSTR szName, // Put name here
    ULONG       cchName,                // Size in chars of name buffer
    ULONG       *pchName,               // Put actual length of name here
    GUID        *pmvid)                 // Put MVID here
{
    HRESULT     hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);
    ModuleRec   *pModuleRec;


    LOG((LOGMD, "RegMeta::GetScopeProps(%S, 0x%08x, 0x%08x, 0x%08x)\n",
            MDSTR(szName), cchName, pchName, pmvid));
    START_MD_PERF();
    LOCKREAD();

    // there is only one module record
    IfFailGo(pMiniMd->GetModuleRecord(1, &pModuleRec));

    if (pmvid != NULL)
    {
        IfFailGo(pMiniMd->getMvidOfModule(pModuleRec, pmvid));
    }
    // This call has to be last to set 'hr', so CLDB_S_TRUNCATION is not rewritten with S_OK
    if (szName || pchName)
        IfFailGo( pMiniMd->getNameOfModule(pModuleRec, szName, cchName, pchName) );
ErrExit:

    STOP_MD_PERF(GetScopeProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::GetScopeProps()

//*****************************************************************************
// Get the token for a Scope's (primary) module record.
//*****************************************************************************
STDMETHODIMP RegMeta::GetModuleFromScope(// S_OK.
    mdModule    *pmd)                   // [OUT] Put mdModule token here.
{
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    LOG((LOGMD, "RegMeta::GetModuleFromScope(0x%08x)\n", pmd));
    START_MD_PERF();

    _ASSERTE(pmd);

    // No need to lock this function.

    *pmd = TokenFromRid(1, mdtModule);

    STOP_MD_PERF(GetModuleFromScope);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::GetModuleFromScope()

//*****************************************************************************
// Given a token, is it (or its parent) global?
//*****************************************************************************
HRESULT RegMeta::IsGlobal(              // S_OK ir error.
    mdToken     tk,                     // [IN] Type, Field, or Method token.
    int         *pbGlobal)              // [OUT] Put 1 if global, 0 otherwise.
{
    HRESULT     hr=S_OK;                // A result.

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);
    mdToken     tkParent;               // Parent of field or method.

    LOG((LOGMD, "RegMeta::GetTokenForGlobalType(0x%08x, %08x)\n", tk, pbGlobal));
    //START_MD_PERF();

    // No need to lock this function.

    if (!IsValidToken(tk))
        IfFailGo(E_INVALIDARG);

    switch (TypeFromToken(tk))
    {
    case mdtTypeDef:
        *pbGlobal = IsGlobalMethodParentToken(tk);
        break;

    case mdtFieldDef:
        IfFailGo( pMiniMd->FindParentOfFieldHelper(tk, &tkParent) );
        *pbGlobal = IsGlobalMethodParentToken(tkParent);
        break;

    case mdtMethodDef:
        IfFailGo( pMiniMd->FindParentOfMethodHelper(tk, &tkParent) );
        *pbGlobal = IsGlobalMethodParentToken(tkParent);
        break;

    case mdtProperty:
        IfFailGo( pMiniMd->FindParentOfPropertyHelper(tk, &tkParent) );
        *pbGlobal = IsGlobalMethodParentToken(tkParent);
        break;

    case mdtEvent:
        IfFailGo( pMiniMd->FindParentOfEventHelper(tk, &tkParent) );
        *pbGlobal = IsGlobalMethodParentToken(tkParent);
        break;

    // Anything else is NOT global.
    default:
        *pbGlobal = FALSE;
    }

ErrExit:
    //STOP_MD_PERF(GetModuleFromScope);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // HRESULT RegMeta::IsGlobal()

//*****************************************************************************
// return flags for a given class
//*****************************************************************************
HRESULT
RegMeta::GetTypeDefProps(
    mdTypeDef td,                   // [IN] TypeDef token for inquiry.
    _Out_writes_opt_ (cchTypeDef) LPWSTR szTypeDef, // [OUT] Put name here.
    ULONG     cchTypeDef,           // [IN] size of name buffer in wide chars.
    ULONG    *pchTypeDef,           // [OUT] put size of name (wide chars) here.
    DWORD    *pdwTypeDefFlags,      // [OUT] Put flags here.
    mdToken  *ptkExtends)           // [OUT] Put base class TypeDef/TypeRef here.
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW  *pMiniMd = &(m_pStgdb->m_MiniMd);
    TypeDefRec *pTypeDefRec;
    BOOL        fTruncation = FALSE;    // Was there name truncation?

    LOG((LOGMD, "{%08x} RegMeta::GetTypeDefProps(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
            this, td, szTypeDef, cchTypeDef, pchTypeDef,
            pdwTypeDefFlags, ptkExtends));
    START_MD_PERF();
    LOCKREAD();

    if (TypeFromToken(td) != mdtTypeDef)
    {
        hr = S_FALSE;
        goto ErrExit;
    }
    if (td == mdTypeDefNil)
    {   // Backward compatibility with CLR 2.0 implementation
        if (pdwTypeDefFlags != NULL)
            *pdwTypeDefFlags = 0;
        if (ptkExtends != NULL)
            *ptkExtends = mdTypeRefNil;
        if (pchTypeDef != NULL)
            *pchTypeDef = 1;
        if ((szTypeDef != NULL) && (cchTypeDef > 0))
            szTypeDef[0] = 0;

        hr = S_OK;
        goto ErrExit;
    }

    IfFailGo(pMiniMd->GetTypeDefRecord(RidFromToken(td), &pTypeDefRec));

    if ((szTypeDef != NULL) || (pchTypeDef != NULL))
    {
        LPCSTR szNamespace;
        LPCSTR szName;

        IfFailGo(pMiniMd->getNamespaceOfTypeDef(pTypeDefRec, &szNamespace));
        MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzNamespace, szNamespace);
        IfNullGo(wzNamespace);

        IfFailGo(pMiniMd->getNameOfTypeDef(pTypeDefRec, &szName));
        MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzName, szName);
        IfNullGo(wzName);

        if (szTypeDef != NULL)
        {
            fTruncation = !(ns::MakePath(szTypeDef, cchTypeDef, wzNamespace, wzName));
        }
        if (pchTypeDef != NULL)
        {
            if (fTruncation || (szTypeDef == NULL))
            {
                *pchTypeDef = ns::GetFullLength(wzNamespace, wzName);
            }
            else
            {
                *pchTypeDef = (ULONG)(wcslen(szTypeDef) + 1);
            }
        }
    }
    if (pdwTypeDefFlags != NULL)
    {   // caller wants type flags
        *pdwTypeDefFlags = pMiniMd->getFlagsOfTypeDef(pTypeDefRec);
    }
    if (ptkExtends != NULL)
    {
        *ptkExtends = pMiniMd->getExtendsOfTypeDef(pTypeDefRec);

        // take care of the 0 case
        if (RidFromToken(*ptkExtends) == 0)
        {
            *ptkExtends = mdTypeRefNil;
        }
    }

    if (fTruncation && (hr == S_OK))
    {
        if ((szTypeDef != NULL) && (cchTypeDef > 0))
        {   // null-terminate the truncated output string
            szTypeDef[cchTypeDef - 1] = W('\0');
        }
        hr = CLDB_S_TRUNCATION;
    }

ErrExit:
    END_ENTRYPOINT_NOTHROW;

    STOP_MD_PERF(GetTypeDefProps);

    return hr;
} // RegMeta::GetTypeDefProps

//*****************************************************************************
// Retrieve information about an implemented interface.
//*****************************************************************************
STDMETHODIMP RegMeta::GetInterfaceImplProps(        // S_OK or error.
    mdInterfaceImpl iiImpl,             // [IN] InterfaceImpl token.
    mdTypeDef   *pClass,                // [OUT] Put implementing class token here.
    mdToken     *ptkIface)              // [OUT] Put implemented interface token here.
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW       *pMiniMd = NULL;
    InterfaceImplRec *pIIRec = NULL;



    LOG((LOGMD, "RegMeta::GetInterfaceImplProps(0x%08x, 0x%08x, 0x%08x)\n",
            iiImpl, pClass, ptkIface));
    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(iiImpl) == mdtInterfaceImpl);

    pMiniMd = &(m_pStgdb->m_MiniMd);
    IfFailGo(pMiniMd->GetInterfaceImplRecord(RidFromToken(iiImpl), &pIIRec));

    if (pClass)
    {
        *pClass = pMiniMd->getClassOfInterfaceImpl(pIIRec);
    }
    if (ptkIface)
    {
        *ptkIface = pMiniMd->getInterfaceOfInterfaceImpl(pIIRec);
    }

ErrExit:
    STOP_MD_PERF(GetInterfaceImplProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::GetInterfaceImplProps()

//*****************************************************************************
// Retrieve information about a TypeRef.
//*****************************************************************************
STDMETHODIMP
RegMeta::GetTypeRefProps(
    mdTypeRef tr,                   // The class ref token.
    mdToken  *ptkResolutionScope,   // Resolution scope, ModuleRef or AssemblyRef.
    _Out_writes_opt_ (cchTypeRef) LPWSTR szTypeRef, // Put the name here.
    ULONG     cchTypeRef,           // Size of the name buffer, wide chars.
    ULONG    *pchTypeRef)           // Put actual size of name here.
{
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW  *pMiniMd;
    TypeRefRec *pTypeRefRec;
    BOOL        fTruncation = FALSE;    // Was there name truncation?

    LOG((LOGMD, "RegMeta::GetTypeRefProps(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
        tr, ptkResolutionScope, szTypeRef, cchTypeRef, pchTypeRef));

    START_MD_PERF();
    LOCKREAD();

    if (TypeFromToken(tr) != mdtTypeRef)
    {
        hr = S_FALSE;
        goto ErrExit;
    }
    if (tr == mdTypeRefNil)
    {   // Backward compatibility with CLR 2.0 implementation
        if (ptkResolutionScope != NULL)
            *ptkResolutionScope = mdTokenNil;
        if (pchTypeRef != NULL)
            *pchTypeRef = 1;
        if ((szTypeRef != NULL) && (cchTypeRef > 0))
            szTypeRef[0] = 0;

        hr = S_OK;
        goto ErrExit;
    }

    pMiniMd = &(m_pStgdb->m_MiniMd);
    IfFailGo(pMiniMd->GetTypeRefRecord(RidFromToken(tr), &pTypeRefRec));

    if (ptkResolutionScope != NULL)
    {
        *ptkResolutionScope = pMiniMd->getResolutionScopeOfTypeRef(pTypeRefRec);
    }

    if ((szTypeRef != NULL) || (pchTypeRef != NULL))
    {
        LPCSTR szNamespace;
        LPCSTR szName;

        IfFailGo(pMiniMd->getNamespaceOfTypeRef(pTypeRefRec, &szNamespace));
        MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzNamespace, szNamespace);
        IfNullGo(wzNamespace);

        IfFailGo(pMiniMd->getNameOfTypeRef(pTypeRefRec, &szName));
        MAKE_WIDEPTR_FROMUTF8_NOTHROW(wzName, szName);
        IfNullGo(wzName);

        if (szTypeRef != NULL)
        {
            fTruncation = !(ns::MakePath(szTypeRef, cchTypeRef, wzNamespace, wzName));
        }
        if (pchTypeRef != NULL)
        {
            if (fTruncation || (szTypeRef == NULL))
            {
                *pchTypeRef = ns::GetFullLength(wzNamespace, wzName);
            }
            else
            {
                *pchTypeRef = (ULONG)(wcslen(szTypeRef) + 1);
            }
        }
    }
    if (fTruncation && (hr == S_OK))
    {
        if ((szTypeRef != NULL) && (cchTypeRef > 0))
        {   // null-terminate the truncated output string
            szTypeRef[cchTypeRef - 1] = W('\0');
        }
        hr = CLDB_S_TRUNCATION;
    }

ErrExit:
    STOP_MD_PERF(GetTypeRefProps);
    END_ENTRYPOINT_NOTHROW;
    return hr;
} // RegMeta::GetTypeRefProps

//*****************************************************************************
// Given a TypeRef name, return the typeref
//*****************************************************************************
STDMETHODIMP RegMeta::FindTypeRef(      // S_OK or error.
    mdToken     tkResolutionScope,      // [IN] Resolution Scope.
    LPCWSTR     wzTypeName,             // [IN] Name of the TypeRef.
    mdTypeRef   *ptk)                   // [OUT] Put the TypeRef token here.
{
    HRESULT     hr = S_OK;              // A result.

    BEGIN_ENTRYPOINT_NOTHROW;

    LPUTF8      szFullName;
    LPCUTF8     szNamespace;
    LPCUTF8     szName;
    CMiniMdRW   *pMiniMd = &(m_pStgdb->m_MiniMd);

    _ASSERTE(wzTypeName && ptk);



    LOG((LOGMD, "RegMeta::FindTypeRef(0x%8x, %ls, 0x%08x)\n",
            tkResolutionScope, MDSTR(wzTypeName), ptk));
    START_MD_PERF();
    LOCKREAD();

    // Convert the  name to UTF8.
    PREFIX_ASSUME(wzTypeName != NULL); // caller might pass NULL, but they'll AV.
    UTF8STR(wzTypeName, szFullName);
    ns::SplitInline(szFullName, szNamespace, szName);

    // Look up the name.
    hr = ImportHelper::FindTypeRefByName(pMiniMd, tkResolutionScope,
                                         szNamespace,
                                         szName,
                                         ptk);
ErrExit:

    STOP_MD_PERF(FindTypeRef);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::FindTypeRef()

//*******************************************************************************
// Find a given param of a Method.
//*******************************************************************************
HRESULT RegMeta::_FindParamOfMethod(    // S_OK or error.
    mdMethodDef md,                     // [IN] The owning method of the param.
    ULONG       iSeq,                   // [IN] The sequence # of the param.
    mdParamDef  *pParamDef)             // [OUT] Put ParamDef token here.
{
    HRESULT   hr;
    ParamRec *pParamRec;
    RID       ridStart, ridEnd;
    RID       pmRid;

    _ASSERTE(TypeFromToken(md) == mdtMethodDef && pParamDef);

    // get the methoddef record
    MethodRec *pMethodRec;
    IfFailRet(m_pStgdb->m_MiniMd.GetMethodRecord(RidFromToken(md), &pMethodRec));

    // figure out the start rid and end rid of the parameter list of this methoddef
    ridStart = m_pStgdb->m_MiniMd.getParamListOfMethod(pMethodRec);
    IfFailRet(m_pStgdb->m_MiniMd.getEndParamListOfMethod(RidFromToken(md), &ridEnd));

    // loop through each param
    // <TODO>@consider: parameters are sorted by sequence. Maybe a binary search?
    //</TODO>
    for (; ridStart < ridEnd; ridStart++)
    {
        IfFailRet(m_pStgdb->m_MiniMd.GetParamRid(ridStart, &pmRid));
        IfFailRet(m_pStgdb->m_MiniMd.GetParamRecord(pmRid, &pParamRec));
        if (iSeq == m_pStgdb->m_MiniMd.getSequenceOfParam(pParamRec))
        {
            // parameter has the sequence number matches what we are looking for
            *pParamDef = TokenFromRid(pmRid, mdtParamDef);
            return S_OK;
        }
    }
    return CLDB_E_RECORD_NOTFOUND;
} // HRESULT RegMeta::_FindParamOfMethod()

//*******************************************************************************
// Given the signature, return the token for signature.
//*******************************************************************************
HRESULT RegMeta::_GetTokenFromSig(              // S_OK or error.
    PCCOR_SIGNATURE pvSig,              // [IN] Signature to define.
    ULONG       cbSig,                  // [IN] Size of signature data.
    mdSignature *pmsig)                 // [OUT] returned signature token.
{
    HRESULT     hr = S_OK;

    _ASSERTE(pmsig);

    if (CheckDups(MDDupSignature))
    {
        hr = ImportHelper::FindStandAloneSig(&(m_pStgdb->m_MiniMd), pvSig, cbSig, pmsig);
        if (SUCCEEDED(hr))
        {
            if (IsENCOn())
                return S_OK;
            else
                return META_S_DUPLICATE;
        }
        else if (hr != CLDB_E_RECORD_NOTFOUND)
            IfFailGo(hr);
    }

    // Create a new record.
    StandAloneSigRec *pSigRec;
    RID     iSigRec;

    IfFailGo(m_pStgdb->m_MiniMd.AddStandAloneSigRecord(&pSigRec, &iSigRec));

    // Set output parameter.
    *pmsig = TokenFromRid(iSigRec, mdtSignature);

    // Save signature.
    IfFailGo(m_pStgdb->m_MiniMd.PutBlob(TBL_StandAloneSig, StandAloneSigRec::COL_Signature,
                                pSigRec, pvSig, cbSig));
    IfFailGo(UpdateENCLog(*pmsig));
ErrExit:
    return hr;
} // RegMeta::_GetTokenFromSig
