// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "cdacmetadata.hpp"

#ifndef DACCESS_COMPILE
CDacMetadata g_cdacMetadata;

void CDacMetadata::Init()
{
    PrecodeMachineDescriptor::Init(&g_cdacMetadata.precode);
}

#endif // !DACCESS_COMPILE
