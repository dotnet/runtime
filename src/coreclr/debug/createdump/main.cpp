// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

extern int createdump_main(const int argc, const char* argv[]);
extern bool InitializePAL();
extern void UninitializePAL(int exitCode);

#if defined(HOST_ARM64) && !defined(HOST_UNIX)
// Flag to check if atomics feature is available on
// the machine
bool g_arm64_atomics_present = false;
#endif

//
// Main entry point
//
int __cdecl main(const int argc, const char* argv[])
{
#ifdef HOST_UNIX
    if (!InitializePAL())
    {
        return -1;
    }
#endif
    int exitCode = createdump_main(argc, argv);
#ifdef HOST_UNIX
    UninitializePAL(exitCode);
#endif
    return exitCode;
}
