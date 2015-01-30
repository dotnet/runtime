//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure OpenProcess works properly.
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


static const char* rgchPathDelim = "\\";


int
mkAbsoluteFilename( LPSTR dirName,
                    DWORD dwDirLength,
                    LPCSTR fileName,
                    DWORD dwFileLength,
                    LPSTR absPathName )
{
    DWORD sizeDN, sizeFN, sizeAPN;

    sizeDN = strlen( dirName );
    sizeFN = strlen( fileName );
    sizeAPN = (sizeDN + 1 + sizeFN + 1);

    /* ensure ((dirName + DELIM + fileName + \0) =< _MAX_PATH ) */
    if( sizeAPN > _MAX_PATH )
    {
        return ( 0 );
    }

    strncpy( absPathName, dirName, dwDirLength +1 );
    strncpy( absPathName, rgchPathDelim, 2 );
    strncpy( absPathName, fileName, dwFileLength +1 );

    return (sizeAPN);

}


int __cdecl main( int argc, char **argv )

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

    char  rgchDirName[_MAX_DIR];
    char  absPathBuf[_MAX_PATH];
    char* rgchAbsPathName;

    BOOL ret = FAIL;
    BOOL bChildDone = FALSE;
    WCHAR wszMutexName[] = { 'T','E','S','T','1','\0' };

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

    dwDirLength = GetCurrentDirectory( _MAX_PATH, rgchDirName );
    if( dwDirLength == 0 )
    {
        dwError = GetLastError();
        if( ReleaseMutex( hMutex ) == 0 )
        {
            Trace( "ERROR:%lu:ReleaseMutex() call failed\n", GetLastError() );
        }
        if( CloseHandle( hMutex ) == 0 )
        {
            Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        }
        Fail( "GetCurrentDirectory call failed with error code %d\n",
              dwError );
    }

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

    /* launch the child process    */
    if( !CreateProcess(     NULL,               /* module name to execute */
                            rgchAbsPathName,    /* command line */
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
        goto cleanup2;
    }

    /* release the mutex so the child can proceed */
    if( ReleaseMutex( hMutex ) == 0 )
    {
        Trace( "ERROR:%lu:ReleaseMutex() call failed\n", GetLastError() );
        goto cleanup;
    }

    /* wait for the child process to complete, using the new handle */
    dwRet = WaitForSingleObject( hChildProcess, 10000 );
    if( dwRet != WAIT_OBJECT_0 )
    {
        Trace( "ERROR:WaitForSingleObject call returned %lu, "
                "expected WAIT_OBJECT_0",
                dwRet );
        goto cleanup;
    }

    /* remember that we waited until the child was finished */
    bChildDone = TRUE;

    /* check the exit code from the process -- this is a bit of an */
    /* extra verification that we opened the correct process handle */
    if( ! GetExitCodeProcess( hChildProcess, &dwExitCode ) )
    {
        Trace( "ERROR:%lu:GetExitCodeProcess call failed\n", GetLastError() );
        goto cleanup;
    }

    /* verification     */
    if( (dwExitCode & 0xFF) != (TEST_EXIT_CODE & 0xFF) )
    {
        Trace( "GetExitCodeProcess returned an incorrect exit code %d, "
              "expected value is %d\n",
              (dwExitCode & 0xFF),
              (TEST_EXIT_CODE & 0xFF));
        goto cleanup;
    }

    /* success if we get here */
    ret = PASS;


cleanup:
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

    /* close all our handles */
    if( CloseHandle ( hChildProcess ) == 0 )
    {
        Trace( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
        ret = FAIL;
    }

cleanup2:
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
