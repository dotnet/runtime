// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
** Source Code: main.c and mutex.c
**    main.c creates process and waits for all processes to get over
**    mutex.c creates a mutex and then calls threads which will contend for the mutex
**
** This test is for WFMO Test case for Mutex
** Algorithm
** o	Create PROCESS_COUNT processes.
** o	Main Thread of each process creates OBJECT_TYPE Object
**
** Author: ShamitP
**
**
**============================================================
*/

#include <palsuite.h>
#include "resulttime.h"

/* Test Input Variables */
unsigned int PROCESS_COUNT = 3;
unsigned int THREAD_COUNT = 30;
unsigned int REPEAT_COUNT = 40;
unsigned int SLEEP_LENGTH = 4;
unsigned int RELATION_ID  = 1001;



struct TestStats{
    DWORD        operationTime;
    unsigned int relationId;
    unsigned int processCount;
    unsigned int threadCount;
    unsigned int repeatCount;
    char*        buildNumber;

};

int GetParameters( int argc, char **argv)
{
  if( (argc != 6) || ((argc == 1) && !strcmp(argv[1],"/?"))
       || !strcmp(argv[1],"/h") || !strcmp(argv[1],"/H"))
    {
        printf("PAL -Composite WFMO Test\n");
        printf("Usage:\n");
        printf("main\n\t[PROCESS_COUNT  [greater than 0] \n");
        printf("\t[THREAD_COUNT  [greater than 0] \n");
        printf("\t[REPEAT_COUNT  [greater than 0]\n");
        printf("\t[SLEEP_LENGTH  [greater than 0]\n");
        printf("\t[RELATION_ID  [greater than 0]\n");



        return -1;
    }

    PROCESS_COUNT = atoi(argv[1]);
    if( (PROCESS_COUNT < 1) || (PROCESS_COUNT > MAXIMUM_WAIT_OBJECTS) )
    {
        printf("\nMain Process:Invalid PROCESS_COUNT number, Pass greater than 1 and less than PROCESS_COUNT %d\n", MAXIMUM_WAIT_OBJECTS);
        return -1;
    }

    THREAD_COUNT = atoi(argv[2]);
    if( (THREAD_COUNT < 1) || (THREAD_COUNT > MAXIMUM_WAIT_OBJECTS) )
    {
        printf("\nInvalid THREAD_COUNT number, Pass greater than 1 and less than %d\n", MAXIMUM_WAIT_OBJECTS);
        return -1;
    }

    REPEAT_COUNT = atoi(argv[3]);
    if( REPEAT_COUNT < 1)
    {
        printf("\nMain Process:Invalid REPEAT_COUNT number, Pass greater than 1\n");
        return -1;
    }

    SLEEP_LENGTH = atoi(argv[4]);
    if( SLEEP_LENGTH < 1)
    {
        printf("\nMain Process:Invalid SLEEP_LENGTH number, Pass greater than 1\n");
        return -1;
    }

    RELATION_ID = atoi(argv[5]);
    if( RELATION_ID < 1)
    {
        printf("\nMain Process:Invalid RELATION_ID number, Pass greater than 1\n");
        return -1;
    }



    return 0;
}

PALTEST(composite_wfmo_paltest_composite_wfmo, "composite/wfmo/paltest_composite_wfmo")
{
    unsigned int i = 0;
    HANDLE hProcess[MAXIMUM_WAIT_OBJECTS];

    STARTUPINFO si[MAXIMUM_WAIT_OBJECTS];
    PROCESS_INFORMATION pi[MAXIMUM_WAIT_OBJECTS];

    char lpCommandLine[MAX_PATH] = "";

    int returnCode = 0;
    DWORD processReturnCode = 0;
    int testReturnCode = PASS;

    char fileName[MAX_PATH];
    FILE *pFile = NULL;
    DWORD dwStartTime;
    struct TestStats testStats;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    if(GetParameters(argc, argv))
    {
        Fail("Error in obtaining the parameters\n");
    }

     /* Register the start time */
    dwStartTime = GetTickCount();
    testStats.relationId = 0;
    testStats.relationId   = RELATION_ID;
    testStats.processCount = PROCESS_COUNT;
    testStats.threadCount  = THREAD_COUNT;
    testStats.repeatCount  = REPEAT_COUNT;
    testStats.buildNumber  = getBuildNumber();



    _snprintf(fileName, MAX_PATH, "main_wfmo_%d_.txt",testStats.relationId);
    pFile = fopen(fileName, "w+");
    if(pFile == NULL)
    {
        Fail("Error in opening main file for write\n");
    }

    for( i = 0; i < PROCESS_COUNT; i++ )
    {

        ZeroMemory( lpCommandLine, MAX_PATH );
        if ( _snprintf( lpCommandLine, MAX_PATH-1, "mutex %d %d %d %d %d", i, THREAD_COUNT, REPEAT_COUNT, SLEEP_LENGTH, RELATION_ID) < 0 )
        {
            Trace ("Error: Insufficient commandline string length for iteration [%d]\n", i);
        }

        /* Zero the data structure space */
        ZeroMemory ( &pi[i], sizeof(pi[i]) );
        ZeroMemory ( &si[i], sizeof(si[i]) );

        /* Set the process flags and standard io handles */
        si[i].cb = sizeof(si[i]);

        //Create Process
        if(!CreateProcess( NULL, /* lpApplicationName*/
                          lpCommandLine, /* lpCommandLine */
                          NULL, /* lpProcessAttributes  */
                          NULL, /* lpThreadAttributes */
                          TRUE, /* bInheritHandles */
                          0, /* dwCreationFlags, */
                          NULL, /* lpEnvironment  */
                          NULL, /* pCurrentDirectory  */
                          &si[i], /* lpStartupInfo  */
                          &pi[i] /* lpProcessInformation  */
                          ))
        {
            Fail("Process Not created for [%d], the error code is [%d]\n", i, GetLastError());
        }
        else
        {
            hProcess[i] = pi[i].hProcess;
            // Trace("Process created for [%d]\n", i);
        }

    }

    returnCode = WaitForMultipleObjects( PROCESS_COUNT, hProcess, TRUE, INFINITE);
    if( WAIT_OBJECT_0 != returnCode )
    {
        Trace("Wait for Object(s) @ Main thread for %d processes returned %d, and GetLastError value is %d\n", PROCESS_COUNT, returnCode, GetLastError());
    }

    for( i = 0; i < PROCESS_COUNT; i++ )
    {
        /* check the exit code from the process */
        if( ! GetExitCodeProcess( pi[i].hProcess, &processReturnCode ) )
        {
            Trace( "GetExitCodeProcess call failed for iteration %d with error code %u\n",
                i, GetLastError() );

            testReturnCode = FAIL;
        }

        if(processReturnCode == FAIL)
        {
            Trace( "Process [%d] failed and returned FAIL\n", i);
            testReturnCode = FAIL;
        }

        if(!CloseHandle(pi[i].hThread))
        {
            Trace("Error:%d: CloseHandle failed for Process [%d] hThread\n", GetLastError(), i);
        }

        if(!CloseHandle(pi[i].hProcess) )
        {
            Trace("Error:%d: CloseHandle failed for Process [%d] hProcess\n", GetLastError(), i);
        }
    }

    testStats.operationTime = GetTimeDiff(dwStartTime);
    fprintf(pFile, "%d,%d,%d,%d,%d,%s\n", testStats.operationTime, testStats.relationId, testStats.processCount, testStats.threadCount, testStats.repeatCount, testStats.buildNumber);
    if(fclose(pFile))
    {
        Trace("Error: fclose failed for pFile\n");
        testReturnCode = FAIL;
    }

    if( testReturnCode == PASS)
    {
        Trace("Test Passed\n");
    }
    else
    {
        Trace("Test Failed\n");
    }
    PAL_Terminate();
    return testReturnCode;
}
