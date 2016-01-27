// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test7.c
**
** Purpose: Try locking an invalid HANDLE and a NULL Handle.
**
**
**============================================================*/

#include <palsuite.h>
#include "../LockFile.h"

int __cdecl main(int argc, char *argv[])
{
    
    HANDLE TheFile = NULL;
    DWORD FileEnd = 0;
    const char lpBuffer[] = "This is a test file.";
    DWORD bytesWritten;
    BOOL bRc = TRUE;
    char fileName[] = "testfile.tmp";

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    TheFile = CreateFile(fileName,     
                         GENERIC_READ|GENERIC_WRITE, 
                         FILE_SHARE_READ|FILE_SHARE_WRITE,
                         NULL,                          
                         CREATE_ALWAYS,                 
                         FILE_ATTRIBUTE_NORMAL, 
                         NULL);
  
    if (TheFile == INVALID_HANDLE_VALUE) 
    { 
        Fail("ERROR: Could not open file '%s' with CreateFile. "
             "GetLastError() returned %d.",fileName,GetLastError()); 
    } 
    
    bRc = WriteFile(
                TheFile,                 // handle to file
                lpBuffer,                // data buffer
                (DWORD)sizeof(lpBuffer),        // number of bytes to write
                &bytesWritten,             // number of bytes written
                NULL                     // overlapped buffer
    );

    if(!bRc)
    {      
        Trace("ERROR: Could not write to file '%s' with WriteFile.",fileName);

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }
        Fail("");

    }
    else if(bytesWritten != (DWORD)sizeof(lpBuffer))
    {
        Trace("ERROR: Could not write the correct number of bytes to the "
        "file '%s' with WriteFile.",fileName);

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }
        Fail("");
    }
    
    /* Attempt to lock a region of this file beyond EOF, to ensure this 
       doesn't cause an error.
    */
    FileEnd = SetFilePointer(TheFile, 0, NULL, FILE_END);

    if(LockFile(TheFile, FileEnd+10, 0, 10, 0) == 0)
    {
        Trace("ERROR: LockFile failed when attempting to lock a region "
             "beyond the EOF.  GetLastError() returned %d.",GetLastError());

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
    }
        Fail("");
    }

    if(UnlockFile(TheFile, FileEnd+10, 0, 10, 0) == 0)
    {
        Trace("ERROR: UnlockFile failed when attempting to unlock the region "
             "which was locked beyond the EOF.  GetLastError returned %d.",
             GetLastError());

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }
        Fail("");
    }
 
    if(CloseHandle(TheFile) == 0)
    {
        Fail("ERROR: Failed to call CloseHandle.  GetLastError "
             "returned %d.",GetLastError());
    }

    /* Attempt to call Lockfile on an HANDLE which has been closed.  This
       should fail.
    */
    if(LockFile(TheFile, 0, 0, 5, 0) != 0) 
    {
        Fail("ERROR: Attempted to Lock an invalid handle and the function "
             "returned success.");
    }

    /* Attempt to call Lockfile by passing it NULL for a handle.  This should 
       fail.
    */

    if(LockFile(NULL, 0, 0, 5, 0) != 0) 
    {
        Fail("ERROR: Attempted to Lock a NULL handle and the function "
             "returned success.");
    }

    PAL_Terminate();
    return PASS;
}

