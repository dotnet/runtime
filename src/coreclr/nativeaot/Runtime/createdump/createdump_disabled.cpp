// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Disabled stub for NativeAOT linked-in createdump.
// Linked when linked createdump is not enabled for the output configuration.

extern "C" bool g_createdumpLinked = false;

extern "C" int nativeaot_createdump_main(int, const char*[])
{
    return 1;
}
