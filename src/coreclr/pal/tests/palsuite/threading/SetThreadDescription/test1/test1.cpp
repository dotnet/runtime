// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test1.cpp
**
** Purpose: Test for SetThreadDescription.  Create a thread, call 
** SetThreadDescription, and then verify that the name of the thread
** matches what was set.
**
**=========================================================*/

#include <palsuite.h>

char * threadName = NULL;
char * expectedThreadName = NULL;
char * actualThreadName = new char[256];

DWORD PALAPI SetThreadDescriptionTestThread(LPVOID lpParameter) 
{
    HRESULT result;
    HANDLE palThread = GetCurrentThread();
    WCHAR wideThreadName[256];

    MultiByteToWideChar(CP_ACP, 0, threadName, strlen(threadName)+1, wideThreadName, 256);
    SetThreadDescription(palThread, wideThreadName);
    result = GetThreadDescription(palThread, 256, actualThreadName);
    return 0;
}


BOOL SetThreadDescriptionTest(char* name, char* expected)
{
    BOOL bResult = FALSE;
    LPSECURITY_ATTRIBUTES lpThreadAttributes = NULL;
    DWORD dwStackSize = 0; 
    LPTHREAD_START_ROUTINE lpStartAddress =  &SetThreadDescriptionTestThread;
    LPVOID lpParameter = (LPVOID)SetThreadDescriptionTestThread;
    DWORD dwCreationFlags = 0; 
    DWORD dwThreadId = 0;
    
    threadName = name;
    expectedThreadName = expected;

    HANDLE hThread = CreateThread(lpThreadAttributes, 
                            dwStackSize, lpStartAddress, lpParameter, 
                            dwCreationFlags, &dwThreadId );
   
    if (hThread != INVALID_HANDLE_VALUE)
    {
        WaitForSingleObject(hThread, INFINITE);
        bResult = strcmp(actualThreadName, expectedThreadName) == 0;
    }
    else
    {
        Trace("Unable to create SetThreadDescription test thread");
    }

    return bResult;
}

BOOL SetThreadDescriptionTests()
{
    if (!SetThreadDescriptionTest("Hello, World", "Hello, World"))
    {
        Trace("Setting thread name failed");
        return false;
    }
    
    // verify that thread name truncations works correctly on linux on macos
    char * threadName;
    char * expected;
    #if defined(__linux__)
    threadName = "linuxstring15characters";
    expected = "linuxstring15ch";
    #endif

    #if defined(__APPLE__)
    threadName = "appplestring63charactersappplestring63charactersappplestring63characters";
    expected = "appplestring63charactersappplestring63charactersappplestring63c";
    #endif

    if (!SetThreadDescriptionTest(threadName, expected))
    {
        Trace("Setting thread name truncation failed");
        return false;
    }

    return true;
}

PALTEST(threading_SetThreadDescription_test1_paltest_setthreaddescription_test1, "threading/SetThreadDescription/test1/paltest_setthreaddescription_test1")
{
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    if (!SetThreadDescriptionTests())
    {
        delete[] actualThreadName;
        Fail("Test Failed");
    }
    
    delete[] actualThreadName;
    PAL_Terminate();
    return PASS;
} 

