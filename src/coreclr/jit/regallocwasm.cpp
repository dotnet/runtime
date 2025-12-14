// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "regallocwasm.h"

RegAllocInterface* GetRegisterAllocator(Compiler* compiler)
{
    return new (compiler->getAllocator(CMK_LSRA)) WasmRegAlloc(compiler);
}

WasmRegAlloc::WasmRegAlloc(Compiler* compiler)
    : m_compiler(compiler)
{
}

PhaseStatus WasmRegAlloc::doRegisterAllocation()
{
    m_compiler->codeGen->setFramePointerUsed(false);
    return PhaseStatus::MODIFIED_NOTHING;
}

void WasmRegAlloc::recordVarLocationsAtStartOfBB(BasicBlock* bb)
{
}

bool WasmRegAlloc::willEnregisterLocalVars() const
{
    return m_compiler->compEnregLocals();
}

#if TRACK_LSRA_STATS
void WasmRegAlloc::dumpLsraStatsCsv(FILE* file)
{
}

void WasmRegAlloc::dumpLsraStatsSummary(FILE* file)
{
}
#endif // TRACK_LSRA_STATS

bool WasmRegAlloc::isRegCandidate(LclVarDsc* varDsc)
{
    NYI_WASM("isRegCandidate");
    return false;
}

bool WasmRegAlloc::isContainableMemoryOp(GenTree* node)
{
    NYI_WASM("isContainableMemoryOp");
    return false;
}
