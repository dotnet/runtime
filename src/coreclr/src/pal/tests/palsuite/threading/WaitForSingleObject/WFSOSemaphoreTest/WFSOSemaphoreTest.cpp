// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: WFSOMutexTest.c
**
** Purpose: Test for WaitForSingleObjectTest.
**	Create Semaphore Object
**	Create Two Threads, Each Threads does WFSO for the Semaphore Object
**			Increments Counter
**			Releases Semaphore
**	Test Passes if the above operations are successful
**
**
**
**=========================================================*/



#include <palsuite.h>


#define NUMBER_OF_WORKER_THREADS 2


//Declaring Variables
HANDLE hSemaphore_WFSOSemaphore = NULL;
unsigned int globalcounter_WFSOSemaphore =0;

//Declaring Function Prototypes
DWORD PALAPI WFSOSemaphoreTest(LPVOID params);
void incrementCounter_WFSOSemaphore(void);

PALTEST(threading_WaitForSingleObject_WFSOSemaphoreTest_paltest_waitforsingleobject_wfsosemaphoretest, "threading/WaitForSingleObject/WFSOSemaphoreTest/paltest_waitforsingleobject_wfsosemaphoretest")
{

	//Declare local variables
		int testReturnCode = PASS;
		int i =0;
		int cMax = 2;

    int returnCode = 0;

	// 2 dimensional array to hold thread handles for each worker thread
		HANDLE hThread[NUMBER_OF_WORKER_THREADS];
		DWORD dwThreadId=0;

	//Initialize PAL
		if(0 != (PAL_Initialize(argc, argv)))
		    {
		        return ( FAIL );
		    }

   //Create Semaphore
		hSemaphore_WFSOSemaphore = CreateSemaphoreExW( 
			NULL,   // no security attributes
			cMax,   // initial count
			cMax,   // maximum count
			NULL,   // unnamed semaphore
			0,
			0);

		if (hSemaphore_WFSOSemaphore == NULL)
		{
		    // Check for error.
		    Fail("Create Semaphore Failed, GetLastError: %d\n", GetLastError());
		}



  //Spawn 2 worker threads
  for (i=0;i<NUMBER_OF_WORKER_THREADS;i++)
  	{
  		//Create Thread

		hThread[i] = CreateThread(
		NULL,
		0,
		WFSOSemaphoreTest,
		NULL,
		0,
		&dwThreadId);

	    if ( NULL == hThread[i] )
	    {
		Fail ( "CreateThread() returned NULL.  Failing test.\n"
		       "GetLastError returned %d\n", GetLastError());
	    }

  	}


    /* Test running */
    returnCode = WaitForMultipleObjects( NUMBER_OF_WORKER_THREADS, hThread, TRUE, 5000);
    if( WAIT_OBJECT_0 != returnCode )
    {
        Trace("Wait for Object(s) returned %d, and GetLastError value is %d\n", returnCode, GetLastError());
        testReturnCode = FAIL;
    }

//Close thread handles
for (i=0;i<NUMBER_OF_WORKER_THREADS;i++)
  	{

	if (0==CloseHandle(hThread[i]))
		 {
		    	Trace("Could not Close thread handle\n");
			Fail ( "GetLastError returned %d\n", GetLastError());
	    	}
	}

//Close Semaphore Handle 
if (0==CloseHandle(hSemaphore_WFSOSemaphore))
	    	{
	    		Trace("Could not close semaphore handle\n");
		Fail ( "GetLastError returned %d\n", GetLastError());
    	}

PAL_TerminateEx(testReturnCode);
return ( testReturnCode );

}


void incrementCounter_WFSOSemaphore(void)
{
	if (INT_MAX == globalcounter_WFSOSemaphore)
		{
			globalcounter_WFSOSemaphore = 0;
		}

	globalcounter_WFSOSemaphore++;	
	Trace("Global Counter Value: %d \n", globalcounter_WFSOSemaphore);
}


DWORD PALAPI WFSOSemaphoreTest(LPVOID params)
{

     DWORD dwWaitResult;

    // Request ownership of Semaphore

    dwWaitResult = WaitForSingleObject( 
        hSemaphore_WFSOSemaphore,   // handle to semaphore
        0L);          // zero-second time-out interval


    switch (dwWaitResult)
    {
        // The semaphore object was signaled.
        case WAIT_OBJECT_0:
            	  		{

				incrementCounter_WFSOSemaphore();
				// Increment the count of the semaphore.

				if (!ReleaseSemaphore(
				        hSemaphore_WFSOSemaphore,  // handle to semaphore
				        1,           // increase count by one
				        NULL) )      // not interested in previous count
				{
				    Fail ( "ReleaseSemaphore() returned NULL.  Failing test.\n"
		       			"GetLastError returned %d\n", GetLastError());
				}

				break;
    			        }

        // Semaphore was nonsignaled, so a time-out occurred.
        case WAIT_TIMEOUT:
				{
					Fail ( "Semaphore was nonsignaled, so a time-out occurred.  Failing test.\n"
		       			"GetLastError returned %d\n", GetLastError());
					return FALSE;
        			}
    }

    return 1;
}



