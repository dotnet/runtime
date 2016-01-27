// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test2.c
**
** Purpose: Open a file, and call Unlock on the file, even though it has yet
** to be locked.  Then lock a portion of the file, and attempt to call unlock
** on a larger portion of the file.  Also, try to unlock a smaller portion
** than was locked.
**
**
**============================================================*/

#include <palsuite.h>
#include "../UnlockFile.h"

int __cdecl main(int argc, char *argv[])
{
    HANDLE TheFile = NULL;
    const char lpBuffer[] = "This is a test file.";
    DWORD bytesWritten;
    BOOL bRc = TRUE;
    char fileName[] = "testfile.tmp";

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Open a file which is in the directory */
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
             "GetLastError() returned %d.\n",fileName,GetLastError()); 
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

    /* Call unlock file on an unlocked file, this should return 0 */
    if(UnlockFile(TheFile, 0, 0, 5, 0) != 0)
    {
        Trace("ERROR: Attempted to unlock a file which was not locked and "
             "the UnlockFile call was successful.\n");

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }
        Fail("");
    }

    /* Lock the file */
    if(LockFile(TheFile, 0, 0, 5, 0) == 0)
    {
        Trace("ERROR: Failed to call LockFile on a valid file handle.  "
             "GetLastError returned %d.\n",GetLastError());

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
    }
        Fail("");
    }

    /* Try to unlock more of the file than was locked by LockFile */
    if(UnlockFile(TheFile, 0, 0, 10, 0) != 0)
    {
        Trace("ERROR: Attempted to unlock bytes 0 to 9, but only bytes "
             "0 to 4 are locked.  But, UnlockFile was successful, when it "
             "should have failed.\n");

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
    }
        Fail("");
    }

    /* Try to unlock less of the file than was locked by LockFile */
    if(UnlockFile(TheFile, 0, 0, 3, 0) != 0)
    {
        Trace("ERROR: Attempted to unlock bytes 0 to 2, but the bytes 0 to "
             "4 were locked by LockFile.  Unlockfile should have failed "
             "when attempting this operation.\n");

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }
        Fail("");
    }
    
    /* Properly unlock the file */
    if(UnlockFile(TheFile, 0, 0, 5, 0) == 0)
    {
        Trace("ERROR: UnlockFile failed to unlock bytes 0 to 4 of the file. "
             "GetLastError returned %d.\n",GetLastError());

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }
        Fail("");
    }
    
    if(CloseHandle(TheFile) == 0)
    {
        Fail("ERROR: CloseHandle failed to close the file.\n");
    }
  
    PAL_Terminate();
    return PASS;
}

