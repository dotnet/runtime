// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test1.c
**
** Purpose: Debugs the helper application.  Checks that certain events, in 
**          particular the OUTPUT_DEBUG_STRING_EVENT, is generated correctly
**          and gives the correct values.
**
**
**============================================================*/

#include <palsuite.h>

const int DELAY_MS = 2000;

struct OutputCheck 
{
    DWORD ExpectedEventCode;
    DWORD ExpectedUnicode;
    char *ExpectedStr;
};

int __cdecl main(int argc, char *argv[])
{
    
    PROCESS_INFORMATION pi;
    STARTUPINFO si;
        
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
    
    ZeroMemory( &si, sizeof(si) );
    si.cb = sizeof(si);
    ZeroMemory( &pi, sizeof(pi) );
    
    /* Create a new process.  This is the process to be Debugged */
    if(!CreateProcess( NULL, "helper", NULL, NULL, 
                       FALSE, 0, NULL, NULL, &si, &pi)) 
    {
        Fail("ERROR: CreateProcess failed to load executable 'helper'.  "
             "GetLastError() returned %d.\n",GetLastError());
    }

    /* This is the main loop.  It exits when the process which is being
       debugged is finished executing.
    */
    
    while(1)
    {    
        DWORD dwRet = 0;
        dwRet = WaitForSingleObject(pi.hProcess,
                                    DELAY_MS /* Wait for 2 seconds max*/
            );
    
        if (dwRet != WAIT_OBJECT_0)
        {
            Trace("WaitForSingleObjectTest:WaitForSingleObject "
                  "failed (%x) after waiting %d seconds for the helper\n",
                  GetLastError(), DELAY_MS / 1000);
        }
        else
        {
            DWORD dwExitCode;

            /* check the exit code from the process */
            if( ! GetExitCodeProcess( pi.hProcess, &dwExitCode ) )
            {
                DWORD dwError;

                dwError = GetLastError();
                CloseHandle ( pi.hProcess );
                CloseHandle ( pi.hThread );
                Fail( "GetExitCodeProcess call failed with error code %d\n", 
                      dwError ); 
            }

            if(dwExitCode != STILL_ACTIVE) {
                CloseHandle(pi.hThread);
                CloseHandle(pi.hProcess);
                break;
            }
            Trace("still executing %d..\n", dwExitCode);
        }        
    }
        
    PAL_Terminate();
    return PASS;
}
