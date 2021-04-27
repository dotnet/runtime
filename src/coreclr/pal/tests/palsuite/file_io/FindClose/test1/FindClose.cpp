// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  FindClose.c
**
** Purpose: Tests the PAL implementation of the FindClose function.
**
**
**===================================================================*/


#include <palsuite.h>



const WCHAR szFindName_FindClose_test1[] =          {'t','e', 's', 't', '0', '1', '.', 't', 'x', 't', '\0'};
const WCHAR szFindName_02_FindClose_test1[] =       {'t','e', 's', 't', '0', '2', '.', 't', 'x', 't', '\0'};
const WCHAR szFindName_03_FindClose_test1[] =       {'t','e', 's', 't', '0', '3', '.', 't', 'x', 't', '\0'};
const WCHAR szFindNameWldCard_01_FindClose_test1[] =  {'t','e', 's', 't', '0', '?', '.', 't', 'x', 't', '\0'};
const WCHAR szFindNameWldCard_02_FindClose_test1[] =  {'*', '.', 't', 'x', 't', '\0'};
const WCHAR szDirName_FindClose_test1[] =             {'t','e', 's', 't', '_', 'd', 'i', 'r', '\0'};
const WCHAR szDirName_02_FindClose_test1[] =          {'t','e', 's', 't', '_', 'd', 'i', 'r', '0', '2', '\0'};
const WCHAR szDirNameWldCard_FindClose_test1[] =      {'t','e', 's', 't', '_', '*', '\0'};



BOOL createTestFile_FindClose_test1(const WCHAR* szName)
{
    FILE *pFile = NULL;
    char* pTemp = NULL;

    pTemp = convertC((WCHAR*)szName);
    pFile = fopen(pTemp, "w");
    if (pFile == NULL)
    {
        Trace("FindClose: ERROR -> Unable to create file \"%s\".\n", pTemp);
        free(pTemp);
        return FALSE;
    }
    else
    {
        fprintf(pFile, "FindClose test file, \"%s\".\n", pTemp);
        free(pTemp);
        fclose(pFile);
    }
    return TRUE;
}


void removeAll()
{
    RemoveDirectoryW(szDirName_FindClose_test1);
    RemoveDirectoryW(szDirName_02_FindClose_test1);

    DeleteFileW(szFindName_FindClose_test1);
    DeleteFileW(szFindName_02_FindClose_test1);
    DeleteFileW(szFindName_03_FindClose_test1);
}


PALTEST(file_io_FindClose_test1_paltest_findclose_test1, "file_io/FindClose/test1/paltest_findclose_test1")
{
    WIN32_FIND_DATAW findFileData;
    WIN32_FIND_DATAW findFileData_02;
    HANDLE hFind = NULL;
    BOOL bRc = FALSE;
    char* pTemp = NULL;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* do some clean up just to be sure */
    removeAll();

    /* FindClose a null  handle */
    if(FindClose(NULL)!=0)
    {
         Fail("FindClose: ERROR -> Closing a NULL handle succeeded.\n");
    }

    /* find a file that exists */
    if(createTestFile_FindClose_test1(szFindName_FindClose_test1) == FALSE)
    {
        removeAll();
        PAL_TerminateEx(FAIL);
        return FAIL;
    }
    if(createTestFile_FindClose_test1(szFindName_02_FindClose_test1) == FALSE)
    {
        removeAll();
        PAL_TerminateEx(FAIL);
        return FAIL;
    }
    if(createTestFile_FindClose_test1(szFindName_03_FindClose_test1) == FALSE)
    {
        removeAll();
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    // close a FindFirstFileW handle
    hFind = FindFirstFileW(szFindName_FindClose_test1, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        pTemp = convertC((WCHAR*)szFindName_FindClose_test1);
        Trace("FindClose: ERROR -> Unable to find \"%s\"\n", pTemp);
        free(pTemp);
        removeAll();
        PAL_TerminateEx(FAIL);
        return FAIL;
    }
    else
    {
        bRc = FindClose(hFind);
        if (bRc == FALSE)
        {
            removeAll();
            Fail("FindClose: ERROR -> Unable to close a valid"
                " FindFirstFileW handle.\n");
        }
    }
    hFind = FindFirstFileW(szFindName_FindClose_test1, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        pTemp = convertC((WCHAR*)szFindName_FindClose_test1);
        Trace("FindClose: ERROR -> Unable to find \"%s\"\n", pTemp);
        free(pTemp);
        removeAll();
        PAL_TerminateEx(FAIL);
        return FAIL;
    }
    else
    {
        bRc = FindNextFileW(hFind, &findFileData);
        if (bRc != FALSE)
        {
            removeAll();
            Fail("FindClose: ERROR -> Found a file that doesn't exist.\n");
        }
        else
        {
            bRc = FindClose(hFind);
            if (bRc == FALSE)
            {
                removeAll();
                Fail("FindClose: ERROR -> Unable to close a valid "
                    "FindNextFileW handle.\n");
            }
        }
    }

    /* find a directory that exists */
    bRc = CreateDirectoryW(szDirName_FindClose_test1, NULL);
    if (bRc == FALSE)
    {
        pTemp = convertC((WCHAR*)szDirName_FindClose_test1);
        Trace("FindClose: ERROR -> Failed to create the directory \"%s\"\n", 
            pTemp);
        free(pTemp);
        removeAll();
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    bRc = CreateDirectoryW(szDirName_02_FindClose_test1, NULL);
    if (bRc == FALSE)
    {
        pTemp = convertC((WCHAR*)szDirName_02_FindClose_test1);
        Trace("FindClose: ERROR -> Failed to create the directory \"%s\"\n",
            pTemp);
        free(pTemp);
        removeAll();
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    hFind = FindFirstFileW(szDirName_FindClose_test1, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        pTemp = convertC((WCHAR*)szDirName_FindClose_test1);
        Trace("FindClose: ERROR. FindFirstFileW was unable to find \"%s\"\n",
            pTemp);
        free(pTemp);
        removeAll();
        PAL_TerminateEx(FAIL);
        return FAIL;
    }
    else
    {
        bRc = FindClose(hFind);
        if (bRc == FALSE)
        {
            removeAll();
            Fail("FindClose: ERROR -> Unable to close a valid"
                " FindFirstFileW handle of a directory.\n");
        }
    }

    /* find a file using wild cards */
    hFind = FindFirstFileW(szFindNameWldCard_01_FindClose_test1, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        pTemp = convertC((WCHAR*)szFindNameWldCard_01_FindClose_test1);
        Trace("FindClose: ERROR -> FindFirstFileW was unable to find \"%s\"\n",
            pTemp);
        free(pTemp);
        removeAll();
        PAL_TerminateEx(FAIL);
        return FAIL;
    }
    else
    {
        bRc = FindNextFileW(hFind, &findFileData_02);
        if (bRc == FALSE)
        {
            removeAll();
            Fail("FindClose: ERROR -> Unable to find another file.\n");
        }
        else
        {
            bRc = FindClose(hFind);
            if (bRc == FALSE)
            {
                removeAll();
                Fail("FindClose: ERROR -> Unable to close a valid"
                    " FindNextFileW handle.\n");
            }
        }
    }

    /* find a directory using wild cards */
    hFind = FindFirstFileW(szDirNameWldCard_FindClose_test1, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        pTemp = convertC((WCHAR*)szDirNameWldCard_FindClose_test1);
        Trace("FindClose: ERROR -> Unable to find \"%s\"\n",
            pTemp);
        free(pTemp);
        removeAll();
        PAL_TerminateEx(FAIL);
        return FAIL;
    }
    else
    {
        bRc = FindNextFileW(hFind, &findFileData_02);
        if (bRc == FALSE)
        {
            removeAll();
            Fail("FindClose: ERROR -> Unable to find another directory.\n");
        }
        else
        {
            bRc = FindClose(hFind);
            if (bRc == FALSE)
            {
                removeAll();
                Fail("FindClose: ERROR -> Unable to close a valid"
                    " FindNextFileW handle of a directory.\n");
            }
        }
    }


    removeAll();
    PAL_Terminate();  

    return PASS;
}

