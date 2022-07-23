// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: criticalsection.c
**
** Purpose: Test Critical Section Reengineering PAL Effort
**
** PseudoCode:
	Preparation:
		Create PROCESS_COUNT processes.
		In each process create a Critical Section

	Test:
		Create THREAD_COUNT threads.
		In a loop repeated REPEAT_COUNT times:
			Enter Critical Section
				Do Work
			Leave Critical Section
		The main thread waits for all of the created threads to exit (WFMO wait all on the created thread handles) and call DeleteCriticalSection

	Parameters:
		PROCESS_COUNT: Number of processes
		THREAD_COUNT: Number of threads in each process
		REPEAT_COUNT: The number of times to execute the loop..

	Statistics Captured:
		Total elapsed time
		MTBF

     Scenario:
     			Single Process with Multiple threads.  Main thread creates critical section.
     			All other threads call EnterCriticalSection.   When thread enters critical section
     			it does some work and leaves critical section.

** Dependencies:
			CreateThread
**	              InitializeCriticalSection
**    	       EnterCriticalSection
**           	LeaveCriticalSection
**               	DeleteCriticalSection
**              	WaitForSingleObject
**
** Author: rameshg
**
**
**=========================================================*/

#include <palsuite.h>
#include "resultbuffer.h"

//Global Variables
DWORD dwThreadId;
long long GLOBAL_COUNTER ;
HANDLE g_hEvent;

/* Test Input Variables */
unsigned int USE_PROCESS_COUNT = 0;
unsigned int THREAD_COUNT = 0;
unsigned int REPEAT_COUNT = 0;
unsigned int SLEEP_LENGTH = 0;
unsigned int RELATION_ID = 0;


CRITICAL_SECTION CriticalSectionM; /* Critical Section Object (used as mutex) */


/* Capture statistics for each worker thread */
struct statistics{
    unsigned int processId;
    unsigned int operationsFailed;
    unsigned int operationsPassed;
    unsigned int operationsTotal;
    DWORD        operationTime; //Milliseconds
    unsigned int relationId;
};


/*Capture Statistics at a Process level*/
struct processStatistics{
    unsigned int processId;
    DWORD        operationTime; //Milliseconds
    unsigned int relationId;
};


ResultBuffer *resultBuffer;

//function declarations
int GetParameters( int , char **);
void setup (void);
void cleanup(void);
void incrementCounter(void);
DWORD PALAPI enterandleavecs( LPVOID );


/*
*Setup for the test case
*/

VOID
setup(VOID)
{

g_hEvent = CreateEvent(NULL,TRUE,FALSE, NULL);
if(g_hEvent == NULL)
{
	Fail("Create Event Failed\n"
		"GetLastError returned %d\n", GetLastError());
}

GLOBAL_COUNTER=0;
/*
* 	Create mutual exclusion mechanisms
*/
InitializeCriticalSection ( &CriticalSectionM );

}


/*
*	Cleanup for the test case
*/
VOID
cleanup(VOID)
{
    /*
     * Clean up Critical Section object
     */
    DeleteCriticalSection(&CriticalSectionM);
    PAL_Terminate();
}


/*function that increments a counter*/
VOID
incrementCounter(VOID)
{

	if (INT_MAX==GLOBAL_COUNTER)
		GLOBAL_COUNTER=0;

	GLOBAL_COUNTER++;

}

/*
 * Enter and Leave Critical Section
 */
DWORD
PALAPI
enterandleavecs( LPVOID lpParam )
{

	struct statistics stats;
	int loopcount = REPEAT_COUNT;
	int i;
	DWORD dwStart =0;

	int Id=(int)lpParam;

	//initialize structure to hold thread level statistics
	stats.relationId = RELATION_ID;
	stats.processId = USE_PROCESS_COUNT;
	stats.operationsFailed = 0;
	stats.operationsPassed = 0;
	stats.operationsTotal  = 0;
	stats.operationTime    = 0;

	//Wait for main thread to signal event
	if (WAIT_OBJECT_0 != WaitForSingleObject(g_hEvent,INFINITE))
		{
		Fail ("readfile: Wait for Single Object (g_hEvent) failed.  Failing test.\n"
	       "GetLastError returned %d\n", GetLastError());
		}

	//Collect operation start time
	dwStart = GetTickCount();

	//Operation starts loopcount times
	for(i = 0; i < loopcount; i++)
	{

		EnterCriticalSection(&CriticalSectionM);
		/*
		*Do Some Thing once you enter critical section
		*/
		incrementCounter();
		LeaveCriticalSection(&CriticalSectionM);

		stats.operationsPassed++;
		stats.operationsTotal++;
	}
	//collect operation end time
	stats.operationTime = GetTickCount() - dwStart;

	/*Trace("\n\n\n\nOperation Time %d\n", stats.operationTime);
	Trace("Operation Passed %d\n", stats.operationsPassed);
	Trace("Operation Total %d\n", stats.operationsTotal);
	Trace("Operation Failed %d\n", stats.operationsFailed); */

	if(resultBuffer->LogResult(Id, (char *)&stats))
	{
		Fail("Error while writing to shared memory, Thread Id is[%d] and Process id is [%d]\n", Id, USE_PROCESS_COUNT);
	}


    return 0;
}


PALTEST(composite_synchronization_criticalsection_paltest_synchronization_criticalsection, "composite/synchronization/criticalsection/paltest_synchronization_criticalsection")
{

/*
* 	Parameter to the threads that will be created
*/
DWORD dwThrdParam = 0;
HANDLE hThread[64];
unsigned int i = 0;
DWORD dwStart;

/* Variables to capture the file name and the file pointer*/
char fileName[MAX_PATH_FNAME];
char processFileName[MAX_PATH_FNAME];
FILE *hFile,*hProcessFile;
struct processStatistics processStats;

struct statistics* buffer;
int statisticsSize = 0;

/*
*	PAL Initialize
*/
if(0 != (PAL_Initialize(argc, argv)))
    {
	return FAIL;
    }

if(GetParameters(argc, argv))
    {
        Fail("Error in obtaining the parameters\n");
    }


/*setup file for process result collection */
_snprintf(processFileName, MAX_PATH_FNAME, "%d_process_criticalsection_%d_.txt", USE_PROCESS_COUNT, RELATION_ID);
hProcessFile = fopen(processFileName, "w+");
if(hProcessFile == NULL)
    {
        Fail("Error in opening file to write process results for process [%d]\n", USE_PROCESS_COUNT);
    }

//Initialize Process Stats Variables
processStats.operationTime = 0;
processStats.processId = USE_PROCESS_COUNT;
processStats.relationId = RELATION_ID;  //Will change later

//Start Process Time Capture
dwStart = GetTickCount();

//setup file for thread result collection
statisticsSize = sizeof(struct statistics);
_snprintf(fileName, MAX_PATH_FNAME, "%d_thread_criticalsection_%d_.txt", USE_PROCESS_COUNT, RELATION_ID);
hFile = fopen(fileName, "w+");
if(hFile == NULL)
{
    Fail("Error in opening file for write for process [%d]\n", USE_PROCESS_COUNT);
}

// For each thread we will log operations failed (int), passed (int), total (int)
// and number of ticks (DWORD) for the operations
resultBuffer = new ResultBuffer( THREAD_COUNT, statisticsSize);

/*
*	Call the Setup Routine
*/
setup();

//Create Thread Count Worker Threads

while (i< THREAD_COUNT)
{
    dwThrdParam = i;

    hThread[i] = CreateThread(
	NULL,
	0,
	enterandleavecs,
	(LPVOID)dwThrdParam,
	0,
	&dwThreadId);

    if ( NULL == hThread[i] )
    {
	Fail ( "CreateThread() returned NULL.  Failing test.\n"
	       "GetLastError returned %d\n", GetLastError());
    }
    i++;
}

/*
* Set Event to signal all threads to start using the CS
*/

if (0==SetEvent(g_hEvent))
{
	Fail ( "SetEvent returned Zero.  Failing test.\n"
	       "GetLastError returned %d\n", GetLastError());
}

/*
 * Wait for worker threads to complete
 *
 */
if ( WAIT_OBJECT_0 != WaitForMultipleObjects (THREAD_COUNT,hThread,TRUE, INFINITE))
{
	Fail ( "WaitForMultipleObject Failed.  Failing test.\n"
	       "GetLastError returned %d\n", GetLastError());
}


//Get the end time of the process
processStats.operationTime = GetTickCount() - dwStart;

//Write Process Result Contents to File
if(hProcessFile!= NULL)
    {
            fprintf(hProcessFile, "%d,%lu,%d\n", processStats.processId, processStats.operationTime, processStats.relationId );
    }

if (0!=fclose(hProcessFile))
{
	Fail("Unable to write process results to file"
		"GetLastError returned %d\n", GetLastError());
}


/*Write Threads Results to a file*/
if(hFile!= NULL)
{
    for( i = 0; i < THREAD_COUNT; i++ )
    {
        buffer = (struct statistics *)resultBuffer->getResultBuffer(i);
        fprintf(hFile, "%d,%d,%d,%d,%lu,%d\n", buffer->processId, buffer->operationsFailed, buffer->operationsPassed, buffer->operationsTotal, buffer->operationTime, buffer->relationId );
        //Trace("Iteration %d over\n", i);
    }
}

if (0!=fclose(hFile))
{
	Fail("Unable to write thread results to file"
		"GetLastError returned %d\n", GetLastError());
}

    /* Logging for the test case over, clean up the handles */
    //Trace("Contents of the buffer are [%s]\n", resultBuffer->getResultBuffer());


//Call Cleanup for Test Case
cleanup();

//Trace("Value of GLOBAL COUNTER %d \n", GLOBAL_COUNTER);
return (PASS);

}


int GetParameters( int argc, char **argv)
{

	if( (argc != 5) || ((argc == 1) && !strcmp(argv[1],"/?"))
       || !strcmp(argv[1],"/h") || !strcmp(argv[1],"/H"))
    {
        printf("PAL -Composite Critical Section Test\n");
        printf("Usage:\n");
	 printf("\t[PROCESS_COUNT] Greater than or Equal to  1 \n");
	 printf("\t[WORKER_THREAD_MULTIPLIER_COUNT]  Greater than or Equal to 1 and Less than or Equal to 64 \n");
        printf("\t[REPEAT_COUNT] Greater than or Equal to 1\n");
	 printf("\t[RELATION_ID  [Greater than or Equal to 1]\n");
        return -1;
    }

//  Trace("Args 1 is [%s], Arg 2 is [%s], Arg 3 is [%s]\n", argv[1], argv[2], argv[3]);

    USE_PROCESS_COUNT = atoi(argv[1]);
    if( USE_PROCESS_COUNT < 0)
    {
        printf("\nPROCESS_COUNT to greater than or equal to 1\n");
        return -1;
    }

    THREAD_COUNT = atoi(argv[2]);
    if( THREAD_COUNT < 1 || THREAD_COUNT > 64)
    {
        printf("\nTHREAD_COUNT to be greater than or equal to 1 or less than or equal to 64\n");
        return -1;
    }

    REPEAT_COUNT = atoi(argv[3]);
    if( REPEAT_COUNT < 1)
    {
        printf("\nREPEAT_COUNT to greater than or equal to 1\n");
        return -1;
    }

    RELATION_ID = atoi(argv[4]);
    if( RELATION_ID < 1)
    {
        printf("\nMain Process:Invalid RELATION_ID number, Pass greater than 1\n");
        return -1;
    }

    return 0;
}

