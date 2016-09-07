// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  createdirectoryw.c
**
** Purpose: Tests the PAL implementation of the CreateDirectoryW function.
**          Test creating a directory in a directory path that does not exist.
**          Test creating directory with trailing slashes.
**
** Depends on:
**          RemoveDirectoryW.
**
**
**==========================================================================*/

#include <palsuite.h>

#if WIN32
WCHAR*  szTestRootDir           = NULL; 
#endif

WCHAR*  szTestDir               = NULL;
WCHAR*  szTestSubDir            = NULL;            
WCHAR*  szTest2SubDir           = NULL;
WCHAR*  szTest2SubDirWinSlash   = NULL;
WCHAR*  szTest2SubDirUnixSlash  = NULL;


/* Free the memory allocated by convert(...) function*/
static void CleanMemory(){

#if WIN32
    free(szTestRootDir);
#endif

    free( szTestDir);
    free( szTestSubDir);            
    free( szTest2SubDir);
    free( szTest2SubDirWinSlash);
    free( szTest2SubDirUnixSlash);

}


int main(int argc, char *argv[])
{
    BOOL        bRc             = FALSE;
    BOOL        clean           = TRUE;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* initialize  strings */

#if WIN32
    szTestRootDir   = convert("/at_root_directory_PALTEST");
#endif

    szTestDir       = convert("test_ directory");
    szTestSubDir    = convert(
            ".\\./././../test2/./../../////////createdirectoryw"
            "\\\\/test2/test_ directory\\sub");
    szTest2SubDir   = convert("test_ directory/sub\\sub_sub");
    szTest2SubDirWinSlash   = convert("test_ directory/sub\\sub_sub\\\\");
    szTest2SubDirUnixSlash  = convert("test_ directory/sub\\sub_sub///");


    /* Platform dependent cases:-
    * test for WIN32, create directory at the root.
    * using /directory_name format
    */
#if WIN32

    bRc = CreateDirectoryW(szTestRootDir, NULL);

    if (bRc != TRUE)
    {

        Trace("CreateDirectoryW: Failed creating the directory "
            "\"%S\" with the error code %ld.\n",
            szTestRootDir,GetLastError());
        CleanMemory();
        Fail("");
    }

    /*clean  szTestRootDir */
    bRc   = RemoveDirectoryW(szTestRootDir);

    if (! bRc)
    {
        clean = bRc;
        Trace("CreateDirectoryW: RemoveDirectoryW failed to remove "
            "\"%S\" with the error code %ld.\n",
            szTestRootDir,
            GetLastError());
    }

#endif


    /* 
    * create  subdirectory "test_directory//sub//sub_sub"
    * while parent directory does not exist.
    */
    bRc = CreateDirectoryW(szTest2SubDir, NULL);
    if (bRc == TRUE)
    {
        bRc = RemoveDirectoryW(szTest2SubDir);

        if (! bRc )
        {
            Trace("CreateDirectoryW: RemoveDirectoryW failed to remove "
                "\"%S\" with the error code %ld.\n",
                szTest2SubDir,
                GetLastError());
        }

        Trace("CreateDirectoryW: Succeeded creating the directory\"%S\" while"
            " its parent directory does not exists. It should fail.\n",
            szTest2SubDir);
        CleanMemory();
        Fail("");

    }


    /* create directory tree one by one 
    * first create "test_dir" 
    */
    bRc = CreateDirectoryW(szTestDir, NULL);


    if (bRc != TRUE)/*failed creating the path*/
    {

        Trace("CreateDirectoryW: Failed creating the directory "
            "\"%S\" with the error code %ld.\n", szTestDir,GetLastError());
        CleanMemory();
        Fail("");
    }

    /* create the sub directory test_directory//sub */
    bRc = CreateDirectoryW(szTestSubDir, NULL);

    if (bRc != TRUE)/*failed creating the path*/
    {
        Trace("CreateDirectoryW: Failed creating the directory "
            "\"%S\" with the error code %ld.\n",
            szTestSubDir , GetLastError());

        /* cleaning... remove parent directory */
        bRc = RemoveDirectoryW(szTestDir);
        if (! bRc)
        {
            Trace("CreateDirectoryW: RemoveDirectoryW failed to remove "
                "\"%S\" with an error code %ld.\n",
                szTestDir,
                GetLastError());
        }
        CleanMemory();
        Fail("");
    }

    /* 
    *  the director structure is test_directory//sub
    *  test creating directory " test_directory//sub//sub_sub" 
    */
    bRc = CreateDirectoryW(szTest2SubDir, NULL);
    if (bRc != TRUE)
    {
        Trace("CreateDirectoryW: Failed creating the directory "
            "\"%S\" with the error code %ld.\n",
            szTest2SubDir , GetLastError());

        /* remove parent directory test_directory//sub */
        bRc = RemoveDirectoryW(szTestSubDir);
        if (! bRc)
        {
            Trace("CreateDirectoryW: RemoveDirectoryW failed to remove "
                "\"%S\" with the error code %ld.\n",
                szTestSubDir,
                GetLastError());
        }

        /* remove parent directory test_directory */
        bRc = RemoveDirectoryW(szTestDir);
        if (! bRc)
        {
            Trace("CreateDirectoryW: RemoveDirectoryW failed to remove "
                "\"%S\" with the error code %ld.\n",
                szTestDir,
                GetLastError());
        }
        CleanMemory();
        Fail("");

    }

    /* Remove Directiry szTest2SubDir*/
    bRc = RemoveDirectoryW(szTest2SubDir);

    if (! bRc)
    {
        clean = bRc;
        Trace("CreateDirectoryW: RemoveDirectoryW failed to remove "
            "\"%S\" with the error code %ld.\n",
            szTest2SubDir,
            GetLastError());
    }

    /* 
    *  the director structure is test_directory//sub
    *  test creating directory " test_directory//sub//sub_sub\\\\" 
    */
    bRc = CreateDirectoryW(szTest2SubDirWinSlash, NULL);
    if (bRc != TRUE)
    {
        Trace("CreateDirectoryW: Failed creating the directory "
            "\"%S\" with the error code %ld.\n",
            szTest2SubDirWinSlash , GetLastError());

        /* remove parent directory test_directory//sub */
        bRc = RemoveDirectoryW(szTestSubDir);
        if (! bRc)
        {
            Trace("CreateDirectoryW: RemoveDirectoryW failed to remove "
                "\"%S\" with the error code %ld.\n",
                szTestSubDir,
                GetLastError());
        }

        /* remove parent directory test_directory */
        bRc = RemoveDirectoryW(szTestDir);
        if (! bRc)
        {
            Trace("CreateDirectoryW: RemoveDirectoryW failed to remove "
                "\"%S\" with the error code %ld.\n",
                szTestDir,
                GetLastError());
        }
        CleanMemory();
        Fail("");

    }

    /* Remove Directiry szTest2SubDirWinSlash */
    bRc = RemoveDirectoryW(szTest2SubDirWinSlash);

    if (! bRc)
    {
        clean = bRc;
        Trace("CreateDirectoryW: RemoveDirectoryW failed to remove "
            "\"%S\" with the error code %ld.\n",
            szTest2SubDirWinSlash,
            GetLastError());
    }

    /* 
    *  the director structure is test_directory//sub
    *  test creating directory " test_directory//sub//sub_sub///" 
    */
    bRc = CreateDirectoryW(szTest2SubDirUnixSlash, NULL);
    if (bRc != TRUE)
    {
        Trace("CreateDirectoryW: Failed creating the directory "
            "\"%S\" with the error code %ld.\n",
            szTest2SubDirUnixSlash , GetLastError());

        /* remove parent directory test_directory//sub */
        bRc = RemoveDirectoryW(szTestSubDir);
        if (! bRc)
        {
            Trace("CreateDirectoryW: RemoveDirectoryW failed to remove "
                "\"%S\" with the error code %ld.\n",
                szTestSubDir,
                GetLastError());
        }

        /* remove parent directory test_directory */
        bRc = RemoveDirectoryW(szTestDir);
        if (! bRc)
        {
            Trace("CreateDirectoryW: RemoveDirectoryW failed to remove "
                "\"%S\" with the error code %ld.\n",
                szTestDir,
                GetLastError());
        }
        CleanMemory();
        Fail("");

    }

    /* Remove Directiry szTest2SubDirUnixSlash.*/
    bRc = RemoveDirectoryW(szTest2SubDirUnixSlash);

    if (! bRc)
    {
        clean = bRc;
        Trace("CreateDirectoryW: RemoveDirectoryW failed to remove "
            "\"%S\" with the error code %ld.\n",
            szTest2SubDirUnixSlash,
            GetLastError());
    }

    /*clean parent szTestSubDir */
    bRc = RemoveDirectoryW(szTestSubDir);

    if (! bRc)
    {
        clean = bRc;
        Trace("CreateDirectoryW: RemoveDirectoryW failed to remove "
            "\"%S\" with the error code %ld.\n",
            szTestSubDir,
            GetLastError());
    }

    /*clean parent szTestDir */
    bRc = RemoveDirectoryW(szTestDir);


    if (! bRc)
    {
        clean = bRc;
        Trace("CreateDirectoryW: RemoveDirectoryW failed to remove "
            "\"%S\" with the error code %ld.\n",
            szTestDir,
            GetLastError());
    }

    if(! clean)
    {
        CleanMemory();
        Fail("");
    }

    CleanMemory();

    PAL_Terminate();  
    return PASS;
}
