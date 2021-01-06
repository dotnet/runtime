// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <pthread.h>

typedef void VOID;
typedef unsigned long DWORD;
typedef long LONG;
typedef unsigned long ULONG;
typedef void* HANDLE;
typedef unsigned long ULONG_PTR;

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

