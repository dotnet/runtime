// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// LoaderHeap.h
//

//
// Utility functions for managing memory allocations that typically do not
// need releasing.
//
//*****************************************************************************


#ifndef __LoaderHeap_h__
#define __LoaderHeap_h__

#include "utilcode.h"
#include "ex.h"

//==============================================================================
// Interface used to back out loader heap allocations.
//==============================================================================
class ILoaderHeapBackout
{
#ifdef _DEBUG
#define BackoutMem(pMem, dwSize)  RealBackoutMem( (pMem), (dwSize), __FILE__, __LINE__, "UNKNOWN", -1 )
#else
#define BackoutMem(pMem, dwSize)  RealBackoutMem( (pMem), (dwSize) )
#endif

public:
    virtual void RealBackoutMem(void *pMem
                        , size_t dwSize
#ifdef _DEBUG
                        , __in __in_z const char *szFile
                        , int lineNum
                        , __in __in_z const char *szAllocFile
                        , int allocLineNum
#endif
                        ) = 0;
};

//==============================================================================
// This structure packages up all the data needed to back out an AllocMem.
// It's mainly a short term parking place to get the data from the AllocMem
// to the AllocMemHolder while preserving the illusion that AllocMem() still
// returns just a pointer as it did in V1.
//==============================================================================
struct TaggedMemAllocPtr
{
    // Note: For AllocAlignedMem blocks, m_pMem and m_dwRequestedSize are the actual values to pass
    // to BackoutMem. Do not add "m_dwExtra"
    void        *m_pMem;                //Pointer to AllocMem'd block (needed to pass back to BackoutMem)
    size_t       m_dwRequestedSize;     //Requested allocation size (needed to pass back to BackoutMem)

    ILoaderHeapBackout  *m_pHeap;          //The heap that alloc'd the block (needed to know who to call BackoutMem on)

    //For AllocMem'd blocks, this is always 0.
    //For AllocAlignedMem blocks, you have to add m_dwExtra to m_pMem to arrive
    //   at the actual aligned pointer.
    size_t       m_dwExtra;

#ifdef _DEBUG
    const char  *m_szFile;              //File that called AllocMem
    int          m_lineNum;             //Line # of AllocMem callsite
#endif

//! Note: this structure is copied around using bitwise copy ("=").
//! Don't get too fancy putting stuff in here. It's really just a temporary
//! holding place to get stuff from RealAllocMem() to the MemAllocHolder.


  public:

    //
    // This makes "void *ptr = pLoaderHeap->AllocMem()" work as in V1.
    //
    operator void*() const
    {
        LIMITED_METHOD_CONTRACT;
        return (void*)(m_dwExtra + (BYTE*)m_pMem);
    }

    template < typename T >
    T cast() const
    {
        LIMITED_METHOD_CONTRACT;
        return reinterpret_cast< T >( operator void *() );
    }
};



// # bytes to leave between allocations in debug mode
// Set to a > 0 boundary to debug problems - I've made this zero, otherwise a 1 byte allocation becomes
// a (1 + LOADER_HEAP_DEBUG_BOUNDARY) allocation
#define LOADER_HEAP_DEBUG_BOUNDARY  0

#define VIRTUAL_ALLOC_RESERVE_GRANULARITY (64*1024)    // 0x10000  (64 KB)

typedef DPTR(struct LoaderHeapBlock) PTR_LoaderHeapBlock;

struct LoaderHeapBlock
{
    PTR_LoaderHeapBlock     pNext;
    PTR_VOID                pVirtualAddress;
    size_t                  dwVirtualSize;
    BOOL                    m_fReleaseMemory;

#ifndef DACCESS_COMPILE
    // pVirtualMemory  - the start address of the virtual memory
    // cbVirtualMemory - the length in bytes of the virtual memory
    // fReleaseMemory  - should LoaderHeap be responsible for releasing this memory
    void Init(void   *pVirtualMemory,
              size_t  cbVirtualMemory,
              BOOL    fReleaseMemory)
    {
        LIMITED_METHOD_CONTRACT;
        this->pNext = NULL;
        this->pVirtualAddress = pVirtualMemory;
        this->dwVirtualSize = cbVirtualMemory;
        this->m_fReleaseMemory = fReleaseMemory;
    }

    // Just calls LoaderHeapBlock::Init
    LoaderHeapBlock(void   *pVirtualMemory,
                    size_t  cbVirtualMemory,
                    BOOL    fReleaseMemory)
    {
        WRAPPER_NO_CONTRACT;
        Init(pVirtualMemory, cbVirtualMemory, fReleaseMemory);
    }

    LoaderHeapBlock()
    {
        WRAPPER_NO_CONTRACT;
        Init(NULL, 0, FALSE);
    }
#else
    // No ctors in DAC builds
    LoaderHeapBlock() {}
#endif
};

struct LoaderHeapFreeBlock;

// Collection of methods for helping in debugging heap corruptions
#ifdef _DEBUG
class  LoaderHeapSniffer;
struct LoaderHeapEvent;
#endif








//===============================================================================
// This is the base class for LoaderHeap and ExplicitControlLoaderHeap. Unfortunately,
// this class has become schizophrenic. Sometimes, it's used as a simple
// allocator that's semantically (but not perfwise!) equivalent to a blackbox
// alloc/free heap. Othertimes, it's used by callers who are actually aware
// of how it reserves addresses and want direct control over the range over which
// this thing allocates. These two types of allocations are handed out
// from two independent pools inside the heap.
//
// The backout strategy we use for the simple heap probably isn't
// directly applicable to the more advanced uses.
//
// We don't have time to refactor this so as a second-best measure,
// we make most of UnlockedLoaderHeap's methods protected and force everyone
// to use it them through two public derived classes that are mutual siblings.
//
// The LoaderHeap is the black-box heap and has a Backout() method but none
// of the advanced features that let you control address ranges.
//
// The ExplicitControlLoaderHeap exposes all the advanced features but
// has no Backout() feature. (If someone wants a backout feature, they need
// to design an appropriate one into this class.)
//===============================================================================
class UnlockedLoaderHeap
{
#ifdef _DEBUG
    friend class LoaderHeapSniffer;
#endif

#ifdef DACCESS_COMPILE
    friend class ClrDataAccess;
#endif

private:
    // Linked list of ClrVirtualAlloc'd pages
    PTR_LoaderHeapBlock m_pFirstBlock;

    // Allocation pointer in current block
    PTR_BYTE            m_pAllocPtr;

    // Points to the end of the committed region in the current block
    PTR_BYTE            m_pPtrToEndOfCommittedRegion;
    PTR_BYTE            m_pEndReservedRegion;

    PTR_LoaderHeapBlock m_pCurBlock;

    // When we need to ClrVirtualAlloc() MEM_RESERVE a new set of pages, number of bytes to reserve
    DWORD               m_dwReserveBlockSize;

    // When we need to commit pages from our reserved list, number of bytes to commit at a time
    DWORD               m_dwCommitBlockSize;

    // Range list to record memory ranges in
    RangeList *         m_pRangeList;

    size_t              m_dwTotalAlloc;

    size_t *             m_pPrivatePerfCounter_LoaderBytes;

    DWORD                m_Options;

    LoaderHeapFreeBlock *m_pFirstFreeBlock;

    // This is used to hold on to a block of reserved memory provided to the
    // constructor. We do this instead of adding it as the first block because
    // that requires comitting the first page of the reserved block, and for
    // startup working set reasons we want to delay that as long as possible.
    LoaderHeapBlock      m_reservedBlock;

public:

#ifdef _DEBUG
    enum
    {
        kCallTracing    = 0x00000001,   // Keep a permanent log of all callers

        kEncounteredOOM = 0x80000000,   // One time flag to record that an OOM interrupted call tracing
    }
    LoaderHeapDebugFlags;

    DWORD               m_dwDebugFlags;

    LoaderHeapEvent    *m_pEventList;   // Linked list of events (in reverse time order)
#endif



#ifdef _DEBUG
    size_t              m_dwDebugWastedBytes;
    static DWORD        s_dwNumInstancesOfLoaderHeaps;
#endif

#ifdef _DEBUG
    size_t DebugGetWastedBytes()
    {
        WRAPPER_NO_CONTRACT;
        return m_dwDebugWastedBytes + GetBytesAvailCommittedRegion();
    }
#endif

#ifdef _DEBUG
    // Stubs allocated from a LoaderHeap will have unwind info registered with NT.
    // The info must be unregistered when the heap is destroyed.
    BOOL                m_fPermitStubsWithUnwindInfo;
    BOOL                m_fStubUnwindInfoUnregistered;
#endif

public:
    BOOL                m_fExplicitControl;  // Am I a LoaderHeap or an ExplicitControlLoaderHeap?

#ifdef DACCESS_COMPILE
public:
    void EnumMemoryRegions(enum CLRDataEnumMemoryFlags flags);
#endif

public:
    typedef bool EnumPageRegionsCallback (PTR_VOID pvArgs, PTR_VOID pvAllocationBase, SIZE_T cbReserved);
    void EnumPageRegions (EnumPageRegionsCallback *pCallback, PTR_VOID pvArgs);

#ifndef DACCESS_COMPILE
protected:
    // Use this version if dwReservedRegionAddress already points to a
    // blob of reserved memory.  This will set up internal data structures,
    // using the provided, reserved memory.
    UnlockedLoaderHeap(DWORD dwReserveBlockSize,
                       DWORD dwCommitBlockSize,
                       const BYTE* dwReservedRegionAddress,
                       SIZE_T dwReservedRegionSize,
                       size_t *pPrivatePerfCounter_LoaderBytes = NULL,
                       RangeList *pRangeList = NULL,
                       BOOL fMakeExecutable = FALSE);

    ~UnlockedLoaderHeap();
#endif

private:
    size_t GetBytesAvailCommittedRegion();
    size_t GetBytesAvailReservedRegion();

protected:
    // number of bytes available in region
    size_t UnlockedGetReservedBytesFree()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pEndReservedRegion - m_pAllocPtr;
    }

private:
    // Get some more committed pages - either commit some more in the current reserved region, or, if it
    // has run out, reserve another set of pages
    BOOL GetMoreCommittedPages(size_t dwMinSize);

protected:
    // Reserve some pages at any address
    BOOL UnlockedReservePages(size_t dwCommitBlockSize);

protected:
    // In debug mode, allocate an extra LOADER_HEAP_DEBUG_BOUNDARY bytes and fill it with invalid data.  The reason we
    // do this is that when we're allocating vtables out of the heap, it is very easy for code to
    // get careless, and end up reading from memory that it doesn't own - but since it will be
    // reading some other allocation's vtable, no crash will occur.  By keeping a gap between
    // allocations, it is more likely that these errors will be encountered.
    void *UnlockedAllocMem(size_t dwSize
#ifdef _DEBUG
                          ,__in __in_z const char *szFile
                          ,int  lineNum
#endif
                          );
    void *UnlockedAllocMem_NoThrow(size_t dwSize
#ifdef _DEBUG
                                   ,__in __in_z const char *szFile
                                   ,int  lineNum
#endif
                                   );





protected:
    // Allocates memory aligned on power-of-2 boundary.
    //
    // The return value is a pointer that's guaranteed to be aligned.
    //
    // FREEING THIS BLOCK: Underneath, the actual block allocated may
    // be larger and start at an address prior to the one you got back.
    // It is this adjusted size and pointer that you pass to BackoutMem.
    // The required adjustment is passed back thru the pdwExtra pointer.
    //
    // Here is how to properly backout the memory:
    //
    //   size_t dwExtra;
    //   void *pMem = UnlockedAllocAlignedMem(dwRequestedSize, alignment, &dwExtra);
    //   _ASSERTE( 0 == (pMem & (alignment - 1)) );
    //   UnlockedBackoutMem( ((BYTE*)pMem) - dExtra, dwRequestedSize + dwExtra );
    //
    // If you use the AllocMemHolder or AllocMemTracker, all this is taken care of
    // behind the scenes.
    //
    //
    void *UnlockedAllocAlignedMem(size_t  dwRequestedSize
                                 ,size_t  dwAlignment
                                 ,size_t *pdwExtra
#ifdef _DEBUG
                                 ,__in __in_z const char *szFile
                                 ,int  lineNum
#endif
                                 );

    void *UnlockedAllocAlignedMem_NoThrow(size_t  dwRequestedSize
                                         ,size_t  dwAlignment
                                         ,size_t *pdwExtra
#ifdef _DEBUG
                                         ,__in __in_z const char *szFile
                                         ,int  lineNum
#endif
                                 );

protected:
    // This frees memory allocated by UnlockAllocMem. It's given this horrible name to emphasize
    // that it's purpose is for error path leak prevention purposes. You shouldn't
    // use LoaderHeap's as general-purpose alloc-free heaps.
    void UnlockedBackoutMem(void *pMem
                          , size_t dwSize
#ifdef _DEBUG
                          , __in __in_z const char *szFile
                          , int lineNum
                          , __in __in_z const char *szAllocFile
                          , int AllocLineNum
#endif
                          );

public:
    // Perf Counter reports the size of the heap
    size_t GetSize ()
    {
        LIMITED_METHOD_CONTRACT;
        return m_dwTotalAlloc;
    }

    BOOL IsExecutable();

public:
#ifdef _DEBUG
    void DumpFreeList();
#endif

public:
// Extra CallTracing support
#ifdef _DEBUG
    void UnlockedClearEvents();     //Discard saved events
    void UnlockedCompactEvents();   //Discard matching alloc/free events
    void UnlockedPrintEvents();     //Print event list
#endif

protected:
    void *UnlockedAllocMemForCode_NoThrow(size_t dwHeaderSize, size_t dwCodeSize, DWORD dwCodeAlignment, size_t dwReserveForJumpStubs);

    void UnlockedSetReservedRegion(BYTE* dwReservedRegionAddress, SIZE_T dwReservedRegionSize, BOOL fReleaseMemory);
};

//===============================================================================
// Create the LoaderHeap lock. It's the same lock for several different Heaps.
//===============================================================================
inline CRITSEC_COOKIE CreateLoaderHeapLock()
{
    return ClrCreateCriticalSection(CrstLoaderHeap,CrstFlags(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD));
}

//===============================================================================
// The LoaderHeap is the black-box heap and has a Backout() method but none
// of the advanced features that let you control address ranges.
//===============================================================================
typedef DPTR(class LoaderHeap) PTR_LoaderHeap;
class LoaderHeap : public UnlockedLoaderHeap, public ILoaderHeapBackout
{
private:
    CRITSEC_COOKIE    m_CriticalSection;

#ifndef DACCESS_COMPILE
public:
    LoaderHeap(DWORD dwReserveBlockSize,
               DWORD dwCommitBlockSize,
               size_t *pPrivatePerfCounter_LoaderBytes = NULL,
               RangeList *pRangeList = NULL,
               BOOL fMakeExecutable = FALSE
               )
      : UnlockedLoaderHeap(dwReserveBlockSize,
                           dwCommitBlockSize,
                           NULL, 0,
                           pPrivatePerfCounter_LoaderBytes,
                           pRangeList,
                           fMakeExecutable)
    {
        WRAPPER_NO_CONTRACT;
        m_CriticalSection = NULL;
        m_CriticalSection = CreateLoaderHeapLock();
        m_fExplicitControl = FALSE;
    }

public:
    LoaderHeap(DWORD dwReserveBlockSize,
               DWORD dwCommitBlockSize,
               const BYTE* dwReservedRegionAddress,
               SIZE_T dwReservedRegionSize,
               size_t *pPrivatePerfCounter_LoaderBytes = NULL,
               RangeList *pRangeList = NULL,
               BOOL fMakeExecutable = FALSE
               )
      : UnlockedLoaderHeap(dwReserveBlockSize,
                           dwCommitBlockSize,
                           dwReservedRegionAddress,
                           dwReservedRegionSize,
                           pPrivatePerfCounter_LoaderBytes,
                           pRangeList,
                           fMakeExecutable)
    {
        WRAPPER_NO_CONTRACT;
        m_CriticalSection = NULL;
        m_CriticalSection = CreateLoaderHeapLock();
        m_fExplicitControl = FALSE;
    }

#endif // DACCESS_COMPILE

    virtual ~LoaderHeap()
    {
        WRAPPER_NO_CONTRACT;

#ifndef DACCESS_COMPILE
        if (m_CriticalSection != NULL)
        {
            ClrDeleteCriticalSection(m_CriticalSection);
        }
#endif // DACCESS_COMPILE
    }



#ifdef _DEBUG
#define AllocMem(dwSize)                  RealAllocMem( (dwSize), __FILE__, __LINE__ )
#define AllocMem_NoThrow(dwSize)          RealAllocMem_NoThrow( (dwSize), __FILE__, __LINE__ )
#else
#define AllocMem(dwSize)                  RealAllocMem( (dwSize) )
#define AllocMem_NoThrow(dwSize)          RealAllocMem_NoThrow( (dwSize) )
#endif

public:
    FORCEINLINE TaggedMemAllocPtr RealAllocMem(S_SIZE_T dwSize
#ifdef _DEBUG
                                  ,__in __in_z const char *szFile
                                  ,int  lineNum
#endif
                  )
    {
        WRAPPER_NO_CONTRACT;

        if(dwSize.IsOverflow()) ThrowOutOfMemory();

        return RealAllocMemUnsafe(dwSize.Value() COMMA_INDEBUG(szFile) COMMA_INDEBUG(lineNum));

    }

    FORCEINLINE TaggedMemAllocPtr RealAllocMem_NoThrow(S_SIZE_T  dwSize
#ifdef _DEBUG
                                           ,__in __in_z const char *szFile
                                           ,int  lineNum
#endif
                  )
    {
        WRAPPER_NO_CONTRACT;

        if(dwSize.IsOverflow()) {
            TaggedMemAllocPtr tmap;
            tmap.m_pMem             = NULL;
            tmap.m_dwRequestedSize  = dwSize.Value();
            tmap.m_pHeap            = this;
            tmap.m_dwExtra          = 0;
#ifdef _DEBUG
            tmap.m_szFile           = szFile;
            tmap.m_lineNum          = lineNum;
#endif

            return tmap;
        }

        return RealAllocMemUnsafe_NoThrow(dwSize.Value() COMMA_INDEBUG(szFile) COMMA_INDEBUG(lineNum));
    }
private:
    
    TaggedMemAllocPtr RealAllocMemUnsafe(size_t dwSize
#ifdef _DEBUG
                                  ,__in __in_z const char *szFile
                                  ,int  lineNum
#endif
                  )
    {
        WRAPPER_NO_CONTRACT;

        
        void *pResult;
        TaggedMemAllocPtr tmap;

        CRITSEC_Holder csh(m_CriticalSection);
        pResult = UnlockedAllocMem(dwSize
#ifdef _DEBUG
                                 , szFile
                                 , lineNum
#endif
                                 );
        tmap.m_pMem             = pResult;
        tmap.m_dwRequestedSize  = dwSize;
        tmap.m_pHeap            = this;
        tmap.m_dwExtra          = 0;
#ifdef _DEBUG
        tmap.m_szFile           = szFile;
        tmap.m_lineNum          = lineNum;
#endif
        return tmap;
    }

    TaggedMemAllocPtr RealAllocMemUnsafe_NoThrow(size_t  dwSize
#ifdef _DEBUG
                                           ,__in __in_z const char *szFile
                                           ,int  lineNum
#endif
                  )
    {
        WRAPPER_NO_CONTRACT;

        void *pResult;
        TaggedMemAllocPtr tmap;

        CRITSEC_Holder csh(m_CriticalSection);

        pResult = UnlockedAllocMem_NoThrow(dwSize
#ifdef _DEBUG
                                           , szFile
                                           , lineNum
#endif
                                           );

        tmap.m_pMem             = pResult;
        tmap.m_dwRequestedSize  = dwSize;
        tmap.m_pHeap            = this;
        tmap.m_dwExtra          = 0;
#ifdef _DEBUG
        tmap.m_szFile           = szFile;
        tmap.m_lineNum          = lineNum;
#endif

        return tmap;
    }



#ifdef _DEBUG
#define AllocAlignedMem(dwSize, dwAlign)                RealAllocAlignedMem( (dwSize), (dwAlign), __FILE__, __LINE__)
#define AllocAlignedMem_NoThrow(dwSize, dwAlign)        RealAllocAlignedMem_NoThrow( (dwSize), (dwAlign), __FILE__, __LINE__)
#else
#define AllocAlignedMem(dwSize, dwAlign)                RealAllocAlignedMem( (dwSize), (dwAlign) )
#define AllocAlignedMem_NoThrow(dwSize, dwAlign)        RealAllocAlignedMem_NoThrow( (dwSize), (dwAlign) )
#endif

public:
    TaggedMemAllocPtr RealAllocAlignedMem(size_t  dwRequestedSize
                                         ,size_t  dwAlignment
#ifdef _DEBUG
                                         ,__in __in_z const char *szFile
                                         ,int  lineNum
#endif
                                         )
    {
        WRAPPER_NO_CONTRACT;

        CRITSEC_Holder csh(m_CriticalSection);


        TaggedMemAllocPtr tmap;
        void *pResult;
        size_t dwExtra;

        pResult = UnlockedAllocAlignedMem(dwRequestedSize
                                         ,dwAlignment
                                         ,&dwExtra
#ifdef _DEBUG
                                         ,szFile
                                         ,lineNum
#endif
                                     );

        tmap.m_pMem             = (void*)(((BYTE*)pResult) - dwExtra);
        tmap.m_dwRequestedSize  = dwRequestedSize + dwExtra;
        tmap.m_pHeap            = this;
        tmap.m_dwExtra          = dwExtra;
#ifdef _DEBUG
        tmap.m_szFile           = szFile;
        tmap.m_lineNum          = lineNum;
#endif

        return tmap;
    }


    TaggedMemAllocPtr RealAllocAlignedMem_NoThrow(size_t  dwRequestedSize
                                                 ,size_t  dwAlignment
#ifdef _DEBUG
                                                 ,__in __in_z const char *szFile
                                                 ,int  lineNum
#endif
                                                 )
    {
        WRAPPER_NO_CONTRACT;

        CRITSEC_Holder csh(m_CriticalSection);


        TaggedMemAllocPtr tmap;
        void *pResult;
        size_t dwExtra;

        pResult = UnlockedAllocAlignedMem_NoThrow(dwRequestedSize
                                                 ,dwAlignment
                                                 ,&dwExtra
#ifdef _DEBUG
                                                 ,szFile
                                                 ,lineNum
#endif
                                            );

        _ASSERTE(!(pResult == NULL && dwExtra != 0));

        tmap.m_pMem             = (void*)(((BYTE*)pResult) - dwExtra);
        tmap.m_dwRequestedSize  = dwRequestedSize + dwExtra;
        tmap.m_pHeap            = this;
        tmap.m_dwExtra          = dwExtra;
#ifdef _DEBUG
        tmap.m_szFile           = szFile;
        tmap.m_lineNum          = lineNum;
#endif

        return tmap;
    }


public:
    // This frees memory allocated by AllocMem. It's given this horrible name to emphasize
    // that it's purpose is for error path leak prevention purposes. You shouldn't
    // use LoaderHeap's as general-purpose alloc-free heaps.
    void RealBackoutMem(void *pMem
                        , size_t dwSize
#ifdef _DEBUG
                        , __in __in_z const char *szFile
                        , int lineNum
                        , __in __in_z const char *szAllocFile
                        , int allocLineNum
#endif
                        )
    {
        WRAPPER_NO_CONTRACT;
        CRITSEC_Holder csh(m_CriticalSection);
        UnlockedBackoutMem(pMem
                           , dwSize
#ifdef _DEBUG
                           , szFile
                           , lineNum
                           , szAllocFile
                           , allocLineNum
#endif
                           );
    }

public:
// Extra CallTracing support
#ifdef _DEBUG
    void ClearEvents()
    {
        WRAPPER_NO_CONTRACT;
        CRITSEC_Holder csh(m_CriticalSection);
        UnlockedClearEvents();
    }

    void CompactEvents()
    {
        WRAPPER_NO_CONTRACT;
        CRITSEC_Holder csh(m_CriticalSection);
        UnlockedCompactEvents();
    }

    void PrintEvents()
    {
        WRAPPER_NO_CONTRACT;
        CRITSEC_Holder csh(m_CriticalSection);
        UnlockedPrintEvents();
    }
#endif

};





//===============================================================================
// The ExplicitControlLoaderHeap exposes all the advanced features but
// has no Backout() feature. (If someone wants a backout feature, they need
// to design an appropriate one into this class.)
//
// Caller is responsible for synchronization. ExplicitControlLoaderHeap is
// not multithread safe.
//===============================================================================
typedef DPTR(class ExplicitControlLoaderHeap) PTR_ExplicitControlLoaderHeap;
class ExplicitControlLoaderHeap : public UnlockedLoaderHeap
{
#ifndef DACCESS_COMPILE
public:
    ExplicitControlLoaderHeap(size_t *pPrivatePerfCounter_LoaderBytes = NULL,
                              RangeList *pRangeList = NULL,
                              BOOL fMakeExecutable = FALSE
               )
      : UnlockedLoaderHeap(0, 0, NULL, 0,
                           pPrivatePerfCounter_LoaderBytes,
                           pRangeList,
                           fMakeExecutable)
    {
        WRAPPER_NO_CONTRACT;
        m_fExplicitControl = TRUE;
    }
#endif // DACCESS_COMPILE

public:
    void *RealAllocMem(size_t dwSize
#ifdef _DEBUG
                       ,__in __in_z const char *szFile
                       ,int  lineNum
#endif
                       )
    {
        WRAPPER_NO_CONTRACT;

        void *pResult;

        pResult = UnlockedAllocMem(dwSize
#ifdef _DEBUG
                                   , szFile
                                   , lineNum
#endif
                                   );
        return pResult;
    }

    void *RealAllocMem_NoThrow(size_t dwSize
#ifdef _DEBUG
                               ,__in __in_z const char *szFile
                               ,int  lineNum
#endif
                               )
    {
        WRAPPER_NO_CONTRACT;

        void *pResult;

        pResult = UnlockedAllocMem_NoThrow(dwSize
#ifdef _DEBUG
                                           , szFile
                                           , lineNum
#endif
                                           );
        return pResult;
    }


public:
    void *AllocMemForCode_NoThrow(size_t dwHeaderSize, size_t dwCodeSize, DWORD dwCodeAlignment, size_t dwReserveForJumpStubs)
    {
        WRAPPER_NO_CONTRACT;
        return UnlockedAllocMemForCode_NoThrow(dwHeaderSize, dwCodeSize, dwCodeAlignment, dwReserveForJumpStubs);
    }

    void SetReservedRegion(BYTE* dwReservedRegionAddress, SIZE_T dwReservedRegionSize, BOOL fReleaseMemory)
    {
        WRAPPER_NO_CONTRACT;
        return UnlockedSetReservedRegion(dwReservedRegionAddress, dwReservedRegionSize, fReleaseMemory);
    }

public:
    // number of bytes available in region
    size_t GetReservedBytesFree()
    {
        WRAPPER_NO_CONTRACT;
        return UnlockedGetReservedBytesFree();
    }
};



//==============================================================================
// AllocMemHolder : Allocated memory from LoaderHeap
//
// Old:
//
//   Foo* pFoo = (Foo*)pLoaderHeap->AllocMem(size);
//   pFoo->BackoutMem(pFoo, size)
//
//
// New:
//
//  {
//      AllocMemHolder<Foo> pfoo = pLoaderHeap->AllocMem();
//  } // BackoutMem on out of scope
//
//==============================================================================
template <typename TYPE>
class AllocMemHolder
{
    private:
        TaggedMemAllocPtr m_value;
        BOOL              m_fAcquired;


    //--------------------------------------------------------------------
    // All allowed (and disallowed) ctors here.
    //--------------------------------------------------------------------
    public:
        // Allow the construction "Holder h;"
        AllocMemHolder()
        {
            LIMITED_METHOD_CONTRACT;

            m_value.m_pMem = NULL;
            m_value.m_dwRequestedSize = 0;
            m_value.m_pHeap = 0;
            m_value.m_dwExtra = 0;
#ifdef _DEBUG
            m_value.m_szFile = NULL;
            m_value.m_lineNum = 0;
#endif
            m_fAcquired    = FALSE;
        }

    public:
        // Allow the construction "Holder h = pHeap->AllocMem()"
        AllocMemHolder(const TaggedMemAllocPtr value)
        {
            LIMITED_METHOD_CONTRACT;
            m_value     = value;
            m_fAcquired = TRUE;
        }

    private:
        // Disallow "Holder holder1 = holder2"
        AllocMemHolder(const AllocMemHolder<TYPE> &);


    private:
        // Disallow "Holder holder1 = void*"
        AllocMemHolder(const LPVOID &);

    //--------------------------------------------------------------------
    // Destructor (and the whole point of AllocMemHolder)
    //--------------------------------------------------------------------
    public:
        ~AllocMemHolder()
        {
            WRAPPER_NO_CONTRACT;
            if (m_fAcquired && m_value.m_pMem)
            {
                m_value.m_pHeap->RealBackoutMem(m_value.m_pMem,
                                                m_value.m_dwRequestedSize
#ifdef _DEBUG
                                                ,__FILE__
                                                ,__LINE__
                                                ,m_value.m_szFile
                                                ,m_value.m_lineNum
#endif
                                                );
            }
        }


    //--------------------------------------------------------------------
    // All allowed (and disallowed) assignment operators here.
    //--------------------------------------------------------------------
    public:
        // Reluctantly allow "AllocMemHolder h; ... h = pheap->AllocMem()"
        void operator=(const TaggedMemAllocPtr & value)
        {
            WRAPPER_NO_CONTRACT;
            // However, prevent repeated assignments as that would leak.
            _ASSERTE(m_value.m_pMem == NULL && !m_fAcquired);
            m_value = value;
            m_fAcquired = TRUE;
        }

    private:
        // Disallow "holder == holder2"
        const AllocMemHolder<TYPE> & operator=(const AllocMemHolder<TYPE> &);

    private:
        // Disallow "holder = void*"
        const AllocMemHolder<TYPE> & operator=(const LPVOID &);


    //--------------------------------------------------------------------
    // Operations on the holder itself
    //--------------------------------------------------------------------
    public:
        // Call this when you're ready to take ownership away from the holder.
        void SuppressRelease()
        {
            LIMITED_METHOD_CONTRACT;
            m_fAcquired = FALSE;
        }



    //--------------------------------------------------------------------
    // ... And the smart-pointer stuff so we can drop holders on top
    // of former pointer variables (mostly)
    //--------------------------------------------------------------------
    public:
        // Allow holder to be treated as the underlying pointer type
        operator TYPE* ()
        {
            LIMITED_METHOD_CONTRACT;
            return (TYPE*)(void*)m_value;
        }

    public:
        // Allow holder to be treated as the underlying pointer type
        TYPE* operator->()
        {
            LIMITED_METHOD_CONTRACT;
            return (TYPE*)(void*)m_value;
        }
    public:
        int operator==(TYPE* value)
        {
            LIMITED_METHOD_CONTRACT;
            return ((void*)m_value) == ((void*)value);
        }

    public:
        int operator!=(TYPE* value)
        {
            LIMITED_METHOD_CONTRACT;
            return ((void*)m_value) != ((void*)value);
        }

    public:
        int operator!() const
        {
            LIMITED_METHOD_CONTRACT;
            return m_value.m_pMem == NULL;
        }


};



// This utility helps track loaderheap allocations. Its main purpose
// is to backout allocations in case of an exception.
class AllocMemTracker
{
    public:
        AllocMemTracker();
        ~AllocMemTracker();

        // Tells tracker to store an allocated loaderheap block.
        //
        // Returns the pointer address of block for convenience.
        //
        // Ok to call on failed loaderheap allocation (will just do nothing and propagate the OOM as apropos).
        //
        // If Track fails due to an OOM allocating node space, it will backout the loaderheap block before returning.
        void *Track(TaggedMemAllocPtr tmap);
        void *Track_NoThrow(TaggedMemAllocPtr tmap);

        void SuppressRelease();

    private:
        struct AllocMemTrackerNode
        {
            ILoaderHeapBackout *m_pHeap;
            void            *m_pMem;
            size_t           m_dwRequestedSize;
#ifdef _DEBUG
            const char      *m_szAllocFile;
            int              m_allocLineNum;
#endif
        };

        enum
        {
            kAllocMemTrackerBlockSize =
#ifdef _DEBUG
                                        3
#else
                                       20
#endif
        };

        struct AllocMemTrackerBlock
        {
            AllocMemTrackerBlock    *m_pNext;
            int                      m_nextFree;
            AllocMemTrackerNode      m_Node[kAllocMemTrackerBlockSize];
        };


        AllocMemTrackerBlock        *m_pFirstBlock;
        AllocMemTrackerBlock        m_FirstBlock; // Stack-allocate the first block - "new" the rest.

    protected:
        BOOL                        m_fReleased;

};

#endif // __LoaderHeap_h__

