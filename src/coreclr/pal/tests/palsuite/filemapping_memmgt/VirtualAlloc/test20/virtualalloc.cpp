// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  virtualalloc.c
**
** Purpose: Positive test the VirtualAlloc API.
**          Ensure that memory re-committed through VirtualAlloc
**          is not changed.
**
**
**============================================================*/
#include <palsuite.h>

PALTEST(filemapping_memmgt_VirtualAlloc_test20_paltest_virtualalloc_test20, "filemapping_memmgt/VirtualAlloc/test20/paltest_virtualalloc_test20")
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

    ptr = (int *) VirtualAlloc(ptr, 4096, MEM_COMMIT, PAGE_READWRITE);
    if (ptr == NULL)
    {
        Fail("Second VirtualAlloc failed!\n");
    }
    if (*ptr != 123)
    {
        Fail("VirtualAlloc modified (probably zeroed) re-committed memory!\n");
    }

    PAL_Terminate();
    return PASS;
}
