// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  FindClose.c
**
** Purpose: Tests the PAL implementation of the FindClose function.
**
**
**===================================================================*/


#include <palsuite.h>



const WCHAR szFindName[] =          {'t','e', 's', 't', '0', '1', '.', 't', 'x', 't', '\0'};
const WCHAR szFindName_02[] =       {'t','e', 's', 't', '0', '2', '.', 't', 'x', 't', '\0'};
const WCHAR szFindName_03[] =       {'t','e', 's', 't', '0', '3', '.', 't', 'x', 't', '\0'};
const WCHAR szFindNameWldCard_01[] =  {'t','e', 's', 't', '0', '?', '.', 't', 'x', 't', '\0'};
const WCHAR szFindNameWldCard_02[] =  {'*', '.', 't', 'x', 't', '\0'};
const WCHAR szDirName[] =             {'t','e', 's', 't', '_', 'd', 'i', 'r', '\0'};
const WCHAR szDirName_02[] =          {'t','e', 's', 't', '_', 'd', 'i', 'r', '0', '2', '\0'};
const WCHAR szDirNameWldCard[] =      {'t','e', 's', 't', '_', '*', '\0'};



BOOL createTestFile(const WCHAR* szName)
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
    RemoveDirectoryW(szDirName);
    RemoveDirectoryW(szDirName_02);

    DeleteFileW(szFindName);
    DeleteFileW(szFindName_02);
    DeleteFileW(szFindName_03);
}


int __cdecl main(int argc, char *argv[])
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
    if(createTestFile(szFindName) == FALSE)
    {
        removeAll();
        PAL_TerminateEx(FAIL);
        return FAIL;
    }
    if(createTestFile(szFindName_02) == FALSE)
    {
        removeAll();
        PAL_TerminateEx(FAIL);
        return FAIL;
    }
    if(createTestFile(szFindName_03) == FALSE)
    {
        removeAll();
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    // close a FindFirstFileW handle
    hFind = FindFirstFileW(szFindName, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        pTemp = convertC((WCHAR*)szFindName);
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
    hFind = FindFirstFileW(szFindName, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        pTemp = convertC((WCHAR*)szFindName);
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
    bRc = CreateDirectoryW(szDirName, NULL);
    if (bRc == FALSE)
    {
        pTemp = convertC((WCHAR*)szDirName);
        Trace("FindClose: ERROR -> Failed to create the directory \"%s\"\n", 
            pTemp);
        free(pTemp);
        removeAll();
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    bRc = CreateDirectoryW(szDirName_02, NULL);
    if (bRc == FALSE)
    {
        pTemp = convertC((WCHAR*)szDirName_02);
        Trace("FindClose: ERROR -> Failed to create the directory \"%s\"\n",
            pTemp);
        free(pTemp);
        removeAll();
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    hFind = FindFirstFileW(szDirName, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        pTemp = convertC((WCHAR*)szDirName);
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
    hFind = FindFirstFileW(szFindNameWldCard_01, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        pTemp = convertC((WCHAR*)szFindNameWldCard_01);
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
    hFind = FindFirstFileW(szDirNameWldCard, &findFileData);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        pTemp = convertC((WCHAR*)szDirNameWldCard);
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

