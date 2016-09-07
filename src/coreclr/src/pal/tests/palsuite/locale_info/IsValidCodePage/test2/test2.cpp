// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source: test2.c
**
** Purpose: Tests IsValidCodePage with all the code pages valid on W2K.
**
**
**==========================================================================*/



#include <palsuite.h>

UINT ValidCodePages[] = 
{
    0x25, 0x1b5, 0x1f4, 0x352, 0x35c, 0x35d, 0x35f, 0x361, 
    0x36a, 0x3a4, 0x3a8, 0x3b5, 0x3b6, 0x4e2, 0x4e3, 0x4e4, 0x4e5, 0x4e6, 
    0x4e7, 0x4e8, 0x4e9, 0x4ea, 0x2710, 0x275f, 0x4e9f, 0x4f25, 0x5182, 0x6faf,
    0x6fb0, 0x6fbd, 0xfde8, 0xfde9
};


int NumValidPages = sizeof(ValidCodePages) / sizeof(ValidCodePages[0]);
int __cdecl main(int argc, char *argv[])
{
    int i;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    for (i=0; i<NumValidPages; i++)
    {
        if (!IsValidCodePage(ValidCodePages[i]))
        {
            Fail("IsValidCodePage() found code page %#x invalid!\n", 
                ValidCodePages[i]);
        }
    }


    PAL_Terminate();

    return PASS;
}

