// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// StackingAllocator.h -
//

//


#ifndef __stacking_allocator_h__
#define __stacking_allocator_h__

#include "util.hpp"
#include "eecontract.h"


// We use zero sized arrays, disable the non-standard extension warning.
#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4200)
#endif

#ifdef _DEBUG
    struct Sentinel
    {
        enum { marker1Val = 0xBAD00BAD };
        Sentinel(Sentinel* next) : m_Marker1(marker1Val), m_Next(next) { LIMITED_METHOD_CONTRACT; }

        unsigned  m_Marker1;        // just some data bytes
        Sentinel* m_Next;           // linked list of these
    };
#endif

    // Blocks from which allocations are carved. Size is determined dynamically,
    // with upper and lower bounds of MinBlockSize and MaxBlockSize respectively
    // (though large allocation requests will cause a block of exactly the right
    // size to be allocated).
    struct StackBlock
    {
        StackBlock     *m_Next;         // Next oldest block in list
        DWORD_PTR   m_Length;       // Length of block excluding header  (needs to be pointer-sized for alignment on IA64)
        INDEBUG(Sentinel*   m_Sentinel;)    // insure that we don't fall of the end of the buffer
        INDEBUG(void**      m_Pad;)    		// keep the size a multiple of 8
        char *GetData() { return (char *)(this + 1);}
    };

    // Whenever a checkpoint is requested, a checkpoint structure is allocated
    // (as a normal allocation) and is filled with information about the state
    // of the allocator prior to the checkpoint. When a Collapse request comes
    // in we can therefore restore the state of the allocator.
    // It is the address of the checkpoint structure that we hand out to the
    // caller of GetCheckpoint as an opaque checkpoint marker.
    struct Checkpoint
    {
        StackBlock *m_OldBlock;     // Head of block list before checkpoint
        unsigned    m_OldBytesLeft; // Number of free bytes before checkpoint
    };



// Non-thread safe allocator designed for allocations with the following
// pattern:
//      allocate, allocate, allocate ... deallocate all
// There may also be recursive uses of this allocator (by the same thread), so
// the usage becomes:
//      mark checkpoint, allocate, allocate, ..., deallocate back to checkpoint
//
// Allocations come from a singly linked list of blocks with dynamically
// determined size (the goal is to have fewer block allocations than allocation
// requests).
//
// Allocations are very fast (in the case where a new block isn't allocated)
// since blocks are carved up into packets by simply moving a cursor through
// the block.
//
// Allocations are guaranteed to be quadword aligned.
class StackingAllocator
{
public:

    enum
    {
        MinBlockSize    = 0x2000,
        MaxBlockSize    = 0x8000,
    };

private:
    struct InitialStackBlock
    {
        InitialStackBlock();
        StackBlock m_initialBlockHeader;
        char m_dataSpace[0x2000];
    };

public:

#ifndef DACCESS_COMPILE
    StackingAllocator();
    ~StackingAllocator();
#else
    StackingAllocator() { LIMITED_METHOD_CONTRACT; }
#endif

    void StoreCheckpoint(Checkpoint *c);
    void* GetCheckpoint();

    // @todo move this into a .inl file as many class users of this class don't need to include this body
    FORCEINLINE void * UnsafeAllocNoThrow(unsigned Size)
    {
        CONTRACT (void*)
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            INJECT_FAULT(CONTRACT_RETURN NULL;);
            PRECONDITION(m_CheckpointDepth > 0);
            POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        }
        CONTRACT_END;

#ifdef _DEBUG
        m_Allocs++;
        m_MaxAlloc = max(Size, m_MaxAlloc);
#endif

        //special case, 0 size alloc, return non-null but invalid pointer
        if (Size == 0)
        {
            RETURN (void*)-1;
        }

        // Round size up to ensure alignment.
        unsigned n = (Size + 7) & ~7;
        if(n < Size)
        {
            return NULL;
        }

        // leave room for sentinel
        INDEBUG(n += sizeof(Sentinel));

        // Is the request too large for the current block?
        if (n > m_BytesLeft)
        {
            if (!AllocNewBlockForBytes(n))
            {
                RETURN NULL;
            }
        }

        // Once we get here we know we have enough bytes left in the block at the
        // head of the chain.
        _ASSERTE(n <= m_BytesLeft);

        void *ret = m_FirstFree;
        m_FirstFree += n;
        m_BytesLeft -= n;

#ifdef _DEBUG
        // Add sentinel to the end
        m_FirstBlock->m_Sentinel = new(m_FirstFree - sizeof(Sentinel)) Sentinel(m_FirstBlock->m_Sentinel);
#endif

        RETURN ret;
    }

    FORCEINLINE void * AllocNoThrow(S_UINT32 size)
    {
        // Safely round size up to ensure alignment.
        if(size.IsOverflow()) return NULL;

        return UnsafeAllocNoThrow(size.Value());
    }

    FORCEINLINE void * AllocSafeThrow(S_UINT32 size){
        WRAPPER_NO_CONTRACT;

        if(size.IsOverflow()) ThrowOutOfMemory();

        return UnsafeAllocSafeThrow(size.Value());
    }

    FORCEINLINE void * Alloc(S_UINT32 size){
        WRAPPER_NO_CONTRACT;

        if(size.IsOverflow()) ThrowOutOfMemory();

        return UnsafeAlloc(size.Value());
    }

    void  Collapse(void* CheckpointMarker);

    void* UnsafeAllocSafeThrow(UINT32 size);
    void* UnsafeAlloc(UINT32 size);

private:

    bool AllocNewBlockForBytes(unsigned n);

    StackBlock       *m_FirstBlock;       // Pointer to head of allocation block list
    char             *m_FirstFree;        // Pointer to first free byte in head block
    unsigned          m_BytesLeft;        // Number of free bytes left in head block
    InitialStackBlock m_InitialBlock;     // The first block is special, we never free it
    StackBlock       *m_DeferredFreeBlock; // Avoid going to the OS too often by deferring one free

#ifdef _DEBUG
    unsigned    m_CheckpointDepth;
    unsigned    m_Allocs;
    unsigned    m_Checkpoints;
    unsigned    m_Collapses;
    unsigned    m_BlockAllocs;
    unsigned    m_MaxAlloc;
#endif

    void Init();

#ifdef _DEBUG
    void Validate(StackBlock *block, void* spot);
#endif

    void Clear(StackBlock *ToBlock);

private :
    static Checkpoint s_initialCheckpoint;
};

#define ACQUIRE_STACKING_ALLOCATOR(stackingAllocatorName)  \
  Thread *pThread__ACQUIRE_STACKING_ALLOCATOR = GetThread(); \
  StackingAllocator *stackingAllocatorName = pThread__ACQUIRE_STACKING_ALLOCATOR->m_stackLocalAllocator; \
  bool allocatorOwner__ACQUIRE_STACKING_ALLOCATOR = false; \
  NewHolder<StackingAllocator> heapAllocatedStackingBuffer__ACQUIRE_STACKING_ALLOCATOR; \
\
  if (stackingAllocatorName == NULL) \
  { \
      if (pThread__ACQUIRE_STACKING_ALLOCATOR->CheckCanUseStackAlloc()) \
      { \
          stackingAllocatorName = new (_alloca(sizeof(StackingAllocator))) StackingAllocator; \
      } \
      else \
      {\
          stackingAllocatorName = new (nothrow) StackingAllocator; \
          if (stackingAllocatorName == NULL) \
              ThrowOutOfMemory(); \
          heapAllocatedStackingBuffer__ACQUIRE_STACKING_ALLOCATOR = stackingAllocatorName; \
      }\
      allocatorOwner__ACQUIRE_STACKING_ALLOCATOR = true; \
  } \
  StackingAllocatorHolder sah_ACQUIRE_STACKING_ALLOCATOR(stackingAllocatorName, pThread__ACQUIRE_STACKING_ALLOCATOR, allocatorOwner__ACQUIRE_STACKING_ALLOCATOR)

class Thread;
class StackingAllocatorHolder
{
    StackingAllocator *m_pStackingAllocator;
    void* m_checkpointMarker;
    Thread* m_thread;
    bool m_owner;

    public:
    ~StackingAllocatorHolder();
    StackingAllocatorHolder(StackingAllocator *pStackingAllocator, Thread *pThread, bool owner);
    StackingAllocator *GetStackingAllocator() { return m_pStackingAllocator; }
    StackingAllocator &operator->() { return *m_pStackingAllocator; }
};


void * __cdecl operator new(size_t n, StackingAllocator *alloc);
void * __cdecl operator new[](size_t n, StackingAllocator *alloc);
void * __cdecl operator new(size_t n, StackingAllocator *alloc, const NoThrow&) throw();
void * __cdecl operator new[](size_t n, StackingAllocator *alloc, const NoThrow&) throw();

#ifdef _MSC_VER
#pragma warning(pop)
#endif

#endif
