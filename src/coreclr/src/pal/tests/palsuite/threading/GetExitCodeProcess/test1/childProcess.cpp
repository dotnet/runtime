// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: childprocess.c
**
** Purpose: Test to ensure GetExitCodeProcess returns the right 
** value. All this program does is return a predefined value.
**
** Dependencies: none
** 

**
**=========================================================*/

#include <pal.h>
#include "myexitcode.h"
#include <palsuite.h>

PALTEST(threading_GetExitCodeProcess_test1_paltest_getexitcodeprocess_test1_child, "threading/GetExitCodeProcess/test1/paltest_getexitcodeprocess_test1_child")
{
    int i;
    
    // simulate some activity 
    for( i=0; i<10000; i++ )
        ;
        
    // return the predefined exit code
    return TEST_EXIT_CODE;
}
