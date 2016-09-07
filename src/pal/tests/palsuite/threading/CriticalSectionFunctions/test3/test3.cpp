// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:      CriticalSectionFunctions/test3/test3.c
**
** Purpose:     Create two threads to exercise TryEnterCriticalSection
**              and EnterCriticalSection.  TryEnterCriticalSection acquires 
**              and holds a CRITICAL_SECTION object.  Another call to 
**              TryEnterCriticalSection is made from a different thread, at 
**              this time, to establish a call to TryEnterCriticalSection
**              will return immediatly and to establish 
**              TryEnterCriticalSection returns the proper value when it
**              attempts to lock a CRITICAL_SECTION that is already owned 
**              by another thread.  The CRITICAL_SECTION object is then 
**              released and held by a call to EnterCriticalSection.  A new 
**              thread is invoked and attempts to acquire the held 
**              CRITICAL_SECTION with a call to TryEnterCriticalSection.  
**              TryEnterCriticalSection returns immediatly and returns
**              with the value that states the CRITICAL_SECTION object is 
**              held by another thread.  This establishes 
**              TryEnterCriticalSection behaves the same way with 
**              CriticalSections locked by TryEnterCriticalSection and 
**              EnterCriticalSection.
**
**
**===================================================================*/
#include <palsuite.h>

#define NUM_THREADS 2
                             
HANDLE hThread[NUM_THREADS]; 
HANDLE hEvent[NUM_THREADS]; 
CRITICAL_SECTION CriticalSection;
BOOL bRet = FAIL;

DWORD PALAPI Thread(LPVOID lpParam)
{
    DWORD dwRet;

    if (0 == TryEnterCriticalSection(&CriticalSection))
    {
        dwRet = WaitForMultipleObjects(NUM_THREADS, hEvent, TRUE, 10000);
        if ((WAIT_OBJECT_0 > dwRet) || 
            ((WAIT_OBJECT_0 + NUM_THREADS - 1) < dwRet))
        {
#if 0
            if (0 == CloseHandle(hThread[1]))
            {
                Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) "
                      "during clean up.\nGetLastError returned '%d'.\n", 
                      hThread[1], GetLastError());
            }
#endif 
            Trace("PALSUITE ERROR: WaitForMultipleObjects(%d, %p, %d, %d) call"
                  "returned an unexpected value, '%d'.\nGetLastError returned "
                  "%d.\n", NUM_THREADS, hEvent, TRUE, 10000, dwRet, 
                  GetLastError());
        }
        else 
        {   
            bRet = PASS;         
        }
    }
    else 
    {
        /* signal thread 0 */
        if (0 == SetEvent(hEvent[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute SetEvent(%p) during "
                 "clean up.\nGetLastError returned '%d'.\n", hEvent[0],
                 GetLastError());
            LeaveCriticalSection(&CriticalSection);
            if (0 == CloseHandle(hThread[0]))
            {
                Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) "
                      "during clean up\nGetLastError returned '%d'.\n", 
                      hThread[0], GetLastError());
            }
            if (0 == CloseHandle(hEvent[0]))
            {
                Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) "
                      "during clean up\nGetLastError returned '%d'.\n", 
                      hEvent[0], GetLastError());
            }
            if (0 == CloseHandle(hEvent[1]))
        {
                Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) "
                      "during clean up\nGetLastError returned '%d'.\n", 
                      hEvent[1], GetLastError());
        }
            DeleteCriticalSection(&CriticalSection);
            Fail("");
        }

        /* wait to be signaled */        
        dwRet = WaitForSingleObject(hEvent[1], 10000);
        if (WAIT_OBJECT_0 != dwRet)
        {
            Trace("PALSUITE ERROR: WaitForSingleObject(%p,%d) should have "
                 "returned\nWAIT_OBJECT_0 ('%d'), instead it returned "
                 "('%d').\nGetLastError returned '%d'.\n", 
                 hEvent[0], 10000, WAIT_OBJECT_0, dwRet, GetLastError());
            if (0 == CloseHandle(hThread[0]))
            {
                Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) "
                      "during clean up.\nGetLastError returned '%d'.\n", 
                      hThread[0], GetLastError());
            }
            if (0 == CloseHandle(hEvent[0]))
            {
                Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) "
                      "during clean up.\nGetLastError returned '%d'.\n", 
                      hEvent[0], GetLastError());
            }
            if (0 == CloseHandle(hEvent[1]))
            {
                Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) "
                      "during clean up.\nGetLastError returned '%d.'\n", 
                      hEvent[1], GetLastError());
            }
            DeleteCriticalSection(&CriticalSection);
            Fail("");
        }
        LeaveCriticalSection(&CriticalSection);    
    }
    return FAIL;
}

int __cdecl main(int argc, char **argv)
{
    HANDLE hThread[NUM_THREADS];
    DWORD dwThreadId[NUM_THREADS];
    DWORD dwRet;

    if ((PAL_Initialize(argc,argv)) != 0)
    {
        return(bRet);
    }

    /* thread 0 event */
    hEvent[0] = CreateEvent(NULL, TRUE, FALSE, NULL);

    if (hEvent[0] == NULL)
    {
        Fail("PALSUITE ERROR: CreateEvent call #0 failed.  GetLastError "
             "returned %d.\n", GetLastError());
    }

    /* thread 1 event */
    hEvent[1] = CreateEvent(NULL, TRUE, FALSE, NULL);
    
    if (hEvent[1] == NULL)
    {
        Trace("PALSUITE ERROR: CreateEvent call #1 failed.  GetLastError "
             "returned %d.\n", GetLastError());
        if (0 == CloseHandle(hEvent[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent[0]);
        }
        Fail("");
    }

    InitializeCriticalSection ( &CriticalSection );

    hThread[0] = CreateThread(NULL,
                           0,
                              &Thread,
                           (LPVOID) NULL,
                           0,
                              &dwThreadId[0]);

    if (hThread[0] == NULL)
    {
        Trace("PALSUITE ERROR: CreateThread call #0 failed.  GetLastError "
             "returned %d.\n", GetLastError());
        if (0 == CloseHandle(hEvent[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent[0], 
                  GetLastError());
        }
        if (0 == CloseHandle(hEvent[1]))
    {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent[1],
                  GetLastError());
        }
        DeleteCriticalSection(&CriticalSection);
        Fail("");
    }

    
    /* wait for thread 0 to be signaled */
    dwRet = WaitForSingleObject(hEvent[0], 10000);
    if (WAIT_OBJECT_0 != dwRet)
    {   
        Trace("PALSUITE ERROR: WaitForSingleObject(%p,%d) should have "
             "returned\nWAIT_OBJECT_0 ('%d'), instead it returned "
             "('%d').\nGetLastError returned '%d'.\n", hEvent[0], 10000, 
             WAIT_OBJECT_0, dwRet, GetLastError());
        if (0 == CloseHandle(hThread[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hThread[0],
                  GetLastError());
        }
        if (0 == CloseHandle(hEvent[0]))
    {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent[0],
                  GetLastError());
    }
        if (0 == CloseHandle(hEvent[1]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent[1],
                  GetLastError());
        }
        Fail("");
    }
    
    /* 
     * Attempting to enter CRITICAL_SECTION object owned by the 
     * created thread and locked with TryEnterCriticalSection 
     */
    if (0 == TryEnterCriticalSection(&CriticalSection))
    {
        /* signal thread 1 */
        if (0 == SetEvent(hEvent[1]))
        {
            Trace("PALSUITE ERROR: Unable to execute SetEvent(%p) call.\n"
                  "GetLastError returned '%d'.\n", hEvent[1],
                  GetLastError());
            goto done;
        }
    }
    else 
    {
        Trace("PALSUITE_ERROR: TryEnterCriticalSection was able to grab a"
             " CRITICAL_SECTION object\nwhich was already owned.\n");
        LeaveCriticalSection(&CriticalSection);
        if (0 == CloseHandle(hThread[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hThread[0],
                  GetLastError());
        }
        if (0 == CloseHandle(hEvent[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent[0],
                  GetLastError());
        }
        if (0 == CloseHandle(hEvent[1]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent[1],
                  GetLastError());
        }
        DeleteCriticalSection(&CriticalSection);
        Fail("");
    }
        /*
     * Enter the CRITICAL_SECTION and launch another thread to attempt
     * to access the CRITICAL_SECTION with a call to TryEnterCriticalSection.
         */
    EnterCriticalSection(&CriticalSection);

    hThread[1] = CreateThread(NULL,
                              0,
                              &Thread,
                              (LPVOID) NULL,
                              0,
                              &dwThreadId[1]);

    if (hThread[1] == NULL)
    {
        Trace("PALSUITE ERROR: CreateThread call #1 failed.  GetLastError "
             "returned %d.\n", GetLastError());
        LeaveCriticalSection(&CriticalSection);
        if (0 == CloseHandle(hThread[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hThread[0],
                  GetLastError());
        }
        if (0 == CloseHandle(hEvent[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent[0],
                  GetLastError());
        }
        if (0 == CloseHandle(hEvent[1]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent[1],
                  GetLastError());
        }
        DeleteCriticalSection(&CriticalSection);
        Fail("");
    }
    
    dwRet = WaitForMultipleObjects(NUM_THREADS, hThread, TRUE, 10000);
    if ((WAIT_OBJECT_0 > dwRet) || 
        ((WAIT_OBJECT_0 + NUM_THREADS - 1) < dwRet))
    {
        Trace("PALSUITE ERROR: WaitForMultipleObjects(%d, %p, %d, %d) call "
             "returned an unexpected value, '%d'.\nGetLastError returned "
             "%d.\n", NUM_THREADS, hThread, TRUE, 10000, dwRet, 
             GetLastError());
        LeaveCriticalSection(&CriticalSection);
        if (0 == CloseHandle(hThread[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hThread[0],
                  GetLastError());
        }
        if (0 == CloseHandle(hThread[1]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hThread[1],
                  GetLastError());
        }
        if (0 == CloseHandle(hEvent[0]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent[0],
                  GetLastError());
        }
        if (0 == CloseHandle(hEvent[1]))
        {
            Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
                  "clean up.\nGetLastError returned '%d'.\n", hEvent[1],
                  GetLastError());
        }
    DeleteCriticalSection(&CriticalSection);
        Fail("");
    }
    
    LeaveCriticalSection(&CriticalSection);
    if (0 == CloseHandle(hThread[1]))
    {
        Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
              "clean up.\nGetLastError returned '%d'.\n", hThread[1],
              GetLastError());
    }
done:
    if (0 == CloseHandle(hThread[0]))
    {
        Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
              "clean up.\nGetLastError returned '%d'.\n", hThread[0],
              GetLastError());
    }
    if (0 == CloseHandle(hEvent[0]))
    {
        Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
              "clean up.\nGetLastError returned '%d'.\n", hEvent[0],
              GetLastError());
    }
    if (0 == CloseHandle(hEvent[1]))
    {
        Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
              "clean up.\nGetLastError returned '%d'.\n", hEvent[1],
              GetLastError());
    }
    DeleteCriticalSection(&CriticalSection);

    PAL_TerminateEx(bRet);

    return (bRet);
}

