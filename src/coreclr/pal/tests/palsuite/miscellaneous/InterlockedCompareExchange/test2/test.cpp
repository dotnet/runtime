// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
PALTEST(miscellaneous_InterlockedCompareExchange_test2_paltest_interlockedcompareexchange_test2, "miscellaneous/InterlockedCompareExchange/test2/paltest_interlockedcompareexchange_test2")
{

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
			The following set of operations is guaranteed to be atomic by virtue of the fact
			that InterLockedCompareExchange was able to guarantee that the compare
			and exchange operation on pLock was thread safe.  If the same set of code was
			executed without using InterlockedCompareExchange the code would fail most of
			time.

			*/
			g_Total++;
			Sleep(100);
			g_Total--;
			if (0!=g_Total)
				{
					Fail("Test Failed because g_Total was not protected \n");
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

