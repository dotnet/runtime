// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * Generational GC handle manager.  Internal Implementation Header.
 *
 * Shared defines and declarations for handle table implementation.
 *

 *
 */

#include "common.h"

#include "handletable.h"

#include "handletableconstants.h"



/****************************************************************************
 *
 * CORE TABLE LAYOUT STRUCTURES
 *
 ****************************************************************************/

/*
 * we need byte packing for the handle table layout to work
 */
#pragma pack(push,1)


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
     * WARNING: Although this array is logically organized as a uint8_t[], it is sometimes
     *  accessed as uint32_t[] when processing bytes in parallel.  Code which treats the
     *  array as an array of ULONG32s must handle big/little endian issues itself.
     */
    uint8_t rgGeneration[HANDLE_BLOCKS_PER_SEGMENT * sizeof(uint32_t) / sizeof(uint8_t)];

    /*
     * Block Allocation Chains
     *
     * Each slot indexes the next block in an allocation chain.
     */
    uint8_t rgAllocation[HANDLE_BLOCKS_PER_SEGMENT];

    /*
     * Block Free Masks
     *
     * Masks - 1 bit for every handle in the segment.
     */
    uint32_t rgFreeMask[HANDLE_MASKS_PER_SEGMENT];

    /*
     * Block Handle Types
     *
     * Each slot holds the handle type of the associated block.
     */
    uint8_t rgBlockType[HANDLE_BLOCKS_PER_SEGMENT];

    /*
     * Block User Data Map
     *
     * Each slot holds the index of a user data block (if any) for the associated block.
     */
    uint8_t rgUserData[HANDLE_BLOCKS_PER_SEGMENT];

    /*
     * Block Lock Count
     *
     * Each slot holds a lock count for its associated block.
     * Locked blocks are not freed, even when empty.
     */
    uint8_t rgLocks[HANDLE_BLOCKS_PER_SEGMENT];

    /*
     * Allocation Chain Tails
     *
     * Each slot holds the tail block index for an allocation chain.
     */
    uint8_t rgTail[HANDLE_MAX_INTERNAL_TYPES];

    /*
     * Allocation Chain Hints
     *
     * Each slot holds a hint block index for an allocation chain.
     */
    uint8_t rgHint[HANDLE_MAX_INTERNAL_TYPES];

    /*
     * Free Count
     *
     * Each slot holds the number of free handles in an allocation chain.
     */
    uint32_t rgFreeCount[HANDLE_MAX_INTERNAL_TYPES];

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
    uint8_t fResortChains      : 1;    // allocation chains need sorting
    uint8_t fNeedsScavenging   : 1;    // free blocks need scavenging
    uint8_t _fUnused           : 6;    // unused

    /*
     * Free List Head
     *
     * Index of the first free block in the segment.
     */
    uint8_t bFreeList;

    /*
     * Empty Line
     *
     * Index of the first KNOWN block of the last group of unused blocks in the segment.
     */
    uint8_t bEmptyLine;

    /*
     * Commit Line
     *
     * Index of the first uncommitted block in the segment.
     */
    uint8_t bCommitLine;

    /*
     * Decommit Line
     *
     * Index of the first block in the highest committed page of the segment.
     */
    uint8_t bDecommitLine;

    /*
     * Sequence
     *
     * Indicates the segment sequence number.
     */
    uint8_t bSequence;
};

typedef DPTR(struct _TableSegmentHeader) PTR__TableSegmentHeader;
typedef DPTR(uintptr_t) PTR_uintptr_t;

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
    uint8_t rgUnused[HANDLE_HEADER_SIZE - sizeof(_TableSegmentHeader)];

    /*
     * Handles
     */
    _UNCHECKED_OBJECTREF rgValue[HANDLE_HANDLES_PER_SEGMENT];

#ifdef DACCESS_COMPILE
    static uint32_t DacSize(TADDR addr);
#endif
};

typedef SPTR(struct TableSegment) PTR_TableSegment;

/*
 * restore default packing
 */
#pragma pack(pop)


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
    int32_t lReserveIndex;


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
    int32_t lFreeIndex;
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
    uint32_t         uFlags;            // HNDGCF_* flags
    BOOL             fEnumUserData;     // whether user data is being enumerated as well
    HANDLESCANPROC   pfnScan;           // per-handle scan callback
    uintptr_t        param1;            // callback param 1
    uintptr_t        param2;            // callback param 2
    uint32_t         dwAgeMask;         // generation mask for ephemeral GCs

#ifdef _DEBUG
    uint32_t DEBUG_BlocksScanned;
    uint32_t DEBUG_BlocksScannedNonTrivially;
    uint32_t DEBUG_HandleSlotsScanned;
    uint32_t DEBUG_HandlesActuallyScanned;
#endif
};


/*
 * BLOCKSCANPROC
 *
 * Prototype for callbacks that implement per-block scanning logic.
 *
 */
typedef void (CALLBACK *BLOCKSCANPROC)(PTR_TableSegment pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *pInfo);


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
                                       const uint32_t *puType, uint32_t uTypeCount,
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
    uint32_t rgTypeFlags[HANDLE_MAX_INTERNAL_TYPES];

    /*
     * head of segment list for this table
     */
    PTR_TableSegment pSegmentList;

    /*
     * lock for this table
     */
    CrstStatic Lock;

    /*
     * number of types this table supports
     */
    uint32_t uTypeCount;

    /*
     * number of handles owned by this table that are marked as "used"
     * (this includes the handles residing in rgMainCache and rgQuickCache)
     */
    uint32_t dwCount;

    /*
     * information on current async scan (if any)
     */
    AsyncScanInfo *pAsyncScanInfo;

    /*
     * per-table user info
     */
    uint32_t uTableIndex;

    /*
     * one-level per-type 'quick' handle cache
     */
    OBJECTHANDLE rgQuickCache[HANDLE_MAX_INTERNAL_TYPES];   // interlocked ops used here

    /*
     * debug-only statistics
     */
#ifdef _DEBUG
    int     _DEBUG_iMaxGen;
    int64_t _DEBUG_TotalBlocksScanned            [MAXSTATGEN];
    int64_t _DEBUG_TotalBlocksScannedNonTrivially[MAXSTATGEN];
    int64_t _DEBUG_TotalHandleSlotsScanned       [MAXSTATGEN];
    int64_t _DEBUG_TotalHandlesActuallyScanned   [MAXSTATGEN];
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
typedef int (*PFNCOMPARE)(uintptr_t p, uintptr_t q);


/*
 * A 32/64 neutral quicksort
 *<TODO>
 * @TODO: move/merge into common util file
 *</TODO>
 */
void QuickSort(uintptr_t *pData, int left, int right, PFNCOMPARE pfnCompare);


/*
 * CompareHandlesByFreeOrder
 *
 * Returns:
 *  <0 - handle P should be freed before handle Q
 *  =0 - handles are equivalent for free order purposes
 *  >0 - handle Q should be freed before handle P
 *
 */
int CompareHandlesByFreeOrder(uintptr_t p, uintptr_t q);

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
__inline BOOL TypeHasUserData(HandleTable *pTable, uint32_t uType)
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
__inline BOOL BlockIsLocked(TableSegment *pSegment, uint32_t uBlock)
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
__inline void BlockLock(TableSegment *pSegment, uint32_t uBlock)
{
    LIMITED_METHOD_CONTRACT;

    // fetch the old lock count
    uint8_t bLocks = pSegment->rgLocks[uBlock];

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
__inline void BlockUnlock(TableSegment *pSegment, uint32_t uBlock)
{
    LIMITED_METHOD_CONTRACT;

    // fetch the old lock count
    uint8_t bLocks = pSegment->rgLocks[uBlock];

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
PTR_uintptr_t BlockFetchUserDataPointer(PTR__TableSegmentHeader pSegment, uint32_t uBlock, BOOL fAssertOnError);


/*
 * HandleValidateAndFetchUserDataPointer
 *
 * Gets the user data pointer for a handle.
 * ASSERTs and returns NULL if handle is not of the expected type.
 *
 */
uintptr_t *HandleValidateAndFetchUserDataPointer(OBJECTHANDLE handle, uint32_t uTypeExpected);


/*
 * HandleQuickFetchUserDataPointer
 *
 * Gets the user data pointer for a handle.
 * Less validation is performed.
 *
 */
PTR_uintptr_t HandleQuickFetchUserDataPointer(OBJECTHANDLE handle);


/*
 * HandleQuickSetUserData
 *
 * Stores user data with a handle.
 * Less validation is performed.
 *
 */
void HandleQuickSetUserData(OBJECTHANDLE handle, uintptr_t lUserData);


/*
 * HandleFetchType
 *
 * Computes the type index for a given handle.
 *
 */
uint32_t HandleFetchType(OBJECTHANDLE handle);


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
void SegmentRemoveFreeBlocks(TableSegment *pSegment, uint32_t uType);


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
 * Attempts to allocate the requested number of handles of the specified type.
 *
 * Returns the number of handles that were actually allocated.  This is always
 * the same as the number of handles requested except in out-of-memory conditions,
 * in which case it is the number of handles that were successfully allocated.
 *
 */
uint32_t TableAllocBulkHandles(HandleTable *pTable, uint32_t uType, OBJECTHANDLE *pHandleBase, uint32_t uCount);


/*
 * TableFreeBulkPreparedHandles
 *
 * Frees an array of handles of the specified type.
 *
 * This routine is optimized for a sorted array of handles but will accept any order.
 *
 */
void TableFreeBulkPreparedHandles(HandleTable *pTable, uint32_t uType, OBJECTHANDLE *pHandleBase, uint32_t uCount);


/*
 * TableFreeBulkUnpreparedHandles
 *
 * Frees an array of handles of the specified type by preparing them and calling TableFreeBulkPreparedHandles.
 *
 */
void TableFreeBulkUnpreparedHandles(HandleTable *pTable, uint32_t uType, const OBJECTHANDLE *pHandles, uint32_t uCount);

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
OBJECTHANDLE TableAllocSingleHandleFromCache(HandleTable *pTable, uint32_t uType);


/*
 * TableFreeSingleHandleToCache
 *
 * Returns a single handle of the specified type to the handle table
 * by trying to store it in the free cache for that handle type.  If the
 * free cache is full, this routine calls TableCacheMissOnFree.
 *
 */
void TableFreeSingleHandleToCache(HandleTable *pTable, uint32_t uType, OBJECTHANDLE handle);


/*
 * TableAllocHandlesFromCache
 *
 * Allocates multiple handles of the specified type by repeatedly
 * calling TableAllocSingleHandleFromCache.
 *
 */
uint32_t TableAllocHandlesFromCache(HandleTable *pTable, uint32_t uType, OBJECTHANDLE *pHandleBase, uint32_t uCount);


/*
 * TableFreeHandlesToCache
 *
 * Frees multiple handles of the specified type by repeatedly
 * calling TableFreeSingleHandleToCache.
 *
 */
void TableFreeHandlesToCache(HandleTable *pTable, uint32_t uType, const OBJECTHANDLE *pHandleBase, uint32_t uCount);

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
                               const uint32_t *puType,
                               uint32_t uTypeCount,
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
                                       const uint32_t *puType,
                                       uint32_t uTypeCount,
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
BOOL TypesRequireUserDataScanning(HandleTable *pTable, const uint32_t *types, uint32_t typeCount);


/*
 * BuildAgeMask
 *
 * Builds an age mask to be used when examining/updating the write barrier.
 *
 */
uint32_t BuildAgeMask(uint32_t uGen, uint32_t uMaxGen);


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
void CALLBACK BlockScanBlocksWithoutUserData(PTR_TableSegment pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *pInfo);


/*
 * BlockScanBlocksWithUserData
 *
 * Calls the specified callback for each handle, optionally aging the corresponding generation clumps.
 * ALWAYS propagates per-handle user data to the callback.
 *
 */
void CALLBACK BlockScanBlocksWithUserData(PTR_TableSegment pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *pInfo);


/*
 * BlockScanBlocksEphemeral
 *
 * Calls the specified callback for each handle from the specified generation.
 * Propagates per-handle user data to the callback if present.
 *
 */
void CALLBACK BlockScanBlocksEphemeral(PTR_TableSegment pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *pInfo);


/*
 * BlockAgeBlocks
 *
 * Ages all clumps in a range of consecutive blocks.
 *
 */
void CALLBACK BlockAgeBlocks(PTR_TableSegment pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *pInfo);


/*
 * BlockAgeBlocksEphemeral
 *
 * Ages all clumps within the specified generation.
 *
 */
void CALLBACK BlockAgeBlocksEphemeral(PTR_TableSegment pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *pInfo);


/*
 * BlockResetAgeMapForBlocks
 *
 * Clears the age maps for a range of blocks.
 *
 */
void CALLBACK BlockResetAgeMapForBlocks(PTR_TableSegment pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *pInfo);


/*
 * BlockVerifyAgeMapForBlocks
 *
 * Verifies the age maps for a range of blocks, and also validates the objects pointed to.
 *
 */
void CALLBACK BlockVerifyAgeMapForBlocks(PTR_TableSegment pSegment, uint32_t uBlock, uint32_t uCount, ScanCallbackInfo *pInfo);


/*
 * xxxAsyncSegmentIterator
 *
 * Implements the core handle scanning loop for a table.
 *
 */
PTR_TableSegment CALLBACK xxxAsyncSegmentIterator(PTR_HandleTable pTable, TableSegment *pPrevSegment, CrstHolderWithState *pCrstHolder);

/*--------------------------------------------------------------------------*/
