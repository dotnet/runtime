// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test4.c
**
** Purpose: Tests the PAL implementation of the CopyFileA function
**          to see if a file can through different users belonging to
**          different groups.
**

=====================================================================*/

/* USECASE
    Copy a file from a different user, belonging to a different group to
    the current user, who is a member of the current group.  Then check
    to see that the current user has the basic access rights to the copied
    file.

    Thie original file used is the passwd file in the etc directory of a
    BSD machine.  This file should exist on all machines.
*/

#include <palsuite.h>

PALTEST(file_io_CopyFileA_test4_paltest_copyfilea_test4, "file_io/CopyFileA/test4/paltest_copyfilea_test4")
{

#if WIN32
    return PASS;

#else

    BOOL bRc = TRUE;
    char* szSrcExisting = "/etc/passwd";
    char* szDest = "temp.tmp";
    char* szStringTest = "Marry had a little lamb";
    char szStringRead[30]; /* large enough for string szStringTest */

    HANDLE hFile = NULL;
    DWORD dwBytesWritten=0;
    DWORD dwBytesRead=0;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* copy the file */
    bRc = CopyFileA(szSrcExisting,szDest,TRUE);
    if(!bRc)
    {
        Fail("CopyFileA: Cannot copy a file with error, %u",GetLastError());
    }

    /* try to get file attributes of destination file */
    if (GetFileAttributesA(szDest) == -1)
    {
        Fail("CopyFileA: GetFileAttributes of destination file "
            "failed with error code %u. \n",
            GetLastError());
    }

    /* set the attributes of the destination file to normal again */
    bRc = SetFileAttributesA(szDest, FILE_ATTRIBUTE_NORMAL);
    if(!bRc)
    {
        Fail("CopyFileA: ERROR-> Couldn't set file attributes for "
            "file %s with error %u\n", szDest, GetLastError());
    }

    /* open the file for write purposes */
    hFile = CreateFile(szDest,
        GENERIC_WRITE,
        0,
        NULL,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if(hFile == INVALID_HANDLE_VALUE)
    {
        Fail("CopyFileA: ERROR -> Unable to create file \"%s\".\n",
            szDest);
    }

    /* Attempt to write to the file */
    bRc = WriteFile(hFile, szStringTest, strlen(szStringTest), &dwBytesWritten, NULL);
    if (!bRc)
    {
        Trace("CopyFileA: ERROR -> Unable to write to copied file with error "
            "%u.\n", GetLastError());
        bRc = CloseHandle(hFile);
        if (!bRc)
        {
            Fail("CopyFileA: ERROR -> Unable to close file \"%s\" with "
                "error %u.\n",szDest, GetLastError());
        }
        Fail("");

    }

    /* Close the file handle */
    bRc = CloseHandle(hFile);
    if (!bRc)
    {
        Fail("CopyFileA: ERROR -> Unable to close file \"%s\" with error %u "
            ".\n",szDest,GetLastError());
    }


    /* open the file for read purposes */
    hFile = CreateFile(szDest,
        GENERIC_READ,
        0,
        NULL,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if(hFile == INVALID_HANDLE_VALUE)
    {
        Fail("CopyFileA: ERROR -> Unable to create file \"%s\".\n",
            szDest);
    }

    /* Attempt to read from the file */
    bRc = ReadFile(hFile, szStringRead, strlen(szStringTest), &dwBytesRead, NULL);
    if (!bRc)
    {
        Trace("CopyFileA: ERROR -> Unable to read from copied file with "
            "error %u.\n",GetLastError());
        bRc = CloseHandle(hFile);
        if (!bRc)
        {
            Fail("CopyFileA: ERROR -> Unable to close file \"%s\" with "
                "error %u.\n",szDest, GetLastError());
        }
        Fail("");

    }

    if(strncmp(szStringTest,szStringRead, strlen(szStringTest)) != 0)
    {
        Trace("CopyFileA: ERROR -> The string which was written '%s' does not "
            "match the string '%s' which was read from the copied file.\n",
            szStringTest,szStringRead);
        bRc = CloseHandle(hFile);
        if (!bRc)
        {
            Fail("CopyFileA: ERROR -> Unable to close file \"%s\" with "
                "error %u.\n",szDest, GetLastError());
        }
        Fail("");
    }

    /* Close the file handle */
    bRc = CloseHandle(hFile);
    if (!bRc)
    {
        Fail("CopyFileA: ERROR -> Unable to close file \"%s\" with error %u "
            ".\n",szDest,GetLastError());
    }

    /* Remove the temporary file */
    int st = remove(szDest);
    if(st != 0)
    {
        Fail("CopyFileA: Could not remove copied file with error %u\n",
            errno);
    }

    PAL_Terminate();
    return PASS;

#endif

}
