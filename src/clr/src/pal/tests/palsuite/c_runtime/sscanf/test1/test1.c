//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: General test of sscanf
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf.h"


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

    ret = sscanf("foo bar baz", "foo bar %n", &num);
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
