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
unsigned int THREAD_COUNT = 0; //default
unsigned int REPEAT_COUNT = 0; //default
unsigned int SLEEP_LENGTH = 0; //default
unsigned int RELATION_ID  = 1001;


//Structure to capture application wide statistics
struct applicationStatistics{
    DWORD        operationTime;
    unsigned int relationId;
    unsigned int processCount;
    unsigned int threadCount;
    unsigned int repeatCount;
    char*        buildNumber;

};


//Get parameters from the commandline
int GetParameters( int argc, char **argv)
{

	if( (argc != 5) || ((argc == 1) && !strcmp(argv[1],"/?"))
       || !strcmp(argv[1],"/h") || !strcmp(argv[1],"/H"))
    {
        printf("Main Wrapper PAL -Composite Critical Section Test\n");
        printf("Usage:\n");
	 printf("\t[PROCESS_COUNT] Greater than or Equal to  1 \n");
	 printf("\t[THREAD_COUNT]  Greater than or Equal to 1 and Less than or Equal to 64 \n");
        printf("\t[REPEAT_COUNT] Greater than or Equal to 1\n");
        printf("\t[RELATION_ID  [Greater than or Equal to 1]\n");

        return -1;
    }

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

//Main entry point for the application
PALTEST(composite_synchronization_criticalsection_paltest_synchronization_criticalsection, "composite/synchronization/criticalsection/paltest_synchronization_criticalsection")
{
	unsigned int i = 0;
	HANDLE hProcess[MAXIMUM_WAIT_OBJECTS];  //Array to hold Process handles
	DWORD processReturnCode = 0;
	int testReturnCode = PASS;
	STARTUPINFO si[MAXIMUM_WAIT_OBJECTS];
	PROCESS_INFORMATION pi[MAXIMUM_WAIT_OBJECTS];
	FILE *hFile;  //handle to application results file
	char fileName[MAX_PATH];  //file name of the application results file
	struct applicationStatistics appStats;
	DWORD dwStart=0;	//to store the tick count
	char lpCommandLine[MAX_PATH] = "";
	int returnCode = 0;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }




    if(GetParameters(argc, argv))
    {
        Fail("Error in obtaining the parameters\n");
    }

   //Initialize Application Statistics Structure
 	appStats.operationTime=0;
	appStats.relationId   = RELATION_ID;
    	appStats.processCount = USE_PROCESS_COUNT;
    	appStats.threadCount  = THREAD_COUNT;
    	appStats.repeatCount  = REPEAT_COUNT;
    	appStats.buildNumber  = getBuildNumber();


_snprintf(fileName, MAX_PATH, "main_criticalsection_%d_.txt", RELATION_ID);

hFile = fopen(fileName, "w+");

if(hFile == NULL)
    {
        Fail("Error in opening file to write application results for Critical Section Test, and error code is %d\n", GetLastError());
    }

//Start Process Time Capture
dwStart = GetTickCount();

for( i = 0; i < USE_PROCESS_COUNT; i++ )
    {

        ZeroMemory( lpCommandLine, MAX_PATH );
        if ( _snprintf( lpCommandLine, MAX_PATH-1, "criticalsection %d %d %d %d", i, THREAD_COUNT, REPEAT_COUNT, RELATION_ID) < 0 )
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
            Fail("Process Not created for [%d] and failed with error code %d\n", i, GetLastError());
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
        Fail("Test Failed\n");

    }

//Write Process Result Contents to File
if(hFile!= NULL)
    {
            fprintf(hFile, "%lu,%d,%d,%d,%d,%s\n", appStats.operationTime, appStats.relationId,appStats.processCount, appStats.threadCount, appStats.repeatCount, appStats.buildNumber);
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
