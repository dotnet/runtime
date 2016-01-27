// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  SetCurrentDirectoryA.c (test 1)
**
** Purpose: Tests the PAL implementation of the SetCurrentDirectoryA function.
**
**
**===================================================================*/

#include <palsuite.h>



/* In order to avoid the "chicken and egg" scenario, this is another
 method of getting the current directory. GetFullPathNameA is called with
 a dummy file name and then the file name is stripped off leaving the
 current working directory
*/

BOOL GetCurrentDir(char* szCurrentDir)
{
    const char* szFileName = "blah";
    DWORD dwRc = 0;
    char szReturnedPath[_MAX_DIR+1];
    LPSTR pPathPtr;
    size_t nCount = 0;

    /* use GetFullPathNameA to to get the current path by stripping
       the file name off the end */
    memset(szReturnedPath, 0, (_MAX_DIR+1));
    dwRc = GetFullPathNameA(szFileName,
        _MAX_DIR,
        szReturnedPath,
        &pPathPtr);

    if (dwRc == 0)
    {
        /* GetFullPathNameA failed */
        Trace("SetCurrentDirectoryA: ERROR -> GetFullPathNameA failed "
            "with error code: %ld.\n", GetLastError());
        return(FALSE);
    }

    /* now strip the file name from the full path to get the current path */
    nCount = strlen(szReturnedPath) - strlen(szFileName);
    memset(szCurrentDir, 0, (_MAX_DIR+1));
    strncpy(szCurrentDir, szReturnedPath, nCount);

    return(TRUE);
}



int __cdecl main(int argc, char *argv[])
{
    const char* szDirName = "testing";
    /* directory name longer than MAX_PATH characters */
    char szLongDirName[MAX_LONGPATH+1];
    char szNewDir[_MAX_DIR+1];
    char szBuiltDir[_MAX_DIR+1];
    char szHomeDir[_MAX_DIR+1];
    WCHAR* szwPtr = NULL;

	memset(szLongDirName, 'a', MAX_LONGPATH+1);
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* remove the directory just in case a previous run of the test didn't */
    szwPtr = convert((LPSTR)szDirName);

    /* clean up. Remove the directory
     * if it exists */
    RemoveDirectoryW(szwPtr);

    /* create a temp directory off the current directory */
    if (CreateDirectoryA(szDirName, NULL) != TRUE)
    {
        free(szwPtr);
        Fail("SetCurrentDirectoryA: ERROR -> CreateDirectoryW failed "
            "with error code: %ld.\n", GetLastError());
    }

    /* find out what the current "home" directory is */
    memset(szHomeDir, 0, (_MAX_DIR+1));
    if(GetCurrentDir(szHomeDir) != TRUE)
    {
        if (!RemoveDirectoryW(szwPtr))
        {
            Trace("SetCurrentDirectoryA: ERROR -> RemoveDirectoryW failed "
            "with error code: %ld.\n", GetLastError());
        }
        free(szwPtr);
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    /* set the current directory to the temp directory */

    if (SetCurrentDirectoryA(szDirName) != TRUE)
    {
        Trace("SetCurrentDirectoryA: ERROR -> Unable to set current "
            "directory. Failed with error code: %ld.\n", GetLastError());
        if (!RemoveDirectoryW(szwPtr))
        {
            Trace("SetCurrentDirectoryA: ERROR -> RemoveDirectoryW failed "
            "with error code: %ld.\n", GetLastError());
        }
        free(szwPtr);
        Fail("");
    }

    /* append the temp name to the "home" directory */
    memset(szBuiltDir, 0, (_MAX_DIR+1));
#if WIN32
    sprintf(szBuiltDir,"%s%s\\", szHomeDir, szDirName);
#else
    sprintf(szBuiltDir,"%s%s/", szHomeDir, szDirName);
#endif

    /* get the new current directory */
    memset(szNewDir, 0, (_MAX_DIR+1));
    if(GetCurrentDir(szNewDir) != TRUE)
    {
        if (!RemoveDirectoryW(szwPtr))
        {
            Trace("SetCurrentDirectoryA: ERROR -> RemoveDirectoryW failed "
            "with error code: %ld.\n", GetLastError());
        }
        free(szwPtr);
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    /*compare the new current dir to the compiled current dir */
    if (strncmp(szNewDir, szBuiltDir, strlen(szNewDir)) != 0)
    {
        if (!RemoveDirectoryW(szwPtr))
        {
            Trace("SetCurrentDirectoryA: ERROR -> RemoveDirectoryW failed "
            "with error code: %ld.\n", GetLastError());
        }
        free(szwPtr);
        Fail("SetCurrentDirectoryA: ERROR -> The set directory \"%s\" does not"
            " compare to the built directory \"%s\".\n",
            szNewDir,
            szBuiltDir);
    }



    /* set the current dir back to the original */
    if (SetCurrentDirectoryA(szHomeDir) != TRUE)
    {
        Trace("SetCurrentDirectoryA: ERROR -> Unable to set current "
            "directory. Failed with error code: %ld.\n", GetLastError());
        if (!RemoveDirectoryW(szwPtr))
        {
            Trace("SetCurrentDirectoryA: ERROR -> RemoveDirectoryW failed "
            "with error code: %ld.\n", GetLastError());
        }
        free(szwPtr);
        Fail("");
    }


    /* get the new current directory */
    memset(szNewDir, 0, sizeof(char)*(_MAX_DIR+1));
    if(GetCurrentDir(szNewDir) != TRUE)
    {
        if (!RemoveDirectoryW(szwPtr))
        {
            Trace("SetCurrentDirectoryA: ERROR -> RemoveDirectoryW failed "
            "with error code: %ld.\n", GetLastError());
        }
        free(szwPtr);
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    /* ensure it compares to the "home" directory which is where
     we should be now */
    if (strncmp(szNewDir, szHomeDir, strlen(szNewDir)) != 0)
    {
        if (!RemoveDirectoryW(szwPtr))
        {
            Trace("SetCurrentDirectoryA: ERROR -> RemoveDirectoryW failed "
            "with error code: %ld.\n", GetLastError());
        }
        free(szwPtr);
        Fail("SetCurrentDirectoryA: ERROR -> The set directory does not "
            "compare to the built directory.\n");
    }


    /* clean up */
    if (!RemoveDirectoryW(szwPtr))
    {
        free(szwPtr);
        Fail("SetCurrentDirectoryA: ERROR -> RemoveDirectoryW failed "
            "with error code: %ld.\n", GetLastError());
    }

    /* Test that SetCurrentDirectoryA fails with paths longer than
       MAX_PATH characters. Note that we cannot actually create this
       directory and try to SetCurrentDirectory into it. */
    if (SetCurrentDirectoryA(szLongDirName) != TRUE)
    {
        /* Failure is expected because the directory doesn't exist and
           it is longer than MAX_PATH characters. Check that the failure 
           is actually because of invalid argument. */
        if (GetLastError() != ERROR_FILENAME_EXCED_RANGE)
        {
           Fail("SetCurrentDirectoryA with a long path failed with some "
                "other error: %d\n", GetLastError());
        }
    }
    else
    {
        Fail("SetCurrentDirectoryA succeeded with a path longer than "
             "MAX_PATH characters\n");
    }

    free(szwPtr);
    PAL_Terminate();

    return PASS;
}


