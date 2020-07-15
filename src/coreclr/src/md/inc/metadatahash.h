// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// MetaDataHash.h -- Meta data hash data structures.
//

//
// Used by Emitters and by E&C.
//
//*****************************************************************************
#ifndef _MetaDataHash_h_
#define _MetaDataHash_h_

#if _MSC_VER >= 1100
 # pragma once
#endif

#include "utilcode.h"


#define     REHASH_THREADSHOLD      3


//*****************************************************************************
// A hash entry list item.
//*****************************************************************************
struct TOKENHASHENTRY
{
	mdToken		tok;
    ULONG       ulHash;
	ULONG		iNext;
};

//*****************************************************************************
// The following is a hash class definition used for hashing MemberDef. The difference
// from the hash table above is because it is expansive to retrieve the parent for MemberDef.
//
//*****************************************************************************
struct MEMBERDEFHASHENTRY
{
	mdToken		tok;
    mdToken     tkParent;
    ULONG       ulHash;
	ULONG		iNext;
};


//*****************************************************************************
// This class is used to create transient indexes for meta data structures.
// This class is generic; one must override it to provide hashing and
// accessor methods for your specific record type.  It can start out on top
// of malloc with a small memory footprint, and as you get larger, it must
// be capable of rehashing.
//*****************************************************************************
template <class Entry> class CMetaDataHashTemplate
{
public:
	CMetaDataHashTemplate()
    {
        m_rgBuckets = 0;
        m_cItems = 0;
        m_iBuckets = 0;
    }

	~CMetaDataHashTemplate()
    {
        // Free the bucket list.
        if (m_rgBuckets)
        {
	        delete [] m_rgBuckets;
	        m_rgBuckets = 0;
            m_cItems = 0;
	        m_iBuckets = 0;
        }
    }

//*****************************************************************************
// Called to allocate the hash table entries so that new data may be added.
//*****************************************************************************
	HRESULT NewInit(					// Return status.
		int			iBuckets=17)		// How many buckets you want.
    {
	    m_rgBuckets = new (nothrow) int[iBuckets];
	    if (!m_rgBuckets)
		    return (OutOfMemory());
	    m_iBuckets = iBuckets;
	    memset(m_rgBuckets, ~0, sizeof(int) * iBuckets);
	    return (S_OK);
    }

//*****************************************************************************
// Add new items to the hash list.
//*****************************************************************************
	Entry *Add( 		        		// Pointer to element to write to.
		ULONG		iHash)				// Hash value of entry to add.
    {
	    Entry       *p = 0;
        HRESULT     hr;

	    int iBucket = iHash % m_iBuckets;

        if (m_cItems > REHASH_THREADSHOLD * m_iBuckets)
        {
            hr = ReHash();
            if (FAILED(hr))
                return (0);
            iBucket = iHash % m_iBuckets;
        }

	    // Add a new item pointer.
	    p = m_Heap.Append();
	    if (!p)
		    return (0);

	    // Chain the new item to the front of the heap.
	    p->iNext = m_rgBuckets[iBucket];
        p->ulHash = iHash;
        m_cItems++;
        m_rgBuckets[iBucket] = m_Heap.ItemIndex(p);
	    return (p);
    }


//*****************************************************************************
// Grow the hash table
//*****************************************************************************
	HRESULT ReHash()
    {
        int         *rgBuckets;
        int         iBuckets;
        int         iBucket;
        int         index;
        int         iCount;
	    Entry       *p = 0;

        iBuckets = m_iBuckets*2 -1;
	    rgBuckets = new (nothrow) int[iBuckets];
	    if (!rgBuckets)
		    return (OutOfMemory());
	    memset(rgBuckets, ~0, sizeof(int) * iBuckets);

        // loop through each of data and rehash them
        iCount = m_Heap.Count();
        for (index = 0; index < iCount; index++)
        {
            // get the hash value of the entry
            p = m_Heap.Get(index);

            // rehash the entry
            iBucket = p->ulHash % iBuckets;

	        // Chain the item to the front of the new heap.
	        p->iNext = rgBuckets[iBucket];
            rgBuckets[iBucket] = index;
        }

        // swap the hash table
	    delete [] m_rgBuckets;
        m_rgBuckets = rgBuckets;
        m_iBuckets = iBuckets;
        return NOERROR;

    }

//*****************************************************************************
// Find first/find next node for a chain given the hash.
//*****************************************************************************
	Entry *FindFirst(			        // Return entry.
		ULONG		iHash,				// The hash value for the entry.
		int			&POS)				// Current position.
    {
	    int iBucket = iHash % m_iBuckets;
	    POS = m_rgBuckets[iBucket];
	    return (FindNext(POS));
    }

	Entry *FindNext(			        // Return entry or 0.
		int			&POS)				// Current location.
    {
	    Entry *p;

	    if (POS == ~0)
		    return (0);

	    p = m_Heap.Get(POS);
	    POS = p->iNext;
	    return (p);
    }

private:
	CDynArray<Entry>  m_Heap;	        // First heap in the list.
	int			*m_rgBuckets;			// Bucket list.
	int			m_iBuckets;				// How many buckets.
    int         m_cItems;               // Number of items in the hash
};


class CMetaDataHashBase : public CMetaDataHashTemplate<TOKENHASHENTRY>
{
};

class CMemberDefHash : public CMetaDataHashTemplate<MEMBERDEFHASHENTRY>
{
};

#endif // _MetaDataHash_h_
