// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// jit.h
//
// JIT code contract: reports the runtime's JIT code heaps. These hold executable
// (RX) code which a private-read-write memory capture does NOT include, so a heap
// dump must enumerate them explicitly. Matches the DAC HEAP2 behavior
// (EECodeGenManager::EnumMemoryRegions in vm/codeman.cpp).
//*****************************************************************************

#ifndef CDACLITE_CONTRACTS_JIT_H
#define CDACLITE_CONTRACTS_JIT_H

#include <stdint.h>

#include "contracts.h"
#include "target.h"

namespace cdac
{
namespace contracts
{
    // Walks EEJitManager -> AllCodeHeaps (CodeHeapListNode list) and reports each
    // code heap's range ([StartAddress, EndAddress)) with kind "jit-code". Returns
    // the number of regions reported, or -1 if the JIT manager could not be located.
    int EnumerateJitCodeRegions(const Target& target, RegionCallback sink, void* sinkContext);
}
} // namespace contracts

#endif // CDACLITE_CONTRACTS_JIT_H
