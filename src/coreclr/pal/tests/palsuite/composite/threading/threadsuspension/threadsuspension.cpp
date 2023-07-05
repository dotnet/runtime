// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** 	Source: \composite\threading\threadsuspension\threadsuspension.c
**
** 	Purpose: To verify Thread Suspension Reegneering effort for this milestone

	PsedoCode:

		Preparation:
		Create PROCESS_COUNT processes.
		Test:
			Create Worker Thread
				Start Reading and writing to a File

			Create Worker Thread
				In an infinite loop do the following
					Enter Critical Section
		 				Increment Counter
					Leave Critical Section

			Create Worker Thread
				Allocate Memory and Free Memory

			Create Worker Thread
				In a tight loop add numbers

			In a loop repeated REPEAT_COUNT times

				Create Thread

					Suspend all worker threads
					Resume all worker threads

			At the end of the loop call PAL_Shutdown

		Parameters:
			PROCESS_COUNT: Number of processes
			WORKER_THREAD_MULTIPLIER_COUNT: Number of instances of worker threads in each process
			REPEAT_COUNT: The number of times to execute the loop.

		Statistics Captured:
			Total elapsed time
			MTBF


	Scenario:
**
		One thread suspends all remaining threads which are in the middle of doing some work and resume all threads
		Thread 1:  Reading and Writing File
		Thread 2:  Enter and Leave Critical Section
		Thread 3:  Allocating chunks of memory
		Thread 4:  Perform Unsafe Operation (printf, malloc)
		Thread 5:  Suspends Thread 1 to Thread 4 and resumes them

**
**
**
** 	Dependencies:
**
**

**
**=========================================================*/

#include <palsuite.h>
#include "resultbuffer.h"

#define BUFSIZE 4096
#define NUMBER_OF_WORKER_THREAD_TYPES 4
#define THREAD_MAX 64

#define TEST_FAIL 1
#define TEST_PASS 0


DWORD GLOBAL_COUNTER ;
DWORD UNIQUE_FILE_NUMBER=0;
HANDLE g_hEvent;

bool failFlag = false;  //To Track failure at the Operation Level

// 2 dimensional array to hold thread handles for each worker thread
HANDLE hThread[NUMBER_OF_WORKER_THREAD_TYPES][THREAD_MAX];

/*unsigned int g_readfileoperation;
unsigned int g_enterleavecsoperation;
unsigned int g_allocatefreeoperation;
unsigned int g_doworintightloop;
*/

int TYPES_OF_WORKER_THREAD = NUMBER_OF_WORKER_THREAD_TYPES;

int testStatus=TEST_PASS;  //Indicates test failure


struct statistics{
    unsigned int processId;
    unsigned int operationsFailed;
    unsigned int operationsPassed;
    unsigned int operationsTotal;
    DWORD        operationTime;
    unsigned int relationId;
};

struct processStatistics{
    unsigned int processId;
    DWORD        operationTime;
    unsigned int relationId;
};

/* Results Buffer */
ResultBuffer *resultBuffer;


/* Test Input Variables */
unsigned int USE_PROCESS_COUNT = 0; 	//Identifies the Process number.  There could potentially
unsigned int WORKER_THREAD_MULTIPLIER_COUNT = 0;  //In this test case this represents the number of worker thread instances
unsigned int REPEAT_COUNT = 0; //Number of  Suspend Resume operation of worker threads
unsigned int RELATION_ID = 0;



CRITICAL_SECTION CriticalSectionM; /* Critical Section Object (used as mutex) */
CRITICAL_SECTION g_csUniqueFileName;

void PALAPI setup(void);
void PALAPI cleanup(void);
void PALAPI incrementCounter(void);
DWORD PALAPI readfile( LPVOID);
DWORD PALAPI enterandleave_cs( LPVOID);
DWORD PALAPI allocateandfree_memory( LPVOID);
DWORD PALAPI doworkintightloop_cs( LPVOID);
DWORD PALAPI suspendandresumethreads( LPVOID);
int GetParameters(int, char * *);


//Main Entry for the Thread Suspension Test Case
PALTEST(composite_threading_threadsuspension_paltest_threading_threadsuspension, "composite/threading/threadsuspension/paltest_threading_threadsuspension")
{

/*
* 	Parameter to the threads that will be created
*/


DWORD dwThrdParam = 0;
DWORD dwStart;

/* Variables to capture the file name and the file pointer*/
char fileName[MAX_PATH];
char processFileName[MAX_PATH];

FILE *hFile, *hProcessFile;
struct statistics* buffer;
struct processStatistics *processBuffer;

struct processStatistics processStats;

struct statistics* tmpBuf = NULL;
int statisticsSize = 0;

DWORD dwThreadId=0;
HANDLE hMainThread;
unsigned int i = 0;
int j = 0;


/*
*	PAL Initialize
*/

if(0 != (PAL_Initialize(argc, argv)))
    {
	return FAIL;
    }


//Get Parameters
if(GetParameters(argc, argv))
    {
        Fail("Error in obtaining the parameters\n");
    }


//Setup for Process Result Collection
statisticsSize = sizeof(struct statistics);
_snprintf(processFileName, MAX_PATH, "%d_process_threadsuspension_%d_.txt", USE_PROCESS_COUNT, RELATION_ID);
hProcessFile = fopen(processFileName, "w+");

if(hProcessFile == NULL)
    {
       Fail("Error in opening file to write process results for process [%d]\n", USE_PROCESS_COUNT);
    }

//Initialize Process Stats Variables
processStats.operationTime = 0;
processStats.processId = USE_PROCESS_COUNT;
processStats.relationId = RELATION_ID;

//Start Process Time Capture
dwStart = GetTickCount();

//Setup for Thread Result Collection
statisticsSize = sizeof(struct statistics);
_snprintf(fileName, MAX_PATH, "%d_thread_threadsuspension_%d_.txt", USE_PROCESS_COUNT,RELATION_ID);
hFile = fopen(fileName, "w+");

if(hFile == NULL)
    {
        Fail("Error in opening file to write thread results for process [%d]\n", USE_PROCESS_COUNT);
    }

// For each thread we will log relationid (int), processid (int), operations failed (int), passed (int), total (int)
// and number of ticks (DWORD) for the operations
resultBuffer = new ResultBuffer( 1, statisticsSize);

/*
*	Call the Setup Routine
*/
setup();

Trace("WORKER_THREAD_MULTIPLIER_COUNT: %d \n", WORKER_THREAD_MULTIPLIER_COUNT);

//Create WORKER_THREAD_MULTIPLIER_COUNT Instances of each type of worker thread
for (i=0;i<WORKER_THREAD_MULTIPLIER_COUNT;i++)
{

 	    /*
	     * Create readfile thread
	     */
	    hThread[0][i] = CreateThread(
		NULL,
		0,
		readfile,
		NULL,
		0,
		&dwThreadId);

	    if ( NULL == hThread[0][i] )
	    {
		Fail ( "CreateThread() returned NULL.  Failing test.\n"
		       "GetLastError returned %d\n", GetLastError());
	    }



	    /*
	     * Create Enter and Leave Critical Section Thread
	     */
	    hThread[1][i] = CreateThread(
		NULL,
		0,
		enterandleave_cs,
		NULL,
		0,
		&dwThreadId);

	    if ( NULL == hThread[1][i] )
	    {
		Fail ( "CreateThread() returned NULL.  Failing test.\n"
		       "GetLastError returned %d\n", GetLastError());
	    }



	    /*
	     * Create Allocate and Free Memory Thread
	     */
	    hThread[2][i] = CreateThread(
		NULL,
		0,
		allocateandfree_memory,
		NULL,
		0,
		&dwThreadId);

	    if ( NULL == hThread[2][i])
	    {
		Fail ( "CreateThread() returned NULL.  Failing test.\n"
		       "GetLastError returned %d\n", GetLastError());
	    }



		/*
	     * Create Work in tight Loop thread
	     */
	    hThread[3][i] = CreateThread(
		NULL,
		0,
		doworkintightloop_cs,
		NULL,
		0,
		&dwThreadId);

	    if ( NULL == hThread[3][i])
	    {
		Fail ( "CreateThread() returned NULL.  Failing test.\n"
		       "GetLastError returned %d\n", GetLastError());
	    }



}





/*
     * Create Main test case thread that Suspends and Resumes Threads
     */
    hMainThread = CreateThread(
	NULL,
	0,
	suspendandresumethreads,
	(LPVOID)dwThrdParam,
	0,
	&dwThreadId);

    if ( NULL == hMainThread )
    {
	Fail ( "CreateThread() returned NULL.  Failing test.\n"
	       "GetLastError returned %d\n", GetLastError());
    }




/*
* Set Event to allow all threads to start
*/

if (0==SetEvent(g_hEvent))
{
	Fail ( "SetEvent returned Zero.  Failing test.\n"
	       "GetLastError returned %d\n", GetLastError());
}

/*
 * Wait for main thread to complete
 *
 */
 if (WAIT_OBJECT_0 != WaitForSingleObject (hMainThread, INFINITE))
 	{
 		Fail ("Main: Wait for Single Object (mainThread) failed.  Failing test.\n"
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


//Write to log file
//Trace("# of Read File Operations %d\n", g_readfileoperation);
//Trace("# of Enter and Leace CS Operations %d\n", g_enterleavecsoperation);
//Trace("# of Do Work In Tight Loop Operations %d\n", g_doworintightloop);
//Trace("# of Allocate and Free Operations %d\n", g_allocatefreeoperation);


//Write Thread Result Contents to File
if(hFile!= NULL)
    {
        for( i = 0; i < 1; i++ )
        {
            buffer = (struct statistics *)resultBuffer->getResultBuffer(i);
            fprintf(hFile, "%d,%d,%d,%d,%lu,%d\n", buffer->processId, buffer->operationsFailed, buffer->operationsPassed, buffer->operationsTotal, buffer->operationTime, buffer->relationId );
        }
    }

if (0!=fclose(hFile))
{
	Fail("Unable to write thread results to file"
		"GetLastError returned %d\n", GetLastError());
}

cleanup();

if (failFlag == TRUE)
{
	return FAIL;
}
else
{
	return PASS;
}
}


/*
*	Setup for the test case
*/

VOID
setup(VOID)
{
	/*Delete All Temporary Files Created by the previous execution of the test case*/
	HANDLE hSearch;
	BOOL fFinished = FALSE;
	WIN32_FIND_DATA FileData;

	//Start searching for .tmp files in the current directory.
	hSearch = FindFirstFile("*.tmp*", &FileData);
	if (hSearch == INVALID_HANDLE_VALUE)
	{
		//No Files That Matched Criteria
		fFinished = TRUE;
	}

	//Delete all files that match the pattern
	while (!fFinished)
	{
		if (!DeleteFile(FileData.cFileName))
		{
			Trace("Setup:  Could not delete temporary file %s\n",FileData.cFileName );
			Fail ("GetLastError returned %d\n", GetLastError());
		}
		if (!FindNextFile(hSearch, &FileData))
		{
			if (GetLastError() == ERROR_NO_MORE_FILES)
			{
				fFinished = TRUE;
			}
			else
			{
				Fail("Unable to Delete Temporary Files, GetLastError is %d \n", GetLastError());
			}
		}
	}

	// Close the search handle, only if HANDLE is Valid
	if (hSearch != INVALID_HANDLE_VALUE)
	{
		if (!FindClose(hSearch))
		{
			Trace("Setup: Could not close search handle \n");
			Fail ("GetLastError returned %d\n", GetLastError());
		}
	}

	g_hEvent = CreateEvent(NULL,TRUE,FALSE, NULL);
	if(g_hEvent == NULL)
	{
		Fail("Create Event Failed\n"
			"GetLastError returned %d\n", GetLastError());
	}

	InitializeCriticalSection ( &g_csUniqueFileName);
}

/*
*	Cleanup for the test case
*/

VOID
cleanup(VOID)
{
	//DeleteCriticalSection(&g_csUniqueFileName);
	PAL_Terminate();
}


VOID
incrementCounter(VOID)
{

	if (INT_MAX == GLOBAL_COUNTER)
		{
			GLOBAL_COUNTER = 0;
		}

	GLOBAL_COUNTER++;
}

/*
 * Worker Thread
 * Read File:  Read from a file and write to a temporary file and then delete the temp file
 */
DWORD
PALAPI
readfile( LPVOID lpParam )
{

	// Declaring Local Variables
	HANDLE hFile,hTempfile;
	char buffer[BUFSIZE];
	DWORD  dwBytesRead, dwBytesWritten, dwBufSize=BUFSIZE;
	 DWORD dwWaitResult=0;
	 char filename[MAX_PATH];

	//Wait for event to signal to start test
	dwWaitResult  = WaitForSingleObject(g_hEvent,INFINITE);
	if (WAIT_OBJECT_0 != dwWaitResult)
		{
		Fail ("readfile: Wait for Single Object (g_hEvent) failed.  Failing test.\n"
	       "GetLastError returned %d\n", GetLastError());
		}


	/*Start Operation*/

	// Open the existing file.
	while(TRUE)
		{

		hFile = CreateFile("samplefile.dat",  // file name
		        GENERIC_READ,                   // open for reading
		        FILE_SHARE_READ,              // Share the file for read
		        NULL,                           	// default security
		        OPEN_EXISTING,                // existing file only
		        FILE_ATTRIBUTE_NORMAL,  // normal file
		        NULL);                          	// no template

			if (hFile == INVALID_HANDLE_VALUE)
		 	{
		        Trace("Could not open file \n");
			 Fail ( "GetLastError returned %d\n", GetLastError());
		    	}

			//Generate Unique File Name to Write
			//Enter CS
			EnterCriticalSection(&g_csUniqueFileName);

				//Increment Number and assign to local variable
				UNIQUE_FILE_NUMBER++;
				_snprintf(filename, MAX_PATH, "%d_%d_tempfile.tmp", USE_PROCESS_COUNT,UNIQUE_FILE_NUMBER);
				//filename  = itoa(UNIQUE_FILE_NUMBER);
			//Leave CS
			LeaveCriticalSection(&g_csUniqueFileName);


			// Create a temporary file with name generate above
			hTempfile = CreateFile(filename,  // file name
				        GENERIC_WRITE, // open for read/write
				        0,                            // do not share
				        NULL,                         // default security
				        CREATE_ALWAYS,                // overwrite existing file
				        FILE_ATTRIBUTE_NORMAL,        // normal file
				        NULL);                        // no template


			 if (hTempfile == INVALID_HANDLE_VALUE)
				    {
				        Trace("Could not create temporary file\n");
					 Fail ( "GetLastError returned %d\n", GetLastError());
				    }

			     // Read 4K blocks to the buffer.
			    // Change all characters in the buffer to upper case.
			    // Write the buffer to the temporary file.

			    do
			    {
			        if (ReadFile(hFile, buffer, 4096,
			            &dwBytesRead, NULL))
			        {

			            WriteFile(hTempfile, buffer, dwBytesRead,
			                &dwBytesWritten, NULL);
			        }
			    } while (dwBytesRead == BUFSIZE);



			     // Close both files.
	 		    if (0==CloseHandle(hFile))
	 		    	{
	 		    		Trace("Could not handle hFile\n");
					Fail ( "GetLastError returned %d\n", GetLastError());
			    	}

			    if (0==CloseHandle(hTempfile))
			    	{
			    		Trace("Could not handle hTempFile\n");
					Fail ( "GetLastError returned %d\n", GetLastError());
			    	}

			    //Delete the file that was created
			    if (!DeleteFile(filename))
			    	{
					Trace("Could not delete temporary file %s\n", filename);
					Fail ( "GetLastError returned %d\n", GetLastError());

			    	}

			//g_readfileoperation++;
		}

/*End Operation*/

    return 0;
}


/* Worker Thread
 * Enter and Leave Nested Critical Sections
 */
DWORD
PALAPI
enterandleave_cs( LPVOID lpParam )
{

	//Declare Local Variables

	CRITICAL_SECTION lcs;
	CRITICAL_SECTION lcsNested;

	 DWORD dwWaitResult;

	//Initialize Critical Section Structures
	InitializeCriticalSection ( &lcs);
	InitializeCriticalSection ( &lcsNested);


	//Wait for event to signal to start test
	dwWaitResult  = WaitForSingleObject(g_hEvent,INFINITE);
	if (WAIT_OBJECT_0 != dwWaitResult)
		{
		Fail ("enterandleave_cs: Wait for Single Object (g_hEvent) failed.  Failing test.\n"
	       "GetLastError returned %d\n", GetLastError());
		}

	//Trace("Critical Section Started\n");

	while(TRUE)
		{
	EnterCriticalSection(&lcs);

		EnterCriticalSection(&lcsNested);

			incrementCounter();

		LeaveCriticalSection(&lcsNested);

	LeaveCriticalSection(&lcs);
	//g_enterleavecsoperation++;
		}

	//Delete Critical Section Structures

	DeleteCriticalSection(&lcs);
	DeleteCriticalSection(&lcsNested);


    return 0;
}


/*
 * Allocate and Free Memory
 */
DWORD
PALAPI
allocateandfree_memory( LPVOID lpParam )
{


	int i;
	char *textArrPtr[64];
	 DWORD dwWaitResult;

	//Wait for event to signal to start test
	dwWaitResult  = WaitForSingleObject(g_hEvent,INFINITE);
	if (WAIT_OBJECT_0 != dwWaitResult)
		{
		Fail ("allocateandfree_memory: Wait for Single Object (g_hEvent) failed.  Failing test.\n"
	       "GetLastError returned %d\n", GetLastError());
		}


	while(TRUE)
	{

		//do allocate and free operation

		for (i=0;i<64;i++)
			{
				textArrPtr[i] = (char*) malloc(BUFSIZE);
				if (textArrPtr[i] == NULL)
					{
						Fail("Insufficient Memory Available, GetLastError is %d \n", GetLastError());
						testStatus = TEST_FAIL;
					}
			}

		for (i=0;i<64;i++)
			{
				free(textArrPtr[i]);
			}
		//g_allocatefreeoperation++;
	}




    return 0;
}

/*
 * Do work in a tight loop
 */
DWORD
PALAPI
doworkintightloop_cs( LPVOID lpParam )
{

	unsigned int i;
	 DWORD dwWaitResult;

	//Wait for event to signal to start test
	dwWaitResult  = WaitForSingleObject(g_hEvent,INFINITE);
	if (WAIT_OBJECT_0 != dwWaitResult)
		{
		Fail ("doworkintightloop_cs: Wait for Single Object (g_hEvent) failed.  Failing test.\n"
	       "GetLastError returned %d\n", GetLastError());
		}

	i= 0;
	while (TRUE)
	{

		if (INT_MAX == i)
			i =0;
		i++;
		//g_doworintightloop++;
	}

    return 0;
}


/*
 * Main Test Case worker thread which will suspend and resume all other worker threads
 */
DWORD
PALAPI
suspendandresumethreads( LPVOID lpParam )
{

	unsigned int loopcount = REPEAT_COUNT;
	int Id=(int)lpParam;
	unsigned int i,j,k;
	DWORD dwStart;
	 DWORD dwWaitResult=0;
	 DWORD dwLastError = 0;
	struct statistics stats;
	  struct statistics* buffer;



	//Initialize the Statistics Structure
	stats.relationId = RELATION_ID;
	stats.processId = USE_PROCESS_COUNT;
	stats.operationsFailed = 0;
	stats.operationsPassed = 0;
	stats.operationsTotal  = 0;
	stats.operationTime    = 0;



	//Wait for event to signal to start test
	WaitForSingleObject(g_hEvent,INFINITE);
	if (WAIT_OBJECT_0 != dwWaitResult)
	{
		Fail ("suspendandresumethreads: Wait for Single Object (g_hEvent) failed.  Failing test.\n"
	    "GetLastError returned %d\n", GetLastError());
	}


	//Capture Start Import
	dwStart = GetTickCount();

	for(i = 0; i < loopcount; i++)
	{

		failFlag = false;

		//Suspend Worker Threads
		for (k=0;k<WORKER_THREAD_MULTIPLIER_COUNT;k++)
		{
			for (j=0;j<4;j++)
			{
				if (-1 == SuspendThread(hThread[j][k]))
				{
					//If the operation indicate failure
					failFlag = true;
				}
			}
		}


		//Resume Worker Threads
		for (k=0;k<WORKER_THREAD_MULTIPLIER_COUNT;k++)
		{
			for (j=0;j<4;j++)
			{

				//Only suspend if not already in suspended state

				if (-1 == ResumeThread(hThread[j][k]))
				{
					//If the operation indicate failure
					failFlag = true;
				}

			}
		}


		//Check for Fail Flag.  If set increment number of failures
		// If Fail flag not set then increment number of operations and number of passe
		if (failFlag == true)
			{
				stats.operationsFailed++;
			}
		else
			{
				stats.operationsPassed +=1;

			}
		stats.operationsTotal  +=1;

	}

	stats.operationTime = GetTickCount() - dwStart;

	/*Trace("\n\n\n\nOperation Time: %d milliseconds\n", stats.operationTime);
	Trace("Operation Passed: %d\n", stats.operationsPassed);
	Trace("Operation Total: %d\n", stats.operationsTotal);
	Trace("Operation Failed: %d\n", stats.operationsFailed);
		*/
	if(resultBuffer->LogResult(Id, (char *)&stats))
    	{
        	Fail("Error while writing to shared memory, Thread Id is[%d] and Process id is [%d]\n", Id, USE_PROCESS_COUNT);
    	}

	 buffer = (struct statistics *)resultBuffer->getResultBuffer(Id);
       //Trace("\n%d,%d,%d,%lu\n", buffer->operationsFailed, buffer->operationsPassed, buffer->operationsTotal, buffer->operationTime );


    return 0;
}



int GetParameters( int argc, char **argv)
{

	if( (argc != 5) || ((argc == 1) && !strcmp(argv[1],"/?"))
       || !strcmp(argv[1],"/h") || !strcmp(argv[1],"/H"))
    {
        Trace("PAL -Composite Thread Suspension Test\n");
        Trace("Usage:\n");
	 Trace("\t[PROCESS_COUNT] Greater than or Equal to  1 \n");
	 Trace("\t[WORKER_THREAD_MULTIPLIER_COUNT]  Greater than or Equal to 1 and Less than or Equal to 64 \n");
        Trace("\t[REPEAT_COUNT] Greater than or Equal to 1\n");
	 Trace("\t[RELATION_ID  [greater than or Equal to 1]\n");
        return -1;
    }

//  Trace("Args 1 is [%s], Arg 2 is [%s], Arg 3 is [%s]\n", argv[1], argv[2], argv[3]);

    USE_PROCESS_COUNT = atoi(argv[1]);
    if( USE_PROCESS_COUNT < 0)
    {
        Trace("\nPROCESS_COUNT to greater than or equal to 1\n");
        return -1;
    }

    WORKER_THREAD_MULTIPLIER_COUNT = atoi(argv[2]);
    if( WORKER_THREAD_MULTIPLIER_COUNT < 1 || WORKER_THREAD_MULTIPLIER_COUNT > 64)
    {
        Trace("\nWORKER_THREAD_MULTIPLIER_COUNT to be greater than or equal to 1 or less than or equal to 64\n");
        return -1;
    }

    REPEAT_COUNT = atoi(argv[3]);
    if( REPEAT_COUNT < 1)
    {
        Trace("\nREPEAT_COUNT to greater than or equal to 1\n");
        return -1;
    }

    RELATION_ID = atoi(argv[4]);
    if( RELATION_ID < 1)
    {
        Trace("\nRELATION_ID to be greater than or equal to 1\n");
        return -1;
    }
    return 0;
}
