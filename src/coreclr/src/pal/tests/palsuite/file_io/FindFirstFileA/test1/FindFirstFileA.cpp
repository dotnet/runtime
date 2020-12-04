// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  FindFirstFileA.c
**
** Purpose: Tests the PAL implementation of the FindFirstFileA function.
**
**
**===================================================================*/


#include <palsuite.h>


#define szNoFileName           "333asdf.x77t"
#define szFindName             "test01.txt"
#define szFindNameWldCard_01   "test0?.txt"
#define szFindNameWldCard_02   "*.txt"
#define szDirName              "test_dir"
#define szDirNameSlash         "test_dir\\"
#define szDirNameWldCard_01    "?est_dir"
#define szDirNameWldCard_02    "test_*"
/* Longer than MAX_LONGPATH characters */
char szLongFindName[MAX_LONGPATH+1];

BOOL CleanUp_FindFirstFileA_test1()
{
    DWORD dwAtt;
    BOOL result = TRUE;

    dwAtt = GetFileAttributesA(szFindName);
    if( dwAtt != INVALID_FILE_ATTRIBUTES )
    {
        if(!SetFileAttributesA (szFindName, FILE_ATTRIBUTE_NORMAL))
        {
            result = FALSE;
            Trace("ERROR:%d: Error setting attributes [%s][%d]\n", szFindName, FILE_ATTRIBUTE_NORMAL);
        }
        if(!DeleteFileA (szFindName))
        {
            result = FALSE;
            Trace("ERROR:%d: Error deleting file [%s][%d]\n", GetLastError(), szFindName, dwAtt);
        }
    }

    dwAtt = GetFileAttributesA(szDirName);
    if( dwAtt != INVALID_FILE_ATTRIBUTES )
    {
        LPWSTR szDirNameW = convert(szDirName);
        if(!RemoveDirectoryW (szDirNameW))
        {
            result = FALSE;
            Trace("ERROR:%d: Error deleting file [%s][%d]\n", GetLastError(), szDirName, dwAtt);
        }
        free(szDirNameW);
    }

    return result;
}

PALTEST(file_io_FindFirstFileA_test1_paltest_findfirstfilea_test1, "file_io/FindFirstFileA/test1/paltest_findfirstfilea_test1")
{
    WIN32_FIND_DATA findFileData;
    HANDLE hFind = NULL;
    FILE *pFile = NULL;
    BOOL bRc = FALSE;
    WCHAR* szwTemp = NULL;

	memset(szLongFindName, 'a', MAX_LONGPATH+1);
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }


    if(!CleanUp_FindFirstFileA_test1())
    {
        Fail("FindFirstFileW: ERROR : Initial Clean Up failed\n");
    }

    //
    // find a file with a NULL pointer
    //
    hFind = FindFirstFileA(NULL, &findFileData);
    if (hFind != INVALID_HANDLE_VALUE)
    {
        Fail ("FindFirstFileA: ERROR -> Found invalid NULL file");
    }


    //
    // find a file that doesn't exist
    //
    hFind = FindFirstFileA(szNoFileName, &findFileData);
    if (hFind != INVALID_HANDLE_VALUE)
    {
        Fail ("FindFirstFileA: ERROR -> Found invalid NULL file");
    }


    //
    // find a file that exists
    //
    pFile = fopen(szFindName, "w");
    if (pFile == NULL)
    {
        Fail("FindFirstFileA: ERROR -> Unable to create a test file\n");
    }
    else
    {
        fclose(pFile);
    }
    hFind = FindFirstFileA(szFindName, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        Fail ("FindFirstFileA: ERROR -> Unable to find \"%s\"\n", szFindName);
    }
    else
    {
        // validate we found the correct file
        if (strcmp(szFindName, findFileData.cFileName) != 0)
        {
            Fail ("FindFirstFileA: ERROR -> Found the wrong file\n");
        }
    }


    //
    // find a directory that exists
    //
    szwTemp = convert((LPSTR)szDirName);
    bRc = CreateDirectoryW(szwTemp, NULL);
    free(szwTemp);
    if (bRc == FALSE)
    {
        Fail("FindFirstFileA: ERROR -> Failed to create the directory "
            "\"%s\"\n",
            szDirName);
    }

    hFind = FindFirstFileA(szDirName, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        Fail ("FindFirstFileA: ERROR. Unable to find \"%s\"\n", szDirName);
    }
    else
    {
        // validate we found the correct directory
        if (strcmp(szDirName, findFileData.cFileName) != 0)
        {
            Fail ("FindFirstFileA: ERROR -> Found the wrong directory\n");
        }
    }


    //
    // find a directory using a trailing '\' on the directory name: should fail
    //
    hFind = FindFirstFileA(szDirNameSlash, &findFileData);
    if (hFind != INVALID_HANDLE_VALUE)
    {
        Fail ("FindFirstFileA: ERROR -> Able to find \"%s\": trailing "
            "slash should have failed.\n",
            szDirNameSlash);
    }

    // find a file using wild cards
    hFind = FindFirstFileA(szFindNameWldCard_01, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        Fail ("FindFirstFileA: ERROR -> Unable to find \"%s\"\n",
            szFindNameWldCard_01);
    }

    hFind = FindFirstFileA(szFindNameWldCard_02, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        Fail ("FindFirstFileA: ERROR -> Unable to find \"%s\"\n", szFindNameWldCard_02);
    }


    //
    // find a directory using wild cards
    //
    hFind = FindFirstFileA(szDirNameWldCard_01, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        Fail ("FindFirstFileA: ERROR -> Unable to find \"%s\"\n", szDirNameWldCard_01);
    }

    hFind = FindFirstFileA(szDirNameWldCard_02, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        Fail ("FindFirstFileA: ERROR -> Unable to find \"%s\"\n", szDirNameWldCard_02);
    }

    if(!CleanUp_FindFirstFileA_test1())
    {
        Fail("FindFirstFileW: ERROR : Final Clean Up failed\n");
    }

    PAL_Terminate();

    return PASS;
}
