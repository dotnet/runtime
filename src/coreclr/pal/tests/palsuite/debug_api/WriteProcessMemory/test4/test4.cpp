// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: test4.c
**
** Purpose: Create a child process and debug it.  When the child
** raises an exception, it sends back a memory location.  Call
** WriteProcessMemory on a restricted memory location and ensure that
** it fails.
**
**
**============================================================*/

#include <palsuite.h>
const int MY_EXCEPTION=999;

PALTEST(debug_api_WriteProcessMemory_test4_paltest_writeprocessmemory_test4, "debug_api/WriteProcessMemory/test4/paltest_writeprocessmemory_test4")
{

    PROCESS_INFORMATION pi;
    STARTUPINFO si;
    DEBUG_EVENT DebugEv;
    DWORD dwContinueStatus = DBG_CONTINUE;
    int Count, ret;
    char* DataBuffer[4096];
    char* Memory;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    ZeroMemory( &si, sizeof(si) );
    si.cb = sizeof(si);
    ZeroMemory( &pi, sizeof(pi) );

    memset(DataBuffer, 'z', 4096);

    /* Create a new process.  This is the process to be Debugged */
    if(!CreateProcess( NULL, "helper", NULL, NULL,
                       FALSE, 0, NULL, NULL, &si, &pi))
    {
        Fail("ERROR: CreateProcess failed to load executable 'helper'.  "
             "GetLastError() returned %d.\n",GetLastError());
    }

    /* Call DebugActiveProcess, because the process wasn't created as a
       debug process.
    */
    if(DebugActiveProcess(pi.dwProcessId) == 0)
    {
        Fail("ERROR: Failed calling DebugActiveProcess on the process "
             "which was created to debug.  GetLastError() returned %d.\n",
             GetLastError());
    }


    /* Call WaitForDebugEvent, which will wait until the helper process
       raises an exception.
    */

    while(1)
    {
        if(WaitForDebugEvent(&DebugEv, INFINITE) == 0)
        {
            Fail("ERROR: WaitForDebugEvent returned 0, indicating failure.  "
                 "GetLastError() returned %d.\n",GetLastError());
        }

        /* We're waiting for the helper process to send this exception.
           When it does, we call WriteProcess.  If it gets called more than
           once, it is ignored.
        */

        if(DebugEv.u.Exception.ExceptionRecord.ExceptionCode == MY_EXCEPTION)
        {

            Memory = (LPVOID)
                DebugEv.u.Exception.ExceptionRecord.ExceptionInformation[0];

            /* Write to this memory which we have no access to. */

            ret = WriteProcessMemory(pi.hProcess,
                                     Memory,
                                     DataBuffer,
                                     4096,
                                     &Count);

            if(ret != 0)
            {
                Fail("ERROR: WriteProcessMemory should have failed, as "
                     "it attempted to write to a range of memory which was "
                     "not accessible.\n");
            }

            if(GetLastError() != ERROR_NOACCESS)
            {
                Fail("ERROR: GetLastError() should have returned "
                     "ERROR_NOACCESS , but instead it returned "
                     "%d.\n",GetLastError());
            }
        }

        if(DebugEv.dwDebugEventCode == EXIT_PROCESS_DEBUG_EVENT)
        {
            break;
        }

        if(ContinueDebugEvent(DebugEv.dwProcessId,
                              DebugEv.dwThreadId, dwContinueStatus) == 0)
        {
            Fail("ERROR: ContinueDebugEvent failed to continue the thread "
                 "which had a debug event.  GetLastError() returned %d.\n",
                 GetLastError());
        }
    }


    PAL_Terminate();
    return PASS;
}
