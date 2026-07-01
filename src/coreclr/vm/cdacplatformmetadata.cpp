// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#include "cdacplatformmetadata.hpp"

GVAL_IMPL(CDacPlatformMetadata, g_cdacPlatformMetadata);

#ifndef DACCESS_COMPILE
void CDacPlatformMetadata::Init()
{
#if defined(TARGET_ARM)
    (&g_cdacPlatformMetadata)->codePointerFlags = CDacCodePointerFlags::HasArm32ThumbBit;
#elif defined(TARGET_ARM64) && !defined(TARGET_WINDOWS)
    CLRConfig::ConfigDWORDInfo jitPacEnabledConfig { W("JitPacEnabled"), 1, CLRConfig::LookupOptions::Default };
    (&g_cdacPlatformMetadata)->codePointerFlags =
        CLRConfig::GetConfigValue(jitPacEnabledConfig) != 0
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
