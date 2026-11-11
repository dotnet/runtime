// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: ReadProcessMemory.c
**
** Purpose: Positive test the ReadProcessMemory API.
**          Call ReadProcessMemory to read memory contents
**          inside current process.
**
**
**============================================================*/
#include <palsuite.h>
#include <stdlib.h>


#define REGIONSIZE 1024

PALTEST(filemapping_memmgt_ProbeMemory_test1_paltest_probememory_test1, "filemapping_memmgt/ProbeMemory/test1/paltest_probememory_test1")
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
    
    lpProcessAddress = malloc(REGIONSIZE);

    if (lpProcessAddress == NULL)
    {
        Fail("\nFailed to allocate memory for ProbeMemory test.\n");
    }

    /*probe the memory for read*/
    bResult = PAL_ProbeMemory(
            lpProcessAddress,      /*base of memory area*/
            REGIONSIZE,            /*buffer length in bytes*/
            FALSE);                /*read access*/

    if(!bResult)
    {
        Trace("\nProbeMemory for read access FAILED\n");

        free(lpProcessAddress);
        Fail("");
    }

    /*probe the memory for write */
    bResult = PAL_ProbeMemory(
            lpProcessAddress,      /*base of memory area*/
            REGIONSIZE,            /*buffer length in bytes*/
            TRUE);                 /*write access*/

    if(!bResult)
    {
        Trace("\nProbeMemory for write access FAILED\n");

        free(lpProcessAddress);
        Fail("");
    }

    free(lpProcessAddress);

    PAL_Terminate();
    return PASS;
}
