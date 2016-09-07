// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source: test2.c
**
** Purpose: Tests GetStringTypeExW with values from every possible unicode 
**          category.
**
**
**==========================================================================*/

#include <palsuite.h>


/*
 * A random selection of unicode characters, each representing a distinct 
 * unicode category
 */
WCHAR TestStr[] = 
{
    0x05D0, /* 4, HEBREW LETTER ALEF */
    0x0488, /* 7, COMBINING CYRILLIC HUNDRED THOUSANDS SIGN */
    0x0030, /* 8, DIGIT ZERO */
    0x0020, /* 22, SPACE */     
};

#define TEST_LEN (sizeof(TestStr) / sizeof(WCHAR))

WORD TestFlags[TEST_LEN] =
{
    C1_ALPHA,
    0,
    C1_DIGIT,
    C1_BLANK|C1_SPACE
};

int __cdecl main(int argc, char *argv[])
{
    WORD Info;
    BOOL ret;
    int i;

    /* check only the bits listed in rotor_pal.doc */
    const WORD PAL_VALID_C1_BITS = C1_DIGIT | C1_SPACE;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    for (i=0; i <TEST_LEN; i++)
    {
        ret = GetStringTypeExW(LOCALE_USER_DEFAULT, CT_CTYPE1, &TestStr[i], 1, &Info);

        if (!ret)
        {
            Fail("GetStringTypeExW failed!\n");
        }

        if ((Info & PAL_VALID_C1_BITS) != (TestFlags[i] & PAL_VALID_C1_BITS))
        {
            
            Fail("GetStringTypeExW (test #%i) returned wrong type info for %c (%d)\n"
                "Expected %#x, got %#x\n", i, TestStr[i], TestStr[i], 
                TestFlags[i], Info);
            
        }
    }

    PAL_Terminate();

    return PASS;
}


