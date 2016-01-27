// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test3.c
**
** Purpose: Open a file, lock a region in the middle.  Create a new process
** and attempt to read and write directly before and after that region, which
** should succeed.  Also, check to see that reading/writing in the locked
** region fails.
**
**
**============================================================*/

#include <palsuite.h>
#include "../LockFile.h"

#define HELPER "helper"
#define FILENAME "testfile.txt"

int __cdecl main(int argc, char *argv[])
{
    
    HANDLE TheFile = NULL;
    DWORD FileStart = 0;
    DWORD FileEnd = 0;
    char* WriteBuffer = "12345678901234567890123456"; 
    
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
    
    /* Call the helper function to Create a file, write 'WriteBuffer' to
       the file, and lock the file.
    */
    
    FileEnd = strlen(WriteBuffer);
    TheFile = CreateAndLockFile(TheFile,FILENAME, WriteBuffer, 
                                FileStart+3, FileEnd-6);
    
    
    /* Launch another process, which will attempt to read and write from
       the locked file.
       
       If the helper program returns 1, then the test fails. More 
       specific errors are given by the Helper file itself.
    */
    if(RunHelper(HELPER))
    {
        Fail("ERROR: The Helper program determined that the file was not "
             "locked properly by LockFile.");
    }

    if(UnlockFile(TheFile, FileStart+3, 0, FileEnd-6, 0) == 0)
    {
        Fail("ERROR: UnlockFile failed.  GetLastError returns %d.",
             GetLastError());
    }
    
    if(CloseHandle(TheFile) == 0)
    {
        Fail("ERROR: CloseHandle failed to close the file. "
             "GetLastError() returned %d.",GetLastError());
    }
    
    PAL_Terminate();
    return PASS;
}
