//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose:
** Call the function to copy into an empty buffer.  Check that the return value
** is pointing at the destination buffer.  Also compare the string copied to
** the origional string, to ensure they are the same.
**
**
**==========================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    char dest[80];
    char *result = "foo";
    char str[] = {'f','o','o',0,'b','a','r',0};
    char *ret;

    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    ret = strcpy(dest, str);

    if (ret != dest)
    {
        Fail("Expected strcpy to return %p, got %p!\n", dest, ret);
            
    }

    if (strcmp(dest, result) != 0)
    {
        Fail("Expected strcpy to give \"%s\", got \"%s\"!\n", result, dest);
    }


    PAL_Terminate();

    return PASS;
}
