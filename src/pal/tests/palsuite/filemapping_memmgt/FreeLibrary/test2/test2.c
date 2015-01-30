//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

int __cdecl main(int argc, char* argv[])
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
