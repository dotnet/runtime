// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// gc.h
//
// GC contract: walks the .NET GC heaps and enumerates their memory regions by
// reading GC types/globals from a Target. Modeled on the managed cDAC GC
// contract (see docs/design/datacontracts/GC.md and
// src/native/managed/cdac/.../Contracts/GC/GC_1.cs).
//*****************************************************************************

#ifndef CDACLITE_CONTRACTS_GC_H
#define CDACLITE_CONTRACTS_GC_H

#include <stdint.h>

#include "contracts.h"
#include "target.h"

namespace cdac
{
namespace contracts
{
    // Walks all GC heaps and their segments, invoking 'sink' for each segment's
    // used range ([Mem, Allocated), or [Mem, AllocAllocated) for the ephemeral
    // segment). The region 'kind' is "gc-gen<N>". Returns the number of regions
    // reported, or -1 if the GC type could not be determined from the descriptor.
    int EnumerateGCHeapRegions(const Target& target, RegionCallback sink, void* sinkContext);
}
} // namespace contracts

#endif // CDACLITE_CONTRACTS_GC_H
