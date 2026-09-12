// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// handles.h
//
// GC handles contract: walks the handle table and reports each handle table
// segment's memory (the GC roots storage). Modeled on the managed cDAC
// IGC.GetHandles walk (Contracts/GC/GC_1.cs).
//*****************************************************************************

#ifndef CDACLITE_CONTRACTS_HANDLES_H
#define CDACLITE_CONTRACTS_HANDLES_H

#include <stdint.h>

#include "contracts.h"
#include "target.h"

namespace cdac
{
namespace contracts
{
    // Walks HandleTableMap -> buckets -> HandleTable -> TableSegment list and
    // reports each segment's memory ([segment, segment+HandleSegmentSize)) with
    // kind "handle-segment". Returns the number of regions reported, or -1 if the
    // handle table could not be located.
    int EnumerateHandleRegions(const Target& target, RegionCallback sink, void* sinkContext);
}
} // namespace contracts

#endif // CDACLITE_CONTRACTS_HANDLES_H
