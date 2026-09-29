// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump.h"

extern int createdump_main(const int argc, const char* argv[]);

//
// Main entry point
//
int __cdecl main(const int argc, const char* argv[])
{
#ifdef HOST_UNIX
    if (PAL_InitializeDLL() != 0)
    {
        printf_error("PAL initialization FAILED\n");
        return -1;
    }
#endif
    int exitCode = createdump_main(argc, argv);
#ifdef HOST_UNIX
    PAL_TerminateEx(exitCode);
#endif
    return exitCode;
}
