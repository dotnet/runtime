// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "verbprintjiteeversion.h"
#include "runtimedetails.h"

// Print the GUID in format a5eec3a4-4176-43a7-8c2b-a05b551d4f49
//
// This is useful for tools that want to determine which MCH file to use for a
// particular JIT: if the JIT and MCS are built from the same source tree, then
// use this function to print out the JITEEVersion, and use that to determine
// which MCH files to use.
//
int verbPrintJITEEVersion::DoWork()
{
    const GUID& g = JITEEVersionIdentifier;
    printf("%08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x\n",
            g.Data1,
            g.Data2,
            g.Data3,
            g.Data4[0],
            g.Data4[1],
            g.Data4[2],
            g.Data4[3],
            g.Data4[4],
            g.Data4[5],
            g.Data4[6],
            g.Data4[7]);

    return 0;
}
