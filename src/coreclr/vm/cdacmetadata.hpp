// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CDACMETADATA_HPP__
#define CDACMETADATA_HPP__

#include "precode.h"

// Cross-cutting metadata for cDAC
#ifndef DACCESS_COMPILE
enum class CDacCodePointerFlags : uint8_t
{
    None = 0,
    HasArm32ThumbBit = 0x1,
    HasArm64PtrAuth = 0x2,
};


struct CDacMetadata
{
    PrecodeMachineDescriptor precode;
    CDacCodePointerFlags codePointerFlags;
    CDacMetadata() = default;
    CDacMetadata(const CDacMetadata&) = delete;
    CDacMetadata& operator=(const CDacMetadata&) = delete;
    static void Init();
};

extern CDacMetadata g_cdacMetadata;
#endif // DACCESS_COMPILE


#endif// CDACMETADATA_HPP__
