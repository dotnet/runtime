// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test.c
**
** Purpose: This test is an example of the basic structure of a PAL test 
**          suite test case.
**
**
**==========================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{
    /* Initialize the PAL.
     */
    if(0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    Trace("\nTest #1...\n");

#ifdef WIN32
    Trace("\nWe are testing under Win32 environment.\n");
#else
    Trace("\nWe are testing under Non-Win32 environment.\n");
#endif

    Trace("\nThis test has passed.\n");

    /* Shutdown the PAL.
     */
    PAL_Terminate();

    return PASS;
}
