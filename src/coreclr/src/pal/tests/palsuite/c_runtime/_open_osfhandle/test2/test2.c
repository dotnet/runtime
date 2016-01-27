// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test2.c (_open_osfhandle)
**
** Purpose: Tests the PAL implementation of the _open_osfhandle function.
**          This API accepts a OS Handle returned from CreatePipe() and
**          a flag of _O_RDONLY and returns a C Run-Time handle. The test
**          will pass a NULL handle, and unsupported flags. All cases
**          should fail.
**
** Depends: CreatePipe
**          CloseHandle
**
**
**===================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{
    HANDLE  hReadPipe   = NULL;
    HANDLE  hWritePipe   = NULL;
    BOOL    bRetVal     = FALSE;
    int     iFiledes    = 0;
    
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
    
    /*Close write pipe handle*/
    if (CloseHandle(hWritePipe) == 0)
    {
        Fail("ERROR: Unable to close write pipe handle "
             "hWritePipe=0x%lx", hWritePipe);
    }
    
    /*Close read pipe handle*/
    if (CloseHandle(hReadPipe) == 0)
    {
        Fail("ERROR: Unable to close read pipe handle "
             "hReadPipe=0x%lx", hReadPipe);
    }
    
    /*Test with a Closed handle and supported flag _O_RDONLY*/
    iFiledes = _open_osfhandle((long)hReadPipe, _O_RDONLY);
    if (iFiledes != -1)
    {
        Fail("ERROR: _open_osfhandle successfullly opened "
             " hReadPipe which was closed, with _O_RDONLY");
    }

    /*Test with a NULL handle and supported flag _O_RDONLY*/
    hReadPipe = NULL;
    iFiledes = _open_osfhandle((long)hReadPipe, _O_RDONLY);
    if (iFiledes != -1)
    {
        Fail("ERROR: _open_osfhandle successfullly opened "
             " hReadPipe=NULL with _O_RDONLY");
    }
    
    PAL_Terminate();
    return (PASS);
}
