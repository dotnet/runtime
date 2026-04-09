// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure YieldProcessor works, without 
**          causing test to hang
**
** Dependencies: PAL_Initialize
**               Fail
**               YieldProcessor
**               WaitForMultipleObject
**               CreateThread
**               GetLastError
** 

**
**===========================================================================*/


#include <palsuite.h>
#define THREAD_COUNT  10
#define REPEAT_COUNT  1000
#define TIMEOUT       60000
void PALAPI Run_Thread_yieldprocessor_test1(LPVOID lpParam);

/**
 * main
 *
 * executable entry point
 */
PALTEST(threading_YieldProcessor_test1_paltest_yieldprocessor_test1, "threading/YieldProcessor/test1/paltest_yieldprocessor_test1")
{
    DWORD  dwParam;
    HANDLE hThread[THREAD_COUNT];
    DWORD  threadId[THREAD_COUNT];
    
    int i = 0;   
    int returnCode = 0;

    /*PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
	    return FAIL;
    }


    for( i = 0; i < THREAD_COUNT; i++ )
    {
        dwParam = (int) i;
        //Create thread
        hThread[i] = CreateThread(
                                    NULL,                   /* no security attributes */
                                    0,                      /* use default stack size */
                                    (LPTHREAD_START_ROUTINE)Run_Thread_yieldprocessor_test1,/* thread function */
                                    (LPVOID)dwParam,  /* argument to thread function */
                                    0,                      /* use default creation flags  */
                                    &threadId[i]     /* returns the thread identifier*/                                  
                                  );

        if(hThread[i] == NULL)
        {
            Fail("Create Thread failed for iteration %d GetLastError value is %d\n", i, GetLastError());
        }
  
    } 


    returnCode = WaitForMultipleObjects(THREAD_COUNT, hThread, TRUE, TIMEOUT);
    if( WAIT_OBJECT_0 != returnCode )
    {
        Trace("Wait for Object(s) returned %d, expected value is  %d, and GetLastError value is %d\n", returnCode, WAIT_OBJECT_0, GetLastError());
    }

    PAL_Terminate();
    return PASS;

}

void  PALAPI Run_Thread_yieldprocessor_test1 (LPVOID lpParam)
{
    int i = 0;

    for(i=0; i < REPEAT_COUNT; i++ )
    {
       // No error code set nor does it have any return code
       YieldProcessor();
    }
}
