// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef _WIN32
// this must exactly match the typedef used by windows.h
typedef long HRESULT;
#else
#include <stdint.h>
typedef int32_t HRESULT;
#endif

HRESULT InitializeDefaultGC();

HRESULT InitializeGCSelector()
{
    return InitializeDefaultGC();
}