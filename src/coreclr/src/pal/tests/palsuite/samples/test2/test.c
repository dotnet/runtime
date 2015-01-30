//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:   test.c
**
** Purpose:  A sample to show how to structure a test case.
**
**
**===================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{
    int exampleInt = 9;
    
    /* Initialize the PAL.
     */
    if(0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    Trace("\nTest #2...\n");

#ifdef WIN32
    Trace("\nWe are testing under Win32 environment.\n");
#else
    Trace("\nWe are testing under Non-Win32 environment.\n");
#endif

    if (exampleInt == 9)
    {
        Fail("This is an example to how to code a failure. "
             "This failure was caused by exampleInt equalling %d\n",
             exampleInt);
    }

    /* Shutdown the PAL.
     */
    PAL_Terminate();

    return PASS;
}
