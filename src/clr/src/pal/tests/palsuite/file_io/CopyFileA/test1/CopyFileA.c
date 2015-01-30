//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
	3. copy an existing file to non-existant with overwrite true
	4. copy an existing file to non-existant with overwrite false
	5. copy non-existant file to existing with overwrite true
	6. copy non-existant file to existing with overwrite false
	7. copy non-existant file to non-existant with overwrite true
	8. copy non-existant file to non-existant with overwrite false
*/

#include <palsuite.h>

struct TESTS{
    char* lpSource;
    char* lpDestination;
    BOOL bFailIfExists;
    int nResult;
    };


int __cdecl main(int argc, char *argv[])
{
    char szSrcExisting[] =     {"src_existing.tmp"};
    char szSrcNonExistant[] =  {"src_non-existant.tmp"};
    char szDstExisting[] =     {"dst_existing.tmp"};
    char szDstNonExistant[] =  {"dst_non-existant.tmp"};
    BOOL bRc = TRUE;
    BOOL bSuccess = TRUE;
    FILE* tempFile = NULL;
    int i;
    struct TESTS testCase[] =
    {
        {szSrcExisting, szDstExisting, FALSE, 1},
        {szSrcExisting, szDstExisting, TRUE, 0},
        {szSrcExisting, szDstNonExistant, FALSE, 1},
        {szSrcExisting, szDstNonExistant, TRUE, 1},
        {szSrcNonExistant, szDstExisting, FALSE, 0},
        {szSrcNonExistant, szDstExisting, TRUE, 0},
        {szSrcNonExistant, szDstNonExistant, FALSE, 0},
        {szSrcNonExistant, szDstNonExistant, TRUE, 0}
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
        DeleteFileA(szDstNonExistant);
    }

    int exitCode = bSuccess ? PASS : FAIL;
    PAL_TerminateEx(exitCode);
    return exitCode;
}
