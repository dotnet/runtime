//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Tests that wcscmp correctly compares two strings with 
** case sensitivity.
**
**
**==========================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    WCHAR str1[] = {'f','o','o',0};
    WCHAR str2[] = {'f','o','o','x',0};
    WCHAR str3[] = {'f','O','o',0};
    char cstr1[] = "foo";
    char cstr2[] = "foox";
    char cstr3[] = "fOo";
    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }



    if (wcscmp(str1, str2) >= 0)
    {
        Fail("ERROR: wcscmp(\"%s\", \"%s\") returned >= 0\n", cstr1, cstr2);
    }

    if (wcscmp(str2, str1) <= 0)
    {
        Fail("ERROR: wcscmp(\"%s\", \"%s\") returned <= 0\n", cstr2, cstr1);
    }

    if (wcscmp(str1, str3) <= 0)
    {
        Fail("ERROR: wcscmp(\"%s\", \"%s\") returned >= 0\n", cstr1, cstr3);
    }

    if (wcscmp(str3, str1) >= 0)
    {
        Fail("ERROR: wcscmp(\"%s\", \"%s\") returned >= 0\n", cstr3, cstr1);
    }

    PAL_Terminate();

    return PASS;
}
