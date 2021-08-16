// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  virtualprotect.c
**
** Purpose: Positive test the VirtualProtect API.
**          Call VirtualProtect to set new protect as
**          PAGE_READONLY
**
**
**============================================================*/
#include <palsuite.h>

#define REGIONSIZE 1024

PALTEST(filemapping_memmgt_VirtualProtect_test7_paltest_virtualprotect_test7, "filemapping_memmgt/VirtualProtect/test7/paltest_virtualprotect_test7")
{
    int err;
    LPVOID lpVirtualAddress;
    DWORD OldProtect;

    //Initialize the PAL environment
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        ExitProcess(FAIL);
    }

    //Allocate the physical storage in memory or in the paging file on disk 
    lpVirtualAddress = VirtualAlloc(NULL,//determine where to allocate the region
            REGIONSIZE,      //specify the size
            MEM_COMMIT,      //allocation type
            PAGE_READWRITE);  //access protection
    if(NULL == lpVirtualAddress)
    {
        Fail("\nFailed to call VirtualAlloc API!\n");
    }

    OldProtect = PAGE_READONLY;
    //Set new access protection
    err = VirtualProtect(lpVirtualAddress,
                      REGIONSIZE,  //specify the region size
                      PAGE_READONLY,//desied access protection
                      &OldProtect);//old access protection
    if(0 == err)
    {
        Trace("\nFailed to call VirtualProtect API!\n");
        err = VirtualFree(lpVirtualAddress,REGIONSIZE,MEM_DECOMMIT);
        if(0 == err)
        {
            Fail("\nFailed to call VirtualFree API!\n");
        }
        Fail("");
    }


    //decommit the specified region
    err = VirtualFree(lpVirtualAddress,REGIONSIZE,MEM_DECOMMIT);
    if(0 == err)
    {
        Fail("\nFailed to call VirtualFree API!\n");
    }

    PAL_Terminate();
    return PASS;
}
