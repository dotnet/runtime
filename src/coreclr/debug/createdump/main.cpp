// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

extern int createdump_main(const int argc, const char* argv[]);
extern void UninitializePAL(int exitCode);

#if defined(HOST_ARM64)
// Flag to check if atomics feature is available on
// the machine
bool g_arm64_atomics_present = false;
#endif

//
// Main entry point
//
int __cdecl main(const int argc, const char* argv[])
{
    int exitCode = createdump_main(argc, argv);
#ifdef HOST_UNIX
    UninitializePAL(exitCode);
#endif
    return exitCode;
}

#ifdef HOST_UNIX

PALIMPORT
VOID
PALAPI
PAL_SetCreateDumpCallback(
    IN PCREATEDUMP_CALLBACK callback) 
{
}

#endif
