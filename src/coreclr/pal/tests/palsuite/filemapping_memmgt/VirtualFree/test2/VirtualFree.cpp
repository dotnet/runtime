// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  virtualfree.c
**
** Purpose: Positive test the VirtualFree API.
**          Call VirtualFree with MEM_RELEASE
**          free operation type
**
**
**============================================================*/
#include <palsuite.h>

PALTEST(filemapping_memmgt_VirtualFree_test2_paltest_virtualfree_test2, "filemapping_memmgt/VirtualFree/test2/paltest_virtualfree_test2")
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
            PAGE_READONLY);  //access protection
    if(NULL == lpVirtualAddress)
    {
        Fail("\nFailed to call VirtualAlloc API!\n");
        PAL_TerminateEx(1);
        return 1;
    }

    //decommit the specified region
    err = VirtualFree(lpVirtualAddress,//base address
                    0,           //must be zero with MEM_RELEASE
                    MEM_RELEASE);//free operation
    if(0 == err)
    {
        Fail("\nFailed to call VirtualFree API!\n");
        PAL_TerminateEx(1);
        return 1;
    }

    PAL_Terminate();
    return PASS;
}
