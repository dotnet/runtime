// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// loaderheaps.h
//
// LoaderAllocator heaps contract: reports the runtime's loader heap blocks
// (high/low-frequency, statics, stub, executable, precode heaps). These hold the
// type system (MethodTables, MethodDescs) and executable stubs/precode -- some of
// which are RX and thus missed by a private-read-write memory capture. Modeled on
// the DAC EnumMemDumpAppDomainInfo -> LoaderAllocator::EnumMemoryRegions path.
//*****************************************************************************

#ifndef CDACLITE_CONTRACTS_LOADERHEAPS_H
#define CDACLITE_CONTRACTS_LOADERHEAPS_H

#include <stdint.h>

#include "contracts.h"
#include "target.h"

namespace cdac
{
namespace contracts
{
    // Collects LoaderAllocators (SystemDomain global + per-module) and walks each
    // LoaderHeap's block list, reporting each block ([VirtualAddress, +VirtualSize))
    // with kind "loader-heap". Returns the number of regions reported, or -1 on
    // failure to locate the loader allocators.
    int EnumerateLoaderHeapRegions(const Target& target, RegionCallback sink, void* sinkContext);
}
} // namespace contracts

#endif // CDACLITE_CONTRACTS_LOADERHEAPS_H
