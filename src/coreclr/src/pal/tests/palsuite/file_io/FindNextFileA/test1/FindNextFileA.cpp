// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  FindNextFileA.c
**
** Purpose: Tests the PAL implementation of the FindNextFileA function.
**
**
**===================================================================*/

#include <palsuite.h>
                                                                   

const char* szFindName =            "test01.txt";
const char* szFindName_02 =         "test02.txt";
const char* szFindNameWldCard_01 =  "test0?.txt";
const char* szFindNameWldCard_02 =  "*.txt";
const char* szDirName =             "test_dir";
const char* szDirName_02 =          "test_dir_02";
const char* szDirNameWldCard =      "test_*";



void removeAll()
{
    WCHAR* wTempPtr = NULL;

    wTempPtr = convert((LPSTR)szDirName);
    RemoveDirectoryW(wTempPtr);
    free (wTempPtr);
    wTempPtr = convert((LPSTR)szDirName_02);
    RemoveDirectoryW(wTempPtr);
    free (wTempPtr);
    DeleteFile(szFindName);
    DeleteFile(szFindName_02);
}



BOOL createTestFile(const char* szName)
{
    FILE *pFile = NULL;

    pFile = fopen(szName, "w");
    if (pFile == NULL)
    {
        Trace("FindNextFile: ERROR -> Unable to create file \"%s\".\n",
            szName);
        removeAll();
        return FALSE;
    }
    else
    {
        fprintf(pFile, "FindNextFile test file, \"%s\".\n", szFindName);
        fclose(pFile);
    }
    return TRUE;
}



int __cdecl main(int argc, char *argv[])
{
    WIN32_FIND_DATA findFileData;
    WIN32_FIND_DATA findFileData_02;
    HANDLE hFind = NULL;
    BOOL bRc = FALSE;
    DWORD dwBytesWritten;
    WCHAR* wTempPtr = NULL;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
    removeAll();


    //
    // find a file with a NULL pointer
    //
    hFind = FindFirstFileA(NULL, &findFileData);
    if (hFind != INVALID_HANDLE_VALUE)
    {
        Fail("FindNextFile: ERROR -> Found invalid NULL file");
    }

    bRc = FindNextFile(hFind, &findFileData);
    if (bRc == TRUE)
    {
        Fail("FindNextFile: ERROR -> Found a file based on an invalid handle");
    }


    //
    // find a file that exists
    //
    if(createTestFile(szFindName) == FALSE)
    {
        PAL_TerminateEx(FAIL);
        return FAIL;
    }
    if(createTestFile(szFindName_02) == FALSE)
    {
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    hFind = FindFirstFileA(szFindName, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE) 
    {
        removeAll();
        Fail("FindNextFile: ERROR -> Unable to find \"%s\"\n", szFindName);
    }
    else
    {
        bRc = FindNextFile(hFind, &findFileData);
        if (bRc != FALSE)
        {
            removeAll();
            Fail("FindNextFile: ERROR -> Found a file that doesn't exist.\n");
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
        removeAll();
        Fail("FindNextFile: ERROR -> Failed to create the directory \"%s\"\n", 
            szDirName);
    }
    wTempPtr = convert((LPSTR)szDirName_02);
    bRc = CreateDirectoryW(wTempPtr, NULL);
    free (wTempPtr);
    if (bRc == FALSE)
    {
        removeAll();
        Fail("FindNextFile: ERROR -> Failed to create the directory \"%s\"\n",
            szDirName_02);
    }

    hFind = FindFirstFileA(szDirName, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        removeAll();
        Fail("FindNextFile: ERROR. FindFirstFileA was unable to find \"%s\"\n",
            szDirName);
    }
    else
    {
        bRc = FindNextFile(hFind, &findFileData);
        if (bRc != FALSE)
        {
            removeAll();
            Fail("FindNextFile: ERROR -> Found a directory that doesn't exist.\n");
        }
    }


    //
    // find a file using wild cards
    //
    hFind = FindFirstFileA(szFindNameWldCard_01, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE) 
    {
        removeAll();
        Fail("FindNextFile: ERROR -> FindFirstFileA was unable to find \"%s\"\n", 
            szFindNameWldCard_01);
    }
    else
    {
        bRc = FindNextFile(hFind, &findFileData_02);
        if (bRc == FALSE)
        {
            removeAll();
            Fail("FindNextFile: ERROR -> Unable to find another file.\n");
        }
        else
        {
            // validate we found the correct file
            if (strcmp(findFileData_02.cFileName, findFileData.cFileName) == 0)
            {
                removeAll();
                Fail("FindNextFile: ERROR -> Found the same file \"%s\".\n", 
                    findFileData.cFileName);
            }
        }
    }


    //
    // find a directory using wild cards
    //
    hFind = FindFirstFileA(szDirNameWldCard, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE) 
    {
        removeAll();
        Fail("FindNextFile: ERROR -> Unable to find \"%s\"\n", 
            szDirNameWldCard);
    }
    else
    {
        bRc = FindNextFile(hFind, &findFileData_02);
        if (bRc == FALSE)
        {
            removeAll();
            Fail("FindNextFile: ERROR -> Unable to find another directory.\n");
        }
        else
        {
            // validate we found the correct directory
            if (strcmp(findFileData_02.cFileName, findFileData.cFileName) == 0)
            {
                removeAll();
                Fail("FindNextFile: ERROR -> Found the same directory \"%s\".\n", 
                    findFileData.cFileName);
            }
        }
    }

    //
    // attempt to write to the hFind handle (which should fail)
    //
    bRc = WriteFile(hFind, "this is a test", 10, &dwBytesWritten, NULL);
    removeAll();
    if (bRc == TRUE)
    {
        Fail("FindNextFile: ERROR -> Able to write to a FindNextFile handle.\n");
    }

    PAL_Terminate();  

    return PASS;
}
