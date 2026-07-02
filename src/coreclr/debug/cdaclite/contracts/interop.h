// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// interop.h
//
// COM interop contract: enumerates the RCW cleanup list. Modeled on the managed
// cDAC BuiltInCOM contract (Contracts/BuiltInCOM_1.cs GetRCWCleanupList).
//*****************************************************************************

#ifndef CDACLITE_CONTRACTS_INTEROP_H
#define CDACLITE_CONTRACTS_INTEROP_H

#include <stdint.h>

#include "contracts.h"
#include "target.h"

namespace cdac
{
namespace contracts
{
    // Reads the RCWCleanupList and its RCW bucket/cleanup chains so the cDAC's BuiltInCOM
    // contract can re-read them from the dump. Memory is captured via the Target's EnumMem
    // sink. Returns the number of RCWs captured, 0 if the cleanup list is empty, or -1 if
    // the list global could not be located.
    int EnumerateInteropRegions(const Target& target);
}
} // namespace contracts

#endif // CDACLITE_CONTRACTS_INTEROP_H
