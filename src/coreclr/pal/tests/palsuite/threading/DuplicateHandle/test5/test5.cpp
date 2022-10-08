// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test5.c (DuplicateHandle)
**
** Purpose: Tests the PAL implementation of the DuplicateHandle function,
**          with CreatePipe. This test will create a pipe and write to it,
**          the duplicate the read handle and read what was written.
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

PALTEST(threading_DuplicateHandle_test5_paltest_duplicatehandle_test5, "threading/DuplicateHandle/test5/paltest_duplicatehandle_test5")
{
    HANDLE  hReadPipe   = NULL;
    HANDLE  hWritePipe  = NULL;
    HANDLE  hDupPipe    = NULL;
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
    bRetVal = CreatePipe(&hReadPipe,       /* read handle*/
                         &hWritePipe,      /* write handle */
                         &lpPipeAttributes,/* security attributes*/
                         0);               /* pipe size*/
    if (bRetVal == FALSE)
    {
        Fail("ERROR:%u:Unable to create pipe\n", GetLastError());
    }

    /*Write to the write pipe handle*/
    bRetVal = WriteFile(hWritePipe,         /* handle to write pipe*/
                        cTestString,        /* buffer to write*/
                        strlen(cTestString),/* number of bytes to write*/
                        &dwBytesWritten,    /* number of bytes written*/
                        NULL);              /* overlapped buffer*/
    if (bRetVal == FALSE)
    {
        Trace("ERROR:%u:unable to write to write pipe handle "
            "hWritePipe=0x%lx\n", GetLastError(), hWritePipe);
        CloseHandle(hReadPipe);
        CloseHandle(hWritePipe);
        Fail("");
    }

    /*Duplicate the pipe handle*/
    if (!(DuplicateHandle(GetCurrentProcess(),       /* source handle process*/
                          hReadPipe,                 /* handle to duplicate*/
                          GetCurrentProcess(),       /* target process handle*/
                          &hDupPipe,                 /* duplicate handle*/
                          GENERIC_READ|GENERIC_WRITE,/* requested access*/
                          FALSE,                     /* handle inheritance*/
                          DUPLICATE_SAME_ACCESS)))   /* optional actions*/
    {
        Trace("ERROR:%u:Fail to create the duplicate handle"
             " to hReadPipe=0x%lx",
             GetLastError(),
             hReadPipe);
        CloseHandle(hReadPipe);
        CloseHandle(hWritePipe);
        Fail("");
    }

    /*Read from the duplicated handle, 256 bytes, more bytes
     than actually written. This will allow us to use the
     value that ReadFile returns for comparison.*/
    bRetVal = ReadFile(hDupPipe,           /* handle to read pipe*/
                       buffer,             /* buffer to write to*/
                       256,                /* number of bytes to read*/
                       &dwBytesRead,       /* number of bytes read*/
                       NULL);              /* overlapped buffer*/
    if (bRetVal == FALSE)
    {
        Trace("ERROR:%u:unable read from the duplicated pipe "
             "hDupPipe=0x%lx\n",
             GetLastError(),
             hDupPipe);
        CloseHandle(hReadPipe);
        CloseHandle(hWritePipe);
        CloseHandle(hDupPipe);
        Fail("");
    }

    /*Compare what was read with what was written.*/
    if ((memcmp(cTestString, buffer, dwBytesRead)) != 0)
    {
        Trace("ERROR:%u: read \"%s\" expected \"%s\" \n",
               GetLastError(),
               buffer,
               cTestString);
        CloseHandle(hReadPipe);
        CloseHandle(hWritePipe);
        CloseHandle(hDupPipe);
        Fail("");
    }

    /*Compare values returned from WriteFile and ReadFile.*/
    if (dwBytesWritten != dwBytesRead)
    {
        Trace("ERROR:%u: WriteFile wrote \"%s\", but ReadFile read \"%s\","
             " these should be the same\n",
             GetLastError(),
             buffer,
             cTestString);
        CloseHandle(hReadPipe);
        CloseHandle(hWritePipe);
        CloseHandle(hDupPipe);
        Fail("");
    }

    /*Cleanup.*/
    CloseHandle(hWritePipe);
    CloseHandle(hReadPipe);
    CloseHandle(hDupPipe);

    PAL_Terminate();
    return (PASS);
}
