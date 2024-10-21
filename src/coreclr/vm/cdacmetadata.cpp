// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "cdacmetadata.hpp"

#ifndef DACCESS_COMPILE
CDacMetadata g_cdacMetadata;

void CDacMetadata::Init()
{
    PrecodeMachineDescriptor::Init(&g_cdacMetadata.precode);
#if defined(TARGET_ARM)
    g_cdacMetadata.codePointerFlags = CDacCodePointerFlags::HasArm32ThumbBit;
#elif defined(TARGET_ARM64) && defined(TARGET_OSX)
    g_cdacMetadata.codePointerFlags = CDacCodePointerFlags::None; // TODO set HasArm64PtrAuth if arm64e
#else
    g_cdacMetadata.codePointerFlags = CDacCodePointerFlags::None;
#endif
}

#endif // !DACCESS_COMPILE
