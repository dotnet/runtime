// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

extern int createdump_main(const int argc, const char* argv[]);

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
    int exitCode = 0;
#ifdef HOST_UNIX
    exitCode = PAL_InitializeDLL();
    if (exitCode != 0)
    {
        printf_error("PAL initialization FAILED %d\n", exitCode);
        return exitCode;
    }
#endif
    exitCode = createdump_main(argc, argv);
#ifdef HOST_UNIX
    PAL_TerminateEx(exitCode);
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
