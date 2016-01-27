// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test3.c
**
** Purpose: 
** Lock a portion of the file with the parent.  Then have the child lock 
** another portion.  Have the child attempt to call Unlock on the parent's 
** locked data, and the parent do the same to the child.  Ensure that the 
** locks are respected.
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
    
    /* When the child proccess is finished setting its lock and testing the
       parent lock, then the parent can test the child's lock.
    */

    if(UnlockFile(TheFile, 10, 0, 10, 0) != 0)
    {
        Trace("ERROR: The parent proccess called Unlock on the child "
              "proccesses lock, and the function returned non-zero, when "
              "it should have failed.\n");
        ParentRetCode = 1;
    }
    
    /* Switch back to the child so that it can unlock its portion and 
       cleanup.
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
       the file, and lock the file from bytes 0-9.
    */
    TheFile = CreateAndLockFile(TheFile, FILENAME, WriteBuffer, 
                                0, 10);
    
    /* Run the test.  Better errors are displayed by Trace throughout. */
    if(RunTest(HELPER, TheFile, WaitFile))
    {
        Fail("ERROR: The test to check that the Unlock will not work on " 
             "on locks set by other proccesses failed.\n");
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
