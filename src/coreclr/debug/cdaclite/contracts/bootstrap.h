// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// bootstrap.h
//
// Bootstrap enumerator: emits the standalone singletons a reader needs to
// bootstrap from the dump alone -- the runtime module's PE headers + export
// directory (to resolve the DotNetRuntimeContractDescriptor export) and the
// well-known global objects the stack walk / R2R resolution reference
// (SystemDomain, the global LoaderAllocator, PlatformMetadata, Debugger).
// Runs in every tier.
//*****************************************************************************

#ifndef CDACLITE_CONTRACTS_BOOTSTRAP_H
#define CDACLITE_CONTRACTS_BOOTSTRAP_H

#include <stdint.h>

#include "contracts.h"
#include "target.h"

namespace cdac
{
namespace contracts
{
    // Emits the runtime module export directory + global singletons. Returns the number of
    // singletons emitted (>= 0).
    int EnumerateBootstrapRegions(const Target& target, RegionCallback sink, void* sinkContext);
}
} // namespace contracts

#endif // CDACLITE_CONTRACTS_BOOTSTRAP_H
