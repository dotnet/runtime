// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source:  virtualquery.c
**
** Purpose: Positive test the VirtualQuery API.
**          Call VirtualQuery to get the virtual
**          page info
**
**
**============================================================*/
#include <palsuite.h>

#define VIRTUALMEMORYSIZE 1024

int __cdecl main(int argc, char *argv[])
{
    int err;
    LPVOID lpVirtualAddress;
    MEMORY_BASIC_INFORMATION PageInfo;
    DWORD dwBufferSize;

    //Initialize the PAL environment
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        ExitProcess(FAIL);
    }

    //Allocate the physical storage in memory or in the paging file on disk
    lpVirtualAddress = VirtualAlloc(NULL,//determine where to allocate the region
            VIRTUALMEMORYSIZE,  //specify the size
            MEM_COMMIT,      //allocation type
            PAGE_READONLY);  //access protection
    if(NULL == lpVirtualAddress)
    {
        Fail("\nFailed to call VirtualAlloc API!\n");
    }

    //get the virtual page info
    dwBufferSize =
 VirtualQuery(lpVirtualAddress,&PageInfo,sizeof(MEMORY_BASIC_INFORMATION));

    if(dwBufferSize <= 0 ||
        PageInfo.RegionSize <=0 ||
        PAGE_READONLY != PageInfo.AllocationProtect ||
        MEM_COMMIT != PageInfo.State)
    {
        Fail("\nFailed to call VirtualQuery API!\n");
    }



    //decommit the specified region
    err = VirtualFree(lpVirtualAddress, //virtual page base address
                VIRTUALMEMORYSIZE,//specify the size
                MEM_DECOMMIT);//free operation
    if(0 == err)
    {
        Fail("\nFailed to call VirtualFree API!\n");
    }

    PAL_Terminate();
    return PASS;
}
