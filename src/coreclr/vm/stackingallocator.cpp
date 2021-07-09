// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// StackingAllocator.cpp -
//

//
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



#include "common.h"
#include "excep.h"

#define INC_COUNTER(_name, _amount)
#define MAX_COUNTER(_name, _amount)

StackingAllocator::StackingAllocator()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE((sizeof(StackBlock) & 7) == 0);
    _ASSERTE((sizeof(Checkpoint) & 7) == 0);

    m_FirstBlock = NULL;
    m_FirstFree = NULL;
    m_DeferredFreeBlock = NULL;

#ifdef _DEBUG
        m_CheckpointDepth = 0;
        m_Allocs = 0;
        m_Checkpoints = 0;
        m_Collapses = 0;
        m_BlockAllocs = 0;
        m_MaxAlloc = 0;
#endif

    Init();
}

StackingAllocator::InitialStackBlock::InitialStackBlock()
{
    m_initialBlockHeader.m_Next = NULL;
    m_initialBlockHeader.m_Length = sizeof(m_dataSpace);
    INDEBUG(m_initialBlockHeader.m_Sentinal = 0);
}

StackingAllocator::~StackingAllocator()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    Clear(&m_InitialBlock.m_initialBlockHeader);

    if (m_DeferredFreeBlock)
    {
        delete [] (char*)m_DeferredFreeBlock;
        m_DeferredFreeBlock = NULL;
    }

#ifdef _DEBUG
        INC_COUNTER(W("Allocs"), m_Allocs);
        INC_COUNTER(W("Checkpoints"), m_Checkpoints);
        INC_COUNTER(W("Collapses"), m_Collapses);
        INC_COUNTER(W("BlockAllocs"), m_BlockAllocs);
        MAX_COUNTER(W("MaxAlloc"), m_MaxAlloc);
#endif
}

void StackingAllocator::Init()
{
    WRAPPER_NO_CONTRACT;

    m_FirstBlock = &m_InitialBlock.m_initialBlockHeader;
    m_FirstFree = m_FirstBlock->GetData();
    _ASSERTE((void*)m_FirstFree == (void*)m_InitialBlock.m_dataSpace);
    m_BytesLeft = static_cast<unsigned>(m_FirstBlock->m_Length);
}

// Lightweight initial checkpoint
Checkpoint StackingAllocator::s_initialCheckpoint;

void StackingAllocator::StoreCheckpoint(Checkpoint *c)
{
    LIMITED_METHOD_CONTRACT;

#ifdef _DEBUG
    m_CheckpointDepth++;
    m_Checkpoints++;
#endif

    // Record previous allocator state in it.
    c->m_OldBlock = m_FirstBlock;
    c->m_OldBytesLeft = m_BytesLeft;
}

void *StackingAllocator::GetCheckpoint()
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

#ifdef _DEBUG
    m_CheckpointDepth++;
    m_Checkpoints++;
#endif

    // As an optimization, initial checkpoints are lightweight (they just return
    // a special marker, s_initialCheckpoint). This is because we know how to restore the
    // allocator state on a Collapse without having to store any additional
    // context info.
    if (m_FirstFree == m_InitialBlock.m_dataSpace)
        return &s_initialCheckpoint;

    // Remember the current allocator state.
    StackBlock *pOldBlock = m_FirstBlock;
    unsigned iOldBytesLeft = m_BytesLeft;

    // Allocate a checkpoint block (just like a normal user request).
    Checkpoint *c = new (this) Checkpoint();

    // Record previous allocator state in it.
    c->m_OldBlock = pOldBlock;
    c->m_OldBytesLeft = iOldBytesLeft;

    // Return the checkpoint marker.
    return c;
}


bool StackingAllocator::AllocNewBlockForBytes(unsigned n)
{
    CONTRACT (bool)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(m_CheckpointDepth > 0);
    }
    CONTRACT_END;

    // already aligned and in the hard case

    _ASSERTE(n % 8 == 0);
    _ASSERTE(n > m_BytesLeft);

    StackBlock* b = NULL;

    // we need a block, but before we allocate a new block
    // we're going to check to see if there is block that we saved
    // rather than return to the OS, if there is such a block
    // and it's big enough we can reuse it now, rather than go to the
    // OS again -- this helps us if we happen to be checkpointing
    // across a block seam very often as in VSWhidbey #100462

    if (m_DeferredFreeBlock != NULL && m_DeferredFreeBlock->m_Length >= n)
    {
        b =  m_DeferredFreeBlock;
        m_DeferredFreeBlock = NULL;

        // b->m_Length doesn't need init because its value is still valid
        // from the original allocation
    }
    else
    {
        // Allocate a block four times as large as the request but with a lower
        // limit of MinBlockSize and an upper limit of MaxBlockSize. If the
        // request is larger than MaxBlockSize then allocate exactly that
        // amount.
        unsigned lower = MinBlockSize;
        size_t allocSize = sizeof(StackBlock) + max(n, min(max(n * 4, lower), MaxBlockSize));

        // Allocate the block.
        // <TODO>@todo: Is it worth implementing a non-thread safe standard heap for
        // this allocator, to get even more MP scalability?</TODO>
        b = (StackBlock *)new (nothrow) char[allocSize];
        if (b == NULL)
            RETURN false;

        // reserve space for the Block structure and then link it in
        b->m_Length = (unsigned) (allocSize - sizeof(StackBlock));

#ifdef _DEBUG
        m_BlockAllocs++;
#endif
     }

     // Link new block to head of block chain and update internal state to
     // start allocating from this new block.
     b->m_Next = m_FirstBlock;
     m_FirstBlock = b;
     m_FirstFree = b->GetData();
     // the cast below is safe because b->m_Length is less than MaxBlockSize (4096)
     m_BytesLeft = static_cast<unsigned>(b->m_Length);

     INDEBUG(b->m_Sentinal = 0);

     RETURN true;
}


void* StackingAllocator::UnsafeAllocSafeThrow(UINT32 Size)
{
    CONTRACT (void*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(ThrowOutOfMemory());
        PRECONDITION(m_CheckpointDepth > 0);
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // OOM fault injection in AllocNoThrow

    void* retval = UnsafeAllocNoThrow(Size);
    if (retval == NULL)
        ENCLOSE_IN_EXCEPTION_HANDLER ( ThrowOutOfMemory );

    RETURN retval;
}

void *StackingAllocator::UnsafeAlloc(UINT32 Size)
{
    CONTRACT (void*)
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(ThrowOutOfMemory());
        PRECONDITION(m_CheckpointDepth > 0);
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // OOM fault injection in AllocNoThrow

    void* retval = UnsafeAllocNoThrow(Size);
    if (retval == NULL)
        ThrowOutOfMemory();

    RETURN retval;
}


void StackingAllocator::Collapse(void *CheckpointMarker)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(m_CheckpointDepth > 0);

#ifdef _DEBUG
    m_CheckpointDepth--;
    m_Collapses++;
#endif

    Checkpoint *c = (Checkpoint *)CheckpointMarker;

    // Special case collapsing back to the initial checkpoint.
    if (c == &s_initialCheckpoint || c->m_OldBlock == NULL) {
        Clear(&m_InitialBlock.m_initialBlockHeader);
        Init();

        // confirm no buffer overruns
        INDEBUG(Validate(m_FirstBlock, m_FirstFree));

        return;
    }

    // Cache contents of checkpoint, we can potentially deallocate it in the
    // next step (if a new block had to be allocated to accomodate the
    // checkpoint).
    StackBlock *pOldBlock = c->m_OldBlock;
    unsigned iOldBytesLeft = c->m_OldBytesLeft;

    // Start deallocating blocks until the block that was at the head on the
    // chain when the checkpoint is taken is there again.
    Clear(pOldBlock);

    // Restore former allocator state.
    m_FirstBlock = pOldBlock;
    m_FirstFree = &pOldBlock->GetData()[pOldBlock->m_Length - iOldBytesLeft];
    m_BytesLeft = iOldBytesLeft;

    // confirm no buffer overruns
    INDEBUG(Validate(m_FirstBlock, m_FirstFree));
}

#ifdef _DEBUG
void StackingAllocator::Validate(StackBlock *block, void* spot)
{
    LIMITED_METHOD_CONTRACT;

    if (!block)
        return;
    _ASSERTE(m_InitialBlock.m_initialBlockHeader.m_Length == sizeof(m_InitialBlock.m_dataSpace));
    Sentinal* ptr = block->m_Sentinal;
    _ASSERTE(spot);
    while(ptr >= spot)
    {
            // If this assert goes off then someone overwrote their buffer!
            // A common candidate is PINVOKE buffer run.  To confirm look
            // up on the stack for NDirect.* Look for the MethodDesc
            // associated with it.  Be very suspicious if it is one that
            // has a return string buffer!.  This usually means the end
            // programmer did not allocate a big enough buffer before passing
            // it to the PINVOKE method.
        if (ptr->m_Marker1 != Sentinal::marker1Val)
            _ASSERTE(!"Memory overrun!! May be bad buffer passed to PINVOKE. turn on logging LF_STUBS level 6 to find method");
        ptr = ptr->m_Next;
    }
    block->m_Sentinal = ptr;
}
#endif // _DEBUG

void StackingAllocator::Clear(StackBlock *ToBlock)
{
    LIMITED_METHOD_CONTRACT;

    StackBlock *p = m_FirstBlock;
    StackBlock *q;

    _ASSERTE(ToBlock != NULL);

    while (p != ToBlock)
    {
        PREFAST_ASSUME(p != NULL);

        q = p;
        p = p->m_Next;

        INDEBUG(Validate(q, q));

        // we don't give the tail block back to the OS
        // because we can get into situations where we're growing
        // back and forth over a single seam for a tiny alloc
        // and the perf is a disaster -- VSWhidbey #100462
        if (m_DeferredFreeBlock != NULL)
        {
            delete [] (char *)m_DeferredFreeBlock;
        }

        m_DeferredFreeBlock = q;
        m_DeferredFreeBlock->m_Next = NULL;
    }
}
void * __cdecl operator new(size_t n, StackingAllocator * alloc)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_FAULT;

#ifdef HOST_64BIT
    // size_t's too big on 64-bit platforms so we check for overflow
    if(n > (size_t)(1<<31)) ThrowOutOfMemory();
#endif
    void *retval = alloc->UnsafeAllocNoThrow((unsigned)n);
    if(retval == NULL) ThrowOutOfMemory();

    return retval;
}

void * __cdecl operator new[](size_t n, StackingAllocator * alloc)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_FAULT;

#ifdef HOST_64BIT
    // size_t's too big on 64-bit platforms so we check for overflow
    if(n > (size_t)(1<<31)) ThrowOutOfMemory();
#else
    if(n == (size_t)-1) ThrowOutOfMemory();    // overflow occurred
#endif

    void *retval = alloc->UnsafeAllocNoThrow((unsigned)n);
    if (retval == NULL)
        ThrowOutOfMemory();

    return retval;
}

void * __cdecl operator new(size_t n, StackingAllocator * alloc, const NoThrow&) throw()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT;

#ifdef HOST_64BIT
    // size_t's too big on 64-bit platforms so we check for overflow
    if(n > (size_t)(1<<31)) return NULL;
#endif

    return alloc->UnsafeAllocNoThrow((unsigned)n);
}

void * __cdecl operator new[](size_t n, StackingAllocator * alloc, const NoThrow&) throw()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_FAULT;

#ifdef HOST_64BIT
    // size_t's too big on 64-bit platforms so we check for overflow
    if(n > (size_t)(1<<31)) return NULL;
#else
    if(n == (size_t)-1) return NULL;    // overflow occurred
#endif

    return alloc->UnsafeAllocNoThrow((unsigned)n);
}

StackingAllocatorHolder::~StackingAllocatorHolder()
{
    m_pStackingAllocator->Collapse(m_checkpointMarker);
    if (m_owner)
    {
        m_thread->m_stackLocalAllocator = NULL;
        m_pStackingAllocator->~StackingAllocator();
    }
}

StackingAllocatorHolder::StackingAllocatorHolder(StackingAllocator *pStackingAllocator, Thread *pThread, bool owner) :
    m_pStackingAllocator(pStackingAllocator),
    m_checkpointMarker(pStackingAllocator->GetCheckpoint()),
    m_thread(pThread),
    m_owner(owner)
{
    if (m_owner)
    {
        m_thread->m_stackLocalAllocator = pStackingAllocator;
    }
}
