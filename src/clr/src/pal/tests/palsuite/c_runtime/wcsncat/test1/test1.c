// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose:
** Tests that wcsncat correctly appends wide strings, making sure it handles
** count argument correctly (appending no more than count characters, always
** placing a null, and padding the string if necessary).
**
**
**==========================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    WCHAR dest[80];
    WCHAR test[] = {'f','o','o',' ','b','a','r','b','a','z',0};
    WCHAR str1[] = {'f','o','o',' ',0};
    WCHAR str2[] = {'b','a','r',' ',0};
    WCHAR str3[] = {'b','a','z',0};
    WCHAR *ptr;
    int i;

    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    dest[0] = 0;
    for (i=1; i<80; i++)
    {
        dest[i] = (WCHAR)'x';
    }

    ptr = wcsncat(dest, str1, wcslen(str1));
    if (ptr != dest)
    {
        Fail("ERROR: Expected wcsncat to return ptr to %p, got %p", dest, ptr);
    }

    ptr = wcsncat(dest, str2, 3);
    if (ptr != dest)
    {
        Fail("ERROR: Expected wcsncat to return ptr to %p, got %p", dest, ptr);
    }
    if (dest[7] != 0)
    {
        Fail("ERROR: wcsncat did not place a terminating NULL!");
    }

    ptr = wcsncat(dest, str3, 20);
    if (ptr != dest)
    {
        Fail("ERROR: Expected wcsncat to return ptr to %p, got %p", dest, ptr);
    }
    if (wcscmp(dest, test) != 0)
    {
        Fail("ERROR: Expected wcsncat to give \"%S\", got \"%S\"\n", 
            test, dest);
    }
    if (dest[wcslen(test)+1] != (WCHAR)'x')
    {
        Fail("wcsncat went out of bounds!\n");
    }

    PAL_Terminate();

    return PASS;
}
