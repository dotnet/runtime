// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "cdacplatformmetadata.hpp"

#ifndef DACCESS_COMPILE
CDacPlatformMetadata g_cdacPlatformMetadata;

void CDacPlatformMetadata::Init()
{
    PrecodeMachineDescriptor::Init(&g_cdacPlatformMetadata.precode);
#if defined(TARGET_ARM)
    g_cdacPlatformMetadata.codePointerFlags = CDacCodePointerFlags::HasArm32ThumbBit;
#elif defined(TARGET_ARM64) && defined(TARGET_APPLE)
    // TODO set HasArm64PtrAuth if arm64e
    g_cdacPlatformMetadata.codePointerFlags = CDacCodePointerFlags::None;
#else
    g_cdacPlatformMetadata.codePointerFlags = CDacCodePointerFlags::None;
#endif
}

#endif // !DACCESS_COMPILE
