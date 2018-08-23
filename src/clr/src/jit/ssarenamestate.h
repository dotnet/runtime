// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "jitstd.h"

// Fixed-size array that can hold elements with no default constructor;
// it will construct them all by forwarding whatever arguments are
// supplied to its constructor.
template <typename T, int N>
class ConstructedArray
{
    union {
        // Storage that gets used to hold the T objects.
        unsigned char bytes[N * sizeof(T)];

#if defined(_MSC_VER) && (_MSC_VER < 1900)
        // With MSVC pre-VS2015, the code in the #else branch would hit error C2621,
        // so in that case just count on pointer alignment being sufficient
        // (currently T is only ever instantiated as jitstd::list<SsaRenameStateForBlock>)

        // Unused (except to impart alignment requirement)
        void* pointer;
#else
        // Unused (except to impart alignment requirement)
        T alignedArray[N];
#endif // defined(_MSC_VER) && (_MSC_VER < 1900)
    };

public:
    T& operator[](size_t i)
    {
        return *(reinterpret_cast<T*>(bytes + i * sizeof(T)));
    }

    template <typename... Args>
    ConstructedArray(Args&&... args)
    {
        for (int i = 0; i < N; ++i)
        {
            new (bytes + i * sizeof(T), jitstd::placement_t()) T(jitstd::forward<Args>(args)...);
        }
    }

    ~ConstructedArray()
    {
        for (int i = 0; i < N; ++i)
        {
            operator[](i).~T();
        }
    }
};

struct SsaRenameStateForBlock
{
    BasicBlock* m_bb;
    unsigned    m_count;

    SsaRenameStateForBlock(BasicBlock* bb, unsigned count) : m_bb(bb), m_count(count)
    {
    }
    SsaRenameStateForBlock() : m_bb(nullptr), m_count(0)
    {
    }
};

// A record indicating that local "m_loc" was defined in block "m_bb".
struct SsaRenameStateLocDef
{
    BasicBlock* m_bb;
    unsigned    m_lclNum;

    SsaRenameStateLocDef(BasicBlock* bb, unsigned lclNum) : m_bb(bb), m_lclNum(lclNum)
    {
    }
};

struct SsaRenameState
{
    typedef jitstd::list<SsaRenameStateForBlock> Stack;
    typedef Stack**                              Stacks;
    typedef jitstd::list<SsaRenameStateLocDef>   DefStack;

    SsaRenameState(CompAllocator allocator, unsigned lvaCount, bool byrefStatesMatchGcHeapStates);

    void EnsureStacks();

    // Requires "lclNum" to be a variable number for which an ssa number at the top of the
    // stack is required i.e., for variable "uses."
    unsigned CountForUse(unsigned lclNum);

    // Requires "lclNum" to be a variable number, and requires "count" to represent
    // an ssa number, that needs to be pushed on to the stack corresponding to the lclNum.
    void Push(BasicBlock* bb, unsigned lclNum, unsigned count);

    // Pop all stacks that have an entry for "bb" on top.
    void PopBlockStacks(BasicBlock* bb);

    // Similar functions for the special implicit memory variable.
    unsigned CountForMemoryUse(MemoryKind memoryKind)
    {
        if ((memoryKind == GcHeap) && byrefStatesMatchGcHeapStates)
        {
            // Share rename stacks in this configuration.
            memoryKind = ByrefExposed;
        }
        return memoryStack[memoryKind].back().m_count;
    }

    void PushMemory(MemoryKind memoryKind, BasicBlock* bb, unsigned count)
    {
        if ((memoryKind == GcHeap) && byrefStatesMatchGcHeapStates)
        {
            // Share rename stacks in this configuration.
            memoryKind = ByrefExposed;
        }
        memoryStack[memoryKind].push_back(SsaRenameStateForBlock(bb, count));
    }

    void PopBlockMemoryStack(MemoryKind memoryKind, BasicBlock* bb);

#ifdef DEBUG
    // Debug interface
    void DumpStacks();
#endif

private:
    // Map of lclNum -> SsaRenameStateForBlock.
    Stacks stacks;

    // This list represents the set of locals defined in the current block.
    DefStack definedLocs;

    // Same state for the special implicit memory variables.
    ConstructedArray<Stack, MemoryKindCount> memoryStack;

    // Number of stacks/counts to allocate.
    unsigned lvaCount;

    // Allocator to allocate stacks.
    CompAllocator m_alloc;

    // Indicates whether GcHeap and ByrefExposed use the same state.
    bool byrefStatesMatchGcHeapStates;
};
