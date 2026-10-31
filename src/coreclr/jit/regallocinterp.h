// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

// The interpreter has no registers: every local and temporary lives in a slot on the
// interpreter stack, whose offsets are assigned when bytecode is generated.
class InterpRegAlloc : public RegAllocInterface
{
    Compiler* m_compiler;

public:
    InterpRegAlloc(Compiler* compiler);

    virtual PhaseStatus doRegisterAllocation();
    virtual void        recordVarLocationsAtStartOfBB(BasicBlock* bb);
    virtual bool        willEnregisterLocalVars() const;
#if TRACK_LSRA_STATS
    virtual void dumpLsraStatsCsv(FILE* file);
    virtual void dumpLsraStatsSummary(FILE* file);
#endif // TRACK_LSRA_STATS

    bool isRegCandidate(LclVarDsc* varDsc);
    bool isContainableMemoryOp(GenTree* node);
    void checkForDNER(unsigned lclNum, LclVarDsc* varDsc);
};

using RegAllocImpl = InterpRegAlloc;
