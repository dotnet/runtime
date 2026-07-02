// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// statics.h
//
// Contract bootstrap contract: reports the runtime's self-description so any
// contract-based tool can re-bootstrap the data contracts from the dump alone.
//
// cdac-lite is a DAC replacement, so it does NOT report the legacy DAC globals
// table. It reports only what the data-contract system needs:
//   1. the contract descriptor struct
//   2. the UTF-8 JSON data descriptor
//   3. the pointer_data table (addresses of the indirect globals)
//   4. the global variable storage those pointer_data entries reference
//*****************************************************************************

#ifndef CDACLITE_CONTRACTS_STATICS_H
#define CDACLITE_CONTRACTS_STATICS_H

#include <stdint.h>

#include "contracts.h"
#include "target.h"

namespace cdac
{
namespace contracts
{
    // Reports the contract descriptor (kinds "contract-descriptor", "descriptor-json",
    // "pointer-data") and the global variable storage referenced by pointer_data
    // (kind "global-var"). Returns the number of regions reported.
    int EnumerateStaticRegions(const Target& target, uint64_t contractDescriptorAddr,
                               RegionCallback sink, void* sinkContext);
}
} // namespace contracts

#endif // CDACLITE_CONTRACTS_STATICS_H
