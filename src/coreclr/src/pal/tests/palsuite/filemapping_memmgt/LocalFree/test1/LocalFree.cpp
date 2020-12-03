// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: LocalFree.c
**
** Purpose: Positive test the LocalFree API.
**          Call LocalFree to free a specified local memory object 
**
**
**============================================================*/
#include <palsuite.h>

PALTEST(filemapping_memmgt_LocalFree_test1_paltest_localfree_test1, "filemapping_memmgt/LocalFree/test1/paltest_localfree_test1")
{
    HLOCAL LocalHeap;
    HLOCAL FreeHeap;
    int err;
    const SIZE_T heapSize = 64;

    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }    

    /*Allocate the specified number of bytes from the heap*/
    /*with zero ad the allocation attribute*/
    LocalHeap = LocalAlloc(0, heapSize);
    if(!LocalHeap)
    {
        Fail("\nFailed to call LocalAlloc API, "
            "error code=%u\n", GetLastError());
    }
    
    /*Free the allocated local heap memory*/
    FreeHeap = LocalFree(LocalHeap);
    if(FreeHeap)
    {
        Fail("Failed to call LocalFree API, "
            "error code=%u\n", GetLastError());
    }

    PAL_Terminate();
    return PASS;
}
