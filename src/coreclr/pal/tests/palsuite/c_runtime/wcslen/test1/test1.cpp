// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose:
** Tests that wcslen correctly returns the length (in wide characters,
** not byte) of a wide string
**
**
**==========================================================================*/



#include <palsuite.h>

PALTEST(c_runtime_wcslen_test1_paltest_wcslen_test1, "c_runtime/wcslen/test1/paltest_wcslen_test1")
{
    WCHAR str1[] = {'f','o','o',' ',0};
    WCHAR str2[] = {0};
    int ret;

    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    ret = wcslen(str1);
    if (ret != 4)
    {
        Fail("ERROR: Expected wcslen of \"foo \" to be 4, got %d\n", ret);
    }
        
    ret = wcslen(str2);
    if (ret != 0)
    {
        Fail("ERROR: Expected wcslen of \"\" to be 0, got %d\n", ret);
    }


    PAL_Terminate();
    return PASS;
}

