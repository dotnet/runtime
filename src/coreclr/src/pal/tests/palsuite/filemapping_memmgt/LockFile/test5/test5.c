// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test5.c
**
** Purpose: 
** Have two processes obtain a lock on a single file, but in different
** regions of the file.  Use Read/Write to ensure the locks are respected.
** This requires some IPC, which is done here with a crude busy wait on a 
** file (waiting for the file size to change) to avoid too many more
** dependencies.
**
**
**============================================================*/

#include <palsuite.h>
#include "../LockFile.h"

#define HELPER "helper"
#define FILENAME "testfile.txt"
#define WAITFILENAME "waitfile"
#define BUF_SIZE 128

int RunTest(char* Helper, HANDLE TheFile, HANDLE WaitFile) 
{
    STARTUPINFO si;
    PROCESS_INFORMATION pi;
    DWORD ChildRetCode = 0;
    DWORD ParentRetCode = 0;
    DWORD BytesRead;
    char DataBuffer[BUF_SIZE];
    
    
    ZeroMemory( &si, sizeof(si) );
    si.cb = sizeof(si);
    ZeroMemory( &pi, sizeof(pi) );
    
    /* Load up the helper Process, and then Wait until it signals that it
       is finished locking.
    */
    if(!CreateProcess( NULL,Helper,NULL,NULL,FALSE,0,NULL,NULL,&si,&pi)) 
    {
        Fail("ERROR: CreateProcess failed to load executable '%s'.",Helper);
    }

    SignalAndBusyWait(WaitFile);
   
    /* Now the child proccess has locked another section of the file, from
       bytes 11 through 20.  Let's check that the parent lock is still ignored
       by the parent proccess and that the child's lock is respected.
    */

    if(ReadFile(TheFile, DataBuffer, 10, &BytesRead, NULL) == 0)
    {
        Trace("ERROR: ReadFile failed when attempting to read a section of "
              "the file which was locked by the current process.  It should "
              "have been able to read this.  GetLastError() returned %d.",
              GetLastError());
        ParentRetCode = 1;
    }

    SetFilePointer(TheFile, 11, 0, FILE_BEGIN);

    if(ReadFile(TheFile, DataBuffer, 10, &BytesRead, NULL) != 0)
    {
        Trace("ERROR: ReadFile returned success when it should "
              "have failed.  Attempted to read 10 bytes of the file which "
              "were locked by the child.");
        ParentRetCode = 1;
    }

    /* We're finished testing.  Let the child proccess know so it can clean
       up, and the parent will wait until it is done.
    */
    SignalFinish(WaitFile);
    WaitForSingleObject(pi.hProcess,INFINITE);
  
    /* Get the return value from the helper process */
    if (GetExitCodeProcess(pi.hProcess, &ChildRetCode) == 0)
    {
        Fail("ERROR: GetExitCodeProccess failed when attempting to retrieve "
             "the exit code of the child process.");
    }

    if(CloseHandle( pi.hProcess ) == 0) 
    {
        Fail("ERROR: CloseHandle failed to close the process.");
    }

    if(CloseHandle( pi.hThread ) == 0)
    {
        Fail("ERROR: CloseHandle failed to close the thread.");
    }

    return (ChildRetCode || ParentRetCode);
}

int __cdecl main(int argc, char *argv[])
{
    HANDLE TheFile = NULL;
    HANDLE WaitFile = NULL;
    char* WriteBuffer = "12345678901234567890123456"; 
   
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Open up the file we'll be using for some crude IPC */ 
    WaitFile = CreateFile(WAITFILENAME,     
                          GENERIC_READ|GENERIC_WRITE, 
                          FILE_SHARE_READ|FILE_SHARE_WRITE,
                          NULL,                          
                          CREATE_ALWAYS,                 
                          FILE_ATTRIBUTE_NORMAL, 
                          NULL);
    
    if (WaitFile == INVALID_HANDLE_VALUE) 
    { 
        Fail("ERROR: Could not open file '%s' with CreateFile. "
             "GetLastError() returned %d.",WAITFILENAME,GetLastError()); 
    }
    
    /* Call the helper function to Create a file, write 'WriteBuffer' to
       the file, and lock the file from bytes 0 to 10.
    */
    TheFile = CreateAndLockFile(TheFile, FILENAME, WriteBuffer, 
                                0, 10);
    
    /* Run the test.  Better errors are displayed by Trace throughout. */
    if(RunTest(HELPER, TheFile, WaitFile))
    {
        Fail("ERROR: Attempting to have two processes lock different "
             "sections of the same file has failed.");
    }

    /* Unlock the first 10 bytes which were locked by the parent proccess */
    if(UnlockFile(TheFile, 0, 0, 10, 0) == 0)
    {
        Fail("ERROR: Failed to Unlock the first 10 bytes of the file.  "
             "GetLastError returned %d.",GetLastError());
    }
    
    if(CloseHandle(TheFile) == 0)
    {
        Fail("ERROR: CloseHandle failed to close the file used for "
             "testing the locks.  GetLastError() returns %d.",GetLastError());
    }

    if(CloseHandle(WaitFile) == 0)
    {
        Fail("ERROR: CloseHandle failed to close the wait file.  "
             "GetLastError() returns %d.",GetLastError());
    }
    
    PAL_Terminate();
    return PASS;
}
