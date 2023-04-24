// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "forward_declarations.h"

#ifdef FEATURE_RWX_MEMORY
#define WRITE_ACCESS_HOLDER_ARG                 , rh::util::WriteAccessHolder *pRWAccessHolder
#define WRITE_ACCESS_HOLDER_ARG_NULL_DEFAULT    , rh::util::WriteAccessHolder *pRWAccessHolder = NULL
#define PASS_WRITE_ACCESS_HOLDER_ARG            , pRWAccessHolder
#else // FEATURE_RWX_MEMORY
#define WRITE_ACCESS_HOLDER_ARG
#define WRITE_ACCESS_HOLDER_ARG_NULL_DEFAULT
#define PASS_WRITE_ACCESS_HOLDER_ARG
#endif // FEATURE_RWX_MEMORY

class AllocHeap
{
  public:
    AllocHeap();

#ifdef FEATURE_RWX_MEMORY
    // If pAccessMgr is non-NULL, it will be used to manage R/W access to the memory allocated.
    AllocHeap(uint32_t rwProtectType = PAGE_READWRITE,
              uint32_t roProtectType = 0, // 0 indicates "same as rwProtectType"
              rh::util::MemAccessMgr* pAccessMgr = NULL);
#endif // FEATURE_RWX_MEMORY

    bool Init();

    bool Init(uint8_t *    pbInitialMem,
              uintptr_t cbInitialMemCommit,
              uintptr_t cbInitialMemReserve,
              bool       fShouldFreeInitialMem);

    ~AllocHeap();

    // If AllocHeap was created with a MemAccessMgr, pRWAccessHolder must be non-NULL.
    // On return, the holder will permit R/W access to the allocated memory until it
    // is destructed.
    uint8_t * Alloc(uintptr_t cbMem WRITE_ACCESS_HOLDER_ARG_NULL_DEFAULT);

    // If AllocHeap was created with a MemAccessMgr, pRWAccessHolder must be non-NULL.
    // On return, the holder will permit R/W access to the allocated memory until it
    // is destructed.
    uint8_t * AllocAligned(uintptr_t cbMem,
                         uintptr_t alignment
                         WRITE_ACCESS_HOLDER_ARG_NULL_DEFAULT);

    // Returns true if this AllocHeap owns the memory range [pvMem, pvMem+cbMem)
    bool Contains(void * pvMem,
                  uintptr_t cbMem);

#ifdef FEATURE_RWX_MEMORY
    // Used with previously-allocated memory for which RW access is needed again.
    // Returns true on success. R/W access will be granted until the holder is
    // destructed.
    bool AcquireWriteAccess(void* pvMem,
                            uintptr_t cbMem,
                            rh::util::WriteAccessHolder* pHolder);
#endif // FEATURE_RWX_MEMORY

  private:
    // Allocation Helpers
    uint8_t* _Alloc(uintptr_t cbMem, uintptr_t alignment WRITE_ACCESS_HOLDER_ARG);
    bool _AllocNewBlock(uintptr_t cbMem);
    uint8_t* _AllocFromCurBlock(uintptr_t cbMem, uintptr_t alignment WRITE_ACCESS_HOLDER_ARG);
    bool _CommitFromCurBlock(uintptr_t cbMem);

    // Access protection helpers
#ifdef FEATURE_RWX_MEMORY
    bool _AcquireWriteAccess(uint8_t* pvMem, uintptr_t cbMem, rh::util::WriteAccessHolder* pHolder);
#endif // FEATURE_RWX_MEMORY
    bool _UpdateMemPtrs(uint8_t* pNextFree, uint8_t* pFreeCommitEnd, uint8_t* pFreeReserveEnd);
    bool _UpdateMemPtrs(uint8_t* pNextFree, uint8_t* pFreeCommitEnd);
    bool _UpdateMemPtrs(uint8_t* pNextFree);
    bool _UseAccessManager() { return m_rwProtectType != m_roProtectType; }

    static const uintptr_t s_minBlockSize = OS_PAGE_SIZE;

    typedef rh::util::MemRange Block;
    typedef DPTR(Block) PTR_Block;
    struct BlockListElem : public Block
    {
        BlockListElem(Block const & block)
            : Block(block)
            {}

        BlockListElem(uint8_t * pbMem, uintptr_t  cbMem)
            : Block(pbMem, cbMem)
            {}

        Block       m_block;
        PTR_Block   m_pNext;
    };

    typedef SList<BlockListElem>    BlockList;
    BlockList                       m_blockList;

    uint32_t                          m_rwProtectType; // READ/WRITE/EXECUTE/etc
    uint32_t                          m_roProtectType; // What to do with fully allocated and initialized pages.

#ifdef FEATURE_RWX_MEMORY
    rh::util::MemAccessMgr*         m_pAccessMgr;
    rh::util::WriteAccessHolder     m_hCurPageRW;   // Used to hold RW access to the current allocation page
                                                    // Passed as pHint to MemAccessMgr::AcquireWriteAccess.
#endif // FEATURE_RWX_MEMORY
    uint8_t *                         m_pNextFree;
    uint8_t *                         m_pFreeCommitEnd;
    uint8_t *                         m_pFreeReserveEnd;

    uint8_t *                         m_pbInitialMem;
    bool                            m_fShouldFreeInitialMem;

    Crst                            m_lock;

    INDEBUG(bool                    m_fIsInit;)
};
typedef DPTR(AllocHeap) PTR_AllocHeap;

//-------------------------------------------------------------------------------------------------
void * __cdecl operator new(size_t n, AllocHeap * alloc);
void * __cdecl operator new[](size_t n, AllocHeap * alloc);

