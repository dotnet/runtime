//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  SetCurrentDirectoryA.c (test 3)
**
** Purpose: Try calling SetCurrentDirectoryA with an invalid path,
**          with a valid filename and with NULL
**
**
**===================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    const char szDirName[MAX_PATH]  = "testing";
    const char szFileName[MAX_PATH] = "setcurrentdirectorya.c";

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* set the current directory to an unexistant folder */
    if (0 != SetCurrentDirectoryA(szDirName))
    {
        Fail("ERROR: SetCurrentDirectoryA should have failed "
             "when trying to set the current directory to "
             "an invalid folder\n");
    }

    /* set the current directory to an unexistant folder */
    if (0 != SetCurrentDirectoryA(szFileName))
    {
        Fail("ERROR: SetCurrentDirectoryA should have failed "
             "when trying to set the current directory to "
             "a valid file name\n");
    }
    
    /* set the current directory to NULL */
    if (0 != SetCurrentDirectoryA(NULL))
    {
        Fail("ERROR: SetCurrentDirectoryA should have failed "
             "when trying to set the current directory to "
             "NULL\n");
    }

    PAL_Terminate();

    return PASS;
}


