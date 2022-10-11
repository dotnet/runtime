// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  MoveFileExW.c
**
** Purpose: Tests the PAL implementation of the MoveFileExW function.
**
**
**===================================================================*/

#include <palsuite.h>


LPWSTR lpSource_MoveFileExW_test1[4];
LPWSTR lpDestination_MoveFileExW_test1[4];
LPWSTR lpFiles_MoveFileExW_test1[14];

DWORD dwFlag_MoveFileExW_test1[2] = {MOVEFILE_COPY_ALLOWED, MOVEFILE_REPLACE_EXISTING};



int createExisting_MoveFileExW_test1(void)
{
    HANDLE tempFile  = NULL;
    HANDLE tempFile2 = NULL;

    /* create the src_existing file and dst_existing file */
    tempFile = CreateFileW(lpSource_MoveFileExW_test1[0], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);
    tempFile2 = CreateFileW(lpDestination_MoveFileExW_test1[0], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);
    CloseHandle(tempFile2);
    CloseHandle(tempFile);

    if ((tempFile == NULL) || (tempFile2 == NULL))
    {
        Trace("ERROR: couldn't create %S or %S\n", lpSource_MoveFileExW_test1[0],
                lpDestination_MoveFileExW_test1[0]);
        return FAIL;
    }

    /* create the src_dir_existing and dst_dir_existing directory and files */
    CreateDirectoryW(lpSource_MoveFileExW_test1[2], NULL);

    tempFile = CreateFileW(lpFiles_MoveFileExW_test1[0], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);
    tempFile2 = CreateFileW(lpFiles_MoveFileExW_test1[1], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);
    CloseHandle(tempFile2);
    CloseHandle(tempFile);

    if ((tempFile == NULL) || (tempFile2 == NULL))
    {
        Trace("ERROR: couldn't create src_dir_existing\\test01.tmp\n");
        return FAIL;
    }

    CreateDirectoryW(lpDestination_MoveFileExW_test1[2], NULL);
    tempFile = CreateFileW(lpFiles_MoveFileExW_test1[2], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);
    tempFile2 = CreateFileW(lpFiles_MoveFileExW_test1[3], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);
    CloseHandle(tempFile2);
    CloseHandle(tempFile);

    if ((tempFile == NULL) || (tempFile2 == NULL))
    {
        Trace("ERROR: couldn't create dst_dir_existing\\test01.tmp\n");
        return FAIL;
    }
    return PASS;
}

void removeDirectoryHelper_MoveFileExW_test1(LPWSTR dir, int location)
{
    DWORD dwAtt = GetFileAttributesW(dir);
//    Trace(" Value of location[%d], and directorye [%S]\n", location, dir);

    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        if(!RemoveDirectoryW(dir))
        {
            Fail("ERROR: Failed to remove Directory [%S], Error Code [%d], location [%d]\n", dir, GetLastError(), location);
        }
    }
}

void removeFileHelper_MoveFileExW_test1(LPWSTR wfile, int location)
{
    FILE *fp;
    char * pfile = convertC(wfile);

//    Trace(" Value of location[%d], and file [%s]\n", location, pfile);
    fp = fopen( pfile, "r");

    if (fp != NULL)
    {
        if(fclose(fp))
        {
          Fail("ERROR: Failed to close the file [%S], Error Code [%d], location [%d]\n", wfile, GetLastError(), location);
        }

        if(!DeleteFileW(wfile))
        {
            Fail("ERROR: Failed to delete file [%S], Error Code [%d], location [%d]\n", wfile, GetLastError(), location);
        }
        else
        {
    //       Trace("Success: deleted file [%S], Error Code [%d], location [%d]\n", wfile, GetLastError(), location);
        }
    }

    free(pfile);
}

void removeAll_MoveFileExW_test1(void)
{
    DWORD dwAtt;
    /* get rid of destination dirs and files */
    removeFileHelper_MoveFileExW_test1(lpSource_MoveFileExW_test1[0], 11);
//    lpSource_MoveFileExW_test1[0] = convert("src_existing.tmp");

    removeFileHelper_MoveFileExW_test1(lpSource_MoveFileExW_test1[1], 12);
  //lpSource_MoveFileExW_test1[1] = convert("src_non-existent.tmp");

    removeFileHelper_MoveFileExW_test1(lpFiles_MoveFileExW_test1[0], 13);
//    lpFiles_MoveFileExW_test1[0] = convert("src_dir_existing\\test01.tmp");

    removeFileHelper_MoveFileExW_test1(lpFiles_MoveFileExW_test1[1], 14);
//    lpFiles_MoveFileExW_test1[1] = convert("src_dir_existing\\test02.tmp");

    removeDirectoryHelper_MoveFileExW_test1(lpSource_MoveFileExW_test1[2], 101);
//    lpSource_MoveFileExW_test1[2] = convert("src_dir_existing");

    removeFileHelper_MoveFileExW_test1(lpFiles_MoveFileExW_test1[4], 15);
//    lpFiles_MoveFileExW_test1[4] = convert("src_dir_non-existent\\test01.tmp");

    removeFileHelper_MoveFileExW_test1(lpFiles_MoveFileExW_test1[5], 16);
//    lpFiles_MoveFileExW_test1[5] = convert("src_dir_non-existent\\test02.tmp");

    removeDirectoryHelper_MoveFileExW_test1(lpSource_MoveFileExW_test1[3], 102);
//    lpSource_MoveFileExW_test1[3] = convert("src_dir_non-existent");

    /* get rid of destination dirs and files */
    dwAtt = GetFileAttributesW(lpDestination_MoveFileExW_test1[0]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        removeFileHelper_MoveFileExW_test1(lpFiles_MoveFileExW_test1[6], 18);
    //    lpFiles_MoveFileExW_test1[6] = convert("dst_existing.tmp\\test01.tmp");
        removeFileHelper_MoveFileExW_test1(lpFiles_MoveFileExW_test1[7], 19);
    //    lpFiles_MoveFileExW_test1[7] = convert("dst_existing.tmp\\test02.tmp");
        removeDirectoryHelper_MoveFileExW_test1(lpDestination_MoveFileExW_test1[0], 103);
    //    lpDestination_MoveFileExW_test1[0] = convert("dst_existing.tmp");

    }
    else
    {
        removeFileHelper_MoveFileExW_test1(lpDestination_MoveFileExW_test1[0], 17);
    //    lpDestination_MoveFileExW_test1[0] = convert("dst_existing.tmp");
    }

    dwAtt = GetFileAttributesW(lpDestination_MoveFileExW_test1[1]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        removeFileHelper_MoveFileExW_test1(lpFiles_MoveFileExW_test1[8], 21);
    //    lpFiles_MoveFileExW_test1[8] = convert("dst_non-existent.tmp\\test01.tmp");
        removeFileHelper_MoveFileExW_test1(lpFiles_MoveFileExW_test1[9], 22);
    //    lpFiles_MoveFileExW_test1[9] = convert("dst_non-existent.tmp\\test02.tmp");
        removeDirectoryHelper_MoveFileExW_test1(lpDestination_MoveFileExW_test1[1], 104);
    //    lpDestination_MoveFileExW_test1[1] = convert("dst_non-existent.tmp");

    }
    else
    {
        removeFileHelper_MoveFileExW_test1(lpDestination_MoveFileExW_test1[1], 19);
            //lpDestination_MoveFileExW_test1[1] = convert("dst_non-existent.tmp");
    }

    dwAtt = GetFileAttributesW(lpDestination_MoveFileExW_test1[2]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        removeFileHelper_MoveFileExW_test1(lpFiles_MoveFileExW_test1[10], 24);
    //    lpFiles_MoveFileExW_test1[10] = convert("dst_dir_existing\\test01.tmp");
        removeFileHelper_MoveFileExW_test1(lpFiles_MoveFileExW_test1[11], 25);
    //    lpFiles_MoveFileExW_test1[11] = convert("dst_dir_existing\\test02.tmp");
        removeDirectoryHelper_MoveFileExW_test1(lpDestination_MoveFileExW_test1[2], 105);
    //    lpDestination_MoveFileExW_test1[2] = convert("dst_dir_existing");

    }
    else
    {
        removeFileHelper_MoveFileExW_test1(lpDestination_MoveFileExW_test1[2], 23);
    //    lpDestination_MoveFileExW_test1[2] = convert("dst_dir_existing");

    }

    dwAtt = GetFileAttributesW(lpDestination_MoveFileExW_test1[3]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        removeFileHelper_MoveFileExW_test1(lpFiles_MoveFileExW_test1[12], 26);
    //    lpFiles_MoveFileExW_test1[12] = convert("dst_dir_non-existent\\test01.tmp");
        removeFileHelper_MoveFileExW_test1(lpFiles_MoveFileExW_test1[13], 27);
    //    lpFiles_MoveFileExW_test1[13] = convert("dst_dir_non-existent\\test02.tmp");
        removeDirectoryHelper_MoveFileExW_test1(lpDestination_MoveFileExW_test1[3], 106);
    //    lpDestination_MoveFileExW_test1[3] = convert("dst_dir_non-existent");

    }
    else
    {
        removeFileHelper_MoveFileExW_test1(lpDestination_MoveFileExW_test1[3], 107);
    //    lpDestination_MoveFileExW_test1[3] = convert("dst_dir_non-existent");

    }

}

PALTEST(file_io_MoveFileExW_test1_paltest_movefileexw_test1, "file_io/MoveFileExW/test1/paltest_movefileexw_test1")
{
    BOOL bRc = TRUE;
    char results[40];
    FILE* resultsFile = NULL;
    int i, j, k, nCounter = 0;
    int res = FAIL;
    WCHAR tempSource[] = {'t','e','m','p','k','.','t','m','p','\0'};
    WCHAR tempDest[] = {'t','e','m','p','2','.','t','m','p','\0'};
    HANDLE hFile;
    DWORD result;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    lpSource_MoveFileExW_test1[0] = convert("src_existing.tmp");
    lpSource_MoveFileExW_test1[1] = convert("src_non-existent.tmp");
    lpSource_MoveFileExW_test1[2] = convert("src_dir_existing");
    lpSource_MoveFileExW_test1[3] = convert("src_dir_non-existent");

    lpDestination_MoveFileExW_test1[0] = convert("dst_existing.tmp");
    lpDestination_MoveFileExW_test1[1] = convert("dst_non-existent.tmp");
    lpDestination_MoveFileExW_test1[2] = convert("dst_dir_existing");
    lpDestination_MoveFileExW_test1[3] = convert("dst_dir_non-existent");

    lpFiles_MoveFileExW_test1[0] = convert("src_dir_existing\\test01.tmp");
    lpFiles_MoveFileExW_test1[1] = convert("src_dir_existing\\test02.tmp");
    lpFiles_MoveFileExW_test1[2] = convert("dst_dir_existing\\test01.tmp");
    lpFiles_MoveFileExW_test1[3] = convert("dst_dir_existing\\test02.tmp");
    lpFiles_MoveFileExW_test1[4] = convert("src_dir_non-existent\\test01.tmp");
    lpFiles_MoveFileExW_test1[5] = convert("src_dir_non-existent\\test02.tmp");

    lpFiles_MoveFileExW_test1[6] = convert("dst_existing.tmp\\test01.tmp");
    lpFiles_MoveFileExW_test1[7] = convert("dst_existing.tmp\\test02.tmp");

    lpFiles_MoveFileExW_test1[8] = convert("dst_non-existent.tmp\\test01.tmp");
    lpFiles_MoveFileExW_test1[9] = convert("dst_non-existent.tmp\\test02.tmp");

    lpFiles_MoveFileExW_test1[10] = convert("dst_dir_existing\\test01.tmp");
    lpFiles_MoveFileExW_test1[11] = convert("dst_dir_existing\\test02.tmp");

    lpFiles_MoveFileExW_test1[12] = convert("dst_dir_non-existent\\test01.tmp");
    lpFiles_MoveFileExW_test1[13] = convert("dst_dir_non-existent\\test02.tmp");

    /* read in the expected results to compare with actual results */
    memset (results, 0, 34);
    resultsFile = fopen("expectedresults.txt", "r");
    if (resultsFile == NULL)
    {
        Trace("MoveFileExW ERROR: Unable to open \"expectedresults.txt\"\n");
        goto EXIT;
    }

    fgets(results, 34, resultsFile);
    fclose(resultsFile);

//    Trace("Value of results[%]=%s\n", i, results);
    for( i = 0; i < 32; i++)
    {
        Trace("Value of results[%d]=%c\n", i, results[i]);
    }
    nCounter = 0;


    /* clean the slate */
    removeAll_MoveFileExW_test1();
    if (createExisting_MoveFileExW_test1() != PASS)
    {
        goto EXIT;
    }

    /* lpSource_MoveFileExW_test1 loop */
    for (i = 0; i < 4; i++)
    {
        /* lpDestination_MoveFileExW_test1 loop */
        for (j = 0; j < 4; j++)
        {
            /* dwFlag_MoveFileExW_test1 loop */
            for (k = 0; k < 2; k++)
            {

                //if(nCounter == 22)
                //{
                //exit(1);
                //}
                /* move the file to the new location */
                bRc = MoveFileExW(lpSource_MoveFileExW_test1[i], lpDestination_MoveFileExW_test1[j], dwFlag_MoveFileExW_test1[k]);

                if (!(
                    ((bRc == TRUE) && (results[nCounter] == '1'))
                    ||
                    ((bRc == FALSE ) && (results[nCounter] == '0'))                    )
                    )
                {
                    Trace("MoveFileExW(%S, %S, %s): Values of i[%d], j[%d], k [%d] and results[%d]=%c LastError[%d]Flag[%d]FAILED\n",
                        lpSource_MoveFileExW_test1[i], lpDestination_MoveFileExW_test1[j],
                        k == 1 ?
                        "MOVEFILE_REPLACE_EXISTING":"MOVEFILE_COPY_ALLOWED", i, j, k, nCounter, results[nCounter], GetLastError(), bRc);
                    goto EXIT;
                }

                //Trace("MoveFileExW(%S, %S, %s): Values of i[%d], j[%d], k [%d] and results[%d]=%c \n",
                //        lpSource_MoveFileExW_test1[i], lpDestination_MoveFileExW_test1[j],
                //        k == 1 ?
                //        "MOVEFILE_REPLACE_EXISTING":"MOVEFILE_COPY_ALLOWED", i, j, k, nCounter, results[nCounter]);


                /* undo the last move */
                removeAll_MoveFileExW_test1();
                if (createExisting_MoveFileExW_test1() != PASS)
                {
                    goto EXIT;
                }
                //Trace("Counter [%d] over \n", nCounter);
                nCounter++;
            }
        }
    }

    /* create the temp source file */
    hFile = CreateFileW(tempSource, GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);

    if( hFile == INVALID_HANDLE_VALUE )
    {
        Trace("MoveFileExW: CreateFile failed to "
            "create the file correctly.\n");
        goto EXIT;
    }

    bRc = CloseHandle(hFile);
    if(!bRc)
    {
        Trace("MoveFileExW: CloseHandle failed to close the "
            "handle correctly. yo %u\n",GetLastError());
        goto EXIT;
    }

    /* set the file attributes to be readonly */
    bRc = SetFileAttributesW(tempSource, FILE_ATTRIBUTE_READONLY);
    if(!bRc)
    {
        Trace("MoveFileExW: SetFileAttributes failed to set file "
            "attributes correctly. ERROR:%u\n",GetLastError());
        goto EXIT;
    }

    /* move the file to the new location */
    bRc = MoveFileExW(tempSource, tempDest, MOVEFILE_COPY_ALLOWED );
    if(!bRc)
    {
        Trace("MoveFileExW(%S, %S, %s): GetFileAttributes "
            "failed to get the file's attributes.\n",
            tempSource, tempDest, "MOVEFILE_COPY_ALLOWED");
        goto EXIT;
    }

    /* check that the newly moved file has the same file attributes
    as the original */
    result = GetFileAttributesW(tempDest);
    if(result == 0)
    {
        Trace("MoveFileExW: GetFileAttributes failed to get "
            "the file's attributes.\n");
        goto EXIT;
    }

    if((result & FILE_ATTRIBUTE_READONLY) != FILE_ATTRIBUTE_READONLY)
    {
        Trace("MoveFileExW: GetFileAttributes failed to get "
            "the correct file attributes.\n");
        goto EXIT;
    }

    /* set the file attributes back to normal, to be deleted */
    bRc = SetFileAttributesW(tempDest, FILE_ATTRIBUTE_NORMAL);
    if(!bRc)
    {
        Trace("MoveFileExW: SetFileAttributes "
            "failed to set file attributes correctly.\n");
        goto EXIT;
    }

    /* delete the newly moved file */
    bRc = DeleteFileW(tempDest);
    if(!bRc)
    {
        Trace("MoveFileExW: DeleteFileW failed to delete the"
            "file correctly.\n");
        goto EXIT;
    }

    res = PASS;

EXIT:
    removeAll_MoveFileExW_test1();
    for (i=0; i<4; i++)
    {
        free(lpSource_MoveFileExW_test1[i]);
        free(lpDestination_MoveFileExW_test1[i]);
    }
    for (i=0; i<14; i++)
    {
        free(lpFiles_MoveFileExW_test1[i]);
    }

    PAL_TerminateEx(res);
    return res;
}

