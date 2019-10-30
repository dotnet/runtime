// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test1.c (_open_osfhandle)
**
** Purpose: Tests the PAL implementation of the _open_osfhandle function. 
**          This API accepts a OS Handle returned from CreatePipe() and
**          a flag of _O_RDONLY and returns a C Run-Time handle. The test
**          will write to the pipe and pass the C Run-Time handle to _fdopen
**          to open the Read Handle to compare what was written with what
**          was wrote. They should be the same.
**
** Depends: CreatePipe
**          WriteFile
**          _fdopen
**          fread
**          memcmp
**          fclose
**          strlen
**          CloseHandle
**
**
**===================================================================*/

#include <palsuite.h>

const char* cTestString = "one fish, two fish, red fish, blue fish.";

int __cdecl main(int argc, char **argv)
{
    HANDLE  hReadPipe   = NULL;
    HANDLE  hWritePipe  = NULL;
    BOOL    bRetVal     = FALSE;
    int     iFiledes    = 0;
    DWORD   dwBytesWritten;
    char    buffer[45];
    FILE    *fp;
    size_t  len;

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
        Fail("ERROR: unable to create pipe");
    }

    /*Write to the write pipe handle*/
    bRetVal = WriteFile(hWritePipe,         /* handle to file*/
                        cTestString,        /* data buffer*/
                        strlen(cTestString),/* number of bytes to write*/
                        &dwBytesWritten,    /* number of bytes written*/
                        NULL);              /* overlapped buffer*/
    if (bRetVal == FALSE)
    {
        Fail("ERROR: unable to write to pipe write handle "
                "hWritePipe=0x%lx", hWritePipe);
    }

    /*Test to see if the WriteFile wrote the correct amount*/
    if(dwBytesWritten != strlen(cTestString))
    {
        Fail("Error: WriteFile wrote \"%d\", should have written \"%d\\n",
             dwBytesWritten, strlen(cTestString));
    }

    /*Get a file descriptor for the read pipe handle.
     *This is what we are testing.*/
    iFiledes = _open_osfhandle((long)hReadPipe, _O_RDONLY);
    if (iFiledes == -1)
    {
        Fail("ERROR: _open_osfhandle failed to open "
             " hReadPipe=0x%lx", hReadPipe);
    }
    
    /*Open read pipe handle in read mode.
     *Verify that we have returned a correct,
     *C Run-time handle*/
    fp = _fdopen(iFiledes, "r");
    if (fp == NULL)
    {
        Fail("ERROR: unable to fdopen file descriptor"
             " iFiledes=%d", iFiledes);
    }

    /*Read from the read pipe handle*/
    len = fread(buffer, sizeof(char), strlen(cTestString), fp);
    if((len == 0) || (len != strlen(cTestString)))
    {
        Fail("ERROR: Unable to read from file stream fp=0x%lx\n", fp);
    }

    /*Compare what was read with what was written.*/
    if ((memcmp(cTestString, buffer, strlen(cTestString))) != 0)
    {
        Fail("ERROR: read \"%s\" expected \"%s\" \n", buffer, cTestString);
    }

    /*Close write pipe handle*/
    if (CloseHandle(hWritePipe) == 0)
    {
        Fail("ERROR: Unable to close write pipe handle "
             "hWritePipe=0x%lx", hWritePipe);
    }
    
    if ((fclose(fp)) != 0)
    {
        Fail("ERROR: Unable to close C-Runtime handle "
             "iFilesdes=%d", iFiledes);
    }

    PAL_Terminate();
    return (PASS);
}
