// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Run through every possible character.  For each time that 
** isxdigit returns:
** 1, check through a list of the known hex characters to ensure that it
** is really a hex char.  Also, when it returns 0, ensure that that character
** isn't a hex character.
**
**
**==========================================================================*/



#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    int i;

    /* Initialize the PAL */
    if ( 0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /* Loop through each character and call isxdigit for each character */
    for (i=1; i<256; i++)
    {

        if (isxdigit(i) == 0)
        {
            if( ((i>=48) && (i<=57)) || ((i>=97) && (i<=102)) ||
                ((i>=65) && (i<=70)) )
            {
                Fail("ERROR: isxdigit() returns true for '%c' (%d)\n", i, i);
            }
        }
        else
        {
            if( ((i<48) && (i>58)) || ((i<97) && (i>102)) ||
                ((i<65) && (i>70)) )
            {
                Fail("ERROR: isxdigit() returns false for '%c' (%d)\n", i, i);
            }
        }
    }

    PAL_Terminate();
    return PASS;
}
