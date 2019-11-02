// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#ifndef __MDMemoTable__h__
#define __MDMemoTable__h__


//========================================================================================
// A MemoTable is a 0-based N element array designed for safe multithreaded access
// and single-initialization rules (e.g. multiple threads can race to initialize an entry
// but only the first one actually modifies the table.)
//
// Template parameters:
//
//    ELEMTYPE   - Type of the data stored in the array. This type must be small enough
//                 to be supported by InterlockedCompareExchange.
//
//    INITVALUE  - Sentinel that indicates that InitEntry() has not been called for that entry yet.
//                 This is the "initial value" for all elements of the array.
//
//    ELEMDELETER - (set to NULL if ELEMTYPE does not represent allocated storage.)
//                 A function that deallocates the memory referenced by a pointer-typed ELEMTYPE.
//                 MemoTable will never pass INITVALUE to this function.
//
//                 Restriction: if ELEMDELETER != NULL, ELEMTYPE must be a C++ pointer type
//                   (annoying template weirdness with InterlockedCompareExchangeT that
//                   life is too short to disentangle.)
//========================================================================================
template <typename ELEMTYPE, void (*ELEMDELETER)(ELEMTYPE)>
class MemoTable
{
public:
    MemoTable(ULONG numElems, ELEMTYPE initValue);
    ~MemoTable();
    HRESULT GetEntry(ULONG index, /* [out] */ ELEMTYPE *pElem);
    HRESULT InitEntry(ULONG index, /* [in,out] */ ELEMTYPE *pElem);

private:
    MemoTable(const MemoTable &other);
    MemoTable& operator=(const MemoTable &other);

    const ULONG     m_numElems;
    const ELEMTYPE  m_initValue;
    ELEMTYPE        *m_pTable;     // Lazily allocated 0-based array with m_numElems elements.
};




//==============================================================================
// MemoTable::MemoTable()
//==============================================================================
template <typename ELEMTYPE, void (*ELEMDELETER)(ELEMTYPE)>
MemoTable<ELEMTYPE, ELEMDELETER>::MemoTable(ULONG numElems, ELEMTYPE initValue) :
    m_numElems(numElems),
    m_initValue(initValue)
{
    m_pTable = NULL;
}



//==============================================================================
// MemoTable::~MemoTable()
//==============================================================================
template <typename ELEMTYPE, void (*ELEMDELETER)(ELEMTYPE)>
MemoTable<ELEMTYPE, ELEMDELETER>::~MemoTable()
{
    if (m_pTable != NULL)
    {
        if (ELEMDELETER != NULL)
        {
            for (ULONG index = 0; index < m_numElems; index++)
            {
                if (m_pTable[index] != m_initValue)
                {
                    ELEMDELETER(m_pTable[index]);
                }
            }
        }
        delete [] m_pTable;
    }
}

//==============================================================================
// HRESULT MemoTable::GetEntry(ULONG index, [out] ELEMTYPE *pElem)
//
// Retrieves the element at the specified index.
//
// Returns:
//   S_OK                   if returned element is not INITVALUE
//   S_FALSE                if returned element is INITVALUE
//   CDLB_E_INDEX_NOT_FOUND if index is out of range.
//==============================================================================
template <typename ELEMTYPE, void (*ELEMDELETER)(ELEMTYPE)>
HRESULT MemoTable<ELEMTYPE, ELEMDELETER>::GetEntry(ULONG index, /* [out] */ ELEMTYPE *pElem)
{
    _ASSERTE(pElem);
    if (index >= m_numElems)
    {
        return CLDB_E_INDEX_NOTFOUND;
    }
    if (m_pTable == NULL)
    {
        *pElem = m_initValue;
        return S_FALSE;
    }
    ELEMTYPE elem = m_pTable[index];
    *pElem = elem;
    return (elem == m_initValue) ? S_FALSE : S_OK;
}

//==============================================================================
// HRESULT MemoTable::InitEntry(ULONG index, [in, out] ELEMTYPE *pElem)
//
// Initalizes the elment at the specified index. It is illegal to attempt
// to initialize to INITVALUE. For scalar tables, it is illegal to attempt
// to overwrite a previously initialized entry with a different value.
// For pointer tables, the entry will be initialized using InterlockedCompareExchangeT
// and your new value thrown away via ELEMDELETER if you lose the race to initialize.
//
// Returns:
//    *pElem overwritten with the actual value written into the table
//           (for pointer tables, this may not be the original pointer you passed
//           due to races.)
//
//    S_OK is the only success value.
//==============================================================================
template <typename ELEMTYPE, void (*ELEMDELETER)(ELEMTYPE)>
HRESULT MemoTable<ELEMTYPE, ELEMDELETER>::InitEntry(ULONG index, /* [in,out] */ ELEMTYPE *pElem)
{
    // This can cause allocations (thus entering the host) during a profiler stackwalk.
    // But we're ok since we're not supporting SQL/F1 profiling with WinMDs. FUTURE:
    // Would be nice to eliminate allocations on stack walks regardless.
    PERMANENT_CONTRACT_VIOLATION(HostViolation, ReasonUnsupportedForSQLF1Profiling);

    HRESULT hr = S_OK;
    _ASSERTE(pElem);
    ELEMTYPE incomingElem = *pElem;

    if (index >= m_numElems)
    {
        IfFailGo(CLDB_E_INDEX_NOTFOUND);
    }
    _ASSERTE(incomingElem != m_initValue);

    // If this is first call to InitEntry(), must initialize the table itself.
    if (m_pTable == NULL)
    {
        NewHolder<ELEMTYPE> pNewTable = NULL;

        if ((((size_t)(-1)) / sizeof(ELEMTYPE)) < m_numElems)
        {
            IfFailGo(E_OUTOFMEMORY);
        }

        // When loading NGen images we go through code paths that expect no faults and no
        // throws.  We will need to take a look at how we use the winmd metadata with ngen,
        // potentially storing the post-mangled metadata in the NI because as the adapter grows
        // we'll see more of these.
        CONTRACT_VIOLATION(FaultViolation);
        pNewTable = new (nothrow) ELEMTYPE[m_numElems];
        IfNullGo(pNewTable);

        for (ULONG walk = 0; walk < m_numElems; walk++)
        {
            pNewTable[walk] = m_initValue;
        }
        if (InterlockedCompareExchangeT<ELEMTYPE *>(&m_pTable, pNewTable, NULL) == NULL)
        {   // We won the initialization race
            pNewTable.SuppressRelease();
        }
    }


    //-------------------------------------------------------------------------
    // Cannot fail after this point, or we may delete *pElem after entering it into table.
    //-------------------------------------------------------------------------
    hr = S_OK;
    if (ELEMDELETER == NULL)
    {
        _ASSERTE(m_pTable[index] == m_initValue || m_pTable[index] == incomingElem);
        m_pTable[index] = incomingElem;
    }
    else
    {
        ELEMTYPE winner = InterlockedCompareExchangeT((ELEMTYPE volatile *)&m_pTable[index], incomingElem, m_initValue);
        if (winner != m_initValue)
        {
            ELEMDELETER(incomingElem); // Lost the race
            *pElem = winner;
        }
    }
    _ASSERTE(*pElem != m_initValue);

ErrExit:
    if (ELEMDELETER != NULL && FAILED(hr))
    {
        ELEMDELETER(*pElem);
        *pElem = m_initValue;
    }
    return hr;
}

#endif // __MDMemoTable__h__
