// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  FindFirstFileW.c
**
** Purpose: Tests the PAL implementation of the FindFirstFileW function.
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


BOOL CleanUp_FindFirstFileW_test1()
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

PALTEST(file_io_FindFirstFileW_test1_paltest_findfirstfilew_test1, "file_io/FindFirstFileW/test1/paltest_findfirstfilew_test1")
{
    WIN32_FIND_DATAW findFileData;
    HANDLE hFind = NULL;
    FILE *pFile = NULL;
    BOOL bRc = FALSE;
    WCHAR* pTemp = NULL;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    if(!CleanUp_FindFirstFileW_test1())
    {
        Fail("FindFirstFileW: ERROR : Initial Clean Up failed\n");
    }

    //
    // find a file that doesn't exist
    //
    pTemp = convert((LPSTR)szNoFileName);
    hFind = FindFirstFileW(pTemp, &findFileData);
    free(pTemp);
    if (hFind != INVALID_HANDLE_VALUE)
    {
        Fail ("FindFirstFileW: ERROR -> Found invalid NULL file\n");
    }


    //
    // find a file that exists
    //
    pFile = fopen(szFindName, "w");
    if (pFile == NULL)
    {
        Fail("FindFirstFileW: ERROR -> Unable to create a test file\n");
    }
    else
    {
        fclose(pFile);
    }
    pTemp = convert((LPSTR)szFindName);
    hFind = FindFirstFileW(pTemp, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        free(pTemp);
        Fail ("FindFirstFileW: ERROR -> Unable to find \"%s\"\n", szFindName);
    }
    else
    {
        // validate we found the correct file
        if (wcscmp(pTemp, findFileData.cFileName) != 0)
        {
            free(pTemp);
            Fail ("FindFirstFileW: ERROR -> Found the wrong file\n");
        }
    }
    free(pTemp);

    //
    // find a directory that exists
    //
    pTemp = convert((LPSTR)szDirName);
    bRc = CreateDirectoryW(pTemp, NULL);
    if (bRc == FALSE)
    {
        Fail("FindFirstFileW: ERROR[%u] -> Failed to create the directory \"%s\"\n",
        GetLastError(), szDirName);
    }

    hFind = FindFirstFileW(pTemp, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        free(pTemp);
        Fail("FindFirstFileW: ERROR. Unable to find \"%s\"\n", szDirName);
    }
    else
    {
        // validate we found the correct directory
        if (wcscmp(pTemp, findFileData.cFileName) != 0)
        {
            free(pTemp);
            Fail("FindFirstFileW: ERROR -> Found the wrong directory\n");
        }
    }
    free(pTemp);

    //
    // find a directory using a trailing '\' on the directory name: should fail
    //
    pTemp = convert((LPSTR)szDirNameSlash);
    hFind = FindFirstFileW(pTemp, &findFileData);
    free(pTemp);
    if (hFind != INVALID_HANDLE_VALUE)
    {
        Fail("FindFirstFileW: ERROR -> Able to find \"%s\": trailing "
            "slash should have failed.\n",
            szDirNameSlash);
    }

    // find a file using wild cards
    pTemp = convert((LPSTR)szFindNameWldCard_01);
    hFind = FindFirstFileW(pTemp, &findFileData);
    free(pTemp);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        Fail("FindFirstFileW: ERROR -> Unable to find \"%s\"\n",
            szFindNameWldCard_01);
    }

    pTemp = convert((LPSTR)szFindNameWldCard_02);
    hFind = FindFirstFileW(pTemp, &findFileData);
    free(pTemp);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        Fail("FindFirstFileW: ERROR -> Unable to find \"%s\"\n",
            szFindNameWldCard_02);
    }


    //
    // find a directory using wild cards
    //

    pTemp = convert((LPSTR)szDirNameWldCard_01);
    hFind = FindFirstFileW(pTemp, &findFileData);
    free(pTemp);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        Fail("FindFirstFileW: ERROR -> Unable to find \"%s\"\n",
            szDirNameWldCard_01);
    }

    pTemp = convert((LPSTR)szDirNameWldCard_02);
    hFind = FindFirstFileW(pTemp, &findFileData);
    free(pTemp);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        Fail("FindFirstFileW: ERROR -> Unable to find \"%s\"\n",
            szDirNameWldCard_02);
    }

    if(!CleanUp_FindFirstFileW_test1())
    {
        Fail("FindFirstFileW: ERROR : Final Clean Up failed\n");
    }

    PAL_Terminate();
    return PASS;
}
