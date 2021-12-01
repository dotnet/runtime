// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  virtualalloc.c
**
** Purpose: Negative test the VirtualAlloc API.
**          Call VirtualAlloc with MEM_COMMIT allocation type
**          and PAGE_READWRITE access protection

**
**============================================================*/
#include <palsuite.h>

PALTEST(filemapping_memmgt_VirtualAlloc_test22_paltest_virtualalloc_test22, "filemapping_memmgt/VirtualAlloc/test22/paltest_virtualalloc_test22")
{
    int err;
    LPVOID lpVirtualAddress;


    //Initialize the PAL environment
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        ExitProcess(FAIL);
    }

    //Allocate the physical storage in memory or in the paging file on disk 
    lpVirtualAddress = VirtualAlloc(NULL,//system determine where to allocate the region
            (SIZE_T)(2147483647000000), //specify the size to be int32.maxvalue mega bytes
            MEM_COMMIT,      //allocation type
            PAGE_READWRITE);  //access protection
    if(NULL != lpVirtualAddress)
    {
        Fail("\nWelcome to the Future, where Unlimited Memory is Available, disregard this test!\n");
    }



    PAL_Terminate();
    return PASS;
}
