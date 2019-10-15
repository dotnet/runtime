// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  SetCurrentDirectoryW.c (test 2)
**
** Purpose: Tests the PAL implementation of the SetCurrentDirectoryW function
**          by setting the current directory with ../
**
**
**===================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    const char szDirName[MAX_PATH] = "testing";
    char szBuiltDir[MAX_PATH];
    WCHAR* szwBuiltDir = NULL;
    WCHAR szwHomeDirBefore[MAX_PATH];
    WCHAR szwHomeDirAfter[MAX_PATH];
    WCHAR* szwPtr = NULL;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* create a temp directory off the current directory */
    szwPtr = convert((LPSTR)szDirName);
 
    if (CreateDirectoryW(szwPtr, NULL) != TRUE)
    {
        free(szwPtr);
        Fail("Unexpected error: CreateDirectoryW failed "
             "with error code: %ld.\n", 
             GetLastError());
    }

    /* find out what the current "home" directory is */
    memset(szwHomeDirBefore, 0, MAX_PATH * sizeof(WCHAR));

    if( 0 == GetCurrentDirectoryW(MAX_PATH, szwHomeDirBefore) )
    {
        Trace("Unexpected error: Unable to get current directory "
              "with GetCurrentDirectoryW that returned %ld\n",
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
    memset(szBuiltDir, 0, MAX_PATH);
#if WIN32
    sprintf_s(szBuiltDir, _countof(szBuiltDir),"%s\\..\\", szDirName);
#else
    sprintf_s(szBuiltDir, _countof(szBuiltDir),"%s/../", szDirName);
#endif

    szwBuiltDir = convert(szBuiltDir);

    /* set the current directory to the temp directory */
    if (SetCurrentDirectoryW(szwBuiltDir) != TRUE)
    {
        Trace("ERROR: Unable to set current "
              "directory to %S. Failed with error code: %ld.\n", 
              szwBuiltDir,
              GetLastError());

        if (!RemoveDirectoryW(szwPtr))
        {
            Trace("Unexpected error: RemoveDirectoryW failed "
            "with error code: %ld.\n", 
            GetLastError());
        }
        free(szwPtr);
        free(szwBuiltDir);
        Fail("");
    }

    free(szwBuiltDir);

    /* find out what the current "home" directory is */
    memset(szwHomeDirAfter, 0, MAX_PATH * sizeof(WCHAR));

    if( 0 == GetCurrentDirectoryW(MAX_PATH, szwHomeDirAfter) )
    {
        Trace("Unexpected error: Unable to get current directory "
              "with GetCurrentDirectoryW that returned %ld\n",
              GetLastError());

        if (!RemoveDirectoryW(szwPtr))
        {
            Trace("ERROR: RemoveDirectoryW failed "
            "with error code: %ld.\n", 
            GetLastError());
        }
        free(szwPtr);

        Fail("");
    }

    /*compare the new current dir to the compiled current dir */
    if (wcsncmp(szwHomeDirBefore, szwHomeDirAfter, wcslen(szwHomeDirBefore)) != 0)
    {
        Trace("ERROR:The set directory \"%S\" does not "
              "compare to the built directory \"%S\".\n",
              szwHomeDirAfter,
              szwHomeDirBefore);

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


