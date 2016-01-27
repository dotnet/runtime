// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test1.c
**
** Purpose: 
** Have the parent Lock a file, then have the child check the lock, then
** have the parent unlock the file, and the child check again.  
** This requires some IPC, which is done here with a crude busy wait on a 
** file (waiting for the file size to change) to avoid too many more
** dependencies.
**
**
**============================================================*/

#include <palsuite.h>
#include "../UnlockFile.h"

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
    DWORD FileEnd;
        
    ZeroMemory( &si, sizeof(si) );
    si.cb = sizeof(si);
    ZeroMemory( &pi, sizeof(pi) );
    
    /* Load up the helper Process, and then Wait until it signals that it
       is finished locking.
    */
    if(!CreateProcess( NULL, Helper, NULL,
                       NULL, FALSE, 0,
                       NULL, NULL, &si, &pi)) 
    {
        Fail("ERROR: CreateProcess failed to load executable '%s'.\n",Helper);
    }

    SignalAndBusyWait(WaitFile);
    
    /* When the child proccess is finished verifying the lock, find the end 
       of the file and unlock the file.
    */

    FileEnd = SetFilePointer(TheFile, 0, NULL, FILE_END);

    if(FileEnd == INVALID_SET_FILE_POINTER)
    {
        Trace("ERROR: SetFilePointer failed to set the file pointer to the "
              "end of the file.  GetLastError() returned %d.\n",
              GetLastError());
        ParentRetCode = 1;
    }

    if(UnlockFile(TheFile, 0, 0, FileEnd, 0) == 0)
    {
        Trace("ERROR: The call to UnlockFile returned 0 when attempting to "
              "unlock the file within the parent. This should have "
              "succeeded.  GetLastError returned %d.\n",GetLastError());
        ParentRetCode = 1;
    }
    
    /* Switch back to the child so that it can ensure the unlock worked 
       properly.
    */

    SignalFinish(WaitFile);
    WaitForSingleObject(pi.hProcess,INFINITE);
  
    /* Get the return value from the helper process */
    if (GetExitCodeProcess(pi.hProcess, &ChildRetCode) == 0)
    {
        Fail("ERROR: GetExitCodeProccess failed when attempting to retrieve "
             "the exit code of the child process.\n");
    }

    if(CloseHandle( pi.hProcess ) == 0) 
    {
        Fail("ERROR: CloseHandle failed to close the process.\n");
    }

    if(CloseHandle( pi.hThread ) == 0)
    {
        Fail("ERROR: CloseHandle failed to close the thread.\n");
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
             "GetLastError() returned %d.\n",WAITFILENAME,GetLastError()); 
    }
    
    /* Call the helper function to Create a file, write 'WriteBuffer' to
       the file, and lock the file from start to end.
    */
    TheFile = CreateAndLockFile(TheFile, FILENAME, WriteBuffer, 
                                0, strlen(WriteBuffer));
    
    /* Run the test.  Better errors are displayed by Trace throughout. */
    if(RunTest(HELPER, TheFile, WaitFile))
    {
        Fail("ERROR: Checking to ensure that Unlock successfully unlocked "
             "a file failed.\n");
    }
    
    if(CloseHandle(TheFile) == 0)
    {
        Fail("ERROR: CloseHandle failed to close the file used for "
             "testing the locks.  GetLastError() returns %d.\n",
             GetLastError());
    }

    if(CloseHandle(WaitFile) == 0)
    {
        Fail("ERROR: CloseHandle failed to close the wait file.  "
             "GetLastError() returns %d.\n",GetLastError());
    }
    
    PAL_Terminate();
    return PASS;
}
