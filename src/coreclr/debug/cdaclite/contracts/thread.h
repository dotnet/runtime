// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// thread.h
//
// Thread contract: walks the ThreadStore's thread list and reports each managed
// thread's stack range. Modeled on the managed cDAC Thread contract
// (docs/design/datacontracts/Thread.md, Contracts/Thread_1.cs).
//*****************************************************************************

#ifndef CDACLITE_CONTRACTS_THREAD_H
#define CDACLITE_CONTRACTS_THREAD_H

#include <stdint.h>

#include "contracts.h"
#include "target.h"

namespace cdac
{
namespace contracts
{
    // Walks ThreadStore -> Thread list, reporting each thread's stack range
    // ([CachedStackLimit, CachedStackBase)) with kind "thread-stack". Returns the
    // number of regions reported, or -1 if the ThreadStore could not be located.
    int EnumerateThreadRegions(const Target& target, RegionCallback sink, void* sinkContext);
}
} // namespace contracts

#endif // CDACLITE_CONTRACTS_THREAD_H
