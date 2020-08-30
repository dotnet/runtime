// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test2.c (fclose)
**
** Purpose: Tests the PAL implementation of the fclose function. 
**          fclose will be passed a closed file handle to make 
**          sure it handles it accordingly.
**
**
**===================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{
    HANDLE  hReadPipe   = NULL;
    HANDLE  hWritePipe  = NULL;
    BOOL    bRetVal     = FALSE;
    int     iFiledes    = 0;
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
    bRetVal = CreatePipe(&hReadPipe,       /* read handle */
                         &hWritePipe,      /* write handle */
                         &lpPipeAttributes,/* security attributes */
                         0);               /* pipe size */

    if (bRetVal == FALSE)
    {
        Fail("ERROR: Unable to create pipe; returned error code %ld"
            , GetLastError());
    }

    /*Get a file descriptor for the read pipe handle*/
    iFiledes = _open_osfhandle((long)hReadPipe,_O_RDONLY);

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

    /*Attempt to close the file stream*/
    if (fclose(fp) != 0)
    {
        Fail("ERROR: Unable to fclose file stream fp=0x%lx\n", fp);
    }

    PAL_Terminate();
    return (PASS);
}
