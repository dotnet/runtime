// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: helper.c
**
** Purpose: A child process which will lock a portion of the file,
** then try to unlock a portion of the file which was locked by the parent.
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
    
    /* Lock a section of the file different from which was locked in the
       parent proccess 
    */
    if(LockFile(TheFile, 10, 0, 10, 0) == 0)
    {
        Trace("ERROR: The LockFile call within the child failed to lock "
              "the file.  GetLastError() returned %d.\n",GetLastError());
        result = 1;
    }
    
    /* Attempt to unlock the portion of the file which was locked within the
       parent process.
    */
    if(UnlockFile(TheFile, 0, 0, 10, 0) != 0)
    {
        Trace("ERROR: The UnlockFile call within the child succeeded in "
              "calling UnlockFile on the portion of the file which was "
              "locked by the parent.\n");
        result = 1;
    }

    // Sleep for a bit to give the parent a chance to block before we do.
    Sleep(1000);

    /* Switch back to the parent, so it can check the child lock */
    SignalAndBusyWait(WaitFile);

    /* Finally, clean up the lock which was done within this proccess and 
       exit.
    */
    if(UnlockFile(TheFile, 10, 0, 10, 0) == 0)
    {
        Trace("ERROR: The UnlockFile call within the child failed to unlock "
              "the portion of the file which was locked by the child.  "
              "GetLastError() returned %d.\n", GetLastError());
        result = 1;
    }

    PAL_TerminateEx(result);
    return result;
}
