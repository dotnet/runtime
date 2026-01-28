// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

class WasmRegAlloc : public RegAllocInterface
{
    Compiler*   m_compiler;
    BasicBlock* m_currentBlock;

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
    Compiler*   GetCompiler() const;
    LIR::Range& CurrentRange();

    void IdentifyCandidates();

    void AllocateAndResolve();
    void AllocateAndResolveBlock(BasicBlock* block);
    void AllocateAndResolveNode(GenTree* node);
    void RewriteLocalStackStore(GenTreeLclVarCommon* node);

    regNumber AllocateFreeRegister(var_types type);

    void PublishAllocationResults();
};

using RegAllocImpl = WasmRegAlloc;
