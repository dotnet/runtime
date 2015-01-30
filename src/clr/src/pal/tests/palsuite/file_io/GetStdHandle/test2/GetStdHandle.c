//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  GetStdHandle.c (test 2)
**
** Purpose: Smoke Tests the PAL implementation of the GetStdHandle function.
**
**
**===================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    HANDLE hFile = NULL;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /*
     * attempt to get an invalid handle
     */
    hFile = GetStdHandle(-2);
    if (hFile != INVALID_HANDLE_VALUE)
    {
        Fail("GetStdHandle: ERROR -> A request for the STD_INPUT_HANDLE "
            "returned an invalid handle.\n");
    }


    /*
     * test the STD_INPUT_HANDLE handle
     */
    hFile = GetStdHandle(STD_INPUT_HANDLE);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        Fail("GetStdHandle: ERROR -> A request for the STD_INPUT_HANDLE "
            "returned an invalid handle.\n");
    }


    /*
     * test the STD_OUTPUT_HANDLE handle
     */
    hFile = GetStdHandle(STD_OUTPUT_HANDLE);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        Fail("GetStdHandle: ERROR -> A request for the STD_OUTPUT_HANDLE "
            "returned an invalid handle.\n");
    }

    /* test the STD_ERROR_HANDLE handle */
    hFile = GetStdHandle(STD_ERROR_HANDLE);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        Fail("GetStdHandle: ERROR -> A request for the STD_ERROR_HANDLE "
            "returned an invalid handle.\n");
    }


    /* check to see if we can CloseHandle works on the STD_ERROR_HANDLE */
    if (!CloseHandle(hFile))
    {
        Fail("GetStdHandle: ERROR -> CloseHandle failed. GetLastError "
            "returned %u.\n",
            GetLastError());
    }


    PAL_Terminate();
    return PASS;
}

