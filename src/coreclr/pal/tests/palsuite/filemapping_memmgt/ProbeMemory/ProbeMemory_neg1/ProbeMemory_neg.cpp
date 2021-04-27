// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: ReadProcessMemory_neg.c
**
** Purpose: Negative test the ReadProcessMemory API.
**          Call ReadProcessMemory to read unreadabel memory area
**
**
**============================================================*/
#include <palsuite.h>

#define REGIONSIZE 1024

PALTEST(filemapping_memmgt_ProbeMemory_ProbeMemory_neg1_paltest_probememory_probememory_neg1, "filemapping_memmgt/ProbeMemory/ProbeMemory_neg1/paltest_probememory_probememory_neg1")
{
    int err;
    BOOL bResult;
    LPVOID lpProcessAddress = NULL;

    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }
    
    /*allocate the virtual memory*/
    lpProcessAddress = VirtualAlloc(
            NULL,            /*system determine where to allocate the region*/
            REGIONSIZE,      /*specify the size*/
            MEM_RESERVE,     /*allocation type*/
            PAGE_READONLY);  /*access protection*/

    if(NULL == lpProcessAddress)
    {
        Fail("\nFailed to call VirtualAlloc API to allocate "
                "virtual memory, error code=%u\n", GetLastError());
    }

    /*try to probe the unreadable memory area*/
    bResult = PAL_ProbeMemory(
            lpProcessAddress,      /*base of memory area*/
            REGIONSIZE,            /*buffer length in bytes*/
            FALSE);                /*read access*/

    /*check the return value*/
    if(bResult)
    {
        Trace("\nProbeMemory for read didn't FAILED\n");

        /*decommit the specified region*/
        err = VirtualFree(lpProcessAddress, REGIONSIZE, MEM_DECOMMIT);
        if(0 == err)
        {
            Fail("\nFailed to call VirtualFree API, error code=%u\n", GetLastError());
        }

        Fail("");
    }

    /*try to probe the unwriteable memory area*/
    bResult = PAL_ProbeMemory(
            lpProcessAddress,      /*base of memory area*/
            REGIONSIZE,            /*buffer length in bytes*/
            FALSE);                /*write access */

    /*check the return value*/
    if(bResult)
    {
        Trace("\nProbeMemory for write didn't FAILED\n");

        /*decommit the specified region*/
        err = VirtualFree(lpProcessAddress, REGIONSIZE, MEM_DECOMMIT);
        if(0 == err)
        {
            Fail("\nFailed to call VirtualFree API, error code=%u\n", GetLastError());
        }

        Fail("");
    }

    /*decommit the specified region*/
    err = VirtualFree(lpProcessAddress, REGIONSIZE, MEM_DECOMMIT);
    if(0 == err)
    {
        Fail("\nFailed to call VirtualFree API, error code=%u\n", GetLastError());
    }

    PAL_Terminate();
    return PASS;
}
