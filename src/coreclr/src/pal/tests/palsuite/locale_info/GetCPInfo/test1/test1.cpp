// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source: test1.c
**
** Purpose: Tests that GetCPInfo works for CP_ACP and 0x4E4 (default codepage)
**          Also makes sure it correctly handles an invalid code page.
**
**
**==========================================================================*/


#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    CPINFO cpinfo;

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    if (!GetCPInfo(CP_ACP, &cpinfo))
    {
        Fail("GetCPInfo() unable to get info for CP_ACP\n");
    }

    if (!GetCPInfo(65001, &cpinfo))
    {
        Fail("GetCPInfo() unable to get info for code page 65001 (utf8)\n");
    }

    if (GetCPInfo(-1, &cpinfo))
    {
        Fail("GetCPInfo() did not error on invalid code page!\n");
    }
    
    if (GetLastError() != ERROR_INVALID_PARAMETER)
    {
        Fail("GetCPInfo() failed to set the last error to"
             " ERROR_INVALID_PARAMETER!\n");
    }
    

    PAL_Terminate();

    return PASS;
}

