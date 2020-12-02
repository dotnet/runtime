// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  virtualalloc.c
**
** Purpose: Positive test the VirtualAlloc API.
**          Call VirtualAlloc to reserve and commit
**          simultaneously with MEM_COMMIT|MEM_RESERVE|MEM_TOP_DOWN 
**          allocation type and PAGE_READONLY access 
**          protection
**
**
**============================================================*/
#include <palsuite.h>

PALTEST(filemapping_memmgt_VirtualAlloc_test19_paltest_virtualalloc_test19, "filemapping_memmgt/VirtualAlloc/test19/paltest_virtualalloc_test19")
{
    int err;
    LPVOID lpVirtualAddress;

    //Initialize the PAL environment
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        ExitProcess(FAIL);
    }
    
    //reserve and commit simultaneously by using MEM_COMMIT|MEM_RESERVE 
    lpVirtualAddress = VirtualAlloc(NULL,//system determine where to allocate the region
            1024,            //specify the size
            MEM_COMMIT|MEM_RESERVE|MEM_TOP_DOWN,      //allocation type
            PAGE_READONLY);  //access protection
    if(NULL == lpVirtualAddress)
    {
        Fail("\nFailed to call VirtualAlloc API!\n");
    }

    //decommit the specified region
    err = VirtualFree(lpVirtualAddress,1024,MEM_DECOMMIT);
    if(0 == err)
    {
        Fail("\nFailed to call VirtualFree API!\n");
    }


    PAL_Terminate();
    return PASS;
}
