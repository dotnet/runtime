// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: helper.c
**
** Purpose: A child process which will attempt to read from the 
** locked file to ensure it is locked. After it has been unlocked, it
** will then read again to check that Unlock worked.
**
**
**============================================================*/

#include <palsuite.h>
#include "../UnlockFile.h"

#define FILENAME "testfile.txt"
#define WAITFILENAME "waitfile"
#define BUF_SIZE 128

int __cdecl main(int argc, char *argv[])
{
    HANDLE TheFile, WaitFile;
    int result = 0;
    char DataBuffer[BUF_SIZE];
    DWORD BytesRead;
    
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
    
    /* Open the same file that the parent has opened and locked */
    TheFile = CreateFile(FILENAME,     
                         GENERIC_READ|GENERIC_WRITE, 
                         FILE_SHARE_READ|FILE_SHARE_WRITE,
                         NULL,     
                         OPEN_EXISTING,                 
                         FILE_ATTRIBUTE_NORMAL, 
                         NULL);
    
    if (TheFile == INVALID_HANDLE_VALUE) 
    { 
        Trace("ERROR: Could not open file '%s' with CreateFile.\n",FILENAME); 
        result = 1;
    }
    
    /* Open up the WaitFile that we're using for IPC */
    WaitFile = CreateFile(WAITFILENAME,     
                          GENERIC_READ|GENERIC_WRITE, 
                          FILE_SHARE_READ|FILE_SHARE_WRITE,
                          NULL,                          
                          OPEN_ALWAYS,                 
                          FILE_ATTRIBUTE_NORMAL, 
                          NULL);
    
    if (WaitFile == INVALID_HANDLE_VALUE) 
    { 
        Trace("ERROR: Could not open file '%s' with CreateFile. "
              "GetLastError() returned %d.\n",WAITFILENAME,GetLastError()); 
        result = 1;
    }
    
    
    /* Check to ensure the parent lock is respected */
    if(ReadFile(TheFile, DataBuffer, 10, &BytesRead, NULL) != 0)
    {
        Trace("ERROR: ReadFile returned success when it should "
              "have failed.  Attempted to read the first 10 bytes "
              "of a file which was locked by the parent process.\n");
        result = 1;
    }

    // Sleep for a bit to give the parent a chance to block before we do.
    Sleep(1000);

    /* Switch back to the parent, so it can unlock the file */
    SignalAndBusyWait(WaitFile);

    if(ReadFile(TheFile, DataBuffer, 10, &BytesRead, NULL) == 0)
    {
        Trace("ERROR: ReadFile was unable to read from the file after it "
              "had been unlocked.  Attempted to read 10 bytes and ReadFile "
              "returned 0.  GetLastError() returned %d.\n",GetLastError());
        result = 1;
    }
    
    PAL_TerminateEx(result);
    return result;
}
