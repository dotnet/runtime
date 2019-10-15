// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  createdirectorya.c
**
** Purpose: Tests the PAL implementation of the CreateDirectoryA function.
**          Test creating a directory in a directory path that does not exist.
**          Test creating directory with trailing slashes.
**
** Depends on:
**          RemoveDirectoryW (since RemoveDirectoryA is unavailable)
**
**
**==========================================================================*/



#include <palsuite.h>

int main(int argc, char *argv[])
{

#if WIN32
    const char* szTestRootDir   = {"/at_root_directory_PALTEST"};
#endif

    const char* szTestDir       = {"test_ directory"};
    const char* szTestSubDir    = 
            {".\\./././../test2/./../../////////createdirectorya"
            "\\\\/test2/test_ directory\\sub"};
    const char* szTest2SubDir   = {"test_ directory/sub\\sub_sub"};
    const char* szTest2SubDirWinSlash  =
            {"test_ directory/sub\\sub_sub\\"};
    const char* szTest2SubDirUnixSlash =
            {"test_ directory/sub\\sub_sub/////"};
    BOOL        bRc             = FALSE;
    BOOL        clean           = TRUE;
    WCHAR*      pTemp           = NULL;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /*  platform dependent cases */
    /* test for WIN32, create directory at the root."
    * using '/directory_name ' format */
#if WIN32

    bRc = CreateDirectoryA(szTestRootDir, NULL);

    if (bRc != TRUE)
    {
        Fail("CreateDirectoryA: Failed creating the directory "
            "\"%s\" with the error code %ld.\n",
            szTestRootDir,GetLastError());
    }

    /* clean  szTestRootDir */
    pTemp = convert((LPSTR) szTestRootDir);
    bRc   = RemoveDirectoryW(pTemp);
    free(pTemp);

    if (! bRc)
    {
        clean=bRc;
        Trace("CreateDirectoryA: RemoveDirectoryW failed to remove "
            "\"%s\" with the error code %ld.\n",
            szTestRootDir,
            GetLastError());
    }

#endif


    /* 
    * create  subdirectory "test_directory//sub//sub_sub"
    * while parent directory does not exist.
    */
    bRc = CreateDirectoryA(szTest2SubDir, NULL);
    if (bRc == TRUE)
    {
        pTemp = convert((LPSTR)szTest2SubDir);
        bRc = RemoveDirectoryW(pTemp);
        free(pTemp);
        if (!bRc)
        {
            Trace("CreateDirectoryA: RemoveDirectoryW failed to remove "
                "\"%s\" with the error code %ld.\n",
                szTest2SubDir,
                GetLastError());
        }
        Fail("CreateDirectoryA: Succeeded creating the directory\"%s\" while"
            " its parent directory does not exists. It should fail.\n",
            szTest2SubDir);

    }


    /* create directory tree one by one 
    * first create "test_dir" 
    */
    bRc = CreateDirectoryA(szTestDir, NULL);

    if (bRc != TRUE)/*failed creating the path*/
    {
        Fail("CreateDirectoryA: Failed creating the directory "
            "\"%s\" with the error code %ld.\n", szTestDir,GetLastError());
    }

    /* create the sub directory test_directory//sub */
    bRc = CreateDirectoryA(szTestSubDir, NULL);

    if (bRc != TRUE)/*failed creating the path*/
    {
        Trace("CreateDirectoryA: Failed creating the directory "
            "\"%s\" with the error code %ld.\n",
            szTestSubDir , GetLastError());

        /* cleaning... remove parent directory */
        pTemp = convert((LPSTR)szTestDir);
        bRc = RemoveDirectoryW(pTemp);
        free(pTemp);
        if (!bRc)
        {
            Trace("CreateDirectoryA: RemoveDirectoryW failed to remove "
                "\"%s\" with an error code %ld.\n",
                szTestDir,
                GetLastError());
        }
        Fail("");
    }

    /* 
    *  the director structure is test_directory//sub
    *  test creating directory " test_directory//sub//sub_sub" 
    */
    bRc = CreateDirectoryA(szTest2SubDir, NULL);
    if (bRc != TRUE)
    {
        Trace("CreateDirectoryA: Failed creating the directory "
            "\"%s\" with the error code %ld.\n",
            szTest2SubDir , GetLastError());

        /* remove parent directory test_directory//sub */
        pTemp = convert((LPSTR)szTestSubDir);
        bRc   = RemoveDirectoryW(pTemp);
        free(pTemp);
        if (!bRc)
        {
            Trace("CreateDirectoryA: RemoveDirectoryW failed to remove "
                "\"%s\" with the error code %ld.\n",
                szTestSubDir,
                GetLastError());
        }

        /* remove parent directory test_directory */
        pTemp = convert((LPSTR)szTestDir);
        bRc = RemoveDirectoryW(pTemp);
        free(pTemp);
        if (! bRc)
        {
            Trace("CreateDirectoryA: RemoveDirectoryW failed to remove "
                "\"%s\" with the error code %ld.\n",
                szTestDir,
                GetLastError());
        }
        Fail("");

    }

    /* RemoveDirectiryW szTest2SubDir */
    pTemp = convert((LPSTR)szTest2SubDir);
    bRc = RemoveDirectoryW(pTemp);
    free(pTemp);

    if (! bRc)
    {
        clean=bRc;
        Trace("CreateDirectoryA: RemoveDirectoryW failed to remove "
            "\"%s\" with the error code %ld.\n",
            szTest2SubDir,
            GetLastError());
    }


    /* 
    *  the director structure is test_directory//sub
    *  test creating directory " test_directory//sub//sub_sub\\" 
    */
    bRc = CreateDirectoryA(szTest2SubDirWinSlash, NULL);
    if (bRc != TRUE)
    {
        Trace("CreateDirectoryA: Failed creating the directory "
            "\"%s\" with the error code %ld.\n",
            szTest2SubDirWinSlash , GetLastError());

        /* remove parent directory test_directory//sub */
        pTemp = convert((LPSTR)szTestSubDir);
        bRc = RemoveDirectoryW(pTemp);
        free(pTemp);
        if (! bRc)
        {
            Trace("CreateDirectoryA: RemoveDirectoryW failed to remove "
                "\"%s\" with the error code %ld.\n",
                szTestSubDir,
                GetLastError());
        }

        /* remove parent directory test_directory */
        pTemp = convert((LPSTR)szTestDir);
        bRc = RemoveDirectoryW(pTemp);
        free(pTemp);
        if (! bRc)
        {
            Trace("CreateDirectoryA: RemoveDirectoryW failed to remove "
                "\"%s\" with the error code %ld.\n",
                szTestDir,
                GetLastError());
        }
        Fail("");

    }

    /* RemoveDirectiryW szTest2SubDirWinSlash */
    pTemp = convert((LPSTR)szTest2SubDirWinSlash);
    bRc = RemoveDirectoryW(pTemp);
    free(pTemp);

    if (! bRc)
    {
        clean=bRc;
        Trace("CreateDirectoryA: RemoveDirectoryW failed to remove "
            "\"%s\" with the error code %ld.\n",
            szTest2SubDirWinSlash,
            GetLastError());
    }

    /* 
    *  the director structure is test_directory//sub
    *  test creating directory " test_directory//sub//sub_sub/////" 
    */
    bRc = CreateDirectoryA(szTest2SubDirUnixSlash, NULL);
    if (bRc != TRUE)
    {
        Trace("CreateDirectoryA: Failed creating the directory "
            "\"%s\" with the error code %ld.\n",
            szTest2SubDirUnixSlash , GetLastError());

        /* remove parent directory test_directory//sub */
        pTemp = convert((LPSTR)szTestSubDir);
        bRc = RemoveDirectoryW(pTemp);
        free(pTemp);
        if (! bRc)
        {
            Trace("CreateDirectoryA: RemoveDirectoryW failed to remove "
                "\"%s\" with the error code %ld.\n",
                szTestSubDir,
                GetLastError());
        }

        /* remove parent directory test_directory */
        pTemp = convert((LPSTR)szTestDir);
        bRc   = RemoveDirectoryW(pTemp);
        free(pTemp);
        if (! bRc)
        {
            Trace("CreateDirectoryA: RemoveDirectoryW failed to remove "
                "\"%s\" with the error code %ld.\n",
                szTestDir,
                GetLastError());
        }
        Fail("");

    }

    /* RemoveDirectiryW will return false if directory does not exist.
    * if it returns true then test is completed and the directory path  
    * is clean for running the test again.
    */
    pTemp = convert((LPSTR)szTest2SubDirUnixSlash);
    bRc   = RemoveDirectoryW(pTemp);
    free(pTemp);

    if (! bRc)
    {
        clean=bRc;
        Trace("CreateDirectoryA: RemoveDirectoryW failed to remove "
            "\"%s\" with the error code %ld.\n",
            szTest2SubDirUnixSlash,
            GetLastError());
    }




    /*clean parent szTestSubDir */
    pTemp = convert((LPSTR)szTestSubDir);
    bRc = RemoveDirectoryW(pTemp);
    free(pTemp);

    if (! bRc)
    {
        clean = bRc;
        Trace("CreateDirectoryA: RemoveDirectoryW failed to remove "
            "\"%s\" with the error code %ld.\n",
            szTestSubDir,
            GetLastError());
    }

    /*clean parent szTestDir */
    pTemp = convert((LPSTR)szTestDir);
    bRc   = RemoveDirectoryW(pTemp);
    free(pTemp);

    if (! bRc)
    {
        clean = bRc;
        Trace("CreateDirectoryA: RemoveDirectoryW failed to remove "
            "\"%s\" with the error code %ld.\n",
            szTestDir,
            GetLastError());
    }

    if(! clean)
    {
        Fail("");
    }

    PAL_Terminate();  
    return PASS;
}
