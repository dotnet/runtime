// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  SetCurrentDirectoryA.c (test 2)
**
** Purpose: Tests the PAL implementation of the SetCurrentDirectoryA function
**          by setting the current directory with ../
**
**
**===================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    const char szDirName[MAX_PATH] = "testing";
    char szBuiltDir[_MAX_DIR+1];
    char szHomeDirBefore[_MAX_DIR+1];
    char szHomeDirAfter[_MAX_DIR+1];
    WCHAR* szwPtr = NULL;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* create a temp directory off the current directory */
    szwPtr = convert((LPSTR)szDirName);
 
    if (CreateDirectoryA(szDirName, NULL) != TRUE)
    {
        free(szwPtr);
        Fail("Unexpected error: CreateDirectoryW failed "
             "with error code: %ld.\n", 
             GetLastError());
    }

    /* find out what the current "home" directory is */
    memset(szHomeDirBefore, 0, (_MAX_DIR+1));

    if( 0 == GetCurrentDirectoryA((_MAX_DIR+1), szHomeDirBefore) )
    {
        Trace("Unexpected error: Unable to get current directory "
              "with GetCurrentDirectoryA that returned %ld\n",
              GetLastError());

        if (!RemoveDirectoryW(szwPtr))
        {
            Trace("Unexpected error: RemoveDirectoryW failed "
                  "with error code: %ld.\n", 
                  GetLastError());
        }
        free(szwPtr);

        Fail("");
    }

     /* append the temp name to the "home" directory */
    memset(szBuiltDir, 0, (_MAX_DIR+1));
#if WIN32
    sprintf_s(szBuiltDir, _countof(szBuiltDir),"%s\\..\\", szDirName);
#else
    sprintf_s(szBuiltDir, _countof(szBuiltDir),"%s/../", szDirName);
#endif


    /* set the current directory to the temp directory */
    if (SetCurrentDirectoryA(szBuiltDir) != TRUE)
    {
        Trace("ERROR: Unable to set current "
              "directory to %s. Failed with error code: %ld.\n", 
              szBuiltDir,
              GetLastError());

        if (!RemoveDirectoryW(szwPtr))
        {
            Trace("SetCurrentDirectoryA: ERROR -> RemoveDirectoryW failed "
            "with error code: %ld.\n", 
            GetLastError());
        }
        free(szwPtr);
        Fail("");
    }

    /* find out what the current "home" directory is */
    memset(szHomeDirAfter, 0, (_MAX_DIR+1));

    if( 0 == GetCurrentDirectoryA((_MAX_DIR+1), szHomeDirAfter) )
    {
        Trace("Unexpected error: Unable to get current directory "
              "with GetCurrentDirectoryA that returned %ld\n",
              GetLastError());

        if (!RemoveDirectoryW(szwPtr))
        {
            Trace("Unexpected error: RemoveDirectoryW failed "
                  "with error code: %ld.\n", 
                  GetLastError());
        }
        free(szwPtr);

        Fail("");
    }

    /*compare the new current dir to the compiled current dir */
    if (strncmp(szHomeDirBefore, szHomeDirAfter, strlen(szHomeDirBefore)) != 0)
    {
        Trace("ERROR: The set directory \"%s\" does not "
              "compare to the built directory \"%s\".\n",
              szHomeDirAfter,
              szHomeDirBefore);

        if (!RemoveDirectoryW(szwPtr))
        {
            Trace("Unexpected error: RemoveDirectoryW failed "
                  "with error code: %ld.\n", 
                  GetLastError());
        }
        free(szwPtr);
        Fail("");
    }

    /* clean up */
    if (!RemoveDirectoryW(szwPtr))
    {
        free(szwPtr);
        Fail("Unexpected error: RemoveDirectoryW failed "
             "with error code: %ld.\n", 
             GetLastError());
    }

    free(szwPtr);
    PAL_Terminate();

    return PASS;
}


