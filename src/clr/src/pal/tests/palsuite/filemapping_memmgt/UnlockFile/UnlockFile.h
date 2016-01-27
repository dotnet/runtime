// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: UnLockFile.h
**
** Purpose: This header file has a RunHelper method which will be used to 
** start a child proccess in many LockFile testcases.  The CreateAndLockFile
** method Creates a file and calls LockFile upon it.  And the two Signal
** methods are used for IPC.
**
**
**============================================================*/

#include <palsuite.h>

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
             "GetLastError() returned %d.\n",FileName,GetLastError()); 
    } 

    if(WriteFile(TheFile, WriteBuffer,
                 strlen(WriteBuffer),&BytesWritten, NULL) == 0)
    {
        Fail("ERROR: WriteFile has failed.  It returned 0 when we "
             "attempted to write to the file '%s'.  GetLastError() "
             "returned %d.\n",FileName,GetLastError());
    }
    
    if(FlushFileBuffers(TheFile) == 0)
    {
        Fail("ERROR: FlushFileBuffers returned failure. GetLastError() "
             "returned %d.\n",GetLastError());
    }
    
    if(LockFile(TheFile, LockStart, 0, LockLength, 0) == 0)
    {
        Fail("ERROR: LockFile failed.  GetLastError returns %d.\n",
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
             "end of the file.  GetLastError() returned %d.\n",GetLastError());
    }
    
    if(WriteFile(TheFile, "x", 1,&BytesWritten, NULL) == 0)
    {
        Fail("ERROR: WriteFile was unable to write to the WaitFile.  "
             "GetLastError() returned %d.\n",GetLastError());
    }

    if(FlushFileBuffers(TheFile) == 0)
    {
        Fail("ERROR: FlushFileBuffers failed when flushing the WaitFile. "
             "GetLastError() returned %d.\n");
    }
    
    while(GetFileSize(TheFile,NULL) == size) {}
}

void SignalFinish(HANDLE TheFile)
{
    DWORD BytesWritten;
    
    if(SetFilePointer(TheFile, 0, NULL, FILE_END) == INVALID_SET_FILE_POINTER)
    {
        Fail("ERROR: SetFilePointer was unable to set the pointer to the "
             "end of the WaitFile.  GetLastError() returned %d.\n",
             GetLastError());
    }
    
    if(WriteFile(TheFile, "x", 1,&BytesWritten, NULL) == 0)
    {
        Fail("ERROR: WriteFile was unable to write to the WaitFile. "
             "GetLastError returned %d.\n",GetLastError());
    }

    if(FlushFileBuffers(TheFile) == 0)
    {
        Fail("ERROR: FlushFileBuffers failed when flushing the WaitFile. "
             "GetLastError() returned %d.\n");
    }

}
