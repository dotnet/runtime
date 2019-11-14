// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test2.c
**
** Purpose: Tests the PAL implementation of the GetFullPathNameA API.
**          GetFullPathA will be passed a directory that contains '..'.
**          To add to this test, we will also call SetCurrentDirectory to
**          ensure this is handled properly.
**          The test will create a file with in the parent directory 
**          to verify that the returned directory is valid.
**
**
**===================================================================*/

#include <palsuite.h>

const char* szDotDot   = "..\\";
const char* szFileName = "testing.tmp";

int __cdecl main(int argc, char *argv[])
{
    DWORD dwRc = 0;
    char szReturnedPath[_MAX_DIR+1];
    char szFullFileName[_MAX_DIR+1];
    LPSTR pPathPtr;
    HANDLE hFile = NULL;

    /* Initialize the PAL.
     */
    if (0 != PAL_Initialize(argc,argv))
    {
        return (FAIL);
    }

    /* change the directory */
    if (!SetCurrentDirectoryA(szDotDot))
    {
        Fail("ERROR: SetCurrentDirectoryA failed with error code %u"
             " when passed \"%s\".\n",
             GetLastError(),
             szDotDot);
    }

    /* Initialize the receiving char buffers.
     */
    memset(szReturnedPath, 0, _MAX_DIR+1);
    memset(szFullFileName, 0, _MAX_DIR+1);

    /* Create Full filename to pass, will include '..\'
     * as a pre-fix. */
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
        Fail("ERROR :%ld: GetFullPathName failed to "
             "retrieve the path of \"%s\".\n",
             GetLastError(),
             szFileName);
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
        Fail("ERROR :%ld: CreateFileA failed to create \"%s\".\n", 
             GetLastError(),
             szReturnedPath);
    }

    /* Close the handle to the created file.
     */
    if (CloseHandle(hFile) != TRUE)
    {
        Trace("ERROR :%ld: CloseHandle failed close hFile=0x%lx.\n",
             GetLastError());
        goto terminate;
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
        Fail("ERROR :%ld: CreateFileA succeeded to create file "
             "\"%s\", that already existed.\n",
             GetLastError(),
             szFullFileName);
    }


    /* Verify that the returned filename is the same as the supplied.
     */
    if (strcmp(pPathPtr, szFileName) != 0)
    {
        Trace("ERROR : Returned filename \"%s\" is not equal to "
             "supplied filename \"%s\".\n",
             pPathPtr, 
             szFileName);
        goto terminate;
    }

terminate:
    /* Delete the create file. 
     */
    if (DeleteFileA(szReturnedPath) != TRUE)
    {
        Fail("ERROR :%ld: DeleteFileA failed to delete \"%s\".\n",
             GetLastError(),
             szFileName);
    }

    /* Terminate the PAL.*/
    PAL_Terminate();
    return PASS;
}


