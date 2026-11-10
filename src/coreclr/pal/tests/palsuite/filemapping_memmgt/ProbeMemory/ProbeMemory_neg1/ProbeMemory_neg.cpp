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
#include <sys/mman.h>
#include <stdlib.h>


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
    
    lpProcessAddress = mmap(NULL, REGIONSIZE, PROT_NONE, MAP_ANON | MAP_PRIVATE, -1, 0);

    if (lpProcessAddress == MAP_FAILED)
    {
        Fail("\nFailed to allocate inaccessible memory for ProbeMemory test.\n");
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

        munmap(lpProcessAddress, REGIONSIZE);
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

        munmap(lpProcessAddress, REGIONSIZE);
        Fail("");
    }

    munmap(lpProcessAddress, REGIONSIZE);

    PAL_Terminate();
    return PASS;
}
