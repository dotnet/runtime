// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
Source Code: mainWrapper.c

mainWrapper.c creates Composite Test Case Processes and waits for all processes to get over

Algorithm
o	Create PROCESS_COUNT processes.

Author: RameshG
*/

#include <palsuite.h>
#include "resulttime.h"

/* Test Input Variables */
unsigned int USE_PROCESS_COUNT = 0; //default
unsigned int WORKER_THREAD_MULTIPLIER_COUNT = 0; //default
unsigned int REPEAT_COUNT = 0; //default
unsigned int SLEEP_LENGTH = 0; //default
unsigned int RELATION_ID  = 0;//default
unsigned int THREAD_COUNT = 1; //There is only one suspender and resume thread for this test case

char *testCaseName;


struct applicationStatistics{
    DWORD        	operationTime;
    unsigned int 	relationId;
    unsigned int 	processCount;
    unsigned int 	threadCount;
    unsigned int 	repeatCount;
    char*        		buildNumber;

};


int GetParameters( int argc, char **argv)
{

	if( (argc != 5) || ((argc == 1) && !strcmp(argv[1],"/?"))
       || !strcmp(argv[1],"/h") || !strcmp(argv[1],"/H"))
    {
        printf("PAL -Composite Thread Suspension Test\n");
        printf("Usage:\n");
	 printf("\t[PROCESS_COUNT] Greater than or Equal to  1 \n");
	 printf("\t[WORKER_THREAD_MULTIPLIER_COUNT]  Greater than or Equal to 1 and Less than or Equal to %d \n", MAXIMUM_WAIT_OBJECTS);
        printf("\t[REPEAT_COUNT] Greater than or Equal to 1\n");
	 printf("\t[RELATION_ID  [greater than or Equal to 1]\n");
        return -1;
    }

 // Trace("Args 1 is [%s], Arg 2 is [%s], Arg 3 is [%s]\n", argv[1], argv[2], argv[3]);

    USE_PROCESS_COUNT = atoi(argv[1]);
    if( USE_PROCESS_COUNT < 0)
    {
        printf("\nPROCESS_COUNT to greater than or equal to 1\n");
        return -1;
    }

    WORKER_THREAD_MULTIPLIER_COUNT = atoi(argv[2]);
    if( WORKER_THREAD_MULTIPLIER_COUNT < 1 || WORKER_THREAD_MULTIPLIER_COUNT > 64)
    {
        printf("\nWORKER_THREAD_MULTIPLIER_COUNT to be greater than or equal to 1 or less than or equal to 64\n");
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
        printf("\nRELATION_ID to be greater than or equal to 1\n");
        return -1;
    }



    return 0;
}

PALTEST(composite_threading_threadsuspension_switchthread_paltest_threading_threadsuspension_switchthread, "composite/threading/threadsuspension_switchthread/paltest_threading_threadsuspension_switchthread")
{
    unsigned int i = 0;
    HANDLE hProcess[MAXIMUM_WAIT_OBJECTS];
     DWORD processReturnCode = 0;
    int testReturnCode = PASS;
    STARTUPINFO si[MAXIMUM_WAIT_OBJECTS];
    PROCESS_INFORMATION pi[MAXIMUM_WAIT_OBJECTS];

    FILE *hFile;
    char fileName[MAX_PATH];
    struct applicationStatistics appStats;

    DWORD dwStart=0;

    char lpCommandLine[MAX_PATH] = "";

    char build[] ="0000.00";
    int returnCode = 0;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

   //Initialize Application Statistics Structure
   appStats.relationId=RELATION_ID;
   appStats.operationTime=0;
   appStats.buildNumber =  getBuildNumber();
   //appStats.buildNumber = build;
      appStats.processCount = 0;
   appStats.threadCount = 0;
   appStats.repeatCount = 0;





//Start Process Time Capture
dwStart = GetTickCount();

    if(GetParameters(argc, argv))
    {
        Fail("Error in obtaining the parameters\n");
    }

//Assign Correct Values to the Application Stats Structure
   appStats.relationId=RELATION_ID;
   appStats.processCount = USE_PROCESS_COUNT;
   appStats.threadCount = THREAD_COUNT ;
   appStats.repeatCount = REPEAT_COUNT;

   Trace("Relation ID: %d \n", RELATION_ID);
      Trace("USE_PROCESS_COUNT: %d \n", USE_PROCESS_COUNT);
   Trace("WORKER_THREAD_MULTIPLIER_COUNT: %d \n", WORKER_THREAD_MULTIPLIER_COUNT);
      Trace("REPEAT_COUNT: %d \n", REPEAT_COUNT);


_snprintf(fileName, MAX_PATH, "main_threadsuspension_%d_.txt",appStats.relationId);

 hFile = fopen(fileName, "w+");
if(hFile == NULL)
    {
        Fail("Error in opening file to write application results for Thread Suspension Test with error code %d \n", GetLastError() );
    }



    for( i = 0; i < USE_PROCESS_COUNT; i++ )
    {

        ZeroMemory( lpCommandLine, MAX_PATH );
        if ( _snprintf( lpCommandLine, MAX_PATH-1, "threadsuspension %d %d %d %d", i, WORKER_THREAD_MULTIPLIER_COUNT, REPEAT_COUNT, RELATION_ID) < 0 )
        {
            Trace ("Error: Insufficient commandline string length for iteration [%d]\n",   i);
        }

        /* Zero the data structure space */
        ZeroMemory ( &pi[i], sizeof(pi[i]) );
        ZeroMemory ( &si[i], sizeof(si[i]) );

        /* Set the process flags and standard io handles */
        si[i].cb = sizeof(si[i]);

	//Printing the Command Line
	//Trace("Command Line \t %s \n", lpCommandLine);

        //Create Process
        if(!CreateProcess( NULL, /* lpApplicationName*/
                          lpCommandLine, /* lpCommandLine */
                          NULL, /* lpProcessAttributes  */
                          NULL, /* lpThreadAttributes */
                          TRUE, /* bInheritHandles */
                          0, /* dwCreationFlags, */
                          NULL, /* lpEnvironment  */
                          NULL, /* pCurrentDirectory  */
                          &si[i], /* lpStartupInfo  */
                          &pi[i] /* lpProcessInformation  */
                          ))
        {
            Fail("Process Not created for [%d] and GetLastError value is %d\n", i, GetLastError());

        }
        else
        {
            hProcess[i] = pi[i].hProcess;
            //Trace("Process created for [%d]\n", i);
        }

    }

    returnCode = WaitForMultipleObjects( USE_PROCESS_COUNT, hProcess, TRUE, INFINITE);
     if( WAIT_OBJECT_0 != returnCode )
    {
        Trace("Wait for Object(s) @ Main thread for %d processes returned %d, and GetLastError value is %d\n", USE_PROCESS_COUNT, returnCode, GetLastError());
        testReturnCode = FAIL;
    }

	for( i = 0; i < USE_PROCESS_COUNT; i++ )
    {
        /* check the exit code from the process */
        if( ! GetExitCodeProcess( pi[i].hProcess, &processReturnCode ) )
        {
            Trace( "GetExitCodeProcess call failed for iteration %d with error code %u\n",
                i, GetLastError() );

            testReturnCode = FAIL;
        }

        if(processReturnCode == FAIL)
        {
            Trace( "Process [%d] failed and returned FAIL\n", i);
            testReturnCode = FAIL;
        }

        if(!CloseHandle(pi[i].hThread))
        {
            Trace("Error:%d: CloseHandle failed for Process [%d] hThread\n", GetLastError(), i);
            testReturnCode = FAIL;
        }

        if(!CloseHandle(pi[i].hProcess) )
        {
            Trace("Error:%d: CloseHandle failed for Process [%d] hProcess\n", GetLastError(), i);
            testReturnCode = FAIL;
        }
    }

//Get the end time of the process
appStats.operationTime = GetTickCount() - dwStart;

if( testReturnCode == PASS)
    {
        Trace("Test Passed\n");
    }
    else
    {
        Trace("Test Failed\n");
    }

//Write Process Result Contents to File
if(hFile!= NULL)
    {
            fprintf(hFile, "%lu,%d,%d,%d,%d,%s\n", appStats.operationTime, appStats.relationId, appStats.processCount, appStats.threadCount, appStats.repeatCount, appStats.buildNumber);
    }

if (0!=fclose(hFile))
{
	Trace("Error:%d: fclose failed for file %s\n", GetLastError(), fileName);
}
    PAL_Terminate();


if( testReturnCode == PASS)
    {
        return PASS;
    }
    else
    {
        return FAIL;

    }

}
