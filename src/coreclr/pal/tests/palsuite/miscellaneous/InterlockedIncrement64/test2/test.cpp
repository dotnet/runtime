// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source : test.c
**
** Purpose: InterlockedIncrement64() function
**
**	The test case spawns MAX_THREADS Threads, and each thread call InterlockedDecrement Function to decrement a 
**	global counter REPEAT_COUNT Times. The Test case sets the global counter to Zero at the beginning of the test.  
**	The test cases passes if at the end the test the value of the global counter is  MAX_THREADS * REPEAT_COUNT.
**
**
**=========================================================*/

#include <palsuite.h>
#define MAX_THREADS 64
#define REPEAT_COUNT 10000

LONG GlobalCounter_InterlockedIncrement64_test2 = 0;
void IncrementCounter_InterlockedIncrement64_test2(void);

PALTEST(miscellaneous_InterlockedIncrement64_test2_paltest_interlockedincrement64_test2, "miscellaneous/InterlockedIncrement64/test2/paltest_interlockedincrement64_test2")
{

	LONG TotalOperations=0;
	int i=0;
	DWORD dwThreadID = 0;
	HANDLE hThread[MAX_THREADS];
	TotalOperations = MAX_THREADS * REPEAT_COUNT;
	GlobalCounter_InterlockedIncrement64_test2 = 0;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
	
	/*
	**  Run only on 64 bit platforms
	*/
	#if defined(HOST_64BIT)

		//Create MAX_THREADS threads that will operate on the global counter
		for (i=0;i<MAX_THREADS;i++)
		{
			hThread[i] = CreateThread( 
				NULL,                        // default security attributes 
				0,                           // use default stack size  
				(LPTHREAD_START_ROUTINE) IncrementCounter_InterlockedIncrement64_test2,                  // thread function 
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
		
		/* Compare the value of  global counter with zero.  
		*/
		if (TotalOperations!=GlobalCounter_InterlockedIncrement64_test2)
			{
				Fail("Test Case Failed: InterlockedDecrement \n");
			}
	#endif  //defined(HOST_64BIT)
	
    PAL_Terminate();
    return PASS; 
} 

void IncrementCounter_InterlockedIncrement64_test2(void)
{
	int i=0;

	for (i=0; i<REPEAT_COUNT;i++)
	{
		InterlockedIncrement(&GlobalCounter_InterlockedIncrement64_test2);
	}
}



