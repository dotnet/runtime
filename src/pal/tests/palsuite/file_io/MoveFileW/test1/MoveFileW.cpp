// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  MoveFileW.c
**
** Purpose: Tests the PAL implementation of the MoveFileW function.
**
**
**===================================================================*/

#include <palsuite.h>

LPSTR lpSource[4] = {"src_existing.txt",
                      "src_non-existant.txt",
                      "src_dir_existing",
                      "src_dir_non-existant"};
LPSTR lpDestination[4] = {"dst_existing.txt",
                          "dst_non-existant.txt",
                          "dst_dir_existing",
                          "dst_dir_non-existant"};


/* Create all the required test files */
int createExisting(void)
{
    FILE* tempFile = NULL;
    DWORD dwError;
    BOOL bRc = FALSE;
    WCHAR* wPtr = NULL;
    char szBuffer[100];

    /* create the src_existing file */
    tempFile = fopen(lpSource[0], "w");
    if (tempFile != NULL)
    {
        fprintf(tempFile, "MoveFile test file: src_existing.txt\n");
        fclose(tempFile);
    }
    else
    {
        Trace("ERROR: couldn't create %s\n", lpSource[0]);
        return FAIL;
    }

    /* create the src_dir_existing directory and files */
    wPtr = convert(lpSource[2]);
    bRc = CreateDirectoryW(wPtr, NULL);
    free(wPtr);
    if (bRc != TRUE)
    {
        Trace("MoveFileW: ERROR: couldn't create \"%s\" because of "
            "error code %ld\n", 
            lpSource[2],
            GetLastError());
        return FAIL;
    }

    memset(szBuffer, 0, 100);
    sprintf_s(szBuffer, _countof(szBuffer), "%s/test01.txt", lpSource[2]);
    tempFile = fopen(szBuffer, "w");
    if (tempFile != NULL)
    {
        fprintf(tempFile, "MoveFileW test file: %s\n", szBuffer);
        fclose(tempFile);
    }
    else
    {
        Trace("ERROR: couldn't create %s\n", szBuffer);
        return FAIL;
    }

    memset(szBuffer, 0, 100);
    sprintf_s(szBuffer, _countof(szBuffer), "%s/test02.txt", lpSource[2]);
    tempFile = fopen(szBuffer, "w");
    if (tempFile != NULL)
    {
        fprintf(tempFile, "MoveFileW test file: %s\n", szBuffer);
        fclose(tempFile);
    }
    else
    {
        Trace("ERROR: couldn't create %s\n", szBuffer);
        return FAIL;
    }


    /* create the dst_existing file */
    tempFile = fopen(lpDestination[0], "w");
    if (tempFile != NULL)
    {
        fprintf(tempFile, "MoveFileW test file: dst_existing.txt\n");
        fclose(tempFile);
    }
    else
    {
        Trace("ERROR: couldn't create \"%s\"\n", lpDestination[0]);
        return FAIL;
    }

    /* create the dst_dir_existing directory and files */
    wPtr = convert(lpDestination[2]);
    bRc = CreateDirectoryW(wPtr, NULL);
    free(wPtr);
    if (bRc != TRUE)
    {
        dwError = GetLastError();
        Trace("MoveFileW: ERROR: couldn't create \"%s\"\n", lpDestination[2]);
        return FAIL;
    }

    tempFile = fopen("dst_dir_existing/test01.txt", "w");
    if (tempFile != NULL)
    {
        fprintf(tempFile, "MoveFileW test file: dst_dir_existing/test01.txt\n");
        fclose(tempFile);
    }
    else
    {
        Trace("ERROR: couldn't create dst_dir_existing/test01.txt\n");
        return FAIL;
    }
    tempFile = fopen("dst_dir_existing/test02.txt", "w");
    if (tempFile != NULL)
    {
        fprintf(tempFile, "MoveFileW test file: dst_dir_existing/test02.txt\n");
        fclose(tempFile);
    }
    else
    {
        Trace("ERROR: couldn't create dst_dir_existing/test02.txt\n");
        return FAIL;
    }

    return PASS;
}

void removeDirectoryHelper(LPSTR dir, int location)
{    
    DWORD dwAtt = GetFileAttributesA(dir);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        if(!RemoveDirectoryA(dir))
        {
            Fail("ERROR: Failed to remove Directory [%s], Error Code [%d], location [%d]\n", dir, GetLastError(), location);           
        }
    }
}

void removeFileHelper(LPSTR pfile, int location)
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
        else
        {
    //       Trace("Success: deleted file [%S], Error Code [%d], location [%d]\n", wfile, GetLastError(), location);           
        }
    }

}

/* remove all created files in preparation for the next test */
void removeAll(void)
{
    char szTemp[40];
    DWORD dwAtt;

    /* get rid of source dirs and files */
    removeFileHelper(lpSource[0], 1);
    removeFileHelper(lpSource[1], 2);

    dwAtt = GetFileAttributesA(lpSource[2]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        sprintf_s(szTemp, _countof(szTemp), "%s/test01.txt", lpSource[2]);
        removeFileHelper(szTemp, 18);
    
        sprintf_s(szTemp, _countof(szTemp), "%s/test02.txt", lpSource[2]);    
        removeFileHelper(szTemp, 19);
        removeDirectoryHelper(lpSource[2], 103);
    }
    else
    {
        removeFileHelper(lpSource[2], 17);
    }


    dwAtt = GetFileAttributesA(lpSource[3]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        sprintf_s(szTemp, _countof(szTemp), "%s/test01.txt", lpSource[3]);
        removeFileHelper(szTemp, 18);
    
        sprintf_s(szTemp, _countof(szTemp), "%s/test02.txt", lpSource[3]);    
        removeFileHelper(szTemp, 19);
        removeDirectoryHelper(lpSource[3], 103);
    }
    else
    {
        removeFileHelper(lpSource[3], 17);
    }

    /* get rid of destination dirs and files */
    dwAtt = GetFileAttributesA(lpDestination[0]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        sprintf_s(szTemp, _countof(szTemp), "%s/test01.txt", lpDestination[0]);
        removeFileHelper(szTemp, 18);
    
        sprintf_s(szTemp, _countof(szTemp), "%s/test02.txt", lpDestination[0]);    
        removeFileHelper(szTemp, 19);
        removeDirectoryHelper(lpDestination[0], 103);
    }
    else
    {
        removeFileHelper(lpDestination[0], 17);
    }

    dwAtt = GetFileAttributesA(lpDestination[1]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        sprintf_s(szTemp, _countof(szTemp), "%s/test01.txt", lpDestination[1]);
        removeFileHelper(szTemp, 18);
    
        sprintf_s(szTemp, _countof(szTemp), "%s/test02.txt", lpDestination[1]);    
        removeFileHelper(szTemp, 19);
        removeDirectoryHelper(lpDestination[1], 103);
    }
    else
    {
        removeFileHelper(lpDestination[1], 17);
    }

    dwAtt = GetFileAttributesA(lpDestination[2]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        sprintf_s(szTemp, _countof(szTemp), "%s/test01.txt", lpDestination[2]);
        removeFileHelper(szTemp, 18);
    
        sprintf_s(szTemp, _countof(szTemp), "%s/test02.txt", lpDestination[2]);    
        removeFileHelper(szTemp, 19);
        removeDirectoryHelper(lpDestination[2], 103);
    }
    else
    {
        removeFileHelper(lpDestination[2], 17);
    }

    dwAtt = GetFileAttributesA(lpDestination[3]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        sprintf_s(szTemp, _countof(szTemp), "%s/test01.txt", lpDestination[3]);
        removeFileHelper(szTemp, 18);
    
        sprintf_s(szTemp, _countof(szTemp), "%s/test02.txt", lpDestination[3]);    
        removeFileHelper(szTemp, 19);
        removeDirectoryHelper(lpDestination[3], 103);
    }
    else
    {
        removeFileHelper(lpDestination[3], 17);
    }

}





int __cdecl main(int argc, char *argv[])
{
    BOOL bRc = TRUE;
    BOOL bSuccess = TRUE;
    char results[40];
    FILE* resultsFile = NULL;
    int nCounter = 0;
    int i, j;
    WCHAR* wSource = NULL;
    WCHAR* wDest = NULL;
    WCHAR tempSource[] = {'t','e','m','p','k','.','t','m','p','\0'};
    WCHAR tempDest[] = {'t','e','m','p','2','.','t','m','p','\0'};
    HANDLE hFile;
    DWORD result;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* read in the expected results to compare with actual results */
    memset (results, 0, 20);
    resultsFile = fopen("expectedresults.txt", "r");
    if (resultsFile == NULL)
    {
        Fail("MoveFileW ERROR: Unable to open \"expectedresults.txt\"\n");
    }

    fgets(results, 20, resultsFile);
    fclose(resultsFile);

    /* clean the slate */
    removeAll();

    if (createExisting() != 0)
    {
        removeAll();
    }


    /* lpSource loop */
    for (i = 0; i < 4; i++)
    {
        /* lpDestination loop */
        for (j = 0; j < 4; j++)
        {
           
            wSource = convert(lpSource[i]);
            wDest = convert(lpDestination[j]);
            bRc = MoveFileW(wSource, wDest);
            free(wSource);
            free(wDest);
            if (!(
                    ((bRc == TRUE) && (results[nCounter] == '1')) 
                    || 
                    ((bRc == FALSE ) && (results[nCounter] == '0'))                    )
                )
            {
                    Trace("MoveFileW: FAILED: test[%d][%d]: \"%s\" -> \"%s\"\n", 
                        i, j, lpSource[i], lpDestination[j]);
                    bSuccess = FALSE;
            }

            /* undo the last move */
            removeAll();
            createExisting();

            nCounter++;
        }
    }

    removeAll();
    if (bSuccess == FALSE)
    {
        Fail("MoveFileW: Test Failed");
    }

    /* create the temp source file */
    hFile = CreateFileW(tempSource, GENERIC_WRITE, 0, 0, CREATE_ALWAYS,                        
                            FILE_ATTRIBUTE_NORMAL, 0);

    if( hFile == INVALID_HANDLE_VALUE )
    {
        Fail("MoveFileW: CreateFile failed to "
            "create the file correctly.\n");
    }

    bRc = CloseHandle(hFile);
    if(!bRc)
    {
        Trace("MoveFileW: CloseHandle failed to close the "
            "handle correctly. ERROR:%u\n",GetLastError());
        
        /* delete the created file */
        bRc = DeleteFileW(tempSource);
        if(!bRc)
        {
            Fail("MoveFileW: DeleteFileW failed to delete the"
                "file correctly.\n");
        }
        Fail("");
    }

    /* set the file attributes to be readonly */
    bRc = SetFileAttributesW(tempSource, FILE_ATTRIBUTE_READONLY);
    if(!bRc)
    {
        Trace("MoveFileW: SetFileAttributes failed to set file "
            "attributes correctly. GetLastError returned %u\n",GetLastError());
        /* delete the created file */
        bRc = DeleteFileW(tempSource);
        if(!bRc)
        {
            Fail("MoveFileW: DeleteFileW failed to delete the"
                "file correctly.\n");
        }
        Fail("");
    }

    /* move the file to the new location */
    bRc = MoveFileW(tempSource, tempDest);
    if(!bRc)
    {
        /* delete the created file */
        bRc = DeleteFileW(tempSource);
        if(!bRc)
        {
            Fail("MoveFileW: DeleteFileW failed to delete the"
                "file correctly.\n");
    }

        Fail("MoveFileW(%S, %S): GetFileAttributes "
            "failed to get the file's attributes.\n",
            tempSource, tempDest);
    }

    /* check that the newly moved file has the same file attributes
    as the original */
    result = GetFileAttributesW(tempDest);
    if(result == 0)
    {
        /* delete the created file */
        bRc = DeleteFileW(tempDest);
        if(!bRc)
        {
            Fail("MoveFileW: DeleteFileW failed to delete the"
                "file correctly.\n");
        }

        Fail("MoveFileW: GetFileAttributes failed to get "
            "the file's attributes.\n");
    }   

    if((result & FILE_ATTRIBUTE_READONLY) != FILE_ATTRIBUTE_READONLY)
    {
        /* delete the newly moved file */
        bRc = DeleteFileW(tempDest);
        if(!bRc)
        {
            Fail("MoveFileW: DeleteFileW failed to delete the"
                "file correctly.\n");
        }

        Fail("MoveFileW: GetFileAttributes failed to get "
            "the correct file attributes.\n");
    }

    /* set the file attributes back to normal, to be deleted */
    bRc = SetFileAttributesW(tempDest, FILE_ATTRIBUTE_NORMAL);
    if(!bRc)
    {
        /* delete the newly moved file */
        bRc = DeleteFileW(tempDest);
        if(!bRc)
        {
            Fail("MoveFileW: DeleteFileW failed to delete the"
                "file correctly.\n");
        }

        Fail("MoveFileW: SetFileAttributes failed to set "
            "file attributes correctly.\n");
    }

    /* delete the newly moved file */
    bRc = DeleteFileW(tempDest);
    if(!bRc)
    {
        Fail("MoveFileW: DeleteFileW failed to delete the"
            "file correctly.\n");
    }

    PAL_Terminate(); 

    return PASS;
}
