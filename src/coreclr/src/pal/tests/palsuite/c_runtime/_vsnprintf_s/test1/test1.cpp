// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test1.c
**
** Purpose:   Test #1 for the _vsnprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../_vsnprintf_s.h"

/*
 * Notes: memcmp is used, as is strlen.
 */

PALTEST(c_runtime__vsnprintf_s_test1_paltest_vsnprintf_test1, "c_runtime/_vsnprintf_s/test1/paltest_vsnprintf_test1")
{
    char checkstr[] = "hello world";
    char buf[256] = { 0 };
    int ret;
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    Testvsnprintf(buf, 256, "hello world");

    if (memcmp(checkstr, buf, strlen(checkstr)+1) != 0)
    {
        Fail("ERROR: expected \"%s\" (up to %d chars), got \"%s\"\n",
             checkstr, 256, buf);
    }

    Testvsnprintf(buf, 256, "xxxxxxxxxxxxxxxxx");

    ret = Testvsnprintf(buf, 8, "hello world");

    if (ret >= 0)
    {
        Fail("ERROR: expected negative return value, got %d", ret);
    }
    if (memcmp(checkstr, buf, 7) != 0 || buf[7] != 0)
    {
        Fail("ERROR: expected %s (up to %d chars), got %s\n", checkstr, 8, buf);
    }

    PAL_Terminate();
    return PASS;
}
