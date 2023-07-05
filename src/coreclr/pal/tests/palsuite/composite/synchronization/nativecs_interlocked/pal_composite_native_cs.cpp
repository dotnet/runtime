// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <time.h>
//#include <pthread.h>
//#include "mtx_critsect.cpp"
#include "mtx_critsect.h"
#include "resultbuffer.h"



#define LONGLONG long long
#define ULONGLONG unsigned LONGLONG
/*Defining Global Variables*/

int THREAD_COUNT=0;
int REPEAT_COUNT=0;
int GLOBAL_COUNTER=0;
int USE_PROCESS_COUNT = 0;
int RELATION_ID =0;
int g_counter = 0;
int MAX_PATH = 256;
LONGLONG calibrationValue = 0;

pthread_mutex_t g_mutex = PTHREAD_MUTEX_INITIALIZER;
pthread_cond_t g_cv = PTHREAD_COND_INITIALIZER;
pthread_cond_t g_cv2 = PTHREAD_COND_INITIALIZER;
CRITICAL_SECTION g_cs;

/* Capture statistics for each worker thread */
struct statistics{
    unsigned int processId;
    unsigned int operationsFailed;
    unsigned int operationsPassed;
    unsigned int operationsTotal;
    DWORD        operationTime;
    unsigned int relationId;
};


struct applicationStatistics{
    DWORD        	operationTime;
    unsigned int 	relationId;
    unsigned int 	processCount;
    unsigned int 	threadCount;
    unsigned int 	repeatCount;
    char*        		buildNumber;

};

ResultBuffer *resultBuffer;


void* waitforworkerthreads(void*);
void starttests(int);
int setuptest(void);
int cleanuptest(void);
int GetParameters( int , char **);
void incrementCounter(void);
ULONGLONG GetTicks(void);
ULONGLONG getPerfCalibrationValue(void);



PALTEST(composite_synchronization_nativecs_interlocked_paltest_synchronization_nativecs_interlocked, "composite/synchronization/nativecs_interlocked/paltest_synchronization_nativecs_interlocked")
 {
	//Variable Declaration
	pthread_t pthreads[640];
	int threadID[640];
	int i=0;
	int j=0;
	int rtn=0;
	ULONGLONG startTicks = 0;

	/* Variables to capture the file name and the file pointer*/
    char fileName[MAX_PATH];
    FILE *hFile;
    struct statistics* buffer;
    int statisticsSize = 0;

	/*Variable to Captutre Information at the Application Level*/
	struct applicationStatistics appStats;
	char mainFileName[MAX_PATH];
    FILE *hMainFile;

	//Get perfCalibrationValue

	calibrationValue = getPerfCalibrationValue();
	printf("Calibration Value for this Platform %llu \n", calibrationValue);


	//Get Parameters
	if(GetParameters(argc, argv))
    {
     printf("Error in obtaining the parameters\n");
	 exit(-1);
    }

	//Assign Values to Application Statistics Members
	appStats.relationId=RELATION_ID;
	appStats.operationTime=0;
	appStats.buildNumber = "999.99";
	appStats.processCount = USE_PROCESS_COUNT;
	appStats.threadCount = THREAD_COUNT;
	appStats.repeatCount = REPEAT_COUNT;

	printf("RELATION ID : %d\n", appStats.relationId);
	printf("Process Count : %d\n", appStats.processCount);
	printf("Thread Count : %d\n", appStats.threadCount);
	printf("Repeat Count : %d\n", appStats.repeatCount);


	//Open file for Application Statistics Collection
	snprintf(mainFileName, MAX_PATH, "main_nativecriticalsection_%d_.txt",appStats.relationId);
	hMainFile = fopen(mainFileName, "w+");

	if(hMainFile == NULL)
	{
	    printf("Error in opening main file for write\n");
	}


	for (i=0;i<THREAD_COUNT;i++)
		{
			threadID[i] = i;
		}

	statisticsSize = sizeof(struct statistics);

	snprintf(fileName, MAX_PATH, "%d_thread_nativecriticalsection_%d_.txt", USE_PROCESS_COUNT, RELATION_ID);
	hFile = fopen(fileName, "w+");

	if(hFile == NULL)
	{
	    printf("Error in opening file for write for process [%d]\n", USE_PROCESS_COUNT);
	}


	// For each thread we will log operations failed (int), passed (int), total (int)
	// and number of ticks (DWORD) for the operations
	resultBuffer = new ResultBuffer( THREAD_COUNT, statisticsSize);

	//Call Test Case Setup Routine
	if (0!=setuptest())
	{
		//Error Condition
		printf("Error Initializing Test Case\n");
		exit(-1);
	}

	//Acquire Lock
    if (0!=pthread_mutex_lock(&g_mutex))
    {
       	//Error Condition
		printf("Error Acquiring Lock\n");
		exit(-1);
    }

	//USE NATIVE METHOD TO GET TICK COUNT
	startTicks = GetTicks();

	/*Loop to create number THREAD_COUNT number of threads*/
	for (i=0;i< THREAD_COUNT;i++)
	{

		//printf("Creating Thread Count %d\n", i);
		//printf("Thread array value = %d\n", threadID[i]);
		rtn=pthread_create(&pthreads[i], NULL, waitforworkerthreads, &threadID[i]);
		if (0 != rtn)
			{ /* ERROR Condition */
				printf("Error:  pthread Creat, %s \n", strerror(rtn));
				exit(-1);
			}

	}


	//printf("Main Thread waits to receive signal when all threads are done\n");
	pthread_cond_wait(&g_cv2,&g_mutex);

	//printf("Main thread has received signal\n");

	/*Signal Threads to Start Working*/
	//printf("Raise signal for all threads to start working\n");



	if (0!=pthread_cond_broadcast(&g_cv))
		{
			//Error Condition
			printf("Error Broadcasting Conditional Event\n");
			exit(-1);
		}

	//Release the lock
	if (0!=pthread_mutex_unlock(&g_mutex))
		{
			//Error Condition
			printf("Error Releasing Lock\n");
			exit(-1);
		}

	/*Join Threads */
	while (j < THREAD_COUNT)
	{
		if (0 != pthread_join(pthreads[j],NULL))
			{
				//Error Condition
				printf("Error Joining Threads\n");
				exit(-1);
			}
		j++;
	}


   /*Write Application Results to File*/
   //CAPTURE NATIVE TICK COUNT HERE
   appStats.operationTime = (DWORD)(GetTicks() - startTicks)/calibrationValue;


	/* Write Results to a file*/
    if(hFile!= NULL)
    {
        for( i = 0; i < THREAD_COUNT; i++ )
        {
            buffer = (struct statistics *)resultBuffer->getResultBuffer(i);
            fprintf(hFile, "%d,%d,%d,%d,%lu,%d\n", buffer->processId, buffer->operationsFailed, buffer->operationsPassed, buffer->operationsTotal, buffer->operationTime, buffer->relationId );
            //printf("Iteration %d over\n", i);
        }
    }
    fclose(hFile);



	//Call Test Case Cleanup Routine
	if (0!=cleanuptest())
		{
			//Error Condition
			printf("Error Cleaning up Test Case");
			exit(-1);
		}


   if(hMainFile!= NULL)
    {
        printf("Writing to Main File \n");
		fprintf(hMainFile, "%lu,%d,%d,%d,%d,%s\n", appStats.operationTime, appStats.relationId, appStats.processCount, appStats.threadCount, appStats.repeatCount, appStats.buildNumber);

    }
    fclose(hMainFile);
	return 0;
 }

void * waitforworkerthreads(void * threadId)
{

	int *threadParam = (int*) threadId;

//	printf("Thread ID : %d \n", *threadParam);

	//Acquire Lock
       if (0!=pthread_mutex_lock(&g_mutex))
       	{
       		//Error Condition
			printf("Error Acquiring Mutex Lock in Wait for Worker Thread\n");
			exit(-1);
       	}

	//Increment Global Counter
	GLOBAL_COUNTER++;


	//If global counter is equal to thread count then signal main thread
	if (GLOBAL_COUNTER == THREAD_COUNT)
		{
			if (0!=pthread_cond_signal(&g_cv2))
				{
					//Error Condition
					printf("Error in setting conditional variable\n");
					exit(-1);
				}
		}

	//Wait for main thread to signal
	if (0!=pthread_cond_wait(&g_cv,&g_mutex))
		{
			//Error Condition
			printf("Error waiting on conditional variable in Worker Thread\n");
			exit(-1);
		}

	//Release the mutex lock
	if (0!=pthread_mutex_unlock(&g_mutex))
		{
			//Error Condition
			printf("Error Releasing Mutex Lock in Worker Thread\n");
			exit(-1);
		}

	//Start the test
	starttests(*threadParam);

}

void starttests(int threadID)
{
	/*All threads beign executing tests cases*/
	int i = 0;
	int Id = threadID;
	struct statistics stats;
	ULONGLONG startTime = 0;
	ULONGLONG endTime = 0;

	LONG volatile Destination;
	LONG Exchange;
	LONG Comperand;
	LONG result;

	stats.relationId = RELATION_ID;
	stats.processId = USE_PROCESS_COUNT;
	stats.operationsFailed = 0;
	stats.operationsPassed = 0;
	stats.operationsTotal  = 0;
	stats.operationTime    = 0;

	//Enter and Leave Critical Section in a loop REPEAT_COUNT Times


	startTime = GetTicks();

	for (i=0;i<REPEAT_COUNT;i++)
	{
		  Destination = (LONG volatile) threadID;
		  Exchange    = (LONG) i;
		  Comperand   = (LONG) threadID;
		  result = InterlockedCompareExchange(&Destination, Exchange, Comperand);

		  if( i != result )
		  {
		       stats.operationsFailed++;
		       stats.operationsTotal++;
		       continue;
		  }

	}


	stats.operationTime = (DWORD)(GetTicks() - startTime)/calibrationValue;

//	printf("Operation Time %d \n", stats.operationTime);

	if(resultBuffer->LogResult(Id, (char *)&stats))
	{
		printf("Error while writing to shared memory, Thread Id is[??] and Process id is [%d]\n", USE_PROCESS_COUNT);
	}

}

int setuptest(void)
{

	//Initialize Critical Section
  /*
	if (0!=MTXInitializeCriticalSection( &g_cs))
		{
			return -1;
		}
  */
	return 0;
}

int cleanuptest(void)
{

	//Delete Critical Section
  /*
	if (0!=MTXDeleteCriticalSection(&g_cs))
		{
			return -1;
		}
  */
	return 0;
}

int GetParameters( int argc, char **argv)
{

	if( (argc != 5) || ((argc == 1) && !strcmp(argv[1],"/?"))
       || !strcmp(argv[1],"/h") || !strcmp(argv[1],"/H"))
    {
        printf("PAL -Composite Native Critical Section Test\n");
        printf("Usage:\n");
	 printf("\t[PROCESS_ID ( greater than 1] \n");
	 printf("\t[THREAD_COUNT ( greater than 1] \n");
        printf("\t[REPEAT_COUNT ( greater than 1]\n");
	 printf("\t[RELATION_ID  [greater than or Equal to 1]\n");
        return -1;
    }


    USE_PROCESS_COUNT = atoi(argv[1]);
    if( USE_PROCESS_COUNT < 0)
    {
        printf("\nInvalid THREAD_COUNT number, Pass greater than 1\n");
        return -1;
    }

    THREAD_COUNT = atoi(argv[2]);
    if( THREAD_COUNT < 1)
    {
        printf("\nInvalid THREAD_COUNT number, Pass greater than 1\n");
        return -1;
    }

    REPEAT_COUNT = atoi(argv[3]);
    if( REPEAT_COUNT < 1)
    {
        printf("\nInvalid REPEAT_COUNT number, Pass greater than 1\n");
        return -1;
    }

    RELATION_ID = atoi(argv[4]);
    if( RELATION_ID < 1)
    {
        printf("\nInvalid RELATION_ID number, Pass greater than 1\n");
        return -1;
    }


    return 0;
}

void incrementCounter(void)
{
	g_counter ++;
}


//Implementation borrowed from pertrace.c
ULONGLONG GetTicks(void)
{
#ifdef i386
    unsigned long a, d;
    asm volatile("rdtsc":"=a" (a), "=d" (d));
    return ((ULONGLONG)((unsigned int)(d)) << 32) | (unsigned int)(a);
#else
    // #error Don''t know how to get ticks on this platform
    return (ULONGLONG)gethrtime();
#endif // i386
}


/**/
ULONGLONG getPerfCalibrationValue(void)
{
	ULONGLONG startTicks;
	ULONGLONG endTicks;

	startTicks = GetTicks();
	sleep(1);
	endTicks = GetTicks();

	return ((endTicks-startTicks)/1000);  //Return number of Ticks in One Milliseconds

}

