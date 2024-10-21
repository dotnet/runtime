// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CDACMETADATA_HPP__
#define CDACMETADATA_HPP__

#include "precode.h"

// Cross-cutting metadata for cDAC
#ifndef DACCESS_COMPILE
struct CDacMetadata
{
    PrecodeMachineDescriptor precode;
    CDacMetadata() = default;
    CDacMetadata(const CDacMetadata&) = delete;
    CDacMetadata& operator=(const CDacMetadata&) = delete;
    static void Init();
};

extern CDacMetadata g_cdacMetadata;
#endif // DACCESS_COMPILE


#endif// CDACMETADATA_HPP__
