// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: WFSOMutexTest.c 
**
** Purpose: Test for WaitForSingleObjectTest. 
**	Create Mutex Object
**	Create Two Threads, Each Threads does WFSO for the Mutex Object
**			Increments Counter
**			Releases Mutex
**	Test Passes if the above operations are successful
**	
**
**
**=========================================================*/



#include <palsuite.h>


#define NUMBER_OF_WORKER_THREADS 2

//Declaring Variables
HANDLE hMutex = NULL;
unsigned int globalcounter =0;
int testReturnCode = PASS;

//Declaring Function Prototypes
DWORD PALAPI WFSOMutexTest(LPVOID params);
void incrementCounter(void);



int __cdecl main(int argc, char **argv)
{

	//Declare local variables
		int i =0;

	// 2 dimensional array to hold thread handles for each worker thread
		HANDLE hThread[NUMBER_OF_WORKER_THREADS];
		DWORD dwThreadId=0; 
    int returnCode = 0;

	//Initialize PAL 
		if(0 != (PAL_Initialize(argc, argv)))
		    {
		        return ( FAIL );
		    }

   //Create Mutex
		hMutex = CreateMutex(NULL,      // no security attributes
                             FALSE,     // initially not owned
                             NULL);     // name of mutex

   //Check for Mutex Creation

		if (hMutex == NULL) 
		{
		 	Fail("Create Mutex Failed, GetLastError: %d\n", GetLastError());
		}


  //Spawn 2 worker threads
  for (i=0;i<NUMBER_OF_WORKER_THREADS;i++)
  	{
  		//Create Thread

		hThread[i] = CreateThread(
		NULL,         
		0,            
		WFSOMutexTest,     
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

//Close Mutex Handle 
if (0==CloseHandle(hMutex))
	    	{
	    		Trace("Could not close mutex handle\n"); 
		Fail ( "GetLastError returned %d\n", GetLastError());  
    	}


PAL_TerminateEx(testReturnCode);
return ( testReturnCode );

}


void incrementCounter(void)
{
	if (INT_MAX == globalcounter)
		{
			globalcounter = 0;
		}
	
	globalcounter++;	
	Trace("Global Counter Value: %d \n", globalcounter);
}


DWORD PALAPI WFSOMutexTest(LPVOID params)
{

     DWORD dwWaitResult; 

    // Request ownership of mutex.
 
    dwWaitResult = WaitForSingleObject( 
        hMutex,   // handle to mutex
        5000L);   // five-second time-out interval

    switch (dwWaitResult) 
    {
        // The thread got mutex ownership.
        case WAIT_OBJECT_0: 
            	  		{

				incrementCounter();

				//Release ownership of the mutex object.
				if (! ReleaseMutex(hMutex)) 
				{ 
				   Fail ( "ReleaseMutex() returned NULL.  Failing test.\n"
		       			"GetLastError returned %d\n", GetLastError());    
				} 

				break; 
    			        } 

        // Cannot get mutex ownership due to time-out.
        case WAIT_TIMEOUT: 
				{
					Fail ( "Cannot get mutex ownership due to time-out.  Failing test.\n"
		       			"GetLastError returned %d\n", GetLastError());  
					return FALSE;
        			}

        // Got ownership of the abandoned mutex object.
        case WAIT_ABANDONED: 
				{
					Fail ( "Got ownership of the abandoned mutex object.  Failing test.\n"
		       			"GetLastError returned %d\n", GetLastError());  
					return FALSE; 
        			}
    }

    return 1;
}



