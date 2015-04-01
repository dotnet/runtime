//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source: test1.c
**
** Purpose: Tests that IsDBCSLeadByteEx does not find any lead-bytes in the
**          current ansi code page or the default code page.  Also tests that
**          it correctly handles an invalid codepage.
**
**
**==========================================================================*/


#include <palsuite.h>

void DoTest(int codepage)
{
    int value;
    int ret;
    int i;


    for (i=0; i<256; i++)
    {
        value = IsDBCSLeadByteEx(codepage, i);

        ret = GetLastError();
        if (ret == ERROR_INVALID_PARAMETER)
        {
            Fail("IsDBCSLeadByteEx unexpectedly errored with ERROR_INVALID_PARAMETER!\n");
        }
        else if (ret != 0)
        {
            Fail("IsDBCSLeadByteEx had an unexpected error!\n");
        }
        else if (value)
        {
            Fail("IsDBCSLeadByteEx incorrectly found a lead byte in code "
                "page %d\n", codepage);
        }

    }
}

int __cdecl main(int argc, char *argv[])
{

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    if (IsDBCSLeadByteEx(-1, 0))
    {
        Fail("IsDBCSLeadByteEx did not error on an invalid code page!\n");
    }

    /* Clear the last error. */
    SetLastError(0);


    DoTest(0);
    DoTest(CP_ACP);

    PAL_Terminate();

    return PASS;
}

