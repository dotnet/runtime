// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Test #1 for the wprintf function. A single, basic, test
**          case with no formatting.
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../wprintf.h"

PALTEST(c_runtime_wprintf_test1_paltest_wprintf_test1, "c_runtime/wprintf/test1/paltest_wprintf_test1")
{
    char checkstr[] = "hello world";
    WCHAR *wcheckstr;
    int ret;


    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    wcheckstr = convert(checkstr);
    
    ret = wprintf(wcheckstr);

    if (ret != wcslen(wcheckstr))
    {
        Fail("Expected wprintf to return %d, got %d.\n", 
            wcslen(wcheckstr), ret);

    }

    free(wcheckstr);
    PAL_Terminate();
    return PASS;
}

