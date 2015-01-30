//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: General test of swscanf
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swscanf.h"


int __cdecl main(int argc, char *argv[])
{
    int num;
    int ret;
    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    DoVoidTest(convert("foo bar"), convert("foo"));
    DoVoidTest(convert("foo bar"), convert("baz"));
    DoVoidTest(convert("foo bar"), convert("foo %*s"));

    DoStrTest(convert("foo % bar"), convert("foo %% %S"), "bar");
    DoStrTest(convert("foo bar baz"), convert("foo %bar %S"), "baz");

    DoVoidTest(convert("foo bar baz"), convert("foo % bar %S"));
    DoVoidTest(convert("foo bar baz"), convert("foo% bar %S"));


    ret = swscanf(convert("foo bar baz"), convert("foo bar %n"), &num);
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
