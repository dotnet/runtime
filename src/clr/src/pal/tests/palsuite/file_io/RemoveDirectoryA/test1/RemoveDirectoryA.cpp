// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  RemoveDirectoryA.c
**
** Purpose: Tests the PAL implementation of the RemoveDirectoryA function.
**
**
**===================================================================*/


#define PAL_STDCPP_COMPAT
#include <palsuite.h>
#undef PAL_STDCPP_COMPAT

#include <unistd.h>


int __cdecl main(int argc, char *argv[])
{
    BOOL bRc = FALSE;
    char szDirName[252];
    DWORD curDirLen;
    char *szTemp = NULL;
    char *szTemp2 = NULL;
    char szwCurrentDir[MAX_PATH];
    char szwSubDir[MAX_PATH];

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /*
     * remove a NULL directory 
     */
    bRc = RemoveDirectoryA(NULL);
    if (bRc != FALSE)
    {
        Fail("Error[%ul]:RemoveDirectoryA: Failed since it was able to remove a"
            " NULL directory name\n", GetLastError());
    }

    /* 
     * remove a directory that does not exist 
     */
    szTemp = (char *) malloc (sizeof("test_directory"));
    sprintf_s(szTemp, sizeof("test_directory"), "test_directory");
    bRc = RemoveDirectoryA(szTemp);
    if (bRc != FALSE)
    {
        free(szTemp);
        Fail("Error[%ul]:RemoveDirectoryA: Failed since it was able to remove"
            " the non-existant directory \"test_directory\"\n", GetLastError());
    }

    /* 
     * remove a symlink to a directory
     */
    bRc = CreateDirectoryA(szTemp, NULL);
    if (bRc != TRUE)
    {
        free(szTemp);
        Fail("Error[%ul]:RemoveDirectoryA: Failed to create the directory "
            "\"test_directory\".\n", GetLastError());
    }

    char *szSymlinkName = (char *) malloc (sizeof("test_directory_symlink"));
    sprintf_s(szSymlinkName, sizeof("test_directory_symlink"), "test_directory_symlink");
    if (symlink(szTemp, szSymlinkName) != 0)
    {
        Fail("Error:RemoveDirectoryA: Failed to create a symlink to the directory \"test_directory\".\n");
    }

    bRc = RemoveDirectoryA(szSymlinkName);
    if (bRc != FALSE)
    {
        Fail("Error:RemoveDirectoryA: RemoveDirectoryA should return FALSE when passed a symlink.\n");
    }

    unlink(szSymlinkName);

    /* 
     * remove a directory that exists 
     */
    bRc = RemoveDirectoryA(szTemp);
    if (bRc == FALSE)
    {
        free(szTemp);
        Fail("Error[%ul]:RemoveDirectoryW: Failed to remove the directory "
            "\"test_directory\"\n",
            GetLastError());
    }
    /* Make sure the directory was removed */
    if( -1 != GetFileAttributesA(szTemp) )
    {
        free(szTemp);
        Fail("Error[%ul]:RemoveDirectoryA: Able to get the attributes of "
             "the removed directory\n" , GetLastError());
    }
    free(szTemp);

    /* 
     * remove long directory names (245 characters) 
     */
    curDirLen = GetCurrentDirectoryA(0, NULL) + 1;
    memset(szDirName, 0, 252);
    memset(szDirName, 'a', 245 - curDirLen);
    szTemp = (char *) malloc (sizeof(szDirName));
    szTemp = strncpy(szTemp, szDirName, strlen(szDirName) + 1);

    bRc = CreateDirectoryA(szTemp, NULL);
    if (bRc == FALSE)
    {
        free(szTemp);
        Fail("Error[%ul]:RemoveDirectoryA: Failed to create a directory name "
            "245 chars long\n" , GetLastError());
    }
    bRc = RemoveDirectoryA(szTemp);
    if (bRc == FALSE)
    {
        free(szTemp);
        Fail("Error[%ul]:RemoveDirectoryA: Failed to remove a 245 char "
            "long directory\n", GetLastError());
    }

    /* Make sure the directory was removed */
    if( -1 != GetFileAttributesA(szTemp) )
    {
        free(szTemp);
        Fail("Error[%ul]:RemoveDirectoryA: Able to get the attributes of "
             "the removed directory\n", GetLastError());
    }
    free(szTemp);

    /* 
     * directories with dots 
     */
    memset(szDirName, 0, 252);
    sprintf_s(szDirName, _countof(szDirName), ".dotDirectory");
    szTemp = (char *) malloc (sizeof(szDirName));
    szTemp = strncpy(szTemp, szDirName, strlen(szDirName) + 1);

    bRc = CreateDirectoryA(szTemp, NULL);
    if (bRc == FALSE)
    {
        free(szTemp);
        Fail("Error[%ul]:RemoveDirectoryA: Failed to create \"%s\"\n", GetLastError(), szDirName);
    }
    bRc = RemoveDirectoryA(szTemp);
    if (bRc == FALSE)
    {
        free(szTemp);
        Fail("Error[%ul]:RemoveDirectoryA: Failed to remove \"%s\"\n", GetLastError(), szDirName);
    }

    /* Make sure the directory was removed */
    if( -1 != GetFileAttributesA(szTemp) )
    {
        free(szTemp);
        Fail("Error[%ul]:RemoveDirectoryA: Able to get the attributes of "
             "the removed directory\n", GetLastError());
    }
    free(szTemp);

    /* 
     * Try calling RemoveDirectory with a file name
     */
    memset(szDirName, 0, 252);
    sprintf_s(szDirName, _countof(szDirName), "removedirectoryw.c");
    szTemp = (char *) malloc (sizeof(szDirName));
    szTemp = strncpy(szTemp, szDirName, strlen(szDirName) + 1);

    bRc = RemoveDirectoryA(szTemp);
    free(szTemp);
    if (bRc != FALSE)
    {
        Fail("Error[%ul]:RemoveDirectoryA: should have failed when "
             "called with a valid file name", GetLastError() );
    }

    /* 
     * remove a non empty directory 
     *
     * To test that, we'll first create non_empty_dir, we'll
     * set the current dir to non_empty_dir in which we'll
     * create sub_dir. We'll go back to the root of non_empty_dir
     * and we'll try to delete it (it shouldn't work).
     * After that we'll cleanup sub_dir and non_empty_dir 
     */

    /* Get the current directory so it is easy to get back
       to it later */
    if( 0 == GetCurrentDirectoryA(MAX_PATH, szwCurrentDir) )
    {
        Fail("RemoveDirectoryA: Failed to get current directory "
            "with GetCurrentDirectoryA.\n");
    }

    /* Create non_empty_dir */
    sprintf_s(szDirName, _countof(szDirName), "non_empty_dir");
    szTemp = (char *) malloc (sizeof(szDirName));
    szTemp = strncpy(szTemp, szDirName, strlen(szDirName) + 1);
    bRc = CreateDirectoryA(szTemp, NULL);
    if (bRc != TRUE)
    {
        free(szTemp);
        Fail("Error[%ul]:RemoveDirectoryA: Failed to create the directory "
             "\"non_empty_dir\" when it exists already.\n", GetLastError());
    }

    if( 0 == SetCurrentDirectoryA(szTemp) )
    {
        free(szTemp);
        Fail("Error[%ul]:RemoveDirectoryA: Failed to set current directory to "
            "\"non_empty_dir\" with SetCurrentDirectoryA.\n", GetLastError());
    }

    /* Get the directory full path so it is easy to get back
       to it later */
    if( 0 == GetCurrentDirectoryA(MAX_PATH, szwSubDir) )
    {
        free(szTemp);
        Fail("Error[%ul]:RemoveDirectoryA: Failed to get current directory "
            "with GetCurrentDirectoryA.\n", GetLastError());
    }

    /* Create sub_dir */
    sprintf_s(szDirName, _countof(szDirName), "sub_dir");
    szTemp2 = (char *) malloc (sizeof(szDirName));
    szTemp2 = strncpy(szTemp2, szDirName, strlen(szDirName) + 1);
    bRc = CreateDirectoryA(szTemp2, NULL);
    if (bRc != TRUE)
    {
        free(szTemp);
        free(szTemp2);
        Fail("Error[%ul]:RemoveDirectoryA: Failed to create the directory "
            "\"sub_dir\" when it exists already.\n", GetLastError());
    }

    /* Set the current dir to the parent of non_empty_dir/sub_dir */
    if( 0 == SetCurrentDirectoryA(szwCurrentDir) )
    {
        free(szTemp);
        free(szTemp2);
        Fail("Error[%ul]:RemoveDirectoryA: Failed to set current directory to "
            "\"non_empty_dir\" with SetCurrentDirectoryA.\n", GetLastError());
    }

    /* Try to remove non_empty_dir (shouldn't work) */
    bRc = RemoveDirectoryA(szTemp);
    if (bRc == TRUE)
    {
        free(szTemp);
        free(szTemp2);
        Fail("Error[%ul]:RemoveDirectoryA: shouldn't have been able to remove "
             "the non empty directory \"non_empty_dir\"\n", GetLastError());
    }

    /* Go back to non_empty_dir and remove sub_dir */
    if( 0 == SetCurrentDirectoryA(szwSubDir) )
    {
        free(szTemp);
        free(szTemp2);
        Fail("Error[%ul]:RemoveDirectoryA: Failed to set current directory to "
            "\"non_empty_dir\" with SetCurrentDirectoryA.\n", GetLastError());
    }

    bRc = RemoveDirectoryA(szTemp2);
    if (bRc == FALSE)
    {
        free(szTemp);
        free(szTemp2);
        Fail("Error[%ul]:RemoveDirectoryA: unable to remove "
             "directory \"sub_dir\" \n",
             GetLastError());
    }
    /* Make sure the directory was removed */
    if( -1 != GetFileAttributesA(szTemp2) )
    {
        Fail("Error[%ul]RemoveDirectoryA: Able to get the attributes of "
             "the removed directory\n", GetLastError());
    }
    free(szTemp2);

    /* Go back to parent of non_empty_dir and remove non_empty_dir */
    if( 0 == SetCurrentDirectoryA(szwCurrentDir) )
    {
        free(szTemp);
        Fail("Error[%ul]:RemoveDirectoryA: Failed to set current directory to "
            "\"..\non_empty_dir\" with SetCurrentDirectoryA.\n", GetLastError());
    }
    bRc = RemoveDirectoryA(szTemp);
    if (bRc == FALSE)
    {
        free(szTemp);    
        Fail("Error[%ul]RemoveDirectoryA: unable to remove "
             "the directory \"non_empty_dir\"\n",
             GetLastError());
    }
    /* Make sure the directory was removed */
    if( -1 != GetFileAttributesA(szTemp) )
    {
        Fail("Error[%ul]:RemoveDirectoryA: Able to get the attributes of "
             "the removed directory\n", GetLastError());
    }
    free(szTemp); 


    PAL_Terminate();  
    return PASS;
}
