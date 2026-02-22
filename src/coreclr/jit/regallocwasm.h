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

struct TemporaryRegStack
{
    unsigned Count    = 0;
    unsigned MaxCount = 0;

    unsigned Push()
    {
        unsigned index = Count++;
        MaxCount       = max(MaxCount, Count);
        return index;
    }

    unsigned Pop()
    {
        assert(Count > 0);
        return --Count;
    }
};

struct VirtualRegReferences
{
    GenTree*              Nodes[16];
    VirtualRegReferences* Prev = nullptr;
};

class WasmRegAlloc : public RegAllocInterface
{
    Compiler*             m_compiler;
    CodeGenInterface*     m_codeGen;
    BasicBlock*           m_currentBlock;
    VirtualRegStack       m_virtualRegs[static_cast<int>(WasmValueType::Count)];
    unsigned              m_lastVirtualRegRefsCount = 0;
    VirtualRegReferences* m_virtualRegRefs          = nullptr;
    TemporaryRegStack     m_temporaryRegs[static_cast<int>(WasmValueType::Count)];

    // The meaning of these fields is borrowed (partially) from the C ABI for WASM. We define "the SP" to be the local
    // which is used to make calls - the stack on entry to callees. We term "the FP" to be the local which is used to
    // access the fixed potion of the frame. For fixed-size frames (no localloc), these will be the same.
    regNumber m_spReg = REG_NA;
    regNumber m_fpReg = REG_NA;

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
    void      InitializeStackPointer();
    void      AllocateStackPointer();
    void      AllocateFramePointer();
    regNumber AllocateVirtualRegister(var_types type);
    regNumber AllocateVirtualRegister(WasmValueType type);
    regNumber AllocateTemporaryRegister(var_types type);
    regNumber ReleaseTemporaryRegister(var_types type);
    regNumber ReleaseTemporaryRegister(WasmValueType wasmType);

    void CollectReferences();
    void CollectReferencesForBlock(BasicBlock* block);
    void CollectReferencesForNode(GenTree* node);
    void CollectReferencesForDivMod(GenTreeOp* divModNode);
    void CollectReferencesForCall(GenTreeCall* callNode);
    void CollectReferencesForCast(GenTreeOp* castNode);
    void CollectReferencesForBinop(GenTreeOp* binOpNode);
    void CollectReferencesForLclVar(GenTreeLclVar* lclVar);
    void RewriteLocalStackStore(GenTreeLclVarCommon* node);
    void CollectReference(GenTree* node);
    void RequestTemporaryRegisterForMultiplyUsedNode(GenTree* node);
    void RequestInternalRegister(GenTree* node, var_types type);
    void ConsumeTemporaryRegForOperand(GenTree* operand DEBUGARG(const char* reason));
    void ConsumeInternalRegisters(GenTree* node DEBUGARG(const char* reason));

    void ResolveReferences();

    void PublishAllocationResults();
};

using RegAllocImpl = WasmRegAlloc;
