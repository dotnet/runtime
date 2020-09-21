// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test8.c (DuplicateHandle)
**
** Purpose: Tests the PAL implementation of the DuplicateHandle function,
**          with a handle from GetCurrentThread. The test will create a thread
**          handle, get the current thread and its duplicate. Then get the
**          priorities of the threads, set the priority of one and the change
**          should be seen in the other.
**
**
**===================================================================*/

#include <palsuite.h>

DWORD PALAPI CreateTestThread_DuplicateHandle_test8(LPVOID lpParam);

PALTEST(threading_DuplicateHandle_test8_paltest_duplicatehandle_test8, "threading/DuplicateHandle/test8/paltest_duplicatehandle_test8")
{
    HANDLE  hThread;
    HANDLE  hCurrentThread;
    HANDLE  hDupThread;
    DWORD   dwThreadId = 0;
    LPTHREAD_START_ROUTINE lpStartAddress =  &CreateTestThread_DuplicateHandle_test8;

    int threadPriority;
    int duplicatePriority;
    int finalPriority;

    /* Initialize the PAL.*/
    if ((PAL_Initialize(argc, argv)) != 0)
    {
        return (FAIL);
    }

#if !HAVE_SCHED_OTHER_ASSIGNABLE
    /* Defining thread priority for SCHED_OTHER is implementation defined.
       Some platforms like NetBSD cannot reassign it as they are dynamic.
    */
    printf("paltest_duplicatehandle_test8 has been disabled on this platform\n");
#else

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

    /*Get a psuedo handle to the current thread.*/
    hCurrentThread = GetCurrentThread();

    /* Duplicate the psuedo thread handle.*/
    if (!(DuplicateHandle(GetCurrentProcess(),       /* source handle process*/
                          hCurrentThread,            /* handle to duplicate*/
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
    threadPriority = GetThreadPriority(hCurrentThread);
    if(threadPriority != 0)
    {
        Trace("ERROR: Thread priority of hCurrentThread=0x%lx should be "
             "set to normal THREAD_PRIORITY_NORMAL=%d\n",
             hCurrentThread,
             THREAD_PRIORITY_NORMAL);
        CloseHandle(hThread);
        CloseHandle(hDupThread);
        Fail("");
    }

    /* Get the priority of the duplicated handle, and compare it to
     * the priority of the original thread. Should be the same.*/
    duplicatePriority = GetThreadPriority(hCurrentThread);
    if(duplicatePriority != threadPriority)
    {
        Trace("ERROR: Expected priority of hCurrentThread=0x%lx and "
             "hDupThread=0x%lx to be the same. Priorities:hThread="
             "\"%d\":hDupThread=\"%d\"\n",
             hCurrentThread,
             hDupThread,
             threadPriority,
             duplicatePriority);
        CloseHandle(hThread);
        CloseHandle(hDupThread);
        Fail("");
    }

    /* Set the priority of the original thread.*/
    if(!SetThreadPriority (hCurrentThread,THREAD_PRIORITY_HIGHEST))
    {
        Trace("ERROR:%u: SetThreadPriority failed on hCurrentThread=0x%lx\n",
             GetLastError(),
             hCurrentThread);
        CloseHandle(hThread);
        CloseHandle(hDupThread);
        Fail("");
    }

    /* Get the priority of the duplicate thread, and
     * compare it to what the original was set to.*/
    finalPriority = GetThreadPriority(hDupThread);
    if (finalPriority != THREAD_PRIORITY_HIGHEST)
    {
        Trace("ERROR: Expected priority of hCurrentThread=0x%lw and "
             "hDupThread=0x%lw to be set the same. Priorities:"
             "hCurrentThread=\"%d\":hDupThread=\"%d\".\n",
             hCurrentThread,
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
        Trace("ERROR:%u: hCurrentThread=0x%lx is in a non-signalled "
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

#endif

    PAL_Terminate();
    return PASS;
}

/*Thread testing function, only return '0'*/
DWORD PALAPI CreateTestThread_DuplicateHandle_test8(LPVOID lpParam)
{
    return (DWORD)0;
}
