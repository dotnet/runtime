// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  GetLongPathNameW.c Win32 version(test 1)
**
** Purpose: Tests the PAL implementation of the GetLongPathNameW function.
**          as expected under Win32
**
** Depends on:
**      CreateDirectoryA
**      RemoveDirectoryW
**
**
**===================================================================*/
/*
tests:
    - test invalid path names
    - test an already short path name
    - test a long path name
    - test with buffer size too small
*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
/* Since GetLongPathNameW operates differently under FreeBSD and Win32 this test
   is for Win32 only. It runs the same tests as the FreeBSD version but checks for
   different results
*/

#if WIN32
    DWORD dwRc = 0;
    WCHAR szwReturnedPath[MAX_LONGPATH];
    WCHAR szwSmallBuff[3];
    const char szShortPathName[] = {"testing"};
    const char szLongPathName[] = {"This_is_a_long_directory_name"};
    const char szShortenedPathName[] = {"THIS_I~1"};
    WCHAR* wLongPathPtr = NULL;
    WCHAR* wShortPathPtr = NULL;



    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    memset(szwReturnedPath, 0, MAX_LONGPATH*sizeof(WCHAR));
    memset(szwSmallBuff, 0, 3*sizeof(WCHAR));
    wLongPathPtr = convert((char*)szLongPathName);
    wShortPathPtr = convert((char*)szShortenedPathName);
    
    /* do some clean up just to be safe */
    RemoveDirectoryW(wLongPathPtr);
    RemoveDirectoryW(wShortPathPtr);


    /* attempt call on an invalid short path name */
    dwRc = GetLongPathNameW(wShortPathPtr, szwReturnedPath, MAX_LONGPATH);
    if (dwRc != 0)
    {
        Trace("GetLongPathNameW: ERROR -> Call made with an invalid short "
            "path \"%S\" returned \"%S\"\n",
            wShortPathPtr,
            szwReturnedPath);
        free (wLongPathPtr);
        free (wShortPathPtr);
        Fail("");
    }


    /* attempt call on an invalid long path name */
    dwRc = GetLongPathNameW(wLongPathPtr, szwReturnedPath, MAX_LONGPATH);
    if (dwRc != 0)
    {
        Trace("GetLongPathNameW: ERROR -> Call made with an invalid long "
            "path \"%S\" returned \"%S\"\n",
            wLongPathPtr,
            szwReturnedPath);
        free (wLongPathPtr);
        free (wShortPathPtr);
        Fail("");
    }


    /* create a long directory name */
    if (TRUE != CreateDirectoryW(wLongPathPtr, NULL))
    {
        free(wLongPathPtr);
        free(wShortPathPtr);
        Fail("GetLongPathNameW: ERROR -> CreateDirectoryW failed with an error"
            " code of %ld when asked to create a directory called \"%s\".\n",
            GetLastError(),
            szLongPathName);
    }


    /* get the long path name */
    memset(szwReturnedPath, 0, MAX_LONGPATH*sizeof(WCHAR));
    dwRc = GetLongPathNameW(wShortPathPtr, szwReturnedPath, MAX_LONGPATH);
    if (dwRc == 0)
    {
        Trace("GetLongPathNameW: ERROR -> failed with an error"
            " code of %ld when asked for the long version of \"%s\".\n",
            GetLastError(),
            szShortenedPathName);
        if (RemoveDirectoryW(wLongPathPtr) != TRUE)
        {
            Trace("GetLongPathNameW: ERROR -> RemoveDirectoryW failed to "
                " remove the directory \"%S\" with an error code of %ld.\n",
                wLongPathPtr,
                GetLastError());
        }
        free(wLongPathPtr);
        free(wShortPathPtr);
        Fail("");
    }

    /* does the returned match the expected */
    if (wcsncmp(wLongPathPtr, szwReturnedPath, wcslen(wLongPathPtr)) ||
        (wcslen(wLongPathPtr) != wcslen(szwReturnedPath)))
    {
        if (RemoveDirectoryW(wLongPathPtr) != TRUE)
        {
            Trace("GetLongPathNameW: ERROR -> RemoveDirectoryW failed to "
                " remove the directory \"%S\" with an error code of %ld.\n",
                wLongPathPtr,
                GetLastError());
        }
        free(wLongPathPtr);
        free(wShortPathPtr);
        Fail("GetLongPathNameW: ERROR -> The returned path, \"%S\" doesn't "
            "match the expected return, \"%s\".\n",
            szwReturnedPath,
            szLongPathName);
    }

    /* does the length returned match the actual length */
    if (dwRc != wcslen(szwReturnedPath))
    {
        if (RemoveDirectoryW(wLongPathPtr) != TRUE)
        {
            Trace("GetLongPathNameW: ERROR -> RemoveDirectoryW failed to "
                " remove the directory \"%S\" with an error code of %ld.\n",
                wLongPathPtr,
                GetLastError());
        }
        free(wLongPathPtr);
        free(wShortPathPtr);
        Fail("GetLongPathNameW: ERROR -> The returned length, %ld, doesn't "
            "match the string length, %ld.\n",
            dwRc,
            wcslen(szwReturnedPath));
    }

    if (RemoveDirectoryW(wLongPathPtr) != TRUE)
    {
        Trace("GetLongPathNameW: ERROR -> RemoveDirectoryW failed to "
            " remove the directory \"%S\" with an error code of %ld.\n",
            wLongPathPtr,
            GetLastError());
        free(wShortPathPtr);
        free(wLongPathPtr);
        Fail("");
    }
    free(wShortPathPtr);
    free(wLongPathPtr);


    /* test an actual short name */
    /* create a long directory name */
    wShortPathPtr = convert((char*)szShortPathName);
    RemoveDirectoryW(wShortPathPtr);

    if (TRUE != CreateDirectoryW(wShortPathPtr, NULL))
    {
        Trace("GetLongPathNameW: ERROR -> CreateDirectoryW failed with an error"
            " code of %ld when asked to create a directory called \"%s\".\n",
            GetLastError(),
            szShortPathName);
        free(wShortPathPtr);
        Fail("");
    }


    /* get the long path name */
    memset(szwReturnedPath, 0, MAX_LONGPATH*sizeof(WCHAR));
    dwRc = GetLongPathNameW(wShortPathPtr, szwReturnedPath, MAX_LONGPATH);
    if (dwRc == 0)
    {
        Trace("GetLongPathNameW: ERROR -> failed with an error"
            " code of %ld when asked for the long version of \"%s\".\n",
            GetLastError(),
            szShortenedPathName);
        if (RemoveDirectoryW(wShortPathPtr) != TRUE)
        {
            Trace("GetLongPathNameW: ERROR -> RemoveDirectoryW failed to "
                " remove the directory \"%S\" with an error code of %ld.\n",
                wShortPathPtr,
                GetLastError());
        }
        free(wShortPathPtr);
        Fail("");
    }

    /* does the returned match the expected */
    if (wcsncmp(wShortPathPtr, szwReturnedPath, wcslen(wShortPathPtr)) ||
        (wcslen(wShortPathPtr) != wcslen(szwReturnedPath)))
    {
        if (RemoveDirectoryW(wShortPathPtr) != TRUE)
        {
            Trace("GetLongPathNameW: ERROR -> RemoveDirectoryW failed to "
                " remove the directory \"%S\" with an error code of %ld.\n",
                wShortPathPtr,
                GetLastError());
        }
        free(wShortPathPtr);
        Fail("GetLongPathNameW: ERROR -> The returned path, \"%S\" doesn't "
            "match the expected return, \"%s\".\n",
            szwReturnedPath,
            szShortPathName);
    }

    /* does the length returned match the actual length */
    if (dwRc != wcslen(szwReturnedPath))
    {
        if (RemoveDirectoryW(wShortPathPtr) != TRUE)
        {
            Trace("GetLongPathNameW: ERROR -> RemoveDirectoryW failed to "
                " remove the directory \"%S\" with an error code of %ld.\n",
                wShortPathPtr,
                GetLastError());
        }
        free(wShortPathPtr);
        Fail("GetLongPathNameW: ERROR -> The returned length, %ld, doesn't "
            "match the string length, %ld.\n",
            dwRc,
            wcslen(szwReturnedPath));
    }

    /* test using a too small return buffer */
    dwRc = GetLongPathNameW(wShortPathPtr, szwSmallBuff, 3);
    if ((dwRc != (strlen(szShortPathName)+1)) || /* +1 for the required NULL */
        szwSmallBuff[0] != '\0')
    {
        if (RemoveDirectoryW(wShortPathPtr) != TRUE)
        {
            Trace("GetLongPathNameW: ERROR -> RemoveDirectoryW failed to "
                " remove the directory \"%S\" with an error code of %ld.\n",
                wShortPathPtr,
                GetLastError());
        }
        free(wShortPathPtr);
        Fail("GetLongPathNameW: ERROR -> using a return buffer that was too"
            " small was not handled properly.\n");
    }

    if (RemoveDirectoryW(wShortPathPtr) != TRUE)
    {
        Trace("GetLongPathNameW: ERROR -> RemoveDirectoryW failed to "
            " remove the directory \"%S\" with an error code of %ld.\n",
            wShortPathPtr,
            GetLastError());
        free(wShortPathPtr);
        Fail("");
    }
    free(wShortPathPtr);
    
    PAL_Terminate();

#endif /* WIN32 */

    return PASS;
}

