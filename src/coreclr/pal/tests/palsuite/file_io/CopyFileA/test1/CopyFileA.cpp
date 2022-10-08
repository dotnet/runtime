// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  CopyFileA.c
**
** Purpose: Tests the PAL implementation of the CopyFileA function
**
**
**===================================================================*/

/*
	1. copy an existing file to existing with overwrite true
	2. copy an existing file to existing with overwrite false
	3. copy an existing file to non-existent with overwrite true
	4. copy an existing file to non-existent with overwrite false
	5. copy non-existent file to existing with overwrite true
	6. copy non-existent file to existing with overwrite false
	7. copy non-existent file to non-existent with overwrite true
	8. copy non-existent file to non-existent with overwrite false
*/

#include <palsuite.h>

struct TESTS{
    char* lpSource;
    char* lpDestination;
    BOOL bFailIfExists;
    int nResult;
    };


PALTEST(file_io_CopyFileA_test1_paltest_copyfilea_test1, "file_io/CopyFileA/test1/paltest_copyfilea_test1")
{
    char szSrcExisting[] =     {"src_existing.tmp"};
    char szSrcNonExistent[] =  {"src_non-existent.tmp"};
    char szDstExisting[] =     {"dst_existing.tmp"};
    char szDstNonExistent[] =  {"dst_non-existent.tmp"};
    BOOL bRc = TRUE;
    BOOL bSuccess = TRUE;
    FILE* tempFile = NULL;
    int i;
    struct TESTS testCase[] =
    {
        {szSrcExisting, szDstExisting, FALSE, 1},
        {szSrcExisting, szDstExisting, TRUE, 0},
        {szSrcExisting, szDstNonExistent, FALSE, 1},
        {szSrcExisting, szDstNonExistent, TRUE, 1},
        {szSrcNonExistent, szDstExisting, FALSE, 0},
        {szSrcNonExistent, szDstExisting, TRUE, 0},
        {szSrcNonExistent, szDstNonExistent, FALSE, 0},
        {szSrcNonExistent, szDstNonExistent, TRUE, 0}
    };


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* create the src_existing file */
    tempFile = fopen(szSrcExisting, "w");
    if (tempFile != NULL)
    {
        fprintf(tempFile, "CopyFileA test file: src_existing.tmp\n");
        fclose(tempFile);
    }
    else
    {
        Fail("CopyFileA: ERROR-> Couldn't create \"src_existing.tmp\" with "
            "error %ld\n",
            GetLastError());
    }

    /* create the dst_existing file */
    tempFile = fopen(szDstExisting, "w");
    if (tempFile != NULL)
    {
        fprintf(tempFile, "CopyFileA test file: dst_existing.tmp\n");
        fclose(tempFile);
    }
    else
    {
        Fail("CopyFileA: ERROR-> Couldn't create \"dst_existing.tmp\" with "
            "error %ld\n",
            GetLastError());
    }



    for (i = 0; i < (sizeof(testCase) / sizeof(struct TESTS)); i++)
    {
        bRc = CopyFileA(testCase[i].lpSource,
                        testCase[i].lpDestination,
                        testCase[i].bFailIfExists);
        if (!bRc)
        {
            if (testCase[i].nResult == 1)
            {
                Trace("CopyFileA: FAILED: %s -> %s with bFailIfExists = %d "
                    "with error %ld\n",
                    testCase[i].lpSource,
                    testCase[i].lpDestination,
                    testCase[i].bFailIfExists,
                    GetLastError());
                bSuccess = FALSE;
            }
        }
        else
        {
            if (testCase[i].nResult == 0)
            {
                Trace("CopyFileA: FAILED: %s -> %s with bFailIfExists = %d\n",
                    testCase[i].lpSource,
                    testCase[i].lpDestination,
                    testCase[i].bFailIfExists);
                bSuccess = FALSE;
            }
            else
            {
                /* verify the file was moved */
                if (GetFileAttributesA(testCase[i].lpDestination) == -1)
                {
                    Trace("CopyFileA: GetFileAttributes of destination file "
                        "failed with error code %ld. \n",
                        GetLastError());
                    bSuccess = FALSE;
                }
                else if (GetFileAttributesA(testCase[i].lpSource) == -1)
                {
                    Trace("CopyFileA: GetFileAttributes of source file "
                        "failed with error code %ld. \n",
                        GetLastError());
                    bSuccess = FALSE;
                }
                else
                {
                    /* verify attributes of destination file to source file*/
                    if(GetFileAttributes(testCase[i].lpSource) !=
                            GetFileAttributes(testCase[i].lpDestination))
                    {
                        Trace("CopyFileA : The file attributes of the "
                            "destination file do not match the file "
                            "attributes of the source file.\n");
                        bSuccess = FALSE;
                    }
                }
            }
        }
        /* delete file file but don't worry if it fails */
        DeleteFileA(szDstNonExistent);
    }

    int exitCode = bSuccess ? PASS : FAIL;
    PAL_TerminateEx(exitCode);
    return exitCode;
}
