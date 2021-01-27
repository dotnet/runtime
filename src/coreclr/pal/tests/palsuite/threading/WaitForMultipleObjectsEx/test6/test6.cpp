// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source:  test6.c
**
** Purpose: Test for WaitForMultipleObjectsEx in multiple
**          scenarios
**
**
**=========================================================*/

#include <palsuite.h>

#define MAX_COUNT 10000
#define MAX_THREADS 256

BOOL g_bMutex         = 0;
BOOL g_bEvent         = 0;
BOOL g_bNamedEvent    = 0;
BOOL g_bSemaphore     = 0;
BOOL g_bProcess       = 0;
BOOL g_bLocalWaitAll  = 0;
BOOL g_bRemoteWaitAll = 0;
BOOL g_bRandom        = 0;

int iCount = 1;
int iThreads = 1;
HANDLE hThreads[MAX_THREADS];

#ifndef MIN
#define MIN(a,b) (((a)<(b)) ? (a) : (b))
#endif

DWORD PALAPI EventTestThread(PVOID pArg)
{
    BOOL bRet;
    DWORD dwRet;
    HANDLE hEvent[2];
    HANDLE (*prgHandles)[] = (HANDLE (*)[])pArg;

    Trace("[EventTestThread] Starting\n");

    bRet = DuplicateHandle(GetCurrentProcess(), (*prgHandles)[0], GetCurrentProcess(),
               &hEvent[0], 0, FALSE, DUPLICATE_SAME_ACCESS);
    bRet &= DuplicateHandle(GetCurrentProcess(), (*prgHandles)[1], GetCurrentProcess(),
               &hEvent[1], 0, FALSE, DUPLICATE_SAME_ACCESS);
    if (FALSE == bRet)
    {
    Fail("[EventTestThread] Failed to duplicate handles\n");
    }

    Sleep(1000);
    bRet = SetEvent(hEvent[1]);
    if (FALSE == bRet)
    {
        Fail("SetEvent failed\n");
        Fail("[EventTestThread] SetEvent failed [GetLastError()=%u]\n",
             GetLastError());
    }

    dwRet = WaitForSingleObject(hEvent[1], INFINITE);
    if (WAIT_FAILED == dwRet)
    {
        Fail("[EventTestThread] WaitForMultipleObjects failed [GetLastError()=%u]\n",
             GetLastError());
    }

    Sleep(1000);
    bRet = SetEvent(hEvent[0]);
    if (FALSE == bRet)
    {
        Fail("[EventTestThread] SetEvent failed [GetLastError()=%u]\n",
             GetLastError());
    }

    Sleep(1000);
    bRet = SetEvent(hEvent[1]);
    if (FALSE == bRet)
    {
        Fail("[EventTestThread] SetEvent failed [GetLastError()=%u]\n",
             GetLastError());
    }

    CloseHandle(hEvent[0]);
    CloseHandle(hEvent[1]);

    Trace("[EventTestThread] Done\n");
    return 0;
}

DWORD PALAPI MutexTestThread(PVOID pArg)
{
    BOOL bRet;
    DWORD dwRet;
    HANDLE hMutex;

    Trace("[MutexTestThread] Starting\n");

    bRet = DuplicateHandle(GetCurrentProcess(), (HANDLE)pArg, GetCurrentProcess(), &hMutex,
               0, FALSE, DUPLICATE_SAME_ACCESS);
    if (FALSE == bRet)
    {
        Fail("[EventTestThread] DuplicateHandle failed [GetLastError()=%u]\n",
             GetLastError());
    }

    dwRet = WaitForSingleObject(hMutex, INFINITE);
    if (WAIT_FAILED == dwRet)
    {
        Fail("[EventTestThread] WaitForMultipleObjects failed [GetLastError()=%u]\n",
             GetLastError());
    }

    Sleep(1000);
    CloseHandle(hMutex);

    Trace("[MutexTestThread] Done\n");

    return 0;
}

DWORD PALAPI TestThread(PVOID pArg)
{
    BOOL bRet;
    DWORD dwRet;
    PROCESS_INFORMATION pi;
    STARTUPINFO si;
    HANDLE hNamedEvent;
    HANDLE hEvent[2] = { 0, 0 };
    HANDLE hMutex = 0;
    HANDLE hSemaphore = 0;
    HANDLE hObjs[2];
    DWORD dwThreadNum;
    DWORD dwSlaveThreadTid = 0;
    HANDLE hThread;
    int i, iCnt, iRet;
    char szTestName[128];
    char szCmd[128];
    char szEventName[128] = { 0 };
    char szMutexName[128] = { 0 };
    char szSemName[128] = { 0 };
    WCHAR wszEventName[128] = { 0 };
    WCHAR wszMutexName[128] = { 0 };
    WCHAR wszSemName[128] = { 0 };
    BOOL bMutex         = g_bMutex;
    BOOL bEvent         = g_bEvent;
    BOOL bNamedEvent    = g_bNamedEvent;
    BOOL bSemaphore     = g_bSemaphore;
    BOOL bProcess       = g_bProcess;
    BOOL bLocalWaitAll  = g_bLocalWaitAll;
    BOOL bRemoteWaitAll = g_bRemoteWaitAll;
    int iDesiredExitCode;

    dwThreadNum = (DWORD)(SIZE_T)pArg;

    sprintf_s (szTestName, 128, "Test6_%u", dwThreadNum);
    szTestName[127] = 0;

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
        Fail("[TestThread] Failed to convert strings\n");
    }

    Trace("[TestThread] TestName=%s Event: %S, Mutex: %S, Semaphore = %S\n",
       szTestName, wszEventName, wszMutexName, wszSemName);

    hEvent[0] = CreateEvent(NULL, FALSE, FALSE, NULL);
    hEvent[1] = CreateEvent(NULL, FALSE, FALSE, NULL);

    hNamedEvent = CreateEventW(NULL, FALSE, FALSE, wszEventName);
    hMutex = CreateMutexW(NULL, FALSE, wszMutexName);
    hSemaphore = CreateSemaphoreExW(NULL, 0, 256, wszSemName, 0, 0);

    if (NULL == hEvent[0] || NULL == hEvent[1] || NULL == hMutex ||
    NULL == hNamedEvent || NULL == hSemaphore)
    {
        Fail("[TestThread] Failed to create objects "
             "[hNamedEvent=%p hMutex=%p hSemaphore=%p]\n",
             (VOID*)hNamedEvent, (VOID*)hMutex, (VOID*)hSemaphore);
    }

    for (iCnt=0; iCnt<iCount; iCnt++)
    {
        if (g_bRandom)
        {
            int iRnd;

            bMutex         = 0;
            bEvent         = 0;
            bNamedEvent    = 0;
            bSemaphore     = 0;
            bProcess       = 0;
            bLocalWaitAll  = 0;
            bRemoteWaitAll = 0;

            iRnd = rand() % 7;
            switch(iRnd)
            {
            case 0:
                bMutex = 1;
                break;
            case 1:
                bEvent = 1;
                break;
            case 2:
                bNamedEvent = 1;
                break;
            case 3:
                bSemaphore = 1;
                break;
            case 4:
                bProcess = 1;
                break;
            case 5:
                bLocalWaitAll = 1;
                break;
            case 6:
                bRemoteWaitAll = 1;
                break;
            }
        }

        if (bEvent)
        {
            Trace("======================================================================\n");
            Trace("Local unnamed event test\n");
            Trace("----------------------------------------\n");
            hThread = CreateThread(NULL, 0, EventTestThread, (PVOID)hEvent, 0, &dwSlaveThreadTid);
            if (NULL == hThread)
            {
                Fail("Failed to create thread\n");
            }

            hObjs[0] = hEvent[0];
            dwRet = WaitForMultipleObjects(1, hObjs, FALSE, INFINITE);
            if (WAIT_FAILED == dwRet)
            {
                Fail("WaitForMultipleObjects failed\n");
            }

            hObjs[0] = hThread;
            dwRet = WaitForMultipleObjects(1, hObjs, FALSE, INFINITE);
            if (WAIT_FAILED == dwRet)
            {
                Fail("WaitForMultipleObjects failed\n");
            }

            CloseHandle(hThread);
            Trace("Local unnamed event test done \n");
            Trace("======================================================================\n");
        }

        if (bMutex)
        {
            Trace("======================================================================\n");
            Trace("Mutex with remote thread awakening test\n");
            Trace("----------------------------------------\n");

            hThread = CreateThread(NULL, 0, MutexTestThread, (PVOID)hMutex, 0, &dwSlaveThreadTid);
            if (NULL == hThread)
            {
                Fail("Failed to create thread\n");
            }

            Sleep(1000);

            hObjs[0] = hMutex;

            for (i=0;i<10;i++)
            {
                dwRet = WaitForMultipleObjects(1, hObjs, FALSE, INFINITE);
                if (WAIT_FAILED == dwRet)
                {
                    Fail("WaitForMultipleObjects failed [dwRet=%x GetLastError()=%d\n",
                         dwRet, GetLastError());
                }
            }

            hObjs[0] = hThread;
            dwRet = WaitForMultipleObjects(1, hObjs, FALSE, INFINITE);
            if (WAIT_FAILED == dwRet)
            {
                Fail("WaitForMultipleObjects failed [GetLastError()=%u]\n",
                      GetLastError());
            }

            for (i=0;i<10;i++)
            {
                bRet = ReleaseMutex(hMutex);
                if (FALSE == bRet)
                {
                    Fail("ReleaseMutex failed [GetLastError()=%u]\n",
                         GetLastError());
                }
            }

            CloseHandle(hThread);
            Trace("Mutex with remote thread awakening test done\n");
            Trace("======================================================================\n");
        }

        if (bNamedEvent)
        {
            Trace("======================================================================\n");
            Trace("Named event with remote thread awakening test\n");
            Trace("----------------------------------------\n");

            ZeroMemory ( &si, sizeof(si) );
            si.cb = sizeof(si);
            ZeroMemory ( &pi, sizeof(pi) );

            sprintf_s (szCmd, 128, "child6 -event %s", szTestName);
            szCmd[127] = 0;

            LPWSTR szCmdW = convert(szCmd);
            bRet = CreateProcessW(NULL, szCmdW, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi);
            free(szCmdW);
            if (FALSE == bRet)
            {
                Fail("CreateProcessW failed [GetLastError()=%u]\n",
                     GetLastError());
            }

            hObjs[0] = pi.hProcess;
            hObjs[1] = hNamedEvent;

            dwRet = WaitForMultipleObjects(2, hObjs, FALSE, INFINITE);
            if (1 != dwRet)
            {
                Fail("WaitForMultipleObjects failed [dwRet=%u GetLastError()=%u]\n",
                     dwRet, GetLastError());
            }

            dwRet = WaitForSingleObject(pi.hProcess, INFINITE);
            if (WAIT_FAILED == dwRet)
            {
                Fail("WaitForMultipleObjects failed [GetLastError()=%u]\n",
                     GetLastError());
            }
            Trace("Named event with remote thread awakening test done\n");
            Trace("======================================================================\n");
        }

        if (bSemaphore)
        {
            Trace("======================================================================\n");
            Trace("Semaphore with remote thread awakening test\n");
            Trace("----------------------------------------\n");

            ZeroMemory ( &si, sizeof(si) );
            si.cb = sizeof(si);
            ZeroMemory ( &pi, sizeof(pi) );

            sprintf_s (szCmd, 128, "child6 -semaphore %s", szTestName);
            szCmd[127] = 0;

            LPWSTR szCmdW = convert(szCmd);
            bRet = CreateProcessW(NULL, szCmdW, NULL, NULL, FALSE,
                                  0, NULL, NULL, &si, &pi);
            free(szCmdW);
            if (FALSE == bRet)
            {
                Fail("CreateProcessW failed [GetLastError()=%u]\n",
                     GetLastError());
            }


            Trace("Setting event %s\n", szEventName);
            bRet = SetEvent(hNamedEvent);
            if (FALSE == bRet)
            {
                Fail("[child] SetEvent failed [GetLastError()=%u]\n",
                     GetLastError());
            }

            Trace("Going to wait on semaphore %s\n", szSemName);


            hObjs[0] = pi.hProcess;
            hObjs[0] = hEvent[0];
            hObjs[1] = hSemaphore;
            for (i=0;i<10;i++)
            {
                dwRet = WaitForMultipleObjects(2, hObjs, FALSE, INFINITE);
                if (1 != dwRet)
                {
                    Trace("WaitForMultipleObjects failed [tid=%u dwRet=%u GetLastError()=%u]\n",
                          GetCurrentThreadId(), dwRet, GetLastError());
                    DebugBreak();
                }
            }

            dwRet = WaitForSingleObject(pi.hProcess, INFINITE);
            if (WAIT_FAILED == dwRet)
            {
                Fail("WaitForMultipleObjects failed [GetLastError()=%u]\n",
                     GetLastError());
            }
            Trace("Semaphore with remote thread awakening test done\n");
            Trace("======================================================================\n");
        }

        if (bProcess)
        {
            DWORD dwExitCode;

            Trace("======================================================================\n");
            Trace("Process wait test\n");
            Trace("----------------------------------------\n");

            iDesiredExitCode = rand() % 0xFF;

            ZeroMemory ( &si, sizeof(si) );
            si.cb = sizeof(si);
            ZeroMemory ( &pi, sizeof(pi) );

            sprintf_s (szCmd, 128, "child6 -mutex %s -exitcode %d", szTestName, iDesiredExitCode);
            szCmd[127] = 0;

            LPWSTR szCmdW = convert(szCmd);
            bRet = CreateProcessW(NULL, szCmdW, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi);
            free(szCmdW);
            if (FALSE == bRet)
            {
                Fail("CreateProcessW failed [GetLastError()=%u]\n",
                     GetLastError());
            }

            Trace("Going to wait on event %s\n", szEventName);
            dwRet = WaitForSingleObject(hNamedEvent, INFINITE);
            if (WAIT_FAILED == dwRet)
            {
                Fail("WaitForMultipleObjects failed [GetLastError()=%u]\n",
                     GetLastError());
            }

            hObjs[0] = hEvent[0]; // dummy, this is a local event
            hObjs[1] = hMutex;

            dwRet = WaitForMultipleObjects(2, hObjs, FALSE, INFINITE);
            if (WAIT_FAILED == dwRet)
            {
                Fail("WaitForMultipleObjects failed [GetLastError()=%u]\n",
                     GetLastError());
            }
            if (1 == dwRet || (1 + WAIT_ABANDONED_0) == dwRet)
            {
                bRet = ReleaseMutex(hMutex);
                if (FALSE == bRet)
                {
                    Fail("ReleaseMutex failed [GetLastError()=%u]\n",
                         GetLastError());
                }
            }

            dwRet = WaitForSingleObject(pi.hProcess, INFINITE);
            if (WAIT_FAILED == dwRet)
            {
                Fail("WaitForMultipleObjects failed [GetLastError()=%u]\n",
                     GetLastError());
            }

            if (!GetExitCodeProcess(pi.hProcess, &dwExitCode))
            {
                Trace("GetExitCodeProcess call failed LastError:(%u)\n",
                      GetLastError());
                dwExitCode = FAIL;
            }

            if (iDesiredExitCode != dwExitCode)
            {
                Fail("Wrong return code: %u [%d]\n", dwExitCode, iDesiredExitCode);
            }
            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
            Trace("Process wait test done\n");
            Trace("======================================================================\n");
        }

        if (bLocalWaitAll)
        {
            Trace("======================================================================\n");
            Trace("WaitAll with local thread awakening test\n");
            Trace("----------------------------------------\n");

            hThread = CreateThread(NULL, 0, EventTestThread, (PVOID)hEvent, 0, &dwSlaveThreadTid);
            if (NULL == hThread)
            {
                Fail("CreateThread failed [GetLastError()=%u]\n",
                     GetLastError());
            }

            dwRet = WaitForMultipleObjects(2, hEvent, TRUE, INFINITE);
            if (WAIT_FAILED == dwRet)
            {
                Fail("WaitForMultipleObjects failed [GetLastError()=%u]\n",
                     GetLastError());
            }

            hObjs[0] = hThread;
            dwRet = WaitForMultipleObjects(1, hObjs, FALSE, INFINITE);
            if (WAIT_FAILED == dwRet)
            {
                Fail("WaitForMultipleObjects failed [GetLastError()=%u]\n",
                     GetLastError());
            }

            CloseHandle(hThread);
            Trace("WaitAll with local thread awakening test done\n");
            Trace("======================================================================\n");
        }

        if (bRemoteWaitAll)
        {
            Trace("======================================================================\n");
            Trace("WaitAll with remote thread awakening test\n");
            Trace("----------------------------------------\n");

            ZeroMemory ( &si, sizeof(si) );
            si.cb = sizeof(si);
            ZeroMemory ( &pi, sizeof(pi) );

            sprintf_s (szCmd, 128, "child6 -mutex_and_named_event %s", szTestName);
            szCmd[127] = 0;

            LPWSTR szCmdW = convert(szCmd);
            bRet = CreateProcessW(NULL, szCmdW, NULL, NULL, FALSE,
                                  0, NULL, NULL, &si, &pi);
            free(szCmdW);
            if (FALSE == bRet)
            {
                Fail("CreateProcessW failed [GetLastError()=%u]\n",
                     GetLastError());
            }

            Sleep(1000);

            hObjs[0] = hMutex;
            hObjs[1] = hNamedEvent;

            dwRet = WaitForMultipleObjects(2, hObjs, TRUE, INFINITE);
            if (WAIT_FAILED == dwRet)
            {
                 Fail("WaitForMultipleObjects failed [GetLastError()=%u]\n",
                      GetLastError());
            }

            bRet = ReleaseMutex(hMutex);
            if (FALSE == bRet)
            {
                Fail("ReleaseMutex failed [GetLastError()=%u]\n",
                     GetLastError());
            }

            dwRet = WaitForSingleObject(pi.hProcess, INFINITE);
            if (WAIT_FAILED == dwRet)
            {
                Fail("WaitForMultipleObjects failed [GetLastError()=%u]\n",
                     GetLastError());
            }

            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
            Trace("WaitAll with remote thread awakening test done\n");
            Trace("======================================================================\n");
        }
    }

    return 0;
}

PALTEST(threading_WaitForMultipleObjectsEx_test6_paltest_waitformultipleobjectsex_test6, "threading/WaitForMultipleObjectsEx/test6/paltest_waitformultipleobjectsex_test6")
{
    DWORD dwRet;
    DWORD dwSlaveThreadTid = 0;
    int i, iCnt;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    srand(time(NULL) * GetCurrentProcessId());

    if (argc == 1)
    {
        g_bMutex = 1;
        g_bEvent = 1;
        g_bNamedEvent = 1;
        g_bSemaphore = 1;
        g_bProcess = 1;
        g_bLocalWaitAll = 1;
        g_bRemoteWaitAll = 1;
    }
    else
    {
        for (i=1;i<argc;i++)
        {
            if (0 == strcmp(argv[i], "-mutex"))
            {
                g_bMutex = 1;
            }
            else if (0 == strcmp(argv[i], "-event"))
            {
                g_bEvent = 1;
            }
            else if (0 == strcmp(argv[i], "-namedevent"))
            {
                g_bNamedEvent = 1;
            }
            else if (0 == strcmp(argv[i], "-semaphore"))
            {
                g_bSemaphore = 1;
            }
            else if (0 == strcmp(argv[i], "-process"))
            {
                g_bProcess = 1;
            }
            else if (0 == strcmp(argv[i], "-localwaitall"))
            {
                g_bLocalWaitAll = 1;
            }
            else if (0 == strcmp(argv[i], "-remotewaitall"))
            {
                g_bRemoteWaitAll = 1;
            }
            else if (0 == strcmp(argv[i], "-all"))
            {
                g_bMutex = 1;
                g_bEvent = 1;
                g_bNamedEvent = 1;
                g_bSemaphore = 1;
                g_bProcess = 1;
                g_bLocalWaitAll = 1;
                g_bRemoteWaitAll = 1;
            }
            else if (0 == strcmp(argv[i], "-random"))
            {
                g_bRandom = 1;
            }
            else if ((0 == strcmp(argv[i], "-count")) && (argc > i+1))
            {
                i++;
                iCnt = atoi(argv[i]);
                if (iCnt > 0 && iCnt < MAX_COUNT)
                {
                    iCount = iCnt;
                }
            }
            else if ((0 == strcmp(argv[i], "-threads")) && (argc > i+1))
            {
                i++;
                iCnt = atoi(argv[i]);
                if (iCnt > 0 && iCnt <= MAX_THREADS)
                {
                    iThreads = iCnt;
                }
            }
            else
            {
                Trace("Unknown option %s ignored\n", argv[i]);
            }
        }
    }


    iCnt = 0;
    for (i=0;i<iThreads;i++)
    {
    hThreads[iCnt] = CreateThread(NULL, 0, TestThread, (VOID*)iCnt, 0, &dwSlaveThreadTid);
    if (NULL == hThreads[iCnt])
    {
        Trace("Failed to create thread\n");
    }
    else
    {
        iCnt++;
    }
    }

    if (0 == iCnt)
    {
        Fail("Can't create any thread\n");
    }

    for (i=0; i<iCnt; i+=64)
    {
        dwRet = WaitForMultipleObjects(MIN(64, iCnt-i), &hThreads[i], TRUE, INFINITE);
        if (WAIT_FAILED == dwRet)
        {
            Fail("WaitForMultipleObjects failed [dwRet=%u GetLastError()=%u iCnt=%d i=%d]\n",
             dwRet, GetLastError(), iCnt, i);
        }
    }


    for (i=0; i<iCnt; i++)
    {
        CloseHandle(hThreads[i]);
    }

    PAL_Terminate();
    return PASS;
}
