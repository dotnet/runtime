// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ******************************************************************************
// WARNING!!!: This code is also used by SOS in the diagnostics repo. Should be
// updated in a backwards and forwards compatible way.
// See: https://github.com/dotnet/diagnostics/blob/main/src/SOS/inc/specialthreadinfo.h
// ******************************************************************************

// This defines a workaround to the MacOS dump format not having the OS process
// and thread ids that SOS needs to map thread "indexes" to thread "ids". The MacOS
// createdump adds this special memory region at this specific address that is not
// in the user or kernel address spaces. lldb is fine with it.

#define SPECIAL_THREADINFO_SIGNATURE "THREADINFO"

#if defined(HOST_ARM64) || defined(__arm64__) || defined(__aarch64__)
// Apple Silicon (arm64) macOS user-space VM is 47 bits. lldb's core reader
// rejects segments above 0x7FFF_FFFF_FFFF, so use a 47-bit-valid address.
const uint64_t SpecialThreadInfoAddress = 0x00007ffffff00000;
#else
const uint64_t SpecialThreadInfoAddress = 0x7fffffff00000000;
#endif

struct SpecialThreadInfoHeader
{
    char signature[16];
    uint32_t pid;
    uint32_t numThreads;        // The number of SpecialThreadInfoEntry's after this header
};

struct SpecialThreadInfoEntry
{
    uint32_t tid;
    uint64_t sp;
};
