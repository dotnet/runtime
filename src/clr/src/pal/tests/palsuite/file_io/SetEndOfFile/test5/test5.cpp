// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test5.c 
**
** Purpose: Tests the PAL implementation of the SetEndOfFile function.
**          Test attempts to read the number of characters up to
**          the EOF pointer, which was specified.
**
**
**===================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    HANDLE  hFile = NULL;
    DWORD   dwBytesWritten;
    DWORD   retCode;
    BOOL    bRc = FALSE;
    char    szBuffer[256];
    DWORD   dwBytesRead     = 0;
    char    testFile[]      = "testfile.tmp";
    char    testString[]    = "watch what happens";
    LONG shiftAmount = 10;

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
            Fail("ERROR:%u%: Unable to close handle 0x%lx.\n",
                 GetLastError(),
                 hFile);
        }
        Fail("");
    }

    /* Set the file pointer to shiftAmount bytes from the front of the file
    */
    retCode = SetFilePointer(hFile, shiftAmount, NULL, FILE_BEGIN);
    if(retCode == INVALID_SET_FILE_POINTER)
    {
        Trace("ERROR:%u: Unable to set the file pointer to %d\n",
            GetLastError(),
            shiftAmount);
        if (!CloseHandle(hFile))
        {
            Fail("ERROR:%u%: Unable to close handle 0x%lx.\n",
                 GetLastError(),
                 hFile);
        }
        Fail("");
    }

    /* set the end of file pointer to 'shiftAmount' */
    bRc = SetEndOfFile(hFile);
    if (bRc == FALSE)
    {
        Trace("ERROR:%u: Unable to set file pointer of file handle 0x%lx.\n",
              GetLastError(),
              hFile);
        if (!CloseHandle(hFile))
        {
            Fail("ERROR:%u%: Unable to close handle 0x%lx.\n",
                 GetLastError(),
                 hFile);
        }
        Fail("");
    }

    /* Set the file pointer to 10 bytes from the front of the file
    */
    retCode = SetFilePointer(hFile, (LONG)NULL, NULL, FILE_BEGIN);
    if(retCode == INVALID_SET_FILE_POINTER)
    {
        Trace("ERROR:%u: Unable to set the file pointer to %d\n",
            GetLastError(),
            FILE_BEGIN);
        if (!CloseHandle(hFile))
        {
            Fail("ERROR:%u%: Unable to close handle 0x%lx.\n",
                 GetLastError(),
                 hFile);
        }
        Fail("");
    }

    /* Attempt to read the entire string, 'testString' from a file
    * that has it's end of pointer set at shiftAmount;
    */
    bRc = ReadFile(hFile,
                   szBuffer,
                   strlen(testString),
                   &dwBytesRead,
                   NULL);

    if (bRc == FALSE)
    {
        Trace("ERROR:%u: Unable to read from file handle 0x%lx.\n",
              GetLastError(),
              hFile);
        if (!CloseHandle(hFile))
        {
            Fail("ERROR:%u%: Unable to close handle 0x%lx.\n",
                 GetLastError(),
                 hFile);
        }
        Fail("");
    }

    /* Confirm the number of bytes read with that requested.
    */
    if (dwBytesRead != shiftAmount)
    {
        Trace("ERROR: The number of bytes read \"%d\" is not equal to the "
              "number that should have been written \"%d\".\n",
              dwBytesRead,
              shiftAmount);
        if (!CloseHandle(hFile))
        {
            Fail("ERROR:%u%: Unable to close handle 0x%lx.\n",
                 GetLastError(),
                 hFile);
        }
        Fail("");
    }

    bRc = CloseHandle(hFile);
    if(!bRc)
    {
        Fail("ERROR:%u CloseHandle failed to close the handle\n",
             GetLastError());
    }
 
    /* Terminate the PAL.
     */
    PAL_Terminate();
    return PASS;
}
            
