// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test4.c
**
** Purpose: Tests the PAL implementation of the CopyFileW function
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

PALTEST(file_io_CopyFileW_test3_paltest_copyfilew_test3, "file_io/CopyFileW/test3/paltest_copyfilew_test3")
{

#if WIN32
    return PASS;

#else

    BOOL bRc = TRUE;
    WCHAR szSrcExisting[] = {'/','e','t','c','/','p','a','s','s','w','d','\0'};
    WCHAR szDest[] = {'t','e','m','p','.','t','m','p','\0'};
    WCHAR szStringTest[] = {'M','a','r','r','y',' ','h','a','d',' ','a',' ',
        'l','i','t','t','l','e',' ','l','a','m','b','\0'};
    WCHAR szStringRead[30]; /* large enough for string szStringTest */

    HANDLE hFile = NULL;
    DWORD dwBytesWritten=0;
    DWORD dwBytesRead=0;
    int size=0;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* copy the file */
    bRc = CopyFileW(szSrcExisting,szDest,TRUE);
    if(!bRc)
    {
        Fail("CopyFileW: Cannot copy a file with error, %u",GetLastError());
    }

    /* try to get file attributes of destination file */
    if (GetFileAttributesW(szDest) == -1)
    {
        Fail("CopyFileW: GetFileAttributes of destination file "
            "failed with error code %u. \n",
            GetLastError());
    }

    /* set the attributes of the destination file to normal again */
    bRc = SetFileAttributesW(szDest, FILE_ATTRIBUTE_NORMAL);
    if(!bRc)
    {
        Fail("CopyFileW: ERROR-> Couldn't set file attributes for "
            "file %S with error %u\n", szDest, GetLastError());
    }

    /* open the file for write purposes */
    hFile = CreateFileW((WCHAR *)szDest,
        GENERIC_WRITE,
        0,
        NULL,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if(hFile == INVALID_HANDLE_VALUE)
    {
        Fail("CopyFileW: ERROR -> Unable to create file \"%S\".\n",
            szDest);
    }

    /* To account for the size of a WCHAR is twice that of a char */
    size = wcslen(szStringTest);
    size = size*sizeof(WCHAR);

    /* Attempt to write to the file */
    bRc = WriteFile(hFile,
            szStringTest,
            size,
            &dwBytesWritten,
            NULL);

    if (!bRc)
    {
        Trace("CopyFileW: ERROR -> Unable to write to copied file with error "
            "%u.\n", GetLastError());
        bRc = CloseHandle(hFile);
        if (!bRc)
        {
            Fail("CopyFileW: ERROR -> Unable to close file \"%S\" with "
                "error %u.\n",szDest, GetLastError());
        }
        Fail("");

    }

    /* Close the file handle */
    bRc = CloseHandle(hFile);
    if (!bRc)
    {
        Fail("CopyFileW: ERROR -> Unable to close file \"%S\" with error %u "
            ".\n",szDest,GetLastError());
    }


    /* open the file for read purposes */
    hFile = CreateFileW((WCHAR *)szDest,
        GENERIC_READ,
        0,
        NULL,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if(hFile == INVALID_HANDLE_VALUE)
    {
        Fail("CopyFileW: ERROR -> Unable to create file \"%S\".\n",
            szDest);
    }

    /* Attempt to read from the file */
    bRc = ReadFile(hFile,
        szStringRead,
        size,
        &dwBytesRead,
        NULL);

    if (!bRc)
    {
        Trace("CopyFileW: ERROR -> Unable to read from copied file with "
            "error %u.\n",GetLastError());
        bRc = CloseHandle(hFile);
        if (!bRc)
        {
            Fail("CopyFileW: ERROR -> Unable to close file \"%S\" with "
                "error %u.\n",szDest, GetLastError());
        }
        Fail("");

    }

    if(wcsncmp(szStringTest,szStringRead, wcslen(szStringTest)) != 0)
    {
        Trace("CopyFileW: ERROR -> The string which was written '%S' does not "
            "match the string '%S' which was read from the copied file.\n",
            szStringTest,szStringRead);
        bRc = CloseHandle(hFile);
        if (!bRc)
        {
            Fail("CopyFileW: ERROR -> Unable to close file \"%S\" with "
                "error %u.\n",szDest, GetLastError());
        }
        Fail("");
    }

    /* Close the file handle */
    bRc = CloseHandle(hFile);
    if (!bRc)
    {
        Fail("CopyFileW: ERROR -> Unable to close file \"%S\" with error %u "
            ".\n",szDest,GetLastError());
    }

    /* Remove the temporary file */
    bRc = DeleteFileW(szDest);
    if(!bRc)
    {
        Fail("CopyFileW: Could not remove copied file with error %u.\n",
            GetLastError());
    }

    PAL_Terminate();
    return PASS;

#endif

}
