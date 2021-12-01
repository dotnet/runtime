// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test1.c
**
** Purpose:   Test #1 for the vsprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../vsprintf.h"

/*
 * Notes: memcmp is used, as is strlen.
 */

PALTEST(c_runtime_vsprintf_test1_paltest_vsprintf_test1, "c_runtime/vsprintf/test1/paltest_vsprintf_test1")
{
    char checkstr[] = "hello world";
    char buf[256] = { 0 };
    int ret;

    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    testvsp(buf, ARRAY_SIZE(buf), "hello world");

    if (memcmp(checkstr, buf, strlen(checkstr)+1) != 0)
    {
        Fail("ERROR: expected \"%s\" (up to %d chars), got \"%s\"\n",
             checkstr, 256, buf);
    }

    testvsp(buf, ARRAY_SIZE(buf), "xxxxxxxxxxxxxxxxx");
    ret = testvsp(buf, ARRAY_SIZE(buf),  "hello world");

    if (ret != strlen(checkstr))
    {
        Fail("ERROR: expected negative return value, got %d", ret);
    }

    if (memcmp(checkstr, buf, ret) != 0)
    {
        Fail("ERROR: expected %s, got %s\n", checkstr, buf);
    }

    PAL_Terminate();
    return PASS;
}
