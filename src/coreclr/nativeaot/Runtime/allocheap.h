// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "forward_declarations.h"

class AllocHeap
{
  public:
    AllocHeap();

    bool Init();

    void Destroy();

    // If AllocHeap was created with a MemAccessMgr, pRWAccessHolder must be non-NULL.
    // On return, the holder will permit R/W access to the allocated memory until it
    // is destructed.
    uint8_t * Alloc(uintptr_t cbMem);

    // If AllocHeap was created with a MemAccessMgr, pRWAccessHolder must be non-NULL.
    // On return, the holder will permit R/W access to the allocated memory until it
    // is destructed.
    uint8_t * AllocAligned(uintptr_t cbMem,
                         uintptr_t alignment);

  private:
    // Allocation Helpers
    bool _AllocNewBlock(uintptr_t cbMem, uintptr_t alignment);
    uint8_t* _AllocFromCurBlock(uintptr_t cbMem, uintptr_t alignment);

    static const uintptr_t BLOCK_SIZE = 4096;

    struct BlockListElem
    {
        BlockListElem* m_pNext;
        uintptr_t m_cbMem;

        uintptr_t GetLength() const { return m_cbMem; }
        uint8_t* GetDataStart() const { return (uint8_t*)this + sizeof(BlockListElem); }
    };

    typedef SList<BlockListElem>    BlockList;
    BlockList                       m_blockList;

    uintptr_t                       m_cbCurBlockUsed;

    CrstStatic                      m_lock;

    INDEBUG(bool                    m_fIsInit;)
};
typedef DPTR(AllocHeap) PTR_AllocHeap;

//-------------------------------------------------------------------------------------------------
void * __cdecl operator new(size_t n, AllocHeap * alloc);
void * __cdecl operator new[](size_t n, AllocHeap * alloc);

