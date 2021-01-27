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

char * actualThreadName = 0;
char * expectedThreadName = "Hello, World";

DWORD PALAPI SetThreadDescriptionTestThread(LPVOID lpParameter) 
{
    HANDLE palThread = GetCurrentThread();
    WCHAR wideThreadName[32];
    MultiByteToWideChar(CP_ACP, 0, expectedThreadName, strlen(expectedThreadName)+1, wideThreadName, 32);
    SetThreadDescription(palThread, wideThreadName);
    GetThreadDescription(palThread, 32, actualThreadName);

    return 0;
}

BOOL SetThreadDescriptionTest()
{
    BOOL bResult = FALSE;
    LPSECURITY_ATTRIBUTES lpThreadAttributes = NULL;
    DWORD dwStackSize = 0; 
    LPTHREAD_START_ROUTINE lpStartAddress =  &SetThreadDescriptionTestThread;
    LPVOID lpParameter = (LPVOID)lpStartAddress;
    DWORD dwCreationFlags = 0; 
    DWORD dwThreadId = 0;
    
    actualThreadName = new char[32];
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

    delete[] actualThreadName;
    return bResult;
}

PALTEST(threading_SetThreadDescription_test1_paltest_setthreaddescription_test1, "threading/SetThreadDescription/test1/paltest_setthreaddescription_test1")
{
    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    if (!SetThreadDescriptionTest())
    {
        Fail("Test Failed");
    }
    
    PAL_Terminate();
    return PASS;
} 

