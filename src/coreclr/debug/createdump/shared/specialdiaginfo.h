// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ******************************************************************************
// WARNING!!!: This code is also used by SOS in the diagnostics repo. Should be
// updated in a backwards and forwards compatible way.
// See: https://github.com/dotnet/diagnostics/blob/main/src/SOS/inc/specialdiaginfo.h
// ******************************************************************************

// This is a special memory region added to ELF and MachO dumps that contains extra diagnostics
// information like the exception record address for a NativeAOT app crash or the runtime module
// base address. The exception record contains the pointer to the JSON formatted crash info.

#pragma once

#define SPECIAL_DIAGINFO_SIGNATURE "DIAGINFOHEADER"
#define SPECIAL_DIAGINFO_VERSION 2

#ifdef __APPLE__
#if defined(HOST_ARM64) || defined(__arm64__) || defined(__aarch64__)
// Apple Silicon (arm64) macOS user-space VM is 47 bits — addresses above 0x7FFF_FFFF_FFFF
// are not mappable and lldb's core file reader rejects segments at those addresses. Use a
// 47-bit-valid address here. The legacy x86_64 macOS address is also probed at read time by
// SOS/dotnet-dump (see the diagnostics repo) so older dumps remain recognized.
const uint64_t SpecialDiagInfoAddress = 0x00007ffffff10000;
#else
const uint64_t SpecialDiagInfoAddress = 0x7fffffff10000000;
#endif
#else
#if TARGET_64BIT
const uint64_t SpecialDiagInfoAddress = 0x00007ffffff10000;
#else
const uint64_t SpecialDiagInfoAddress = 0x7fff1000;
#endif
#endif

const uint64_t SpecialDiagInfoSize = 0x1000;

struct SpecialDiagInfoHeader
{
    char Signature[16];
    int32_t Version;
    uint64_t ExceptionRecordAddress;
    uint64_t RuntimeBaseAddress;
};
