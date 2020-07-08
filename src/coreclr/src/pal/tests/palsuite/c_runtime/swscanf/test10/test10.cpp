// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test10.c
**
** Purpose:Tests swscanf with wide characters 
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swscanf.h"

int __cdecl main(int argc, char *argv[])
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoCharTest(convert("1234"), convert("%C"), "1", 1);
    DoCharTest(convert("abc"), convert("%2C"), "ab", 2);
    DoCharTest(convert(" ab"), convert("%C"), " ", 1);
    DoCharTest(convert("ab"), convert("%hC"), "a", 1);
    DoWCharTest(convert("ab"), convert("%lC"), convert("a"), 1);
    DoCharTest(convert("ab"), convert("%LC"), "a", 1);
    DoCharTest(convert("ab"), convert("%I64C"), "a", 1);

    PAL_Terminate();
    return PASS;
}
