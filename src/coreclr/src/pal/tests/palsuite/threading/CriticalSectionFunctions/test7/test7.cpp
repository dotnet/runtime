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

/* 
 * Tokens 0 and 1 are events.  Token 2 is the thread.
 */
#define NUM_TOKENS 3                             

HANDLE hToken[NUM_TOKENS];
CRITICAL_SECTION CriticalSection;

BOOL CleanupHelper (HANDLE *hArray, DWORD dwIndex)
{
    BOOL bCHRet;

    bCHRet = CloseHandle(hArray[dwIndex]);
    if (!bCHRet)
    {
        Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
              "clean up.\nGetLastError returned '%u'.\n", hArray[dwIndex],
              GetLastError());
    }

    return (bCHRet);
}

BOOL Cleanup(HANDLE *hArray, DWORD dwIndex)
{
    BOOL bCRet;
    BOOL bCHRet = 0;

    while (--dwIndex > 0)
    {
        bCHRet = CleanupHelper(&hArray[0], dwIndex); 
    }
   
    bCRet = CloseHandle(hArray[0]);
    if (!bCRet)
    {
        Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
              "clean up.\nGetLastError returned '%u'.\n", hArray[dwIndex],
              GetLastError());  
    }
    
    return (bCRet&&bCHRet);
}

DWORD PALAPI Thread(LPVOID lpParam)
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

int __cdecl main(int argc, char **argv)
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
                           &Thread,
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



