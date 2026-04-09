// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  WriteFile.c (test 3)
**
** Purpose: Tests the PAL implementation of the WriteFile function.
**          Performs multiple writes to a file and verifies the results.
**
**
**===================================================================*/


#include <palsuite.h>


#define szStringTest "The quick fox jumped over the lazy dog's back.\0"
#define szWritableFile "writeable.txt"


BOOL validateResults_WriteFile_test3(const char* szString)
{
    FILE *pFile = NULL;
    char szReadString[100];
    DWORD dwBytesRead;
    DWORD dwStringLength = strlen(szString);



    memset(szReadString, 0, 100);

    /* open the file */
    pFile = fopen(szWritableFile, "r");
    if (pFile == NULL)
    {
        Trace("couldn't open test file\n");
        return FALSE;
    }

    dwBytesRead = fread(szReadString, sizeof(char), dwStringLength, pFile);
    fclose(pFile);

    if(dwBytesRead != dwStringLength)
    {
        Trace("dwbyteread != string length\n");
        return FALSE;
    }

    if (strcmp(szReadString, szString))
    {
        Trace("read = %s  string = %s", szReadString, szString);
        return FALSE;
    }
    return TRUE;
}




BOOL writeTest_WriteFile_test3(const char* szString)
{
    HANDLE hFile = NULL;
    DWORD dwBytesWritten;
    BOOL bRc = FALSE;
    BOOL bAllPassed = TRUE;
    int nStringLength = 0;
    char* szPtr = NULL;
    int i = 0;

    // create the test file 
    hFile = CreateFile(szWritableFile, 
        GENERIC_WRITE,
        FILE_SHARE_WRITE,
        NULL,
        CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if(hFile == INVALID_HANDLE_VALUE)
    {
        Trace("WriteFile: ERROR -> Unable to create file \"%s\".\n", 
            szWritableFile);
        return FALSE;
    }

    nStringLength = strlen(szString);
    szPtr = (char*) szString;

    for (i = 0; i < nStringLength; i++)
    {
        bRc = WriteFile(hFile, szPtr++, 1, &dwBytesWritten, NULL);
        if ((bRc == FALSE) || (dwBytesWritten != 1))
        {
            bAllPassed = FALSE;
        }
    }
    CloseHandle(hFile);

    if (bAllPassed == FALSE)
    {
        Trace ("WriteFile: ERROR: Failed to write data.\n"); 
        return FALSE;
    }
    else
    {
        return (validateResults_WriteFile_test3(szString));
    }

    return TRUE;
}




PALTEST(file_io_WriteFile_test3_paltest_writefile_test3, "file_io/WriteFile/test3/paltest_writefile_test3")
{
    const char *pString = szStringTest;
    BOOL bRc = FALSE;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }


    bRc = writeTest_WriteFile_test3(pString);
    if (bRc != TRUE)
    {
        Fail("WriteFile: ERROR -> Failed\n");
    }

    PAL_Terminate();
    return PASS;
}
