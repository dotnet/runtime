// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: General test of sscanf_s
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf_s.h"


PALTEST(c_runtime_sscanf_s_test1_paltest_sscanf_test1, "c_runtime/sscanf_s/test1/paltest_sscanf_test1")
{
    int num;
    int ret;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    DoVoidTest("foo bar", "foo ");
    DoVoidTest("foo bar", "baz");
    DoVoidTest("foo bar", "foo %*s");

    DoStrTest("foo % bar", "foo %% %s", "bar");
    DoStrTest("foo bar baz", "foo %bar %s", "baz");

    DoVoidTest("foo bar baz", "foo % bar %s");
    DoVoidTest("foo baz bar", "foo% baz %s");

    ret = sscanf_s("foo bar baz", "foo bar %n", &num);
    if (ret != 0 || num != 8)
    {
        Fail("ERROR: Got incorrect values in scanning \"%s\" using \"%s\".\n"
            "Expected to get a value of %d with return value of %d, "
            "got %d with return %d\n", "foo bar baz", "foo bar %n", 8, 0, 
            num, ret);
           
    }

    PAL_Terminate();
    return PASS;
}
