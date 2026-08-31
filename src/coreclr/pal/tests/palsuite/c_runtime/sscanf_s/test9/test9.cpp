// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test9.c
**
** Purpose: Tests sscanf_s with characters
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf_s.h"


PALTEST(c_runtime_sscanf_s_test9_paltest_sscanf_test9, "c_runtime/sscanf_s/test9/paltest_sscanf_test9")
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoCharTest("1234d", "%c", "1", 1);
    DoCharTest("1234d", "%c", "1", 1);
    DoCharTest("abc", "%2c", "ab", 2);
    DoCharTest(" ab", "%c", " ", 1);
    DoCharTest("ab", "%hc", "a", 1);
    DoWCharTest("ab", "%lc", convert("a"), 1);
    DoCharTest("ab", "%Lc", "a", 1);
    DoCharTest("ab", "%I64c", "a", 1);

    PAL_Terminate();
    return PASS;
}
