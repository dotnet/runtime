// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test4.c
**
** Purpose: Tests the PAL implementation of the GetFullPathNameW API.
**          GetFullPathNameW will be passed a directory that begins with '..'.
**          Example: ..\test_directory\testing.tmp.
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
**          DeleteFileW,
**          RemoveDirectory.
**

**
**===================================================================*/
#define UNICODE
#include <palsuite.h>

PALTEST(file_io_GetFullPathNameW_test4_paltest_getfullpathnamew_test4, "file_io/GetFullPathNameW/test4/paltest_getfullpathnamew_test4")
{
#ifdef WIN32
    const WCHAR szSeparator[] = {'\\','\\','\0'};
#else
    const WCHAR szSeparator[] = {'/','/','\0'};
#endif

    const WCHAR szDotDot[]   = {'.','.','\0'};
    const WCHAR szFileName[] = {'t','e','s','t','i','n','g','.','t','m','p','\0'};

    DWORD   dwRc = 0;

    WCHAR   szReturnedPath[_MAX_DIR+1];
    WCHAR   szFullFileName[_MAX_DIR+1];
    WCHAR   szDirectory[256];
    WCHAR   szCreatedDir[]     = {'t','e','s','t','_','d','i','r','\0'};

    LPWSTR  pPathPtr;
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
    memset(szDirectory, '\0', 256 * sizeof(szDirectory[0]));

    /* Create the path to the next level of directory to create.
     */
    wcscat(szDirectory, szDotDot);        /* ..                  */
    wcscat(szDirectory, szSeparator);     /* ../                 */
    wcscat(szDirectory, szCreatedDir);    /* ../test_directory   */

    /* Create a test directory.
     */
    if (!CreateDirectoryW(szDirectory, NULL))
    {
        Fail("ERROR:%u: Unable to create directories \"%S\".\n",
             GetLastError(),
             szDirectory);
    }

    /* Initialize the receiving char buffers.
     */
    memset(szReturnedPath, 0, sizeof(szFullFileName));
    memset(szFullFileName, 0, sizeof(szFullFileName));

    /* Create Full filename to pass, will include '..\'
     * in the middle of the path.
     */
    wcscat( szFullFileName, szDotDot );     /* ..                            */
    wcscat( szFullFileName, szSeparator );  /* ../                           */
    wcscat( szFullFileName, szCreatedDir ); /* ../test_directory             */
    wcscat( szFullFileName, szSeparator );  /* ../test_directory/            */
    wcscat( szFullFileName, szFileName );   /* ../test_directory/testing.tmp */

    /* Get the full path to the filename.
     */
    dwRc = GetFullPathNameW(szFullFileName,
                            _MAX_DIR,
                            szReturnedPath,
                            &pPathPtr);
    if (dwRc == 0)
    {
        Trace("ERROR :%ld: GetFullPathName failed to "
              "retrieve the path of \"%S\".\n",
              GetLastError(),
              szFileName);
        bRetVal = FAIL;
        goto cleanUpOne;
    }

    /* The returned value should be the parent directory with the
     * file name appended. */
    hFile = CreateFileW(szReturnedPath,
                        GENERIC_READ,
                        FILE_SHARE_READ,
                        NULL,
                        CREATE_ALWAYS,
                        FILE_ATTRIBUTE_NORMAL,
                        NULL);

    if (hFile == INVALID_HANDLE_VALUE)
    {
        Trace("ERROR :%ld: CreateFileA failed to create \"%S\".\n",
              GetLastError(),
              szReturnedPath);
        bRetVal = FAIL;
        goto cleanUpOne;
    }

    /* Close the handle to the created file.
     */
    if (CloseHandle(hFile) != TRUE)
    {
        Trace("ERROR :%ld: CloseHandle failed close hFile=0x%lx.\n",
              GetLastError());
        bRetVal = FAIL;
        goto cleanUpTwo;
    }

    /* Verify that the file was created, attempt to create
     * the file again. */
    hFile = CreateFileW(szReturnedPath,
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
              "\"%S\", that already existed.\n",
              GetLastError(),
              szFullFileName);
        bRetVal = FAIL;
        goto cleanUpTwo;
    }

    /* Verify that the returned filename is the same as the supplied.
     */
    if (wcscmp(pPathPtr, szFileName) != 0)
    {
        Trace("ERROR : Returned filename \"%S\" is not equal to "
              "supplied filename \"%S\".\n",
              pPathPtr,
              szFileName);
        bRetVal = FAIL;
        goto cleanUpTwo;
    }

    /* Successful test.
     */
    bRetVal = PASS;

cleanUpTwo:

    /* Delete the create file.
     */
    if (DeleteFileW(szReturnedPath) != TRUE)
    {
        Fail("ERROR :%ld: DeleteFileA failed to delete \"%S\".\n",
             GetLastError(),
             szFileName);
    }

cleanUpOne:

    /* Remove the empty directory.
     */
    if (!RemoveDirectoryW(szDirectory))
    {
        Fail("ERROR:%u: Unable to remove directory \"%s\".\n",
             GetLastError(),
             szCreatedDir);
    }

    /* Terminate the PAL.*/
    PAL_TerminateEx(bRetVal);
    return bRetVal;
}
