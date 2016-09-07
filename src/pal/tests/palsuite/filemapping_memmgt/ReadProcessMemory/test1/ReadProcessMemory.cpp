// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: ReadProcessMemory.c
**
** Purpose: Positive test the ReadProcessMemory API.
**          Call ReadProcessMemory to read memory contents
**          inside current process.
**
**
**============================================================*/
#include <palsuite.h>

#define REGIONSIZE 1024

int __cdecl main(int argc, char *argv[])
{
    int err;
    BOOL bResult;
    HANDLE ProcessHandle;
    DWORD ProcessID;
    LPVOID lpProcessAddress = NULL;
    char ProcessBuffer[REGIONSIZE];
    ULONG_PTR size = 0;


    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
       return FAIL;
    }
    
    /*retrieve the current process ID*/    
    ProcessID = GetCurrentProcessId();

    /*retrieve the current process handle*/
    ProcessHandle = OpenProcess(
                PROCESS_VM_READ,/*access flag*/
                FALSE,          /*not inherited*/
                ProcessID);
    
    if(NULL == ProcessHandle)
    {
        Fail("\nFailed to call OpenProcess API to retrieve "
                "current process handle error code=%u\n",
                GetLastError());
    }

    /*allocate the virtual memory*/
    lpProcessAddress = VirtualAlloc(
            NULL,            /*system determine where to allocate the region*/
            REGIONSIZE,      /*specify the size*/
            MEM_COMMIT,      /*allocation type*/
            PAGE_READONLY);  /*access protection*/

    if(NULL == lpProcessAddress)
    {
        Fail("\nFailed to call VirtualAlloc API to allocate "
                "virtual memory, error code=%u!\n", GetLastError());
    }

    /*zero the memory*/
    memset(ProcessBuffer, 0, REGIONSIZE);

    /*retrieve the memory contents*/
    bResult = ReadProcessMemory(
            ProcessHandle,         /*current process handle*/
            lpProcessAddress,      /*base of memory area*/
            (LPVOID)ProcessBuffer,
            REGIONSIZE,            /*buffer length in bytes*/
            &size);

    if(!bResult || REGIONSIZE != size)
    {
        Trace("\nFailed to call ReadProcessMemory API "
                "to retrieve the memory contents, error code=%u\n",
                GetLastError());

        err = CloseHandle(ProcessHandle);
        if(0 == err)
        {
            Trace("\nFailed to call CloseHandle API, error code=%u\n",
                GetLastError());
        }

        /*decommit the specified region*/
        err = VirtualFree(lpProcessAddress, REGIONSIZE, MEM_DECOMMIT);
        if(0 == err)
        {
            Trace("\nFailed to call VirtualFree API, error code=%u\n",
                GetLastError());
        }
        Fail("");
    }

    err = CloseHandle(ProcessHandle);
    if(0 == err)
    {
        Trace("\nFailed to call CloseHandle API, error code = %u\n",
                GetLastError());

        err = VirtualFree(lpProcessAddress, REGIONSIZE, MEM_DECOMMIT);
        if(0 == err)
        {
            Trace("\nFailed to call VirtualFree API, error code=%u\n",
                    GetLastError());
        }

        Fail("");
    }

    /*decommit the specified region*/
    err = VirtualFree(lpProcessAddress, REGIONSIZE, MEM_DECOMMIT);
    if(0 == err)
    {
        Fail("\nFailed to call VirtualFree API, error code=%u\n",
                GetLastError());
    }

    PAL_Terminate();
    return PASS;
}
