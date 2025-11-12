// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "regallocwasm.h"

LinearScanInterface* getLinearScanAllocator(Compiler* compiler)
{
    return new (compiler->getAllocator(CMK_LSRA)) LinearScan(compiler);
}

LinearScan::LinearScan(Compiler* compiler)
    : m_compiler(compiler)
{
}

PhaseStatus LinearScan::doLinearScan()
{
    m_compiler->codeGen->setFramePointerUsed(false);
    return PhaseStatus::MODIFIED_NOTHING;
}

void LinearScan::recordVarLocationsAtStartOfBB(BasicBlock* bb)
{
}

bool LinearScan::willEnregisterLocalVars() const
{
    return m_compiler->compEnregLocals();
}

#if TRACK_LSRA_STATS
void LinearScan::dumpLsraStatsCsv(FILE* file)
{
}

void LinearScan::dumpLsraStatsSummary(FILE* file)
{
}
#endif // TRACK_LSRA_STATS

bool LinearScan::isRegCandidate(LclVarDsc* varDsc)
{
    NYI_WASM("isRegCandidate");
    return false;
}

bool LinearScan::isContainableMemoryOp(GenTree* node)
{
    NYI_WASM("isContainableMemoryOp");
    return false;
}
