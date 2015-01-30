//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================
**
** Source: UnmapViewOfFile.c (test 2)
**
** Purpose: Negative test the UnmapViewOfFile API.
**          Call UnmapViewOfFile to unmap a view of
**          NULL.
**
**
**============================================================*/
#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    int err;

    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }

    /* Negative test the UnmapViewOfFile by passing a NULL*/
    /* mapping address handle*/
    err = UnmapViewOfFile(NULL);
    if(0 != err)
    {
        Fail("ERROR: Able to call UnmapViewOfFile API "
             "by passing a NULL mapping address.\n" );

    }

    /* Terminate the PAL.
     */
    PAL_Terminate();
    return PASS;
}
