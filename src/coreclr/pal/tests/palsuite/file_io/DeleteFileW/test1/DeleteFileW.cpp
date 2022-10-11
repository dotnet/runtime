// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  DeleteFileW.c
**
** Purpose: Tests the PAL implementation of the DeleteFileW function.
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


#include <palsuite.h>


PALTEST(file_io_DeleteFileW_test1_paltest_deletefilew_test1, "file_io/DeleteFileW/test1/paltest_deletefilew_test1")
{
    FILE *tempFile = NULL;
    BOOL bRc = FALSE;
    WCHAR* pTemp = NULL;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    //
    // deleting an existing file
    //
    tempFile = fopen("testFile01.tmp", "w");
    if (tempFile == NULL)
    {
        Fail ("DeleteFileW: ERROR: Couldn't create \"DeleteFileW's"
            " testFile01.tmp\"\n");
    }

    fprintf(tempFile, "DeleteFileW test file.\n");
    if (fclose(tempFile) != 0)
    {
        Fail ("DeleteFileA: ERROR: Couldn't close \"DeleteFileW's"
        " testFile01.tmp\"\n");
    }

    pTemp = convert("testFile01.tmp");
    bRc = DeleteFileW(pTemp);
    free(pTemp);
    if (bRc != TRUE)
    {
        Fail ("DeleteFileW: ERROR: Couldn't delete DeleteFileW's"
            " \"testFile01.tmp\"\n"
            " Error is %d\n", GetLastError());
    }


    //
    // deleting a non-existent file : should fail
    //

    pTemp = convert("testFile02.tmp");
    bRc = DeleteFileW(pTemp);
    free(pTemp);
    if (bRc != FALSE)
    {
        Fail ("DeleteFileW: ERROR: Was able to delete the non-existent"
            " file \"testFile02.tmp\"\n");
    }




    //
    // deleting an open file
    //
    tempFile = fopen("testFile03.tmp", "w");
    if (tempFile == NULL)
    {
        Fail("DeleteFileW: ERROR: Couldn't create \"DeleteFileW's"
            " testFile03.tmp\"\n");
    }

    fprintf(tempFile, "DeleteFileW test file.\n");
    if (fclose(tempFile) != 0)
    {
        Fail ("DeleteFileA: ERROR: Couldn't close \"DeleteFileW's"
        " testFile03.tmp\"\n");
    }

    pTemp = convert("testFile03.tmp");
    bRc = DeleteFileW(pTemp);
    if (bRc != TRUE)
    {
        Fail("DeleteFileW: ERROR: Couldn't delete DeleteFileW's"
            " \"testFile03.tmp\"\n"
            " Error is %d\n", GetLastError());
        free(pTemp);
    }
    bRc = DeleteFileW(pTemp);
    free(pTemp);




    //
    // delete using wild cards
    //

    // create the test file
    tempFile = fopen("testFile04.tmp", "w");
    if (tempFile == NULL)
    {
        Fail("DeleteFileW: ERROR: Couldn't create DeleteFileW's"
            " \"testFile04.tmp\"\n");
    }
    fprintf(tempFile, "DeleteFileW test file.\n");
    if (fclose(tempFile) != 0)
    {
        Fail ("DeleteFileA: ERROR: Couldn't close \"DeleteFileW's"
        " testFile04.tmp\"\n");
    }

    // delete using '?'
    pTemp = convert("testFile0?.tmp");
    bRc = DeleteFileW(pTemp);
    free(pTemp);
    if (bRc == TRUE)
    {
        Fail("DeleteFileW: ERROR: Was able to delete using the"
            " \'?\' wildcard\n");
    }

    // delete using '*'
    pTemp = convert("testFile*.tmp");
    bRc = DeleteFileW(pTemp);
    free(pTemp);
    if (bRc == TRUE)
    {
        Fail("DeleteFileW: ERROR: Was able to delete using the"
            " \'*\' wildcard\n");
    }

    pTemp = convert("testFile04.tmp");
    bRc = DeleteFileW(pTemp);
    free(pTemp);
    if (bRc != TRUE)
    {
        Fail ("DeleteFileW: ERROR: Couldn't delete DeleteFileW's"
            " \"testFile04.tmp\"\n"
            " Error is %d\n", GetLastError());
    }

    PAL_Terminate();
    return PASS;
}
