// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CDACPLATFORMMETADATA_HPP__
#define CDACPLATFORMMETADATA_HPP__

#include "precode.h"

// Cross-cutting metadata for cDAC
#ifndef DACCESS_COMPILE
enum class CDacCodePointerFlags : uint8_t
{
    None = 0,
    HasArm32ThumbBit = 0x1,
    HasArm64PtrAuth = 0x2,
};


struct CDacPlatformMetadata
{
    PrecodeMachineDescriptor precode;
    CDacCodePointerFlags codePointerFlags;
    CDacPlatformMetadata() = default;
    CDacPlatformMetadata(const CDacPlatformMetadata&) = delete;
    CDacPlatformMetadata& operator=(const CDacPlatformMetadata&) = delete;
    static void Init();
};

extern CDacPlatformMetadata g_cdacPlatformMetadata;
#endif // DACCESS_COMPILE


#endif// CDACPLATFORMMETADATA_HPP__
