// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Checks every character against the known range of digits.
**
**
**==========================================================================*/


#include <palsuite.h>

PALTEST(c_runtime_isdigit_test1_paltest_isdigit_test1, "c_runtime/isdigit/test1/paltest_isdigit_test1")
{
    int i;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    for (i=0; i<256; i++)
    {
        if (isdigit(i))
        {
            if (i < '0' || i > '9')
            {
                Fail("ERROR: isdigit returned true for '%c' (%d)!\n", i, i);
            }
        }
        else
        {
            if (i >= '0' && i <= '9')
            {
                Fail("ERROR: isdigit returned false for '%c' (%d)!\n", i, i);
            }
        }
    }

    PAL_Terminate();
    return PASS;
}
