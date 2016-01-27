// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: palstartup.h
//
// An implementation of startup code for Rotor's Unix PAL.  This file should
// be included by any file in a PAL application that defines main.
// we have added palsuite.h to include test related macros etc...
// ===========================================================================

#ifndef __PALSTARTUP_H__
#define __PALSTARTUP_H__

#include <palsuite.h>

int __cdecl PAL_startup_main(int argc, char **argv);

struct _mainargs
{
    int argc;
    char ** argv;
};

static DWORD PALAPI run_main(struct _mainargs *args)
{
    return (DWORD) PAL_startup_main(args->argc, args->argv);
}

int __cdecl main(int argc, char **argv) {
    struct _mainargs mainargs;

    if (PAL_Initialize(argc, argv)) {
        return FAIL;;
    }

    // PAL_Terminate is a stdcall function, but it takes no parameters
    // so the difference doesn't matter.
    atexit((void (__cdecl *)(void)) PAL_Terminate);

    mainargs.argc = argc;
    mainargs.argv = argv;
    exit((int)PAL_EntryPoint((PTHREAD_START_ROUTINE)run_main, &mainargs));
    return 0;   // Quiet a compiler warning
}

#define main    PAL_startup_main

#endif  // __PALSTARTUP_H__
