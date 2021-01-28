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
#include "pthread_helpers.hpp"

char * threadName;
char * expectedThreadName;
char * actualThreadName;

DWORD PALAPI SetThreadDescriptionTestThread(LPVOID lpParameter) 
{
    HANDLE palThread = GetCurrentThread();
    WCHAR wideThreadName[256];

    MultiByteToWideChar(CP_ACP, 0, threadName, strlen(threadName)+1, wideThreadName, 256);
    SetThreadDescription(palThread, wideThreadName);
    actualThreadName = GetThreadName();

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
        return FAIL;
    }
    
    // verify that thread name truncations works correctly on linux on macOS.
    char * threadName = "aaaaaaa_15chars_aaaaaaa_31chars_aaaaaaaaaaaaaaaaaaaaaaa_63chars_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    char * expected;
    #if defined(__APPLE__)
    expected = "aaaaaaa_15chars_aaaaaaa_31chars_aaaaaaaaaaaaaaaaaaaaaaa_63chars";
    #else
    expected = "aaaaaaa_15chars";
    #endif
    
    if (!SetThreadDescriptionTest(threadName, expected))
    {
        return PASS;
    }

    return FAIL;
}

PALTEST(threading_SetThreadDescription_test1_paltest_setthreaddescription_test1, "threading/SetThreadDescription/test1/paltest_setthreaddescription_test1")
{
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    BOOL result = SetThreadDescriptionTests();
    if(actualThreadName) free(actualThreadName);
    if (!result)
    {
        Fail("Test Failed");
    }
    
    PAL_Terminate();
    return PASS;
} 

