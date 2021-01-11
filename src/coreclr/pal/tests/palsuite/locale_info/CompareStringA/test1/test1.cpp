// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source: test1.c
**
** Purpose: Tests that CompareStringA returns the correct value and can handle
**          invalid parameters.
**
**
**==========================================================================*/

#define CSTR_LESS_THAN 1
#define CSTR_EQUAL 2
#define CSTR_GREATER_THAN 3

#include <palsuite.h>

PALTEST(locale_info_CompareStringA_test1_paltest_comparestringa_test1, "locale_info/CompareStringA/test1/paltest_comparestringa_test1")
{    
    char str1[] = {'f','o','o',0};
    char str2[] = {'f','o','o','x',0};
    char str3[] = {'f','O','o',0};
    int flags = NORM_IGNORECASE | NORM_IGNOREWIDTH;
    int ret;

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    ret = CompareStringA(0x0409, flags, str1, -1, str2, -1);
    if (ret != CSTR_LESS_THAN)
    {
        Fail("CompareStringA with \"%S\" (%d) and \"%S\" (%d) did not return "
            "CSTR_LESS_THAN!\n", str1, -1, str2, -1);
    }

    ret = CompareStringA(0x0409, flags, str1, -1, str2, 3);
    if (ret != CSTR_EQUAL)
    {
        Fail("CompareStringA with \"%S\" (%d) and \"%S\" (%d) did not return "
            "CSTR_EQUAL!\n", str1, -1, str2, 3);
    }

    ret = CompareStringA(0x0409, flags, str2, -1, str1, -1);
    if (ret != CSTR_GREATER_THAN)
    {
        Fail("CompareStringA with \"%S\" (%d) and \"%S\" (%d) did not return "
            "CSTR_GREATER_THAN!\n", str2, -1, str1, -1);
    }

    ret = CompareStringA(0x0409, flags, str1, -1, str3, -1);
    if (ret != CSTR_EQUAL)
    {
        Fail("CompareStringA with \"%S\" (%d) and \"%S\" (%d) did not return "
            "CSTR_EQUAL!\n", str1, -1, str3, -1);
    }

    ret = CompareStringA(0x0409, flags, str3, -1, str1, -1);
    if (ret != CSTR_EQUAL)
    {
        Fail("CompareStringA with \"%S\" (%d) and \"%S\" (%d) did not return "
            "CSTR_EQUAL!\n", str3, -1, str1, -1);
    }

    ret = CompareStringA(0x0409, flags, str3, -1, str1, -1);
    if (ret != CSTR_EQUAL)
    {
        Fail("CompareStringA with \"%S\" (%d) and \"%S\" (%d) did not return "
            "CSTR_EQUAL!\n", str3, -1, str1, -1);
    }

    ret = CompareStringA(0x0409, flags, str1, 0, str3, -1);
    if (ret != CSTR_LESS_THAN)
    {
        Fail("CompareStringA with \"%S\" (%d) and \"%S\" (%d) did not return "
            "CSTR_GREATER_THAN!\n", str1, 0, str3, -1);
    }

    
    ret = CompareStringA(0x0409, flags, NULL, -1, str3, -1);
    if (ret != 0)
    {
        Fail("CompareStringA should have returned 0, got %d!\n", ret);
    }
    if (GetLastError() != ERROR_INVALID_PARAMETER)
    {
        Fail("CompareStringA should have set the last error to "
            "ERROR_INVALID_PARAMETER!\n");
    }

    PAL_Terminate();

    return PASS;
}

