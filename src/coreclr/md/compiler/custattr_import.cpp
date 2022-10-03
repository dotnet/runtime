// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************

//
// CustAttr_Import.cpp
//
// Implementation for the meta data custom attribute import code (code:IMetaDataImport).
//
//*****************************************************************************
#include "stdafx.h"
#include "regmeta.h"
#include "metadata.h"
#include "corerror.h"
#include "mdutil.h"
#include "rwutil.h"
#include "mdlog.h"
#include "importhelper.h"
#include "mdperf.h"
#include "posterror.h"
#include "cahlprinternal.h"
#include "custattr.h"
#include "corhdr.h"
#include <metamodelrw.h>

//*****************************************************************************
// Implementation of hash for custom attribute types.
//*****************************************************************************
unsigned int CCustAttrHash::Hash(const CCustAttrHashKey *pData)
{
    return static_cast<unsigned int>(pData->tkType);
} // unsigned long CCustAttrHash::Hash()
unsigned int CCustAttrHash::Compare(const CCustAttrHashKey *p1, CCustAttrHashKey *p2)
{
    if (p1->tkType == p2->tkType)
        return 0;
    return 1;
} // unsigned long CCustAttrHash::Compare()
CCustAttrHash::ELEMENTSTATUS CCustAttrHash::Status(CCustAttrHashKey *p)
{
    if (p->tkType == FREE)
        return (FREE);
    if (p->tkType == DELETED)
        return (DELETED);
    return (USED);
} // CCustAttrHash::ELEMENTSTATUS CCustAttrHash::Status()
void CCustAttrHash::SetStatus(CCustAttrHashKey *p, CCustAttrHash::ELEMENTSTATUS s)
{
    p->tkType = s;
} // void CCustAttrHash::SetStatus()
void* CCustAttrHash::GetKey(CCustAttrHashKey *p)
{
    return &p->tkType;
} // void* CCustAttrHash::GetKey()


//*****************************************************************************
// Get the value of a CustomAttribute, using only TypeName for lookup.
//*****************************************************************************
STDMETHODIMP RegMeta::GetCustomAttributeByName( // S_OK or error.
    mdToken     tkObj,                  // [IN] Object with Custom Attribute.
    LPCWSTR     wzName,                 // [IN] Name of desired Custom Attribute.
    const void  **ppData,               // [OUT] Put pointer to data here.
    ULONG       *pcbData)               // [OUT] Put size of data here.
{
    HRESULT     hr;                     // A result.

    BEGIN_ENTRYPOINT_NOTHROW;

    LPUTF8      szName;                 // Name in UFT8.
    int         iLen;                   // A length.
    CMiniMdRW   *pMiniMd = NULL;

    START_MD_PERF();
    LOCKREAD();
    pMiniMd = &(m_pStgdb->m_MiniMd);

    iLen = WszWideCharToMultiByte(CP_UTF8,0, wzName,-1, NULL,0, 0,0);
    szName = (LPUTF8)_alloca(iLen);
    VERIFY(WszWideCharToMultiByte(CP_UTF8,0, wzName,-1, szName,iLen, 0,0));

    hr = ImportHelper::GetCustomAttributeByName(pMiniMd, tkObj, szName, ppData, pcbData);

ErrExit:

    STOP_MD_PERF(GetCustomAttributeByName);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::GetCustomAttributeByName()


//*****************************************************************************
// Enumerate the CustomAttributes for a given token.
//*****************************************************************************
STDMETHODIMP RegMeta::EnumCustomAttributes(
    HCORENUM        *phEnum,            // Pointer to the enum.
    mdToken         tk,                 // Token to scope the enumeration.
    mdToken         tkType,             // Type to limit the enumeration.
    mdCustomAttribute   rCustomAttributes[],    // Put CustomAttributes here.
    ULONG           cMax,               // Max CustomAttributes to put.
    ULONG           *pcCustomAttributes)    // Put # tokens returned here.
{
    HRESULT         hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    HENUMInternal   **ppmdEnum = reinterpret_cast<HENUMInternal **> (phEnum);
    RID             ridStart;
    RID             ridEnd;
    HENUMInternal   *pEnum = NULL;
    CustomAttributeRec  *pRec;
    ULONG           index;

    LOG((LOGMD, "RegMeta::EnumCustomAttributes(0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x, 0x%08x)\n",
            phEnum, tk, tkType, rCustomAttributes, cMax, pcCustomAttributes));
    START_MD_PERF();
    LOCKREAD();

    if ( *ppmdEnum == 0 )
    {
        // instantiating a new ENUM
        CMiniMdRW       *pMiniMd = &(m_pStgdb->m_MiniMd);
        CLookUpHash     *pHashTable = pMiniMd->m_pLookUpHashes[TBL_CustomAttribute];

        // Does caller want all custom Values?
        if (IsNilToken(tk))
        {
            IfFailGo( HENUMInternal::CreateSimpleEnum(mdtCustomAttribute, 1, pMiniMd->getCountCustomAttributes()+1, &pEnum) );
        }
        else
        {   // Scope by some object.
            if ( pMiniMd->IsSorted( TBL_CustomAttribute ) )
            {
                // Get CustomAttributes for the object.
                IfFailGo(pMiniMd->getCustomAttributeForToken(tk, &ridEnd, &ridStart));

                if (IsNilToken(tkType))
                {
                    // Simple enumerator for object's entire list.
                    IfFailGo( HENUMInternal::CreateSimpleEnum( mdtCustomAttribute, ridStart, ridEnd, &pEnum) );
                }
                else
                {
                    // Dynamic enumerator for subsetted list.

                    IfFailGo( HENUMInternal::CreateDynamicArrayEnum( mdtCustomAttribute, &pEnum) );

                    for (index = ridStart; index < ridEnd; index ++ )
                    {
                        IfFailGo(pMiniMd->GetCustomAttributeRecord(index, &pRec));
                        if (tkType == pMiniMd->getTypeOfCustomAttribute(pRec))
                        {
                            IfFailGo( HENUMInternal::AddElementToEnum(pEnum, TokenFromRid(index, mdtCustomAttribute) ) );
                        }
                    }
                }
            }
            else
            {

                if (pHashTable)
                {
                    // table is not sorted but hash is built
                    // We want to create dynmaic array to hold the dynamic enumerator.
                    TOKENHASHENTRY *p;
                    ULONG       iHash;
                    int         pos;
                    mdToken     tkParentTmp;
                    mdToken     tkTypeTmp;

                    // Hash the data.
                    iHash = pMiniMd->HashCustomAttribute(tk);

                    IfFailGo( HENUMInternal::CreateDynamicArrayEnum( mdtCustomAttribute, &pEnum) );

                    // Go through every entry in the hash chain looking for ours.
                    for (p = pHashTable->FindFirst(iHash, pos);
                         p;
                         p = pHashTable->FindNext(pos))
                    {

                        CustomAttributeRec *pCustomAttribute;
                        IfFailGo(pMiniMd->GetCustomAttributeRecord(RidFromToken(p->tok), &pCustomAttribute));
                        tkParentTmp = pMiniMd->getParentOfCustomAttribute(pCustomAttribute);
                        tkTypeTmp = pMiniMd->getTypeOfCustomAttribute(pCustomAttribute);
                        if (tkParentTmp == tk)
                        {
                            if (IsNilToken(tkType) || tkType == tkTypeTmp)
                            {
                                // compare the blob value
                                IfFailGo( HENUMInternal::AddElementToEnum(pEnum, TokenFromRid(p->tok, mdtCustomAttribute )) );
                            }
                        }
                    }
                }
                else
                {

                    // table is not sorted and hash is not built so we have to create dynmaic array
                    // create the dynamic enumerator and loop through CA table linearly
                    //
                    ridStart = 1;
                    ridEnd = pMiniMd->getCountCustomAttributes() + 1;

                    IfFailGo( HENUMInternal::CreateDynamicArrayEnum( mdtCustomAttribute, &pEnum) );

                    for (index = ridStart; index < ridEnd; index ++ )
                    {
                        IfFailGo(pMiniMd->GetCustomAttributeRecord(index, &pRec));
                        if ( tk == pMiniMd->getParentOfCustomAttribute(pRec) &&
                            (tkType == pMiniMd->getTypeOfCustomAttribute(pRec) || IsNilToken(tkType)))
                        {
                            IfFailGo( HENUMInternal::AddElementToEnum(pEnum, TokenFromRid(index, mdtCustomAttribute) ) );
                        }
                    }
                }
            }
        }

        // set the output parameter
        *ppmdEnum = pEnum;
        pEnum = NULL;
    }

    // fill the output token buffer
    hr = HENUMInternal::EnumWithCount(*ppmdEnum, cMax, rCustomAttributes, pcCustomAttributes);

ErrExit:
    HENUMInternal::DestroyEnumIfEmpty(ppmdEnum);
    HENUMInternal::DestroyEnum(pEnum);

    STOP_MD_PERF(EnumCustomAttributes);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // STDMETHODIMP RegMeta::EnumCustomAttributes()


//*****************************************************************************
// Get information about a CustomAttribute.
//*****************************************************************************
STDMETHODIMP RegMeta::GetCustomAttributeProps(
    mdCustomAttribute   cv,                 // The attribute token
    mdToken     *ptkObj,                // [OUT, OPTIONAL] Put object token here.
    mdToken     *ptkType,               // [OUT, OPTIONAL] Put TypeDef/TypeRef token here.
    void const  **ppBlob,               // [OUT, OPTIONAL] Put pointer to data here.
    ULONG       *pcbSize)               // [OUT, OPTIONAL] Put size of data here.
{
    HRESULT     hr = S_OK;              // A result.

    BEGIN_ENTRYPOINT_NOTHROW;

    CMiniMdRW   *pMiniMd;

    START_MD_PERF();
    LOCKREAD();

    _ASSERTE(TypeFromToken(cv) == mdtCustomAttribute);

    pMiniMd = &(m_pStgdb->m_MiniMd);
    CustomAttributeRec  *pCustomAttributeRec;   // The custom value record.

    IfFailGo(pMiniMd->GetCustomAttributeRecord(RidFromToken(cv), &pCustomAttributeRec));

    if (ptkObj)
        *ptkObj = pMiniMd->getParentOfCustomAttribute(pCustomAttributeRec);

    if (ptkType)
        *ptkType = pMiniMd->getTypeOfCustomAttribute(pCustomAttributeRec);

    if (ppBlob != NULL)
    {
        IfFailGo(pMiniMd->getValueOfCustomAttribute(pCustomAttributeRec, (const BYTE **)ppBlob, pcbSize));
    }

ErrExit:

    STOP_MD_PERF(GetCustomAttributeProps);
    END_ENTRYPOINT_NOTHROW;

    return hr;
} // RegMeta::GetCustomAttributeProps
