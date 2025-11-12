// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

// TODO-WASM-Factoring: rename the abstractions related to register allocation to make them less LSRA-specific.
class LinearScan : public LinearScanInterface
{
    Compiler* m_compiler;

public:
    LinearScan(Compiler* compiler);

    virtual PhaseStatus doLinearScan();
    virtual void        recordVarLocationsAtStartOfBB(BasicBlock* bb);
    virtual bool        willEnregisterLocalVars() const;
#if TRACK_LSRA_STATS
    virtual void dumpLsraStatsCsv(FILE* file);
    virtual void dumpLsraStatsSummary(FILE* file);
#endif // TRACK_LSRA_STATS

    bool isRegCandidate(LclVarDsc* varDsc);
    bool isContainableMemoryOp(GenTree* node);
};
