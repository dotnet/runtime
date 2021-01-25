// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: LocalFree.c
**
** Purpose: Positive test the LocalFree API.
**          call LocalFree by passing NULL as local memory 
**          object handle 
**
**
**============================================================*/
#include <palsuite.h>

PALTEST(filemapping_memmgt_LocalFree_test2_paltest_localfree_test2, "filemapping_memmgt/LocalFree/test2/paltest_localfree_test2")
{
    HLOCAL FreeHeap;
    int err;

    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }    

    /*call LocalFree by passing NULL as local memory object handle*/
    FreeHeap = LocalFree(NULL);
    if(FreeHeap)
    {
        Fail("Failed to call LocalFree API, "
            "error code=%u\n", GetLastError());
    }

    PAL_Terminate();
    return PASS;
}
