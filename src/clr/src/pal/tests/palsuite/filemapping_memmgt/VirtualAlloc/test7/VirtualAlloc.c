//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================
**
** Source:  virtualalloc.c
**
** Purpose: Positive test the VirtualAlloc API.
**          Call VirtualAlloc with MEM_RESERVE allocation type
**          and PAGE_READONLY access protection

**
**============================================================*/
#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
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
            MEM_RESERVE,      //allocation type
            PAGE_READONLY);  //access protection
    if(NULL == lpVirtualAddress)
    {
        Fail("\nFailed to call VirtualAlloc API!\n");
        PAL_Terminate();
        return 1;
    }

    //decommit the specified region
    err = VirtualFree(lpVirtualAddress,1024,MEM_DECOMMIT);
    if(0 == err)
    {
        Fail("\nFailed to call VirtualFree API!\n");
        PAL_Terminate();
        return 1;
    }


    PAL_Terminate();
    return PASS;
}
