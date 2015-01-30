//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

int __cdecl main(int argc, char *argv[])
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
