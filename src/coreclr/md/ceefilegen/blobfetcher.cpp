// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// Implementation for CBlobFetcher
//

//
//
//*****************************************************************************
#include "stdafx.h" // for ASSERTE and friends
#include "blobfetcher.h"
#include "log.h"

//-----------------------------------------------------------------------------
//  round up to a certain alignment
static inline unsigned roundUp(unsigned val, unsigned align) {
    _ASSERTE((align & (align - 1)) == 0);       // align must be a power of 2

    return((val + (align-1)) & ~(align-1));
}

//-----------------------------------------------------------------------------
//  round up to a certain alignment
static inline unsigned padForAlign(unsigned val, unsigned align) {
    _ASSERTE((align & (align - 1)) == 0);       // align must be a power of 2
    return ((-int(val)) & (align-1));
}

//*****************************************************************************
// Pillar implementation
//*****************************************************************************
//-----------------------------------------------------------------------------
CBlobFetcher::CPillar::CPillar()
{
    m_dataAlloc = NULL;
    m_dataStart = NULL;
    m_dataCur = NULL;
    m_dataEnd = NULL;

    // Default initial size is 4K bytes.
    m_nTargetSize = 0x1000;
}

//-----------------------------------------------------------------------------
CBlobFetcher::CPillar::~CPillar() {
// Sanity check to make sure nobody messed up the pts
    _ASSERTE((m_dataCur >= m_dataStart) && (m_dataCur <= m_dataEnd));

    delete [] m_dataAlloc;
}


//-----------------------------------------------------------------------------
// Transfer ownership of data, so src will lose data and this will get it.
// Data itself will remain untouched, just ptrs & ownership change
//-----------------------------------------------------------------------------
void CBlobFetcher::CPillar::StealDataFrom(CBlobFetcher::CPillar & src)
{
// We should only be moving into an empty Pillar
    _ASSERTE(m_dataStart == NULL);


    m_dataAlloc     = src.m_dataAlloc;
    m_dataStart     = src.m_dataStart;
    m_dataCur       = src.m_dataCur;
    m_dataEnd       = src.m_dataEnd;

    m_nTargetSize   = src.m_nTargetSize;

// Take away src's claim to data. This prevents multiple ownership and double deleting
    src.m_dataAlloc = src.m_dataStart = src.m_dataCur = src.m_dataEnd = NULL;

}

//-----------------------------------------------------------------------------
// Allocate a block in this particular pillar
//-----------------------------------------------------------------------------
/* make a new block 'len' bytes long'  However, move the pointer 'pad' bytes
   over so that the memory has the correct alignment characteristics.

   If the return value is NULL, there are two possibilities:
   - This CPillar reserved less memory than needed for the current allocation.
   - We are out-of-memory. In this case, CPillar:GetDataLen() will be 0.
 */

char * CBlobFetcher::CPillar::MakeNewBlock(unsigned len, unsigned pad) {

    _ASSERTE(pad < maxAlign);

    // Make sure we have memory in this block to allocate
    if (m_dataStart == NULL) {

        // make sure allocate at least as big as length
        unsigned nNewTargetSize = max(m_nTargetSize, len);

        //
        // We need to allocate memory with an offset of "pad" from
        // being "maxAlign" aligned. (data % maxAlign == pad).
        // Since "new" doesn't do this, allocate some extra
        // to handle the worst possible alignment case.
        //
        unsigned allocationSize = nNewTargetSize + (maxAlign-1);
        // Check for integer overflow
        if (allocationSize < nNewTargetSize)
        {   // Integer overflow happened, fail the allocation
            return NULL;
        }

        m_dataAlloc = new (nothrow) char[allocationSize];

        if (m_dataAlloc == NULL)
            return NULL;

        // Ensure that no uninitialized values are placed into the pe file.
        // While most of the logic carefully memset's appropriate pad bytes to 0, at least
        // one place has been found where that wasn't true.
        memset(m_dataAlloc, 0, allocationSize);

        m_nTargetSize = nNewTargetSize;

        m_dataStart = m_dataAlloc +
          ((pad - (UINT_PTR)(m_dataAlloc)) & (((UINT_PTR)maxAlign)-1));

        _ASSERTE((UINT_PTR)(m_dataStart) % maxAlign == pad);

        m_dataCur = m_dataStart;

        m_dataEnd = &m_dataStart[m_nTargetSize];
    }

    _ASSERTE(m_dataCur >= m_dataStart);
    _ASSERTE((int) len > 0);

    // If this block is full, then get out, we'll have to try another block
    if (m_dataCur + len > m_dataEnd)  {
        return NULL;
    }

    char* ret = m_dataCur;
    m_dataCur += len;
    _ASSERTE(m_dataCur <= m_dataEnd);
    return(ret);
}


//*****************************************************************************
// Blob Fetcher Implementation
//*****************************************************************************

//-----------------------------------------------------------------------------
CBlobFetcher::CBlobFetcher()
{
    // Setup storage
    m_pIndex = NULL;
    m_nIndexMax = 1; // start off with arbitrary small size  @@@ (minimum is 1)
    m_nIndexUsed = 0;
    _ASSERTE(m_nIndexUsed < m_nIndexMax); // use <, not <=

    m_nDataLen = 0;

    m_pIndex = new CPillar[m_nIndexMax];
    _ASSERTE(m_pIndex);
    //<TODO>@FUTURE: what do we do here if we run out of memory??!!</TODO>
}

//-----------------------------------------------------------------------------
CBlobFetcher::~CBlobFetcher()
{
    delete [] m_pIndex;
}


//-----------------------------------------------------------------------------
// Dynamic mem allocation, but we can't move old blocks (since others
// have pointers to them), so we need a fancy way to grow
// Returns NULL if the memory could not be allocated.
//-----------------------------------------------------------------------------
char* CBlobFetcher::MakeNewBlock(unsigned len, unsigned align) {

    _ASSERTE(m_pIndex);
    _ASSERTE(0 < align && align <= maxAlign);

    // deal with alignment
    unsigned pad = padForAlign(m_nDataLen, align);
    char* pChRet = NULL;
    if (pad != 0) {
        pChRet = m_pIndex[m_nIndexUsed].MakeNewBlock(pad, 0);

        // Did we run out of memory?
        if (pChRet == NULL && m_pIndex[m_nIndexUsed].GetDataLen() == 0)
            return NULL;

        // if don't have space for the pad, then need to allocate a new pillar
        // the allocation will handle the padding for the alignment of m_nDataLen
        if (pChRet) {
            memset(pChRet, 0, pad);
            m_nDataLen += pad;
            pad = 0;
        }
    }
#ifdef _DEBUG
    if (pChRet)
        _ASSERTE((m_nDataLen % align) == 0);
#endif

    // Quickly computing total data length is tough since we have alignment problems
    // We'll do it by getting the length of all the completely full pillars so far
    // and then adding on the size of the current pillar
    unsigned nPreDataLen = m_nDataLen - m_pIndex[m_nIndexUsed].GetDataLen();

    pChRet = m_pIndex[m_nIndexUsed].MakeNewBlock(len + pad, 0);

    // Did we run out of memory?
    if (pChRet == NULL &&  m_pIndex[m_nIndexUsed].GetDataLen() == NULL)
        return NULL;

    if (pChRet == NULL) {

        nPreDataLen = m_nDataLen;

        if (m_nIndexUsed + 1 == m_nIndexMax) {
            // entire array of pillars are full, re-org

            const unsigned nNewMax = m_nIndexMax * 2; // arbitrary new size

            CPillar* pNewIndex = new (nothrow) CPillar[nNewMax];
            if (pNewIndex == NULL)
                return NULL;

            // Copy old stuff
            for(unsigned i = 0; i < m_nIndexMax; i++)
                pNewIndex[i].StealDataFrom(m_pIndex[i]);

            delete [] m_pIndex;

            m_nIndexMax = nNewMax;
            m_pIndex = pNewIndex;

            STRESS_LOG2(LF_LOADER, LL_INFO10, "CBlobFetcher %08X reallocates m_pIndex %08X\n", this, m_pIndex);
        }

        m_nIndexUsed ++; // current pillar is full, move to next

        // Make sure the new pillar is large enough to hold the data
        // How we do this is *totally arbitrary* and has been optimized for how
        // we intend to use this.

        unsigned minSizeOfNewPillar = (3 * m_nDataLen) / 2;
        if (minSizeOfNewPillar < len)
            minSizeOfNewPillar = len;

        if (m_pIndex[m_nIndexUsed].GetAllocateSize() < minSizeOfNewPillar) {
            m_pIndex[m_nIndexUsed].SetAllocateSize(roundUp(minSizeOfNewPillar, maxAlign));
        }

        // Under stress, we have seen that m_pIndex[0] is empty, but
        // m_pIndex[1] is not. This assert tries to catch that scenario.
        _ASSERTE(m_pIndex[0].GetDataLen() != 0);

        // Now that we're on new pillar, try again
        pChRet = m_pIndex[m_nIndexUsed].MakeNewBlock(len + pad, m_nDataLen % maxAlign);
        if (pChRet == NULL)
            return NULL;
        _ASSERTE(pChRet);

        // The current pointer picks up at the same alignment that the last block left off
        _ASSERTE(nPreDataLen % maxAlign == ((UINT_PTR) pChRet) % maxAlign);
    }

    if (pad != 0) {
        memset(pChRet, 0, pad);
        pChRet += pad;
    }

    m_nDataLen = nPreDataLen + m_pIndex[m_nIndexUsed].GetDataLen();

    _ASSERTE(((unsigned) m_nDataLen - len) % align == 0);
    _ASSERTE((UINT_PTR(pChRet) % align) == 0);
    return pChRet;
}

//-----------------------------------------------------------------------------
// Index segment as if this were linear (middle weight function)
//-----------------------------------------------------------------------------
char * CBlobFetcher::ComputePointer(unsigned offset) const
{
    _ASSERTE(m_pIndex);
    unsigned idx = 0;

    if (offset == 0) {
        // if ask for a 0 offset and no data, return NULL
        if (m_pIndex[0].GetDataLen() == 0)
        {
            return NULL;
        }
    }
    else
    {
        while (offset >= m_pIndex[idx].GetDataLen()) {
            offset -= m_pIndex[idx].GetDataLen();
            idx ++;
            // Overflow - have asked for an offset greater than what exists
            if (idx > m_nIndexUsed) {
                _ASSERTE(!"CBlobFetcher::ComputePointer() Overflow");
                return NULL;
            }
        }
    }

    char * ptr = (char*) (m_pIndex[idx].GetRawDataStart() + offset);
    return ptr;
}

//-----------------------------------------------------------------------------
// See if a pointer came from this blob fetcher
//-----------------------------------------------------------------------------
BOOL CBlobFetcher::ContainsPointer( _In_ char *ptr) const
{
    _ASSERTE(m_pIndex);

    CPillar *p = m_pIndex;
    CPillar *pEnd = p + m_nIndexUsed;

    unsigned offset = 0;

    while (p <= pEnd) {
        if (p->Contains(ptr))
            return TRUE;

        offset += p->GetDataLen();
        p++;
    }

    return FALSE;
}

//-----------------------------------------------------------------------------
// Find a pointer as if this were linear (middle weight function)
//-----------------------------------------------------------------------------
unsigned CBlobFetcher::ComputeOffset(_In_ char *ptr) const
{
    _ASSERTE(m_pIndex);

    CPillar *p = m_pIndex;
    CPillar *pEnd = p + m_nIndexUsed;

    unsigned offset = 0;

    while (p <= pEnd) {
        if (p->Contains(ptr))
            return offset + p->GetOffset(ptr);

        offset += p->GetDataLen();
        p++;
    }

    _ASSERTE(!"Pointer not found");
    return 0;
}


//Take the data from our previous blob and copy it into our new blob
//after whatever was already in that blob.
HRESULT CBlobFetcher::Merge(CBlobFetcher *destination) {
    unsigned dataLen;
    char *dataBlock;
    char *dataCurr;
    unsigned idx;
    _ASSERTE(destination);

    dataLen = GetDataLen();
    _ASSERTE( dataLen >= 0 );

    // Make sure there actually is data in the previous blob before trying to append it.
    if ( 0 == dataLen )
    {
        return S_OK;
    }

    //Get the length of our data and get a new block large enough to hold all of it.
    dataBlock = destination->MakeNewBlock(dataLen, 1);
    if (dataBlock == NULL) {
        return E_OUTOFMEMORY;
    }

    //Copy all of the bytes using the write algorithm from PEWriter.cpp
    dataCurr=dataBlock;
    for (idx=0; idx<=m_nIndexUsed;  idx++) {
        if (m_pIndex[idx].GetDataLen()>0) {
            _ASSERTE(dataCurr<dataBlock+dataLen);
            memcpy(dataCurr, m_pIndex[idx].GetRawDataStart(), m_pIndex[idx].GetDataLen());
            dataCurr+=m_pIndex[idx].GetDataLen();
        }
    }

    return S_OK;

}
