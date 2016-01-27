// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test2.c
**
** Purpose: Create a child process and some events for communications with it.  
** When the child gets back to us with a memory location and a length,
** Call WriteProcessMemory on this location and check to see that it 
** writes successfully. Then call ReadProcessMemory to check if the
** contents read are same as those written
**
**
**============================================================*/

#define UNICODE

#include "commonconsts.h"

#include <palsuite.h>

#if defined(BIT64) && defined(PLATFORM_UNIX)
#define LLFORMAT "%I64u"
#else
#define LLFORMAT "%u"
#endif

int __cdecl main(int argc, char *argv[])
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
    
    if (!sprintf(cmdComposeBuf, "helper %s", commsFileName)) 
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
        SIZE_T Count;
        SIZE_T wpmCount;
        char incomingCMDBuffer[MAX_PATH + 1];

        int err;
        HANDLE readProcessHandle;
        DWORD readProcessID;
        char readProcessBuffer[REGIONSIZE]; // size 1024
        BOOL bResult;
        size_t size = 0;

        readProcessID = pi.dwProcessId;

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
        sscanf(incomingCMDBuffer, LLFORMAT " " LLFORMAT, &pDestMemory, &Count);
        if (argc > 1) 
        {
            Trace("Preparing to write to " LLFORMAT " bytes @ " LLFORMAT "('%s')\n", 
                  Count, pDestMemory, incomingCMDBuffer);
        }
     
        /* compose some data to write to the client process */
        if (!(pSrcMemory = malloc(Count)))
        {
            Trace("could not dynamically allocate memory to copy from "
                  "for reasons %u & %u\n",
                  errno, GetLastError());
            goto doneIteration;
        }
        memset(pSrcMemory, nextValue, Count);
        Trace("Preparing to write to " LLFORMAT " bytes @ " LLFORMAT " ('%s')[%u]\n", 
                  Count, pDestMemory, incomingCMDBuffer, pSrcMemory);

        /* do the work */
        dwRet = WriteProcessMemory(pi.hProcess, 
                           pDestMemory,
                           pSrcMemory,
                           Count,
                           &wpmCount);

        if (!dwRet)
        {
            Trace("%s: Problem: on a write to "LLFORMAT " bytes @ " LLFORMAT " ('%s')\n", 
                  argv[0], Count, pDestMemory, incomingCMDBuffer);
            Trace("test1 WriteProcessMemory returned a (!=0) (GLE=%u)\n", 
                  GetLastError());
        }
        if(Count != wpmCount)
        {
            Trace("%s: Problem: on a write to " LLFORMAT " bytes @ " LLFORMAT " ('%s')\n", 
                  argv[0], Count, pDestMemory, incomingCMDBuffer);
            Trace("The number of bytes written should have been "
                 LLFORMAT ", but was reported as " LLFORMAT " \n", Count, wpmCount);
        }
    
        readProcessHandle = OpenProcess(
                PROCESS_VM_READ,
                FALSE,          
                readProcessID);

        if(NULL == readProcessHandle)
        {
            Fail("\nFailed to call OpenProcess API to retrieve "
                    "current process handle error code=%u\n",
                    GetLastError());
        }
        
        /*zero the memory*/
        memset(readProcessBuffer, 0, size);

        /*retrieve the memory contents*/
        bResult = ReadProcessMemory(
                readProcessHandle,         /*current process handle*/
                pDestMemory,      /*base of memory area*/
                (LPVOID)readProcessBuffer,
                Count,            /*buffer length in bytes*/
                &size);
         

        if( !bResult || (Count != size) )
        {
            Trace("\nFailed to call ReadProcessMemory API "
                "to retrieve the memory contents, error code=%u; Bresult[%u] Count[" LLFORMAT "], Size[%d]\n",
                GetLastError(), bResult, Count, size);

            err = CloseHandle(readProcessHandle);

            if(0 == err)
            {
                Trace("\nFailed to call CloseHandle API, error code=%u\n",
                GetLastError());
            }
            dwExitCode = FAIL;
        }

        if( !memcmp (pDestMemory, readProcessBuffer, Count ) )
        {
            Trace("Difference in memory contents, expected [%s], but received [%s]\n", pDestMemory, readProcessBuffer);
            dwExitCode = FAIL;
        }

        Trace("ReadProcessBuffer contains [%s]\n", readProcessBuffer);
        err = CloseHandle(readProcessHandle);
    
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
