// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source:  child6.c
**
** Purpose: Test for WaitForMultipleObjectsEx in multiple
**          scenarios - child process
**
**
**=========================================================*/

#include <palsuite.h>

PALTEST(threading_WaitForMultipleObjectsEx_test6_paltest_waitformultipleobjectsex_test6_child, "threading/WaitForMultipleObjectsEx/test6/paltest_waitformultipleobjectsex_test6_child")
{
    int i, iRet;
    BOOL bRet;
    BOOL bNamedEvent = 0;
    BOOL bMutex = 0;
    BOOL bMutexAndNamedEvent = 0;
    BOOL bSemaphore = 0;
    DWORD dwRet;
    HANDLE hNamedEvent;
    HANDLE hMutex;
    char szTestName[256];
    WCHAR wszTestName[256] = { 0 };
    char szEventName[128] = { 0 };
    char szMutexName[128] = { 0 };
    char szSemName[128] = { 0 };
    WCHAR wszEventName[128];
    WCHAR wszMutexName[128];
    WCHAR wszSemName[128];
    DWORD iExitCode = 0;
    HANDLE hSemaphore;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    Trace("[child] Starting\n");

    for (i=1; i<argc; i++)
    {
        if (0 == strcmp(argv[i],"-event"))
        {
            bNamedEvent = 1;
        }
        else if (0 == strcmp(argv[i],"-mutex"))
        {
            bMutex = 1;
        }
        else if (0 == strcmp(argv[i],"-mutex_and_named_event"))
        {
            bMutexAndNamedEvent = 1;
        }
        else if (0 == strcmp(argv[i],"-semaphore"))
        {
            bSemaphore = 1;
        }
        else if (0 == strcmp(argv[i],"-exitcode") && i < argc-1 )
        {
            i++;
            iExitCode = atoi(argv[i]);
            Trace("[child] My exit code is %d\n", iExitCode);
        }

        else if ('-' != *argv[i])
        {
            strncpy(szTestName, argv[i], 256);
            szTestName[255] = 0;
            iRet = MultiByteToWideChar(CP_ACP, 0, szTestName, strlen(szTestName)+1, wszTestName, 256);
            if (0 == iRet)
            {
                Fail("Failed to convert test string\n");
            }
        }
    }

    sprintf_s(szEventName, 128, "%s_Event", szTestName);
    szEventName[127] = 0;
    sprintf_s(szMutexName, 128, "%s_Mutex", szTestName);
    szMutexName[127] = 0;
    sprintf_s(szSemName, 128, "%s_Semaphore", szTestName);
    szSemName[127] = 0;

    iRet = MultiByteToWideChar(CP_ACP, 0, szEventName, strlen(szEventName)+1, wszEventName, 128);
    iRet &= MultiByteToWideChar(CP_ACP, 0, szMutexName, strlen(szMutexName)+1, wszMutexName, 128);
    iRet &= MultiByteToWideChar(CP_ACP, 0, szSemName, strlen(szSemName)+1, wszSemName, 128);
    if (0 == iRet)
    {
        Fail("[child] Failed to convert strings\n");
    }

    Trace("[child] TestName=%s Event: %S, Mutex: %S, Semaphore = %S\n",
          szTestName, wszEventName, wszMutexName, wszSemName);

    hNamedEvent = OpenEventW(0, FALSE, wszEventName);
    if (NULL == hNamedEvent)
    {
        Fail("[child] OpenEventW failed [szEventName=%s GetLastError()=%u]\n",
             szEventName, GetLastError());
    }
    hMutex = OpenMutexW(0, FALSE, wszMutexName);
    if (NULL == hMutex)
    {
        Fail("[child] OpenMutexW failed [GetLastError()=%u]\n",
             GetLastError());
    }
    hSemaphore = CreateSemaphoreExW(NULL, 0, 256, wszSemName, 0, 0);
    if (NULL == hSemaphore)
    {
        Fail("[child] CreateSemaphore failed [GetLastError()=%u]\n",
             GetLastError());
    }


    if (bMutex)
    {
        Trace("[child] Going to wait on mutex %s\n", szMutexName);
        dwRet = WaitForSingleObject(hMutex, INFINITE);
        if (WAIT_FAILED == dwRet)
        {
            Fail("[child] WaitForMultipleObjects failed [GetLastError()=%u]\n",
                 GetLastError());
        }

        Trace("[child] Setting event %s\n", szEventName);
        bRet = SetEvent(hNamedEvent);
        if (FALSE == bRet)
        {
            Fail("[child] SetEvent failed [GetLastError()=%u]\n",
                 GetLastError());
        }

        // mutex will be abandoned
    }
    else if (bMutexAndNamedEvent)
    {
        dwRet = WaitForSingleObject(hMutex, INFINITE);
        if (WAIT_FAILED == dwRet)
        {
            Fail("[child] WaitForMultipleObjects failed [GetLastError()=%u]\n",
                 GetLastError());
        }

        Sleep(2000);

        bRet = ReleaseMutex(hMutex);
        if (FALSE == bRet)
        {
            Fail("[child] ReleaseMutex failed [GetLastError()=%u]\n",
                 GetLastError());
        }

        Sleep(1000);

        bRet = SetEvent(hNamedEvent);
        if (FALSE == bRet)
        {
            Fail("[child] SetEvent failed [GetLastError()=%u]\n",
                 GetLastError());
        }
    }
    else if (bSemaphore)
    {
        LONG lPrevCount = 42;


        Trace("[child] Going to wait on event %s\n", szEventName);
        dwRet = WaitForSingleObject(hNamedEvent, INFINITE);
        if (WAIT_FAILED == dwRet)
        {
            Fail("[child] WaitForMultipleObjects failed [GetLastError()=%u]\n",
                 GetLastError());
        }

        Trace("[child] Releasing semaphore %s\n", szSemName);
        bRet = ReleaseSemaphore(hSemaphore, 10, &lPrevCount);
        if (FALSE == bRet)
        {
            Fail("ReleaseMutex failed [GetLastError()=%u]\n",
                 GetLastError());
        }
        if (0 != lPrevCount)
        {
            Fail("Previous count from semaphore=%d, expected 0\n", lPrevCount);
        }
    }
    else if (bNamedEvent)
    {
        Sleep(1000);

        bRet = SetEvent(hNamedEvent);
        if (FALSE == bRet)
        {
            Fail("[child] SetEvent failed [GetLastError()=%u]\n",
                 GetLastError());
        }
    }

    Sleep(1000);

    Trace("[child] Done\n");

    PAL_TerminateEx(iExitCode);
    return iExitCode;
}
