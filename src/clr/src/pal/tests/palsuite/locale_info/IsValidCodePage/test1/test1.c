//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source: test1.c
**
** Purpose: Tests IsValidCodePage with a collection of valid and invalid 
**          code pages.
**
**
**==========================================================================*/


#include <palsuite.h>


UINT InvalidCodePages[] = 
{
    0, 0x1, 0x2, 0x3, 0xfff
};

int NumInvalidPages = sizeof(InvalidCodePages) / sizeof(InvalidCodePages[0]);

int __cdecl main(int argc, char *argv[])
{
    int i;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

   

    for (i=0; i<NumInvalidPages; i++)
    {
        if (IsValidCodePage(InvalidCodePages[i]))
        {
            Fail("IsValidCodePage() found code page %#x valid!\n", 
                InvalidCodePages[i]);
        }
    }

    PAL_Terminate();

    return PASS;
}

