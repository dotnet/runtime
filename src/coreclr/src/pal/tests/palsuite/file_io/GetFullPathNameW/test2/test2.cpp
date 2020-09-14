// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test2.c
**
** Purpose: Tests the PAL implementation of the GetFullPathNameW function.
**          Get the full path for a file name and verify the results.
**	    This test will use a relative path, containing '..\'. To
**          add to this test, we will also call SetCurrentDirectory to
**          ensure this is handled properly.   
**
**
**===================================================================*/

#include <palsuite.h>

PALTEST(file_io_GetFullPathNameW_test2_paltest_getfullpathnamew_test2, "file_io/GetFullPathNameW/test2/paltest_getfullpathnamew_test2")
{
    WCHAR szwDotDot[]   = {'.','.','\\','\0'};
    WCHAR szwFileName[] = {'t','e','s','t','i','n','g','.','t','m','p','\0'};

    DWORD dwRc = 0;
    WCHAR szwReturnedPath[_MAX_DIR+1];
    WCHAR szwFullFileName[_MAX_DIR+1];
    char  *szReturnedPath;
    char  *szFileName;
    LPWSTR pPathPtr;
    HANDLE hFile = NULL;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* change the directory */
    if (!SetCurrentDirectoryW(szwDotDot))
    {
        Fail("ERROR: SetCurrentDirectoryW failed with error code %u"
             " when passed \"%S\".\n",
             GetLastError(),
             szwDotDot);
    }

    /* Initialize the receiving char buffers.
     */    
    memset(szwReturnedPath, 0, sizeof(szwReturnedPath));
    memset(szwFullFileName, 0, sizeof(szwFullFileName));

    /* Create Full filename to pass, will include '..\'
     * as a pre-fix. */
    wcscat(szwFullFileName, szwDotDot);
    wcscat(szwFullFileName, szwFileName);

   /* Convert wide char strings to multibyte, to us
     * incase of error messages.*/
    szFileName     = convertC(szwFileName);

    /* Get the full path to the filename.
     */
    dwRc = GetFullPathNameW(szwFullFileName,
                            _MAX_DIR,
                            szwReturnedPath,
                            &pPathPtr);
    
    szReturnedPath = convertC(szwReturnedPath);
   
    if (dwRc == 0)
    {
        Trace("ERROR :%ld: Failed to get path to  \"%s\".\n", 
             GetLastError(),
             szReturnedPath);
        free(szReturnedPath);
        free(szFileName);
        Fail("");
    }
    
    /*
     * The returned value should be the parent directory with the
     * file name appended.
     */
    hFile = CreateFileW(szwReturnedPath,
                        GENERIC_READ,
                        FILE_SHARE_READ,
                        NULL,
                        CREATE_ALWAYS,
                        FILE_ATTRIBUTE_NORMAL,
                        NULL);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        Trace("ERROR :%ld: CreateFileW failed to create file \"%s\".\n",
             GetLastError(),
             szReturnedPath);
        free(szFileName);
        free(szReturnedPath);
        Fail("");
    }

    /* Close the handle to the create file.*/
    if (CloseHandle(hFile) != TRUE)
    {
        Trace("ERROR :%ld: Failed to close handle hFile=0x%lx.\n",
              GetLastError(),
              hFile);
        goto terminate;
    }

    /* Verify that the file was created, attempt to create 
     * the file again. */
    hFile = CreateFileW(szwReturnedPath,
                        GENERIC_READ,
                        FILE_SHARE_READ,
                        NULL,
                        CREATE_NEW,
                        FILE_ATTRIBUTE_NORMAL,
                        NULL);
    if ((hFile != INVALID_HANDLE_VALUE) && 
        (GetLastError() != ERROR_ALREADY_EXISTS))
    {
        Trace("ERROR :%ld: CreateFileW succeeded to create file "
              "\"%s\", that already existed.\n",
              GetLastError(),
              szReturnedPath);
        goto terminate;
    }

    /* Verify that the returned filename is the same as the supplied.
     */
    if (wcsncmp(pPathPtr, szwFileName, wcslen(szwFileName)) != 0)
    {
        Trace("ERROR : Returned filename is not equal to \"%s\".\n",
              szFileName);
        goto terminate;
    }

terminate:
    /* Delete the create file.
     */
    if (DeleteFileW(szwFullFileName) != TRUE)
    {
        Trace("ERROR :%ld: DeleteFileW failed to delete \"%s\".\n",
              szFileName,
              GetLastError());
        free(szFileName);
        free(szReturnedPath);
        Fail("");
    }

    free(szFileName);
    free(szReturnedPath);

    /* Terminate the PAL.
     */
    PAL_Terminate();
    return PASS;
}

