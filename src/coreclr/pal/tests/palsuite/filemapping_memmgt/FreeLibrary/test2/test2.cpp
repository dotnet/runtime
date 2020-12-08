// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
** 
** Source:  test2.c (FreeLibrary)
**
** Purpose: Tests the PAL implementation of the FreeLibrary function.
**          This is a negative test that will pass an invalid and a
**          null handle to FreeLibrary.
**
**
**===================================================================*/

#include <palsuite.h>

PALTEST(filemapping_memmgt_FreeLibrary_test2_paltest_freelibrary_test2, "filemapping_memmgt/FreeLibrary/test2/paltest_freelibrary_test2")
{
    HANDLE hLib;

    /* Initialize the PAL. 
     */
    if ((PAL_Initialize(argc, argv)) != 0)
    {
        return (FAIL);
    }

    /* Attempt to pass FreeLibrary an invalid handle. 
     */
    hLib = INVALID_HANDLE_VALUE;
    if (FreeLibrary(hLib))
    {
        Fail("ERROR: Able to free library handle = \"0x%lx\".\n",
              hLib);
    }
    
    /* Attempt to pass FreeLibrary a NULL handle. 
     */
    hLib = NULL;
    if (FreeLibrary(hLib))
    {
        Fail("ERROR: Able to free library handle = \"NULL\".\n");
    }


    /* Terminate the PAL.
     */
    PAL_Terminate();
    return PASS;

}
