//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: 
** Concatenate a few strings together, setting different lengths to be 
** used for each one.  Check to ensure the pointers which are returned are
** correct, and that the final string is what was expected.
**
**
**==========================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    char dest[80];
    char *test = "foo barbaz";
    char *str1 = "foo ";
    char *str2 = "bar ";
    char *str3 = "baz";
    char *ptr;
    int i;

    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    dest[0] = 0;
    for (i=1; i<80; i++)
    {
        dest[i] = 'x';
    }

    ptr = strncat(dest, str1, strlen(str1));
    if (ptr != dest)
    {
        Fail("ERROR: Expected strncat to return ptr to %p, got %p", dest, ptr);
    }

    ptr = strncat(dest, str2, 3);
    if (ptr != dest)
    {
        Fail("ERROR: Expected strncat to return ptr to %p, got %p", dest, ptr);
    }
    if (dest[7] != 0)
    {
        Fail("ERROR: strncat did not place a terminating NULL!");
    }

    ptr = strncat(dest, str3, 20);
    if (ptr != dest)
    {
        Fail("ERROR: Expected strncat to return ptr to %p, got %p", dest, ptr);
    }
    if (strcmp(dest, test) != 0)
    {
        Fail("ERROR: Expected strncat to give \"%s\", got \"%s\"\n", 
            test, dest);
    }
    if (dest[strlen(test)+1] != 'x')
    {
        Fail("strncat went out of bounds!\n");
    }

    PAL_Terminate();

    return PASS;
}
