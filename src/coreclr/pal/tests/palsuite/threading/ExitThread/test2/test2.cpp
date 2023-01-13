// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test2.c
**
** Purpose: Test to ensure ExitThread() called from the last thread of
**          a process shuts down that process and returns the proper
**          exit code as specified in the ExitThread() call.
**
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               Fail
**               ZeroMemory
**               GetCurrentDirectoryW
**               CreateProcessW
**               WaitForSingleObject
**               GetLastError
**               strlen
**               strncpy
**

**
**===========================================================================*/
#include <palsuite.h>
#include "myexitcode.h"

PALTEST(threading_ExitThread_test2_paltest_exitthread_test2, "threading/ExitThread/test2/paltest_exitthread_test2")
{
    const char* rgchChildFile = "childprocess";

    STARTUPINFO si;
    PROCESS_INFORMATION pi;

    DWORD dwError;
    DWORD dwExitCode;
    DWORD dwFileLength;
    DWORD dwDirLength;
    DWORD dwSize;
    DWORD dwExpected = TEST_EXIT_CODE;

    char  rgchDirName[_MAX_DIR];
    char  absPathBuf[_MAX_PATH];
    char* rgchAbsPathName;

    /* initialize the PAL */
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return( FAIL );
    }

    /* zero our process and startup info structures */
    ZeroMemory( &si, sizeof(si) );
    si.cb = sizeof( si );
    ZeroMemory( &pi, sizeof(pi) );

    /* build the absolute path to the child process */
    rgchAbsPathName = &absPathBuf[0];
    dwFileLength = strlen( rgchChildFile );

    strcpy(rgchDirName, ".\\");
    dwDirLength = strlen(rgchDirName);

    dwSize = mkAbsoluteFilename(   rgchDirName,
                                   dwDirLength,
                                   rgchChildFile,
                                   dwFileLength,
                                   rgchAbsPathName );
    if( dwSize == 0 )
    {
        Fail( "Palsuite Code: mkAbsoluteFilename() call failed.  Could ",
              "not build absolute path name to file\n.  Exiting.\n" );
    }

    LPWSTR rgchAbsPathNameW = convert(rgchAbsPathName);
    /* launch the child process */
    if( !CreateProcess(     NULL,               /* module name to execute */
                            rgchAbsPathNameW,   /* command line */
                            NULL,               /* process handle not */
                                                /* inheritable */
                            NULL,               /* thread handle not */
                                                /* inheritable */
                            FALSE,              /* handle inheritance */
                            CREATE_NEW_CONSOLE, /* dwCreationFlags */
                            NULL,               /* use parent's environment */
                            NULL,               /* use parent's starting */
                                                /* directory */
                            &si,                /* startup info struct */
                            &pi )               /* process info struct */
        )
    {
        dwError = GetLastError();
        free(rgchAbsPathNameW);
        Fail( "CreateProcess call failed with error code %d\n",
              dwError );
    }

    free(rgchAbsPathNameW);

    /* wait for the child process to complete */
    WaitForSingleObject ( pi.hProcess, INFINITE );

    /* check the exit code from the process */
    if( ! GetExitCodeProcess( pi.hProcess, &dwExitCode ) )
    {
        dwError = GetLastError();
        CloseHandle ( pi.hProcess );
        CloseHandle ( pi.hThread );
        Fail( "GetExitCodeProcess call failed with error code %d\n",
              dwError );
    }

    /* close process and thread handle */
    CloseHandle ( pi.hProcess );
    CloseHandle ( pi.hThread );

    /* check for the expected exit code */
    /* exit code for some systems is as small as a char, so that's all */
    /* we'll compare for checking success                              */
    if( LOBYTE(LOWORD(dwExitCode)) != LOBYTE(LOWORD(dwExpected)) )
    {
        Fail( "GetExitCodeProcess returned an incorrect exit code %d, "
              "expected value is %d\n",
              LOWORD(dwExitCode), dwExpected );
    }

    /* terminate the PAL */
    PAL_Terminate();

    /* return success */
    return PASS;
}
