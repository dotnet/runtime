// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: WFSOThreadTest.c 
**
** Purpose: Test for WaitForSingleObjectTest. 
**			Create One Thread and do some work
**			Use WFSO For the Thread to finish 
**			
** Test Passes if the above operations are successful
**	
**
**
**=========================================================*/



#include <palsuite.h>


//Declaring Variables
HANDLE hThread_WFSOThreadTest = NULL;
HANDLE hEvent_WFSOThreadTest = NULL;

unsigned int globalcounter_WFSOThreadTest =0;

//Declaring Function Prototypes
DWORD PALAPI incrementCounter(LPVOID params);

PALTEST(threading_WaitForSingleObject_WFSOThreadTest_paltest_waitforsingleobject_wfsothreadtest, "threading/WaitForSingleObject/WFSOThreadTest/paltest_waitforsingleobject_wfsothreadtest")
{

	//Declare local variables
	DWORD dwThreadId=0; 
	DWORD dwWaitResult=0; 

	//Initialize PAL 
	if(0 != (PAL_Initialize(argc, argv)))
	    {
	        return ( FAIL );
	    }


	//Create Event
	hEvent_WFSOThreadTest = CreateEvent(NULL,TRUE,FALSE, NULL);
	if(hEvent_WFSOThreadTest == NULL)
	{
		Fail("Create Event Failed\n"
			"GetLastError returned %d\n", GetLastError());
	}

	
	//Create Thread
	hThread_WFSOThreadTest = CreateThread(
		NULL,         
		0,            
		incrementCounter,     
		NULL,     
		0,           
		&dwThreadId);

	    if ( NULL == hThread_WFSOThreadTest ) 
	    {
		Fail ( "CreateThread() returned NULL.  Failing test.\n"
		       "GetLastError returned %d\n", GetLastError());   
	    }


	//Wait For Thread to signal start  
	dwWaitResult  = WaitForSingleObject(hEvent_WFSOThreadTest,INFINITE);
	
	switch (dwWaitResult) 
    	{
        // The thread wait was successful
        case WAIT_OBJECT_0: 
            	  		{

				Trace ("Wait for Single Object (hEvent) was successful.\n");
			       break; 
    			        } 

	// Time-out.
        case WAIT_TIMEOUT: 
				{
					Fail ( "Time -out.  Failing test.\n"
		       			"GetLastError returned %d\n", GetLastError());  
					return FALSE;
        			}

        // Got ownership of the abandoned event object.
        case WAIT_ABANDONED: 
				{
					Fail ( "Got ownership of the abandoned event object.  Failing test.\n"
		       			"GetLastError returned %d\n", GetLastError());  
					return FALSE; 
        			}

    	}

		
	//Wait for Thread to finish 
	dwWaitResult = WaitForSingleObject( 
	        hThread_WFSOThreadTest,   //handle to thread
	        5000L);     //Wait Indefinitely

       
	switch (dwWaitResult) 
    	{
        // The thread wait was successful
        case WAIT_OBJECT_0: 
            	  		{

				Trace("Wait for thread was successful\n");
			
				break; 
    			        } 

        // Time-out.
        case WAIT_TIMEOUT: 
				{
					Fail ( "Time -out.  Failing test.\n"
		       			"GetLastError returned %d\n", GetLastError());  
					return FALSE;
        			}

        // Got ownership of the abandoned thread object.
        case WAIT_ABANDONED: 
				{
					Fail ( "Got ownership of the abandoned thread object.  Failing test.\n"
		       			"GetLastError returned %d\n", GetLastError());  
					return FALSE; 
        			}

    }


//Close Handles
if (0==CloseHandle(hEvent_WFSOThreadTest))
		 {
		    	Trace("Could not Close event handle\n"); 
			Fail ( "GetLastError returned %d\n", GetLastError());  
	    	}
if (0==CloseHandle(hThread_WFSOThreadTest))
		 {
		    	Trace("Could not Close thread handle\n"); 
			Fail ( "GetLastError returned %d\n", GetLastError());  
	    	}

PAL_Terminate();
return ( PASS );

}

DWORD PALAPI incrementCounter(LPVOID params)
{

	//Signal Event so that main thread can start to wait for thread object
	if (0==SetEvent(hEvent_WFSOThreadTest))
	{
		Fail ( "SetEvent returned Zero.  Failing test.\n"
		       "GetLastError returned %d\n", GetLastError());  
	}

	for (globalcounter_WFSOThreadTest=0;globalcounter_WFSOThreadTest<100000;globalcounter_WFSOThreadTest++);

	//Sleep(5000);
	
	Trace("Global Counter Value: %d \n", globalcounter_WFSOThreadTest);
	return 0;
}






