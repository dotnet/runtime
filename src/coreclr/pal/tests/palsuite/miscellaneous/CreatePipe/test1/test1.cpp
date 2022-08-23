// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c (CreatePipe)
**
** Purpose: Tests the PAL implementation of the CreatePipe function.
**          This test will create two pipes, a read and a write. Once
**          the pipes have been created, they will be tested by writing
**          and then reading, then comparing the results.
**
** Depends: WriteFile
**          ReadFile
**          memcmp
**          CloseHandle
**
**
**===================================================================*/

#include <palsuite.h>

#define cTestString "one fish, two fish, read fish, blue fish."

PALTEST(miscellaneous_CreatePipe_test1_paltest_createpipe_test1, "miscellaneous/CreatePipe/test1/paltest_createpipe_test1")
{
    HANDLE  hReadPipe   = NULL;
    HANDLE  hWritePipe  = NULL;
    BOOL    bRetVal     = FALSE;
    DWORD   dwBytesWritten;
    DWORD   dwBytesRead;
    char    buffer[256];

    SECURITY_ATTRIBUTES lpPipeAttributes;

    /*Initialize the PAL*/
    if ((PAL_Initialize(argc, argv)) != 0)
    {
        return (FAIL);
    }

    /*Setup SECURITY_ATTRIBUTES structure for CreatePipe*/
    lpPipeAttributes.nLength              = sizeof(lpPipeAttributes);
    lpPipeAttributes.lpSecurityDescriptor = NULL;
    lpPipeAttributes.bInheritHandle       = TRUE;

    /*Create a Pipe*/
    bRetVal = CreatePipe(&hReadPipe,      /* read handle*/
                &hWritePipe,              /* write handle */
                &lpPipeAttributes,        /* security attributes*/
                0);                       /* pipe size*/
    if (bRetVal == FALSE)
    {
        Fail("ERROR: %ld :Unable to create pipe\n", GetLastError());
    }

    /*Write to the write pipe handle*/
    bRetVal = WriteFile(hWritePipe,         /* handle to write pipe*/
                        cTestString,        /* buffer to write*/
                        strlen(cTestString),/* number of bytes to write*/
                        &dwBytesWritten,    /* number of bytes written*/
                        NULL);              /* overlapped buffer*/
    if (bRetVal == FALSE)
    {
        Fail("ERROR: %ld :unable to write to write pipe handle "
            "hWritePipe=0x%lx\n", GetLastError(), hWritePipe);
    }

    /*Read, 256 bytes, more bytes then actually written.
     This will give allow us to use the value that ReadFile
     returns for comparison.*/
    bRetVal = ReadFile(hReadPipe,          /* handle to read pipe*/
                       buffer,             /* buffer to write to*/
                       256,                /* number of bytes to read*/
                       &dwBytesRead,       /* number of bytes read*/
                       NULL);              /* overlapped buffer*/
    if (bRetVal == FALSE)
    {
        Fail("ERROR: %ld : unable read hWritePipe=0x%lx\n",
            GetLastError(), hWritePipe);
    }

    /*Compare what was read with what was written.*/
    if ((memcmp(cTestString, buffer, dwBytesRead)) != 0)
    {
        Fail("ERROR: read \"%s\" expected \"%s\" \n", buffer, cTestString);
    }

    /*Compare values returned from WriteFile and ReadFile.*/
    if (dwBytesWritten != dwBytesRead)
    {
        Fail("ERROR: WriteFile wrote \"%d\", but ReadFile read \"%d\","
             " these should be the same\n", buffer, cTestString);
    }

    /*Close write pipe handle*/
    if (CloseHandle(hWritePipe) == 0)
    {
        Fail("ERROR: %ld : Unable to close write pipe handle "
             "hWritePipe=0x%lx\n",GetLastError(), hWritePipe);
    }

    /*Close Read pipe handle*/
    if (CloseHandle(hReadPipe) == 0)
    {
        Fail("ERROR: %ld : Unable to close read pipe handle "
             "hReadPipe=0x%lx\n", GetLastError(), hReadPipe);
    }

    PAL_Terminate();
    return (PASS);
}
