// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

class VirtualRegStack
{
    WasmValueType m_type;
    unsigned      m_nextIndex = 0;

public:
    VirtualRegStack()
        : m_type(WasmValueType::Invalid)
    {
    }
    VirtualRegStack(WasmValueType type)
        : m_type(type)
    {
    }

    bool IsInitialized() const
    {
        return m_type != WasmValueType::Invalid;
    }

    WasmValueType GetType()
    {
        assert(IsInitialized());
        return m_type;
    }

    unsigned Count() const
    {
        return m_nextIndex;
    }

    regNumber Push()
    {
        assert(IsInitialized());
        return MakeWasmReg(m_nextIndex++, m_type);
    }

    void Pop()
    {
        assert(IsInitialized());
        assert(m_nextIndex != 0);
        m_nextIndex--;
    }
};

struct VirtualRegReferences
{
    GenTreeLclVarCommon*  Nodes[16];
    VirtualRegReferences* Prev = nullptr;
};

class WasmRegAlloc : public RegAllocInterface
{
    Compiler*             m_compiler;
    BasicBlock*           m_currentBlock;
    VirtualRegStack       m_virtualRegs[static_cast<int>(WasmValueType::Count)];
    unsigned              m_lastVirtualRegRefsCount = 0;
    VirtualRegReferences* m_virtualRegRefs          = nullptr;

    // The meaning of these fields is borrowed (partially) from the C ABI for WASM. We define "the SP" to be the local
    // which is used to make calls - the stack on entry to callees. We term "the FP" to be the local which is used to
    // access the fixed potion of the frame. For fixed-size frames (no localloc), these will be the same.
    LclVarDsc* m_spVarDsc = nullptr;
    regNumber  m_fpReg    = REG_NA;

public:
    WasmRegAlloc(Compiler* compiler);

    virtual PhaseStatus doRegisterAllocation();
    virtual void        recordVarLocationsAtStartOfBB(BasicBlock* bb);
    virtual bool        willEnregisterLocalVars() const;
#if TRACK_LSRA_STATS
    virtual void dumpLsraStatsCsv(FILE* file);
    virtual void dumpLsraStatsSummary(FILE* file);
#endif // TRACK_LSRA_STATS

    bool isRegCandidate(LclVarDsc* varDsc);
    bool isContainableMemoryOp(GenTree* node);

private:
    LIR::Range& CurrentRange();

    void      IdentifyCandidates();
    void      InitializeCandidate(LclVarDsc* varDsc);
    regNumber AllocateStackPointer();
    void      AllocateFramePointer();
    regNumber AllocateVirtualRegister(var_types type);

    void CollectReferences();
    void CollectReferencesForBlock(BasicBlock* block);
    void CollectReferencesForNode(GenTree* node);
    void RewriteLocalStackStore(GenTreeLclVarCommon* node);
    void CollectReference(GenTreeLclVarCommon* node);

    void ResolveReferences();

    void PublishAllocationResults();
};

using RegAllocImpl = WasmRegAlloc;
