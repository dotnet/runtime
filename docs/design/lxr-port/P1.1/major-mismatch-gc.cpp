// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"

#ifndef DLLEXPORT
#define DLLEXPORT __declspec(dllexport)
#endif

#ifndef P11_GC_MAJOR_VERSION_DELTA
#define P11_GC_MAJOR_VERSION_DELTA -1
#endif

extern "C" DLLEXPORT void LOCALGC_CALLCONV GC_VersionInfo(VersionInfo* info)
{
    info->MajorVersion = GC_INTERFACE_MAJOR_VERSION + P11_GC_MAJOR_VERSION_DELTA;
    info->MinorVersion = GC_INTERFACE_MINOR_VERSION;
    info->BuildVersion = 0;
    info->Name = "P1.1 major-version compatibility probe";
}

extern "C" DLLEXPORT HRESULT LOCALGC_CALLCONV GC_Initialize(
    IGCToCLR*,
    IGCHeap**,
    IGCHandleManager**,
    GcDacVars*)
{
    return E_UNEXPECTED;
}
