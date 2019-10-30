// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  SetCurrentDirectoryW.c (test 1)
**
** Purpose: Tests the PAL implementation of the SetCurrentDirectoryW function.
**
**
**===================================================================*/

#include <palsuite.h>

const char* szFileName = "blah";
const char* szDirName = "testing";


// In order to avoid the "chicken and egg" scenario, this is another
// method of getting the current directory. GetFullPathNameW is called with
// a dummy file name and then the file name is stripped off leaving the
// current working directory
BOOL GetCurrentDir(WCHAR* szwCurrentDir)
{
    DWORD dwRc = 0;
    WCHAR szwReturnedPath[_MAX_DIR+1];
    LPWSTR pPathPtr;
    WCHAR* szwFileName = NULL;
    WCHAR* szwDirName = NULL;
    int nCount = 0;

    // use GetFullPathName to to get the current path by stripping
    // the file name off the end
    memset(szwReturnedPath, 0, sizeof(WCHAR)*(_MAX_DIR+1));
    szwFileName = convert((char*)szFileName);
    dwRc = GetFullPathNameW(szwFileName,
        _MAX_DIR,
        szwReturnedPath,
        &pPathPtr);

    if (dwRc == 0)
    {
        // GetFullPathName failed
        Trace("SetCurrentDirectoryW: ERROR -> GetFullPathNameW failed "
            "with error code: %ld.\n", GetLastError());
        RemoveDirectoryW(szwDirName);
        free(szwFileName);
        return(FALSE);
    }

    // now strip the file name from the full path to get the current path
    nCount = wcslen(szwReturnedPath) - wcslen(szwFileName);
    memset(szwCurrentDir, 0, sizeof(WCHAR)*(_MAX_DIR+1));
    wcsncpy(szwCurrentDir, szwReturnedPath, nCount - 1);

    free(szwFileName);
    return(TRUE);
}

int __cdecl main(int argc, char *argv[])
{
    WCHAR* szwDirName = NULL;
    WCHAR szwNewDir[_MAX_DIR+1];
    WCHAR szwBuiltDir[_MAX_DIR+1];
    WCHAR szwHomeDir[_MAX_DIR+1];
#if WIN32    
    WCHAR szwSlash[] = {'\\','\0'};
#else
    WCHAR szwSlash[] = {'/','\0'};
#endif

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    // remove the directory just in case a previous run of the test didn't
    szwDirName = convert((char*)szDirName);
    RemoveDirectoryW(szwDirName);

    // create a temp directory off the current directory
    if (CreateDirectoryW(szwDirName, NULL) != TRUE)
    {
        Trace("SetCurrentDirectoryW: ERROR -> CreateDirectoryW failed "
            "with error code: %ld.\n", GetLastError());
        RemoveDirectoryW(szwDirName);
        free(szwDirName);
        Fail("");
    }

    // find out what the current "home" directory is
    memset(szwHomeDir, 0, sizeof(WCHAR)*(_MAX_DIR+1));
    if(GetCurrentDir(szwHomeDir) != TRUE)
    {
        RemoveDirectoryW(szwDirName);
        free(szwDirName);
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    // set the current directory to the temp directory
    if (SetCurrentDirectoryW(szwDirName) != TRUE)
    {
        Trace("SetCurrentDirectoryW: ERROR -> Unable to set current "
            "directory. Failed with error code: %ld.\n", GetLastError());
        RemoveDirectoryW(szwDirName);
        free(szwDirName);
        Fail("");
    }

    // append the temp name to the "home" directory
    memset(szwBuiltDir, 0, sizeof(WCHAR)*(_MAX_DIR+1));
    wcscpy(szwBuiltDir, szwHomeDir);
    wcscat(szwBuiltDir, szwSlash);
    wcscat(szwBuiltDir, szwDirName);

    // get the new current directory
    memset(szwNewDir, 0, sizeof(WCHAR)*(_MAX_DIR+1));
    if(GetCurrentDir(szwNewDir) != TRUE)
    {
        RemoveDirectoryW(szwDirName);
        free(szwDirName);
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    // compare the new current dir to the compiled current dir
    if (wcsncmp(szwNewDir, szwBuiltDir, wcslen(szwNewDir)) != 0)
    {
        RemoveDirectoryW(szwDirName);
        free(szwDirName);
        Fail("SetCurrentDirectoryW: ERROR -> The set directory does not "
            "compare to the built directory.\n");
    }



    // set the current dir back to the original
    if (SetCurrentDirectoryW(szwHomeDir) != TRUE)
    {
        Trace("SetCurrentDirectoryW: ERROR -> Unable to set current "
            "directory. Failed with error code: %ld.\n", GetLastError());
        RemoveDirectoryW(szwDirName);
        free(szwDirName);
        Fail("");
    }


    // get the new current directory
    memset(szwNewDir, 0, sizeof(WCHAR)*(_MAX_DIR+1));
    if(GetCurrentDir(szwNewDir) != TRUE)
    {
        RemoveDirectoryW(szwDirName);
        free(szwDirName);
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    // ensure it compares to the "home" directory which is where 
    // we should be now
    if (wcsncmp(szwNewDir, szwHomeDir, wcslen(szwNewDir)) != 0)
    {
        RemoveDirectoryW(szwDirName);
        free(szwDirName);
        Fail("SetCurrentDirectoryW: ERROR -> The set directory does not "
            "compare to the built directory.\n");
    }


    RemoveDirectoryW(szwDirName);
    free(szwDirName);
    PAL_Terminate();

    return PASS;
}


