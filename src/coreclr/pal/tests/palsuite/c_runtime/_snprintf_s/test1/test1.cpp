// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: General test to see if sprintf_s works correctly
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../_snprintf_s.h"

/*
 * Notes: memcmp is used, as is strlen.
 */

PALTEST(c_runtime__snprintf_s_test1_paltest_snprintf_test1, "c_runtime/_snprintf_s/test1/paltest_snprintf_test1")
{
    char checkstr[] = "hello world";
    char buf[256] = { 0 };
    int ret;

    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    _snprintf_s(buf, 256, _TRUNCATE, "hello world");
    if (memcmp(checkstr, buf, strlen(checkstr)+1) != 0)
    {
        Fail("ERROR: expected \"%s\" (up to %d chars), got \"%s\"\n",
             checkstr, 256, buf);
    }

    _snprintf_s(buf, 256, _TRUNCATE, "xxxxxxxxxxxxxxxxx");
    ret = _snprintf_s(buf, 8, _TRUNCATE, "hello world");

    if (ret >= 0)
    {
        Fail("ERROR: expected negative return value, got %d", ret);
    }
    if (memcmp(checkstr, buf, 7) != 0 || buf[7] != 0 || buf[8] != 'x')
    {
        Fail("ERROR: expected %s (up to %d chars), got %s\n",
              checkstr, 8, buf);
    }
    
    PAL_Terminate();
    return PASS;
}
