// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// RWUtil.cpp
//

//
// contains utility code to MD directory
//
//*****************************************************************************
#include "stdafx.h"
#include "metadata.h"
#include "rwutil.h"
#include "utsem.h"
#include "../inc/mdlog.h"

//*****************************************************************************
// Helper methods
//*****************************************************************************
void
Unicode2UTF(
    LPCWSTR wszSrc, // The string to convert.
  _Out_writes_(cbDst)
    LPUTF8  szDst,  // Buffer for the output UTF8 string.
    int     cbDst)  // Size of the buffer for UTF8 string.
{
    int cchSrc = (int)u16_strlen(wszSrc);
    int cchRet;

    cchRet = WszWideCharToMultiByte(
        CP_UTF8,
        0,
        wszSrc,
        cchSrc + 1,
        szDst,
        cbDst,
        NULL,
        NULL);

    if (cchRet == 0)
    {
        _ASSERTE_MSG(FALSE, "Converting unicode string to UTF8 string failed!");
        szDst[0] = '\0';
    }
} // Unicode2UTF


HRESULT HENUMInternal::CreateSimpleEnum(
    DWORD           tkKind,             // kind of token that we are iterating
    ULONG           ridStart,           // starting rid
    ULONG           ridEnd,             // end rid
    HENUMInternal   **ppEnum)           // return the created HENUMInternal
{
    HENUMInternal   *pEnum;
    HRESULT         hr = NOERROR;

    // Don't create an empty enum.
    if (ridStart >= ridEnd)
    {
        *ppEnum = 0;
        goto ErrExit;
    }

    pEnum = new (nothrow) HENUMInternal;

    // check for out of memory error
    if (pEnum == NULL)
        IfFailGo( E_OUTOFMEMORY );

    HENUMInternal::ZeroEnum(pEnum);
    pEnum->m_tkKind = tkKind;
    pEnum->m_EnumType = MDSimpleEnum;
    pEnum->u.m_ulStart = pEnum->u.m_ulCur = ridStart;
    pEnum->u.m_ulEnd = ridEnd;
    pEnum->m_ulCount = ridEnd - ridStart;

    *ppEnum = pEnum;
ErrExit:
    return hr;

}   // CreateSimpleEnum


//*****************************************************************************
// Helper function to destroy Enumerator
//*****************************************************************************
void HENUMInternal::DestroyEnum(
    HENUMInternal   *pmdEnum)
{
    if (pmdEnum == NULL)
        return;

    if (pmdEnum->m_EnumType == MDDynamicArrayEnum)
    {
        TOKENLIST       *pdalist;
        pdalist = (TOKENLIST *) &(pmdEnum->m_cursor);

        // clear the embedded dynamic array before we delete the enum
        pdalist->Clear();
    }
    delete pmdEnum;
}   // DestroyEnum


//*****************************************************************************
// Helper function to destroy Enumerator if the enumerator is empty
//*****************************************************************************
void HENUMInternal::DestroyEnumIfEmpty(
    HENUMInternal   **ppEnum)           // reset the enumerator pointer to NULL if empty
{

    if (*ppEnum == NULL)
        return;

    if ((*ppEnum)->m_ulCount == 0)
    {
        HENUMInternal::DestroyEnum(*ppEnum);
        *ppEnum = NULL;
    }
}   // DestroyEnumIfEmpty


void HENUMInternal::ClearEnum(
    HENUMInternal   *pmdEnum)
{
    if (pmdEnum == NULL)
        return;

    if (pmdEnum->m_EnumType == MDDynamicArrayEnum)
    {
        TOKENLIST       *pdalist;
        pdalist = (TOKENLIST *) &(pmdEnum->m_cursor);

        // clear the embedded dynamic array before we delete the enum
        pdalist->Clear();
    }
}   // ClearEnum


//*****************************************************************************
// Helper function to iterate the enum
//*****************************************************************************
bool HENUMInternal::EnumNext(
    HENUMInternal *phEnum,              // [IN] the enumerator to retrieve information
    mdToken     *ptk)                   // [OUT] token to scope the search
{
    _ASSERTE(phEnum && ptk);

    if (phEnum->u.m_ulCur >= phEnum->u.m_ulEnd)
        return false;

    if ( phEnum->m_EnumType == MDSimpleEnum )
    {
        *ptk = phEnum->u.m_ulCur | phEnum->m_tkKind;
        phEnum->u.m_ulCur++;
    }
    else
    {
        TOKENLIST       *pdalist = (TOKENLIST *)&(phEnum->m_cursor);

        _ASSERTE( phEnum->m_EnumType == MDDynamicArrayEnum );
        *ptk = *( pdalist->Get(phEnum->u.m_ulCur++) );
    }
    return true;
}   // EnumNext

//*****************************************************************************
// Number of items in the enumerator.
//*****************************************************************************
HRESULT HENUMInternal::GetCount(
    HENUMInternal   *phEnum,            // [IN] the enumerator to retrieve information
    ULONG           *pCount)            // ]OUT] the index of the desired item
{
    // Check for empty enum.
    if (phEnum == 0)
        return S_FALSE;

    *pCount = phEnum->u.m_ulEnd - phEnum->u.m_ulStart;
    return S_OK;
}

//*****************************************************************************
// Get a specific element.
//*****************************************************************************
HRESULT HENUMInternal::GetElement(
    HENUMInternal   *phEnum,            // [IN] the enumerator to retrieve information
    ULONG           ix,                 // ]IN] the index of the desired item
    mdToken         *ptk)               // [OUT] token to fill
{
    // Check for empty enum.
    if (phEnum == 0)
        return S_FALSE;

    if (ix > (phEnum->u.m_ulEnd - phEnum->u.m_ulStart))
        return S_FALSE;

    if ( phEnum->m_EnumType == MDSimpleEnum )
    {
        *ptk = (phEnum->u.m_ulStart + ix) | phEnum->m_tkKind;
    }
    else
    {
        TOKENLIST       *pdalist = (TOKENLIST *)&(phEnum->m_cursor);

        _ASSERTE( phEnum->m_EnumType == MDDynamicArrayEnum );
        *ptk = *( pdalist->Get(ix) );
    }

    return S_OK;
}

//*****************************************************************************
// Helper function to fill output token buffers given an enumerator
//*****************************************************************************
HRESULT HENUMInternal::EnumWithCount(
    HENUMInternal   *pEnum,             // enumerator
    ULONG           cMax,               // max tokens that caller wants
    mdToken         rTokens[],          // output buffer to fill the tokens
    ULONG           *pcTokens)          // number of tokens fill to the buffer upon return
{
    ULONG           cTokens;
    HRESULT         hr = NOERROR;

    // Check for empty enum.
    if (pEnum == 0)
    {
        if (pcTokens)
            *pcTokens = 0;
        return S_FALSE;
    }

    // we can only fill the minimum of what caller asked for or what we have left
    cTokens = min ( (pEnum->u.m_ulEnd - pEnum->u.m_ulCur), cMax);

    if (pEnum->m_EnumType == MDSimpleEnum)
    {

        // now fill the output
        for (ULONG i = 0; i < cTokens; i ++, pEnum->u.m_ulCur++)
        {
            rTokens[i] = TokenFromRid(pEnum->u.m_ulCur, pEnum->m_tkKind);
        }

    }
    else
    {
        // cannot be any other kind!
        _ASSERTE( pEnum->m_EnumType == MDDynamicArrayEnum );

        // get the embedded dynamic array
        TOKENLIST       *pdalist = (TOKENLIST *)&(pEnum->m_cursor);

        for (ULONG i = 0; i < cTokens; i ++, pEnum->u.m_ulCur++)
        {
            rTokens[i] = *( pdalist->Get(pEnum->u.m_ulCur) );
        }
    }

    if (pcTokens)
        *pcTokens = cTokens;

    if (cTokens == 0)
        hr = S_FALSE;
    return hr;
}   // EnumWithCount


//*****************************************************************************
// Helper function to fill output token buffers given an enumerator
// This is a variation that takes two output arrays.  The tokens in the
// enumerator are interleaved, one for each array.  This is currently used by
// EnumMethodImpl which needs to return two arrays.
//*****************************************************************************
HRESULT HENUMInternal::EnumWithCount(
    HENUMInternal   *pEnum,             // enumerator
    ULONG           cMax,               // max tokens that caller wants
    mdToken         rTokens1[],         // first output buffer to fill the tokens
    mdToken         rTokens2[],         // second output buffer to fill the tokens
    ULONG           *pcTokens)          // number of tokens fill to each buffer upon return
{
    ULONG           cTokens;
    HRESULT         hr = NOERROR;

    // cannot be any other kind!
    _ASSERTE( pEnum->m_EnumType == MDDynamicArrayEnum );

    // Check for empty enum.
    if (pEnum == 0)
    {
        if (pcTokens)
            *pcTokens = 0;
        return S_FALSE;
    }

    // Number of tokens must always be a multiple of 2.
    _ASSERTE(! ((pEnum->u.m_ulEnd - pEnum->u.m_ulCur) % 2) );

    // we can only fill the minimum of what caller asked for or what we have left
    cTokens = min ( (pEnum->u.m_ulEnd - pEnum->u.m_ulCur), cMax * 2);

    // get the embedded dynamic array
    TOKENLIST       *pdalist = (TOKENLIST *)&(pEnum->m_cursor);

    for (ULONG i = 0; i < (cTokens / 2); i++)
    {
        rTokens1[i] = *( pdalist->Get(pEnum->u.m_ulCur++) );
        rTokens2[i] = *( pdalist->Get(pEnum->u.m_ulCur++) );
    }

    if (pcTokens)
        *pcTokens = cTokens / 2;

    if (cTokens == 0)
        hr = S_FALSE;
    return hr;
}   // EnumWithCount


//*****************************************************************************
// Helper function to create HENUMInternal
//*****************************************************************************
HRESULT HENUMInternal::CreateDynamicArrayEnum(
    DWORD           tkKind,             // kind of token that we are iterating
    HENUMInternal   **ppEnum)           // return the created HENUMInternal
{
    HENUMInternal   *pEnum;
    HRESULT         hr = NOERROR;
    TOKENLIST       *pdalist;

    pEnum = new (nothrow) HENUMInternal;

    // check for out of memory error
    if (pEnum == NULL)
        IfFailGo( E_OUTOFMEMORY );

    HENUMInternal::ZeroEnum(pEnum);
    pEnum->m_tkKind = tkKind;
    pEnum->m_EnumType = MDDynamicArrayEnum;

    // run the constructor in place
    pdalist = (TOKENLIST *) &(pEnum->m_cursor);
    ::new (pdalist) TOKENLIST;

    *ppEnum = pEnum;
ErrExit:
    return hr;

}   // _CreateDynamicArrayEnum



//*****************************************************************************
// Helper function to init HENUMInternal
//*****************************************************************************
void HENUMInternal::InitDynamicArrayEnum(
    HENUMInternal   *pEnum)             // HENUMInternal to be initialized
{
    TOKENLIST       *pdalist;

    HENUMInternal::ZeroEnum(pEnum);
    pEnum->m_EnumType = MDDynamicArrayEnum;
    pEnum->m_tkKind = (DWORD) -1;

    // run the constructor in place
    pdalist = (TOKENLIST *) &(pEnum->m_cursor);
    ::new (pdalist) TOKENLIST;
}   // CreateDynamicArrayEnum


//*****************************************************************************
// Helper function to init HENUMInternal
//*****************************************************************************
void HENUMInternal::InitSimpleEnum(
    DWORD           tkKind,             // kind of token that we are iterating
    ULONG           ridStart,           // starting rid
    ULONG           ridEnd,             // end rid
    HENUMInternal   *pEnum)             // HENUMInternal to be initialized
{
    pEnum->m_EnumType = MDSimpleEnum;
    pEnum->m_tkKind = tkKind;
    pEnum->u.m_ulStart = pEnum->u.m_ulCur = ridStart;
    pEnum->u.m_ulEnd = ridEnd;
    pEnum->m_ulCount = ridEnd - ridStart;

}   // InitSimpleEnum




//*****************************************************************************
// Helper function to init HENUMInternal
//*****************************************************************************
HRESULT HENUMInternal::AddElementToEnum(
    HENUMInternal   *pEnum,             // return the created HENUMInternal
    mdToken         tk)                 // token value to be stored
{
    HRESULT         hr = NOERROR;
    TOKENLIST       *pdalist;
    mdToken         *ptk;

    pdalist = (TOKENLIST *) &(pEnum->m_cursor);

        {
        // TODO: Revisit this violation.
        CONTRACT_VIOLATION(ThrowsViolation);
    ptk = ((mdToken *)pdalist->Append());
        }
    if (ptk == NULL)
        IfFailGo( E_OUTOFMEMORY );
    *ptk = tk;

    // increase the count
    pEnum->m_ulCount++;
    pEnum->u.m_ulEnd++;
ErrExit:
    return hr;

}   // _AddElementToEnum





//*****************************************************************************
// find a token in the tokenmap.
//*****************************************************************************
MDTOKENMAP::~MDTOKENMAP()
{
    if (m_pMap)
        m_pMap->Release();
} // MDTOKENMAP::~MDTOKENMAP()

HRESULT MDTOKENMAP::Init(
    IUnknown    *pImport)               // The import that this map is for.
{
    HRESULT     hr;                     // A result.
    IMetaDataTables *pITables=0;        // Table information.
    ULONG       cRows;                  // Count of rows in a table.
    ULONG       cTotal;                 // Running total of rows in db.
    TOKENREC    *pRec;                  // A TOKENREC record.
    mdToken     tkTable;                // Token kind for a table.

    hr = pImport->QueryInterface(IID_IMetaDataTables, (void**)&pITables);
    if (hr == S_OK)
    {
        // Determine the size of each table.
        cTotal = 0;
        for (ULONG ixTbl=0; ixTbl<TBL_COUNT; ++ixTbl)
        {
            // Where does this table's data start.
            m_TableOffset[ixTbl] = cTotal;
            // See if this table has tokens.
            tkTable = CMiniMdRW::GetTokenForTable(ixTbl);
            if (tkTable == (ULONG) -1)
            {
                // It doesn't have tokens, so we won't see any tokens for the table.
            }
            else
            {   // It has tokens, so we may see a token for every row.
                IfFailGo(pITables->GetTableInfo(ixTbl, 0, &cRows, 0,0,0));
                // Safe: cTotal += cRows
                if (!ClrSafeInt<ULONG>::addition(cTotal, cRows, cTotal))
                {
                    IfFailGo(COR_E_OVERFLOW);
                }
            }
        }
        m_TableOffset[TBL_COUNT] = cTotal;
        m_iCountIndexed = cTotal;
        // Attempt to allocate space for all of the possible remaps.
        if (!AllocateBlock(cTotal))
            IfFailGo(E_OUTOFMEMORY);
        // Note that no sorts are needed.
        m_sortKind = Indexed;
        // Initialize entries to "not found".
        for (ULONG i=0; i<cTotal; ++i)
        {
            pRec = Get(i);
            pRec->SetEmpty();
        }
    }
#if defined(_DEBUG)
    if (SUCCEEDED(pImport->QueryInterface(IID_IMetaDataImport, (void**)&m_pImport)))
    {
        // Ok, here's a pretty nasty workaround. We're going to make a big assumption here
        // that we're owned by the pImport, and so we don't need to keep a refcount
        // on the pImport object.
        //
        // If we did, we'd create a circular reference and neither this object nor
        // the RegMeta would be freed.
        m_pImport->Release();

    }



#endif

ErrExit:
    if (pITables)
        pITables->Release();
    return hr;
} // HRESULT MDTOKENMAP::Init()

HRESULT MDTOKENMAP::EmptyMap()
{
    int nCount = Count();
    for (int i=0; i<nCount; ++i)
    {
        Get(i)->SetEmpty();
    }

    return S_OK;
}// HRESULT MDTOKENMAP::Clear()


//*****************************************************************************
// find a token in the tokenmap.
//*****************************************************************************
bool MDTOKENMAP::Find(
    mdToken     tkFind,                 // [IN] the token value to find
    TOKENREC    **ppRec)                // [OUT] point to the record found in the dynamic array
{
    int         lo,mid,hi;              // binary search indices.
    TOKENREC    *pRec = NULL;

    if (m_sortKind == Indexed && TypeFromToken(tkFind) != mdtString)
    {
        // Get the entry.
        ULONG ixTbl = CMiniMdRW::GetTableForToken(tkFind);
        if(ixTbl == (ULONG) -1)
            return false;
        ULONG iRid = RidFromToken(tkFind);
        if((m_TableOffset[ixTbl] + iRid) > m_TableOffset[ixTbl+1])
            return false;
        pRec = Get(m_TableOffset[ixTbl] + iRid - 1);
        // See if it has been set.
        if (pRec->IsEmpty())
            return false;
        // Verify that it is what we think it is.
        _ASSERTE(pRec->m_tkFrom == tkFind);
        *ppRec = pRec;
        return true;
    }
    else
    {   // Shouldn't be any unsorted records, and table must be sorted in proper ordering.
        _ASSERTE( m_iCountTotal == m_iCountSorted &&
            (m_sortKind == SortByFromToken || m_sortKind == Indexed) );
        _ASSERTE( (m_iCountIndexed + m_iCountTotal) == (ULONG)Count() );

        // Start with entire table.
        lo = m_iCountIndexed;
        hi = Count() - 1;

        // While there are rows in the range...
        while (lo <= hi)
        {   // Look at the one in the middle.
            mid = (lo + hi) / 2;

            pRec = Get(mid);

            // If equal to the target, done.
            if (tkFind == pRec->m_tkFrom)
            {
                *ppRec = Get(mid);
                return true;
            }

            // If middle item is too small, search the top half.
            if (pRec->m_tkFrom < tkFind)
                lo = mid + 1;
            else // but if middle is to big, search bottom half.
                hi = mid - 1;
        }
    }

    // Didn't find anything that matched.
    return false;
} // bool MDTOKENMAP::Find()



//*****************************************************************************
// remap the token
//*****************************************************************************
HRESULT MDTOKENMAP::Remap(
    mdToken     tkFrom,
    mdToken     *ptkTo)
{
    HRESULT     hr = NOERROR;
    TOKENREC    *pRec;

    // Remap nil to same thing (helps because System.Object has no base class.)
    if (IsNilToken(tkFrom))
    {
        *ptkTo = tkFrom;
        return hr;
    }

    if ( Find(tkFrom, &pRec) )
    {
        *ptkTo = pRec->m_tkTo;
    }
    else
    {
        _ASSERTE( !" Bad lookup map!");
        hr = META_E_BADMETADATA;
    }
    return hr;
} // HRESULT MDTOKENMAP::Remap()



//*****************************************************************************
// find a token in the tokenmap.
//*****************************************************************************
HRESULT MDTOKENMAP::InsertNotFound(
    mdToken     tkFind,
    bool        fDuplicate,
    mdToken     tkTo,
    TOKENREC    **ppRec)
{
    HRESULT     hr = NOERROR;
    int         lo, mid, hi;                // binary search indices.
    TOKENREC    *pRec;

    // If possible, validate the input.
    _ASSERTE(!m_pImport || m_pImport->IsValidToken(tkFind));

    if (m_sortKind == Indexed && TypeFromToken(tkFind) != mdtString)
    {
        // Get the entry.
        ULONG ixTbl = CMiniMdRW::GetTableForToken(tkFind);
        _ASSERTE(ixTbl != (ULONG) -1);
        ULONG iRid = RidFromToken(tkFind);
        _ASSERTE((m_TableOffset[ixTbl] + iRid) <= m_TableOffset[ixTbl+1]);
        pRec = Get(m_TableOffset[ixTbl] + iRid - 1);
        // See if it has been set.
        if (!pRec->IsEmpty())
        {   // Verify that it is what we think it is.
            _ASSERTE(pRec->m_tkFrom == tkFind);
        }
        // Store the data.
        pRec->m_tkFrom = tkFind;
        pRec->m_isDuplicate = fDuplicate;
        pRec->m_tkTo = tkTo;
        pRec->m_isFoundInImport = false;
        // Return the result.
        *ppRec = pRec;
    }
    else
    {   // Shouldn't be any unsorted records, and table must be sorted in proper ordering.
        _ASSERTE( m_iCountTotal == m_iCountSorted &&
            (m_sortKind == SortByFromToken || m_sortKind == Indexed) );

        if ((Count() - m_iCountIndexed) > 0)
        {
            // Start with entire table.
            lo = m_iCountIndexed;
            hi = Count() - 1;

            // While there are rows in the range...
            while (lo < hi)
            {   // Look at the one in the middle.
                mid = (lo + hi) / 2;

                pRec = Get(mid);

                // If equal to the target, done.
                if (tkFind == pRec->m_tkFrom)
                {
                    *ppRec = Get(mid);
                    goto ErrExit;
                }

                // If middle item is too small, search the top half.
                if (pRec->m_tkFrom < tkFind)
                    lo = mid + 1;
                else // but if middle is to big, search bottom half.
                    hi = mid - 1;
            }
            _ASSERTE(hi <= lo);
            pRec = Get(lo);

            if (tkFind == pRec->m_tkFrom)
            {
                if (tkTo == pRec->m_tkTo && fDuplicate == pRec->m_isDuplicate)
                {
                    *ppRec = pRec;
                }
                else
                {
                    _ASSERTE(!"inconsistent token has been added to the table!");
                    IfFailGo( E_FAIL );
                }
            }

            if (tkFind < pRec->m_tkFrom)
            {
                // insert before lo;
                pRec = Insert(lo);
            }
            else
            {
                // insert after lo
                pRec = Insert(lo + 1);
            }
        }
        else
        {
            // table is empty
            pRec = Insert(m_iCountIndexed);
        }


        // If pRec == NULL, return E_OUTOFMEMORY
        IfNullGo(pRec);

        m_iCountTotal++;
        m_iCountSorted++;

        *ppRec = pRec;

        // initialize the record
        pRec->m_tkFrom = tkFind;
        pRec->m_isDuplicate = fDuplicate;
        pRec->m_tkTo = tkTo;
        pRec->m_isFoundInImport = false;
    }

ErrExit:
    return hr;
} // HRESULT MDTOKENMAP::InsertNotFound()


//*****************************************************************************
// find a "to" token in the tokenmap. Now that we are doing the ref to def optimization,
// we might have several from tokens map to the same to token. We need to return a range of index
// instead....
//*****************************************************************************
bool MDTOKENMAP::FindWithToToken(
    mdToken     tkFind,                 // [IN] the token value to find
    int         *piPosition)            // [OUT] return the first from-token that has the matching to-token
{
    int         lo, mid, hi;            // binary search indices.
    TOKENREC    *pRec;
    TOKENREC    *pRec2;

    // This makes sure that no insertions take place between calls to FindWithToToken.
    // We want to avoid repeated sorting of the table.
    _ASSERTE(m_sortKind != SortByToToken || m_iCountTotal == m_iCountSorted);

    // If the map is sorted with From tokens, change it to be sorted with To tokens.
    if (m_sortKind != SortByToToken)
        SortTokensByToToken();

    // Start with entire table.
    lo = 0;
    hi = Count() - 1;

    // While there are rows in the range...
    while (lo <= hi)
    {   // Look at the one in the middle.
        mid = (lo + hi) / 2;

        pRec = Get(mid);

        // If equal to the target, done.
        if (tkFind == pRec->m_tkTo)
        {
            for (int i = mid-1; i >= 0; i--)
            {
                pRec2 = Get(i);
                if (tkFind != pRec2->m_tkTo)
                {
                    *piPosition = i + 1;
                    return true;
                }
            }
            *piPosition = 0;
            return true;
        }

        // If middle item is too small, search the top half.
        if (pRec->m_tkTo < tkFind)
            lo = mid + 1;
        else // but if middle is to big, search bottom half.
            hi = mid - 1;
    }
    // Didn't find anything that matched.
    return false;
} // bool MDTOKENMAP::FindWithToToken()



//*****************************************************************************
// output a remapped token
//*****************************************************************************
mdToken MDTOKENMAP::SafeRemap(
    mdToken     tkFrom)                 // [IN] the token value to find
{
    TOKENREC    *pRec;

    // If possible, validate the input.
    _ASSERTE(!m_pImport || m_pImport->IsValidToken(tkFrom));

    SortTokensByFromToken();

    if ( Find(tkFrom, &pRec) )
    {
        return pRec->m_tkTo;
    }

    return tkFrom;
} // mdToken MDTOKENMAP::SafeRemap()


//*****************************************************************************
// Sorting
//*****************************************************************************
void MDTOKENMAP::SortTokensByToToken()
{
    // Only sort if there are unsorted records or the sort kind changed.
    if (m_iCountSorted < m_iCountTotal || m_sortKind != SortByToToken)
    {
        // Sort the entire array.
        m_iCountTotal = Count();
        m_iCountIndexed = 0;
        SortRangeToToken(0, m_iCountTotal - 1);
        m_iCountSorted = m_iCountTotal;
        m_sortKind = SortByToToken;
    }
} // void MDTOKENMAP::SortTokensByToToken()

void MDTOKENMAP::SortRangeFromToken(
    int         iLeft,
    int         iRight)
{
    int         iLast;
    int         i;                      // loop variable.

    // if less than two elements you're done.
    if (iLeft >= iRight)
        return;

    // The mid-element is the pivot, move it to the left.
    Swap(iLeft, (iLeft+iRight)/2);
    iLast = iLeft;

    // move everything that is smaller than the pivot to the left.
    for(i = iLeft+1; i <= iRight; i++)
        if (CompareFromToken(i, iLeft) < 0)
            Swap(i, ++iLast);

    // Put the pivot to the point where it is in between smaller and larger elements.
    Swap(iLeft, iLast);

    // Sort the each partition.
    SortRangeFromToken(iLeft, iLast-1);
    SortRangeFromToken(iLast+1, iRight);
} // void MDTOKENMAP::SortRangeFromToken()


//*****************************************************************************
// Sorting
//*****************************************************************************
void MDTOKENMAP::SortRangeToToken(
    int         iLeft,
    int         iRight)
{
    int         iLast;
    int         i;                      // loop variable.

    // if less than two elements you're done.
    if (iLeft >= iRight)
        return;

    // The mid-element is the pivot, move it to the left.
    Swap(iLeft, (iLeft+iRight)/2);
    iLast = iLeft;

    // move everything that is smaller than the pivot to the left.
    for(i = iLeft+1; i <= iRight; i++)
        if (CompareToToken(i, iLeft) < 0)
            Swap(i, ++iLast);

    // Put the pivot to the point where it is in between smaller and larger elements.
    Swap(iLeft, iLast);

    // Sort the each partition.
    SortRangeToToken(iLeft, iLast-1);
    SortRangeToToken(iLast+1, iRight);
} // void MDTOKENMAP::SortRangeToToken()


//*****************************************************************************
// find a token in the tokenmap.
//*****************************************************************************
HRESULT MDTOKENMAP::AppendRecord(
    mdToken     tkFind,
    bool        fDuplicate,
    mdToken     tkTo,
    TOKENREC    **ppRec)
{
    HRESULT     hr = NOERROR;
    TOKENREC    *pRec;

    // If possible, validate the input.
    _ASSERTE(!m_pImport || m_pImport->IsValidToken(tkFind));

    // If the map is indexed, and this is a table token, update-in-place.
    if (m_sortKind == Indexed && TypeFromToken(tkFind) != mdtString)
    {
        // Get the entry.
        ULONG ixTbl = CMiniMdRW::GetTableForToken(tkFind);
        _ASSERTE(ixTbl != (ULONG) -1);
        ULONG iRid = RidFromToken(tkFind);
        _ASSERTE((m_TableOffset[ixTbl] + iRid) <= m_TableOffset[ixTbl+1]);
        pRec = Get(m_TableOffset[ixTbl] + iRid - 1);
        // See if it has been set.
        if (!pRec->IsEmpty())
        {   // Verify that it is what we think it is.
            _ASSERTE(pRec->m_tkFrom == tkFind);
        }
    }
    else
    {
        pRec = Append();
        IfNullGo(pRec);

        // number of entries increased but not the sorted entry
        m_iCountTotal++;
    }

    // Store the data.
    pRec->m_tkFrom = tkFind;
    pRec->m_isDuplicate = fDuplicate;
    pRec->m_tkTo = tkTo;
    pRec->m_isFoundInImport = false;
    *ppRec = pRec;

ErrExit:
    return hr;
} // HRESULT MDTOKENMAP::AppendRecord()



//*********************************************************************************************************
//
// CMapToken's constructor
//
//*********************************************************************************************************
CMapToken::CMapToken()
{
    m_cRef = 1;
    m_pTKMap = NULL;
    m_isSorted = true;
} // TokenManager::TokenManager()



//*********************************************************************************************************
//
// CMapToken's destructor
//
//*********************************************************************************************************
CMapToken::~CMapToken()
{
    delete m_pTKMap;
}   // CMapToken::~CMapToken()


ULONG CMapToken::AddRef()
{
    return InterlockedIncrement(&m_cRef);
}   // CMapToken::AddRef()



ULONG CMapToken::Release()
{
    ULONG   cRef = InterlockedDecrement(&m_cRef);
    if (!cRef)
        delete this;
    return (cRef);
}   // CMapToken::Release()


HRESULT CMapToken::QueryInterface(REFIID riid, void **ppUnk)
{
	if (ppUnk == NULL)
		return E_INVALIDARG;

	if (IsEqualIID(riid, IID_IMapToken))
	{
		*ppUnk = (IMapToken *) this;
	}
	else if (IsEqualIID(riid, IID_IUnknown))
	{
		*ppUnk = (IUnknown *) this;
	}
	else
	{
		*ppUnk = NULL;
		return (E_NOINTERFACE);
	}

    AddRef();
    return (S_OK);
}   // CMapToken::QueryInterface



//*********************************************************************************************************
//
// Track the token mapping
//
//*********************************************************************************************************
HRESULT CMapToken::Map(
    mdToken     tkFrom,
    mdToken     tkTo)
{
    HRESULT     hr = NOERROR;
    TOKENREC    *pTkRec;

    if (m_pTKMap == NULL)
        m_pTKMap = new (nothrow) MDTOKENMAP;

    IfNullGo( m_pTKMap );

    IfFailGo( m_pTKMap->AppendRecord(tkFrom, false, tkTo, &pTkRec) );
    _ASSERTE( pTkRec );

    m_isSorted = false;
ErrExit:
    return hr;
}


//*********************************************************************************************************
//
// return what tkFrom is mapped to ptkTo. If there is no remap
// (ie the token from is filtered out by the filter mechanism, it will return false.
//
//*********************************************************************************************************
bool    CMapToken::Find(
    mdToken     tkFrom,
    TOKENREC    **pRecTo)
{
    TOKENREC    *pRec;
    bool        bRet;
    if ( m_isSorted == false )
    {
        // sort the map
        m_pTKMap->SortTokensByFromToken();
        m_isSorted = true;
    }

    bRet =  m_pTKMap->Find(tkFrom, &pRec) ;
    if (bRet)
    {
        _ASSERTE(pRecTo);
        *pRecTo = pRec;
    }
    else
    {
        pRec = NULL;
    }
    return bRet;
}


//*********************************************************************************************************
//
// This function returns true if tkFrom is resolved to a def token. Otherwise, it returns
// false.
//
//*********************************************************************************************************
bool TokenRemapManager::ResolveRefToDef(
    mdToken tkRef,                      // [IN] ref token
    mdToken *ptkDef)                    // [OUT] def token that it resolves to. If it does not resolve to a def
                                        // token, it will return the tkRef token here.
{
    mdToken     tkTo;

    _ASSERTE(ptkDef);

    if (TypeFromToken(tkRef) == mdtTypeRef)
    {
        tkTo = m_TypeRefToTypeDefMap[RidFromToken(tkRef)];
    }
    else
    {
        _ASSERTE( TypeFromToken(tkRef) == mdtMemberRef );
        tkTo = m_MemberRefToMemberDefMap[RidFromToken(tkRef)];
    }
    if (RidFromToken(tkTo) == mdTokenNil)
    {
        *ptkDef = tkRef;
        return false;
    }
    *ptkDef = tkTo;
    return true;
}   // ResolveRefToDef



//*********************************************************************************************************
//
// Destructor
//
//*********************************************************************************************************
TokenRemapManager::~TokenRemapManager()
{
    m_TypeRefToTypeDefMap.Clear();
    m_MemberRefToMemberDefMap.Clear();
}   // ~TokenRemapManager


//*********************************************************************************************************
//
// Initialize the size of Ref to Def optimization table. We will grow the tables in this function.
// We also initialize the table entries to zero.
//
//*********************************************************************************************************
HRESULT TokenRemapManager::ClearAndEnsureCapacity(
    ULONG       cTypeRef,
    ULONG       cMemberRef)
{
    HRESULT     hr = NOERROR;
    if ( ((ULONG) (m_TypeRefToTypeDefMap.Count())) < (cTypeRef + 1) )
    {
        if ( m_TypeRefToTypeDefMap.AllocateBlock(cTypeRef + 1 - m_TypeRefToTypeDefMap.Count() ) == 0 )
            IfFailGo( E_OUTOFMEMORY );
    }
    memset( m_TypeRefToTypeDefMap.Get(0), 0, (cTypeRef + 1) * sizeof(mdToken) );

    if ( ((ULONG) (m_MemberRefToMemberDefMap.Count())) < (cMemberRef + 1) )
    {
        if ( m_MemberRefToMemberDefMap.AllocateBlock(cMemberRef + 1 - m_MemberRefToMemberDefMap.Count() ) == 0 )
            IfFailGo( E_OUTOFMEMORY );
    }
    memset( m_MemberRefToMemberDefMap.Get(0), 0, (cMemberRef + 1) * sizeof(mdToken) );

ErrExit:
    return hr;
} // HRESULT TokenRemapManager::ClearAndEnsureCapacity()



//*********************************************************************************************************
//
// Constructor
//
//*********************************************************************************************************
CMDSemReadWrite::CMDSemReadWrite(
    UTSemReadWrite * pSem)
{
    m_fLockedForRead = false;
    m_fLockedForWrite = false;
    m_pSem = pSem;
} // CMDSemReadWrite::CMDSemReadWrite



//*********************************************************************************************************
//
// Destructor
//
//*********************************************************************************************************
CMDSemReadWrite::~CMDSemReadWrite()
{
    _ASSERTE(!m_fLockedForRead || !m_fLockedForWrite);
    if (m_pSem == NULL)
    {
        return;
    }
    if (m_fLockedForRead)
    {
        LOG((LF_METADATA, LL_EVERYTHING, "UnlockRead called from CSemReadWrite::~CSemReadWrite \n"));
        m_pSem->UnlockRead();
    }
    if (m_fLockedForWrite)
    {
        LOG((LF_METADATA, LL_EVERYTHING, "UnlockWrite called from CSemReadWrite::~CSemReadWrite \n"));
        m_pSem->UnlockWrite();
    }
} // CMDSemReadWrite::~CMDSemReadWrite

//*********************************************************************************************************
//
// Used to obtain the read lock
//
//*********************************************************************************************************
HRESULT CMDSemReadWrite::LockRead()
{
    HRESULT hr = S_OK;

    _ASSERTE(!m_fLockedForRead && !m_fLockedForWrite);

    if (m_pSem == NULL)
    {
        INDEBUG(m_fLockedForRead = true);
        return hr;
    }

    LOG((LF_METADATA, LL_EVERYTHING, "LockRead called from CSemReadWrite::LockRead \n"));
    IfFailRet(m_pSem->LockRead());
    m_fLockedForRead = true;

    return hr;
} // CMDSemReadWrite::LockRead

//*********************************************************************************************************
//
// Used to obtain the read lock
//
//*********************************************************************************************************
HRESULT CMDSemReadWrite::LockWrite()
{
    HRESULT hr = S_OK;

    _ASSERTE(!m_fLockedForRead && !m_fLockedForWrite);

    if (m_pSem == NULL)
    {
        INDEBUG(m_fLockedForWrite = true);
        return hr;
    }

    LOG((LF_METADATA, LL_EVERYTHING, "LockWrite called from CSemReadWrite::LockWrite \n"));
    IfFailRet(m_pSem->LockWrite());
    m_fLockedForWrite = true;

    return hr;
}

//*********************************************************************************************************
//
// Convert a read lock to a write lock
//
//*********************************************************************************************************
HRESULT CMDSemReadWrite::ConvertReadLockToWriteLock()
{
    _ASSERTE(!m_fLockedForWrite);

    HRESULT hr = S_OK;

    if (m_pSem == NULL)
    {
        INDEBUG(m_fLockedForRead = false);
        INDEBUG(m_fLockedForWrite = true);
        return hr;
    }

    if (m_fLockedForRead)
    {
        LOG((LF_METADATA, LL_EVERYTHING, "UnlockRead called from CSemReadWrite::ConvertReadLockToWriteLock \n"));
        m_pSem->UnlockRead();
        m_fLockedForRead = false;
    }
    LOG((LF_METADATA, LL_EVERYTHING, "LockWrite called from  CSemReadWrite::ConvertReadLockToWriteLock\n"));
    IfFailRet(m_pSem->LockWrite());
    m_fLockedForWrite = true;

    return hr;
} // CMDSemReadWrite::ConvertReadLockToWriteLock


//*********************************************************************************************************
//
// Unlocking for write
//
//*********************************************************************************************************
void CMDSemReadWrite::UnlockWrite()
{
    _ASSERTE(!m_fLockedForRead);

    if (m_pSem == NULL)
    {
        INDEBUG(m_fLockedForWrite = false);
        return;
    }
    if (m_fLockedForWrite)
    {
        LOG((LF_METADATA, LL_EVERYTHING, "UnlockWrite called from CSemReadWrite::UnlockWrite \n"));
        m_pSem->UnlockWrite();
        m_fLockedForWrite = false;
    }
} // CMDSemReadWrite::UnlockWrite
