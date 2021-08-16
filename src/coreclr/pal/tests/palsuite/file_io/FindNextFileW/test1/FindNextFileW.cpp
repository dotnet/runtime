// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  FindNextFileW.c
**
** Purpose: Tests the PAL implementation of the FindNextFileW function.
**
**
**===================================================================*/

#include <palsuite.h>


#define szFindName             "test01.txt"
#define szFindName_02          "test02.txt"
#define szFindNameWldCard_01   "test0?.txt"
#define szFindNameWldCard_02   "*.txt"
#define szDirName              "test_dir"
#define szDirName_02           "test_dir_02"
#define szDirNameWldCard       "test_*"

void removeAll_FindNextFileW_test1()
{
    WCHAR* wTempPtr = NULL;

    wTempPtr = convert((LPSTR)szDirName);
    RemoveDirectoryW(wTempPtr);
    free(wTempPtr);

    wTempPtr = convert((LPSTR)szDirName_02);
    RemoveDirectoryW(wTempPtr);
    free(wTempPtr);

    wTempPtr = convert((LPSTR)szFindName);
    DeleteFileW(wTempPtr);
    free(wTempPtr);

    wTempPtr = convert((LPSTR)szFindName_02);
    DeleteFileW(wTempPtr);
    free(wTempPtr);
}



BOOL createTestFile_FindNextFileW_test1(const char* szName)
{
    FILE *pFile = NULL;

    pFile = fopen(szName, "w");
    if (pFile == NULL)
    {
        Trace("FindNextFileW: ERROR -> Unable to create file \"%s\".\n", szName);
        removeAll_FindNextFileW_test1();
        return FALSE;
    }
    else
    {
        fprintf(pFile, "FindNextFileW test file, \"%s\".\n", szFindName);
        fclose(pFile);
    }

    return TRUE;
}



PALTEST(file_io_FindNextFileW_test1_paltest_findnextfilew_test1, "file_io/FindNextFileW/test1/paltest_findnextfilew_test1")
{
    WIN32_FIND_DATAW findFileData;
    WIN32_FIND_DATAW findFileData_02;
    HANDLE hFind = NULL;
    BOOL bRc = FALSE;
    DWORD dwBytesWritten;
    WCHAR* wTempPtr = NULL;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
    removeAll_FindNextFileW_test1();


    //
    // find a file that exists
    //
    if(createTestFile_FindNextFileW_test1(szFindName) == FALSE)
    {
        PAL_TerminateEx(FAIL);  
        return FAIL;
    }
    if(createTestFile_FindNextFileW_test1(szFindName_02) == FALSE)
    {
        PAL_TerminateEx(FAIL);  
        return FAIL;
    }

    wTempPtr = convert((LPSTR)szFindName);
    hFind = FindFirstFileW(wTempPtr, &findFileData);
    free(wTempPtr);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        removeAll_FindNextFileW_test1();
        Fail("FindNextFileW: ERROR -> Unable to find \"%s\"\n", szFindName);
    }
    else
    {
        bRc = FindNextFileW(hFind, &findFileData);
        if (bRc != FALSE)
        {
            removeAll_FindNextFileW_test1();
            Fail("FindNextFileW: ERROR -> Found a file that doesn't exist.\n");
        }
    }


    //
    // find a directory that exists
    //
    wTempPtr = convert((LPSTR)szDirName);
    bRc = CreateDirectoryW(wTempPtr, NULL);
    free (wTempPtr);
    if (bRc == FALSE)
    {
        removeAll_FindNextFileW_test1();
        Fail("FindNextFileW: ERROR -> Failed to create the directory \"%s\"\n",
            szDirName);
    }
    wTempPtr = convert((LPSTR)szDirName_02);
    bRc = CreateDirectoryW(wTempPtr, NULL);
    free (wTempPtr);
    if (bRc == FALSE)
    {
        removeAll_FindNextFileW_test1();
        Fail("FindNextFileW: ERROR -> Failed to create the directory "
            "\"%s\"\n",
            szDirName_02);
    }

    wTempPtr = convert((LPSTR)szDirName);
    hFind = FindFirstFileW(wTempPtr, &findFileData);
    free (wTempPtr);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        removeAll_FindNextFileW_test1();
        Fail("FindNextFileW: ERROR. FindFirstFileW was unable "
            "to find \"%s\"\n",
            szDirName);
    }
    else
    {
        bRc = FindNextFileW(hFind, &findFileData);
        if (bRc != FALSE)
        {
            removeAll_FindNextFileW_test1();
            Fail("FindNextFileW: ERROR -> Found a directory that "
                "doesn't exist.\n");
        }
    }


    //
    // find a file using wild cards
    //
    wTempPtr = convert((LPSTR)szFindNameWldCard_01);
    hFind = FindFirstFileW(wTempPtr, &findFileData);
    free(wTempPtr);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        removeAll_FindNextFileW_test1();
        Fail("FindNextFileW: ERROR -> FindFirstFileW was unable to "
            "find \"%s\"\n",
            szFindNameWldCard_01);
    }
    else
    {
        bRc = FindNextFileW(hFind, &findFileData_02);
        if (bRc == FALSE)
        {
            removeAll_FindNextFileW_test1();
            Fail("FindNextFileW: ERROR -> Unable to find another file.\n");
        }
        else
        {
            // validate we found the correct file
            if (wcscmp(findFileData_02.cFileName, findFileData.cFileName) == 0)
            {
                removeAll_FindNextFileW_test1();
                Fail("FindNextFileW: ERROR -> Found the same file \"%S\".\n",
                    findFileData.cFileName);
            }
        }
    }


    //
    // find a directory using wild cards
    //
    wTempPtr = convert((LPSTR)szDirNameWldCard);
    hFind = FindFirstFileW(wTempPtr, &findFileData);
    free(wTempPtr);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        removeAll_FindNextFileW_test1();
        Fail("FindNextFileW: ERROR -> Unable to find \"%s\"\n",
            szDirNameWldCard);
    }
    else
    {
        bRc = FindNextFileW(hFind, &findFileData_02);
        if (bRc == FALSE)
        {
            removeAll_FindNextFileW_test1();
            Fail("FindNextFileW: ERROR -> Unable to find another directory.\n");
        }
        else
        {
            // validate we found the correct directory
            if (wcscmp(findFileData_02.cFileName, findFileData.cFileName) == 0)
            {
                removeAll_FindNextFileW_test1();
                Fail("FindNextFileW: ERROR -> Found the same directory "
                    "\"%S\".\n",
                    findFileData.cFileName);
            }
        }
    }

    //
    // attempt to write to the hFind handle (which should fail)
    //
    bRc = WriteFile(hFind, "this is a test", 10, &dwBytesWritten, NULL);
    if (bRc == TRUE)
    {
        removeAll_FindNextFileW_test1();
        Fail("FindNextFileW: ERROR -> Able to write to a FindNextFileW "
            "handle.\n");
    }

    removeAll_FindNextFileW_test1();
    PAL_Terminate();  

    return PASS;
}
