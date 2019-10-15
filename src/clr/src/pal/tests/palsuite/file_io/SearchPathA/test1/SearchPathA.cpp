// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  SearchPathA.c
**
** Purpose: Tests the PAL implementation of the SearchFileA function.
**
**
** TODO: Write a test where complete path is passed (say c:\?)
**===================================================================*/
//SearchPath
//
//The SearchPath function searches for the specified file in the specified path.
//


#include <palsuite.h>
char* szDir                   =          ".";

char* szNoFileName            =          "333asdf";
char* szNoFileNameExt         =          ".x77t";

char* szFileNameExists        =          "searchfile";
char* szFileNameExtExists     =          ".txt";

char* szFileNameExistsWithExt =          "searchfile.txt";
char  fileloc[_MAX_PATH];

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


void RemoveAll()
{
    removeFileHelper(fileloc, 1);
}

int __cdecl main(int argc, char *argv[]) {

    char* lpPath        = NULL;
    char* lpFileName    = NULL;
    char* lpExtension   = NULL;
    DWORD  nBufferLength = 0;
    char  lpBuffer[_MAX_PATH];
    char** lpFilePart    = NULL;
    DWORD  error         = 0;
    DWORD  result        = 0;

    HANDLE hsearchfile;
    char fname[_MAX_FNAME];
    char ext[_MAX_EXT];
    char   fullPath[_MAX_DIR];
    char drive[_MAX_DRIVE];
    char dir[_MAX_DIR];


    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

   
    /* Initalize the buffer.
     */
    memset(fullPath, 0, _MAX_DIR);

    if (GetTempPathA(_MAX_DIR, fullPath) == 0)
    {
        Fail("ERROR: GetTempPathA failed to get a path\n");
    }

    memset(fileloc, 0, _MAX_PATH);
    sprintf_s(fileloc, _countof(fileloc), "%s%s", fullPath, szFileNameExistsWithExt);

    RemoveAll();

    hsearchfile = CreateFileA(fileloc, GENERIC_WRITE, 0, 0, CREATE_ALWAYS,                        
                            FILE_ATTRIBUTE_NORMAL, 0);

    if (hsearchfile == INVALID_HANDLE_VALUE)
    {
        Trace("ERROR[%ul]: couldn't create %s\n", GetLastError(), fileloc);
        return FAIL;    
    }

    CloseHandle(hsearchfile);

    //
    // find a file that doesn't exist
    //
    ZeroMemory( lpBuffer, sizeof(lpBuffer));
    lpPath        = fullPath;
    lpFileName    = szNoFileName;
    lpExtension   = NULL;

    if( SearchPathA( lpPath, lpFileName, lpExtension, nBufferLength, lpBuffer, lpFilePart) != 0 ){
        error = GetLastError();
        Fail ("SearchPathA: ERROR1 -> Found invalid file[%s][%s][%s][%d]\n", lpPath, szNoFileName, szNoFileNameExt, error);
    }

    //
    // find a file that exists, when path is mentioned explicitly
    //
    ZeroMemory( lpBuffer, sizeof(lpBuffer));
    lpPath        = fullPath;
    lpFileName    = szFileNameExistsWithExt;
    lpExtension   = NULL;

    result  = SearchPathA( lpPath, lpFileName, lpExtension, nBufferLength, lpBuffer, lpFilePart);

    if( result == 0 ){
        error = GetLastError();
        Fail ("SearchPathA: ERROR2 -> Did not Find valid file[%s][%s][%d]\n", lpPath, szFileNameExistsWithExt, error);
    }

    RemoveAll();
    PAL_Terminate();
    return PASS; 
}
