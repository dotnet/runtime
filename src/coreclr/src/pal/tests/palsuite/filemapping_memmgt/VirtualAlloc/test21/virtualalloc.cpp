// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source:  virtualalloc.c
**
** Purpose: Positive test the VirtualAlloc API.
**          Ensure that memory committed through VirtualAlloc,
**          then freed, then committed again is zeroed.
**
**
**============================================================*/
#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    int err;
    int *ptr;

    //Initialize the PAL environment
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        ExitProcess(FAIL);
    }
    
    ptr = (int *) VirtualAlloc(NULL, 4096, MEM_COMMIT | MEM_RESERVE,
                               PAGE_READWRITE);
    if (ptr == NULL)
    {
        Fail("First VirtualAlloc failed!\n");
    }
    *ptr = 123;

    if (!VirtualFree(ptr, 4096, MEM_DECOMMIT))
    {
        Fail("VirtualFree failed!\n");
    }
    
    ptr = (int *) VirtualAlloc(ptr, 4096, MEM_COMMIT, PAGE_READWRITE);
    if (ptr == NULL)
    {
        Fail("Second VirtualAlloc failed!\n");
    }
    if (*ptr != 0)
    {
        Fail("VirtualAlloc failed to zero its memory!\n");
    }

    PAL_Terminate();
    return PASS;
}
