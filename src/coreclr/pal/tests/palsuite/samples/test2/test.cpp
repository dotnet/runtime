// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:   test.c
**
** Purpose:  A sample to show how to structure a test case.
**
**
**===================================================================*/

#include <palsuite.h>

PALTEST(samples_test2_paltest_samples_test2, "samples/test2/paltest_samples_test2")
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
