// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: test1.c
**
** Purpose: Create a child process and some events for communications with it.
** When the child gets back to us with a memory location and a length,
** Call WriteProcessMemory on this location and check to see that it
** writes successfully.
**
**
**============================================================*/

#define UNICODE

#include "commonconsts.h"

#include <palsuite.h>

PALTEST(debug_api_WriteProcessMemory_test1_paltest_writeprocessmemory_test1, "debug_api/WriteProcessMemory/test1/paltest_writeprocessmemory_test1")
{

    PROCESS_INFORMATION pi;
    STARTUPINFO si;
    HANDLE hEvToHelper;
    HANDLE hEvFromHelper;
    DWORD dwExitCode;


    DWORD dwRet;
    char cmdComposeBuf[MAX_PATH];
    PWCHAR uniString;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Create the signals we need for cross process communication */
    hEvToHelper = CreateEvent(NULL, TRUE, FALSE, szcToHelperEvName);
    if (!hEvToHelper)
    {
        Fail("WriteProcessMemory: CreateEvent of '%S' failed. "
             "GetLastError() returned %d.\n", szcToHelperEvName,
             GetLastError());
    }
    if (GetLastError() == ERROR_ALREADY_EXISTS)
    {
        Fail("WriteProcessMemory: CreateEvent of '%S' failed. "
             "(already exists!)\n", szcToHelperEvName);
    }
    hEvFromHelper = CreateEvent(NULL, TRUE, FALSE, szcFromHelperEvName);
    if (!hEvToHelper)
    {
        Fail("WriteProcessMemory: CreateEvent of '%S' failed. "
             "GetLastError() returned %d.\n", szcFromHelperEvName,
             GetLastError());
    }
    if (GetLastError() == ERROR_ALREADY_EXISTS)
    {
        Fail("WriteProcessMemory: CreateEvent of '%S' failed. "
             "(already exists!)\n", szcFromHelperEvName);
    }
    ResetEvent(hEvFromHelper);
    ResetEvent(hEvToHelper);

    if (!sprintf_s(cmdComposeBuf, ARRAY_SIZE(cmdComposeBuf), "helper %s", commsFileName))
    {
        Fail("Could not convert command line\n");
    }
    uniString = convert(cmdComposeBuf);

    ZeroMemory( &si, sizeof(si) );
    si.cb = sizeof(si);
    ZeroMemory( &pi, sizeof(pi) );

    /* Create a new process.  This is the process that will ask for
     * memory munging */
    if(!CreateProcess( NULL, uniString, NULL, NULL,
                        FALSE, 0, NULL, NULL, &si, &pi))
    {
        Trace("ERROR: CreateProcess failed to load executable '%S'.  "
             "GetLastError() returned %u.\n",
              uniString, GetLastError());
        free(uniString);
        Fail("");
    }
    free(uniString);

    while(1)
    {
        FILE *commsFile;
        char* pSrcMemory;
        char* pDestMemory;
        int Count;
        SIZE_T wpmCount;
        char incomingCMDBuffer[MAX_PATH + 1];

        /* wait until the helper tells us that it has given us
         * something to do */
        dwRet = WaitForSingleObject(hEvFromHelper, TIMEOUT);
        if (dwRet != WAIT_OBJECT_0)
        {
            Trace("test1 WaitForSingleObjectTest:  WaitForSingleObject "
                  "failed (%u)\n", GetLastError());
            break; /* no more work incoming */
        }

        /* get the parameters to test WriteProcessMemory with */
        if (!(commsFile = fopen(commsFileName, "r")))
        {
            /* no file means there is no more work */
            break;
        }
        if ( NULL == fgets(incomingCMDBuffer, MAX_PATH, commsFile))
        {
            Fail ("unable to read from communication file %s "
                  "for reasons %u & %u\n",
                  errno, GetLastError());
        }
        PEDANTIC1(fclose,(commsFile));
        sscanf(incomingCMDBuffer, "%u %u", &pDestMemory, &Count);
        if (argc > 1)
        {
            Trace("Preparing to write to %u bytes @ %u ('%s')\n",
                  Count, pDestMemory, incomingCMDBuffer);
        }

        /* compose some data to write to the client process */
        if (!(pSrcMemory = (char*)malloc(Count)))
        {
            Trace("could not dynamically allocate memory to copy from "
                  "for reasons %u & %u\n",
                  errno, GetLastError());
            goto doneIteration;
        }
        memset(pSrcMemory, nextValue, Count);

        /* do the work */
        dwRet = WriteProcessMemory(pi.hProcess,
                           pDestMemory,
                           pSrcMemory,
                           Count,
                           &wpmCount);
        if (!dwRet)
        {
            Trace("%s: Problem: on a write to %u bytes @ %u ('%s')\n",
                  argv[0], Count, pDestMemory, incomingCMDBuffer);
            Trace("test1 WriteProcessMemory returned a%u(!=0) (GLE=%u)\n",
                  GetLastError());
        }
        if(Count != wpmCount)
        {
            Trace("%s: Problem: on a write to %u bytes @ %u ('%s')\n",
                  argv[0], Count, pDestMemory, incomingCMDBuffer);
            Trace("The number of bytes written should have been "
                 "%u, but was reported as %u.\n", Count, wpmCount);
        }
        free(pSrcMemory);

    doneIteration:
        PEDANTIC(ResetEvent, (hEvFromHelper));
        PEDANTIC(SetEvent, (hEvToHelper));
    }

    /* wait for the child process to complete */
    WaitForSingleObject ( pi.hProcess, TIMEOUT );
    /* this may return a failure code on a success path */

    /* check the exit code from the process */
    if( ! GetExitCodeProcess( pi.hProcess, &dwExitCode ) )
    {
        Trace( "GetExitCodeProcess call failed with error code %u\n",
              GetLastError() );
        dwExitCode = FAIL;
    }


    PEDANTIC(CloseHandle, (hEvToHelper));
    PEDANTIC(CloseHandle, (hEvFromHelper));
    PEDANTIC(CloseHandle, (pi.hThread));
    PEDANTIC(CloseHandle, (pi.hProcess));

    PAL_TerminateEx(dwExitCode);
    return dwExitCode;
}
