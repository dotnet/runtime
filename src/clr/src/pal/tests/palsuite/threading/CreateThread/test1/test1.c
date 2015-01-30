//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source: test1.c 
**
** Purpose: Test for CreateThread.  Call CreateThread and ensure
** that it succeeds.  Also check to ensure the paramater is passed
** properly.
**
**
**=========================================================*/

#include <palsuite.h>

#ifndef PLATFORM_UNIX
#define LLFORMAT "%I64u"
#else
#define LLFORMAT "%llu"
#endif

ULONGLONG dwCreateThreadTestParameter = 0;

DWORD PALAPI CreateThreadTestThread( LPVOID lpParameter)
{
    DWORD dwRet = 0;
    
    /* save parameter for test */
    dwCreateThreadTestParameter = (ULONGLONG)lpParameter;
    
    return dwRet;
}

BOOL CreateThreadTest()
{
    BOOL bRet = FALSE;
    DWORD dwRet = 0;

    LPSECURITY_ATTRIBUTES lpThreadAttributes = NULL;
    DWORD dwStackSize = 0; 
    LPTHREAD_START_ROUTINE lpStartAddress =  &CreateThreadTestThread;
    LPVOID lpParameter = lpStartAddress;
    DWORD dwCreationFlags = 0;  /* run immediately */
    DWORD dwThreadId = 0;

    HANDLE hThread = 0;

    dwCreateThreadTestParameter = 0;

    /* Create a thread, passing the appropriate paramaters as declared
       above.
    */
    
    hThread = CreateThread( lpThreadAttributes,   
                            dwStackSize,          
                            lpStartAddress,       
                            lpParameter,          
                            dwCreationFlags,      
                            &dwThreadId );       
    
    /* Ensure that the HANDLE is not invalid! */
    if (hThread != INVALID_HANDLE_VALUE)
    {
        dwRet = WaitForSingleObject(hThread,INFINITE);

        if (dwRet != WAIT_OBJECT_0)
        {
            Trace("CreateThreadTest:WaitForSingleObject "
                   "failed (%x)\n",GetLastError());
        }
        else
        {
            /* Check to ensure that the parameter passed to the thread
               function is the same in the function as what we passed.
            */
            
            if (dwCreateThreadTestParameter != (ULONGLONG)lpParameter)
            {
                Trace("CreateThreadTest:parameter error.  The "
                       "parameter passed should have been " LLFORMAT " but when "
                       "passed to the Thread function it was " LLFORMAT " . GetLastError[%x]\n",
                       dwCreateThreadTestParameter,lpParameter, GetLastError());
            }
            else
            {
                bRet = TRUE;
            }

        }
	CloseHandle(hThread);
    }
    else
    {
        Trace("CreateThreadTest:CreateThread failed (%x)\n",GetLastError());
    }

    return bRet; 
}


int __cdecl main(int argc, char **argv)
{
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }
    
    if(!CreateThreadTest()) 
    {
        Fail ("Test failed\n");
    }
      
    Trace("Test Passed\n");
    PAL_Terminate();
    return ( PASS );

}
