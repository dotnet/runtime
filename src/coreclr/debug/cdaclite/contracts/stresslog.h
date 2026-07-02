// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// stresslog.h
//
// StressLog contract: enumerates the per-thread stress logs and their message
// chunk buffers. Modeled on the managed cDAC StressLog contract
// (Contracts/StressLog.cs). Only meaningful when the stress log is enabled.
//*****************************************************************************

#ifndef CDACLITE_CONTRACTS_STRESSLOG_H
#define CDACLITE_CONTRACTS_STRESSLOG_H

#include <stdint.h>

#include "contracts.h"
#include "target.h"

namespace cdac
{
namespace contracts
{
    // Reads the StressLog, the ThreadStressLog list, and each StressLogChunk (including its
    // message buffer) so the cDAC can re-read them from the dump. Memory is captured via the
    // Target's EnumMem sink. Returns the number of chunks captured, 0 if the stress log is
    // disabled, or -1 if the stress log could not be located.
    int EnumerateStressLogRegions(const Target& target);
}
} // namespace contracts

#endif // CDACLITE_CONTRACTS_STRESSLOG_H
