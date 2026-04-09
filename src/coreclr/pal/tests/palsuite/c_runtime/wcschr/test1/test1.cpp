// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose:
** Tests that wcschr correctly finds the first occurrence of a character in a
** string
**
**
**==========================================================================*/



#include <palsuite.h>

PALTEST(c_runtime_wcschr_test1_paltest_wcschr_test1, "c_runtime/wcschr/test1/paltest_wcschr_test1")
{
    WCHAR str[] = {'f','o','o',' ','b','a','r',' ',0};
    WCHAR c = (WCHAR)' ';
    WCHAR c2 = (WCHAR)'$';
    WCHAR *ptr;

    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    ptr = wcschr(str, c);
    if (ptr != str + 3)
    {
        Fail("ERROR: expected wcschr to return pointer to %p, got %p\n", 
            str + 3, ptr);
    }

    ptr = wcschr(str, c2);
    if (ptr != NULL)
    {
        Fail("ERROR: expected wcschr to return pointer to %p, got %p\n", 
            NULL, ptr);
    }
        
    PAL_Terminate();
    return PASS;
}

