//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
** 
** Source:  test1.c (FreeLibraryAndExitThread)
**
** Purpose: Tests the PAL implementation of the FreeLibraryAndExitThread
**          function. FreeLibraryAndExitThread when run will exit the 
**          process that it is called within, therefore we create a
**          thread to run the API. Then test for the existance of the
**          thread and access to the library.
**
**
**===================================================================*/

#include <palsuite.h>

/*Define platform specific information*/

/* SHLEXT is defined only for Unix variants */
#if defined(SHLEXT)
#define LibraryName    "dlltest"SHLEXT
#else
#define LibraryName    "dlltest"
#endif

#define TIMEOUT 60000

BOOL  PALAPI StartThreadTest();
DWORD PALAPI CreateTestThread(LPVOID);
BOOL  PALAPI TestDll(HMODULE, int);

int __cdecl main(int argc, char* argv[])
{
    /*Initialize the PAL*/
    if ((PAL_Initialize(argc, argv)) != 0)
    {
        return (FAIL);
    }

    if (!StartThreadTest())
    {
        Fail("ERROR: FreeLibraryAndExitThread test failed.\n");
    }

    /*Terminate the PAL*/
    PAL_Terminate();
    return PASS;

}


BOOL  PALAPI StartThreadTest()
{
    HMODULE hLib;
    HANDLE  hThread;  
    DWORD   dwThreadId;
    LPTHREAD_START_ROUTINE lpStartAddress =  &CreateTestThread;
    LPVOID lpParameter = lpStartAddress;
    DWORD rc = -1;
    /*Load library (DLL).*/
    hLib = LoadLibrary(LibraryName);
    if(hLib == NULL)
    {
        Trace("ERROR: Unable to load library %s\n", LibraryName);
        
        return (FALSE);
    }

    /*Start the test thread*/
    hThread = CreateThread(NULL, 
                            (DWORD)0,
                            lpParameter,
                            hLib,
                            (DWORD)NULL,
                            &dwThreadId);
    if(hThread == NULL)
    {
        Trace("ERROR:%u: Unable to create thread.\n",
                GetLastError());

        FreeLibrary(hLib);
        return (FALSE);
    }

    /*Wait on thread.*/
    rc = WaitForSingleObject(hThread, TIMEOUT);
    if( rc != WAIT_OBJECT_0 )
    {
        Trace("ERROR:%u: hThread=0x%4.4lx not exited by "
            "FreeLibraryAndExitThread, RC[%d]\n",
            GetLastError(),  
            hThread, rc);

// There is a possibility that the other thread might 
// still be using the library VSW:337893
//        FreeLibrary(hLib);
        CloseHandle(hThread);
        return (FALSE);
    }
            
    /*Test access to DLL.*/
    if(!TestDll(hLib, 0))
    {
        Trace("ERROR: TestDll function returned FALSE "
            "expected TRUE\n.");
        
        CloseHandle(hThread);
        return (FALSE);
    }

    FreeLibrary(hLib);
    /*Clean-up thread.*/
    CloseHandle(hThread);

    return (TRUE);
}

BOOL PALAPI TestDll(HMODULE hLib, int testResult)
{
    int     RetVal;
    char    FunctName[] = "DllTest";
    FARPROC DllAddr;    

    /* Attempt to grab the proc address of the dll function.
     * This one should succeed.*/
    if(testResult == 1)
    {
        DllAddr = GetProcAddress(hLib, FunctName);
        if(DllAddr == NULL)
        {
            Trace("ERROR: Unable to load function \"%s\" library \"%s\"\n", 
                    FunctName,
                    LibraryName);
            return (FALSE);
        }
        /* Run the function in the DLL, 
         * to ensure that the DLL was loaded properly.*/
        RetVal = DllAddr();
        if (RetVal != 1)
        {
            Trace("ERROR: Unable to receive correct information from DLL! "
                ":expected \"1\", returned \"%d\"\n",
                RetVal);
            return (FALSE);
        }
    }

    /* Attempt to grab the proc address of the dll function.
     * This one should fail.*/
    if(testResult == 0)
    {
        DllAddr = GetProcAddress(hLib, FunctName);
        if(DllAddr != NULL)
        {
            Trace("ERROR: Able to load function \"%s\" from free'd"
                " library \"%s\"\n", 
                FunctName, 
                LibraryName);
            return (FALSE);
        }
    }
    return (TRUE);
}

DWORD PALAPI CreateTestThread(LPVOID lpParam)
{
    /* Test access to DLL.*/
    TestDll(lpParam, 1);

    /*Free library and exit thread.*/
    FreeLibraryAndExitThread(lpParam, (DWORD)0);

    /* NOT REACHED */

    /*Infinite loop, we should not get here.*/
    while(1);

    return (DWORD)0;
}

