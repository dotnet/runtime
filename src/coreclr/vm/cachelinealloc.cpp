// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//---------------------------------------------------------------------------
// CCacheLineAllocator
//

//
//      This file dImplements the CCacheLineAllocator class.
//
// @comm
//
//  Notes:
//      The CacheLineAllocator maintains a pool of free CacheLines
//
//      The CacheLine Allocator provides static member functions
//      GetCacheLine and FreeCacheLine,
//---------------------------------------------------------------------------



#include "common.h"
#include <stddef.h>
#include "cachelinealloc.h"

#include "threads.h"
#include "excep.h"

///////////////////////////////////////////////////////
//    CCacheLineAllocator::CCacheLineAllocator()
//
//////////////////////////////////////////////////////

CCacheLineAllocator::CCacheLineAllocator()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_freeList32.Init();
    m_freeList64.Init();
    m_registryList.Init();
}

///////////////////////////////////////////////////////
//           void CCacheLineAllocator::~CCacheLineAllocator()
//
//////////////////////////////////////////////////////

CCacheLineAllocator::~CCacheLineAllocator()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LPCacheLine tempPtr = NULL;
    while((tempPtr = m_registryList.RemoveHead()) != NULL)
    {
        for (int i =0; i < CacheLine::numEntries; i++)
        {
            if(tempPtr->m_pAddr[i] != NULL)
            {
                if (!g_fProcessDetach)
                    VFree(tempPtr->m_pAddr[i]);
            }
        }
        delete tempPtr;
    }
}



///////////////////////////////////////////////////////
// static void *CCacheLineAllocator::VAlloc(ULONG cbSize)
//
//////////////////////////////////////////////////////


void *CCacheLineAllocator::VAlloc(ULONG cbSize)
{
    CONTRACT(void*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(CONTRACT_RETURN NULL);
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    // helper to call virtual free to release memory

    int i =0;
    void* pv = ClrVirtualAlloc (NULL, cbSize, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
    if (pv != NULL)
    {
        LPCacheLine tempPtr = m_registryList.GetHead();
        if (tempPtr == NULL)
        {
            goto LNew;
        }

        for (i =0; i < CacheLine::numEntries; i++)
        {
            if(tempPtr->m_pAddr[i] == NULL)
            {
                tempPtr->m_pAddr[i] = pv;
                RETURN pv;
            }
        }

LNew:
        // initialize the bucket before returning
        tempPtr = new (nothrow) CacheLine();
        if (tempPtr != NULL)
        {
            tempPtr->m_pAddr[0] = pv;
            m_registryList.InsertHead(tempPtr);
        }
        else
        {
            // couldn't find space to register this page
            ClrVirtualFree(pv, 0, MEM_RELEASE);
            RETURN NULL;
        }
    }
    RETURN pv;
}

///////////////////////////////////////////////////////
//   void CCacheLineAllocator::VFree(void* pv)
//
//////////////////////////////////////////////////////


void CCacheLineAllocator::VFree(void* pv)
{
    BOOL bRes = FALSE;

    CONTRACT_VOID
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pv));
        POSTCONDITION(bRes);
    }
    CONTRACT_END;

    // helper to call virtual free to release memory

    bRes = ClrVirtualFree (pv, 0, MEM_RELEASE);

    RETURN_VOID;
}

///////////////////////////////////////////////////////
//           void *CCacheLineAllocator::GetCacheLine()
//
//////////////////////////////////////////////////////

//WARNING: must have a lock when calling this function
void *CCacheLineAllocator::GetCacheLine64()
{
    CONTRACT(void*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(CONTRACT_RETURN NULL);
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    LPCacheLine tempPtr = m_freeList64.RemoveHead();
    if (tempPtr == NULL)
    {
        const ULONG AllocSize  = 4096 * 16;

        // Virtual Allocation for some more cache lines
        BYTE* ptr = (BYTE*)VAlloc(AllocSize);
        if(!ptr)
            RETURN NULL;

        tempPtr = (LPCacheLine)ptr;
        // Link all the buckets
        tempPtr = tempPtr+1;
        LPCacheLine maxPtr = (LPCacheLine)(ptr + AllocSize);

        while(tempPtr < maxPtr)
        {
            m_freeList64.InsertHead(tempPtr);
            tempPtr++;
        }

        // return the first block
        tempPtr = (LPCacheLine)ptr;
    }

    // initialize cacheline, 64 bytes
    memset((void*)tempPtr,0,64);
    RETURN tempPtr;
}


///////////////////////////////////////////////////////
//   void *CCacheLineAllocator::GetCacheLine32()
//
//////////////////////////////////////////////////////

//WARNING: must have a lock when calling this function
void *CCacheLineAllocator::GetCacheLine32()
{
    CONTRACT(void*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(CONTRACT_RETURN NULL);
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    LPCacheLine tempPtr = m_freeList32.RemoveHead();
    if (tempPtr != NULL)
    {
        // initialize cacheline, 32 bytes
        memset((void*)tempPtr,0,32);
        RETURN tempPtr;
    }
    tempPtr = (LPCacheLine)GetCacheLine64();
    if (tempPtr != NULL)
    {
        m_freeList32.InsertHead(tempPtr);
        tempPtr = (LPCacheLine)((BYTE *)tempPtr+32);
    }
    RETURN tempPtr;
}
///////////////////////////////////////////////////////
//    void CCacheLineAllocator::FreeCacheLine64(void * tempPtr)
//
//////////////////////////////////////////////////////
//WARNING: must have a lock when calling this function
void CCacheLineAllocator::FreeCacheLine64(void * tempPtr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;

        PRECONDITION(CheckPointer(tempPtr));
    }
    CONTRACTL_END;

    LPCacheLine pCLine = (LPCacheLine )tempPtr;
    m_freeList64.InsertHead(pCLine);
}


///////////////////////////////////////////////////////
//    void CCacheLineAllocator::FreeCacheLine32(void * tempPtr)
//
//////////////////////////////////////////////////////
//WARNING: must have a lock when calling this function
void CCacheLineAllocator::FreeCacheLine32(void * tempPtr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;

        PRECONDITION(CheckPointer(tempPtr));
    }
    CONTRACTL_END;

    LPCacheLine pCLine = (LPCacheLine )tempPtr;
    m_freeList32.InsertHead(pCLine);
}
