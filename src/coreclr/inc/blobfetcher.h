// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// CBlobFetcher - it fetches binary chunks, similar to new, but more controlled
//
// Fast, dynamic, memory management which doesn't relocate blocks
// m_pIndex has array of pillars, where each pillar starts off empty and has
// just-in-time allocation. As each pillar fills up, we move to the next pillar
// If the entire array of pillars fill up, we need to allocate a new array and
// copy the pillars over. But the actual data returned from GetBlock() never
// gets moved. So everyone's happy.
//
//*****************************************************************************


#ifndef __BLOB_FETCHER_H_
#define __BLOB_FETCHER_H_

#include <windef.h>


class  CBlobFetcher
{
protected:

    class CPillar {
    public:
        CPillar();
        ~CPillar();

        void SetAllocateSize(unsigned nSize);
        unsigned GetAllocateSize() const;

        char* MakeNewBlock(unsigned len, unsigned pad);
        void StealDataFrom(CPillar & src);
        unsigned GetDataLen() const;
        char* GetRawDataStart();
        BOOL Contains(_In_ char *ptr);
        ULONG32 GetOffset(_In_ char *ptr);

    protected:
        unsigned m_nTargetSize; // when we allocate, make it this large

    // Make these public so CBlobFetcher can do easy manipulation
    public:
        char* m_dataAlloc;
        char* m_dataStart;
        char* m_dataCur;
        char* m_dataEnd;
    };


    CPillar * m_pIndex; // array of pillars

    unsigned m_nIndexMax;   // actual size of m_ppIndex
    unsigned m_nIndexUsed;  // current pillar, so start at 0

    unsigned m_nDataLen;    // sum of all pillars' lengths

// Don't allow these because they'll mess up the ownership
    CBlobFetcher(const CBlobFetcher & src);
    CBlobFetcher& operator=(const CBlobFetcher & src);

public:
#if defined(HOST_64BIT)
    // needs to be 64 so that we can purposefully cache align code in ngen'd images
    enum { maxAlign = 64 }; // maximum alignment we support
#else
    enum { maxAlign = 32 }; // maximum alignment we support
#endif
    CBlobFetcher();
    ~CBlobFetcher();

// get a block to write on (use instead of write to avoid copy)
    char * MakeNewBlock(unsigned int nSize, unsigned align=1);

// Index segment as if this were linear
    char * ComputePointer(unsigned offset) const;

// Determine if pointer came from this fetcher
    BOOL ContainsPointer(_In_ char *ptr) const;

// Find an offset as if this were linear
    unsigned ComputeOffset(_In_ char *ptr) const;

// Write out the section to the stream
    HRESULT Write(HANDLE file);

// Write out the section to memory
    HRESULT WriteMem(void ** pMem);

// Get the total length of all our data (sum of all the pillar's data length's)
// cached value, so light weight & no computations
    unsigned GetDataLen() const;

    HRESULT Merge(CBlobFetcher *destination);

// Set the blob fetcher to slow growth mode. This should be done before any allocations
    void SetInitialGrowth(unsigned growth);
};


//*****************************************************************************
// Inlines
//*****************************************************************************

// Set the size that the Pillar will allocate if we call getBlock()
inline void CBlobFetcher::CPillar::SetAllocateSize(unsigned nSize)
{
    LIMITED_METHOD_CONTRACT;
    m_nTargetSize = nSize;
}

// Get the size we will allocate so we can decide if we need to change it
// This is not the same as the GetDataLen() and is only useful
// before we do the allocation
inline unsigned CBlobFetcher::CPillar::GetAllocateSize() const
{
    LIMITED_METHOD_CONTRACT;
    return m_nTargetSize;
}

inline char* CBlobFetcher::CPillar::GetRawDataStart()
{
    LIMITED_METHOD_CONTRACT;
    return m_dataStart;
}

inline BOOL CBlobFetcher::CPillar::Contains(_In_ char *ptr)
{
    LIMITED_METHOD_CONTRACT;

    return ptr >= m_dataStart && ptr < m_dataCur;
}

inline ULONG32 CBlobFetcher::CPillar::GetOffset(_In_ char *ptr)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(Contains(ptr));

    return (ULONG32)(ptr - m_dataStart);
}

//-----------------------------------------------------------------------------
// Calculate the length of data being used, (not the length allocated)
//-----------------------------------------------------------------------------
inline unsigned CBlobFetcher::CPillar::GetDataLen() const
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE((m_dataCur >= m_dataStart) && (m_dataCur <= m_dataEnd));

    return (unsigned)(m_dataCur - m_dataStart);
}

inline unsigned CBlobFetcher::GetDataLen() const
{
    LIMITED_METHOD_CONTRACT;

    return m_nDataLen;
}

// Set the blob fetcher to slow growth mode. This should be done before any allocations
inline void CBlobFetcher::SetInitialGrowth(unsigned growth)
{
    _ASSERTE(GetDataLen() == 0);
    if (GetDataLen() == 0)
    {
        m_pIndex[0].SetAllocateSize(growth);
    }
}

#endif  // __BLOB_FETCHER_H_
