//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source : test.c
**
** Purpose: Test for InterlockedCompareExchange() function using multiple threads
**
**
**=========================================================*/



#include <palsuite.h>

#define MAX_THREADS 10
#define REPEAT_COUNT 10

//Global Variable Declaration
LONG g_Total = 0;
LONG Lock=0;


void ModifyGlobalResource(void);
void AcquireLock(PLONG pLock);
void ReleaseLock(PLONG pLock);



//Main entry point of the program
int __cdecl main(int argc, char *argv[]) {
  
    int i = 0;
    DWORD dwThreadID=0;
    LONG totalOperations = 0;

    HANDLE hThread[MAX_THREADS];

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }


	totalOperations = MAX_THREADS * REPEAT_COUNT;


	//Create MAX_THREADS threads that will operate on the global counter
	for (i=0;i<MAX_THREADS;i++)
	{
			hThread[i] = CreateThread( 
				NULL,                        // default security attributes 
				0,                           // use default stack size  
				(LPTHREAD_START_ROUTINE) ModifyGlobalResource,    // thread function 
				NULL,                // argument to thread function 
				0,                           // use default creation flags 
				&dwThreadID);                // returns the thread identifier 
 
		   // Check the return value for success. 
 
			if (hThread[i] == NULL) 
			{
				Fail("ERROR: Was not able to create thread\n"
           				 "GetLastError returned %d\n", GetLastError());
			}

	}


	//Wait for all threads to finish
	for (i=0;i<MAX_THREADS;i++)
	{

		 if (WAIT_OBJECT_0 != WaitForSingleObject (hThread[i], INFINITE))
 		{
	 		Fail ("Main: Wait for Single Object failed.  Failing test.\n"
		       "GetLastError returned %d\n", GetLastError());  
 		}
	
	}
	

	if (0!= g_Total)
		{
			Fail("Test Failed \n");
		}

	Trace("Global Counter Value at the end of the test %d \n", g_Total);

    /*
     * Terminate PAL 
     */
	
    PAL_Terminate();
    return PASS; 
} 


void ModifyGlobalResource(void)
{

	int i =0;
	
	for (i=0;i<REPEAT_COUNT;i++)
		{

			/*
				Acquire Lock Provides Synchronization Around g_Total global variable
			*/

			AcquireLock(&Lock);
			
			/*
			The following set of operations is gauranteed to be atomic by virtue of the fact 
			that InterLockedCompareExchange was able to gaurantee that the compare 
			and exchange operation on pLock was thread safe.  If the same set of code was 
			executed without using InterlockedCompareExchange the code would fail most of 
			time.
			
			*/
			g_Total++;
			Sleep(100);
			g_Total--;
			if (0!=g_Total)
				{
					Fail("Test Failed beacuse g_Total was not protected \n");
				}

			
			/*
				Acquire Lock releases the lock around g_Total Global variable 
			*/

			ReleaseLock(&Lock);
		}
	
	
}


void AcquireLock(PLONG pLock)
{
	//Spin Lock implemented with the help of InterlockedCompareExchange

	
	while(1)
		{
		if (InterlockedCompareExchange(pLock,1,0)==0)
			break;
		}
		
}


void ReleaseLock(PLONG pLock)
{
	

	MemoryBarrier();
	*pLock = 0;
}

