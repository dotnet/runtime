// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  virtualalloc.c
**
** Purpose: Positive test the VirtualAlloc API.
**          Call VirtualAlloc with MEM_COMMIT allocation type
**          and PAGE_EXECUTE_READ access protection

**
**============================================================*/
#include <palsuite.h>

PALTEST(filemapping_memmgt_VirtualAlloc_test4_paltest_virtualalloc_test4, "filemapping_memmgt/VirtualAlloc/test4/paltest_virtualalloc_test4")
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
            1024,            //specify the size
            MEM_COMMIT,      //allocation type
            PAGE_EXECUTE_READ);  //access protection
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
