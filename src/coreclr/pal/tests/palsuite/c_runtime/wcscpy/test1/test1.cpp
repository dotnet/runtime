// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Tests that wcscpy correctly copies a null-terminated wide string.
**
**
**==========================================================================*/


#include <palsuite.h>

/*
 * Notes: uses memcmp and sprintf_s.
 */

PALTEST(c_runtime_wcscpy_test1_paltest_wcscpy_test1, "c_runtime/wcscpy/test1/paltest_wcscpy_test1")
{
    WCHAR str[] = {'f','o','o',0,'b','a','r',0};
    WCHAR dest[80];
    WCHAR result[] = {'f','o','o',0};
    WCHAR *ret;
    char buffer[256];


    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    ret = wcscpy(dest, str);

    if (ret != dest || memcmp(dest, result, sizeof(result)) != 0)
    {
        sprintf_s(buffer, ARRAY_SIZE(buffer), "%S", dest);
        Fail("Expected wcscpy to give \"%s\" with a return value of %p, got \"%s\" "
            "with a return value of %p.\n", "foo", dest, buffer, ret);
    }

    PAL_Terminate();
    return PASS;
}
