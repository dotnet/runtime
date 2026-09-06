// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// contracts.h
//
// Shared types for the cdac-lite memory-enumeration contracts. Each contract
// walks a subset of runtime structures (GC heaps, threads, modules, ...) and
// reports the memory regions that a heap dump should include.
//*****************************************************************************

#ifndef CDACLITE_CONTRACTS_H
#define CDACLITE_CONTRACTS_H

#include <stdint.h>

namespace cdac
{
namespace contracts
{
    // Reports one enumerated memory region [start, start+size). 'kind' is a short
    // label identifying the source (e.g. "gc-gen0", "thread-stack", "module").
    typedef void (*RegionCallback)(void* context, const char* kind, uint64_t start, uint64_t size);
}
} // namespace contracts

#endif // CDACLITE_CONTRACTS_H
