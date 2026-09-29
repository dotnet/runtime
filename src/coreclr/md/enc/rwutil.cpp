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
#include "contract.h"
#include "../inc/mdlog.h"

#if defined(FEATURE_METADATA_IN_VM) && !defined(SELF_NO_HOST) && defined(TARGET_X86) && defined(TARGET_WINDOWS)
#define TRACK_METADATA_CANT_STOP_COUNT
#endif

HRESULT CreateMDReadWriteLock(minipal_rwlock **ppLock)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    minipal_rwlock *pLock = new (nothrow) minipal_rwlock;
    IfNullRet(pLock);

    if (!minipal_rwlock_init(pLock))
    {
        delete pLock;
        return E_OUTOFMEMORY;
    }

    *ppLock = pLock;
    return S_OK;
}

void DestroyMDReadWriteLock(minipal_rwlock *pLock)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (pLock != NULL)
    {
        minipal_rwlock_destroy(pLock);
        delete pLock;
    }
}

HRESULT AcquireMDReadLock(minipal_rwlock *pLock)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

#ifdef TRACK_METADATA_CANT_STOP_COUNT
    IncCantStopCount();
#endif

    if (!minipal_rwlock_enter_read(pLock))
    {
#ifdef TRACK_METADATA_CANT_STOP_COUNT
        DecCantStopCount();
#endif
        return E_FAIL;
    }

    EE_LOCK_TAKEN(pLock);
    return S_OK;
}

HRESULT AcquireMDWriteLock(minipal_rwlock *pLock COMMA_INDEBUG(CMiniMdRW *pMiniMd))
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
    }
    CONTRACTL_END;

#ifdef TRACK_METADATA_CANT_STOP_COUNT
    IncCantStopCount();
#endif

    if (!minipal_rwlock_enter_write(pLock))
    {
#ifdef TRACK_METADATA_CANT_STOP_COUNT
        DecCantStopCount();
#endif
        return E_FAIL;
    }

#ifdef _DEBUG
    if (pMiniMd != NULL)
    {
        pMiniMd->Debug_SetIsLockedForWrite(true);
    }
#endif // _DEBUG
    EE_LOCK_TAKEN(pLock);
    return S_OK;
}

void ReleaseMDReadLock(minipal_rwlock *pLock)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    minipal_rwlock_leave_read(pLock);
#ifdef TRACK_METADATA_CANT_STOP_COUNT
    DecCantStopCount();
#endif
    EE_LOCK_RELEASED(pLock);
}

void ReleaseMDWriteLock(minipal_rwlock *pLock COMMA_INDEBUG(CMiniMdRW *pMiniMd))
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    if (pMiniMd != NULL)
    {
        pMiniMd->Debug_SetIsLockedForWrite(false);
    }
#endif // _DEBUG
    minipal_rwlock_leave_write(pLock);
#ifdef TRACK_METADATA_CANT_STOP_COUNT
    DecCantStopCount();
#endif
    EE_LOCK_RELEASED(pLock);
}

#undef TRACK_METADATA_CANT_STOP_COUNT

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

    cchRet = WideCharToMultiByte(
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
    cTokens = min ( (ULONG)(pEnum->u.m_ulEnd - pEnum->u.m_ulCur), cMax);

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
    cTokens = min ( (ULONG)(pEnum->u.m_ulEnd - pEnum->u.m_ulCur), cMax * 2);

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





#ifdef FEATURE_METADATA_PERSISTENCE
HRESULT MDTOKENMAP::EmptyMap()
{
    Clear();
    m_iCountSorted = 0;
    return S_OK;
}


//*****************************************************************************
// find a token in the tokenmap.
//*****************************************************************************
bool MDTOKENMAP::Find(
    mdToken     tkFind,                 // [IN] the token value to find
    TOKENREC    **ppRec)                // [OUT] point to the record found in the dynamic array
{
    int         lo,mid,hi;              // binary search indices.
    TOKENREC    *pRec = NULL;

    _ASSERTE(m_iCountSorted == (ULONG)Count());

    lo = 0;
    hi = Count() - 1;
    while (lo <= hi)
    {
        mid = (lo + hi) / 2;
        pRec = Get(mid);

        if (tkFind == pRec->m_tkFrom)
        {
            *ppRec = pRec;
            return true;
        }

        if (pRec->m_tkFrom < tkFind)
            lo = mid + 1;
        else
            hi = mid - 1;
    }

    return false;
}
//*****************************************************************************
// output a remapped token
//*****************************************************************************
mdToken MDTOKENMAP::SafeRemap(
    mdToken     tkFrom)                 // [IN] the token value to find
{
    TOKENREC    *pRec;

    SortTokensByFromToken();

    if (Find(tkFrom, &pRec))
    {
        return pRec->m_tkTo;
    }

    return tkFrom;
}

void MDTOKENMAP::SortTokensByFromToken()
{
    ULONG count = Count();
    if (m_iCountSorted < count)
    {
        SortRangeFromToken(0, count - 1);
        m_iCountSorted = count;
    }
}

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
}


//*****************************************************************************
// find a token in the tokenmap.
//*****************************************************************************
HRESULT MDTOKENMAP::AppendRecord(
    mdToken     tkFind,
    mdToken     tkTo)
{
    TOKENREC *pRec = Append();
    IfNullRet(pRec);

    pRec->m_tkFrom = tkFind;
    pRec->m_tkTo = tkTo;
    return S_OK;
}



#endif

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
CMDReadWriteLock::CMDReadWriteLock(
    minipal_rwlock * pLock
    COMMA_INDEBUG(CMiniMdRW *pMiniMd))
{
    m_fLockedForRead = false;
    m_fLockedForWrite = false;
    m_pLock = pLock;
    INDEBUG(m_pMiniMd = pMiniMd;)
} // CMDReadWriteLock::CMDReadWriteLock



//*********************************************************************************************************
//
// Destructor
//
//*********************************************************************************************************
CMDReadWriteLock::~CMDReadWriteLock()
{
    _ASSERTE(!m_fLockedForRead || !m_fLockedForWrite);
    if (m_pLock == NULL)
    {
        return;
    }
    if (m_fLockedForRead)
    {
        LOG((LF_METADATA, LL_EVERYTHING, "ReleaseMDReadLock called from CMDReadWriteLock::~CMDReadWriteLock\n"));
        ReleaseMDReadLock(m_pLock);
    }
    if (m_fLockedForWrite)
    {
        LOG((LF_METADATA, LL_EVERYTHING, "ReleaseMDWriteLock called from CMDReadWriteLock::~CMDReadWriteLock\n"));
        ReleaseMDWriteLock(m_pLock COMMA_INDEBUG(m_pMiniMd));
    }
} // CMDReadWriteLock::~CMDReadWriteLock

//*********************************************************************************************************
//
// Used to obtain the read lock
//
//*********************************************************************************************************
HRESULT CMDReadWriteLock::LockRead()
{
    HRESULT hr = S_OK;

    _ASSERTE(!m_fLockedForRead && !m_fLockedForWrite);

    if (m_pLock == NULL)
    {
        INDEBUG(m_fLockedForRead = true);
        return hr;
    }

    LOG((LF_METADATA, LL_EVERYTHING, "AcquireMDReadLock called from CMDReadWriteLock::LockRead\n"));
    IfFailRet(AcquireMDReadLock(m_pLock));
    m_fLockedForRead = true;

    return hr;
} // CMDReadWriteLock::LockRead

//*********************************************************************************************************
//
// Used to obtain the write lock
//
//*********************************************************************************************************
HRESULT CMDReadWriteLock::LockWrite()
{
    HRESULT hr = S_OK;

    _ASSERTE(!m_fLockedForRead && !m_fLockedForWrite);

    if (m_pLock == NULL)
    {
        INDEBUG(m_fLockedForWrite = true);
        return hr;
    }

    LOG((LF_METADATA, LL_EVERYTHING, "AcquireMDWriteLock called from CMDReadWriteLock::LockWrite\n"));
    IfFailRet(AcquireMDWriteLock(m_pLock COMMA_INDEBUG(m_pMiniMd)));
    m_fLockedForWrite = true;

    return hr;
}

//*********************************************************************************************************
//
// Convert a read lock to a write lock
//
//*********************************************************************************************************
HRESULT CMDReadWriteLock::ConvertReadLockToWriteLock()
{
    _ASSERTE(!m_fLockedForWrite);

    HRESULT hr = S_OK;

    if (m_pLock == NULL)
    {
        INDEBUG(m_fLockedForRead = false);
        INDEBUG(m_fLockedForWrite = true);
        return hr;
    }

    if (m_fLockedForRead)
    {
        LOG((LF_METADATA, LL_EVERYTHING, "ReleaseMDReadLock called from CMDReadWriteLock::ConvertReadLockToWriteLock\n"));
        ReleaseMDReadLock(m_pLock);
        m_fLockedForRead = false;
    }
    LOG((LF_METADATA, LL_EVERYTHING, "AcquireMDWriteLock called from CMDReadWriteLock::ConvertReadLockToWriteLock\n"));
    IfFailRet(AcquireMDWriteLock(m_pLock COMMA_INDEBUG(m_pMiniMd)));
    m_fLockedForWrite = true;

    return hr;
} // CMDReadWriteLock::ConvertReadLockToWriteLock


//*********************************************************************************************************
//
// Unlocking for write
//
//*********************************************************************************************************
void CMDReadWriteLock::UnlockWrite()
{
    _ASSERTE(!m_fLockedForRead);

    if (m_pLock == NULL)
    {
        INDEBUG(m_fLockedForWrite = false);
        return;
    }
    if (m_fLockedForWrite)
    {
        LOG((LF_METADATA, LL_EVERYTHING, "ReleaseMDWriteLock called from CMDReadWriteLock::UnlockWrite\n"));
        ReleaseMDWriteLock(m_pLock COMMA_INDEBUG(m_pMiniMd));
        m_fLockedForWrite = false;
    }
} // CMDReadWriteLock::UnlockWrite

#ifdef _DEBUG
void CMDReadWriteLock::Debug_DetachMiniMd()
{
    _ASSERTE(m_fLockedForWrite);
    _ASSERTE(m_pMiniMd != NULL);

    if (m_pLock != NULL)
    {
        m_pMiniMd->Debug_SetIsLockedForWrite(false);
    }
    m_pMiniMd = NULL;
}
#endif // _DEBUG
