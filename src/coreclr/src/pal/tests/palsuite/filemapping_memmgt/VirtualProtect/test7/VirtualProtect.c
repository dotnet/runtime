//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

int __cdecl main(int argc, char *argv[])
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
