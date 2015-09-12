//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*
 * Generational GC handle manager.  Internal Implementation Header.
 *
 * Shared defines and declarations for handle table implementation.
 *

 *
 */

#include "common.h"

#include "handletable.h"

/*--------------------------------------------------------------------------*/

//<TODO>@TODO: find a home for this in a project-level header file</TODO>
#define BITS_PER_BYTE               (8)
/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * MAJOR TABLE DEFINITIONS THAT CHANGE DEPENDING ON THE WEATHER
 *
 ****************************************************************************/

// 64k reserved per segment with 4k as header.
#define HANDLE_SEGMENT_SIZE     (0x10000)   // MUST be a power of 2 (and currently must be 64K due to VirtualAlloc semantics)
#define HANDLE_HEADER_SIZE      (0x1000)    // SHOULD be <= OS page size

#define HANDLE_SEGMENT_ALIGNMENT     HANDLE_SEGMENT_SIZE 


#if !BIGENDIAN

    // little-endian write barrier mask manipulation
    #define GEN_CLUMP_0_MASK        (0x000000FF)
    #define NEXT_CLUMP_IN_MASK(dw)  (dw >> BITS_PER_BYTE)

#else

    // big-endian write barrier mask manipulation
    #define GEN_CLUMP_0_MASK        (0xFF000000)
    #define NEXT_CLUMP_IN_MASK(dw)  (dw << BITS_PER_BYTE)

#endif


// if the above numbers change than these will likely change as well
#define HANDLE_HANDLES_PER_CLUMP    (16)        // segment write-barrier granularity
#define HANDLE_HANDLES_PER_BLOCK    (64)        // segment suballocation granularity
#define HANDLE_OPTIMIZE_FOR_64_HANDLE_BLOCKS    // flag for certain optimizations

// maximum number of internally supported handle types
#define HANDLE_MAX_INTERNAL_TYPES   (12)                             // should be a multiple of 4

// number of types allowed for public callers
#define HANDLE_MAX_PUBLIC_TYPES     (HANDLE_MAX_INTERNAL_TYPES - 1) // reserve one internal type

// internal block types
#define HNDTYPE_INTERNAL_DATABLOCK  (HANDLE_MAX_INTERNAL_TYPES - 1) // reserve last type for data blocks

// max number of generations to support statistics on
#define MAXSTATGEN                  (5)

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * MORE DEFINITIONS
 *
 ****************************************************************************/

// fast handle-to-segment mapping
#define HANDLE_SEGMENT_CONTENT_MASK     (HANDLE_SEGMENT_SIZE - 1)
#define HANDLE_SEGMENT_ALIGN_MASK       (~HANDLE_SEGMENT_CONTENT_MASK)

// table layout metrics
#define HANDLE_SIZE                     sizeof(_UNCHECKED_OBJECTREF)
#define HANDLE_HANDLES_PER_SEGMENT      ((HANDLE_SEGMENT_SIZE - HANDLE_HEADER_SIZE) / HANDLE_SIZE)
#define HANDLE_BLOCKS_PER_SEGMENT       (HANDLE_HANDLES_PER_SEGMENT / HANDLE_HANDLES_PER_BLOCK)
#define HANDLE_CLUMPS_PER_SEGMENT       (HANDLE_HANDLES_PER_SEGMENT / HANDLE_HANDLES_PER_CLUMP)
#define HANDLE_CLUMPS_PER_BLOCK         (HANDLE_HANDLES_PER_BLOCK / HANDLE_HANDLES_PER_CLUMP)
#define HANDLE_BYTES_PER_BLOCK          (HANDLE_HANDLES_PER_BLOCK * HANDLE_SIZE)
#define HANDLE_HANDLES_PER_MASK         (sizeof(ULONG32) * BITS_PER_BYTE)
#define HANDLE_MASKS_PER_SEGMENT        (HANDLE_HANDLES_PER_SEGMENT / HANDLE_HANDLES_PER_MASK)
#define HANDLE_MASKS_PER_BLOCK          (HANDLE_HANDLES_PER_BLOCK / HANDLE_HANDLES_PER_MASK)
#define HANDLE_CLUMPS_PER_MASK          (HANDLE_HANDLES_PER_MASK / HANDLE_HANDLES_PER_CLUMP)

// We use this relation to check for free mask per block.
C_ASSERT (HANDLE_HANDLES_PER_MASK * 2 == HANDLE_HANDLES_PER_BLOCK);


// cache layout metrics
#define HANDLE_CACHE_TYPE_SIZE          128 // 128 == 63 handles per bank
#define HANDLES_PER_CACHE_BANK          ((HANDLE_CACHE_TYPE_SIZE / 2) - 1)

// cache policy defines
#define REBALANCE_TOLERANCE             (HANDLES_PER_CACHE_BANK / 3)
#define REBALANCE_LOWATER_MARK          (HANDLES_PER_CACHE_BANK - REBALANCE_TOLERANCE)
#define REBALANCE_HIWATER_MARK          (HANDLES_PER_CACHE_BANK + REBALANCE_TOLERANCE)

// bulk alloc policy defines
#define SMALL_ALLOC_COUNT               (HANDLES_PER_CACHE_BANK / 10)

// misc constants
#define MASK_FULL                       (0)
#define MASK_EMPTY                      (0xFFFFFFFF)
#define MASK_LOBYTE                     (0x000000FF)
#define TYPE_INVALID                    ((BYTE)0xFF)
#define BLOCK_INVALID                   ((BYTE)0xFF)

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * CORE TABLE LAYOUT STRUCTURES
 *
 ****************************************************************************/

/*
 * we need byte packing for the handle table layout to work
 */
#include <pshpack1.h>



/*
 * Table Segment Header
 *
 * Defines the layout for a segment's header data.
 */
struct _TableSegmentHeader
{
    /*
     * Write Barrier Generation Numbers
     *
     * Each slot holds four bytes.  Each byte corresponds to a clump of handles.
     * The value of the byte corresponds to the lowest possible generation that a
     * handle in that clump could point into.
     *
     * WARNING: Although this array is logically organized as a BYTE[], it is sometimes
     *  accessed as ULONG32[] when processing bytes in parallel.  Code which treats the
     *  array as an array of ULONG32s must handle big/little endian issues itself.
     */
    BYTE rgGeneration[HANDLE_BLOCKS_PER_SEGMENT * sizeof(ULONG32) / sizeof(BYTE)];

    /*
     * Block Allocation Chains
     *
     * Each slot indexes the next block in an allocation chain.
     */
    BYTE rgAllocation[HANDLE_BLOCKS_PER_SEGMENT];

    /*
     * Block Free Masks
     *
     * Masks - 1 bit for every handle in the segment.
     */
    ULONG32 rgFreeMask[HANDLE_MASKS_PER_SEGMENT];

    /*
     * Block Handle Types
     *
     * Each slot holds the handle type of the associated block.
     */
    BYTE rgBlockType[HANDLE_BLOCKS_PER_SEGMENT];

    /*
     * Block User Data Map
     *
     * Each slot holds the index of a user data block (if any) for the associated block.
     */
    BYTE rgUserData[HANDLE_BLOCKS_PER_SEGMENT];

    /*
     * Block Lock Count
     *
     * Each slot holds a lock count for its associated block.
     * Locked blocks are not freed, even when empty.
     */
    BYTE rgLocks[HANDLE_BLOCKS_PER_SEGMENT];

    /*
     * Allocation Chain Tails
     *
     * Each slot holds the tail block index for an allocation chain.
     */
    BYTE rgTail[HANDLE_MAX_INTERNAL_TYPES];

    /*
     * Allocation Chain Hints
     *
     * Each slot holds a hint block index for an allocation chain.
     */
    BYTE rgHint[HANDLE_MAX_INTERNAL_TYPES];

    /*
     * Free Count
     *
     * Each slot holds the number of free handles in an allocation chain.
     */
    UINT rgFreeCount[HANDLE_MAX_INTERNAL_TYPES];

    /*
     * Next Segment
     *
     * Points to the next segment in the chain (if we ran out of space in this one).
     */
#ifdef DACCESS_COMPILE     
    TADDR pNextSegment;
#else
    struct TableSegment *pNextSegment;
#endif // DACCESS_COMPILE

    /*
     * Handle Table
     *
     * Points to owning handle table for this table segment.
     */
    PTR_HandleTable pHandleTable;

    /*
     * Flags
     */
    BYTE fResortChains      : 1;    // allocation chains need sorting
    BYTE fNeedsScavenging   : 1;    // free blocks need scavenging
    BYTE _fUnused           : 6;    // unused

    /*
     * Free List Head
     *
     * Index of the first free block in the segment.
     */
    BYTE bFreeList;

    /*
     * Empty Line
     *
     * Index of the first KNOWN block of the last group of unused blocks in the segment.
     */
    BYTE bEmptyLine;

    /*
     * Commit Line
     *
     * Index of the first uncommited block in the segment.
     */
    BYTE bCommitLine;

    /*
     * Decommit Line
     *
     * Index of the first block in the highest committed page of the segment.
     */
    BYTE bDecommitLine;

    /*
     * Sequence
     *
     * Indicates the segment sequence number.
     */
    BYTE bSequence;
};

typedef DPTR(struct _TableSegmentHeader) PTR__TableSegmentHeader;
typedef DPTR(LPARAM) PTR_LPARAM;

// The handle table is large and may not be entirely mapped. That's one reason for splitting out the table
// segment and the header as two separate classes. In DAC builds, we generally need only a single element from
// the table segment, so we can use the DAC to retrieve just the information we require. 
/*
 * Table Segment
 *
 * Defines the layout for a handle table segment.
 */
struct TableSegment : public _TableSegmentHeader
{
    /*
     * Filler
     */
    BYTE rgUnused[HANDLE_HEADER_SIZE - sizeof(_TableSegmentHeader)];

    /*
     * Handles
     */
    _UNCHECKED_OBJECTREF rgValue[HANDLE_HANDLES_PER_SEGMENT];
    
#ifdef DACCESS_COMPILE
    static ULONG32 DacSize(TADDR addr);
#endif
};

typedef SPTR(struct TableSegment) PTR_TableSegment;

/*
 * restore default packing
 */
#include <poppack.h>


/*
 * Handle Type Cache
 *
 * Defines the layout of a per-type handle cache.
 */
struct HandleTypeCache
{
    /*
     * reserve bank
     */
    OBJECTHANDLE rgReserveBank[HANDLES_PER_CACHE_BANK];

    /*
     * index of next available handle slot in the reserve bank
     */
    LONG lReserveIndex;
    

    /*---------------------------------------------------------------------------------
     * N.B. this structure is split up this way so that when HANDLES_PER_CACHE_BANK is
     * large enough, lReserveIndex and lFreeIndex will reside in different cache lines
     *--------------------------------------------------------------------------------*/

    /*
     * free bank
     */
    OBJECTHANDLE rgFreeBank[HANDLES_PER_CACHE_BANK];

    /*
     * index of next empty slot in the free bank
     */
    LONG lFreeIndex;
};


/*---------------------------------------------------------------------------*/



/****************************************************************************
 *
 * SCANNING PROTOTYPES
 *
 ****************************************************************************/

/*
 * ScanCallbackInfo
 *
 * Carries parameters for per-segment and per-block scanning callbacks.
 *
 */
struct ScanCallbackInfo
{
    PTR_TableSegment pCurrentSegment;   // segment we are presently scanning, if any
    UINT             uFlags;            // HNDGCF_* flags
    BOOL             fEnumUserData;     // whether user data is being enumerated as well
    HANDLESCANPROC   pfnScan;           // per-handle scan callback
    LPARAM           param1;            // callback param 1
    LPARAM           param2;            // callback param 2
    ULONG32          dwAgeMask;         // generation mask for ephemeral GCs

#ifdef _DEBUG
    UINT DEBUG_BlocksScanned;
    UINT DEBUG_BlocksScannedNonTrivially;
    UINT DEBUG_HandleSlotsScanned;
    UINT DEBUG_HandlesActuallyScanned;
#endif
};


/*
 * BLOCKSCANPROC
 *
 * Prototype for callbacks that implement per-block scanning logic.
 *
 */
typedef void (CALLBACK *BLOCKSCANPROC)(PTR_TableSegment pSegment, UINT uBlock, UINT uCount, ScanCallbackInfo *pInfo);


/*
 * SEGMENTITERATOR
 *
 * Prototype for callbacks that implement per-segment scanning logic.
 *
 */
typedef PTR_TableSegment (CALLBACK *SEGMENTITERATOR)(PTR_HandleTable pTable, PTR_TableSegment pPrevSegment, CrstHolderWithState *pCrstHolder);


/*
 * TABLESCANPROC
 *
 * Prototype for TableScanHandles and xxxTableScanHandlesAsync.
 *
 */
typedef void (CALLBACK *TABLESCANPROC)(PTR_HandleTable pTable,
                                       const UINT *puType, UINT uTypeCount,
                                       SEGMENTITERATOR pfnSegmentIterator,
                                       BLOCKSCANPROC pfnBlockHandler,
                                       ScanCallbackInfo *pInfo,
                                       CrstHolderWithState *pCrstHolder);

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * ADDITIONAL TABLE STRUCTURES
 *
 ****************************************************************************/

/*
 * AsyncScanInfo
 *
 * Tracks the state of an async scan for a handle table.
 *
 */
struct AsyncScanInfo
{
    /*
     * Underlying Callback Info
     *
     * Specifies callback info for the underlying block handler.
     */
    struct ScanCallbackInfo *pCallbackInfo;

    /*
     * Underlying Segment Iterator
     *
     * Specifies the segment iterator to be used during async scanning.
     */
    SEGMENTITERATOR   pfnSegmentIterator;

    /*
     * Underlying Block Handler
     *
     * Specifies the block handler to be used during async scanning.
     */
    BLOCKSCANPROC     pfnBlockHandler;

    /*
     * Scan Queue
     *
     * Specifies the nodes to be processed asynchronously.
     */
    struct ScanQNode *pScanQueue;

    /*
     * Queue Tail
     *
     * Specifies the tail node in the queue, or NULL if the queue is empty.
     */
    struct ScanQNode *pQueueTail;
};


/*
 * Handle Table
 *
 * Defines the layout of a handle table object.
 */
#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable : 4200 )  // zero-sized array
#endif
struct HandleTable
{
    /*
     * flags describing handle attributes
     *
     * N.B. this is at offset 0 due to frequent access by cache free codepath
     */
    UINT rgTypeFlags[HANDLE_MAX_INTERNAL_TYPES];

    /*
     * lock for this table
     */
    CrstStatic Lock;

    /*
     * number of types this table supports
     */
    UINT uTypeCount;

    /*
     * number of handles owned by this table that are marked as "used"
     * (this includes the handles residing in rgMainCache and rgQuickCache)
     */
    DWORD dwCount;

    /*
     * head of segment list for this table
     */
    PTR_TableSegment pSegmentList;

    /*
     * information on current async scan (if any)
     */
    AsyncScanInfo *pAsyncScanInfo;

    /*
     * per-table user info
     */
    UINT uTableIndex;

    /*
     * per-table AppDomain info
     */
    ADIndex uADIndex;

    /*
     * one-level per-type 'quick' handle cache
     */
    OBJECTHANDLE rgQuickCache[HANDLE_MAX_INTERNAL_TYPES];   // interlocked ops used here

    /*
     * debug-only statistics
     */
#ifdef _DEBUG
    int     _DEBUG_iMaxGen;
    INT64   _DEBUG_TotalBlocksScanned            [MAXSTATGEN];
    INT64   _DEBUG_TotalBlocksScannedNonTrivially[MAXSTATGEN];
    INT64   _DEBUG_TotalHandleSlotsScanned       [MAXSTATGEN];
    INT64   _DEBUG_TotalHandlesActuallyScanned   [MAXSTATGEN];
#endif

    /*
     * primary per-type handle cache
     */
    HandleTypeCache rgMainCache[0];                         // interlocked ops used here
};

#ifdef _MSC_VER
#pragma warning(pop)
#endif

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * HELPERS
 *
 ****************************************************************************/

/*
 * A 32/64 comparison callback
 *<TODO>
 * @TODO: move/merge into common util file
 *</TODO>
 */
typedef int (*PFNCOMPARE)(UINT_PTR p, UINT_PTR q);


/*
 * A 32/64 neutral quicksort
 *<TODO>
 * @TODO: move/merge into common util file
 *</TODO>
 */
void QuickSort(UINT_PTR *pData, int left, int right, PFNCOMPARE pfnCompare);


/*
 * CompareHandlesByFreeOrder
 *
 * Returns:
 *  <0 - handle P should be freed before handle Q
 *  =0 - handles are eqivalent for free order purposes
 *  >0 - handle Q should be freed before handle P
 *
 */
int CompareHandlesByFreeOrder(UINT_PTR p, UINT_PTR q);

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * CORE TABLE MANAGEMENT
 *
 ****************************************************************************/

/*
 * TypeHasUserData
 *
 * Determines whether a given handle type has user data.
 *
 */
__inline BOOL TypeHasUserData(HandleTable *pTable, UINT uType)
{
    LIMITED_METHOD_CONTRACT;

    // sanity
    _ASSERTE(uType < HANDLE_MAX_INTERNAL_TYPES);

    // consult the type flags
    return (pTable->rgTypeFlags[uType] & HNDF_EXTRAINFO);
}


/*
 * TableCanFreeSegmentNow
 *
 * Determines if it is OK to free the specified segment at this time.
 *
 */
BOOL TableCanFreeSegmentNow(HandleTable *pTable, TableSegment *pSegment);


/*
 * BlockIsLocked
 *
 * Determines if the lock count for the specified block is currently non-zero.
 *
 */
__inline BOOL BlockIsLocked(TableSegment *pSegment, UINT uBlock)
{
    LIMITED_METHOD_CONTRACT;

    // sanity
    _ASSERTE(uBlock < HANDLE_BLOCKS_PER_SEGMENT);

    // fetch the lock count and compare it to zero
    return (pSegment->rgLocks[uBlock] != 0);
}


/*
 * BlockLock
 *
 * Increases the lock count for a block.
 *
 */
__inline void BlockLock(TableSegment *pSegment, UINT uBlock)
{
    LIMITED_METHOD_CONTRACT;

    // fetch the old lock count
    BYTE bLocks = pSegment->rgLocks[uBlock];

    // assert if we are about to trash the count
    _ASSERTE(bLocks < 0xFF);

    // store the incremented lock count
    pSegment->rgLocks[uBlock] = bLocks + 1;
}


/*
 * BlockUnlock
 *
 * Decreases the lock count for a block.
 *
 */
__inline void BlockUnlock(TableSegment *pSegment, UINT uBlock)
{
    LIMITED_METHOD_CONTRACT;

    // fetch the old lock count
    BYTE bLocks = pSegment->rgLocks[uBlock];

    // assert if we are about to trash the count
    _ASSERTE(bLocks > 0);

    // store the decremented lock count
    pSegment->rgLocks[uBlock] = bLocks - 1;
}


/*
 * BlockFetchUserDataPointer
 *
 * Gets the user data pointer for the first handle in a block.
 *
 */
PTR_LPARAM BlockFetchUserDataPointer(PTR__TableSegmentHeader pSegment, UINT uBlock, BOOL fAssertOnError);


/*
 * HandleValidateAndFetchUserDataPointer
 *
 * Gets the user data pointer for a handle.
 * ASSERTs and returns NULL if handle is not of the expected type.
 *
 */
LPARAM *HandleValidateAndFetchUserDataPointer(OBJECTHANDLE handle, UINT uTypeExpected);


/*
 * HandleQuickFetchUserDataPointer
 *
 * Gets the user data pointer for a handle.
 * Less validation is performed.
 *
 */
PTR_LPARAM HandleQuickFetchUserDataPointer(OBJECTHANDLE handle);


/*
 * HandleQuickSetUserData
 *
 * Stores user data with a handle.
 * Less validation is performed.
 *
 */
void HandleQuickSetUserData(OBJECTHANDLE handle, LPARAM lUserData);


/*
 * HandleFetchType
 *
 * Computes the type index for a given handle.
 *
 */
UINT HandleFetchType(OBJECTHANDLE handle);


/*
 * HandleFetchHandleTable
 *
 * Returns the containing handle table of a given handle.
 *
 */
PTR_HandleTable HandleFetchHandleTable(OBJECTHANDLE handle);


/*
 * SegmentAlloc
 *
 * Allocates a new segment.
 *
 */
TableSegment *SegmentAlloc(HandleTable *pTable);


/*
 * SegmentFree
 *
 * Frees the specified segment.
 *
 */
void SegmentFree(TableSegment *pSegment);

/*
 * TableHandleAsyncPinHandles
 *
 * Mark ready for all non-pending OverlappedData that get moved to default domain.
 *
 */
BOOL TableHandleAsyncPinHandles(HandleTable *pTable);

/*
 * TableRelocateAsyncPinHandles
 *
 * Replaces async pin handles with ones in default domain.
 *
 */
void TableRelocateAsyncPinHandles(HandleTable *pTable, HandleTable *pTargetTable);

/*
 * Check if a handle is part of a HandleTable
 */
BOOL TableContainHandle(HandleTable *pTable, OBJECTHANDLE handle);

/*
 * SegmentRemoveFreeBlocks
 *
 * Removes a block from a block list in a segment.  The block is returned to
 * the segment's free list.
 *
 */
void SegmentRemoveFreeBlocks(TableSegment *pSegment, UINT uType);


/*
 * SegmentResortChains
 *
 * Sorts the block chains for optimal scanning order.
 * Sorts the free list to combat fragmentation.
 *
 */
void SegmentResortChains(TableSegment *pSegment);


/*
 * DoesSegmentNeedsToTrimExcessPages
 *
 * Checks to see if any pages can be decommitted from the segment.
 *
 */
BOOL DoesSegmentNeedsToTrimExcessPages(TableSegment *pSegment);

/*
 * SegmentTrimExcessPages
 *
 * Checks to see if any pages can be decommitted from the segment.
 * In case there any unused pages it goes and decommits them.
 *
 */
void SegmentTrimExcessPages(TableSegment *pSegment);


/*
 * TableAllocBulkHandles
 *
 * Attempts to allocate the requested number of handes of the specified type.
 *
 * Returns the number of handles that were actually allocated.  This is always
 * the same as the number of handles requested except in out-of-memory conditions,
 * in which case it is the number of handles that were successfully allocated.
 *
 */
UINT TableAllocBulkHandles(HandleTable *pTable, UINT uType, OBJECTHANDLE *pHandleBase, UINT uCount);


/*
 * TableFreeBulkPreparedHandles
 *
 * Frees an array of handles of the specified type.
 *
 * This routine is optimized for a sorted array of handles but will accept any order.
 *
 */
void TableFreeBulkPreparedHandles(HandleTable *pTable, UINT uType, OBJECTHANDLE *pHandleBase, UINT uCount);


/*
 * TableFreeBulkUnpreparedHandles
 *
 * Frees an array of handles of the specified type by preparing them and calling TableFreeBulkPreparedHandles.
 *
 */
void TableFreeBulkUnpreparedHandles(HandleTable *pTable, UINT uType, const OBJECTHANDLE *pHandles, UINT uCount);

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * HANDLE CACHE
 *
 ****************************************************************************/

/*
 * TableAllocSingleHandleFromCache
 *
 * Gets a single handle of the specified type from the handle table by
 * trying to fetch it from the reserve cache for that handle type.  If the
 * reserve cache is empty, this routine calls TableCacheMissOnAlloc.
 *
 */
OBJECTHANDLE TableAllocSingleHandleFromCache(HandleTable *pTable, UINT uType);


/*
 * TableFreeSingleHandleToCache
 *
 * Returns a single handle of the specified type to the handle table
 * by trying to store it in the free cache for that handle type.  If the
 * free cache is full, this routine calls TableCacheMissOnFree.
 *
 */
void TableFreeSingleHandleToCache(HandleTable *pTable, UINT uType, OBJECTHANDLE handle);


/*
 * TableAllocHandlesFromCache
 *
 * Allocates multiple handles of the specified type by repeatedly
 * calling TableAllocSingleHandleFromCache.
 *
 */
UINT TableAllocHandlesFromCache(HandleTable *pTable, UINT uType, OBJECTHANDLE *pHandleBase, UINT uCount);


/*
 * TableFreeHandlesToCache
 *
 * Frees multiple handles of the specified type by repeatedly
 * calling TableFreeSingleHandleToCache.
 *
 */
void TableFreeHandlesToCache(HandleTable *pTable, UINT uType, const OBJECTHANDLE *pHandleBase, UINT uCount);

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * TABLE SCANNING
 *
 ****************************************************************************/

/*
 * TableScanHandles
 *
 * Implements the core handle scanning loop for a table.
 *
 */
void CALLBACK TableScanHandles(PTR_HandleTable pTable,
                               const UINT *puType,
                               UINT uTypeCount,
                               SEGMENTITERATOR pfnSegmentIterator,
                               BLOCKSCANPROC pfnBlockHandler,
                               ScanCallbackInfo *pInfo,
                               CrstHolderWithState *pCrstHolder);


/*
 * xxxTableScanHandlesAsync
 *
 * Implements asynchronous handle scanning for a table.
 *
 */
void CALLBACK xxxTableScanHandlesAsync(PTR_HandleTable pTable,
                                       const UINT *puType,
                                       UINT uTypeCount,
                                       SEGMENTITERATOR pfnSegmentIterator,
                                       BLOCKSCANPROC pfnBlockHandler,
                                       ScanCallbackInfo *pInfo,
                                       CrstHolderWithState *pCrstHolder);


/*
 * TypesRequireUserDataScanning
 *
 * Determines whether the set of types listed should get user data during scans
 *
 * if ALL types passed have user data then this function will enable user data support
 * otherwise it will disable user data support
 *
 * IN OTHER WORDS, SCANNING WITH A MIX OF USER-DATA AND NON-USER-DATA TYPES IS NOT SUPPORTED
 *
 */
BOOL TypesRequireUserDataScanning(HandleTable *pTable, const UINT *types, UINT typeCount);


/*
 * BuildAgeMask
 *
 * Builds an age mask to be used when examining/updating the write barrier.
 *
 */
ULONG32 BuildAgeMask(UINT uGen, UINT uMaxGen);


/*
 * QuickSegmentIterator
 *
 * Returns the next segment to be scanned in a scanning loop.
 *
 */
PTR_TableSegment CALLBACK QuickSegmentIterator(PTR_HandleTable pTable, PTR_TableSegment pPrevSegment, CrstHolderWithState *pCrstHolder = 0);


/*
 * StandardSegmentIterator
 *
 * Returns the next segment to be scanned in a scanning loop.
 *
 * This iterator performs some maintenance on the segments,
 * primarily making sure the block chains are sorted so that
 * g0 scans are more likely to operate on contiguous blocks.
 *
 */
PTR_TableSegment CALLBACK StandardSegmentIterator(PTR_HandleTable pTable, PTR_TableSegment pPrevSegment, CrstHolderWithState *pCrstHolder = 0);


/*
 * FullSegmentIterator
 *
 * Returns the next segment to be scanned in a scanning loop.
 *
 * This iterator performs full maintenance on the segments,
 * including freeing those it notices are empty along the way.
 *
 */
PTR_TableSegment CALLBACK FullSegmentIterator(PTR_HandleTable pTable, PTR_TableSegment pPrevSegment, CrstHolderWithState *pCrstHolder = 0);


/*
 * BlockScanBlocksWithoutUserData
 *
 * Calls the specified callback for each handle, optionally aging the corresponding generation clumps.
 * NEVER propagates per-handle user data to the callback.
 *
 */
void CALLBACK BlockScanBlocksWithoutUserData(PTR_TableSegment pSegment, UINT uBlock, UINT uCount, ScanCallbackInfo *pInfo);


/*
 * BlockScanBlocksWithUserData
 *
 * Calls the specified callback for each handle, optionally aging the corresponding generation clumps.
 * ALWAYS propagates per-handle user data to the callback.
 *
 */
void CALLBACK BlockScanBlocksWithUserData(PTR_TableSegment pSegment, UINT uBlock, UINT uCount, ScanCallbackInfo *pInfo);


/*
 * BlockScanBlocksEphemeral
 *
 * Calls the specified callback for each handle from the specified generation.
 * Propagates per-handle user data to the callback if present.
 *
 */
void CALLBACK BlockScanBlocksEphemeral(PTR_TableSegment pSegment, UINT uBlock, UINT uCount, ScanCallbackInfo *pInfo);


/*
 * BlockAgeBlocks
 *
 * Ages all clumps in a range of consecutive blocks.
 *
 */
void CALLBACK BlockAgeBlocks(PTR_TableSegment pSegment, UINT uBlock, UINT uCount, ScanCallbackInfo *pInfo);


/*
 * BlockAgeBlocksEphemeral
 *
 * Ages all clumps within the specified generation.
 *
 */
void CALLBACK BlockAgeBlocksEphemeral(PTR_TableSegment pSegment, UINT uBlock, UINT uCount, ScanCallbackInfo *pInfo);


/*
 * BlockResetAgeMapForBlocks
 *
 * Clears the age maps for a range of blocks.
 *
 */
void CALLBACK BlockResetAgeMapForBlocks(PTR_TableSegment pSegment, UINT uBlock, UINT uCount, ScanCallbackInfo *pInfo);


/*
 * BlockVerifyAgeMapForBlocks
 *
 * Verifies the age maps for a range of blocks, and also validates the objects pointed to.
 *
 */
void CALLBACK BlockVerifyAgeMapForBlocks(PTR_TableSegment pSegment, UINT uBlock, UINT uCount, ScanCallbackInfo *pInfo);


/*
 * xxxAsyncSegmentIterator
 *
 * Implements the core handle scanning loop for a table.
 *
 */
PTR_TableSegment CALLBACK xxxAsyncSegmentIterator(PTR_HandleTable pTable, TableSegment *pPrevSegment, CrstHolderWithState *pCrstHolder);

/*--------------------------------------------------------------------------*/
