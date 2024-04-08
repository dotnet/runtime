// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "forward_declarations.h"

class AllocHeap
{
  public:
    AllocHeap();

    bool Init();

    bool Init(uint8_t *    pbInitialMem,
              uintptr_t cbInitialMemCommit,
              uintptr_t cbInitialMemReserve,
              bool       fShouldFreeInitialMem);

    ~AllocHeap();

    // If AllocHeap was created with a MemAccessMgr, pRWAccessHolder must be non-NULL.
    // On return, the holder will permit R/W access to the allocated memory until it
    // is destructed.
    uint8_t * Alloc(uintptr_t cbMem);

    // If AllocHeap was created with a MemAccessMgr, pRWAccessHolder must be non-NULL.
    // On return, the holder will permit R/W access to the allocated memory until it
    // is destructed.
    uint8_t * AllocAligned(uintptr_t cbMem,
                         uintptr_t alignment);

    // Returns true if this AllocHeap owns the memory range [pvMem, pvMem+cbMem)
    bool Contains(void * pvMem,
                  uintptr_t cbMem);

  private:
    // Allocation Helpers
    uint8_t* _Alloc(uintptr_t cbMem, uintptr_t alignment);
    bool _AllocNewBlock(uintptr_t cbMem);
    uint8_t* _AllocFromCurBlock(uintptr_t cbMem, uintptr_t alignment);
    bool _CommitFromCurBlock(uintptr_t cbMem);

    // Access protection helpers
    bool _UpdateMemPtrs(uint8_t* pNextFree, uint8_t* pFreeCommitEnd, uint8_t* pFreeReserveEnd);
    bool _UpdateMemPtrs(uint8_t* pNextFree, uint8_t* pFreeCommitEnd);
    bool _UpdateMemPtrs(uint8_t* pNextFree);

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

