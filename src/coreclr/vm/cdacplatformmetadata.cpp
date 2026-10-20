// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "cdacplatformmetadata.hpp"

GVAL_IMPL(CDacPlatformMetadata, g_cdacPlatformMetadata);

#ifndef DACCESS_COMPILE
void CDacPlatformMetadata::Init()
{
#if defined(TARGET_ARM)
    (&g_cdacPlatformMetadata)->codePointerFlags = CDacCodePointerFlags::HasArm32ThumbBit;
#elif defined(TARGET_ARM64)
    // R2R code may use PAC even when it is disabled for the JIT, so JitPacEnabled
    // cannot reliably indicate whether a process may contain signed return addresses.
    // The mask must be updated if the upper address bits are used for another purpose.
    (&g_cdacPlatformMetadata)->codePointerFlags = CDacCodePointerFlags::HasArm64PtrAuth;
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
