//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source: test1.c 
**
** Purpose: Test for SetThreadContext. Create a thread, which is a 
** function that is counting.  Then suspend it, and then make sure
** GetThreadContext and SetThreadContext can run.
** This test case is modified from suspendthread test1 test case.
**
**
**=========================================================*/

#include <palsuite.h>

volatile DWORD dwSuspendThreadTestParameter = 0;
volatile DWORD dwSuspendThreadTestCounter = 0;

#if defined(_PPC_)
void ModifyContext(CONTEXT *lpContext)
{
    ULONG *gpr;
    double *fpr;
    int i;
    /* Since Fpscr is a double in PAL,
       This variable is used for setting Fpscr.
       Only the lower 32-bits are valid. */
    ULONGLONG fpscrll = 0x12345678;

    gpr = &lpContext->Gpr0;
    for (i = 0; i < 32; i++, gpr++)
    {
        *gpr = i;
    }

    /* Note: Msr cannot be changed. */
    lpContext->Cr = 1;
    lpContext->Xer = 2;
    lpContext->Iar = 3;
    lpContext->Lr = 4;
    lpContext->Ctr = 5;

    lpContext->Fpscr = *(double *)&fpscrll;
    fpr = &lpContext->Fpr0;
    for (i = 0; i < 32; i++, fpr++)
    {
        *fpr = (double) (i*0.1);
    }
}
#elif defined(_SPARC_)
void ModifyContext(CONTEXT *lpContext)
{
    ULONG *gpr;
    double *fpr;
    int i;

    gpr = &lpContext->g0;
    for (i = 0; i < 32; i++, gpr++)
    {
        *gpr = i;
    }

    /* Note: psr, and fsr cannot be changed. */
    /* pc and npc must be 4-byte aligned. */
    lpContext->y = 1;
    lpContext->pc = 0x11223344;
    lpContext->npc = 0x12341234;

    fpr = &lpContext->fprs.d[0];
    for (i = 0; i < 16; i++, fpr++)
    {
        *fpr = (double) (i*0.1);
    }
}
#else
void ModifyContext(CONTEXT *lpContext)
{
}
#endif


DWORD PALAPI SuspendThreadTestThread( LPVOID lpParameter)
{
    DWORD dwRet = 0;
    
    /* save parameter for test */
    dwSuspendThreadTestParameter = (DWORD) ((UINT_PTR) lpParameter);

    while (dwSuspendThreadTestParameter)
    {
        dwSuspendThreadTestCounter++;
    }

    return dwRet;
}

BOOL SuspendThreadTest()
{
    BOOL bRet = FALSE;
    DWORD dwRet = 0;

    LPSECURITY_ATTRIBUTES lpThreadAttributes = NULL;
    DWORD dwStackSize = 0; 
    LPTHREAD_START_ROUTINE lpStartAddress =  &SuspendThreadTestThread;
    LPVOID lpParameter = (LPVOID)1;
    DWORD dwCreationFlags = 0;  /* run immediately */
    DWORD dwThreadId = 0;
    CONTEXT savedContext;
    CONTEXT modifiedContext;
    CONTEXT verifyContext;


    HANDLE hThread = 0;

    dwSuspendThreadTestParameter = 0;

    /* Create a thread and ensure it returned a valid handle */

    hThread = CreateThread( lpThreadAttributes, 
                            dwStackSize, lpStartAddress, lpParameter, 
                            dwCreationFlags, &dwThreadId ); 

    if (hThread != INVALID_HANDLE_VALUE)
    {
        /* Wait and ensure that WAIT_TIMEOUT is returned */
        dwRet = WaitForSingleObject(hThread,1000);
        
        if (dwRet != WAIT_TIMEOUT)
        {
            Trace("SuspendThreadTest:WaitForSingleObject "
                   "failed (%x)\n",GetLastError());
        }
        else
        {
            /* Suspend the thread */
            dwRet = SuspendThread(hThread);

            if (dwRet != 0)
            {
                Trace("SuspendThreadTest:SuspendThread "
                       "failed (%x)\n",GetLastError());
            }
            else
            {
                /* now check parameter, it should be greater than 0 */
                if (dwSuspendThreadTestCounter == 0)
                {
                    Trace("SuspendThreadTest:parameter error\n");
                }
                else
                {   

                    /* Initialize CONTEXT variables before testing */
                    memset(&savedContext, 0, sizeof(CONTEXT));
                    savedContext.ContextFlags = CONTEXT_FULL;
                    memset(&verifyContext, 0, sizeof(CONTEXT));
                    verifyContext.ContextFlags = CONTEXT_FULL;
                    
                    /* Read the suspended thread's context using saveContext. */
                    dwRet = GetThreadContext(hThread, &savedContext);
                    if (dwRet != 1)
                    {
                        Fail("GetThreadContext failed for savedContext: %d (%d)\n", dwRet, GetLastError());
                    }

                    /* Make sure GeThreadContext dose get some non-zero values in registers */
                    dwRet = memcmp(&savedContext, &verifyContext, sizeof(CONTEXT));
                    if (dwRet == 0)
                        Fail("GetThreadContext for savedContext do not return meaningful values: (%d)\n", dwRet);
                    
                    /* Copy savedContext to modifiedContext before modification */
                    memcpy(&modifiedContext, &savedContext, sizeof(CONTEXT));
                    
                    ModifyContext(&modifiedContext);

                    /* Modify the suspended thread's context using modifiedContext. */
                    dwRet = SetThreadContext(hThread, &modifiedContext);
                    if (dwRet != 1)
                    {
                        Fail("SetThreadContext failed for modifiedContext: %d (%d)\n", dwRet, GetLastError());
                    }

                    /* Read the suspended thread's context again using verifyContext. */
                    dwRet = GetThreadContext(hThread, &verifyContext);
                    if (dwRet != 1)
                    {
                        Fail("GetThreadContext failed for verifyContext: %d (%d)\n", dwRet, GetLastError());
                    }

                    /* modifiedContext and verifyContext should be the same for suspended thread. */
                    dwRet = memcmp(&modifiedContext, &verifyContext, sizeof(CONTEXT));
                    if (dwRet != 0)
                        Fail("modifiedContext and verifyContext do not match: (%d)\n", dwRet);

                    /* Restore the suspended thread's context. */
                    dwRet = SetThreadContext(hThread, &savedContext);
                    if (dwRet != 1)
                    {
                        Fail("SetThreadContext failed for savedContext: %d (%d)\n", dwRet, GetLastError());
                    }

                    /* Save the counter */
                    dwRet = dwSuspendThreadTestCounter;
                    
                    /* Wait a second */
                    Sleep(1000);

                    /* Ensure the counter hasn't changed becuase the
                       thread was suspended.
                    */
                    
                    if (dwSuspendThreadTestCounter != dwRet)
                    {
                        Trace("1SuspendThreadTest:parameter error\n");
                    }
                    else
                    {
                        /* Resume the thread */
                        dwRet = ResumeThread(hThread);

                        if (dwRet != 1)
                        {
                            Trace("SuspendThreadTest:ResumeThread "
                                   "failed (%x)\n",GetLastError());
                        }
                        else
                        {
                            dwSuspendThreadTestParameter = 0;

                            /* set thread to exit and wait */
                            dwRet = WaitForSingleObject(hThread,INFINITE);

                            if (dwRet != WAIT_OBJECT_0)
                            {
                                Trace("SuspendThreadTest:WaitForSingleObject"
                                       " failed (%x)\n",GetLastError());
                            }
                            else
                            {
                                bRet = TRUE;
                            }
                        }
                    }
                }
            }
        }
    }
    else
    {
        Trace("SuspendThreadTest:CreateThread failed (%x)\n",GetLastError());
    }

    return bRet; 
}

int __cdecl main(int argc, char **argv)
{
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

#if defined(PLATFORM_UNIX) && (defined(_X86_) || defined(_IA64_) || defined(_SPARC_))
    /* Cannot do setthreadcontext test on FreeBSD and HPUX/IA64 when
       both calling thread and the suspended thread are in the same process.
       SetThreadContext is disabled on Solaris due to a threading library issue
       on Solaris 8. See VSWhidbey 343949 for details.*/
#else
    if(!SuspendThreadTest())
    {
        Fail ("Test failed\n");
    }
#endif
    
    PAL_Terminate();
    return (PASS);

}
