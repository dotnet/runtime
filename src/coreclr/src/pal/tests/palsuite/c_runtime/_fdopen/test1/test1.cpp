// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c (fdopen)
**
** Purpose: Tests the PAL implementation of the fdopen function. 
**          This will test fdopen in r (read) mode. This test 
**          creates and opens a test pipe, to write and read 
**          from. fdopen requires a file handle(int), therefore
**          _open_osfhandle is used to get that handle. 
**          _open_osfhandle is only used with CreatePipe. The 
**          test will write and read from the pipe comparing 
**          the results.
**      
**          See /tests/palsuite/README.txt for more information.
**
**
**===================================================================*/

#include <palsuite.h>

#define cTestString "one fish, two fish, read fish, blue fish."

PALTEST(c_runtime__fdopen_test1_paltest_fdopen_test1, "c_runtime/_fdopen/test1/paltest_fdopen_test1")
{
    HANDLE  hReadPipe   = NULL;
    HANDLE  hWritePipe  = NULL;
    BOOL    bRetVal     = FALSE;
    int     iFiledes    = 0;
    DWORD   dwBytesWritten;
    char    buffer[45];
    FILE    *fp;

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
    bRetVal = CreatePipe(&hReadPipe,      // read handle
                &hWritePipe,              // write handle
                &lpPipeAttributes,        // security attributes
                0);                       // pipe size

    if (bRetVal == FALSE)
    {
        Fail("ERROR: unable to create pipe");
    }

    /*Write to the write pipe handle*/
    bRetVal = WriteFile(hWritePipe,       // handle to file
                cTestString,              // data buffer
                (DWORD)strlen(cTestString),      // number of bytes to write
                &dwBytesWritten,          // number of bytes written
                NULL);                    // overlapped buffer

    if (bRetVal == FALSE)
    {
        Fail("ERROR: unable to write to pipe write handle "
                "hWritePipe=0x%lx", hWritePipe);
    }

    /*Get a file descriptor for the read pipe handle*/
    iFiledes = _open_osfhandle((long)hReadPipe, _O_RDONLY);

    if (iFiledes == -1)
    {
        Fail("ERROR: _open_osfhandle failed to open "
             " hReadPipe=0x%lx", hReadPipe);
    }
    
    /*Open read pipe handle in read mode*/
    fp = _fdopen(iFiledes, "r");

    if (fp == NULL)
    {
        Fail("ERROR: unable to fdopen file descriptor"
             " iFiledes=%d", iFiledes);
    }

    /*Read from the read pipe handle*/
    if((fread(buffer, sizeof(char), strlen(cTestString), fp)) == 0)
    {
        Fail("ERROR: Unable to read from file stream fp=0x%lx\n", fp);
    }

    /*Compare what was read with what was written.*/
    if ((memcmp(cTestString, buffer, strlen(cTestString))) != 0)
    {
        Fail("ERROR: read \"%s\" expected \"%s\" \n", buffer, cTestString);
    }

    /*Close the file handle*/
    if (_close(iFiledes) != 0)
    {
        Fail("ERROR: Unable to close file handle iFiledes=%d\n", iFiledes);
    }
    
    PAL_Terminate();
    return (PASS);
}
