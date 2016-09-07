// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
** 
** Source:  test7.c (DuplicateHandle)
**
** Purpose: Tests the PAL implementation of the DuplicateHandle function,
**          with a handle from CreateThread. The test will create a thread
**          handle and its duplicate. Then get the priorities of the threads,
**          set the priority of one and the change should be seen in the
**          other.
**
**
**===================================================================*/

#include <palsuite.h>

DWORD PALAPI CreateTestThread(LPVOID lpParam);

int __cdecl main(int argc, char* argv[])
{
    HANDLE  hThread;  
    HANDLE  hDupThread;  
    DWORD   dwThreadId = 0;
    LPTHREAD_START_ROUTINE lpStartAddress =  &CreateTestThread;

    int threadPriority;
    int duplicatePriority;
    int finalPriority;

    /* Initialize the PAL.*/
    if ((PAL_Initialize(argc, argv)) != 0)
    {
        return (FAIL);
    }
    
    /* Create a thread.*/
    hThread = CreateThread(NULL,            /* SD*/
                          (DWORD)0,         /* initial stack size*/
                          lpStartAddress,   /* thread function*/
                          NULL,             /* thread argument*/
                          (DWORD)0,         /* creation option*/
                          &dwThreadId);     /* thread identifier*/
    if (hThread == NULL)
    {
        Fail("ERROR:%u: Unable to create thread.\n",
             GetLastError());
    }

    /* Duplicate the thread handle.*/
    if (!(DuplicateHandle(GetCurrentProcess(),       /* source handle process*/
                          hThread,                   /* handle to duplicate*/
                          GetCurrentProcess(),       /* target process handle*/
                          &hDupThread,               /* duplicate handle*/
                          (DWORD)0,                  /* requested access*/
                          FALSE,                     /* handle inheritance*/
                          DUPLICATE_SAME_ACCESS)))   /* optional actions*/
    {
        Trace("ERROR: %ld :Fail to create the duplicate handle"
              " to hThread=0x%lx",
              GetLastError(),
              hThread);
        CloseHandle(hThread);
        Fail("");
    }

    /* Get the priority of the thread.*/
    threadPriority = GetThreadPriority(hThread);
    if(threadPriority != 0)
    {
        Trace("ERROR: Thread priority of hThread=0x%lx should be "
             "set to normal THREAD_PRIORITY_NORMAL=%d\n",
             hThread,
             THREAD_PRIORITY_NORMAL);
        CloseHandle(hThread);
        CloseHandle(hDupThread);
        Fail("");
    }

    /* Get the priority of the duplicated handle, and compare it to
     * the priority of the original thread. Should be the same.*/
    duplicatePriority = GetThreadPriority(hThread);
    if(duplicatePriority != threadPriority)
    {
        Trace("ERROR: Expected priority of hThread=0x%lx and hDupThread=0x%lx"
             " to be the same. Priorities:hThread=\"%d\":hDupThread=\"%d\"\n",
             hThread,
             hDupThread,
             threadPriority,
             duplicatePriority);
        CloseHandle(hThread);
        CloseHandle(hDupThread);
        Fail("");
    }

    /* Set the priority of the duplicate thread.*/
    if(!SetThreadPriority (hDupThread,THREAD_PRIORITY_HIGHEST))
    {
        Trace("ERROR:%u: SetThreadPriority failed on hThread=0x%lx\n",
             GetLastError(),
             hDupThread);
        CloseHandle(hThread);
        CloseHandle(hDupThread);
        Fail("");
    }

    /* Get the priority of the origianl thread, and
     * compare it to what the duplicate was set to.*/
    finalPriority = GetThreadPriority(hThread);
    if (finalPriority != THREAD_PRIORITY_HIGHEST)
    {
        Trace("ERROR: Expected priority of hThread=0x%lw and "
             "hDupThread=0x%lw to be set the same. Priorities:"
             "hThread=\"%d\":hDupThread=\"%d\".\n",
             hThread,
             hDupThread,
             threadPriority,
             duplicatePriority);
        CloseHandle(hThread);
        CloseHandle(hDupThread);
        Fail("");
    }

    /* Wait on the original thread.*/
    if((WaitForSingleObject(hThread, 100)) != WAIT_OBJECT_0)
    {
        Trace("ERROR:%u: hThread=0x%lx is in a non-signalled "
              "mode, yet created signalled.\n",
              GetLastError(),
              hThread);
        CloseHandle(hThread);
        CloseHandle(hDupThread);
        Fail("");
    }

    /* Clean-up thread and Terminate the PAL.*/
    CloseHandle(hThread);
    CloseHandle(hDupThread);
    PAL_Terminate();
    return PASS;
}

/*Thread testing function, only return '0'*/
DWORD PALAPI CreateTestThread(LPVOID lpParam)
{
    return (DWORD)0;
}
