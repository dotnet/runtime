//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source: test2.c
**
** Purpose: Tests that GetCPInfo gives the correct information for codepage 0x4E4 
**          (the default).
**
**
**==========================================================================*/


#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    CPINFO cpinfo;
    int codepage;
    unsigned int i;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /*
     * codepage 1252 (0x4E4): Windows 3.1 Latin 1 (U.S., Western Europe)
     */
    codepage = 1252;
    
    if (!GetCPInfo(codepage, &cpinfo))
    {
        Fail("GetCPInfo() failed on default ansi code page!\n");
    }
    if (cpinfo.MaxCharSize != 1)
    {
        Fail("GetCPInfo() returned incorrect MaxCharSize information!\n");
    }
    if (cpinfo.DefaultChar[0] != '?' || cpinfo.DefaultChar[1] != 0)
    {
        Fail("GetCPInfo() returned incorrect DefaultChar information");
    }

    for (i = 0; i<MAX_LEADBYTES; i++)
    {
        if (cpinfo.LeadByte[i] != 0)
        {
            Fail("GetCPInfo() returned incorrect LeadByte information");
        }
    }

    PAL_Terminate();

    return PASS;
}

