// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:      CriticalSectionFunctions/test7/test7.c
**
** Purpose:     Attempt to delete a critical section owned by the current 
**              thread.
**
**
**===================================================================*/
#include <palsuite.h>

DWORD PALAPI Thread_CriticalSectionFunctions_test7(LPVOID lpParam)
{
    DWORD dwTRet;

    EnterCriticalSection(&CriticalSection);

    /* signal thread 0 */
    if (0 == SetEvent(hToken[0]))
    {
        Trace("PALSUITE ERROR: Unable to execute SetEvent(%p) during "
              "clean up.\nGetLastError returned '%u'.\n", hToken[0],
              GetLastError());
        LeaveCriticalSection(&CriticalSection);
        Cleanup (&hToken[0], NUM_TOKENS);
        DeleteCriticalSection(&CriticalSection);
        Fail("");
    }
    
    /* wait to be signaled */        
    dwTRet = WaitForSingleObject(hToken[1], 10000);
    if (WAIT_OBJECT_0 != dwTRet)
    {
        Trace("PALSUITE ERROR: WaitForSingleObject(%p,%d) should have "
              "returned\nWAIT_OBJECT_0 ('%d'), instead it returned "
              "('%d').\nGetLastError returned '%u'.\n", 
              hToken[0], 10000, WAIT_OBJECT_0, dwTRet, GetLastError());
        LeaveCriticalSection(&CriticalSection);
        Cleanup (&hToken[0], NUM_TOKENS);
        DeleteCriticalSection(&CriticalSection);
        Fail("");
    }      

    DeleteCriticalSection(&CriticalSection); 
    
    return 0;
}

PALTEST(threading_CriticalSectionFunctions_test7_paltest_criticalsectionfunctions_test7, "threading/CriticalSectionFunctions/test7/paltest_criticalsectionfunctions_test7")
{
    DWORD dwThreadId;
    DWORD dwMRet;

    if ((PAL_Initialize(argc,argv)) != 0)
    {
        return(FAIL);
    }

    /* thread 0 event */
    hToken[0] = CreateEvent(NULL, TRUE, FALSE, NULL);

    if (hToken[0] == NULL)
    {
        Fail("PALSUITE ERROR: CreateEvent call #0 failed.  GetLastError "
             "returned %u.\n", GetLastError());
    }

    /* thread 1 event */
    hToken[1] = CreateEvent(NULL, TRUE, FALSE, NULL);
    
    if (hToken[1] == NULL)
    {
        Trace("PALSUITE ERROR: CreateEvent call #1 failed.  GetLastError "
             "returned %u.\n", GetLastError());
        Cleanup (&hToken[0], (NUM_TOKENS - 2));
        Fail("");
    }

    InitializeCriticalSection(&CriticalSection);

    hToken[2] = CreateThread(NULL,
                           0,
                           &Thread_CriticalSectionFunctions_test7,
                           (LPVOID) NULL,
                           0,
                           &dwThreadId);

    if (hToken[2] == NULL)
    {
        Trace("PALSUITE ERROR: CreateThread call #0 failed.  GetLastError "
              "returned %u.\n", GetLastError());
        Cleanup (&hToken[0], (NUM_TOKENS - 1));
        DeleteCriticalSection(&CriticalSection);
        Fail("");
    }
    
    /* wait for thread 0 to be signaled */
    dwMRet = WaitForSingleObject(hToken[0], 10000);
    if (WAIT_OBJECT_0 != dwMRet)
    {   
        Trace("PALSUITE ERROR: WaitForSingleObject(%p,%d) should have "
              "returned\nWAIT_OBJECT_0 ('%d'), instead it returned "
              "('%d').\nGetLastError returned '%u'.\n", hToken[0], 10000, 
              WAIT_OBJECT_0, dwMRet, GetLastError());
        Cleanup (&hToken[0], NUM_TOKENS);
        Fail("");
    }

    /* signal thread 1 */
    if (0 == SetEvent(hToken[1]))
    {
        Trace("PALSUITE ERROR: Unable to execute SetEvent(%p) call.\n"
              "GetLastError returned '%u'.\n", hToken[1],
              GetLastError());
        Cleanup (&hToken[0], NUM_TOKENS);
        Fail("");
    }

    dwMRet = WaitForSingleObject(hToken[2], 10000);
    if (WAIT_OBJECT_0 != dwMRet)
    {
        Trace("PALSUITE ERROR: WaitForSingleObject(%p, %d) call "
              "returned an unexpected value '%d'.\nGetLastError returned "
              "%u.\n", hToken[2], 10000, dwMRet, GetLastError());
        Cleanup (&hToken[0], NUM_TOKENS);
        Fail("");
    }              

    if (!Cleanup(&hToken[0], NUM_TOKENS)) 
    {
        Fail("");
    }

    PAL_Terminate();

    return (PASS);
}



