// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  SetCurrentDirectoryW.c (test 3)
**
** Purpose: Try calling SetCurrentDirectoryW with an invalid path,
**          with a valid filename and with NULL
**
**
**===================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    char szDirName[MAX_PATH]  = "testing";
    WCHAR* szwDirName = NULL;
    char szFileName[MAX_PATH] = "setcurrentdirectorya.c";
    WCHAR* szwFileName = NULL;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* set the current directory to an unexistant folder */
    szwDirName = convert(szDirName);
    if (0 != SetCurrentDirectoryW(szwDirName))
    {
        free(szwDirName);
        Fail("ERROR: SetCurrentDirectoryW should have failed "
             "when trying to set the current directory to "
             "an invalid folder\n");
    }
    free(szwDirName);

    /* set the current directory to an unexistant folder */
    szwFileName = convert(szFileName);
    if (0 != SetCurrentDirectoryW(szwFileName))
    {
        free(szwFileName);
        Fail("ERROR: SetCurrentDirectoryW should have failed "
             "when trying to set the current directory to "
             "a valid file name\n");
    }
    free(szwFileName);
    
    /* set the current directory to NULL */
    if (0 != SetCurrentDirectoryW(NULL))
    {
        Fail("ERROR: SetCurrentDirectoryW should have failed "
             "when trying to set the current directory to "
             "NULL\n");
    }

    PAL_Terminate();

    return PASS;
}


