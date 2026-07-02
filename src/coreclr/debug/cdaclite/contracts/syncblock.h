// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// syncblock.h
//
// SyncBlock contract: enumerates the sync-block table (SyncTableEntry array),
// the SyncBlockCache, and the SyncBlock structs for in-use entries. Modeled on
// the managed cDAC SyncBlock_1 contract (Contracts/SyncBlock_1.cs).
//*****************************************************************************

#ifndef CDACLITE_CONTRACTS_SYNCBLOCK_H
#define CDACLITE_CONTRACTS_SYNCBLOCK_H

#include <stdint.h>

#include "contracts.h"
#include "target.h"

namespace cdac
{
namespace contracts
{
    // Reads the SyncBlockCache + SyncTableEntry array and each in-use SyncBlock so the
    // cDAC's SyncBlock contract can re-read them from the dump. Memory is captured via the
    // Target's EnumMem sink (no explicit region kind). Returns the number of in-use sync
    // blocks captured, or -1 if the sync table could not be located.
    int EnumerateSyncBlockRegions(const Target& target);
}
} // namespace contracts

#endif // CDACLITE_CONTRACTS_SYNCBLOCK_H
