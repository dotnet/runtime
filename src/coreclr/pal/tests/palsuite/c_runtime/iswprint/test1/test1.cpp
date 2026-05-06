// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests iswprint with all wide characters, ensuring they are 
**          consistent with GetStringTypeExW.
**
**
**===================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_iswprint_test1_paltest_iswprint_test1, "c_runtime/iswprint/test1/paltest_iswprint_test1")
{
    WORD Info;
    int ret;
    int i;
    WCHAR ch;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    for (i=0; i<=0xFFFF; i++)
    {
        ch = i;
        ret = GetStringTypeExW(LOCALE_USER_DEFAULT, CT_CTYPE1, &ch, 1, &Info);
        if (!ret)
        {
            Fail("GetStringTypeExW failed to get information for %#X!\n", ch);
        }

        ret = iswprint(ch);
        if (Info & (C1_BLANK|C1_PUNCT|C1_ALPHA|C1_DIGIT))
        {
            if (!ret)
            {
                Fail("iswprint returned incorrect results for %#X: "
                    "expected printable\n", ch);
            }
        }
        else
        {
            if (ret)
            {
                Fail("iswprint returned incorrect results for %#X: "
                    "expected non-printable\n", ch);
            }
        }
    }

    PAL_Terminate();
    return PASS;
}
