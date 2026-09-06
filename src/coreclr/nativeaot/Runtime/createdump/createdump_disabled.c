// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Disabled stub for NativeAOT linked-in createdump.
// Linked when linked createdump is not enabled for the output configuration.

#include <stdbool.h>

bool g_createdumpLinked = false;

int nativeaot_createdump_main(int argc, const char* argv[])
{
    return 1;
}
