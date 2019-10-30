// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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


int __cdecl main(int argc, char *argv[])
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
