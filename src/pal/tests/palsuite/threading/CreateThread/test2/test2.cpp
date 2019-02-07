// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

HANDLE hThread[NUM_TESTS];
DWORD dwThreadId[NUM_TESTS];
volatile BOOL bResult[NUM_TESTS];
volatile DWORD dwThreadId1[NUM_TESTS];

DWORD PALAPI Thread( LPVOID lpParameter)
{
    dwThreadId1[(DWORD)(SIZE_T)lpParameter] = GetCurrentThreadId();
    bResult[(DWORD)(SIZE_T) lpParameter] = TRUE;
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

struct testCase testCases[]=
{
    {NULL, 0, &Thread, 0, NULL},
    {NULL, 0, &Thread, CREATE_SUSPENDED, NULL},
    {NULL, 0, &Thread, 0, (LPDWORD) 1}
};

/*
 * close handles 
 */
BOOL cleanup(int index)
{
    int i;
    BOOL bRet = TRUE;

    for (i = 0; i < index; i++)
    {
        if (!CloseHandle(hThread[i]))
        {
            bRet = FALSE;
            Trace("PALSUITE ERROR: CloseHandle(%p) call failed for index %d\n",
                  hThread[i], i);
        }
    }

    return(bRet);
}

int __cdecl main(int argc, char **argv)
{
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
        bResult[i]=FALSE;
        dwThreadId[i]=0;
    }

    for (i = 0; i < NUM_TESTS; i++)
    {
        if (NULL != testCases[i].lpThreadId)
        {
            testCases[i].lpThreadId = &dwThreadId[i];
        }
        /* pass the index as the thread argument */
        hThread[i] = CreateThread( testCases[i].lpThreadAttributes,   
                                   testCases[i].dwStackSize,          
                                   testCases[i].lpStartAddress,       
                                   (LPVOID)i,
                                   testCases[i].dwCreationFlags,      
                                   testCases[i].lpThreadId);  
        if (hThread[i] == NULL)
        {
            Trace("PALSUITE ERROR: CreateThread('%p' '%d' '%p' '%p' '%d' "
                  "'%p') call failed.\nGetLastError returned '%u'.\n", 
                  testCases[i].lpThreadAttributes, testCases[i].dwStackSize,
                  testCases[i].lpStartAddress, (LPVOID)i, 
                  testCases[i].dwCreationFlags, 
                  testCases[i].lpThreadId, GetLastError());
            cleanup(i - 1);
            Fail("");
        } 

        /* Resume suspended threads */
        if (testCases[i].dwCreationFlags == CREATE_SUSPENDED)
        {   
            dwRetRT = ResumeThread (hThread[i]);
            if (dwRetRT != 1)
            {
                Trace ("PALSUITE ERROR: ResumeThread(%p) "
                       "call returned %d it should have returned %d.\n"
                       "GetLastError returned %u.\n", hThread[i], dwRetRT,
                       1, GetLastError());
                cleanup(i);
                Fail("");
            }
        }
    }

    /* cleanup */
    for (i = 0; i < NUM_TESTS; i++)
    {
        dwRetWFSO = WaitForSingleObject(hThread[i], 10000);
        if (dwRetWFSO != WAIT_OBJECT_0)
        {
            Trace ("PALSUITE ERROR: WaitForSingleObject('%p' '%d') "
                   "call returned %d instead of WAIT_OBJECT_0 ('%d').\n"
                   "GetLastError returned %u.\n", hThread[i], 10000, 
                   dwRetWFSO, WAIT_OBJECT_0, GetLastError());
            cleanup(i);
            Fail("");
        }
    }
    if(!cleanup(NUM_TESTS))
    {
        Fail("");
    }

    for (i = 0; i < NUM_TESTS; i++)
    {
        /* 
         * check to see that all threads were created and were passed 
         * the array index as an argument. 
         */
        if (FALSE == bResult[i])
        {
            bRet = FALSE;
            Trace("PALSUITE ERROR: result[%d]=%d.  It should be %d\n", i, 
                  FALSE, TRUE);
        }
        /* 
         * check to see that lpThreadId received the correct value. 
         */
        if (0 != dwThreadId[i])
        {
            if (dwThreadId[i] != dwThreadId1[i])
            {
                bRet = FALSE;
                Trace("PALSUITE ERROR: dwThreadId[%d]=%p and dwThreadId1[%d]"
                      "=%p\nThese values should be identical.\n",  i, 
                      dwThreadId[i], i, dwThreadId1[i]);
            }
        }
    }  
    if (!bRet)
    {
        cleanup(NUM_TESTS);
        Fail("");
    }

    PAL_Terminate();
    return (PASS);
}



