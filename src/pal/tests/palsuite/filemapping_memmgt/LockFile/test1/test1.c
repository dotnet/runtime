// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test1.c
**
** Purpose: Open a file, and lock it from start to EOF.  Then create a 
** new process, which will attempt to Read and Write from the file.  Check
** to ensure both of these operations fail.
**
**
**============================================================*/

#include <palsuite.h>
#include "../LockFile.h"

#define HELPER "helper"

int __cdecl main(int argc, char *argv[])
{
    
    HANDLE TheFile;
    DWORD FileStart = 0;
    DWORD FileEnd = 0; 
    const char lpBuffer[] = "This is a test file.";
    DWORD bytesWritten;
    BOOL bRc = TRUE;
    char fileName[] = "testfile.tmp";
    
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
    
    /* Important to have sharing enabled, or there is no need for the lock. */
    TheFile = CreateFile(fileName,
                         GENERIC_READ|GENERIC_WRITE,
                         FILE_SHARE_READ|FILE_SHARE_WRITE,
                         NULL,
                         CREATE_ALWAYS,                 
                         FILE_ATTRIBUTE_NORMAL, 
                         NULL);
    
    if (TheFile == INVALID_HANDLE_VALUE) 
    { 
        Fail("ERROR: Could not open file '%s' with CreateFile.",fileName); 
    } 

    bRc = WriteFile(TheFile,
                    lpBuffer,
                    (DWORD)sizeof(lpBuffer),
                    &bytesWritten,
                    NULL);

    if(!bRc)
    {
        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }
        
        Fail("ERROR: Could not write to file '%s' with WriteFile.",fileName);
    }
    else if(bytesWritten != (DWORD)sizeof(lpBuffer))
    {
        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }        
        
        Fail("ERROR: Could not write the correct number of bytes to the "
        "file '%s' with WriteFile.",fileName);
    }

    /* Find the value for the End of the file */
    FileEnd = SetFilePointer(TheFile,0,NULL,FILE_END);
    
    if(FileEnd == INVALID_SET_FILE_POINTER)
    {
        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }

        Fail("ERROR: Could not set the file pointer to the end of the file "
             "using SetFilePointer.  It returned INVALID_SET_FILE_POINTER.");
    }
    
    /* Lock the file from Start to EOF */
    
    if(LockFile(TheFile, FileStart, 0, FileEnd, 0) == 0)
    {
        Trace("ERROR: LockFile failed.  GetLastError returns %d.",
             GetLastError());
        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }
        Fail("");
    }
    
    /* Launch another process, which will attempt to read and write from
       the locked file.
       
       If the helper program returns 1, then the test fails. More 
       specific errors are given by the Helper file itself.
    */
    if(RunHelper(HELPER))
    {
        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }
        
        Fail("ERROR: The Helper program determined that the file was not "
             "locked properly by LockFile.");
    }

    if(UnlockFile(TheFile, FileStart, 0, FileEnd, 0) == 0)
    {
        Trace("ERROR: UnlockFile failed.  GetLastError returns %d.",
             GetLastError());
        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }
        Fail("");
    }
    
    if(CloseHandle(TheFile) == 0)
    {
        Fail("ERROR: CloseHandle failed to close the file.");
    }
  
    PAL_Terminate();
    return PASS;
}

