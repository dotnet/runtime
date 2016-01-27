// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: LockFile.h
**
** Purpose: This header file has a RunHelper method which will be used to 
** start a child proccess in many LockFile testcases.  The CreateAndLockFile
** method Creates a file and calls LockFile upon it.  And the two Signal
** methods are used for IPC.
**
**
**============================================================*/

#include <palsuite.h>

int RunHelper(char* Helper) 
{
    STARTUPINFO si;
    PROCESS_INFORMATION pi;
    DWORD RetCode;
    
    ZeroMemory( &si, sizeof(si) );
    si.cb = sizeof(si);
    ZeroMemory( &pi, sizeof(pi) );
    
    if(!CreateProcess( NULL,Helper,NULL,NULL,FALSE,0,NULL,NULL,&si,&pi)) 
    {
        Fail("ERROR: CreateProcess failed to load executable '%s'.",Helper);
    }
    
    if(WaitForSingleObject( pi.hProcess, INFINITE ) == WAIT_FAILED)
    {
        Fail("ERROR: WaitForSingleObject returned WAIT_FAILED when it was "
             "called.");
    }
    
    /* Get the return value from the helper process */
    if (GetExitCodeProcess(pi.hProcess, &RetCode) == 0)
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

    return RetCode;
}

HANDLE CreateAndLockFile(HANDLE TheFile, char* FileName, char* WriteBuffer, 
                         DWORD LockStart, DWORD LockLength) 
{
    DWORD BytesWritten;

    TheFile = CreateFile(FileName,     
                         GENERIC_READ|GENERIC_WRITE, 
                         FILE_SHARE_READ|FILE_SHARE_WRITE,
                         NULL,                          
                         CREATE_ALWAYS,                 
                         FILE_ATTRIBUTE_NORMAL, 
                         NULL);
    
    if (TheFile == INVALID_HANDLE_VALUE) 
    { 
        Fail("ERROR: Could not open file '%s' with CreateFile. "
             "GetLastError() returned %d.",FileName,GetLastError()); 
    } 

    if(WriteFile(TheFile, WriteBuffer,
                 strlen(WriteBuffer),&BytesWritten, NULL) == 0)
    {
        Fail("ERROR: WriteFile has failed.  It returned 0 when we "
             "attempted to write to the file '%s'.  GetLastError() "
             "returned %d.",FileName,GetLastError());
    }
    
    if(FlushFileBuffers(TheFile) == 0)
    {
        Fail("ERROR: FlushFileBuffers returned failure. GetLastError() "
             "returned %d.",GetLastError());
    }
    
    if(LockFile(TheFile, LockStart, 0, LockLength, 0) == 0)
    {
        Fail("ERROR: LockFile failed.  GetLastError returns %d.",
             GetLastError());
    }
    
    return TheFile;
}

void SignalAndBusyWait(HANDLE TheFile)
{
    int size;
    DWORD BytesWritten;

    size = GetFileSize(TheFile,NULL)+1;
    
    if(SetFilePointer(TheFile, 0, NULL, FILE_END) == INVALID_SET_FILE_POINTER)
    {
        Fail("ERROR: SetFilePointer was unable to set the pointer to the "
             "end of the file.  GetLastError() returned %d.",GetLastError());
    }
    
    if(WriteFile(TheFile, "x", 1,&BytesWritten, NULL) == 0)
    {
        Fail("ERROR: WriteFile was unable to write to the WaitFile.  "
             "GetLastError() returned %d.",GetLastError());
    }

    if(FlushFileBuffers(TheFile) == 0)
    {
        Fail("ERROR: FlushFileBuffers failed when flushing the WaitFile. "
             "GetLastError() returned %d.");
    }
    
    while(GetFileSize(TheFile,NULL) == size) { Sleep(100); }
}

void SignalFinish(HANDLE TheFile)
{
    DWORD BytesWritten;
    
    if(SetFilePointer(TheFile, 0, NULL, FILE_END) == INVALID_SET_FILE_POINTER)
    {
        Fail("ERROR: SetFilePointer was unable to set the pointer to the "
             "end of the WaitFile.  GetLastError() returned %d.",
             GetLastError());
    }
    
    if(WriteFile(TheFile, "x", 1,&BytesWritten, NULL) == 0)
    {
        Fail("ERROR: WriteFile was unable to write to the WaitFile. "
             "GetLastError returned %d.",GetLastError());
    }
    
    if(FlushFileBuffers(TheFile) == 0)
    {
        Fail("ERROR: FlushFileBuffers failed when flushing the WaitFile. "
             "GetLastError() returned %d.");
    }
}
