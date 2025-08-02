// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: childprocess.c
**
** Purpose: Test to ensure OpenProcess works properly.
** All this program does is return a predefined value.
**
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               CreateMutexW
**               WaitForSingleObject
**               CloseHandle
**
**
**=========================================================*/

#include <palsuite.h>
#include "myexitcode.h"


PALTEST(threading_OpenProcess_test1_paltest_openprocess_test1_child, "threading/OpenProcess/test1/paltest_openprocess_test1_child")
{
    DWORD dwRet;
    int i;

    /* initialize the PAL */
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return( FAIL );
    }

    /* simulate some activity  */
    for( i=0; i<50000; i++ )
        ;

    /* terminate the PAL */
    PAL_Terminate();

    /* return the predefined exit code */
    return TEST_EXIT_CODE;
}
