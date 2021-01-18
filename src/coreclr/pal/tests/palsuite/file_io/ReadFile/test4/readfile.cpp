// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  ReadFile.c (test 4)
**
** Purpose: Tests the PAL implementation of the ReadFile function.
**          Creates a file and writes a small string to it, attempt
**          to read many more characters that exist. The returned 
**          number of chars should be the amount written originally
**          not the number requested.
**
**
**===================================================================*/
#include <palsuite.h>

PALTEST(file_io_ReadFile_test4_paltest_readfile_test4, "file_io/ReadFile/test4/paltest_readfile_test4")
{
    HANDLE  hFile = NULL;
    DWORD   dwBytesWritten;
    BOOL    bRc = FALSE;
    char    szBuffer[256];
    DWORD   dwBytesRead     = 0;
    int     szRequestSize   = 256;
    char    testFile[]      = "testfile.tmp";
    char    testString[]    = "people stop and stare";
    DWORD   res             = 0; 

    /* Initialize the PAL.
     */
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* Initialize the buffer.
     */
    memset(szBuffer, 0, 256);

    /* Create a file to test with.
     */
    hFile = CreateFile(testFile, 
                       GENERIC_WRITE|GENERIC_READ,
                       FILE_SHARE_WRITE|FILE_SHARE_READ,
                       NULL,
                       CREATE_ALWAYS,
                       FILE_ATTRIBUTE_NORMAL,
                       NULL);

    if(hFile == INVALID_HANDLE_VALUE)
    {
        Fail("ERROR:%u: Unable to create file \"%s\".\n",
             GetLastError(),
             testFile);
    }

    /* Write to the File handle.
     */ 
    bRc = WriteFile(hFile,
                    testString,
                    strlen(testString),
                    &dwBytesWritten,
                    NULL);

    if (bRc == FALSE)
    {
        Trace("ERROR:%u: Unable to write to file handle "
                "hFile=0x%lx\n",
                GetLastError(),
                hFile);
        if (!CloseHandle(hFile))
        {
            Trace("ERROR:%u%: Unable to close handle 0x%lx.\n",
                  GetLastError(),
                  hFile);
        }
        Fail("");
    }
    
    /* Set the file pointer to beginning of file.
     */
    res = SetFilePointer(hFile, (LONG)NULL, NULL, FILE_BEGIN);

    if( (res == INVALID_SET_FILE_POINTER) &&
        (GetLastError() != NO_ERROR))
    {
        Trace("ERROR:%u: Unable to set file pointer to the beginning of file.",
               GetLastError());

        if (!CloseHandle(hFile))
        {
            Trace("ERROR:%u%: Unable to close handle 0x%lx.\n",
                  GetLastError(),
                  hFile);
        }
        Fail("");
    }


    /* Attempt to read 256 characters from a file
     * that does not contain that many.
     */
    bRc = ReadFile(hFile,
                   szBuffer,
                   szRequestSize,
                   &dwBytesRead,
                   NULL);

    if (bRc == FALSE)
    {
        Trace("ERROR:%u: Unable to read from file handle 0x%lx.\n",
              GetLastError(),
              hFile);
        if (!CloseHandle(hFile))
        {
            Trace("ERROR:%u%: Unable to close handle 0x%lx.\n",
                  GetLastError(),
                  hFile);
        }
        Fail("");
    }

    /* Confirm the number of bytes read with that requested.
    */
    if (dwBytesRead != strlen(testString))
    {
        Trace("ERROR: The number of bytes read \"%d\" is not equal to the "
              "number originally written \"%d\" to the file.\n",
              dwBytesRead,
              strlen(testString));
        if (!CloseHandle(hFile))
        {
            Trace("ERROR:%u%: Unable to close handle 0x%lx.\n",
                  GetLastError(),
                  hFile);
        }
        Fail("");
    }

    /* Terminate the PAL.
     */
    PAL_Terminate();
    return PASS;
}

