// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose:
** Concatenate three strings into one string.  Each time, check to ensure 
** the pointer returned was what we expected.  When finished, compare the 
** newly formed string to what it should be to ensure no characters were 
** lost.
**
**
**==========================================================================*/

#include <palsuite.h>


PALTEST(c_runtime_strcat_test1_paltest_strcat_test1, "c_runtime/strcat/test1/paltest_strcat_test1")
{
    char dest[80];
    char *test = "foo bar baz";
    char *str1 = "foo ";
    char *str2 = "bar ";
    char *str3 = "baz";
    char *ptr;

    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    dest[0] = 0;

    ptr = strcat(dest, str1);
    if (ptr != dest)
    {
        Fail("ERROR: Expected strcat to return ptr to %p, got %p", dest, ptr);
    }

    ptr = strcat(dest, str2);
    if (ptr != dest)
    {
        Fail("ERROR: Expected strcat to return ptr to %p, got %p", dest, ptr);
    }

    ptr = strcat(dest, str3);
    if (ptr != dest)
    {
        Fail("ERROR: Expected strcat to return ptr to %p, got %p", dest, ptr);
    }

    if (strcmp(dest, test) != 0)
    {
        Fail("ERROR: Expected strcat to give \"%s\", got \"%s\"\n", 
            test, dest);
    }

    PAL_Terminate();

    return PASS;
}
