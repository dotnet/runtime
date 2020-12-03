// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests that DisableThreadLibraryCalls actually stops thread 
**          attach/detach notifications to a library.  Also tests how it
**          handles an invalid module handle.
**
**
**===================================================================*/

#include <palsuite.h>


/* SHLEXT is defined only for Unix variants */

#if defined(SHLEXT)
#define LibName  "testlib"SHLEXT
#define GETCALLCOUNT "GetCallCount"
#else
#define LibName  "testlib"
#define GETCALLCOUNT "_GetCallCount@0"
#endif

DWORD PALAPI ThreadFunc_DisableThreadLibraryCalls_test1(LPVOID lpParam);
int RunTest_DisableThreadLibraryCalls_test1(int DisableThreadCalls);

int __cdecl threading_DisableThreadLibraryCalls_test1(int argc, char* argv[]);
static PALTest threading_DisableThreadLibraryCalls_test1_lookup(threading_DisableThreadLibraryCalls_test1, "threading/DisableThreadLibraryCalls/test1");
int __cdecl threading_DisableThreadLibraryCalls_test1(int argc, char* argv[])
{
    int ret;

    if ((PAL_Initialize(argc,argv)) != 0)
    {
        return (FAIL);
    }


    /* 
     * Although MSDN says that DisableThreadLibraryCalls will fail if passed 
     * an invalid handle, it actually returns success!
     */
    ret = DisableThreadLibraryCalls(NULL);
    if (!ret)
    {
        Fail("DisableThreadLibraryCalls failed on an invalid module "
            "handle (it actually should pass)!\n");
    }
    

    /*
     * Test once without calling DisableThreadLibraryCalls and make sure we 
     * get expected results.
     */
    ret = RunTest_DisableThreadLibraryCalls_test1(0);
    if (ret != 2)
    {
        Fail("Expected to get 2 thread library calls, got %d!\n", ret);
    }
    

    /*
     * Test again, this time calling DisableThreadLibraryCalls.
     */
    ret = RunTest_DisableThreadLibraryCalls_test1(1);
    if (ret != 0)
    {
        Fail("Expected to get 0 thread library calls, got %d!\n", ret);
    }

    PAL_Terminate();
    return PASS;
}

/*
 * Thread entry point.  Doesn't do anything.
 */
DWORD PALAPI ThreadFunc_DisableThreadLibraryCalls_test1(LPVOID lpParam)
{
    return 0;
}


int RunTest(int DisableThreadCalls)
{
    HMODULE LibMod;
    HANDLE hThread;
    DWORD threadID;
    DWORD WaitRet;
    int (*GetCallCount)();
    int count;

    LibMod = LoadLibrary(LibName);
    if (LibMod == NULL)
    {
        Fail("Unable to load test library!\nGetLastError returned %d\n", 
            GetLastError());
    }

    GetCallCount = (int(*)())GetProcAddress(LibMod, GETCALLCOUNT);
    if (GetCallCount == NULL)
    {
        Fail("Unable to get function GetCallCount from library!\n"
            "GetLastError returned %d\n", GetLastError());
    }

    if (DisableThreadCalls)
    {
        if (!DisableThreadLibraryCalls(LibMod))
        {
            Fail("DisabledThreadLibraryCalls failed!\n"
                "GetLastError returned %d!\n", GetLastError());
        }
    }

    hThread = CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE) ThreadFunc_DisableThreadLibraryCalls_test1,
        NULL, 0, &threadID);

    if (hThread == NULL)
    {
        Fail("Unable to create a thread!\n");
    }

    WaitRet = WaitForSingleObject(hThread, INFINITE);
    if (WaitRet == WAIT_FAILED)
    {
        Fail("Unable to wait on thread!\nGetLastError returned %d\n", 
            GetLastError());
    }

    count = GetCallCount();

    CloseHandle(hThread);

    if (!FreeLibrary(LibMod))
    {
        Fail("Failed freeing library!\nGetLastError returned %d\n",
            GetLastError());
    }
    
    return count;
}
