// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source: test1.c
**
** Purpose: Tests that IsDBCSLeadByte does not find any lead-bytes in the
**          current ansi code page 
**
**
** TODO: Test for positive, i.e., if it is potentially isdbcsleadbyte
**==========================================================================*/


#include <palsuite.h>

void DoTest()
{
    int value;
    int ret;
    int i;


    for (i=0; i<256; i++)
    {
        value = IsDBCSLeadByte(i);

        ret = GetLastError();
        if (ret == ERROR_INVALID_PARAMETER)
        {
            Fail("IsDBCSLeadByte unexpectedly errored with ERROR_INVALID_PARAMETER for %d!\n", i);
        }
        else if (ret != 0)
        {
            Fail("IsDBCSLeadByte had an unexpected error [%d] for %d!\n", ret, i);
        }
        else if (value)
        {
            Fail("IsDBCSLeadByte incorrectly found a lead byte in value [%d] for"
                " %d\n", value, i);
        }

    }
}

int __cdecl main(int argc, char *argv[])
{

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoTest();

    PAL_Terminate();

//    setlocale( "japan", );

    return PASS;
}

