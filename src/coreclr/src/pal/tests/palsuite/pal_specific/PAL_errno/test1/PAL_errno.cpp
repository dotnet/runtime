// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: pal_errno.c
**
** Purpose: Positive test the PAL_errno API.
**          call PAL_errno to retrieve the pointer to 
**          the per-thread errno value.
**
**
**============================================================*/
#include <palsuite.h>

PALTEST(pal_specific_PAL_errno_test1_paltest_pal_errno_test1, "pal_specific/PAL_errno/test1/paltest_pal_errno_test1")
{
    int err;
    FILE *pFile = NULL;

    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if( 0 != err)
    {
        return FAIL;
    }
    
    /*Try to open a not-exist file to read to generate an error*/
    pFile = fopen( "no_exist_file_name", "r" );
    
    if( NULL != pFile )
    {
        Trace("\nFailed to call fopen to open a not exist for reading, "
                "an error is expected, but no error occurred\n");

        if( EOF == fclose( pFile ) )
        {
            Trace("\nFailed to call fclose to close a file stream\n");
        }
        Fail( "Test failed! fopen() Should not have worked!" );
    }

    /*retrieve the per-thread error value pointer*/
    if( 2 != errno )
    {
        Fail("\nFailed to call PAL_errno API, this value is not correct."
             " The correct value is ENOENT[2] ( No such file or directory.).\n");
    }
    
    PAL_Terminate();
    return PASS;
}
