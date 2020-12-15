// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  ReadFile.c (test 1)
**
** Purpose: Tests the PAL implementation of the ReadFile function.
**          This test will attempt to read from a NULL handle and from 
**          a file without read permissions set.
**
**
**===================================================================*/

#include <palsuite.h>


PALTEST(file_io_ReadFile_test1_paltest_readfile_test1, "file_io/ReadFile/test1/paltest_readfile_test1")
{
    HANDLE hFile = NULL;
    DWORD dwByteCount = 0;
    DWORD dwBytesRead = 0;
    BOOL bRc = FALSE;
    char szBuffer[256];
    char* szNonReadableFile = {"nonreadablefile.txt"};

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    memset(szBuffer, 0, 256);

    /* Read from a NULL handle
    */

    bRc = ReadFile(hFile, szBuffer, 20, &dwBytesRead, NULL);

    if (bRc == TRUE)
    {
        Fail("ReadFile: ERROR -> Able to read from a NULL handle\n");
    }


    /* Read from a file without read permissions
    */

#if WIN32

#else
    /* attempt to read from the unreadable file
     * open a file without read permissions
     */
    hFile = CreateFile(szNonReadableFile, 
        GENERIC_WRITE,
        FILE_SHARE_WRITE,
        NULL,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if(hFile == INVALID_HANDLE_VALUE)
    {
        dwByteCount = GetLastError();
        Fail("ReadFile: ERROR -> Unable to create file \"%s\".\n", 
            szNonReadableFile);
    }

    bRc = ReadFile(hFile, szBuffer, 20, &dwBytesRead, NULL);

    if (bRc == TRUE)
    {
        Fail("ReadFile: ERROR -> Able to read from a file without read "
            "permissions\n");
    }
#endif


    PAL_Terminate();
    return PASS;
}
