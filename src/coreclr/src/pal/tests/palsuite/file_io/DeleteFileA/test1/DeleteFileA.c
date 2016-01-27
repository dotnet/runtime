// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  DeleteFileA.c
**
** Purpose: Tests the PAL implementation of the DeleteFileA function.
**
**
**===================================================================*/

//	delete an existing file
//	delete a non-existant file
//  delete an open file
//	delete files using wild cards
//	delete a hidden file
//  delete a file without proper permissions
//

#include <palsuite.h>



int __cdecl main(int argc, char *argv[])
{
    FILE *tempFile = NULL;
    BOOL bRc = FALSE;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    //
    // deleting an existing file
    //
    tempFile = fopen("testFile01.txt", "w");
    if (tempFile == NULL)
    {
        Fail ("DeleteFileA: ERROR: Couldn't create \"DeleteFileA's"
            " testFile.txt\"\n");
    }

    fprintf(tempFile, "DeleteFileA test file.\n");
    fclose(tempFile);

    bRc = DeleteFileA("testFile01.txt");
    if (bRc != TRUE)
    {
        Fail ("DeleteFileA: ERROR: Couldn't delete DeleteFileA's"
            " \"testFile01.txt\"\n");
    }


    //
    // deleting a non-existant file : should fail
    //

    bRc = DeleteFileA("testFile02.txt");
    if (bRc != FALSE)
    {
        Fail ("DeleteFileA: ERROR: Was able to delete the non-existant"
            " file \"testFile02.txt\"\n");
    }




    //
    // deleting an open file 
    //
    tempFile = fopen("testFile03.txt", "w");
    if (tempFile == NULL)
    {
        Fail("DeleteFileA: ERROR: Couldn't create \"DeleteFileA's"
            " testFile.txt\"\n");
    }

    fprintf(tempFile, "DeleteFileA test file.\n");
    fclose(tempFile);

    bRc = DeleteFileA("testFile03.txt");
    if (bRc != TRUE)
    {
        Fail("DeleteFileA: ERROR: Couldn't delete DeleteFileA's"
            " \"testFile01.txt\"\n");
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
    fclose(tempFile);

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
            " \"testFile04.txt\"\n");
    }

    PAL_Terminate();  
    return PASS;
}
