// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  RemoveDirectoryW.c
**
** Purpose: Tests the PAL implementation of the RemoveDirectoryW function.
**
**
**===================================================================*/


#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    BOOL bRc = FALSE;
    char szDirName[252];
    DWORD curDirLen;
    WCHAR *szwTemp = NULL;
    WCHAR *szwTemp2 = NULL;
    WCHAR szwCurrentDir[MAX_PATH];
    WCHAR szwSubDir[MAX_PATH];

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /*
     * remove a NULL directory 
     */
    bRc = RemoveDirectoryW(NULL);
    if (bRc != FALSE)
    {
        Fail("RemoveDirectoryW: Failed since it was able to remove a"
            " NULL directory name\n");
    }

    /* 
     * remove a directory that does not exist 
     */
    szwTemp = convert("test_directory");
    bRc = RemoveDirectoryW(szwTemp);
    if (bRc != FALSE)
    {
        free(szwTemp);
        Fail("RemoveDirectoryW: Failed since it was able to remove"
            " the non-existant directory \"test_directory\"\n");
    }

    /* 
     * remove a directory that exists 
     */
    bRc = CreateDirectoryW(szwTemp, NULL);
    if (bRc != TRUE)
    {
        free(szwTemp);
        Fail("RemoveDirectoryW: Failed to create the directory "
            "\"test_directory\" when it exists already.\n");
    }
    bRc = RemoveDirectoryW(szwTemp);
    if (bRc == FALSE)
    {
        free(szwTemp);
        Fail("RemoveDirectoryW: Failed to remove the directory "
            "\"test_directory\" (error code %d)\n",
            GetLastError());
    }
    /* Make sure the directory was removed */
    if( -1 != GetFileAttributesW(szwTemp) )
    {
        free(szwTemp);
        Fail("RemoveDirectoryW: Able to get the attributes of "
             "the removed directory\n");
    }
    free(szwTemp);

    /* 
     * remove long directory names (245 characters) 
     */
    curDirLen = GetCurrentDirectoryA(0, NULL) + 1;
    memset(szDirName, 0, 252);
    memset(szDirName, 'a', 245 - curDirLen);
    szwTemp = convert(szDirName);
    bRc = CreateDirectoryW(szwTemp, NULL);
    if (bRc == FALSE)
    {
        free(szwTemp);
        Fail("RemoveDirectoryW: Failed to create a directory name "
            "245 chars long\n");
    }
    bRc = RemoveDirectoryW(szwTemp);
    if (bRc == FALSE)
    {
        free(szwTemp);
        Fail("RemoveDirectoryW: Failed to remove a 245 char "
            "long directory\n");
    }

    /* Make sure the directory was removed */
    if( -1 != GetFileAttributesW(szwTemp) )
    {
        free(szwTemp);
        Fail("RemoveDirectoryW: Able to get the attributes of "
             "the removed directory\n");
    }
    free(szwTemp);

    /* 
     * directories with dots 
     */
    memset(szDirName, 0, 252);
    sprintf_s(szDirName, _countof(szDirName), ".dotDirectory");
    szwTemp = convert(szDirName);
    bRc = CreateDirectoryW(szwTemp, NULL);
    if (bRc == FALSE)
    {
        free(szwTemp);
        Fail("RemoveDirectoryW: Failed to create \"%s\"\n", szDirName);
    }
    bRc = RemoveDirectoryW(szwTemp);
    if (bRc == FALSE)
    {
        free(szwTemp);
        Fail("RemoveDirectoryW: Failed to remove \"%s\"\n", szDirName);
    }

    /* Make sure the directory was removed */
    if( -1 != GetFileAttributesW(szwTemp) )
    {
        free(szwTemp);
        Fail("RemoveDirectoryW: Able to get the attributes of "
             "the removed directory\n");
    }
    free(szwTemp);

    /* 
     * Try calling RemoveDirectory with a file name
     */
    memset(szDirName, 0, 252);
    sprintf_s(szDirName, _countof(szDirName), "removedirectoryw.c");
    szwTemp = convert(szDirName);

    bRc = RemoveDirectoryW(szwTemp);
    free(szwTemp);
    if (bRc != FALSE)
    {
        Fail("RemoveDirectoryW: should have failed when "
             "called with a valid file name" );
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
    if( 0 == GetCurrentDirectoryW(MAX_PATH, szwCurrentDir) )
    {
        Fail("RemoveDirectoryW: Failed to get current directory "
            "with GetCurrentDirectoryW.\n");
    }

    /* Create non_empty_dir */
    szwTemp = convert("non_empty_dir");
    bRc = CreateDirectoryW(szwTemp, NULL);
    if (bRc != TRUE)
    {
        free(szwTemp);
        Fail("RemoveDirectoryW: Failed to create the directory "
             "\"non_empty_dir\" when it exists already.\n");
    }

    if( 0 == SetCurrentDirectoryW(szwTemp) )
    {
        free(szwTemp);
        Fail("RemoveDirectoryW: Failed to set current directory to "
            "\"non_empty_dir\" with SetCurrentDirectoryW.\n");
    }

    /* Get the directory full path so it is easy to get back
       to it later */
    if( 0 == GetCurrentDirectoryW(MAX_PATH, szwSubDir) )
    {
        free(szwTemp);
        Fail("RemoveDirectoryW: Failed to get current directory "
            "with GetCurrentDirectoryW.\n");
    }

    /* Create sub_dir */
    szwTemp2 = convert("sub_dir");
    bRc = CreateDirectoryW(szwTemp2, NULL);
    if (bRc != TRUE)
    {
        free(szwTemp);
        free(szwTemp2);
        Fail("RemoveDirectoryW: Failed to create the directory "
            "\"sub_dir\" when it exists already.\n");
    }

    /* Set the current dir to the parent of non_empty_dir/sub_dir */
    if( 0 == SetCurrentDirectoryW(szwCurrentDir) )
    {
        free(szwTemp);
        free(szwTemp2);
        Fail("RemoveDirectoryW: Failed to set current directory to "
            "\"non_empty_dir\" with SetCurrentDirectoryW.\n");
    }

    /* Try to remove non_empty_dir (shouldn't work) */
    bRc = RemoveDirectoryW(szwTemp);
    if (bRc == TRUE)
    {
        free(szwTemp);
        free(szwTemp2);
        Fail("RemoveDirectoryW: shouldn't have been able to remove "
             "the non empty directory \"non_empty_dir\"\n");
    }

    /* Go back to non_empty_dir and remove sub_dir */
    if( 0 == SetCurrentDirectoryW(szwSubDir) )
    {
        free(szwTemp);
        free(szwTemp2);
        Fail("RemoveDirectoryW: Failed to set current directory to "
            "\"non_empty_dir\" with SetCurrentDirectoryW.\n");
    }

    bRc = RemoveDirectoryW(szwTemp2);
    if (bRc == FALSE)
    {
        free(szwTemp);
        free(szwTemp2);
        Fail("RemoveDirectoryW: unable to remove "
             "directory \"sub_dir\"(error code %d)\n",
             GetLastError());
    }
    /* Make sure the directory was removed */
    if( -1 != GetFileAttributesW(szwTemp2) )
    {
        Fail("RemoveDirectoryW: Able to get the attributes of "
             "the removed directory\n");
    }
    free(szwTemp2);

    /* Go back to parent of non_empty_dir and remove non_empty_dir */
    if( 0 == SetCurrentDirectoryW(szwCurrentDir) )
    {
        free(szwTemp);
        Fail("RemoveDirectoryW: Failed to set current directory to "
            "\"..\non_empty_dir\" with SetCurrentDirectoryW.\n");
    }
    bRc = RemoveDirectoryW(szwTemp);
    if (bRc == FALSE)
    {
        free(szwTemp);    
        Fail("RemoveDirectoryW: unable to remove "
             "the directory \"non_empty_dir\"(error code %d)\n",
             GetLastError());
    }
    /* Make sure the directory was removed */
    if( -1 != GetFileAttributesW(szwTemp) )
    {
        Fail("RemoveDirectoryW: Able to get the attributes of "
             "the removed directory\n");
    }
    free(szwTemp); 


    PAL_Terminate();  
    return PASS;
}
