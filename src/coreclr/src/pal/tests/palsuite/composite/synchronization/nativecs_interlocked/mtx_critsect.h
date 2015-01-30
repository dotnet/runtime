//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#include <pthread.h>

typedef void VOID;
typedef void* HANDLE;
typedef unsigned long ULONG_PTR;

#ifdef HPUX
 typedef unsigned int DWORD; 
 typedef int LONG;
 typedef unsigned int ULONG;
#else
 typedef unsigned long DWORD;
 typedef long LONG;
 typedef unsigned long ULONG;
#endif




#define FALSE 0
#define TRUE 1

#define CSBIT_CS_IS_LOCKED  1
#define CSBIT_NEW_WAITER    2

typedef enum CsInitState { CS_NOT_INIZIALIZED, CS_INITIALIZED, CS_FULLY_INITIALIZED } CsInitState;
typedef enum _CsWaiterReturnState { CS_WAITER_WOKEN_UP, CS_WAITER_DIDNT_WAIT } CsWaiterReturnState;

typedef struct _CRITICAL_SECTION_DEBUG_INFO {
    LONG volatile ContentionCount;
    LONG volatile InternalContentionCount;
    ULONG volatile AcquireCount;
    ULONG volatile EnterCount;
} CRITICAL_SECTION_DEBUG_INFO, *PCRITICAL_SECTION_DEBUG_INFO;

typedef struct _CRITICAL_SECTION_NATIVE_DATA {
    pthread_mutex_t Mutex;
} CRITICAL_SECTION_NATIVE_DATA, *PCRITICAL_SECTION_NATIVE_DATA;

typedef struct _CRITICAL_SECTION {
	
    CsInitState InitCount;
    PCRITICAL_SECTION_DEBUG_INFO DebugInfo;
    LONG LockCount;
    LONG RecursionCount;
    HANDLE OwningThread;
    HANDLE LockSemaphore;
    ULONG_PTR SpinCount;
    CRITICAL_SECTION_NATIVE_DATA NativeData;
    
} CRITICAL_SECTION, *PCRITICAL_SECTION, *LPCRITICAL_SECTION;

int MTXInitializeCriticalSection(LPCRITICAL_SECTION lpCriticalSection);
int MTXDeleteCriticalSection(LPCRITICAL_SECTION lpCriticalSection);
int MTXEnterCriticalSection(LPCRITICAL_SECTION lpCriticalSection);
int MTXLeaveCriticalSection(LPCRITICAL_SECTION lpCriticalSection);

extern "C" {
  LONG InterlockedCompareExchange(
				  LONG volatile *Destination,
				  LONG Exchange,
				  LONG Comperand);
}
