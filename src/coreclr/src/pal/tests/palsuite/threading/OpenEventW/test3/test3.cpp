// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test3.c
**
** Purpose: Test to ensure that OpenEventW() works when
** opening an event created by another process. This test
** program launches a child process which creates a
** named, initially-unset event. The child waits up to
** 10 seconds for the parent process to open that event
** and set it, and returns PASS if the event was set or FAIL
** otherwise. The parent process checks the return value
** from the child to verify that the opened event was
** properly used across processes.
**
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               Fail
**               ZeroMemory
**               GetCurrentDirectoryW
**               CreateProcessW
**               WaitForSingleObject
**               GetExitCodeProcess
**               GetLastError
**               strlen
**               strncpy
**
**
**===========================================================================*/
#include <palsuite.h>

#define TIMEOUT 60000

PALTEST(threading_OpenEventW_test3_paltest_openeventw_test3, "threading/OpenEventW/test3/paltest_openeventw_test3")
{
    BOOL ret = FAIL;
    LPSECURITY_ATTRIBUTES   lpEventAttributes = NULL;

    STARTUPINFO si;
    PROCESS_INFORMATION pi;

    DWORD dwExitCode;

    DWORD  dwRet = 0;
    HANDLE hEvent = NULL;
    WCHAR  wcName[] = {'P','A','L','R','o','c','k','s','\0'};
    LPWSTR lpName = wcName;
    char lpCommandLine[MAX_PATH] = "";

    /* initialize the PAL */
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return( FAIL );
    }

    /* zero our process and startup info structures */
    ZeroMemory( &si, sizeof(si) );
    si.cb = sizeof( si );
    ZeroMemory( &pi, sizeof(pi) );

    /* create an event which we can use with SetEvent */
    hEvent = CreateEventW(  lpEventAttributes,
                            TRUE,               /* manual reset */
                            FALSE,              /* unsignalled  */
                            lpName );

    if( hEvent == NULL )
    {
        /* ERROR */
        Fail(   "ERROR:%lu:CreateEventW() call failed in child\n",
                GetLastError());
    }

    ZeroMemory( lpCommandLine, MAX_PATH );
    if ( sprintf_s( lpCommandLine, MAX_PATH-1, "childprocess ") < 0 )
    {
        Fail ("Error: Insufficient lpCommandline for\n");
    }

    LPWSTR lpCommandLineW = convert(lpCommandLine);
    /* launch the child process */
    if( !CreateProcess(     NULL,               /* module name to execute */
                            lpCommandLineW,     /* command line */
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
        DWORD dwError = GetLastError();
        free(lpCommandLineW);
        Fail( "ERROR:%lu:CreateProcess call failed\n",
              dwError);
    }

    free(lpCommandLineW);

    /* verify that the event is signalled by the child process */
    dwRet = WaitForSingleObject( hEvent, TIMEOUT );
    if( dwRet != WAIT_OBJECT_0 )
    {
        ret = FAIL;
        /* ERROR */
        Trace( "ERROR:WaitForSingleObject() call returned %lu, "
                "expected WAIT_TIMEOUT\n",
                "expected WAIT_OBJECT_0\n",
                dwRet );

        goto cleanup;

        if( !CloseHandle( hEvent ) )
        {
            Trace(   "ERROR:%lu:CloseHandle() call failed in child\n",
                    GetLastError());
        }
        goto cleanup;
    }

    /* wait for the child process to complete */
    dwRet = WaitForSingleObject ( pi.hProcess, TIMEOUT );
    if( dwRet != WAIT_OBJECT_0 )
    {
        ret = FAIL;
        Trace( "ERROR:WaitForSingleObject() returned %lu, "
                "expected %lu\n",
                dwRet,
                WAIT_OBJECT_0 );
        goto cleanup;
    }

    /* check the exit code from the process */
    if( ! GetExitCodeProcess( pi.hProcess, &dwExitCode ) )
    {
        ret = FAIL;
        Trace( "ERROR:%lu:GetExitCodeProcess call failed\n",
              GetLastError() );
        goto cleanup;
    }

    /* check for success */
    ret = (dwExitCode == PASS) ? PASS : FAIL;

cleanup:
    if( hEvent != NULL )
    {
        if( ! CloseHandle ( hEvent ) )
        {
            Trace( "ERROR:%lu:CloseHandle call failed on event handle\n",
                  GetLastError() );
            ret = FAIL;
        }
    }


    /* close process and thread handle */
    if( ! CloseHandle ( pi.hProcess ) )
    {
        Trace( "ERROR:%lu:CloseHandle call failed on process handle\n",
              GetLastError() );
        ret = FAIL;
    }

    if( ! CloseHandle ( pi.hThread ) )
    {
        Trace( "ERROR:%lu:CloseHandle call failed on thread handle\n",
              GetLastError() );
        ret = FAIL;
    }

    /* output a convenient error message and exit if we failed */
    if( ret == FAIL )
    {
        Fail( "test failed\n" );
    }


    /* terminate the PAL */
    PAL_Terminate();

    /* return success */
    return ret;
}
