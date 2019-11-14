// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

int __cdecl main(int argc, char *argv[])
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

