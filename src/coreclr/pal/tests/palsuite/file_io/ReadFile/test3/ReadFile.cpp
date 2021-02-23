// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  ReadFile.c (test 3)
**
** Purpose: Tests the PAL implementation of the ReadFile function.
**          Creates a test file and performs an array of sequential read
**          tests.
**
** Assumes successful:
**          CreateFile
**          CloseHandle
**          memset
**          WriteFile
**          CreateFile
**          CloseHandle
**          GetLastError
**
**
**===================================================================*/

#include <palsuite.h>

#define szStringTest "The quick fox jumped over the lazy dog's back.\0"
#define szEmptyString ""
#define szReadableFile "Readable.txt"
#define szResultsFile "Results.txt"


BOOL validateResults_ReadFile_test3(const char* szString,  // string read
                     DWORD dwByteCount,     // amount requested
                     DWORD dwBytesRead)  // amount read
{
    // were the correct number of bytes read?
    if (dwBytesRead > dwByteCount)
    {
        Trace("bytes read > bytes asked for\n");
        return FALSE;
    }
    if (dwBytesRead != strlen(szString))
    {
        Trace("bytes read != length of read string\n");
        return FALSE;
    }


    //
    // compare results
    //

    if (memcmp(szString, szStringTest, dwByteCount) != 0)
    {
        Trace("read = %s  string = %s", szString, szStringTest);
        return FALSE;
    }

    return TRUE;
}




BOOL readTest_ReadFile_test3(DWORD dwByteCount, char cResult)
{
    HANDLE hFile = NULL;
    DWORD dwBytesRead = 0;
    DWORD dwTotal = 0;
    DWORD dwRequested = 0;
    BOOL bRc = FALSE;
    char szString[100];
    char* szPtr = szString;
    int i = 0;

    // open the test file 
    hFile = CreateFile(szReadableFile, 
        GENERIC_READ,
        FILE_SHARE_READ,
        NULL,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        NULL);
    if(hFile == INVALID_HANDLE_VALUE)
    {
        Trace("ReadFile: ERROR -> Unable to open file \"%s\".\n", 
            szReadableFile);
        return FALSE;
    }

    memset(szString, 0, 100);

    for (i = 0; i < 5; i++)
    {
        bRc = ReadFile(hFile, szPtr, dwByteCount, &dwBytesRead, NULL);
        szPtr += dwByteCount;
        dwTotal += dwBytesRead;
        dwRequested += dwByteCount;
    }

    if (bRc == FALSE)
    {
        // if it failed, was it supposed to fail?
        if (cResult == '1')
        {
            Trace("\nbRc = %d\n", bRc);
            Trace("szString = [%s]  dwByteCount = %d  dwBytesRead = %d\n", 
                szString, 
                dwByteCount, 
                dwBytesRead);
            Trace ("cresult = 1\n");
            Trace ("getlasterror = %d\n", GetLastError()); 
            CloseHandle(hFile);
            return FALSE;
        }
    }
    else
    {
        CloseHandle(hFile);
        // if it passed, was it supposed to pass?
        if (cResult == '0')
        {
            Trace ("cresult = 0\n");
            return FALSE;
        }
        else
        {
            return (validateResults_ReadFile_test3(szString, dwRequested, dwTotal));
        }
    }

    CloseHandle(hFile);
    return TRUE;
}



PALTEST(file_io_ReadFile_test3_paltest_readfile_test3, "file_io/ReadFile/test3/paltest_readfile_test3")
{
    HANDLE hFile = NULL;
    DWORD dwByteCount[4] = {0, 1, 2, 3};
    char szResults[4] = {'1', '1', '1', '1'};
    int i;
    BOOL bRc = FALSE;
    DWORD dwBytesWritten = 0;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }


    // create the test file 
    hFile = CreateFile(szReadableFile, 
        GENERIC_WRITE,
        FILE_SHARE_WRITE,
        NULL,
        CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);
    if(hFile == INVALID_HANDLE_VALUE)
    {
        Fail("ReadFile: ERROR -> Unable to create file \"%s\".\n", 
            szReadableFile);
    }

    bRc = WriteFile(hFile, szStringTest, strlen(szStringTest), 
        &dwBytesWritten, 
        NULL);
    CloseHandle(hFile);

    for (i = 0; i < 4; i++)
    {
        bRc = readTest_ReadFile_test3(dwByteCount[i], szResults[i]);
        if (bRc != TRUE)
        {
            Fail("ReadFile: ERROR -> Failed on test[%d]\n", i);
        }
    }

    PAL_Terminate();
    return PASS;
}
