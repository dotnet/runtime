// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

PALTEST(filemapping_memmgt_UnmapViewOfFile_test2_paltest_unmapviewoffile_test2, "filemapping_memmgt/UnmapViewOfFile/test2/paltest_unmapviewoffile_test2")
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
