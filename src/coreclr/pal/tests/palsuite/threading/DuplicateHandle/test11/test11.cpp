// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test11.c
**
** Purpose:
**
** Test to ensure proper operation of the DuplicateHandle API.
** The test launches a trivial child process, then opens
** a handle to it using OpenProcess. It then duplicates that
** handle and uses it to wait for the child process to terminate,
** and then checks the exit code of the child process in order to
** verify that it was in fact a handle to the correct
** process. The test tries to duplicate the handle again after
** the process has been closed, to verify that failure ensues.
**
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               Fail
**               ZeroMemory
**               GetCurrentDirectoryW
**               CreateProcessW
**               WaitForSingleObject
**               CreateMutexW
**               ReleaseMutex
**               CloseHandle
**               GetLastError
**               strlen
**               strncpy
**
**
**===========================================================================*/
#include <palsuite.h>
#include "myexitcode.h"

PALTEST(threading_DuplicateHandle_test11_paltest_duplicatehandle_test11, "threading/DuplicateHandle/test11/paltest_duplicatehandle_test11")
{
    const char* rgchChildFile = "childprocess";

    STARTUPINFO si;
    PROCESS_INFORMATION pi;

    DWORD dwError;
    DWORD dwExitCode;
    DWORD dwFileLength;
    DWORD dwDirLength;
    DWORD dwSize;
    DWORD dwRet;

    HANDLE hMutex;
    HANDLE hChildProcess;
    HANDLE hDupChildProcess;

    char  rgchDirName[_MAX_DIR];
    char  absPathBuf[_MAX_PATH];
    char* rgchAbsPathName;

    BOOL ret = FAIL;
    BOOL bChildDone = FALSE;
    WCHAR wszMutexName[] = { 'T','E','S','T','1','1','\0' };

    /* initialize the PAL */
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return( FAIL );
    }

    /* create a mutex to synchronize with the child process */
    hMutex = CreateMutexW( NULL, TRUE, wszMutexName );
    if( hMutex == NULL )
    {
        Fail( "ERROR:%lu:CreateMutex() call failed\r\n", GetLastError() );
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
        if( ReleaseMutex( hMutex ) == 0 )
        {
            Trace( "ERROR:%lu:ReleaseMutex() call failed\n", GetLastError() );
        }
        if( CloseHandle( hMutex ) == 0 )
        {
            Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        }
        Fail( "Palsuite Code: mkAbsoluteFilename() call failed.  Could ",
              "not build absolute path name to file\n.  Exiting.\n" );
    }

    LPWSTR rgchAbsPathNameW = convert(rgchAbsPathName);
    /* launch the child process    */
    if( !CreateProcess(     NULL,               /* module name to execute */
                            rgchAbsPathNameW,   /* command line */
                            NULL,               /* process handle not */
                                                /* inheritable */
                            NULL,               /* thread handle not */
                                                /*inheritable */
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
        if( ReleaseMutex( hMutex ) == 0 )
        {
            Trace( "ERROR:%lu:ReleaseMutex() call failed\n", GetLastError() );
        }
        if( CloseHandle( hMutex ) == 0 )
        {
            Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        }
        Fail( "CreateProcess call failed with error code %d\n",
              dwError );
    }

    free(rgchAbsPathNameW);

    /* open another handle to the child process */
    hChildProcess = OpenProcess(    PROCESS_ALL_ACCESS,   /* access */
                                    FALSE,                /* inheritable */
                                    pi.dwProcessId        /* process id */
                                );
    if( hChildProcess == NULL )
    {
        dwError = GetLastError();
        if( ReleaseMutex( hMutex ) == 0 )
        {
            Trace( "ERROR:%lu:ReleaseMutex() call failed\n", GetLastError() );
        }
        Trace( "ERROR:%lu:OpenProcess call failed\n", dwError );
        goto cleanup3;
    }

    /* duplicate the child process handle */
    if( ! DuplicateHandle(  GetCurrentProcess(),
                            hChildProcess,
                            GetCurrentProcess(),
                            &hDupChildProcess,
                            GENERIC_READ|GENERIC_WRITE,
                            FALSE,
                            DUPLICATE_SAME_ACCESS) )
    {
        Trace( "ERROR:%lu:DuplicateHandle() call failed\n", GetLastError() );
        if( ReleaseMutex( hMutex ) == 0 )
        {
            Trace( "ERROR:%lu:ReleaseMutex() call failed\n", GetLastError() );
        }
        goto cleanup2;
    }

    /* release the mutex so the child can proceed */
    if( ReleaseMutex( hMutex ) == 0 )
    {
        Trace( "ERROR:%lu:ReleaseMutex() call failed\n", GetLastError() );
        goto cleanup1;
    }

    /* wait for the child process to complete, using the new handle */
    dwRet = WaitForSingleObject( hDupChildProcess, 10000 );
    if( dwRet != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForSingleObject call returned %lu, "
                "expected WAIT_OBJECT_0",
                dwRet );
        goto cleanup1;
    }

    /* remember that we waited until the child was finished */
    bChildDone = TRUE;

    /* check the exit code from the process -- this is a bit of an */
    /* extra verification that we opened the correct process handle */
    if( ! GetExitCodeProcess( hDupChildProcess, &dwExitCode ) )
    {
        Trace( "ERROR:%lu:GetExitCodeProcess call failed\n", GetLastError() );
        goto cleanup1;
    }

    /* verification     */
    if( (dwExitCode & 0xFF) != (TEST_EXIT_CODE & 0xFF) )
    {
        Trace( "GetExitCodeProcess returned an incorrect exit code %d, "
              "expected value is %d\n",
              (dwExitCode & 0xFF),
              (TEST_EXIT_CODE & 0xFF));
        goto cleanup1;
    }

    /* close the duplicate handle */
    if( ! CloseHandle( hDupChildProcess ) )
    {
        Trace( "ERROR:%lu:CloseHandle call failed\n", GetLastError() );
        goto cleanup2;
    }

    /* close the child process handle */
    if( ! CloseHandle ( hChildProcess ) )
    {
        Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        goto cleanup3;
    }

    /* try to call duplicate handle on the closed child process handle */
    if( DuplicateHandle(    GetCurrentProcess(),
                            hChildProcess,
                            GetCurrentProcess(),
                            &hDupChildProcess,
                            GENERIC_READ|GENERIC_WRITE,
                            FALSE,
                            DUPLICATE_SAME_ACCESS) )
    {
        Trace( "ERROR:%lu:DuplicateHandle call succeeded on "
                "a closed process handle, expected ERROR_INVALID_HANDLE\n" );
        if( ! CloseHandle( hDupChildProcess ) )
        {
            Trace( "ERROR:%lu:CloseHandle call failed\n", GetLastError() );
        }
        goto cleanup3;
    }

    /* verify that the last error was ERROR_INVALID_HANDLE */
    dwRet = GetLastError();
    if( dwRet != ERROR_INVALID_HANDLE )
    {
        Trace( "ERROR:DuplicateHandle returned %lu, "
                "expected ERROR_INVALID_HANDLE\n",
                dwRet );
        goto cleanup3;
    }


    /* success if we get here */
    ret = PASS;

    /* skip the cleanup stuff that's already done */
    goto cleanup3;


cleanup1:
    /* close our duplicate handle */
    if( ! CloseHandle( hDupChildProcess ) )
    {
        Trace( "ERROR:%lu:CloseHandle call failed\n", GetLastError() );
        ret = FAIL;
    }

cleanup2:
    /* wait on the child process to complete if necessary */
    if( ! bChildDone )
    {
        dwRet = WaitForSingleObject( hChildProcess, 10000 );
        if( dwRet != WAIT_OBJECT_0 )
        {
            Trace( "ERROR:WaitForSingleObject call returned %lu, "
                    "expected WAIT_OBJECT_0",
                    dwRet );
            ret = FAIL;
        }
    }

    /* close our child process handle */
    if( CloseHandle ( hChildProcess ) == 0 )
    {
        Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        ret = FAIL;
    }

cleanup3:
    /* close all our other handles */
    if( CloseHandle ( pi.hProcess ) == 0 )
    {
        Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        ret = FAIL;
    }
    if( CloseHandle ( pi.hThread ) == 0 )
    {
        Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        ret = FAIL;
    }
    if( CloseHandle( hMutex ) == 0 )
    {
        Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        ret = FAIL;
    }

    if( ret == FAIL )
    {
        Fail( "test failed\n" );
    }



    /* terminate the PAL */
    PAL_Terminate();

    /* return success */
    return PASS;
}
