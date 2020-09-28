// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: WFSOProcessTest.c
**
** Purpose: Test for WaitForSingleObjectTest.
**			Create One Process and do some work
**			Use WFSO For the Process to finish
**
** Test Passes if the above operations are successful
**
**
**
**=========================================================*/



#include <palsuite.h>

PALTEST(threading_WaitForSingleObject_WFSOProcessTest_paltest_waitforsingleobject_wfsoprocesstest, "threading/WaitForSingleObject/WFSOProcessTest/paltest_waitforsingleobject_wfsoprocesstest")
{

//Declare local variables
STARTUPINFO si;
PROCESS_INFORMATION pi;

DWORD dwWaitResult=0;

//Initialize PAL
if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }


ZeroMemory( &si, sizeof(si) );
si.cb = sizeof(si);
ZeroMemory( &pi, sizeof(pi) );

LPWSTR nameW =  convert("childprocess");
// Start the child process.
if( !CreateProcess( NULL, // No module name (use command line).
    nameW,  // Command line.
    NULL,             // Process handle not inheritable.
    NULL,             // Thread handle not inheritable.
    FALSE,            // Set handle inheritance to FALSE.
    0,                // No creation flags.
    NULL,             // Use parent's environment block.
    NULL,             // Use parent's starting directory.
    &si,              // Pointer to STARTUPINFO structure.
    &pi )             // Pointer to PROCESS_INFORMATION structure.
)

{
DWORD dwError = GetLastError();
free(nameW);
Fail ( "Create Process Failed.  Failing test.\n"
	       "GetLastError returned %d\n", GetLastError());
}

free(nameW);

// Wait until child process exits.
  dwWaitResult = WaitForSingleObject( pi.hProcess, INFINITE );
switch (dwWaitResult)
	{
    // The Process wait was successful
    case WAIT_OBJECT_0:
        	  		{

			Trace("Wait for Process was successful\n");
			break;
			        }

    // Time-out.
    case WAIT_TIMEOUT:
			{
				Fail ( "Time -out.  Failing test.\n"
	       			"GetLastError returned %d\n", GetLastError());
				return FALSE;
    			}

    // Got ownership of the abandoned process object.
    case WAIT_ABANDONED:
			{
				Fail ( "Got ownership of the abandoned Process object.  Failing test.\n"
	       			"GetLastError returned %d\n", GetLastError());
				return FALSE;
    			}

    //Error condition
    case WAIT_FAILED:
			{
				Fail ( "Wait for Process Failed.  Failing test.\n"
					"GetLastError returned %d\n", GetLastError());
				return FALSE;
  			}

}



// Close process handle
if (0==CloseHandle(pi.hProcess))
	 		    	{
	 		    		Trace("Could not close process handle\n");
					Fail ( "GetLastError returned %d\n", GetLastError());
			    	}


PAL_Terminate();
return ( PASS );

}







