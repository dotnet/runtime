// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: 
** Tests to see that wcsrchr correctly returns a pointer to the last occurence 
** of a character in a a string.
**
**
**==========================================================================*/



#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    WCHAR str[] = {'f','o','o',' ','b','a','r',' ','b','a','z',0};
    WCHAR c = (WCHAR)' ';
    WCHAR c2 = (WCHAR)'$';
    WCHAR *ptr;
    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    ptr = wcsrchr(str, c);
    if (ptr != str + 7)
    {
        Fail("ERROR: expected wcsrchr to return pointer to %p, got %p\n", 
            str + 7, ptr);
    }

    ptr = wcsrchr(str, c2);
    if (ptr != NULL)
    {
        Fail("ERROR: expected wcsrchr to return pointer to %p, got %p\n", 
            NULL, ptr);
    }

    PAL_Terminate();
    return PASS;
}

