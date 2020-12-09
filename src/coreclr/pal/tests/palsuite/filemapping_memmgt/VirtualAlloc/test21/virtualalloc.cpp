// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

PALTEST(filemapping_memmgt_VirtualAlloc_test21_paltest_virtualalloc_test21, "filemapping_memmgt/VirtualAlloc/test21/paltest_virtualalloc_test21")
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
