// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//---------------------------------------------------------------------------
// CCacheLineAllocator
//

//
// @doc
// @module	cachelineAlloc.h
//
//		This file defines the CacheLine Allocator class.
//
// @comm
//
//
// <nl> Definitions.:
// <nl>	Class Name						Header file
// <nl>	---------------------------		---------------
// <nl>	<c CCacheLineAllocator>			BAlloc.h
//
// <nl><nl>
//  Notes:
//		The CacheLineAllocator maintains a pool of free CacheLines
//
//		The CacheLine Allocator provides static member functions
//		GetCacheLine and FreeCacheLine,
//
// <nl><nl>
//
//---------------------------------------------------------------------------

#ifndef _H_CACHELINE_ALLOCATOR_
#define _H_CACHELINE_ALLOCATOR_

#include "slist.h"

#include <pshpack1.h>

class CacheLine
{
public:
    enum
    {
        numEntries       = 15,
        numValidBytes    = numEntries * sizeof(void *)
    };

    // store next pointer and the entries - total of 16 pointers
    SLink   m_Link;
    union
    {
        void*   m_pAddr[numEntries];
        BYTE    m_xxx[numValidBytes];
    };

    CacheLine()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        // initialize cacheline
        m_Link = {};
        memset(m_xxx,0,numValidBytes);
    }
};
static_assert(sizeof(CacheLine) == (16 * sizeof(void*)), "Cacheline should be exactly 16 pointers in size");
#include <poppack.h>

typedef CacheLine* LPCacheLine;

/////////////////////////////////////////////////////////
//		class CCacheLineAllocator
//		Handles Allocation/DeAllocation of cache lines
//		used for hash table overflow buckets
///////////////////////////////////////////////////////
class CCacheLineAllocator
{
    typedef SList<CacheLine, true> REGISTRYLIST;
    typedef SList<CacheLine, true> FREELIST32;
    typedef SList<CacheLine, true> FREELIST64;

public:

    //constructor
    CCacheLineAllocator ();
    //destructor
    ~CCacheLineAllocator ();

    // free cacheline blocks
    FREELIST32         m_freeList32; //32 byte
    FREELIST64         m_freeList64; //64 byte

    // registry for virtual free
    REGISTRYLIST     m_registryList;

    void *VAlloc(ULONG cbSize);

    void VFree(void* pv);

	// GetCacheLine,
	void *	GetCacheLine32();

    // GetCacheLine,
	void *	GetCacheLine64();

	// FreeCacheLine,
	void FreeCacheLine32(void *pCacheLine);

	// FreeCacheLine,
	void FreeCacheLine64(void *pCacheLine);

};
#endif
