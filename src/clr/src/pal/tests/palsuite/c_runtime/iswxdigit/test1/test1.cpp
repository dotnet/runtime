// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests iswxdigit with every possible wide character, ensuring it 
**          returns the correct results.
**
**
**===================================================================*/

#include <palsuite.h>

/*
 *  These are the only wide characters Win2000 recogonizes as valid hex digits.
 */
WCHAR ValidHexDigits[] = 
{
    48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 65, 66, 67, 68, 69, 70, 97, 98, 99, 100,
    101, 102, 65296, 65297, 65298, 65299, 65300, 65301, 65302, 65303, 65304, 65305,
    65313, 65314, 65315, 65316, 65317, 65318, 65345, 65346, 65347, 65348, 65349, 65350,
    0
};

int __cdecl main(int argc, char **argv)
{   
    int i;
    WCHAR c;
    int ret;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    for (i=0; i<=0xFFFF; i++)
    {
        ret = iswxdigit(i);
        c = (WCHAR) i;

        if (ret)
        {
            if (wcschr(ValidHexDigits, c) == NULL)
            {
                /* iswxdigit says its a hex digit.  We know better */
                Fail("iswxdigit incorrectly found %#x to be a hex digit!\n", c);
            }
        }
        else if (wcschr(ValidHexDigits, c) != NULL && c != 0)
        {
            /* iswxdigit says it isn't a hex digit.  We know better */
            Fail("iswxdigit failed to find %#x to be a hex digit!\n", c);
        }
    }

    PAL_Terminate();
    return PASS;
}
