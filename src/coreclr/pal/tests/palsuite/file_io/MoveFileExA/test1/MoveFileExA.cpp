// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  MoveFileExA.c
**
** Purpose: Tests the PAL implementation of the MoveFileExA function.
**
**
**===================================================================*/

#include <palsuite.h>


LPSTR lpSource_MoveFileExA_test1[4] = {
						"src_existing.tmp",
						"src_non-existent.tmp",
						"src_dir_existing",
						"src_dir_non-existent"
					};
LPSTR lpDestination_MoveFileExA_test1[4]={
						"dst_existing.tmp",
						"dst_non-existent.tmp",
						"dst_dir_existing",
						"dst_dir_non-existent"
						};

LPSTR lpFiles_MoveFileExA_test1[14] ={
						"src_dir_existing\\test01.tmp",
						"src_dir_existing\\test02.tmp",
						"dst_dir_existing\\test01.tmp",
						"dst_dir_existing\\test02.tmp",
						"src_dir_non-existent\\test01.tmp",
						"src_dir_non-existent\\test02.tmp",
						"dst_existing.tmp\\test01.tmp",
						"dst_existing.tmp\\test02.tmp",
						"dst_non-existent.tmp\\test01.tmp",
						"dst_non-existent.tmp\\test02.tmp",
						"dst_dir_existing\\test01.tmp",
						"dst_dir_existing\\test02.tmp",
						"dst_dir_non-existent\\test01.tmp",
						"dst_dir_non-existent\\test02.tmp"
						};

DWORD dwFlag_MoveFileExA_test1[2] = {MOVEFILE_COPY_ALLOWED, MOVEFILE_REPLACE_EXISTING};




int createExisting_MoveFileExA_test1(void)
{
    HANDLE tempFile  = NULL;
    HANDLE tempFile2 = NULL;

    /* create the src_existing file and dst_existing file */
    tempFile = CreateFileA(lpSource_MoveFileExA_test1[0], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);
    tempFile2 = CreateFileA(lpDestination_MoveFileExA_test1[0], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);
    CloseHandle(tempFile2);
    CloseHandle(tempFile);

    if ((tempFile == NULL) || (tempFile2 == NULL))
    {
        Trace("ERROR[%ul]: couldn't create %S or %S\n", GetLastError(), lpSource_MoveFileExA_test1[0],
                lpDestination_MoveFileExA_test1[0]);
        return FAIL;
    }

    /* create the src_dir_existing and dst_dir_existing directory and files */
    CreateDirectoryA(lpSource_MoveFileExA_test1[2], NULL);

    tempFile = CreateFileA(lpFiles_MoveFileExA_test1[0], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);
    tempFile2 = CreateFileA(lpFiles_MoveFileExA_test1[1], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);
    CloseHandle(tempFile2);
    CloseHandle(tempFile);

    if ((tempFile == NULL) || (tempFile2 == NULL))
    {
        Trace("ERROR[%ul]: couldn't create src_dir_existing\\test01.tmp\n", GetLastError());
        return FAIL;
    }

    CreateDirectoryA(lpDestination_MoveFileExA_test1[2], NULL);
    tempFile = CreateFileA(lpFiles_MoveFileExA_test1[2], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);
    tempFile2 = CreateFileA(lpFiles_MoveFileExA_test1[3], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);
    CloseHandle(tempFile2);
    CloseHandle(tempFile);

    if ((tempFile == NULL) || (tempFile2 == NULL))
    {
        Trace("ERROR[%ul]: couldn't create dst_dir_existing\\test01.tmp\n" , GetLastError());
        return FAIL;
    }
    return PASS;

}

void removeDirectoryHelper_MoveFileExA_test1(LPSTR dir, int location)
{
    DWORD dwAtt = GetFileAttributesA(dir);

    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        LPWSTR dirW = convert(dir);
        if(!RemoveDirectoryW(dirW))
        {
            DWORD dwError = GetLastError();
            free(dirW);
            Fail("ERROR: Failed to remove Directory [%s], Error Code [%d], location [%d]\n", dir, dwError, location);
        }

        free(dirW);
    }
}

void removeFileHelper_MoveFileExA_test1(LPSTR pfile, int location)
{
    FILE *fp;
    fp = fopen( pfile, "r");

    if (fp != NULL)
    {
        if(fclose(fp))
        {
          Fail("ERROR: Failed to close the file [%s], Error Code [%d], location [%d]\n", pfile, GetLastError(), location);
        }

        if(!DeleteFileA(pfile))
        {
            Fail("ERROR: Failed to delete file [%s], Error Code [%d], location [%d]\n", pfile, GetLastError(), location);
        }
    }

}

void removeAll_MoveFileExA_test1(void)
{
    DWORD dwAtt;
    /* get rid of destination dirs and files */
    removeFileHelper_MoveFileExA_test1(lpSource_MoveFileExA_test1[0], 11);
    removeFileHelper_MoveFileExA_test1(lpSource_MoveFileExA_test1[1], 12);
    removeFileHelper_MoveFileExA_test1(lpFiles_MoveFileExA_test1[0], 13);
    removeFileHelper_MoveFileExA_test1(lpFiles_MoveFileExA_test1[1], 14);

    removeDirectoryHelper_MoveFileExA_test1(lpSource_MoveFileExA_test1[2], 101);
    removeFileHelper_MoveFileExA_test1(lpFiles_MoveFileExA_test1[4], 15);
    removeFileHelper_MoveFileExA_test1(lpFiles_MoveFileExA_test1[5], 16);
    removeDirectoryHelper_MoveFileExA_test1(lpSource_MoveFileExA_test1[3], 102);

    /* get rid of destination dirs and files */
    dwAtt = GetFileAttributesA(lpDestination_MoveFileExA_test1[0]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        removeFileHelper_MoveFileExA_test1(lpFiles_MoveFileExA_test1[6], 18);
        removeFileHelper_MoveFileExA_test1(lpFiles_MoveFileExA_test1[7], 19);
        removeDirectoryHelper_MoveFileExA_test1(lpDestination_MoveFileExA_test1[0], 103);
    }
    else
    {
        removeFileHelper_MoveFileExA_test1(lpDestination_MoveFileExA_test1[0], 17);
    }

    dwAtt = GetFileAttributesA(lpDestination_MoveFileExA_test1[1]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        removeFileHelper_MoveFileExA_test1(lpFiles_MoveFileExA_test1[8], 21);
        removeFileHelper_MoveFileExA_test1(lpFiles_MoveFileExA_test1[9], 22);
        removeDirectoryHelper_MoveFileExA_test1(lpDestination_MoveFileExA_test1[1], 104);
    }
    else
    {
        removeFileHelper_MoveFileExA_test1(lpDestination_MoveFileExA_test1[1], 19);
    }

    dwAtt = GetFileAttributesA(lpDestination_MoveFileExA_test1[2]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        removeFileHelper_MoveFileExA_test1(lpFiles_MoveFileExA_test1[10], 24);
        removeFileHelper_MoveFileExA_test1(lpFiles_MoveFileExA_test1[11], 25);
        removeDirectoryHelper_MoveFileExA_test1(lpDestination_MoveFileExA_test1[2], 105);
    }
    else
    {
        removeFileHelper_MoveFileExA_test1(lpDestination_MoveFileExA_test1[2], 23);
    }

    dwAtt = GetFileAttributesA(lpDestination_MoveFileExA_test1[3]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        removeFileHelper_MoveFileExA_test1(lpFiles_MoveFileExA_test1[12], 26);
        removeFileHelper_MoveFileExA_test1(lpFiles_MoveFileExA_test1[13], 27);
        removeDirectoryHelper_MoveFileExA_test1(lpDestination_MoveFileExA_test1[3], 106);
    }
    else
    {
        removeFileHelper_MoveFileExA_test1(lpDestination_MoveFileExA_test1[3], 107);
    }

}

PALTEST(file_io_MoveFileExA_test1_paltest_movefileexa_test1, "file_io/MoveFileExA/test1/paltest_movefileexa_test1")
{
    BOOL bRc = TRUE;
    char results[40];
    FILE* resultsFile = NULL;
    int i, j, k, nCounter = 0;
    int res = FAIL;
    char tempSource[] = {'t','e','m','p','k','.','t','m','p','\0'};
    char tempDest[] = {'t','e','m','p','2','.','t','m','p','\0'};
    HANDLE hFile;
    DWORD result;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* read in the expected results to compare with actual results */
    memset (results, 0, 34);
    resultsFile = fopen("expectedresults.txt", "r");
    if (resultsFile == NULL)
    {
        Trace("MoveFileExA ERROR: Unable to open \"expectedresults.txt\"\n");
        goto EXIT;
    }

    fgets(results, 34, resultsFile);
    fclose(resultsFile);

    nCounter = 0;


    /* clean the slate */
    removeAll_MoveFileExA_test1();
    if (createExisting_MoveFileExA_test1() != PASS)
    {
        goto EXIT;
    }

    /* lpSource_MoveFileExA_test1 loop */
    for (i = 0; i < 4; i++)
    {
        /* lpDestination_MoveFileExA_test1 loop */
        for (j = 0; j < 4; j++)
        {
            /* dwFlag_MoveFileExA_test1 loop */
            for (k = 0; k < 2; k++)
            {

                /* move the file to the new location */
                bRc = MoveFileExA(lpSource_MoveFileExA_test1[i], lpDestination_MoveFileExA_test1[j], dwFlag_MoveFileExA_test1[k]);

                if (!(
                    ((bRc == TRUE) && (results[nCounter] == '1'))
                    ||
                    ((bRc == FALSE ) && (results[nCounter] == '0'))                    )
                    )
                {
                    Trace("MoveFileExA(%s, %s, %s): Values of i[%d], j[%d], k [%d] and results[%d]=%c LastError[%d]Flag[%d]FAILED\n",
                        lpSource_MoveFileExA_test1[i], lpDestination_MoveFileExA_test1[j],
                        k == 1 ?
                        "MOVEFILE_REPLACE_EXISTING":"MOVEFILE_COPY_ALLOWED", i, j, k, nCounter, results[nCounter], GetLastError(), bRc);
                    goto EXIT;
                }

                /* undo the last move */
                removeAll_MoveFileExA_test1();
                if (createExisting_MoveFileExA_test1() != PASS)
                {
                    goto EXIT;
                }
                nCounter++;
            }
        }
    }

    /* create the temp source file */
    hFile = CreateFileA(tempSource, GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);

    if( hFile == INVALID_HANDLE_VALUE )
    {
        Trace("MoveFileExA: CreateFile failed to "
            "create the file correctly.\n");
        goto EXIT;
    }

    bRc = CloseHandle(hFile);
    if(!bRc)
    {
        Trace("MoveFileExA: CloseHandle failed to close the "
            "handle correctly. yo %u\n",GetLastError());
        goto EXIT;
    }

    /* set the file attributes to be readonly */
    bRc = SetFileAttributesA(tempSource, FILE_ATTRIBUTE_READONLY);
    if(!bRc)
    {
        Trace("MoveFileExA: SetFileAttributes failed to set file "
            "attributes correctly. ERROR:%u\n",GetLastError());
        goto EXIT;
    }

    /* move the file to the new location */
    bRc = MoveFileExA(tempSource, tempDest, MOVEFILE_COPY_ALLOWED );
    if(!bRc)
    {
        Trace("MoveFileExA(%S, %S, %s): GetFileAttributes "
            "failed to get the file's attributes.\n",
            tempSource, tempDest, "MOVEFILE_COPY_ALLOWED");
        goto EXIT;
    }

    /* check that the newly moved file has the same file attributes
    as the original */
    result = GetFileAttributesA(tempDest);
    if(result == 0)
    {
        Trace("MoveFileExA: GetFileAttributes failed to get "
            "the file's attributes.\n");
        goto EXIT;
    }

    if((result & FILE_ATTRIBUTE_READONLY) != FILE_ATTRIBUTE_READONLY)
    {
        Trace("MoveFileExA: GetFileAttributes failed to get "
            "the correct file attributes.\n");
        goto EXIT;
    }

    /* set the file attributes back to normal, to be deleted */
    bRc = SetFileAttributesA(tempDest, FILE_ATTRIBUTE_NORMAL);
    if(!bRc)
    {
        Trace("MoveFileExA: SetFileAttributes "
            "failed to set file attributes correctly.\n");
        goto EXIT;
    }

    /* delete the newly moved file */
    bRc = DeleteFileA(tempDest);
    if(!bRc)
    {
        Trace("MoveFileExA: DeleteFileA failed to delete the"
            "file correctly.\n");
        goto EXIT;
    }

    res = PASS;

EXIT:
    removeAll_MoveFileExA_test1();

    PAL_TerminateEx(res);
    return res;
}

