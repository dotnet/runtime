// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*===========================================================
**
** Source: test2.c 
**
** Purpose: Test that lpThreadId is assigned the correct 
** threadId value and that lpThreadId can be NULL.
**
**
**=========================================================*/

#include <palsuite.h>

#define NUM_TESTS 3

HANDLE hThread_CreateThread_test2[NUM_TESTS];
DWORD dwThreadId_CreateThread_test2[NUM_TESTS];
volatile BOOL bResult_CreateThread_test2[NUM_TESTS];
volatile DWORD dwThreadId1_CreateThread_test2[NUM_TESTS];

DWORD PALAPI Thread_CreateThread_test2( LPVOID lpParameter)
{
    dwThreadId1_CreateThread_test2[(DWORD)(SIZE_T)lpParameter] = GetCurrentThreadId();
    bResult_CreateThread_test2[(DWORD)(SIZE_T) lpParameter] = TRUE;
    return (DWORD)(SIZE_T) lpParameter;
}

struct testCase 
{
    LPSECURITY_ATTRIBUTES lpThreadAttributes;
    DWORD dwStackSize;
    LPTHREAD_START_ROUTINE lpStartAddress;
    DWORD dwCreationFlags;
    LPDWORD lpThreadId;
};

/*
 * close handles 
 */
BOOL cleanup_CreateThread_test2(int index)
{
    int i;
    BOOL bRet = TRUE;

    for (i = 0; i < index; i++)
    {
        if (!CloseHandle(hThread_CreateThread_test2[i]))
        {
            bRet = FALSE;
            Trace("PALSUITE ERROR: CloseHandle(%p) call failed for index %d\n",
                  hThread_CreateThread_test2[i], i);
        }
    }

    return(bRet);
}

PALTEST(threading_CreateThread_test2_paltest_createthread_test2, "threading/CreateThread/test2/paltest_createthread_test2")
{
    struct testCase testCases[]=
    {
        {NULL, 0, &Thread_CreateThread_test2, 0, NULL},
        {NULL, 0, &Thread_CreateThread_test2, CREATE_SUSPENDED, NULL},
        {NULL, 0, &Thread_CreateThread_test2, 0, (LPDWORD) 1}
    };

    SIZE_T i;
    DWORD dwRetWFSO;
    DWORD dwRetRT;
    BOOL bRet = TRUE;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return (FAIL);
    }

    /* set results array to FALSE */
    for (i = 0; i < NUM_TESTS; i++)
    {
        bResult_CreateThread_test2[i]=FALSE;
        dwThreadId_CreateThread_test2[i]=0;
    }

    for (i = 0; i < NUM_TESTS; i++)
    {
        if (NULL != testCases[i].lpThreadId)
        {
            testCases[i].lpThreadId = &dwThreadId_CreateThread_test2[i];
        }
        /* pass the index as the thread argument */
        hThread_CreateThread_test2[i] = CreateThread( testCases[i].lpThreadAttributes,   
                                   testCases[i].dwStackSize,          
                                   testCases[i].lpStartAddress,       
                                   (LPVOID)i,
                                   testCases[i].dwCreationFlags,      
                                   testCases[i].lpThreadId);  
        if (hThread_CreateThread_test2[i] == NULL)
        {
            Trace("PALSUITE ERROR: CreateThread('%p' '%d' '%p' '%p' '%d' "
                  "'%p') call failed.\nGetLastError returned '%u'.\n", 
                  testCases[i].lpThreadAttributes, testCases[i].dwStackSize,
                  testCases[i].lpStartAddress, (LPVOID)i, 
                  testCases[i].dwCreationFlags, 
                  testCases[i].lpThreadId, GetLastError());
            cleanup_CreateThread_test2(i - 1);
            Fail("");
        } 

        /* Resume suspended threads */
        if (testCases[i].dwCreationFlags == CREATE_SUSPENDED)
        {   
            dwRetRT = ResumeThread (hThread_CreateThread_test2[i]);
            if (dwRetRT != 1)
            {
                Trace ("PALSUITE ERROR: ResumeThread(%p) "
                       "call returned %d it should have returned %d.\n"
                       "GetLastError returned %u.\n", hThread_CreateThread_test2[i], dwRetRT,
                       1, GetLastError());
                cleanup_CreateThread_test2(i);
                Fail("");
            }
        }
    }

    /* cleanup */
    for (i = 0; i < NUM_TESTS; i++)
    {
        dwRetWFSO = WaitForSingleObject(hThread_CreateThread_test2[i], 10000);
        if (dwRetWFSO != WAIT_OBJECT_0)
        {
            Trace ("PALSUITE ERROR: WaitForSingleObject('%p' '%d') "
                   "call returned %d instead of WAIT_OBJECT_0 ('%d').\n"
                   "GetLastError returned %u.\n", hThread_CreateThread_test2[i], 10000, 
                   dwRetWFSO, WAIT_OBJECT_0, GetLastError());
            cleanup_CreateThread_test2(i);
            Fail("");
        }
    }
    if(!cleanup_CreateThread_test2(NUM_TESTS))
    {
        Fail("");
    }

    for (i = 0; i < NUM_TESTS; i++)
    {
        /* 
         * check to see that all threads were created and were passed 
         * the array index as an argument. 
         */
        if (FALSE == bResult_CreateThread_test2[i])
        {
            bRet = FALSE;
            Trace("PALSUITE ERROR: result[%d]=%d.  It should be %d\n", i, 
                  FALSE, TRUE);
        }
        /* 
         * check to see that lpThreadId received the correct value. 
         */
        if (0 != dwThreadId_CreateThread_test2[i])
        {
            if (dwThreadId_CreateThread_test2[i] != dwThreadId1_CreateThread_test2[i])
            {
                bRet = FALSE;
                Trace("PALSUITE ERROR: dwThreadId_CreateThread_test2[%d]=%p and dwThreadId1_CreateThread_test2[%d]"
                      "=%p\nThese values should be identical.\n",  i, 
                      dwThreadId_CreateThread_test2[i], i, dwThreadId1_CreateThread_test2[i]);
            }
        }
    }  
    if (!bRet)
    {
        cleanup_CreateThread_test2(NUM_TESTS);
        Fail("");
    }

    PAL_Terminate();
    return (PASS);
}



