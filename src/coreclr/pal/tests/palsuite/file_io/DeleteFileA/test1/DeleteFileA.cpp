// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  DeleteFileA.c
**
** Purpose: Tests the PAL implementation of the DeleteFileA function.
**
**
**===================================================================*/

//	delete an existing file
//	delete a non-existent file
//  delete an open file
//	delete files using wild cards
//	delete a hidden file
//  delete a file without proper permissions
//

#define PAL_STDCPP_COMPAT
#include <palsuite.h>
#undef PAL_STDCPP_COMPAT

#include <unistd.h>
#include <sys/stat.h>


PALTEST(file_io_DeleteFileA_test1_paltest_deletefilea_test1, "file_io/DeleteFileA/test1/paltest_deletefilea_test1")
{
    FILE *tempFile = NULL;
    BOOL bRc = FALSE;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    //
    // create a test file
    //
    tempFile = fopen("testFile01.txt", "w");
    if (tempFile == NULL)
    {
        Fail ("DeleteFileA: ERROR: Couldn't create \"DeleteFileA's"
            " testFile01.txt\"\n");
    }

    fprintf(tempFile, "DeleteFileA test file.\n");
    if (fclose(tempFile) != 0)
    {
        Fail ("DeleteFileA: ERROR: Couldn't close \"DeleteFileA's"
            " testFile01.txt\"\n");
    }

    //
    // delete a symlink to an existing file
    //
    if (symlink("testFile01.txt", "testFile01_symlink") != 0)
    {
        Fail("DeleteFileA: ERROR: Failed to create a symlink to testFile01.txt.\n");
    }

    bRc = DeleteFileA("testFile01_symlink");
    if (bRc != TRUE)
    {
        Fail ("DeleteFileA: ERROR: Couldn't delete symlink!\n Error is %d\n", GetLastError());
    }

    struct stat statBuffer;
    if (lstat("testFile01.txt", &statBuffer) != 0)
    {
        Fail("DeleteFileA: ERROR: Deleting a symlink deleted the file it was pointing to.\n");
    }

    if (lstat("testFile01_symlink", &statBuffer) == 0)
    {
        Fail("DeleteFileA: ERROR: Failed to delete a symlink.\n");
    }

    //
    // deleting an existing file
    //
    bRc = DeleteFileA("testFile01.txt");
    if (bRc != TRUE)
    {
        Fail ("DeleteFileA: ERROR: Couldn't delete DeleteFileA's"
            " \"testFile01.txt\"\n"
            " Error is %d\n", GetLastError());
    }


    //
    // deleting a non-existent file : should fail
    //

    bRc = DeleteFileA("testFile02.txt");
    if (bRc != FALSE)
    {
        Fail ("DeleteFileA: ERROR: Was able to delete the non-existent"
            " file \"testFile02.txt\"\n");
    }




    //
    // deleting an open file
    //
    tempFile = fopen("testFile03.txt", "w");
    if (tempFile == NULL)
    {
        Fail("DeleteFileA: ERROR: Couldn't create \"DeleteFileA's"
            " testFile03.txt\"\n");
    }

    fprintf(tempFile, "DeleteFileA test file.\n");
    if (fclose(tempFile) != 0)
    {
        Fail ("DeleteFileA: ERROR: Couldn't close \"DeleteFileA's"
        " testFile03.txt\"\n");
    }

    bRc = DeleteFileA("testFile03.txt");
    if (bRc != TRUE)
    {
        Fail("DeleteFileA: ERROR: Couldn't delete DeleteFileA's"
            " \"testFile03.txt\"\n"
            " Error is %d\n", GetLastError());
    }
    bRc = DeleteFileA("testFile03.txt");




    //
    // delete using wild cards
    //

    // create the test file
    tempFile = fopen("testFile04.txt", "w");
    if (tempFile == NULL)
    {
        Fail("DeleteFileA: ERROR: Couldn't create DeleteFileA's"
            " \"testFile04.txt\"\n");
    }
    fprintf(tempFile, "DeleteFileA test file.\n");
    if (fclose(tempFile) != 0)
    {
        Fail ("DeleteFileA: ERROR: Couldn't close \"DeleteFileA's"
        " testFile04.txt\"\n");
    }

    // delete using '?'
    bRc = DeleteFileA("testFile0?.txt");
    if (bRc == TRUE)
    {
        Fail("DeleteFileA: ERROR: Was able to delete using the"
            " \'?\' wildcard\n");
    }

    // delete using '*'
    bRc = DeleteFileA("testFile*.txt");
    if (bRc == TRUE)
    {
        Fail("DeleteFileA: ERROR: Was able to delete using the"
            " \'*\' wildcard\n");
    }

    bRc = DeleteFileA("testFile04.txt");
    if (bRc != TRUE)
    {
        Fail ("DeleteFileA: ERROR: Couldn't delete DeleteFileA's"
            " \"testFile04.txt\"\n"
            " Error is %d\n", GetLastError());
    }

    PAL_Terminate();
    return PASS;
}
