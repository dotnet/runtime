// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source:  CreateMutexW_ReleaseMutex/test1/CreateMutexW.c
**
** Purpose: This test case tests whether a Mutex object created
**          with CreateMutex really works by mutually excluding
**          threads from accessing a data structure at the same
**          time.  Here we have a buffer that can be filled or
**          emptied, we use a Mutex object to ensure that one
**          operation cannot be started until the other is
**          finished.  If one operation detects that the other
**          has not finished, it fails.  There is a Producer
**          thread which will try to fill the buffer 25 times,
**          and a consumer thread which try to empty the buffer
**          25 times.  If either the fill or empty operations
**          fails because the Mutex failed to mutually exclude
**          them, the corresponding thread will set an error
**          flag and return.  This will cause the test case to
**          fail.
**
**          To increase the probability of identifying problems,
**          the Fill operation has been slowed dowm with a call
**          to Sleep.  This ensures that one operation will try
**          to access the shared buffer while the other is in
**          progress.
**
**          NOTE: this test case also serves as a test case for
**          WaitForSingleObject.
**
**
** Dependencies: CreateThread
**               ReleaseMutex
**               WaitForSingleObject
**               WaitForMultipleObjects
**               Sleep
**               memset
**

**
**=========================================================*/

#define UNICODE
#include <palsuite.h>

/* Define some values that we will using many times */
#define MAIN_BUF_SIZE 40
#define NUM_OF_CYCLES 40

/* Buffer Operation return codes */
#define OP_OK   0
#define OP_ERR  1
#define OP_NONE 2


static HANDLE hMutex;      /* handle to mutex */

static BOOL bProdErr;      /* Producer error Flag */
static BOOL bConErr;       /* Consumer error Flag */

/* Test Buffer */
static char Buffer[MAIN_BUF_SIZE];

/*
 *  EmptyBuffer implements the empty operation for test buffer.
 */
int
EmptyBuffer()
{
    int i;

   if ( WaitForSingleObject(hMutex, INFINITE) == WAIT_FAILED)
   {
        Fail("ERROR: WaitForSingleObject failed.\n");
   }

    /* Check to see if the buffer is already completely empty */
    for (i=0; i<MAIN_BUF_SIZE && Buffer[i] == 0; i++);
    if (i == MAIN_BUF_SIZE)
    {
        /* Its empty so just return */
        if (ReleaseMutex(hMutex) == FALSE)
        {
            Fail("ERROR: ReleaseMutex Failed.\n");
        }
        return OP_NONE;
    }

    /* Its not empty so we must empty it. */
    for (i=0; i<MAIN_BUF_SIZE; i++)
    {
        /* Check for empty slots if we find one then the */
        /* fill operation did no finish.  return an error */
        if (Buffer[i] == 0)
        {
            if (ReleaseMutex(hMutex) == FALSE)
            {
                Fail("ERROR: ReleaseMutex Failed.\n");
            }
            return OP_ERR;
        }

        Buffer[i] = 0;
    }

    if (ReleaseMutex(hMutex) == FALSE)
    {
        Fail("ERROR: ReleaseMutex Failed.\n");
    }
    return OP_OK;
}

/*
 *  FillBuffer implements the fill operation for test buffer.
 */
int
FillBuffer()
{
    int i;

   if ( WaitForSingleObject(hMutex, INFINITE) == WAIT_FAILED)
   {
        Fail("ERROR: WaitForSingleObject failed.\n");
   }

    /* Check to see if the buffer is already completely full */
    for (i=0; i<MAIN_BUF_SIZE && Buffer[i] != 0; i++);
    if (i == MAIN_BUF_SIZE)
    {
        /* Its full so just return */
        if (ReleaseMutex(hMutex) == FALSE)
        {
            Fail("ERROR: ReleaseMutex Failed.\n");
        }
        return OP_NONE;
    }

    /* Its not full so we must fill it. */
    for (i=0; i<MAIN_BUF_SIZE; i++)
    {
        /* Check for filled slots if we find one then the */
        /* empty operation did not finish.  return an error */
        if (Buffer[i] == 1)
        {
            if (ReleaseMutex(hMutex) == FALSE)
            {
                Fail("ERROR: ReleaseMutex Failed.\n");
            }
            return OP_ERR;
        }

        Buffer[i] = 1;
        Sleep(10);
    }

    if (ReleaseMutex(hMutex) == FALSE)
    {
        Fail("ERROR: ReleaseMutex Failed.\n");
    }
    return OP_OK;
}




/*
 * Producer thread function.
 */
DWORD PALAPI Producer(LPVOID lpParam)
{
    int n = 0;
    int ret;

    while (n < NUM_OF_CYCLES)
    {
        if (bConErr == TRUE)
        {
            /* The consumer ran into an error so we'll stop */
            return 0;
        }

        ret = FillBuffer();

        if (ret == OP_OK)
        {
            n++;
        }
        else if (ret == OP_ERR)
        {
            bProdErr = TRUE;
            return 0;
        }
    }

    return 0;
}

/*
 * Consumer thread function.
 */
DWORD PALAPI Consumer( LPVOID lpParam )
{
    int n = 0;
    int ret;

    while (n < NUM_OF_CYCLES)
    {
        if (bProdErr == TRUE)
        {
            /* The consumer ran into an error so we'll stop */
            return 0;
        }

        ret = EmptyBuffer();

        if (ret == OP_OK)
        {
            n++;
        }
        else if (ret == OP_ERR)
        {
            bConErr = TRUE;
            return 0;
        }
    }

    return 0;
}


PALTEST(threading_CreateMutexW_ReleaseMutex_test1_paltest_createmutexw_releasemutex_test1, "threading/CreateMutexW_ReleaseMutex/test1/paltest_createmutexw_releasemutex_test1")
{
    DWORD dwThreadId;
    DWORD dwWaitRet;

    HANDLE hThread1;    /* handle to consumer thread */
    HANDLE hThread2;    /* handle to producer thread */
    HANDLE handleArray[2];


    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    /* Initialize our error flags */
    bProdErr = FALSE;
    bConErr = FALSE;

    /*
     * Initialize the Buffer to be empty
     */
    memset(Buffer, 0, MAIN_BUF_SIZE);

    /*
     * Create Mutex
     */
    hMutex = CreateMutexW (NULL, FALSE, NULL);

    if (NULL == hMutex)
    {
        Fail("hMutex = CreateMutexW() - returned NULL\n"
             "Failing Test.\nGetLastError returned %u\n", GetLastError());
    }


    /*
     * Create the Producer thread
     */
    hThread1 = CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)Producer,
                            0, 0, &dwThreadId);

    if ( NULL == hThread1 )
    {
        CloseHandle(hMutex);

        Fail("CreateThread() returned NULL.  Failing test.\n"
             "GetLastError returned %u\n", GetLastError());
    }

    /*
     * Create the Consumer thread
     */
    hThread2 = CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)Consumer,
                            0, 0, &dwThreadId);

    if ( NULL == hThread2 )
    {
        CloseHandle(hMutex);

        /* Set the error flag and give thread1 some time to exit */
        bConErr = FALSE;
        Sleep(250);

        Fail("CreateThread() returned NULL.  Failing test.\n"
             "GetLastError returned %u\n", GetLastError());
    }

    /*
     * Wait for both threads to complete (Max 45 Seconds)
     */
    handleArray[0] = hThread1;
    handleArray[1] = hThread2;
    dwWaitRet = WaitForMultipleObjects (2, handleArray, TRUE, 450000);
    if (dwWaitRet == WAIT_FAILED)
    {
        Fail("ERROR: WaitForMultipleObjects failed.\n");
    }
    else if (dwWaitRet == WAIT_TIMEOUT)
    {
        /* Set the error flags and give the threads some time to exit */
        bProdErr = FALSE;
        bConErr = FALSE;
        Sleep(250);

        Fail("ERROR: Timeout interval exceeded.\n");
    }

    /*
     * Clean up
     */
    if (CloseHandle(hThread1) == FALSE ||
        CloseHandle(hThread2) == FALSE ||
        CloseHandle(hMutex) == FALSE)
    {
        Fail("ERROR: CloseHandle failed.\n");
    }


    /*
     * Check our error flags
     */
    if (bProdErr == TRUE || bConErr == TRUE)
    {
        Fail("ERROR: A collision occurred, so the mutex failed.\n");
    }

    PAL_Terminate();
    return ( PASS );

}
