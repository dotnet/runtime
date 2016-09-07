// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test4.c
**
** Purpose: Pass an invalid handle to UnlockFile.  Pass a null handle to 
** UnlockFile.  Create a file and lock two consecuative regions and call 
** UnlockFile on the whole region (this should fail, see msdn)
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

    if(CloseHandle(TheFile) == 0)
    {
        Fail("ERROR: CloseHandle failed to close the file.\n");
    }


    /* Test an invalid handle and a NULL handle */
    if(UnlockFile(TheFile, 0, 0, 0, 0) != 0)
    {
        Fail("ERROR: Called UnlockFile on an invalid HANDLE and it "
             "returned a success value.\n");
    }

    if(UnlockFile(NULL, 0, 0, 0, 0) != 0)
    {
        Fail("ERROR: Called UnlockFile with NULL passed for the HANDLE and "
             "it returned a success value.\n");
    }

    /* Re-open the file */
    TheFile = CreateFile(fileName,     
                         GENERIC_READ|GENERIC_WRITE, 
                         FILE_SHARE_READ|FILE_SHARE_WRITE,
                         NULL,                          
                         OPEN_ALWAYS,                 
                         FILE_ATTRIBUTE_NORMAL, 
                         NULL);

    if (TheFile == INVALID_HANDLE_VALUE) 
    { 
        Fail("ERROR: Could not open file '%s' with CreateFile. "
             "GetLastError() returned %d.\n",fileName,GetLastError()); 
    }

    /* Lock two consecuative regions of this file */
    if(LockFile(TheFile, 0, 0, 5, 0) == 0)
    {
        Trace("ERROR: LockFile failed attempting to lock bytes 0-4. "
             "GetLastError() returned %d.\n",GetLastError());

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }
        Fail("");
    }

    if(LockFile(TheFile, 5, 0, 5, 0) == 0)
    {
        Fail("ERROR: LockFile failed attempting to lock bytes 5-9.  "
             "GetLastError() returned %d.\n",GetLastError());

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
    }
        Fail("");
    }

    /* Attempt to unlock the entire region which was locked with one 
       call to UnlockFile.  This should fail.
    */
    if(UnlockFile(TheFile, 0, 0, 10, 0) != 0)
    {
        Fail("ERROR: Called UnlockFile on bytes 0-9 which were locked with "
             "two seperate LockFile calls.  This should have failed.  "
             "UnlockFile will not unlock consecuative locked regions.\n");

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
    }
        Fail("");
    }


    /* Now, unlock the regions one at a time. */
    if(UnlockFile(TheFile, 0, 0, 5, 0) == 0)
    {
        Fail("ERROR: UnlockFile failed when attempting to unlock bytes "
             "0-4 of the file.  GetLastError() returned %d.\n",GetLastError());

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }
        Fail("");
    }

    if(UnlockFile(TheFile, 5, 0, 5, 0) == 0)
    {
        Fail("ERROR: UnlockFile failed when attempting to unlock bytes "
             "5-9 of the file.  GetLastError() returned %d.\n",GetLastError());

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

