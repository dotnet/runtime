// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#ifndef __HANDLETABLECONSTANTS_H__
#define __HANDLETABLECONSTANTS_H__

 // Build support for async pinned handles into standalone GC to make it usable with older runtimes
#if defined(BUILD_AS_STANDALONE) && !defined(FEATURE_NATIVEAOT)
#define FEATURE_ASYNC_PINNED_HANDLES
#endif

#define INITIAL_HANDLE_TABLE_ARRAY_SIZE 10
#define HANDLE_MAX_INTERNAL_TYPES       12

/*--------------------------------------------------------------------------*/

//<TODO>@TODO: find a home for this in a project-level header file</TODO>
#ifndef BITS_PER_BYTE
#define BITS_PER_BYTE               (8)
#endif
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
    #define NEXT_CLUMP_IN_MASK(dw)  ((dw) >> BITS_PER_BYTE)

#else

    // big-endian write barrier mask manipulation
    #define GEN_CLUMP_0_MASK        (0xFF000000)
    #define NEXT_CLUMP_IN_MASK(dw)  ((dw) << BITS_PER_BYTE)

#endif


// if the above numbers change than these will likely change as well
#define HANDLE_HANDLES_PER_CLUMP    (16)        // segment write-barrier granularity
#define HANDLE_HANDLES_PER_BLOCK    (64)        // segment suballocation granularity
#define HANDLE_OPTIMIZE_FOR_64_HANDLE_BLOCKS    // flag for certain optimizations

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
#define HANDLE_HANDLES_PER_MASK         (sizeof(uint32_t) * BITS_PER_BYTE)
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
#define TYPE_INVALID                    ((uint8_t)0xFF)
#define BLOCK_INVALID                   ((uint8_t)0xFF)

/*--------------------------------------------------------------------------*/

#endif // __HANDLETABLECONSTANTS_H__
