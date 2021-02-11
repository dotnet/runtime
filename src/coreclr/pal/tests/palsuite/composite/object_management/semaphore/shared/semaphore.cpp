// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source Code: main.c and semaphore.c
**     main.c creates process and waits for all processes to get over
**     semaphore.c creates a semaphore and then calls threads which will contend for the semaphore
**
** This test is for Object Management Test case for semaphore where Object type is shareable.
** Algorithm
** o	Main Process Creates OBJECT_TYPE Object
** o	Create PROCESS_COUNT processes aware of the Shared Object
**
**
**
**============================================================
*/

#include <palsuite.h>
#include "resultbuffer.h"
#include "resulttime.h"

#define TIMEOUT 5000
/* Test Input Variables */
unsigned int USE_PROCESS_COUNT = 0;
unsigned int THREAD_COUNT = 0;
unsigned int REPEAT_COUNT = 0;
unsigned int RELATION_ID= 0;

/* Capture statistics at per thread basis */
struct statistics{
    unsigned int processId;
    unsigned int operationsFailed;
    unsigned int operationsPassed;
    unsigned int operationsTotal;
    DWORD        operationTime;
    unsigned int relationId;
};

struct ProcessStats{
    unsigned int processId;
    DWORD        operationTime;
    unsigned int relationId;
};

/* Semaphore variables */
unsigned long lInitialCount = 1; /* Signaled */
unsigned long lMaximumCount = 1; /* Maximum value of 1 */

HANDLE StartTestsEvHandle = NULL;
HANDLE hSemaphoreHandle = NULL;

/* Results Buffer */
ResultBuffer *resultBuffer = NULL;

int testStatus;

const char sTmpEventName[MAX_PATH] = "StartTestEvent";
char objectSuffix[MAX_PATH];

void PALAPI Run_Thread_semaphore_shared(LPVOID lpParam);

int GetParameters( int argc, char **argv)
{
    if( (!((argc == 5) || (argc == 6) ) )|| ((argc == 1) && !strcmp(argv[1],"/?"))
       || !strcmp(argv[1],"/h") || !strcmp(argv[1],"/H"))
    {
        printf("PAL -Composite Object Management event Test\n");
        printf("Usage:\n");
        printf("main\n\t[PROCESS_COUNT (greater than 1)] \n");
        printf("\t[THREAD_COUNT (greater than 1)] \n");
        printf("\t[REPEAT_COUNT (greater than 1)]\n");
        printf("\t[RELATION_ID  [greater than or equal to 1]\n");
	 printf("\t[Object Name Suffix]\n");
        return -1;
    }

    USE_PROCESS_COUNT = atoi(argv[1]);
    if(USE_PROCESS_COUNT < 0)
    {
        printf("\nMain Process:Invalid PROCESS_COUNT number, Pass greater than 0 \n");
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

    RELATION_ID = atoi(argv[4]);
    if( RELATION_ID < 1)
    {
        printf("\nMain Process:Invalid RELATION_ID number, Pass greater than 1\n");
        return -1;
    }

    if(argc == 6)
    {
        strncpy(objectSuffix, argv[5], MAX_PATH-1);
    }

    return 0;
}

PALTEST(composite_object_management_semaphore_shared_paltest_semaphore_shared, "composite/object_management/semaphore/shared/paltest_semaphore_shared")
{
    unsigned int i = 0;
    HANDLE hThread[MAXIMUM_WAIT_OBJECTS];
    DWORD  threadId[MAXIMUM_WAIT_OBJECTS];

    char ObjName[MAX_PATH] = "SHARED_SEMAPHORE";
    DWORD dwParam = 0;

    int returnCode = 0;

    /* Variables to capture the file name and the file pointer at thread level*/
    char fileName[MAX_PATH];
    FILE *pFile = NULL;
    struct statistics* buffer = NULL;
    int statisticsSize = 0;

    /* Variables to capture the file name and the file pointer at process level*/
    char processFileName[MAX_PATH];
    FILE *pProcessFile = NULL;
    struct ProcessStats processStats;
    DWORD dwStartTime;

    testStatus = PASS;

    ZeroMemory( objectSuffix, MAX_PATH );

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    if(GetParameters(argc, argv))
    {
        Fail("Error in obtaining the parameters\n");
    }

    if(argc == 6)
    {
        strncat(ObjName , objectSuffix, MAX_PATH - (sizeof(ObjName) + 1) );
    }

         /* Register the start time */
    dwStartTime = GetTickCount();
    processStats.relationId = RELATION_ID;
    processStats.processId  = USE_PROCESS_COUNT;

    _snprintf(processFileName, MAX_PATH, "%d_process_semaphore_%d_.txt", USE_PROCESS_COUNT, RELATION_ID);
    pProcessFile = fopen(processFileName, "w+");
    if(pProcessFile == NULL)
    {
        Fail("Error in opening process File file for write for process [%d]\n", USE_PROCESS_COUNT);
    }

    statisticsSize = sizeof(struct statistics);

    _snprintf(fileName, MAX_PATH, "%d_thread_semaphore_%d_.txt", USE_PROCESS_COUNT, RELATION_ID);
    pFile = fopen(fileName, "w+");
    if(pFile == NULL)
    {
        Fail("Error in opening file for write for process [%d]\n", USE_PROCESS_COUNT);
    }
    // For each thread we will log operations failed (int), passed (int), total (int)
    // and number of ticks (DWORD) for the operations
    resultBuffer = new ResultBuffer( THREAD_COUNT, statisticsSize);

    /* Create Start Tests event */
    StartTestsEvHandle  = CreateEvent( NULL, /* lpEventAttributes*/
                                        TRUE,  /* bManualReset */
                                        FALSE,   /* bInitialState */
                                        NULL);  /* name of Event */

    if( StartTestsEvHandle  == NULL )
    {
        Fail("Error:%d: Unexpected failure "
            "to create %s Event for process count %d\n", GetLastError(), sTmpEventName, USE_PROCESS_COUNT );

    }

    hSemaphoreHandle = CreateSemaphore(
                                NULL, /* lpSemaphoreAttributes */
                                lInitialCount, /*lInitialCount*/
                                lMaximumCount, /*lMaximumCount */
                                ObjName,
                                0,
                                0
                               );


    if( (hSemaphoreHandle == NULL) || (GetLastError() != ERROR_ALREADY_EXISTS) )
    {
        Fail("Unable to create Semaphore handle for process id [%d], returned error [%d], expected ERROR_ALREADY_EXISTS\n", i, GetLastError());
    }

    /* We already assume that the Semaphore was created previously*/

    for( i = 0; i < THREAD_COUNT; i++ )
    {
        dwParam = (int) i;
        //Create thread
        hThread[i] = CreateThread(
                                    NULL,                   /* no security attributes */
                                    0,                      /* use default stack size */
                                    (LPTHREAD_START_ROUTINE)Run_Thread_semaphore_shared,/* thread function */
                                    (LPVOID)dwParam,  /* argument to thread function */
                                    0,                      /* use default creation flags  */
                                    &threadId[i]     /* returns the thread identifier*/
                                  );

        if(hThread[i] == NULL)
        {
            Fail("Create Thread failed for %d process, and GetLastError value is %d\n", USE_PROCESS_COUNT, GetLastError());
        }
    }

    if (!SetEvent(StartTestsEvHandle))
    {
        Fail("Set Event for Start Tests failed for %d process, and GetLastError value is %d\n", USE_PROCESS_COUNT, GetLastError());
    }

    /* Test running */
    returnCode = WaitForMultipleObjects( THREAD_COUNT, hThread, TRUE, INFINITE);

    if( WAIT_OBJECT_0 != returnCode )
    {
        Trace("Wait for Object(s) for %d process returned %d, and GetLastError value is %d\n", USE_PROCESS_COUNT, returnCode, GetLastError());
        testStatus = FAIL;
    }

    processStats.operationTime = GetTimeDiff(dwStartTime);

    /* Write to a file*/
    if(pFile!= NULL)
    {
        for( i = 0; i < THREAD_COUNT; i++ )
        {
            buffer = (struct statistics *)resultBuffer->getResultBuffer(i);
            returnCode = fprintf(pFile, "%d,%d,%d,%d,%lu,%d\n", buffer->processId, buffer->operationsFailed, buffer->operationsPassed, buffer->operationsTotal, buffer->operationTime, buffer->relationId );
        }
    }
    fclose(pFile);
    /* Logging for the test case over, clean up the handles */

    for( i = 0; i < THREAD_COUNT; i++ )
    {
        if(!CloseHandle(hThread[i]) )
        {
            Trace("Error:%d: CloseHandle failed for Process [%d] hThread[%d]\n", GetLastError(), USE_PROCESS_COUNT, i);
            testStatus = FAIL;
        }
    }

    if(!CloseHandle(StartTestsEvHandle))
    {
        Trace("Error:%d: CloseHandle failed for Process [%d] StartTestsEvHandle\n", GetLastError(), USE_PROCESS_COUNT);
        testStatus = FAIL;
    }

    if(!CloseHandle(hSemaphoreHandle))
    {
        Trace("Error:%d: CloseHandle failed for Process [%d] hSemaphoreHandle\n", GetLastError(), USE_PROCESS_COUNT);
        testStatus = FAIL;
    }

    PAL_Terminate();
    return PASS;
}

void  PALAPI Run_Thread_semaphore_shared (LPVOID lpParam)
{
    unsigned int i = 0;
    DWORD dwWaitResult;

    int Id=(int)lpParam;

    struct statistics stats;
    DWORD dwStartTime;

	stats.relationId = RELATION_ID;
    stats.processId = USE_PROCESS_COUNT;
    stats.operationsFailed = 0;
    stats.operationsPassed = 0;
    stats.operationsTotal  = 0;
    stats.operationTime    = 0;

    dwWaitResult = WaitForSingleObject(
                            StartTestsEvHandle,   // handle to start test handle
                            TIMEOUT);

    if(dwWaitResult != WAIT_OBJECT_0)
    {
        Fail("Error while waiting for StartTest Event@ thread %d, RC is %d, Error is %d\n", Id, dwWaitResult, GetLastError());
    }

    dwStartTime = GetTickCount();

    for( i = 0; i < REPEAT_COUNT; i++ )
    {
        dwWaitResult = WaitForSingleObject(
                            hSemaphoreHandle,   // handle to Semaphore
                            TIMEOUT);

        if(dwWaitResult != WAIT_OBJECT_0)
        {
            stats.operationsFailed += 1;
            stats.operationsTotal  += 1;
            testStatus = FAIL;
            continue;
        }
        if (! ReleaseSemaphore(hSemaphoreHandle, 1, NULL))
        {
            // Deal with error.
            stats.operationsFailed += 1;
            stats.operationsTotal  += 1;
            // Probably need to have while true loop to attempt to release semaphore..
            testStatus = FAIL;
            continue;
        }

        stats.operationsTotal  += 1;
        stats.operationsPassed += 1;
    }

    stats.operationTime = GetTimeDiff(dwStartTime);
    if(resultBuffer->LogResult(Id, (char *)&stats))
    {
        Fail("Error:%d: while writing to shared memory, Thread Id is[%d] and Process id is [%d]\n", GetLastError(), Id, USE_PROCESS_COUNT);
    }
}
