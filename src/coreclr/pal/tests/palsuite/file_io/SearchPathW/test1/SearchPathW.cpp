// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  SearchPathW.c
**
** Purpose: Tests the PAL implementation of the SearchFileW function.
**
**
** TODO: Write a test where complete path is passed (say c:\?)
**===================================================================*/
//SearchPath
//
//The SearchPath function searches for the specified file in the specified path.
//
//
//DWORD SearchPath(
//  LPCTSTR lpPath,
//  LPCTSTR lpFileName,
//  LPCTSTR lpExtension,
//  DWORD nBufferLength,
//  LPTSTR lpBuffer,
//  LPTSTR* lpFilePart
//);
//
//Parameters
//lpPath
//[in] Pointer to a null-terminated string that specifies the path to be searched for the file. If this parameter is NULL, the function searches for a matching file in the following directories in the following sequence:
//The directory from which the application loaded.
//The current directory.
//The system directory. Use the GetSystemDirectory function to get the path of this directory.
//The 16-bit system directory. There is no function that retrieves the path of this directory, but it is searched.
//The Windows directory. Use the GetWindowsDirectory function to get the path of this directory.
//The directories that are listed in the PATH environment variable.

//lpFileName
//[in] Pointer to a null-terminated string that specifies the name of the file to search for.

//lpExtension
//[in] Pointer to a null-terminated string that specifies an extension to be added to the file name when searching for the file. The first character of the file name extension must be a period (.). The extension is added only if the specified file name does not end with an extension.
//If a file name extension is not required or if the file name contains an extension, this parameter can be NULL.
//
//nBufferLength
//[in] Size of the buffer that receives the valid path and file name, in TCHARs.

//lpBuffer
//[out] Pointer to the buffer that receives the path and file name of the file found.

//lpFilePart
//[out] Pointer to the variable that receives the address (within lpBuffer) of the last component of the valid path and file name, which is the address of the character immediately following the final backslash (\) in the path.

//Return Values
//If the function succeeds, the value returned is the length, in TCHARs, of the string copied to the buffer, not including the terminating null character. If the return value is greater than nBufferLength, the value returned is the size of the buffer required to hold the path.
//
//If the function fails, the return value is zero. To get extended error information, call GetLastError.


#include <palsuite.h>
#define szDir                             "."

#define szNoFileName                      "333asdf"
#define szNoFileNameExt                   ".x77t"

#define szFileNameExists                  "searchpathw"
#define szFileNameExtExists               ".c"

#define szFileNameExistsWithExt           "searchpathw.c"

char  fileloc_SearchPathW_test1[_MAX_PATH];

void removeFileHelper_SearchPathW_test1(LPSTR pfile, int location)
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

void RemoveAll_SearchPathW_test1()
{
    removeFileHelper_SearchPathW_test1(fileloc_SearchPathW_test1, 1);
}

PALTEST(file_io_SearchPathW_test1_paltest_searchpathw_test1, "file_io/SearchPathW/test1/paltest_searchpathw_test1")
{

    WCHAR* lpPath        = NULL;
    WCHAR* lpFileName    = NULL;
    WCHAR* lpExtension   = NULL;
    DWORD  nBufferLength = 0;
    WCHAR  lpBuffer[_MAX_PATH];
    WCHAR** lpFilePart    = NULL;
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

   /* Initialize the buffer.
     */
    memset(fullPath, 0, _MAX_DIR);

    if (GetTempPathA(_MAX_DIR, fullPath) == 0)
    {
        Fail("ERROR: GetTempPathA failed to get a path\n");
    }

    memset(fileloc_SearchPathW_test1, 0, _MAX_PATH);
    sprintf_s(fileloc_SearchPathW_test1, ARRAY_SIZE(fileloc_SearchPathW_test1), "%s%s", fullPath, szFileNameExistsWithExt);

    RemoveAll_SearchPathW_test1();

    hsearchfile = CreateFileA(fileloc_SearchPathW_test1, GENERIC_WRITE, 0, 0, CREATE_ALWAYS,
                            FILE_ATTRIBUTE_NORMAL, 0);

    if (hsearchfile == NULL)
    {
        Trace("ERROR[%ul]: couldn't create %s\n", GetLastError(), fileloc_SearchPathW_test1);
        return FAIL;
    }

    CloseHandle(hsearchfile);

    //
    // find a file that doesn't exist
    //
    ZeroMemory( lpBuffer, sizeof(lpBuffer));
    lpPath        = convert((LPSTR)fullPath);
    lpFileName    = convert((LPSTR)szNoFileName);
    lpExtension   = NULL;

    if( SearchPathW( lpPath, lpFileName, lpExtension, nBufferLength, lpBuffer, lpFilePart) != 0 ){
        error = GetLastError();
        free(lpPath);
        free(lpFileName);
        Fail ("SearchPathW: ERROR1 -> Found invalid file[%s][%s][%s][%d]\n", lpPath, szNoFileName, szNoFileNameExt, error);
    }

    free(lpPath);
    free(lpFileName);

    //
    // find a file that exists, when path is mentioned explicitly
    //
    ZeroMemory( lpBuffer, sizeof(lpBuffer));
    lpPath        = convert((LPSTR)fullPath);
    lpFileName    = convert((LPSTR)szFileNameExistsWithExt);
    lpExtension   = NULL;

    result  = SearchPathW( lpPath, lpFileName, lpExtension, nBufferLength, lpBuffer, lpFilePart);

    if( result == 0 ){
        error = GetLastError();
        free(lpPath);
        free(lpFileName);
        Fail ("SearchPathW: ERROR2 -> Did not Find valid file[%s][%s][%d]\n", lpPath, szFileNameExistsWithExt, error);
    }

    free(lpPath);
    free(lpFileName);

    RemoveAll_SearchPathW_test1();

    PAL_Terminate();
    return PASS;
}
