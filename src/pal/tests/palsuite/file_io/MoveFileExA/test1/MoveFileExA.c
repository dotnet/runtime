// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  MoveFileExA.c
**
** Purpose: Tests the PAL implementation of the MoveFileExA function.
**
**
**===================================================================*/

#include <palsuite.h>


LPSTR lpSource[4] = { 
						"src_existing.tmp",
						"src_non-existant.tmp",
						"src_dir_existing",
						"src_dir_non-existant"
					};
LPSTR lpDestination[4]={
						"dst_existing.tmp",
						"dst_non-existant.tmp",
						"dst_dir_existing",
						"dst_dir_non-existant"
						};

LPSTR lpFiles[14] ={
						"src_dir_existing\\test01.tmp",
						"src_dir_existing\\test02.tmp",
						"dst_dir_existing\\test01.tmp",
						"dst_dir_existing\\test02.tmp",
						"src_dir_non-existant\\test01.tmp",
						"src_dir_non-existant\\test02.tmp",
						"dst_existing.tmp\\test01.tmp",
						"dst_existing.tmp\\test02.tmp",
						"dst_non-existant.tmp\\test01.tmp",
						"dst_non-existant.tmp\\test02.tmp",
						"dst_dir_existing\\test01.tmp",
						"dst_dir_existing\\test02.tmp",
						"dst_dir_non-existant\\test01.tmp",
						"dst_dir_non-existant\\test02.tmp"
						};
  
DWORD dwFlag[2] = {MOVEFILE_COPY_ALLOWED, MOVEFILE_REPLACE_EXISTING};




int createExisting(void)
{
    HANDLE tempFile  = NULL;
    HANDLE tempFile2 = NULL;

    /* create the src_existing file and dst_existing file */
    tempFile = CreateFileA(lpSource[0], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,                        
                            FILE_ATTRIBUTE_NORMAL, 0);
    tempFile2 = CreateFileA(lpDestination[0], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);
    CloseHandle(tempFile2);
    CloseHandle(tempFile);

    if ((tempFile == NULL) || (tempFile2 == NULL))
    {
        Trace("ERROR[%ul]: couldn't create %S or %S\n", GetLastError(), lpSource[0], 
                lpDestination[0]);
        return FAIL;    
    }

    /* create the src_dir_existing and dst_dir_existing directory and files */
    CreateDirectoryA(lpSource[2], NULL);

    tempFile = CreateFileA(lpFiles[0], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);
    tempFile2 = CreateFileA(lpFiles[1], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);
    CloseHandle(tempFile2);
    CloseHandle(tempFile);

    if ((tempFile == NULL) || (tempFile2 == NULL))
    {
        Trace("ERROR[%ul]: couldn't create src_dir_existing\\test01.tmp\n", GetLastError());
        return FAIL;
    }

    CreateDirectoryA(lpDestination[2], NULL);
    tempFile = CreateFileA(lpFiles[2], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);
    tempFile2 = CreateFileA(lpFiles[3], GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
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
    }

}

void removeAll(void)
{
    DWORD dwAtt;
    /* get rid of destination dirs and files */
    removeFileHelper(lpSource[0], 11);  
    removeFileHelper(lpSource[1], 12);
    removeFileHelper(lpFiles[0], 13);
    removeFileHelper(lpFiles[1], 14);

    removeDirectoryHelper(lpSource[2], 101);
    removeFileHelper(lpFiles[4], 15);
    removeFileHelper(lpFiles[5], 16);
    removeDirectoryHelper(lpSource[3], 102);

    /* get rid of destination dirs and files */
    dwAtt = GetFileAttributesA(lpDestination[0]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        removeFileHelper(lpFiles[6], 18);
        removeFileHelper(lpFiles[7], 19);
        removeDirectoryHelper(lpDestination[0], 103);
    }
    else
    {
        removeFileHelper(lpDestination[0], 17);
    }

    dwAtt = GetFileAttributesA(lpDestination[1]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        removeFileHelper(lpFiles[8], 21);
        removeFileHelper(lpFiles[9], 22);
        removeDirectoryHelper(lpDestination[1], 104);
    }
    else
    {
        removeFileHelper(lpDestination[1], 19);
    }
 
    dwAtt = GetFileAttributesA(lpDestination[2]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        removeFileHelper(lpFiles[10], 24);
        removeFileHelper(lpFiles[11], 25);
        removeDirectoryHelper(lpDestination[2], 105);
    }
    else
    {
        removeFileHelper(lpDestination[2], 23);  
    }

    dwAtt = GetFileAttributesA(lpDestination[3]);
    if (( dwAtt != INVALID_FILE_ATTRIBUTES ) && ( dwAtt & FILE_ATTRIBUTE_DIRECTORY) )
    {
        removeFileHelper(lpFiles[12], 26);
        removeFileHelper(lpFiles[13], 27);
        removeDirectoryHelper(lpDestination[3], 106);
    }
    else
    {
        removeFileHelper(lpDestination[3], 107);
    }

}

int __cdecl main(int argc, char *argv[])
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
    removeAll();
    if (createExisting() != PASS)
    {
        goto EXIT;
    }  

    /* lpSource loop */
    for (i = 0; i < 4; i++)
    {
        /* lpDestination loop */
        for (j = 0; j < 4; j++)
        {
            /* dwFlag loop */
            for (k = 0; k < 2; k++)
            {

                /* move the file to the new location */
                bRc = MoveFileExA(lpSource[i], lpDestination[j], dwFlag[k]);

                if (!(
                    ((bRc == TRUE) && (results[nCounter] == '1')) 
                    || 
                    ((bRc == FALSE ) && (results[nCounter] == '0'))                    )
                    )
                {
                    Trace("MoveFileExA(%s, %s, %s): Values of i[%d], j[%d], k [%d] and results[%d]=%c LastError[%d]Flag[%d]FAILED\n", 
                        lpSource[i], lpDestination[j], 
                        k == 1 ? 
                        "MOVEFILE_REPLACE_EXISTING":"MOVEFILE_COPY_ALLOWED", i, j, k, nCounter, results[nCounter], GetLastError(), bRc);
                    goto EXIT;
                }

                /* undo the last move */
                removeAll();
                if (createExisting() != PASS)
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
    removeAll();

    PAL_TerminateEx(res);
    return res;
}

