// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  CopyFileW.c
**
** Purpose: Tests the PAL implementation of the CopyFileW function
**
**
**===================================================================*/

/*
1. copy an existing file to non-existent with overwrite true
2. copy an existing file to non-existent with overwrite false
3. copy an existing file to existing with overwrite true
4. copy an existing file to existing with overwrite false
5. copy non-existent file to non-existent with overwrite true
6. copy non-existent file to non-existent with overwrite false
7. copy non-existent file to existing with overwrite true
8. copy non-existent file to existing with overwrite false
*/

#include <palsuite.h>

PALTEST(file_io_CopyFileW_test1_paltest_copyfilew_test1, "file_io/CopyFileW/test1/paltest_copyfilew_test1")
{
    LPSTR lpSource[2] = {"src_existing.tmp", "src_non-existent.tmp"};
    LPSTR lpDestination[2] = {"dst_existing.tmp", "dst_non-existent.tmp"};
    WCHAR* wcSource;
    WCHAR* wcDest;
    BOOL bFailIfExists[3] = {FALSE, TRUE};
    BOOL bRc = TRUE;
    BOOL bSuccess = TRUE;
    char results[20];
    FILE* resultsFile = NULL;
    FILE* tempFile = NULL;
    int nCounter = 0;
    int i, j, k;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* load the expected results */
    resultsFile = fopen("expectedresults.txt", "r");
    memset (results, 0, 20);
    fgets(results, 20, resultsFile);
    fclose(resultsFile);

    nCounter = 0;

    /* create the src_existing file */
    tempFile = fopen(lpSource[0], "w");
    if (tempFile != NULL)
    {
        fprintf(tempFile, "CopyFileW test file: src_existing.tmp\n");
        fclose(tempFile);
    }
    else
    {
        Fail("CopyFileW: ERROR-> Couldn't create \"src_existing.tmp\"\n");
    }

    /* create the dst_existing file */
    tempFile = fopen(lpDestination[0], "w");
    if (tempFile != NULL)
    {
        fprintf(tempFile, "CopyFileW test file: dst_existing.tmp\n");
        fclose(tempFile);
    }
    else
    {
        Fail("CopyFileW: ERROR-> Couldn't create \"dst_existing.tmp\"\n");
    }


    /* lpSource loop */
    for (i = 0; i < 2; i++)
    {
        /* lpDestination loop */
        for (j = 0; j < 2; j++)
        {
            /* bFailIfExists loop */
            for (k = 0; k < 2; k++)
            {
                wcSource = convert(lpSource[i]);
                wcDest = convert(lpDestination[j]);
                bRc = CopyFileW(wcSource,
                                wcDest,
                                bFailIfExists[k]);
                free(wcSource);
                free(wcDest);
                if (!bRc)
                {
                    if (results[nCounter] == '1')
                    {
                        Trace("CopyFileW: FAILED: test[%d][%d][%d]\n", i, j, k);
                        bSuccess = FALSE;
                    }
                }
                else
                {
                    if (results[nCounter] == '0')
                    {
                        Trace("CopyFileW: FAILED: test[%d][%d][%d]\n", i, j, k);
                        bSuccess = FALSE;
                    }
                    else
                    {
                        /* verify the file was moved */
                        if (GetFileAttributesA(lpDestination[j]) == -1)
                        {
                            Trace("CopyFileW: GetFileAttributes of destination"
                                "file failed on test[%d][%d][%d] with error "
                                "code %ld. \n",i,j,k,GetLastError());
                            bSuccess = FALSE;
                        }
                        else if (GetFileAttributesA(lpSource[i]) == -1)
                        {
                            Trace("CopyFileW: GetFileAttributes of source file "
                                "file failed on test[%d][%d][%d] with error "
                                "code %ld. \n",i,j,k,GetLastError());
                            bSuccess = FALSE;
                        }
                        else
                        {
                            /* verify attributes of destination file to
                            source file*/
                            if(GetFileAttributes(lpSource[i]) !=
                                    GetFileAttributes(lpDestination[j]))
                            {
                                Trace("CopyFileW : The file attributes of the "
                                    "destination file do not match the file "
                                    "attributes of the source file on test "
                                    "[%d][%d][%d].\n",i,j,k);
                                bSuccess = FALSE;
                            }
                        }
                    }

                }
                nCounter++;
                /* delete file file but don't worry if it fails */
                DeleteFileA(lpDestination[1]);
            }
        }
    }

    int exitCode = bSuccess ? PASS : FAIL;
    PAL_TerminateEx(exitCode);
    return exitCode;
}
