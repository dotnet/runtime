// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "cdacplatformmetadata.hpp"

GVAL_IMPL(CDacPlatformMetadata, g_cdacPlatformMetadata);

#ifndef DACCESS_COMPILE
void CDacPlatformMetadata::Init()
{
    (&g_cdacPlatformMetadata)->pointerAuthenticationMask = 0;
#if defined(TARGET_ARM)
    (&g_cdacPlatformMetadata)->codePointerFlags = CDacCodePointerFlags::HasArm32ThumbBit;
#elif defined(TARGET_ARM64)
    // Bit 55 selects the VA range and is not part of PAC. See InsertPAC():
    // https://developer.arm.com/documentation/ddi0602/latest/Shared-Pseudocode/aarch64-functions-pac
    // XPACLRI accounts for VA size and pointer tagging; XOR gives the PAC mask.
    const uintptr_t probe = ~(uintptr_t(1) << 55);
    const uint64_t mask = probe ^ reinterpret_cast<uintptr_t>(PacStripPtr(reinterpret_cast<void*>(probe)));
    (&g_cdacPlatformMetadata)->pointerAuthenticationMask = mask;
    // Mark PAC present when supported by hardware, since R2R code may use PAC even
    // when JitPacEnabled is false.
    (&g_cdacPlatformMetadata)->codePointerFlags = mask != 0
        ? CDacCodePointerFlags::HasArm64PtrAuth
        : CDacCodePointerFlags::None;
#else
    (&g_cdacPlatformMetadata)->codePointerFlags = CDacCodePointerFlags::None;
#endif
}

void CDacPlatformMetadata::InitPrecodes()
{
#ifndef FEATURE_PORTABLE_ENTRYPOINTS
    PrecodeMachineDescriptor::Init(&(&g_cdacPlatformMetadata)->precode);
#endif // !FEATURE_PORTABLE_ENTRYPOINTS
}

#endif // !DACCESS_COMPILE
