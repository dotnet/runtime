//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: 
** Tests to see that wcsncpy correctly copies wide strings, including handling 
** the count argument correctly (copying no more that count characters, not 
** automatically adding a null, and padding if necessary).
**
**
**==========================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    WCHAR dest[80];
    WCHAR result[] = {'f','o','o','b','a','r',0};
    WCHAR str[] = {'f','o','o','b','a','r',0,'b','a','z',0};
    WCHAR *ret;
    int i;
    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    for (i=0; i<80; i++)
    {
        dest[i] = 'x';
    }

    ret = wcsncpy(dest, str, 3);
    if (ret != dest)
    {
        Fail("Expected wcsncpy to return %p, got %p!\n", dest, ret);        
    }

    if (wcsncmp(dest, result, 3) != 0)
    {
        Fail("Expected wcsncpy to give \"%S\", got \"%S\"!\n", result, dest);
    }

    if (dest[3] != (WCHAR)'x')
    {
        Fail("wcsncpy overflowed!\n");
    }

    ret = wcsncpy(dest, str, 40);
    if (ret != dest)
    {
        Fail("Expected wcsncpy to return %p, got %p!\n", dest, ret);        
    }

    if (wcscmp(dest, result) != 0)
    {
        Fail("Expected wcsncpy to give \"%S\", got \"%S\"!\n", result, dest);
    }

    for (i=wcslen(str); i<40; i++)
    {
        if (dest[i] != 0)
        {
            Fail("wcsncpy failed to pad the destination with NULLs!\n");
        }
    }

    if (dest[40] != (WCHAR)'x')
    {
        Fail("wcsncpy overflowed!\n");
    }
    


    PAL_Terminate();

    return PASS;
}
