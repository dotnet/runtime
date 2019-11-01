// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source: test2.c
**
** Purpose: Tests that MultiByteToWideChar respects the length of the wide 
**          character string.

**
**==========================================================================*/

#include <palsuite.h>

/*
 * For now, it is assumed that MultiByteToWideChar will only be used in the PAL
 * with CP_ACP, and that dwFlags will be 0.
 */

int __cdecl main(int argc, char *argv[])
{    
    char mbStr[128];
    WCHAR wideStr[128];
    int ret;
    int i;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    for (i=0; i<128; i++)
    {
        mbStr[i] = 'a';
        wideStr[i] = 0;
    }

    mbStr[127] = 0;


    ret = MultiByteToWideChar(CP_ACP, 0, mbStr, 10, wideStr, 0);
    if (ret != 10)
    {
        Fail("MultiByteToWideChar did not return correct string length!\n"
            "Got %d, expected %d\n", ret, 10);
    }

    wideStr[10] = (WCHAR) 'b';

    ret = MultiByteToWideChar(CP_ACP, 0, mbStr, 10, wideStr, 128);
    if (ret != 10)
    {
        Fail("MultiByteToWideChar did not return correct string length!\n"
            "Got %d, expected %d\n", ret, 10);
    }

    if (wideStr[10] != 'b')
    {
        Fail("WideCharToMultiByte overflowed the destination buffer!\n");
    }

    PAL_Terminate();

    return PASS;
}

