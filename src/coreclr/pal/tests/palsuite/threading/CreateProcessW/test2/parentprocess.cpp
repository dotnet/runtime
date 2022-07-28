// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: createprocessw/test2/parentprocess.c
**
** Purpose: Test the following features of CreateProcessW:
**          - Check to see if hProcess & hThread are set in
**            return PROCESS_INFORMATION structure
**          - Check to see if stdin, stdout, & stderr handles
**            are used when STARTF_USESTDHANDLES is specified
**            in STARUPINFO flags and bInheritHandles = TRUE
**          - Check to see that proper arguments are passed to
**            child process
**
** Dependencies: CreatePipe
**               strcpy, strlen, strncmp, memset
**               WaitForSingleObject
**               WriteFile, ReadFile
**               GetExitCodeProcess
**

**
**=========================================================*/

#define UNICODE
#include <palsuite.h>
#include "test2.h"



PALTEST(threading_CreateProcessW_test2_paltest_createprocessw_test2, "threading/CreateProcessW/test2/paltest_createprocessw_test2")
{

    /*******************************************
     *  Declarations
     *******************************************/
    STARTUPINFO si;
    PROCESS_INFORMATION pi;

    HANDLE hTestStdInR = NULL;
    HANDLE hTestStdInW = NULL;
    HANDLE hTestStdOutR = NULL;
    HANDLE hTestStdOutW = NULL;
    HANDLE hTestStdErrR = NULL;
    HANDLE hTestStdErrW = NULL;

    BOOL bRetVal = FALSE;
    DWORD dwBytesWritten = 0;
    DWORD dwBytesRead = 0;
    DWORD dwExitCode = 0;

    SECURITY_ATTRIBUTES pipeAttributes;

    char szStdOutBuf[BUF_LEN];
    char szStdErrBuf[BUF_LEN];
    WCHAR szFullPathNameW[_MAX_PATH];


    /*******************************************
     *  Initialization
     *******************************************/

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    /*Setup SECURITY_ATTRIBUTES structure for CreatePipe*/
    pipeAttributes.nLength              = sizeof(SECURITY_ATTRIBUTES);
    pipeAttributes.lpSecurityDescriptor = NULL;
    pipeAttributes.bInheritHandle       = TRUE;


    /*Create a StdIn pipe for child*/
    bRetVal = CreatePipe(&hTestStdInR,      /* read handle*/
                         &hTestStdInW,      /* write handle */
                         &pipeAttributes,   /* security attributes*/
                         1024);                /* pipe size*/

    if (bRetVal == FALSE)
    {
        Fail("ERROR: %ld :Unable to create stdin pipe\n", GetLastError());
    }


    /*Create a StdOut pipe for child*/
    bRetVal = CreatePipe(&hTestStdOutR,     /* read handle*/
                         &hTestStdOutW,     /* write handle */
                         &pipeAttributes,   /* security attributes*/
                         0);                /* pipe size*/

    if (bRetVal == FALSE)
    {
        Fail("ERROR: %ld :Unable to create stdout pipe\n", GetLastError());
    }


    /*Create a StdErr pipe for child*/
    bRetVal = CreatePipe(&hTestStdErrR,     /* read handle*/
                         &hTestStdErrW,     /* write handle */
                         &pipeAttributes,   /* security attributes*/
                         0);                /* pipe size*/

    if (bRetVal == FALSE)
    {
        Fail("ERROR: %ld :Unable to create stderr pipe\n", GetLastError());
    }

    /* Zero the data structure space */
    ZeroMemory ( &pi, sizeof(pi) );
    ZeroMemory ( &si, sizeof(si) );

    /* Set the process flags and standard io handles */
    si.cb = sizeof(si);
    si.dwFlags = STARTF_USESTDHANDLES;
    si.hStdInput = hTestStdInR;
    si.hStdOutput = hTestStdOutW;
    si.hStdError = hTestStdErrW;

    int mbwcResult = MultiByteToWideChar(CP_ACP, 0, argv[0], -1, szFullPathNameW, sizeof(szFullPathNameW));

    if (0 == mbwcResult)
    {
        Fail ("Palsuite Code: MultiByteToWideChar() call failed. Exiting.\n");
    }

    wcscat(szFullPathNameW, u" ");
    wcscat(szFullPathNameW, szChildFileW);

    wcscat(szFullPathNameW, szArgs);

    /*******************************************
     *  Start Testing
     *******************************************/

    /* Launch the child */
    if ( !CreateProcess (NULL, szFullPathNameW, NULL, NULL, TRUE, 0, NULL, NULL, &si, &pi ))
    {
        Fail("ERROR: CreateProcess call failed.  GetLastError returned %d\n",
             GetLastError() );
    }

    /* Check the returned process information for validity */
    if (pi.hProcess == 0 || pi.hThread == 0)
    {
        Fail("ERROR: CreateProcess Error: Process Handle = %u, Thread Handle = %u\n",
            pi.hProcess, pi.hThread);
    }


    /* Write the Constructed string to stdin pipe for the child process */
    if (WriteFile(hTestStdInW, szTestString, strlen(szTestString), &dwBytesWritten, NULL) == FALSE
        || WriteFile(hTestStdInW, "\n", strlen("\n"), &dwBytesWritten, NULL) == FALSE)
    {
        Fail("ERROR: %ld :unable to write to write pipe handle "
             "hTestStdInW=0x%lx\n", GetLastError(), hTestStdInW);
    }

    /* Wait for the child to finish, Max 20 seconds */
    dwExitCode = WaitForSingleObject(pi.hProcess, 20000);

    /* If the child failed then whole thing fails */
    if (dwExitCode != WAIT_OBJECT_0)
    {
        TerminateProcess(pi.hProcess, 0);
        Fail("ERROR: The child failed to run properly.\n");
    }

    /* Check for problems in the child process */
    if (GetExitCodeProcess(pi.hProcess, &dwExitCode) == FALSE)
    {
        Fail("ERROR: Call to GetExitCodeProcess failed.\n");
    }
    else if (dwExitCode == EXIT_ERR_CODE1)
    {
        Fail("ERROR: The Child process could not reead the string "
             "written to the stdin pipe.\n");
    }
    else if (dwExitCode == EXIT_ERR_CODE2)
    {
        Fail("ERROR: The Child process could not write the string "
             "the stdout pipe or stderr pipe.\n");
    }
    else if (dwExitCode == EXIT_ERR_CODE3)
    {
        Fail("ERROR: The Child received the wrong number of "
             "command line arguments.\n");
    }
    else if (dwExitCode == EXIT_ERR_CODE4)
    {
        Fail("ERROR: The Child received the wrong "
             "command line arguments.\n");
    }
    else if (dwExitCode != EXIT_OK_CODE)
    {
        Fail("ERROR: Unexpected exit code returned: %u.  Child process "
             "did not complete its part of the test.\n", dwExitCode);
    }


    /* The child ran ok, so check to see if we received the proper */
    /* strings through the pipes.                                  */

    /* clear our buffers */
    memset(szStdOutBuf, 0, BUF_LEN);
    memset(szStdErrBuf, 0, BUF_LEN);

    /* Read the data back from the child process stdout */
    bRetVal = ReadFile(hTestStdOutR,       /* handle to read pipe*/
                       szStdOutBuf,        /* buffer to write to*/
                       BUF_LEN,            /* number of bytes to read*/
                       &dwBytesRead,       /* number of bytes read*/
                       NULL);              /* overlapped buffer*/

    /*Read the data back from the child process stderr */
    bRetVal = ReadFile(hTestStdErrR,       /* handle to read pipe*/
                       szStdErrBuf,        /* buffer to write to*/
                       BUF_LEN,                /* number of bytes to read*/
                       &dwBytesRead,       /* number of bytes read*/
                       NULL);              /* overlapped buffer*/


    /* Confirm that we received the same string that we originally */
    /* wrote to the child and was received on both stdout & stderr.*/
    if (strncmp(szTestString, szStdOutBuf, strlen(szTestString)) != 0
        || strncmp(szTestString, szStdErrBuf, strlen(szTestString)) != 0)
    {
        Fail("ERROR: The data read back from child does not match "
             "what was written. STDOUT: %s  STDERR: %s\n",
             szStdOutBuf, szStdErrBuf);
    }


    /*******************************************
     *  Clean Up
     *******************************************/

    /* Close process and thread handle */
    CloseHandle ( pi.hProcess );
    CloseHandle ( pi.hThread );

    CloseHandle(hTestStdInR);
    CloseHandle(hTestStdInW);
    CloseHandle(hTestStdOutR);
    CloseHandle(hTestStdOutW);
    CloseHandle(hTestStdErrR);
    CloseHandle(hTestStdErrW);

    PAL_Terminate();
    return ( PASS );
}
