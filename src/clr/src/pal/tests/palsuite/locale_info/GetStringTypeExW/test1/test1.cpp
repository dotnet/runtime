// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source: test1.c
**
** Purpose: Tests GetStringTypeExW with values that will ensure all possible 
**          flags get set once.
**
**
**==========================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    WCHAR wideStr[] = {'9',' '};
    WORD values1[] = { C1_DIGIT, C1_SPACE };
    int len = 2;
    WORD Info[256];
    BOOL ret;
    int i;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    for (i=0; i<len; i++)
    {
        ret = GetStringTypeExW(LOCALE_USER_DEFAULT, CT_CTYPE1, &wideStr[i], 1, &Info[i]);
        if (!ret)
        {
            Fail("GetStringTypeExW failed!\n");
        }
    
        if ((Info[i] & values1[i])!= values1[i])
        {
            
            Fail("GetStringTypeExW returned wrong type info for %c (%d)\n"
                "Expected %#x, got %#x\n", wideStr[i], wideStr[i], 
                values1[i], Info[i]);
            
        }
    }

    PAL_Terminate();

    return PASS;
}

