// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "regallocinterp.h"

RegAllocInterface* GetRegisterAllocator(Compiler* compiler)
{
    return new (compiler->getAllocator(CMK_LSRA)) InterpRegAlloc(compiler);
}

InterpRegAlloc::InterpRegAlloc(Compiler* compiler)
    : m_compiler(compiler)
{
}

PhaseStatus InterpRegAlloc::doRegisterAllocation()
{
    for (unsigned lclNum = 0; lclNum < m_compiler->lvaCount; lclNum++)
    {
        m_compiler->lvaGetDesc(lclNum)->SetRegNum(REG_STK);
    }

    return PhaseStatus::MODIFIED_NOTHING;
}

void InterpRegAlloc::recordVarLocationsAtStartOfBB(BasicBlock* bb)
{
}

bool InterpRegAlloc::willEnregisterLocalVars() const
{
    return false;
}

#if TRACK_LSRA_STATS
void InterpRegAlloc::dumpLsraStatsCsv(FILE* file)
{
}

void InterpRegAlloc::dumpLsraStatsSummary(FILE* file)
{
}
#endif // TRACK_LSRA_STATS
