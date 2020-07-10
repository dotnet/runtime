// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//#include <stdio.h>
#include <assert.h>
#include "mtx_critsect.h"

CsWaiterReturnState MTXWaitOnCS(LPCRITICAL_SECTION lpCriticalSection);
void MTXDoActualWait(LPCRITICAL_SECTION lpCriticalSection);
void MTXWakeUpWaiter(LPCRITICAL_SECTION lpCriticalSection);

/*extern "C" { 
    	LONG InterlockedCompareExchange(
        LONG volatile *Destination,
        LONG Exchange,
        LONG Comperand);
}
*/
int MTXInitializeCriticalSection(LPCRITICAL_SECTION lpCriticalSection)
{
    int retcode = 0;

    lpCriticalSection->DebugInfo = NULL;
    lpCriticalSection->LockCount = 0;
    lpCriticalSection->RecursionCount = 0;
    lpCriticalSection->SpinCount = 0;
    lpCriticalSection->OwningThread = NULL;

    lpCriticalSection->LockSemaphore = (HANDLE)&lpCriticalSection->NativeData;
	
    if (0!= pthread_mutex_init(&lpCriticalSection->NativeData.Mutex, NULL))
    	{
    			printf("Error Initializing Critical Section\n");
			retcode = -1;
    	}
    

    lpCriticalSection->InitCount = CS_INITIALIZED;
    return retcode;
}

int MTXDeleteCriticalSection(LPCRITICAL_SECTION lpCriticalSection)
{
    int retcode = 0;
	 
    if (lpCriticalSection->InitCount == CS_INITIALIZED)
    {
        
        if (0!=pthread_mutex_destroy(&lpCriticalSection->NativeData.Mutex))
        	{
        		printf("Error Deleting Critical Section\n");
			retcode = -1;
        	}
    }

    lpCriticalSection->InitCount = CS_NOT_INIZIALIZED;
    return retcode;
}

int MTXEnterCriticalSection(LPCRITICAL_SECTION lpCriticalSection)
{
    
    DWORD thread_id;
    int retcode = 0;
	
    thread_id = (DWORD)THREADSilentGetCurrentThreadId();

    /* check if the current thread already owns the criticalSection */
    if (lpCriticalSection->OwningThread == (HANDLE)thread_id)
    {
        lpCriticalSection->RecursionCount++;
	//Check if this is a failure condition
	return 0;
    }

    	if (0!= pthread_mutex_lock(&lpCriticalSection->NativeData.Mutex))
    	{
    		//Error Condition
		printf("Error Entering Critical Section\n");
		retcode = -1;
    	}
	else
        {
        	lpCriticalSection->OwningThread = (HANDLE)thread_id;
		lpCriticalSection->RecursionCount = 1;
        }

    return retcode;
}

int MTXLeaveCriticalSection(LPCRITICAL_SECTION lpCriticalSection)
{
    int retcode = 0;

    if (--lpCriticalSection->RecursionCount > 0)
	//*****check this *****
	return 0;

    lpCriticalSection->OwningThread = 0;

    if (0!= pthread_mutex_unlock(&lpCriticalSection->NativeData.Mutex))
    	{
    		//Error Condition
		printf("Error Leaving Critical Section\n");
		retcode = -1;
    	}

	return retcode;
}
        
