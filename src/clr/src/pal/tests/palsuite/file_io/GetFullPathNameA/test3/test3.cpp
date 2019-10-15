// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test3.c
**
** Purpose: Tests the PAL implementation of the GetFullPathNameA API.
**          GetFullPathA will be passed a directory that contains '..'.
**          Example: test_directory\level1\..\testing.tmp.
**          To add to this test, we will also call SetCurrentDirectory to
**          ensure this is handled properly.
**          The test will create a file with in the parent directory 
**          to verify that the returned directory is valid.
**
** Depends: SetCurrentDirectory,
**          CreateDirectory,
**          strcat,
**          memset,
**          CreateFile,
**          CloseHandle,
**          strcmp,
**          DeleteFileA,
**          RemoveDirectory.
**      

**
**===================================================================*/

#include <palsuite.h>

#ifdef WIN32
    const char* szSeperator = "\\";
#else
    const char* szSeperator = "//";
#endif

const char* szDotDot   = "..\\";
const char* szFileName = "testing.tmp";

int __cdecl main(int argc, char *argv[])
{
    DWORD   dwRc = 0;
    char    szReturnedPath[_MAX_DIR+1];
    char    szFullFileName[_MAX_DIR+1];
    char    szDirectory[256];
    char*   szCreatedDir = {"test_directory"};
    char*   szCreatedNextDir = {"level1"};
    WCHAR   *szCreatedDirW;
    LPSTR   pPathPtr;
    HANDLE  hFile = NULL;
    BOOL    bRetVal = FAIL;

    /* Initialize the PAL.
     */
    if (0 != PAL_Initialize(argc,argv))
    {
        return (FAIL);
    }

    /* Initialize the buffer.
     */
    memset(szDirectory, '\0', 256);

    /* Change the current working directory.
     */
    if (!SetCurrentDirectoryA(szDotDot))
    {
        Fail("ERROR: SetCurrentDirectoryA failed with error code %u"
             " when passed \"%s\".\n",
             GetLastError(),
             szDotDot);
    }
    
    /* Create the path to the next level of directory to create.
     */
    strcat( szDirectory, szCreatedDir );


    /* Create a test directory.
     */
    if ( !CreateDirectoryA( szDirectory, NULL ) )
    {
        Fail("ERROR:%u: Unable to create directories \"%s\".\n",
             GetLastError(),
             szDirectory);
    }

    /* Create the path to the next level of directory to create.
     */
    strcat( szDirectory, szSeperator );
    strcat( szDirectory, szCreatedNextDir );

    /* Create a test directory.
     */
    if ( !CreateDirectoryA( szDirectory, NULL ) )
    {
        Trace("ERROR:%u: Unable to create directories \"%s\".\n",
              GetLastError(),
              szDirectory);
        bRetVal = FAIL;
        goto cleanUpOne;
    }
    
    /* Initialize the receiving char buffers.
     */
    memset(szReturnedPath, 0, _MAX_DIR+1);
    memset(szFullFileName, 0, _MAX_DIR+1);

    /* Create Full filename to pass, will include '..\'
     * in the middle of the path.
     */
    strcat(szFullFileName, szCreatedDir);
    strcat(szFullFileName, szDotDot);
    strcat(szFullFileName, szFileName);

    /* Get the full path to the filename.
     */
    dwRc = GetFullPathNameA(szFullFileName, 
                            _MAX_DIR,
                            szReturnedPath, 
                            &pPathPtr);
    if (dwRc == 0)
    {
        Trace("ERROR :%ld: GetFullPathName failed to "
              "retrieve the path of \"%s\".\n",
              GetLastError(),
              szFileName);
        bRetVal = FAIL;
        goto cleanUpTwo;
    }

    /* The returned value should be the parent directory with the 
     * file name appended. */
    hFile = CreateFileA(szReturnedPath,
                        GENERIC_READ,
                        FILE_SHARE_READ,
                        NULL,
                        CREATE_ALWAYS,
                        FILE_ATTRIBUTE_NORMAL,
                        NULL);

    if (hFile == INVALID_HANDLE_VALUE)
    {
        Trace("ERROR :%ld: CreateFileA failed to create \"%s\".\n", 
              GetLastError(),
              szReturnedPath);
        bRetVal = FAIL;
        goto cleanUpTwo;
    }

    /* Close the handle to the created file.
     */
    if (CloseHandle(hFile) != TRUE)
    {
        Trace("ERROR :%ld: CloseHandle failed close hFile=0x%lx.\n",
              GetLastError());
        bRetVal = FAIL;
        goto cleanUpThree;
    }

    /* Verify that the file was created, attempt to create 
     * the file again. */
    hFile = CreateFileA(szReturnedPath,
                        GENERIC_READ,
                        FILE_SHARE_READ,
                        NULL,
                        CREATE_NEW,
                        FILE_ATTRIBUTE_NORMAL,
                        NULL);
    if ((hFile != INVALID_HANDLE_VALUE) && 
        (GetLastError() != ERROR_ALREADY_EXISTS))
    {
        Trace("ERROR :%ld: CreateFileA succeeded to create file "
              "\"%s\", that already existed.\n",
              GetLastError(),
              szFullFileName);
        bRetVal = FAIL;
        goto cleanUpThree;
    }

    /* Verify that the returned filename is the same as the supplied.
     */
    if (strcmp(pPathPtr, szFileName) != 0)
    {
        Trace("ERROR : Returned filename \"%s\" is not equal to "
             "supplied filename \"%s\".\n",
             pPathPtr, 
             szFileName);
        bRetVal = FAIL;
        goto cleanUpThree;
    }

    /* Successful test.
     */
    bRetVal = PASS;

cleanUpThree:
    
    /* Delete the create file. 
     */
    if (DeleteFileA(szReturnedPath) != TRUE)
    {
        Fail("ERROR :%ld: DeleteFileA failed to delete \"%s\".\n",
             GetLastError(),
             szFileName);
    }

cleanUpTwo:

    /* Remove the empty directory.
     */
    szCreatedDirW = convert((LPSTR)szDirectory);
    if (!RemoveDirectoryW(szCreatedDirW))
    {
        free (szCreatedDirW);
        Fail("ERROR:%u: Unable to remove directory \"%s\".\n",
             GetLastError(),
             szCreatedDir);
    }
    free (szCreatedDirW);

cleanUpOne:

    /* Remove the empty directory.
     */
    szCreatedDirW = convert((LPSTR)szCreatedDir);
    if (!RemoveDirectoryW(szCreatedDirW))
    {
        free (szCreatedDirW);
        Fail("ERROR:%u: Unable to remove directory \"%s\".\n",
             GetLastError(),
             szCreatedDir);
    }
    free (szCreatedDirW);

    /* Terminate the PAL.*/
    PAL_Terminate();
    return bRetVal;
}
