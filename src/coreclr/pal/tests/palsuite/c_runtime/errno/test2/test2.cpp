// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test2.c
**
** Purpose: Test that errno is 'per-thread' as noted in the documentation. 
**
**
**==========================================================================*/

#include <palsuite.h>

/* 
   This thread function just checks that errno is initially 0 and then sets
   it to a new value before returning.
*/
DWORD PALAPI ThreadFunc_errno_test2( LPVOID lpParam ) 
{ 
       
    if(errno != 0) 
    {
        *((DWORD*)lpParam) = 1;
    }

    errno = 20;

    return 0; 
} 


PALTEST(c_runtime_errno_test2_paltest_errno_test2, "c_runtime/errno/test2/paltest_errno_test2")
{
    DWORD dwThreadId, dwThrdParam = 0; 
    HANDLE hThread; 
    
    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }
    
    /* Set errno to a value within this thread */

    errno = 50;
    
    hThread = CreateThread(NULL, 0, ThreadFunc_errno_test2, &dwThrdParam, 0, &dwThreadId);
    
    if (hThread == NULL) 
    {
        Fail("ERROR: CreateThread failed to create a thread.  "
             "GetLastError() returned %d.\n",GetLastError());
    }
    
    WaitForSingleObject(hThread, INFINITE);
 
    /* This checks the result of calling the thread */
    if(dwThrdParam)
    {
        Fail("ERROR: errno was not set to 0 in the new thread.  Each "
             "thread should have its own value for errno.\n");
    }
    
    /* Check to make sure errno is still set to 50 */
    if(errno != 50)
    {
        Fail("ERROR: errno should be 50 in the main thread, even though "
             "it was set to 20 in another thread.  Currently it is %d.\n",
             errno);
    }
    
    PAL_Terminate();
    return PASS;
}
