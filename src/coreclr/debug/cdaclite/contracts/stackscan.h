// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// stackscan.h
//
// Normal-tier conservative stack scan. Instead of bulk-emitting every JIT code
// heap and loader heap (which does not scale to large programs), this walks each
// managed thread's stack, and for every pointer-aligned value that resolves to
// managed code (via the ExecutionManager RangeSectionMap), emits just that
// method's code + code header + MethodDesc chain -- the analog of the DAC's
// stack-walk-driven DumpAllInstances for MiniDumpNormal.
//*****************************************************************************

#ifndef CDACLITE_CONTRACTS_STACKSCAN_H
#define CDACLITE_CONTRACTS_STACKSCAN_H

#include <stdint.h>

#include "contracts.h"
#include "target.h"

namespace cdac
{
namespace contracts
{
    // Scans all managed thread stacks and captures the code + method metadata reachable from
    // code pointers found on them. Returns the number of distinct methods captured, or -1 if
    // the ExecutionManager range map could not be located.
    int EnumerateStackScanRegions(const Target& target, RegionCallback sink, void* sinkContext);
}
} // namespace contracts

#endif // CDACLITE_CONTRACTS_STACKSCAN_H
